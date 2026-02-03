using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WinFormsApp6
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;
        private bool isConnected = false;
        private string serverIP = "127.0.0.1"; // IP-адрес сервера
        private int port = 27015; // Порт сервера

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = "Готов к подключению..." + Environment.NewLine;
            UpdateConnectionStatus();
        }

        private void UpdateConnectionStatus()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateConnectionStatus));
                return;
            }

            if (isConnected)
            {
                this.Text = "Клиент чата - Подключено";
                button1.Enabled = true;
                getbutton.Text = "Отключиться";
                textBox2.Enabled = true;
            }
            else
            {
                this.Text = "Клиент чата - Не подключено";
                button1.Enabled = false;
                getbutton.Text = "Подключиться";
                textBox2.Enabled = false;
            }
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                client.Connect(serverIP, port);
                stream = client.GetStream();
                isConnected = true;

                AppendToChat("Подключено к серверу " + serverIP + ":" + port);

                // Запускаем поток для приема сообщений
                receiveThread = new Thread(new ThreadStart(ReceiveMessages));
                receiveThread.IsBackground = true;
                receiveThread.Start();

                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                AppendToChat("Ошибка подключения: " + ex.Message);
                isConnected = false;
                UpdateConnectionStatus();
            }
        }

        private void DisconnectFromServer()
        {
            try
            {
                isConnected = false;

                if (stream != null)
                    stream.Close();

                if (client != null)
                    client.Close();

                if (receiveThread != null && receiveThread.IsAlive)
                    receiveThread.Abort();

                AppendToChat("Отключено от сервера");
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                AppendToChat("Ошибка отключения: " + ex.Message);
            }
        }

        private void ReceiveMessages()
        {
            byte[] buffer = new byte[1024];
            StringBuilder messageBuilder = new StringBuilder();

            while (isConnected)
            {
                try
                {
                    if (stream.DataAvailable)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            messageBuilder.Append(receivedData);

                            // Проверяем, содержит ли сообствие символ конца строки
                            if (receivedData.Contains("\n") || receivedData.Contains(Environment.NewLine))
                            {
                                string fullMessage = messageBuilder.ToString().Trim();
                                AppendToChat("Сервер: " + fullMessage);
                                messageBuilder.Clear();
                            }
                        }
                    }
                    Thread.Sleep(100); // Небольшая задержка для снижения нагрузки на CPU
                }
                catch (Exception ex)
                {
                    if (isConnected) // Выводим ошибку только если мы еще должны быть подключены
                    {
                        AppendToChat("Ошибка приема: " + ex.Message);
                        DisconnectFromServer();
                    }
                    break;
                }
            }
        }

        private void SendMessageToServer(string message)
        {
            if (!isConnected || stream == null || client == null || !client.Connected)
            {
                AppendToChat("Не подключено к серверу");
                return;
            }

            try
            {
                // Обработка специальных команд
                if (message.Trim().ToLower() == "get()")
                {
                    AppendToChat("Запрашиваю историю чата...");
                }
                {
                    if (stream != null && stream.CanWrite)
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                        stream.Write(data, 0, data.Length);
                        stream.Flush();
                        AppendToChat("Вы: " + message);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToChat("Ошибка отправки: " + ex.Message);
                DisconnectFromServer();
            }
        }

        private void AppendToChat(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendToChat), message);
                return;
            }

            textBox1.AppendText(message + Environment.NewLine);

            // Автопрокрутка к последнему сообщению
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string message = textBox2.Text.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                SendMessageToServer(message);
                textBox2.Clear();
                textBox2.Focus();
            }
        }

        private void getbutton_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                DisconnectFromServer();
            }
            else
            {
                ConnectToServer();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift && isConnected)
            {
                e.SuppressKeyPress = true;
                button1_Click(sender, e);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectFromServer();
        }

        
    }
}
