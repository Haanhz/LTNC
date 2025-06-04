using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public class ChatForm : Form
{
    private TextBox inputBox;
    private Button sendBtn;
    private ListBox chatBox;

    TcpClient client;
    NetworkStream stream;

    public ChatForm()
    {
        this.Text = "Chat Client";
        this.Width = 400;
        this.Height = 400;

        chatBox = new ListBox() { Top = 10, Left = 10, Width = 360, Height = 250 };
        inputBox = new TextBox() { Top = 270, Left = 10, Width = 260 };
        sendBtn = new Button() { Text = "Send", Top = 270, Left = 280, Width = 90 };

        sendBtn.Click += SendBtn_Click;

        Controls.Add(chatBox);
        Controls.Add(inputBox);
        Controls.Add(sendBtn);

        ConnectToServer();
    }

    private void ConnectToServer()
    {
        client = new TcpClient("127.0.0.1", 8888);
        stream = client.GetStream();

        Thread receiveThread = new Thread(() =>
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
                        chatBox.Items.Add("Server: " + msg);
                    }));
                }
                catch { break; }
            }
        });
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void SendBtn_Click(object sender, EventArgs e)
    {
        string msg = inputBox.Text;
        byte[] data = Encoding.UTF8.GetBytes(msg);
        stream.Write(data, 0, data.Length);
        chatBox.Items.Add("Me: " + msg);
        inputBox.Clear();

        if (msg.ToLower() == "bye")
        {
            stream.Close();
            client.Close();
            Application.Exit();
        }
    }
}
