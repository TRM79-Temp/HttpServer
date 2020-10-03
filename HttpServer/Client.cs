using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HttpServer
{
    // Класс-обработчик клиента
    class Client
    {
        // Отправка страницы с ошибкой
        private void SendError(TcpClient Client, int Code)
        {
            // Получаем строку вида "200 OK"
            // HttpStatusCode хранит в себе все статус-коды HTTP/1.1
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            // Код простой HTML-странички
            string Html = "<html><body><h1>" + CodeStr + "</h1></body></html>";
            // Необходимые заголовки: ответ сервера, тип и длина содержимого. После двух пустых строк - само содержимое
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            // Приведем строку к виду массива байт
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            // Отправим его клиенту
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            // Закроем соединение
            Client.Close();
        }

        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public Client(TcpClient Client, WebProxy proxy, string key)
        {
            try
            {
                // Объявим строку, в которой будет хранится запрос клиента
                string Request = "";
                // Буфер для хранения принятых от клиента данных
                byte[] Buffer = new byte[1024];
                // Переменная для хранения количества байт, принятых от клиента
                int Count;
                // Читаем из потока клиента до тех пор, пока от него поступают данные
                while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
                {
                    // Преобразуем эти данные в строку и добавим ее к переменной Request
                    Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                    // Запрос должен обрываться последовательностью \r\n\r\n
                    // Либо обрываем прием данных сами, если длина строки Request превышает 4 килобайта
                    // Нам не нужно получать данные из POST-запроса (и т. п.), а обычный запрос
                    // по идее не должен быть больше 4 килобайт
                    if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096)
                    {
                        break;
                    }
                }

                // Парсим строку запроса с использованием регулярных выражений
                // При этом отсекаем все переменные GET-запроса
                Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");

                // Если запрос не удался
                if (ReqMatch == Match.Empty)
                {
                    // Передаем клиенту ошибку 400 - неверный запрос
                    SendError(Client, 400);
                    return;
                }

                // Получаем строку запроса
                string RequestUri = ReqMatch.Groups[1].Value;

                // Приводим ее к изначальному виду, преобразуя экранированные символы
                // Например, "%20" -> " "
                RequestUri = Uri.UnescapeDataString(RequestUri);

                // Если в строке содержится двоеточие, передадим ошибку 400
                // Это нужно для защиты от URL типа http://example.com/../../file.txt
                if (RequestUri.IndexOf("..") >= 0)
                {
                    SendError(Client, 400);
                    return;
                }

                if (RequestUri == String.Empty)
                {
                    throw new ArgumentException("WTF???");
                }

                RequestUri = RequestUri.Substring(1);
                Console.WriteLine(RequestUri);

                // Тип содержимого
                string ContentType = "application/unknown";

                byte[] data = null;

                using (var client = new WebClient())
                {
                    if (proxy != null)
                    {
                        client.Proxy = proxy;
                    }

                    data = client.DownloadData(RequestUri);

                    data = Crypt(data, key);

                    // Посылаем заголовки
                    string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + data.Length + "\n\n";
                    byte[] HeadersBuffer = Encoding.ASCII.GetBytes(Headers);
                    Client.GetStream().Write(HeadersBuffer, 0, HeadersBuffer.Length);

                    Client.GetStream().Write(data, 0, data.Length);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Client Exception:");
                Console.WriteLine(exception);
            }
            finally
            {
                Client.Close();
            }
        }

        static byte[] Crypt(byte[] data, string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(key);
            }

            if (key == String.Empty)
            {
                return data;
            }

            var result = new byte[data.Length];

            var passBytes = Encoding.UTF8.GetBytes(key);
            var count = 0;

            for (var i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ passBytes[count++]);

                if (count == passBytes.Length)
                {
                    count = 0;
                }
            }

            return result;
        }

    }
}
