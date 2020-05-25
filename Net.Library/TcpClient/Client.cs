using System;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace SomeProject.Library.Client
{
    public class Client
    {
        public TcpClient tcpClient;

        /// <summary>
        /// Сообщение, которое передает в первом байте информацию о действии пользователя
        /// </summary>
        enum MessageType { Message, File, NewClientID, Delete }

        /// <summary>
        /// ИД сессии пользователя
        /// </summary>
        public byte clientSessionID = 0;

        public Client(byte ID)
        {
            clientSessionID = ID;
        }

        /// <summary>
        /// Получение сообщение от сервера. Обработка количества пользователей.
        /// </summary>
        /// <returns></returns>
        public OperationResult ReceiveMessageFromServer()
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8081);
                StringBuilder recievedMessage = new StringBuilder();
                byte[] data = new byte[256];
                NetworkStream stream = tcpClient.GetStream();

                if (stream.ReadByte() == (byte)MessageType.NewClientID)
                {
                    clientSessionID = (byte)stream.ReadByte();

                    if (clientSessionID == 0)
                    {
                        return new OperationResult(Result.Fail, "Connection rejected: too many clients.");
                    }

                    return new OperationResult(Result.OK, "Connected to server.");
                }
                else
                {
                    do
                    {
                        int bytes = stream.Read(data, 0, data.Length);
                        recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    stream.Close();
                    tcpClient.Close();

                    return new OperationResult(Result.OK, recievedMessage.ToString());
                }
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.ToString());
            }
        }

        /// <summary>
        /// Отправление сообщения серверу
        /// </summary>
        /// <param name="message">Содержимое сообщения</param>
        /// <returns>Возвращает результат этого дествия</returns>
        public OperationResult SendMessageToServer(string message)
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8081);
                NetworkStream stream = tcpClient.GetStream();

                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
               
                stream.WriteByte(clientSessionID);
                stream.WriteByte((byte)MessageType.Message);

                stream.Write(data, 0, data.Length);
                stream.Close();
                tcpClient.Close();
                return new OperationResult(Result.OK, "") ;
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Разрыв соединения с сервером
        /// </summary>
        public void DisconnectFromServer()
        {

            tcpClient = new TcpClient("127.0.0.1", 8081);
            NetworkStream stream = tcpClient.GetStream();

            stream.WriteByte(clientSessionID);
            stream.WriteByte((byte)MessageType.Delete);

            stream.Close();
            tcpClient.Close();

        }

        /// <summary>
        /// Отправка файла на сервер. 3 первых байта отвечают за ИД, тип операции, длину расширения
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <returns>Возвращает результат этого дествия</returns>
        public OperationResult SendFileToServer(string path)
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8081);
                NetworkStream stream = tcpClient.GetStream();

                byte[] extension = System.Text.Encoding.UTF8.GetBytes(Path.GetExtension(path));
                byte[] data = File.ReadAllBytes(path);

                stream.WriteByte(clientSessionID);
                stream.WriteByte((byte)MessageType.File);

                stream.WriteByte(Convert.ToByte(extension.Length));

                stream.Write(extension, 0, extension.Length);
                stream.Write(data, 0, data.Length);

                stream.Close();
                tcpClient.Close();
                return new OperationResult(Result.OK, "");
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }
    }
}
