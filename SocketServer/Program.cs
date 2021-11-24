using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    class Program
    {
        private static readonly Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<SocketClient> clients = new List<SocketClient>();

        static void Main(string[] args)
        {
            Console.Title = "Socket Server";

            int port = -1;

            if(args.Length > 0)
            {
                foreach (string command in args)
                {
                    if(command.Contains("--port") || command.Contains("-P"))
                    {
                        port = Int32.Parse(command.Split(':')[1]);
                    }
                }
            }
            else
            {
                Console.WriteLine("You need send port. Example SocketServer --port:123 or SocketServer -P:123");
                return;
            }

            if(port == -1)
            {
                Console.WriteLine("Wrong port");
                return;
            }

            Console.WriteLine("Welcome to Socket Server!");

            IPAddress ipAddress = GetIPAddress();

            Console.WriteLine("Connect by: " + ipAddress.ToString() + ":" + port);

            StartServer(ipAddress, port);

            while (Console.ReadLine() != "stop") { }

            StopServer();

            Console.WriteLine("Server stopped!");
        }

        private static IPAddress GetIPAddress()
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            IPAddress ip = hostEntry.AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

            return ip;
        }

        private static void StartServer(IPAddress ipAddress, int port)
        {
            try
            {
                IPEndPoint endPoint = new IPEndPoint(ipAddress, port);
                socket.Bind(endPoint);
                socket.Listen(0);

                socket.BeginAccept(Accept, null);

                Console.WriteLine("Server started!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void StopServer()
        {
            foreach (SocketClient client in clients)
            {
                client.Dispose();
            }

            socket.Close();
        }

        private static void Accept(IAsyncResult result)
        {
            try
            {
                SocketClient client = new SocketClient(socket.EndAccept(result));
                clients.Add(client);

                client.Socket.BeginReceive(client.Buffer, 0, client.BufferSize, SocketFlags.None, Receive, client.Socket);

                Console.WriteLine($"Client {client.Socket.RemoteEndPoint} connected");

                socket.BeginAccept(Accept, null);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Receive(IAsyncResult result)
        {
            SocketClient client = clients.FirstOrDefault(c=>c.Socket == (Socket)result.AsyncState);

            int received = 0;

            try
            { 
                received = client.Socket.EndReceive(result);

               if(received == 0)
                {
                    Console.WriteLine($"Client {client.Socket.RemoteEndPoint} disconnected!");

                    client.Dispose();
                    clients.Remove(client);

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {client.Socket.RemoteEndPoint} disconnected!");

                client.Dispose();
                clients.Remove(client);

                return;
            }

            Array.Copy(client.Buffer, 0, client.Received, client.ReceivedCount++, received);

            string text = client.ReceivedText();
            int number;

            if (text.Contains(Environment.NewLine))
            {
                if (text == "stop\r\n")
                {
                    Console.WriteLine($"Client {client.Socket.RemoteEndPoint} disconnected!");

                    SendMessage(client, "Connection Closed");

                    client.Dispose();
                    clients.Remove(client);

                    return;
                }
                else if(text == "list\r\n")
                {
                    foreach (SocketClient cl in clients)
                    {
                        SendMessage(client, $"{cl.Socket.RemoteEndPoint}: {cl.Summary}");
                    }
                }
                else if(Int32.TryParse(text,out number))
                {
                    Console.WriteLine($"{client.Socket.RemoteEndPoint} client send: {number}");
                    client.Summary += number;
                    SendMessage(client, $"Total : {client.Summary}");
                }
                else
                {
                    SendMessage(client, "Wrong Command!");
                }

                Array.Clear(client.Received, 0, client.Received.Length);
                client.ReceivedCount = 0;

            }

            if (client.Socket.Connected) client.Socket.BeginReceive(client.Buffer, 0, client.BufferSize, SocketFlags.None, Receive, client.Socket);

        }

        private static void SendMessage(SocketClient client, string message, bool newLine = true){
            byte[] data = Encoding.ASCII.GetBytes((newLine ? Environment.NewLine : "") + message + (newLine ? Environment.NewLine:""));
            client.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, Send, client.Socket);
        }

        private static void Send(IAsyncResult result)
        {
            if (!result.IsCompleted)
            {
                Console.WriteLine("Error Sending data!");
            }
        }

    }
}
