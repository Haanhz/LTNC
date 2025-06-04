using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public class ChatServerForm : Form
{
    private TextBox inputBox;
    private Button sendBtn;
    private ListBox chatBox;

    TcpListener server;
    TcpClient client;
    NetworkStream stream;

    private Dictionary<string, string> quizData = new Dictionary<string, string>()
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
        { "Can a deadlock occur in a single-threaded program?", "No" },
        { "Is overfitting a common problem in machine learning?", "Yes" },
        { "Is k-means a supervised learning algorithm?", "No" },
        { "Does gradient descent minimize a loss function?", "Yes" },
        { "Is backpropagation used in training neural networks?", "Yes" },
        { "Does increasing the number of epochs always improve accuracy?", "No" },
        { "Is ray tracing used in computer graphics for realistic rendering?", "Yes" },
        { "Is a pixel the smallest unit in a digital image?", "Yes" },
        { "Does a higher frame rate reduce motion blur in animations?", "Yes" },
        { "Is OpenGL used for web development?", "No" },
        { "Is anti-aliasing used to smooth jagged edges in images?", "Yes" }
    };

    private List<string> remainingQuestions;
    private string currentQuestion = "";
    private string currentAnswer = "";
    private int score = 0;

    public ChatServerForm()
    {
        this.Text = "AI Quiz Server";
        this.Width = 400;
        this.Height = 400;

        chatBox = new ListBox() { Top = 10, Left = 10, Width = 360, Height = 250 };
        inputBox = new TextBox() { Top = 270, Left = 10, Width = 260 };
        sendBtn = new Button() { Text = "Send", Top = 270, Left = 280, Width = 90 };

        sendBtn.Click += SendBtn_Click;

        Controls.Add(chatBox);
        Controls.Add(inputBox);
        Controls.Add(sendBtn);

        remainingQuestions = new List<string>(quizData.Keys);
        StartServer();
    }

    private void StartServer()
    {
        server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        Thread acceptThread = new Thread(() =>
        {
            client = server.AcceptTcpClient();
            stream = client.GetStream();
            SendNextQuestion();
            ListenForMessages();
        });
        acceptThread.IsBackground = true;
        acceptThread.Start();
    }

    private void SendNextQuestion()
    {
        if (remainingQuestions.Count == 15)
        {
            SendToClient("End game. Final score: " + score);
            this.Invoke(new Action(() =>
            {
                chatBox.Items.Add("Server: End game. Final score: " + score);
            }));
            return;
        }

        Random rand = new Random();
        int index = rand.Next(remainingQuestions.Count);
        currentQuestion = remainingQuestions[index];
        remainingQuestions.RemoveAt(index);
        currentAnswer = quizData[currentQuestion];

        SendToClient(currentQuestion);
        this.Invoke(new Action(() =>
        {
            chatBox.Items.Add("Server: " + currentQuestion);
        }));
    }

    private void ListenForMessages()
    {
        while (true)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int len = stream.Read(buffer, 0, buffer.Length);
                if (len == 0) break;
                string msg = Encoding.UTF8.GetString(buffer, 0, len);

                this.Invoke(new Action(() =>
                {
                    chatBox.Items.Add("Client: " + msg);
                }));

                HandleAnswer(msg);
            }
            catch { break; }
        }
    }

    private void HandleAnswer(string msg)
    {
        string response;
        if (msg.Trim().ToLower() == currentAnswer.Trim().ToLower())
        {
            score++;
            response = "Correct! Your score: " + score;
        }
        else
        {
            response = $"Wrong! The correct answer was: {currentAnswer}. Your score: {score}";
        }

        SendToClient(response);
        this.Invoke(new Action(() =>
        {
            chatBox.Items.Add("Server: " + response);
        }));

        Thread.Sleep(1000);
        SendNextQuestion();
    }

    private void SendToClient(string msg)
    {
        if (stream == null) return;

        byte[] data = Encoding.UTF8.GetBytes(msg);
        stream.Write(data, 0, data.Length);
    }

    private void SendBtn_Click(object sender, EventArgs e)
    {
        string msg = inputBox.Text;
        SendToClient(msg);
        chatBox.Items.Add("Me: " + msg);
        inputBox.Clear();

        if (msg.ToLower() == "bye")
        {
            stream.Close();
            client.Close();
            server.Stop();
            Application.Exit();
        }
    }
}