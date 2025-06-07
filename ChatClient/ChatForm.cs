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

    private TcpClient? client;
    private NetworkStream? stream;

    public ChatForm()
    {
        this.Text = "Quiz Client";
        this.Width = 520;
        this.Height = 620;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = System.Drawing.Color.FromArgb(18, 32, 47); // Deep tech blue

        Label title = new Label()
        {
            Text = "⎓ QUIZ GAME CLIENT ⎓",
            Font = new System.Drawing.Font("Consolas", 22, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.Cyan,
            AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
            Top = 18,
            Left = 18,
            Width = 470,
            Height = 50
        };

        Panel borderPanel = new Panel()
        {
            Top = 85,
            Left = 18,
            Width = 470,
            Height = 430, // tăng chiều cao panel
            BackColor = System.Drawing.Color.FromArgb(28, 41, 56),
            BorderStyle = BorderStyle.FixedSingle
        };

        chatBox = new ListBox()
        {
            Top = 10,
            Left = 10,
            Width = 450,
            Height = 410, // tăng chiều cao chatbox
            Font = new System.Drawing.Font("Consolas", 11, System.Drawing.FontStyle.Bold), // giảm font size
            BackColor = System.Drawing.Color.FromArgb(10, 10, 20),
            ForeColor = System.Drawing.Color.Lime,
            BorderStyle = BorderStyle.None
        };
        borderPanel.Controls.Add(chatBox);

        Label inputLabel = new Label()
        {
            Text = "Gõ câu trả lời hoặc chat (ENTER để gửi):",
            Top = 525,
            Left = 18,
            Width = 470,
            Font = new System.Drawing.Font("Consolas", 10, System.Drawing.FontStyle.Italic),
            ForeColor = System.Drawing.Color.Cyan
        };

        inputBox = new TextBox()
        {
            Top = 555,
            Left = 18,
            Width = 330,
            Font = new System.Drawing.Font("Consolas", 13),
            BackColor = System.Drawing.Color.FromArgb(10, 10, 20),
            ForeColor = System.Drawing.Color.Lime,
            BorderStyle = BorderStyle.FixedSingle
        };
        sendBtn = new Button()
        {
            Text = "SEND ⎓",
            Top = 553,
            Left = 360,
            Width = 128,
            Height = 40,
            Font = new System.Drawing.Font("Consolas", 13, System.Drawing.FontStyle.Bold),
            BackColor = System.Drawing.Color.Cyan,
            ForeColor = System.Drawing.Color.FromArgb(18, 32, 47),
            FlatStyle = FlatStyle.Flat
        };
        sendBtn.FlatAppearance.BorderSize = 0;

        Label decoLabel = new Label()
        {
            Text = "─────────────────────────────────────────────────────────────",
            Top = 75,
            Left = 18,
            Width = 470,
            Height = 10,
            ForeColor = System.Drawing.Color.Cyan,
            Font = new System.Drawing.Font("Consolas", 12, System.Drawing.FontStyle.Bold),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        sendBtn.Click += SendBtn_Click;
        inputBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) SendBtn_Click(sendBtn, e); };

        Controls.Add(title);
        Controls.Add(decoLabel);
        Controls.Add(borderPanel);
        Controls.Add(inputLabel);
        Controls.Add(inputBox);
        Controls.Add(sendBtn);

        userName = Prompt.ShowDialog("Nhập tên người dùng:", "Đăng nhập");

        ConnectToServer();

        // Họa tiết mạch điện tử bằng các đường line và hình chữ nhật nhỏ
        Panel decorPanel = new Panel()
        {
            Top = 85,
            Left = 0,
            Width = this.Width,
            Height = 430,
            BackColor = System.Drawing.Color.Transparent
        };
        decorPanel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            var pen = new System.Drawing.Pen(System.Drawing.Color.Cyan, 2);
            var pen2 = new System.Drawing.Pen(System.Drawing.Color.Lime, 1);
            // Vẽ các đường mạch ngang dọc
            g.DrawLine(pen, 40, 50, 200, 50);
            g.DrawLine(pen, 200, 50, 200, 350);
            g.DrawLine(pen, 200, 200, 400, 200);
            g.DrawLine(pen2, 60, 100, 60, 350);
            g.DrawLine(pen2, 60, 100, 300, 100);
            // Vẽ các node mạch
            g.FillEllipse(System.Drawing.Brushes.Cyan, 195, 45, 10, 10);
            g.FillEllipse(System.Drawing.Brushes.Lime, 55, 95, 10, 10);
            g.FillEllipse(System.Drawing.Brushes.Cyan, 295, 95, 10, 10);
            g.FillEllipse(System.Drawing.Brushes.Lime, 395, 195, 10, 10);
            // Vẽ chip
            g.FillRectangle(System.Drawing.Brushes.Cyan, 120, 270, 40, 20);
            g.DrawRectangle(pen, 120, 270, 40, 20);
            g.DrawLine(pen2, 140, 290, 140, 350);
        };
        borderPanel.Controls.Add(decorPanel);
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

    private void SendBtn_Click(object? sender, EventArgs e)
    {
        string msg = inputBox.Text.Trim();
        if (string.IsNullOrEmpty(msg) || stream == null || client == null) return;

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