using System.Net;
using System.Net.Sockets;
using System.Text;

// Создаем и запускаем сервер
ServerObject server = new ServerObject();
await server.ListenAsync();

class ServerObject
{
    TcpListener tcpListener = new TcpListener(IPAddress.Any, 27015);
    List<ClientObject> clients = new List<ClientObject>();

    // Для потокобезопасной работы со списком клиентов
    private readonly object clientsLock = new object();

    protected internal void RemoveConnection(string id)
    {
        lock (clientsLock)
        {
            ClientObject? client = clients.FirstOrDefault(c => c.Id == id);
            if (client != null)
            {
                clients.Remove(client);
                client?.Close();
                Console.WriteLine($"Клиент {id} отключен");
            }
        }
    }

    // Прослушивание входящих подключений
    protected internal async Task ListenAsync()
    {
        try
        {
            tcpListener.Start();
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (true)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                ClientObject clientObject = new ClientObject(tcpClient, this);
                lock (clientsLock)
                {
                    clients.Add(clientObject);
                }

                Console.WriteLine($"Новое подключение: {clientObject.Id}");
                _ = Task.Run(() => clientObject.ProcessAsync());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в ListenAsync: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    // Трансляция сообщения подключенным клиентам
    protected internal async Task BroadcastMessageAsync(string message, string id)
    {
        List<ClientObject> clientsCopy;
        lock (clientsLock)
        {
            clientsCopy = new List<ClientObject>(clients);
        }

        foreach (var client in clientsCopy)
        {
            if (client.Id != id) // если id клиента не равно id отправителя
            {
                try
                {
                    await client.Writer.WriteLineAsync(message);
                    await client.Writer.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка отправки клиенту {client.Id}: {ex.Message}");
                }
            }
        }
    }

    // Отключение всех клиентов
    protected internal void Disconnect()
    {
        Console.WriteLine("Отключение всех клиентов...");

        List<ClientObject> clientsCopy;
        lock (clientsLock)
        {
            clientsCopy = new List<ClientObject>(clients);
            clients.Clear();
        }

        foreach (var client in clientsCopy)
        {
            try
            {
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отключении клиента: {ex.Message}");
            }
        }

        tcpListener.Stop();
        Console.WriteLine("Сервер остановлен");
    }
}

class ClientObject
{
    protected internal string Id { get; } = Guid.NewGuid().ToString();
    protected internal StreamWriter Writer { get; }
    protected internal StreamReader Reader { get; }

    private TcpClient client;
    private ServerObject server;
    private string? userName;
    private static readonly object fileLock = new object();

    public ClientObject(TcpClient tcpClient, ServerObject serverObject)
    {
        client = tcpClient;
        server = serverObject;

        var stream = client.GetStream();
        Reader = new StreamReader(stream, Encoding.UTF8);
        Writer = new StreamWriter(stream, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    public async Task ProcessAsync()
    {
        try
        {
            // Получаем имя пользователя
            userName = await Reader.ReadLineAsync();
            string message = $"{userName} вошел в чат";

            // Посылаем сообщение о входе в чат всем подключенным пользователям
            await server.BroadcastMessageAsync(message, Id);
            Console.WriteLine(message);

            // В бесконечном цикле получаем сообщения от клиента
            while (true)
            {
                try
                {
                    string? clientMessage = await Reader.ReadLineAsync();

                    if (clientMessage == null)
                    {
                        // Клиент отключился
                        break;
                    }

                    if (clientMessage.Trim() == "get()") // trim удаляет пробельные символы в начале и конце

                    {
                        Console.WriteLine("Чтение из файла");
                        string text = ReadFromFile();
                        Console.WriteLine($"Прочитано из файла: {text}");

                        // Отправляем прочитанное обратно клиенту
                        await Writer.WriteLineAsync($"История чата: {text}");
                    }
                    else
                    {
                        Console.WriteLine("Запись в файл");
                        string formattedMessage = $"{userName}: {clientMessage}";
                        Console.WriteLine(formattedMessage);

                        WriteToFile(formattedMessage);

                        // Рассылаем сообщение всем клиентам
                        await server.BroadcastMessageAsync(formattedMessage, Id);
                    }
                }
                catch (IOException)
                {
                    // Ошибка чтения/записи - клиент отключился
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
                    // Продолжаем работу
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Ошибка в ProcessAsync для клиента {Id}: {e.Message}");
        }
        finally
        {
            // При выходе из цикла отправляем сообщение о выходе
            if (!string.IsNullOrEmpty(userName))
            {
                string leaveMessage = $"{userName} покинул чат";
                Console.WriteLine(leaveMessage);

                try
                {
                    await server.BroadcastMessageAsync(leaveMessage, Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Не удалось отправить сообщение о выходе: {ex.Message}");
                }
            }

            // Закрываем подключение
            server.RemoveConnection(Id);
        }
    }

    private void WriteToFile(string message)
    {
        lock (fileLock)
        {
            try
            {
                using (var fileWriter = new StreamWriter("chattext.txt", true, Encoding.UTF8))
                {
                    fileWriter.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в файл: {ex.Message}");
            }
        }
    }

    private string ReadFromFile()
    {
        lock (fileLock)
        {
            try
            {
                if (File.Exists("chattext.txt"))
                {
                    using (var fileReader = new StreamReader("chattext.txt", Encoding.UTF8))
                    {
                        return fileReader.ReadToEnd();
                    }
                }
                else
                {
                    return "Файл истории не найден";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения файла: {ex.Message}");
                return $"Ошибка чтения файла: {ex.Message}";
            }
        }
    }

    // Закрытие подключения
    protected internal void Close()
    {
        try
        {
            Writer?.Close();
        }
        catch { }

        try
        {
            Reader?.Close();
        }
        catch { }

        try
        {
            client?.Close();
        }
        catch { }
    }
}
