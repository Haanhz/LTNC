using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public class ChatServerForm : Form
{
    private ListBox chatBox;
    private TcpListener server;
    private List<ClientHandler> clients = new List<ClientHandler>();
    private Dictionary<ClientHandler, int> scores = new Dictionary<ClientHandler, int>();
    private Dictionary<string, string> quizData;
    private string currentQuestion = "";
    private string currentAnswer = "";
    private bool hasAnsweredCorrectly = false;
    private bool gameEnded = false;

    public string CurrentQuestion => currentQuestion;

    public ChatServerForm()
    {
        this.Text = "Quiz Server";
        this.Width = 500;
        this.Height = 400;

        chatBox = new ListBox() { Top = 10, Left = 10, Width = 460, Height = 330 };
        Controls.Add(chatBox);

        ResetQuizData();
        StartServer();
    }

    private void ResetQuizData()
    {
        quizData = new Dictionary<string, string>()
        {
            { "Is Python a compiled language?", "No" },
            { "Does a stack follow the FIFO principle?", "No" },
            { "Can a binary tree have more than two children per node?", "No" },
            { "Is RAM volatile memory?", "Yes" },
            { "Is HTML a programming language?", "No" },
            { "Can recursion always be replaced with iteration?", "No" },
            { "Is IPv6 longer than IPv4?", "Yes" },
            { "Does the OSI model have 7 layers?", "Yes" },
            { "Is public an access modifier in Java?", "Yes" },
            { "Can a deadlock occur in a single-threaded program?", "No" }
        };
    }

    private void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 8888);
        server.Start();

        Thread acceptThread = new Thread(() =>
        {
            while (true)
            {
                TcpClient tcpClient = server.AcceptTcpClient();
                ClientHandler handler = new ClientHandler(tcpClient, this);
                clients.Add(handler);
                scores[handler] = 0;

                Thread clientThread = new Thread(handler.HandleClient);
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        });
        acceptThread.IsBackground = true;
        acceptThread.Start();

        Thread.Sleep(1000);
        BroadcastNextQuestion();
    }

    public void Broadcast(string msg)
    {
        foreach (var client in clients)
        {
            client.SendMessage(msg);
        }
        AddMessage("Server: " + msg);
    }

    public void AddMessage(string msg)
    {
        if (chatBox.InvokeRequired)
        {
            chatBox.Invoke(new Action(() => chatBox.Items.Add(msg)));
        }
        else
        {
            chatBox.Items.Add(msg);
        }
    }

    public void HandleAnswer(ClientHandler sender, string message)
    {
        if (gameEnded)
        {
            if (message.ToLower() == "yes")
            {
                Broadcast($"{sender.UserName} wants to play again!");
                RestartGame();
            }
            else if (message.ToLower() == "no")
            {
                sender.SendMessage("Okay. You can wait or close the app.");
            }
            return;
        }

        if (hasAnsweredCorrectly)
        {
            sender.SendMessage("Too late! Someone already answered correctly.");
            return;
        }

        if (message.Trim().ToLower() == currentAnswer.Trim().ToLower())
        {
            hasAnsweredCorrectly = true;
            scores[sender]++;
            Broadcast($"{sender.UserName} answered correctly! Score: {scores[sender]}");

            Thread.Sleep(1500);
            BroadcastNextQuestion();
        }
        else
        {
            sender.SendMessage("Wrong answer.");
        }
    }

    private void BroadcastNextQuestion()
    {
        if (quizData.Count == 0)
        {
            gameEnded = true;
            Broadcast("Quiz ended! Let's see the results...");

            var topScore = scores.Values.Max();
            var winners = scores.Where(p => p.Value == topScore).Select(p => p.Key.UserName).ToList();

            string ranking = "Final Scores:\n";
            foreach (var pair in scores.OrderByDescending(p => p.Value))
            {
                ranking += $"• {pair.Key.UserName}: {pair.Value} điểm\n";
            }

            if (winners.Count == 1)
                Broadcast($"Winner: {winners[0]} with {topScore} points!");
            else
                Broadcast($"It's a tie between: {string.Join(", ", winners)} with {topScore} points!");

            Broadcast(ranking.Trim());

            // Hỏi người chơi có muốn chơi lại
            Broadcast("Do you want to play another round? (yes/no)");
            return;
        }

        var rand = new Random();
        int index = rand.Next(quizData.Count);
        currentQuestion = quizData.Keys.ElementAt(index);
        currentAnswer = quizData[currentQuestion];
        quizData.Remove(currentQuestion);

        hasAnsweredCorrectly = false;
        Broadcast("Question: " + currentQuestion);
    }

    private void RestartGame()
    {
        if (!gameEnded) return;

        gameEnded = false;
        ResetQuizData();

        foreach (var client in clients)
        {
            scores[client] = 0;
        }

        Broadcast("Game restarted! Let's begin a new round.");
        Thread.Sleep(1000);
        BroadcastNextQuestion();
    }
}

public class ClientHandler
{
    private TcpClient client;
    private NetworkStream stream;
    private ChatServerForm serverForm;
    public string UserName { get; private set; }

    public ClientHandler(TcpClient tcpClient, ChatServerForm form)
    {
        this.client = tcpClient;
        this.serverForm = form;
    }

    public void HandleClient()
    {
        try
        {
            stream = client.GetStream();

            // Nhận tên người dùng
            byte[] buffer = new byte[1024];
            int nameLen = stream.Read(buffer, 0, buffer.Length);
            UserName = Encoding.UTF8.GetString(buffer, 0, nameLen).Trim();
            serverForm.AddMessage($"{UserName} joined the game.");

            // Gửi hướng dẫn cho người chơi
            SendMessage("Welcome!");
            SendMessage("Trả lời câu hỏi bằng cách nhập yes/ no rồi ấn send.\n");
            SendMessage("Người trả lời đúng ĐẦU TIÊN sẽ được điểm.\n");
            SendMessage("Trả lời sai hoặc chậm sẽ không được điểm.\n");
            SendMessage("Nhập bye để thoát game.\n");

            // Gửi thông báo đến các client khác
            serverForm.AddMessage($"{UserName} joined the game.\n");
            serverForm.Broadcast($"* {UserName} has joined the game.\n");

            // Gửi lại câu hỏi hiện tại cho người mới
            if (!string.IsNullOrEmpty(serverForm.CurrentQuestion))
            {
                SendMessage("Question: " + serverForm.CurrentQuestion);
            }

            while (true)
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                if (len == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, len).Trim();
                serverForm.AddMessage($"{UserName}: {message}");
                serverForm.HandleAnswer(this, message);
            }
        }
        catch
        {
            serverForm.AddMessage($"{UserName} disconnected.");
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    public void SendMessage(string msg)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);
        }
        catch { }
    }
}
