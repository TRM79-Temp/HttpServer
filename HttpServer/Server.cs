using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer
{
    // Based on https://habr.com/ru/post/120157/.

    class Server
    {
        TcpListener Listener; // Объект, принимающий TCP-клиентов

        // Запуск сервера
        public Server(int Port)
        {
            // Создаем "слушателя" для указанного порта
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start(); // Запускаем его

            Console.WriteLine("Server started.");

            // В бесконечном цикле
            while (true)
            {
                // Принимаем новых клиентов и передаем их на обработку новому экземпляру класса Client
                new Client(Listener.AcceptTcpClient());
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

        static void Main(string[] args)
        {
            // Создадим новый сервер на порту 80
            if (args.Length != 1)
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

            try
            {
                new Server(port);
            }
            catch (Exception exception)
            {
                Console.WriteLine("An error occured:");
                Console.WriteLine(exception.ToString());
                Console.WriteLine("\nTry to run the program on another port.");
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine(@"The program uasge:
    HttpServer port");
        }
    }
}
