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
        this.Width = 650;
        this.Height = 620;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = System.Drawing.Color.FromArgb(18, 32, 47); // Deep tech blue

        Label title = new Label()
        {
            Text = "⎓ QUIZ GAME SERVER ⎓",
            Font = new System.Drawing.Font("Consolas", 26, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.Cyan,
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Top = 18,
            Left = 18,
            Width = 600,
            Height = 60
        };

        Panel borderPanel = new Panel()
        {
            Top = 85,
            Left = 18,
            Width = 600,
            Height = 420,
            BackColor = System.Drawing.Color.FromArgb(28, 41, 56),
            BorderStyle = BorderStyle.FixedSingle
        };

        chatBox = new ListBox()
        {
            Top = 10,
            Left = 10,
            Width = 580,
            Height = 400,
            Font = new System.Drawing.Font("Consolas", 13, System.Drawing.FontStyle.Bold),
            BackColor = System.Drawing.Color.FromArgb(10, 10, 20),
            ForeColor = System.Drawing.Color.Lime,
            BorderStyle = BorderStyle.None
        };
        borderPanel.Controls.Add(chatBox);

        Label statusLabel = new Label()
        {
            Text = "⎓ Server đang chạy trên cổng 8888...",
            Top = 520,
            Left = 18,
            Width = 600,
            Height = 30,
            Font = new System.Drawing.Font("Consolas", 12, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic),
            ForeColor = System.Drawing.Color.Cyan,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };

        Label decoLabel = new Label()
        {
            Text = "─────────────────────────────────────────────────────────────────────────────",
            Top = 75,
            Left = 18,
            Width = 600,
            Height = 10,
            ForeColor = System.Drawing.Color.Cyan,
            Font = new System.Drawing.Font("Consolas", 12, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        Controls.Add(title);
        Controls.Add(decoLabel);
        Controls.Add(borderPanel);
        Controls.Add(statusLabel);

        // Họa tiết mạch điện tử vẽ ở ngoài panel chứa câu hỏi (bên ngoài borderPanel)
        Panel decorPanel = new Panel()
        {
            Top = borderPanel.Top - 10,
            Left = borderPanel.Left - 30,
            Width = borderPanel.Width + 60,
            Height = borderPanel.Height + 60,
            BackColor = System.Drawing.Color.Transparent
        };
        decorPanel.SendToBack();
        decorPanel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            var pen = new System.Drawing.Pen(System.Drawing.Color.Cyan, 2);
            var pen2 = new System.Drawing.Pen(System.Drawing.Color.Lime, 1);
            // Vẽ các đường mạch chạy quanh borderPanel
            g.DrawLine(pen, 20, 30, 580, 30);
            g.DrawLine(pen, 580, 30, 580, 420);
            g.DrawLine(pen, 580, 420, 20, 420);
            g.DrawLine(pen, 20, 420, 20, 30);
            // Vẽ các node mạch ở góc
            g.FillEllipse(System.Drawing.Brushes.Cyan, 15, 25, 10, 10);
            g.FillEllipse(System.Drawing.Brushes.Lime, 575, 25, 10, 10);
            g.FillEllipse(System.Drawing.Brushes.Cyan, 575, 415, 10, 10);
            g.FillEllipse(System.Drawing.Brushes.Lime, 15, 415, 10, 10);
            // Vẽ chip ở góc trên phải
            g.FillRectangle(System.Drawing.Brushes.Cyan, 520, 10, 40, 18);
            g.DrawRectangle(pen, 520, 10, 40, 18);
            // Vẽ chip ở góc dưới trái
            g.FillRectangle(System.Drawing.Brushes.Lime, 30, 430, 40, 18);
            g.DrawRectangle(pen2, 30, 430, 40, 18);
        };
        Controls.Add(decorPanel);
        decorPanel.BringToFront();
        borderPanel.BringToFront();

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