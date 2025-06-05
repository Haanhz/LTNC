using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

public class ChatForm : Form
{
    private string userName;

    private TextBox inputBox;
    private Button sendBtn;
    private ListBox chatBox;

    TcpClient client;
    NetworkStream stream;

    public ChatForm()
    {
        this.Text = "Quiz Client";
        this.Width = 400;
        this.Height = 400;

        chatBox = new ListBox() { Top = 10, Left = 10, Width = 360, Height = 250 };
        inputBox = new TextBox() { Top = 270, Left = 10, Width = 260 };
        sendBtn = new Button() { Text = "Send", Top = 270, Left = 280, Width = 90 };

        sendBtn.Click += SendBtn_Click;

        Controls.Add(chatBox);
        Controls.Add(inputBox);
        Controls.Add(sendBtn);

        userName = Prompt.ShowDialog("Nhập tên người dùng:", "Đăng nhập");

        ConnectToServer();
    }

    private void ConnectToServer()
    {
        client = new TcpClient("127.0.0.1", 8888);
        stream = client.GetStream();

        // Gửi tên người dùng sau khi kết nối
        byte[] nameData = Encoding.UTF8.GetBytes(userName);
        stream.Write(nameData, 0, nameData.Length);

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
                        // Nếu là câu hỏi thì hiển thị nổi bật
                        if (msg.StartsWith("Question:"))
                        {
                            chatBox.Items.Add("====== Question ======");
                            chatBox.Items.Add(msg);
                            chatBox.Items.Add("====================");
                        }
                        else if (msg.StartsWith("Final Scores:") || msg.Contains("Winner") || msg.Contains("tie"))
                        {
                            chatBox.Items.Add("====== KẾT QUẢ CUỐI CÙNG ======");
                            foreach (string line in msg.Split('\n'))
                            {
                                chatBox.Items.Add("🌟 " + line.Trim());
                            }
                            chatBox.Items.Add("===============================");
                        }
                        else
                        {
                            foreach (string line in msg.Split('\n'))
                            {
                                chatBox.Items.Add(line.Trim());
                            }
                        }
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
        string msg = inputBox.Text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        byte[] data = Encoding.UTF8.GetBytes(msg);
        stream.Write(data, 0, data.Length);

        chatBox.Items.Add(userName + ": " + msg);
        inputBox.Clear();

        if (msg.ToLower() == "bye")
        {
            stream.Close();
            client.Close();
            Application.Exit();
        }
    }

    // Hộp thoại nhập tên người dùng
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                Text = caption
            };
            Label lbl = new Label() { Left = 10, Top = 10, Text = text };
            TextBox txt = new TextBox() { Left = 10, Top = 40, Width = 250 };
            Button confirm = new Button() { Text = "OK", Left = 180, Width = 80, Top = 70 };

            string result = "";
            confirm.Click += (sender, e) => { result = txt.Text; prompt.Close(); };

            prompt.Controls.Add(lbl);
            prompt.Controls.Add(txt);
            prompt.Controls.Add(confirm);
            prompt.AcceptButton = confirm;

            prompt.ShowDialog();
            return result;
        }
    }
}
