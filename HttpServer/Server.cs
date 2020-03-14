using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpServer
{
    // Based on https://habr.com/ru/post/120157/.

    public class ClientParams
    {
        public TcpClient Client { get; set; }
        public WebProxy Proxy { get; set; }
        public string Key { get; set; }
    }

    class Server
    {
        TcpListener Listener; // Объект, принимающий TCP-клиентов

        // Запуск сервера
        public Server(int Port, WebProxy proxy, string Key)
        {
            // Создаем "слушателя" для указанного порта
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start(); // Запускаем его

            Console.WriteLine("Server started.");

            // В бесконечном цикле
            while (true)
            {
                // Принимаем новых клиентов. После того, как клиент был принят, он передается в новый поток (ClientThread)
                // с использованием пула потоков.
                ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread),
                    new ClientParams {
                        Client = Listener.AcceptTcpClient(),
                        Proxy = proxy,
                        Key = Key
                    }
                );
            }
        }

        // Остановка сервера
        ~Server()
        {
            // Если "слушатель" был создан
            if (Listener != null)
            {
                // Остановим его
                Listener.Stop();
            }
        }

        static void ClientThread(Object StateInfo)
        {
            var clientParams = (ClientParams)StateInfo;
            new Client(clientParams.Client, clientParams.Proxy, clientParams.Key);
        }

        static void Main(string[] args)
        {
            // Создадим новый сервер на порту 80
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            int port;
            if (!int.TryParse(args[0], out port))
            {
                ShowUsage();
                return;
            }

            Console.WriteLine("Starting server on port {0}...", port);

            // Определим нужное максимальное количество потоков
            // Пусть будет по 4 на каждый процессор
            int MaxThreadsCount = Environment.ProcessorCount * 4;
            // Установим максимальное количество рабочих потоков
            ThreadPool.SetMaxThreads(MaxThreadsCount, MaxThreadsCount);
            // Установим минимальное количество рабочих потоков
            ThreadPool.SetMinThreads(2, 2);

            var webProxy = GetProxy("proxy.ini");

            var key = String.Empty;
            if (args.Length > 1)
            {
                key = args[1];
            }

            try
            {
                new Server(port, webProxy, key);
            }
            catch (Exception exception)
            {
                Console.WriteLine("An error occured:");
                Console.WriteLine(exception.ToString());
                Console.WriteLine("\nTry to run the program on another port.");
            }
        }

        static WebProxy GetProxy(string proxyFileName)
        {
            try
            {
                using (var reader = new StreamReader(proxyFileName))
                {
                    var proxyUrl    = reader.ReadLine();
                    var login    = reader.ReadLine();
                    var password = reader.ReadLine();

                    WebProxy proxy = new WebProxy(proxyUrl, true);
                    proxy.Credentials = new NetworkCredential(login, password);
                    WebRequest.DefaultWebProxy = proxy;

                    return proxy;
                }
            }
            catch
            {
                return null;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine(@"The program uasge:
    HttpServer port [key]");
        }
    }
}
