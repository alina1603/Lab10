using System;
using System.Windows.Forms;
using SomeProject.Library.Client;
using SomeProject.Library;

namespace SomeProject.TcpClient
{
    public partial class ClientMainWindow : Form
    {
        Client client;
        byte sessionID = 0;

        public ClientMainWindow()
        {
            client = new Client(sessionID);
            InitializeComponent();
        }

        /// <summary>
        /// Отправление сообщения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMsgBtnClick(object sender, EventArgs e)
        {

            Result res = client.SendMessageToServer(textBox.Text).Result;
            if(res == Result.OK)
            {
                textBox.Text = "";
                labelRes.Text = "Message was sent succefully!";
            }
            else
            {
                labelRes.Text = "Cannot send the message to the server.";
            }
            timer.Interval = 2000;
            timer.Start();
            //res = client.SendMessageToServer(textBox.Text).Result;
        }

        /// <summary>
        /// Отображает результат работы от сервера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object sender, EventArgs e)
        {
            labelRes.Text = "";
            timer.Stop();
        }

        /// <summary>
        /// Отправление файла на сервер
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sendFileButton_Click(object sender, EventArgs e)
        {
            openFileDialog.ShowDialog();

            Result res = client.SendFileToServer(openFileDialog.FileName).Result;
            if (res == Result.OK)
            {
                textBox.Text = "";
                labelRes.Text = "File was sent succefully!";
            }
            else
            {
                labelRes.Text = "Cannot send the file to the server.";
            }
            timer.Interval = 2000;
            timer.Start();
            res = client.SendFileToServer(openFileDialog.FileName).Result;
        }

        /// <summary>
        /// При загрузке формы получение ИД от сервера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientMainWindow_Load(object sender, EventArgs e)
        {
            OperationResult result = client.SendMessageToServer("Connected.");
            labelRes.Text = client.ReceiveMessageFromServer().Message;
            sessionID = client.clientSessionID;
        }

        /// <summary>
        /// При закрытии разрыв соединения
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClientMainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            client.DisconnectFromServer();
            sessionID = client.clientSessionID;
        }

        private void openFileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
