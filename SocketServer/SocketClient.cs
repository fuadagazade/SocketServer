using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    public class SocketClient : IDisposable
    {
        public Socket Socket { get; }

        public byte[] Buffer { get; }
        public byte[] Received { get;}

        public int ReceivedCount { get; set; }
        public int Summary { get; set; }

        public int BufferSize { get; set; }

        public SocketClient(Socket client)
        {
            this.BufferSize = 2048;
            this.Socket = client;
            this.Buffer = new byte[this.BufferSize];
            this.Received = new byte[this.BufferSize];
            this.ReceivedCount = 0;
            this.Summary = 0;
        }

        public string ReceivedText()
        {
            return Encoding.ASCII.GetString(Received, 0, Received.TakeWhile(c => c != 0).Count());
        }

        public void Dispose()
        {
            this.Socket.Shutdown(SocketShutdown.Both);
            this.Socket.Close();
        }
    }
}
