using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GlassPanel
{
    // Dependency-free HTTP + WebSocket server on a single port, on a raw Socket.
    //  - GET (not a WS upgrade) -> serves the embedded panel page.
    //  - WS upgrade             -> kept for broadcast frames.
    internal class MiniServer
    {
        private const string WS_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private readonly int _port;
        private readonly byte[] _pageBytes;
        private readonly List<Socket> _clients = new List<Socket>();
        private readonly object _lock = new object();

        private Socket _listen;
        private Thread _acceptThread;
        private volatile bool _running;

        // Raised (off-thread) when a connected panel sends us a message.
        public System.Action<string> OnMessage;

        public MiniServer(int port, string pageHtml)
        {
            _port = port;
            _pageBytes = Encoding.UTF8.GetBytes(pageHtml ?? "");
        }

        public void Start()
        {
            try
            {
                _running = true;
                _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listen.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listen.Bind(new IPEndPoint(IPAddress.Any, _port));
                _listen.Listen(16);
                Plugin.Log?.LogInfo("MiniServer LISTENING on " + _listen.LocalEndPoint);
                _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "GlassPanel-accept" };
                _acceptThread.Start();
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError("MiniServer bind FAILED: " + ex);
            }
        }

        public void Stop()
        {
            _running = false;
            try { _listen?.Close(); } catch { }
            lock (_lock)
            {
                foreach (Socket c in _clients) { try { c.Close(); } catch { } }
                _clients.Clear();
            }
        }

        public int ClientCount { get { lock (_lock) { return _clients.Count; } } }

        private void AcceptLoop()
        {
            while (_running)
            {
                Socket client = null;
                try { client = _listen.Accept(); }
                catch { if (!_running) break; Thread.Sleep(50); continue; }
                try { Handle(client); }
                catch (Exception ex)
                {
                    Plugin.Log?.LogWarning("client handle error: " + ex.Message);
                    try { client.Close(); } catch { }
                }
            }
        }

        private void Handle(Socket sock)
        {
            string request;
            using (var stream = new NetworkStream(sock, ownsSocket: false))
                request = ReadHeaders(stream);

            if (request == null) { sock.Close(); return; }

            if (request.IndexOf("upgrade: websocket", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string key = ExtractHeader(request, "sec-websocket-key");
                if (key == null) { sock.Close(); return; }

                string accept;
                using (SHA1 sha = SHA1.Create())
                    accept = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(key + WS_GUID)));

                string resp = "HTTP/1.1 101 Switching Protocols\r\n" +
                              "Upgrade: websocket\r\nConnection: Upgrade\r\n" +
                              "Sec-WebSocket-Accept: " + accept + "\r\n\r\n";
                sock.Send(Encoding.ASCII.GetBytes(resp));
                lock (_lock) { _clients.Add(sock); }
                Plugin.Log?.LogInfo("panel connected (" + ClientCount + " total)");
                new Thread(() => ReadLoop(sock)) { IsBackground = true, Name = "GlassPanel-read" }.Start();
            }
            else
            {
                StringBuilder head = new StringBuilder();
                head.Append("HTTP/1.1 200 OK\r\n");
                head.Append("Content-Type: text/html; charset=utf-8\r\n");
                head.Append("Content-Length: ").Append(_pageBytes.Length).Append("\r\n");
                head.Append("Cache-Control: no-cache\r\nConnection: close\r\n\r\n");
                sock.Send(Encoding.ASCII.GetBytes(head.ToString()));
                sock.Send(_pageBytes);
                sock.Close();
            }
        }

        private static string ReadHeaders(NetworkStream stream)
        {
            byte[] buf = new byte[4096];
            StringBuilder sb = new StringBuilder();
            stream.ReadTimeout = 5000;
            while (sb.Length < 16384)
            {
                int n;
                try { n = stream.Read(buf, 0, buf.Length); }
                catch { return null; }
                if (n <= 0) break;
                sb.Append(Encoding.ASCII.GetString(buf, 0, n));
                if (sb.ToString().IndexOf("\r\n\r\n", StringComparison.Ordinal) >= 0) break;
            }
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static string ExtractHeader(string request, string name)
        {
            foreach (string line in request.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                int c = line.IndexOf(':');
                if (c > 0 && line.Substring(0, c).Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(c + 1).Trim();
            }
            return null;
        }

        public void Broadcast(string message)
        {
            byte[] frame = EncodeTextFrame(message);
            lock (_lock)
            {
                for (int i = _clients.Count - 1; i >= 0; i--)
                {
                    Socket c = _clients[i];
                    try { c.Send(frame); }
                    catch { try { c.Close(); } catch { } _clients.RemoveAt(i); }
                }
            }
        }

        // Read masked client->server frames (RFC 6455) and surface text payloads.
        private void ReadLoop(Socket sock)
        {
            try
            {
                NetworkStream ns = new NetworkStream(sock, false);
                while (_running && sock.Connected)
                {
                    int b0 = ns.ReadByte(); if (b0 < 0) break;
                    int b1 = ns.ReadByte(); if (b1 < 0) break;
                    int opcode = b0 & 0x0F;
                    bool masked = (b1 & 0x80) != 0;
                    long len = b1 & 0x7F;
                    if (len == 126) { len = (ReadN(ns) << 8) | ReadN(ns); }
                    else if (len == 127) { len = 0; for (int i = 0; i < 8; i++) len = (len << 8) | (long)ReadN(ns); }
                    if (len < 0 || len > 65536) break;
                    byte[] mask = new byte[4]; if (masked) ReadFull(ns, mask, 4);
                    byte[] payload = new byte[len]; ReadFull(ns, payload, (int)len);
                    if (masked) for (int i = 0; i < len; i++) payload[i] ^= mask[i & 3];
                    if (opcode == 0x8) break;                       // close
                    if (opcode == 0x1 && OnMessage != null)         // text
                        OnMessage(Encoding.UTF8.GetString(payload));
                }
            }
            catch { }
            finally { lock (_lock) { _clients.Remove(sock); } try { sock.Close(); } catch { } }
        }

        private static int ReadN(NetworkStream ns) { int b = ns.ReadByte(); if (b < 0) throw new IOException(); return b; }
        private static void ReadFull(NetworkStream ns, byte[] buf, int count)
        {
            int off = 0;
            while (off < count) { int n = ns.Read(buf, off, count - off); if (n <= 0) throw new IOException(); off += n; }
        }

        // Single unmasked server->client text frame (RFC 6455).
        private static byte[] EncodeTextFrame(string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            int len = payload.Length;
            byte[] header;
            if (len < 126)
                header = new byte[] { 0x81, (byte)len };
            else if (len <= ushort.MaxValue)
                header = new byte[] { 0x81, 126, (byte)(len >> 8), (byte)(len & 0xFF) };
            else
                header = new byte[] { 0x81, 127, 0, 0, 0, 0,
                    (byte)(len >> 24), (byte)(len >> 16), (byte)(len >> 8), (byte)(len & 0xFF) };

            byte[] frame = new byte[header.Length + len];
            Array.Copy(header, frame, header.Length);
            Array.Copy(payload, 0, frame, header.Length, len);
            return frame;
        }
    }
}
