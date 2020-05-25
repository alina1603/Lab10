using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SomeProject.Library.Server
{
    public class Server
    {
        TcpListener serverListener;


        int countFile = 0;
        int maxClients = 2;
        int idForNewClient = 1;

        /// <summary>
        /// Список пользователей
        /// </summary>
        List<byte> clients = new List<byte>();


        enum MessageType { Message, File, NewClientID, Delete }

        /// <summary>
        /// Обеспечивает функционал сервера TCP
        /// </summary>
        public Server()
        {
            serverListener = new TcpListener(IPAddress.Loopback, 8081);
        }

        /// <summary>
        /// Сервер ожидает сообщение от клиента
        /// </summary>
        /// <returns>Результат работы программы</returns>
        public async Task TurnOnListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Start();
                while (true)
                {
                    OperationResult result = await ReceiveSmthFromClient();
                    switch (result.Result)
                    {
                        case Result.Fail:
                            Console.WriteLine("Unexpected error: " + result.Message);
                            SendMessageToClient("Server error: " + result.Message);
                            break;
                        case Result.OK:
                            Console.WriteLine("New message from client " + result.Message);
                            result = SendMessageToClient("Server recieved message successfully!");
                            break;
                        case Result.ForbiddenUser:
                            Console.WriteLine("New client rejected: too many clients." + result.Message);
                            SendNewIDToClient(0);
                            break;
                        case Result.OkFile:
                            Console.WriteLine("New file from client " + result.Message);
                            result = SendMessageToClient("Server recieved file successfully!");
                            break;
                        default:
                            Console.WriteLine(result.Message);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn on listener: " + e.Message);
            }
        }


        /// <summary>
        /// Проверка количества клиентов. Если все проходит успешно, 
        /// то отправляем на сервер сообщение о новом клиенте. Увеличиваем кличество пользователей
        /// </summary>
        /// <param name="clientID"></param>
        /// <returns>ИД клиента</returns>
        public int CheckClient(int clientID)
        {
            if (clientID == 0 && clients.Count < maxClients)
            {
                clients.Add((byte)idForNewClient);
                SendNewIDToClient((byte)idForNewClient);
                Interlocked.Increment(ref idForNewClient);
                return 0;
            }
            else if (clients.Contains((byte)clientID))
            {
                return clientID;
            }

            return -1;
        }

        /// <summary>
        /// Определяем, какое действие хочет от нас пользователь. 
        /// И вызываем его в отдельном потоке
        /// </summary>
        /// <returns></returns>
        public async Task<OperationResult> ReceiveSmthFromClient()
        {
            try
            {
                Console.WriteLine("Waiting for connections...");
                TcpClient client = serverListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                int clientID = CheckClient(stream.ReadByte());

                if (clientID == -1)
                {
                    return new OperationResult(Result.ForbiddenUser, "");
                }
                else if (clientID == 0)
                {
                    return new OperationResult(Result.OkNewUser, "New client added. ID: " + clients[clients.Count - 1]);
                }

                OperationResult res = new OperationResult(Result.Fail, "Unknown message format.");

                int msgType = stream.ReadByte();

                switch (msgType)
                {
                    case (int)MessageType.Message:
                        res = await ReceiveMessageFromClient(stream);
                        res.Message = clientID + ": " + res.Message;
                        break;
                    case (int)MessageType.File:
                        res = await ReceiveFileFromClient(stream);
                        break;
                    case (int)MessageType.Delete:
                        clients.Remove((byte)clientID);
                        res.Result = Result.OkDeleted;
                        res.Message = "User " + clientID + " disconnected";
                        break;
                }

                stream.Close();
                client.Close();

                return res;
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }

        }

        /// <summary>
        /// Получение файла от клиента
        /// </summary>
        /// <param name="stream">Поток, по которому идет передача</param>
        /// <returns>Результат операции</returns>
        public async Task<OperationResult> ReceiveFileFromClient(NetworkStream stream)
        {
            try
            {
                string extension = getExtension(stream);

                string newDirectory = Directory.GetCurrentDirectory() + @"\" + DateTime.Today.ToString("yyyy-MM-dd");
                Directory.CreateDirectory(newDirectory);
                string newPath = newDirectory + @"\File" + countFile + extension;

                int recievedFileNumberTmp = countFile;

                Interlocked.Increment(ref countFile);

                FileStream file = new FileStream(newPath, FileMode.OpenOrCreate);

                byte[] data = new byte[256];
                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    file.Write(data, 0, bytes);
                }
                while (stream.DataAvailable);

                file.Close();

                return new OperationResult(Result.OkFile, " recieved and saved as " + "File" + recievedFileNumberTmp + extension);
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Получение расширения файла
        /// </summary>
        /// <param name="stream">Поток, по которому идет передача</param>
        /// <returns>Расширение файла</returns>
        private string getExtension(NetworkStream stream)
        {
            int exSize = stream.ReadByte();
            byte[] extention = new byte[exSize];

            stream.Read(extention, 0, exSize);

            return Encoding.UTF8.GetString(extention, 0, exSize);
        }

        /// <summary>
        /// Отправка ИД клиенту
        /// </summary>
        /// <param name="id">Ид</param>
        /// <returns>Результат операции</returns>
        public OperationResult SendNewIDToClient(byte id)
        {
            try
            {
                TcpClient client = serverListener.AcceptTcpClient();

                NetworkStream stream = client.GetStream();

                stream.WriteByte((byte)MessageType.NewClientID);
                stream.WriteByte(id);

                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
            return new OperationResult(Result.OK, "");
        }

        /// <summary>
        /// Отправка сообщения клиенту
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public OperationResult SendMessageToClient(string message)
        {
            try
            {
                TcpClient client = serverListener.AcceptTcpClient();

                NetworkStream stream = client.GetStream();

                stream.WriteByte((byte)MessageType.Message);

                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);

                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
            return new OperationResult(Result.OK, "");
        }


        /// <summary>
        /// Получение сообщение от клиента
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Результат операции</returns>
        public async Task<OperationResult> ReceiveMessageFromClient(NetworkStream stream)
        {
            try
            {
                StringBuilder recievedMessage = new StringBuilder();

                byte[] data = new byte[256];

                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);

                return new OperationResult(Result.OK, recievedMessage.ToString());
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

    }
}