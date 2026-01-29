using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Net.Http;

namespace WinFormsApp6
{
    public partial class Form1 : Form
    {
        private TcpClient? client;
        private StreamReader? reader;
        private StreamWriter? writer;
        private string? userName;
        private bool isConnected = false;

        private const string host = "127.0.0.1";
        private const int port = 27015;
        public Form1()
        {
            InitializeComponent();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        public void button1_Click(object sender, EventArgs e)
        {
            
        }
            
        async private void getbutton_Click(object sender, EventArgs e)
        {
            writer.Write("reertirreleeenneerrtttoooorpppeqqqwweeaasdddkfkkkffaaasssdaaasd");
            string message = await reader.ReadToEndAsync();
            textBox1.Text = message;
        }
        //async private void ReceiveData()
        //{
        //    byte[] buffer = new byte[1024];

        //    while (client?.Connected == true)
        //    {
        //        try
        //        {
        //            byte[] data = new byte[512];
        //            var stream = client.GetStream();
        //            if (data != null)
        //            {
        //                // Преобразуем байты в строку
        //                int bytes = await stream.ReadAsync(data);
        //                // получаем отправленное время
        //                string receivedData = Encoding.UTF8.GetString(data, 0, bytes);

        //                // Выводим в TextBox (используем Invoke для безопасности потока)
        //                Invoke(new Action(() =>
        //                {
        //                    textBox1.AppendText($"Получено: {receivedData}\r\n");
        //                    textBox1.ScrollToCaret(); // Автопрокрутка
        //                }));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Invoke(new Action(() =>
        //            {
        //                textBox1.AppendText($"Ошибка приема: {ex.Message}\r\n");
        //            }));
        //            break;
        //        }
        //    }
        //}

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {

                if (textBox2.Text != string.Empty)
                {
                    MessageBox.Show("Имя не может быть пустым!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    // Подключение к серверу
                    client = new TcpClient();
                    client.ConnectAsync(host, port);

                    var stream = client.GetStream();
                    reader = new StreamReader(stream, Encoding.UTF8);
                    writer = new StreamWriter(stream, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                    writer.WriteLineAsync(userName);

                    isConnected = true;
                    textBox1.AppendText($"Добро пожаловать, {userName}\n");
                    textBox1.AppendText("Подключение к серверу установлено.\n");
                    textBox1.AppendText("Для отправки сообщений введите текст и нажмите Enter или кнопку 'Отправить'.\n");

                }
            }
            catch (Exception ex)
            {


                MessageBox.Show($"Ошибка подключения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
    }

