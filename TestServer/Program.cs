﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WatsonTcp;

namespace TestServer
{
    internal class TestServer
    {
        private static string serverIp = "";
        private static int serverPort = 0;
        private static bool useSsl = false;
        private static WatsonTcpServer server = null;
        private static string certFile = "";
        private static string certPass = "";
        private static bool debug = false;
        private static bool acceptInvalidCerts = true;
        private static bool mutualAuthentication = true;

        private static void Main(string[] args)
        {
            serverIp = InputString("Server IP:", "127.0.0.1", false);
            serverPort = InputInteger("Server port:", 9000, true, false);
            useSsl = InputBoolean("Use SSL:", false);

            if (!useSsl)
            {
                server = new WatsonTcpServer(serverIp, serverPort);
            }
            else
            { 
                certFile = InputString("Certificate file:", "test.pfx", false);
                certPass = InputString("Certificate password:", "password", false); 
                acceptInvalidCerts = InputBoolean("Accept invalid certs:", true);
                mutualAuthentication = InputBoolean("Mutually authenticate:", false);

                server = new WatsonTcpServer(serverIp, serverPort, certFile, certPass);
                server.AcceptInvalidCertificates = acceptInvalidCerts;
                server.MutuallyAuthenticate = mutualAuthentication;
            }

            server.ClientConnected = ClientConnected;
            server.ClientDisconnected = ClientDisconnected;
            server.MessageReceived = MessageReceived;
            server.Debug = debug;

            // server.Start();
            Task serverStart = server.StartAsync();

            bool runForever = true;
            while (runForever)
            {
                Console.Write("Command [? for help]: ");
                string userInput = Console.ReadLine();

                List<string> clients;
                string ipPort;
                bool success = false;

                if (String.IsNullOrEmpty(userInput)) continue;

                switch (userInput)
                {
                    case "?":
                        Console.WriteLine("Available commands:");
                        Console.WriteLine("  ?          help (this menu)");
                        Console.WriteLine("  q          quit");
                        Console.WriteLine("  cls        clear screen");
                        Console.WriteLine("  list       list clients");
                        Console.WriteLine("  dispose    dispose of the connection");
                        Console.WriteLine("  send       send message to client");
                        Console.WriteLine("  sendasync  send message to a client asynchronously");
                        Console.WriteLine("  remove     disconnect client");
                        Console.WriteLine("  psk        set preshared key");
                        Console.WriteLine("  debug      enable/disable debug (currently " + server.Debug + ")");
                        break;

                    case "q":
                        runForever = false;
                        break;

                    case "cls":
                        Console.Clear();
                        break;

                    case "list":
                        clients = server.ListClients();
                        if (clients != null && clients.Count > 0)
                        {
                            Console.WriteLine("Clients");
                            foreach (string curr in clients)
                            {
                                Console.WriteLine("  " + curr);
                            }
                        }
                        else
                        {
                            Console.WriteLine("None");
                        }
                        break;

                    case "dispose":
                        server.Dispose();
                        break;

                    case "send":
                        Console.Write("IP:Port: ");
                        ipPort = Console.ReadLine();
                        if (String.IsNullOrEmpty(ipPort)) break;
                        Console.Write("Data: ");
                        userInput = Console.ReadLine();
                        if (String.IsNullOrEmpty(userInput)) break;
                        success = server.Send(ipPort, Encoding.UTF8.GetBytes(userInput));
                        Console.WriteLine(success);
                        break;

                    case "sendasync":
                        Console.Write("IP:Port: ");
                        ipPort = Console.ReadLine();
                        if (String.IsNullOrEmpty(ipPort)) break;
                        Console.Write("Data: ");
                        userInput = Console.ReadLine();
                        if (String.IsNullOrEmpty(userInput)) break;
                        success = server.SendAsync(ipPort, Encoding.UTF8.GetBytes(userInput)).Result;
                        Console.WriteLine(success);
                        break;

                    case "remove":
                        Console.Write("IP:Port: ");
                        ipPort = Console.ReadLine();
                        server.DisconnectClient(ipPort);
                        break;

                    case "psk":
                        server.PresharedKey = InputString("Preshared key:", "1234567812345678", false);
                        break;

                    case "debug":
                        server.Debug = !server.Debug;
                        Console.WriteLine("Debug set to: " + server.Debug);
                        break;

                    default:
                        break;
                }
            }
        }

        private static bool InputBoolean(string question, bool yesDefault)
        {
            Console.Write(question);

            if (yesDefault) Console.Write(" [Y/n]? ");
            else Console.Write(" [y/N]? ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                if (yesDefault) return true;
                return false;
            }

            userInput = userInput.ToLower();

            if (yesDefault)
            {
                if (
                    (String.Compare(userInput, "n") == 0)
                    || (String.Compare(userInput, "no") == 0)
                   )
                {
                    return false;
                }

                return true;
            }
            else
            {
                if (
                    (String.Compare(userInput, "y") == 0)
                    || (String.Compare(userInput, "yes") == 0)
                   )
                {
                    return true;
                }

                return false;
            }
        }

        private static string InputString(string question, string defaultAnswer, bool allowNull)
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

        private static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
        {
            while (true)
            {
                Console.Write(question);
                Console.Write(" [" + defaultAnswer + "] ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    return defaultAnswer;
                }

                int ret = 0;
                if (!Int32.TryParse(userInput, out ret))
                {
                    Console.WriteLine("Please enter a valid integer.");
                    continue;
                }

                if (ret == 0)
                {
                    if (allowZero)
                    {
                        return 0;
                    }
                }

                if (ret < 0)
                {
                    if (positiveOnly)
                    {
                        Console.WriteLine("Please enter a value greater than zero.");
                        continue;
                    }
                }

                return ret;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static async Task ClientConnected(string ipPort)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Console.WriteLine("Client connected: " + ipPort);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static async Task ClientDisconnected(string ipPort)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Console.WriteLine("Client disconnected: " + ipPort);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private static async Task MessageReceived(string ipPort, byte[] data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            string msg = "";
            if (data != null && data.Length > 0)
            {
                msg = Encoding.UTF8.GetString(data);
            }

            Console.WriteLine("Message received from " + ipPort + ": " + msg);
        }
    }
}