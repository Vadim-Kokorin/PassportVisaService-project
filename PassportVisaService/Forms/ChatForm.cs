using System;
using System.Drawing;
using System.Windows.Forms;
using PassportVisaService.Data;
using PassportVisaService.Models;
using ChatMessage = PassportVisaService.Models.Message;

namespace PassportVisaService.Forms
{
    public partial class ChatForm : Form
    {
        private User currentUser;
        private User chatUser;
        private int ticketId;
        private DatabaseContext dbContext;

        private ListBox messagesListBox;
        private TextBox txtMessage;
        private Button btnSend;
        private Timer refreshTimer;

        public ChatForm(User current, User other, int ticketId, string subject)
        {
            currentUser = current;
            chatUser = other;
            this.ticketId = ticketId;
            dbContext = new DatabaseContext();

            InitializeComponent();
            InitializeCustomComponent(subject);
            LoadMessages();
            StartAutoRefresh();

            dbContext.MarkMessagesAsRead(ticketId, currentUser.Id);
        }

        private void InitializeCustomComponent(string subject)
        {
            this.Text = $"💬 Чат: {subject}";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 450);
            this.BackColor = Color.FromArgb(240, 248, 255);

            // Верхняя информационная панель
            var infoPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 51, 102),
                Padding = new Padding(10)
            };

            var lblChatWith = new Label
            {
                Text = $"Чат с: {chatUser.FullName} ({chatUser.Role})",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };

            var lblStatus = new Label
            {
                Text = "🟢 Онлайн",
                Location = new Point(10, 35),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.LightGreen
            };

            infoPanel.Controls.Add(lblChatWith);
            infoPanel.Controls.Add(lblStatus);

            // Панель сообщений
            var messagesPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 10),
                BackColor = Color.FromArgb(240, 248, 255)
            };

            messagesListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawVariable,
                IntegralHeight = false,
                ItemHeight = 40
            };
            messagesListBox.DrawItem += MessagesListBox_DrawItem;
            messagesListBox.MeasureItem += MessagesListBox_MeasureItem;

            messagesPanel.Controls.Add(messagesListBox);

            // Нижняя панель ввода
            var inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            txtMessage = new TextBox
            {
                Location = new Point(10, 15),
                Size = new Size(500, 40),
                Font = new Font("Segoe UI", 10),
                Multiline = true,
                MaxLength = 500,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };
            txtMessage.KeyDown += TxtMessage_KeyDown;

            btnSend = new Button
            {
                Text = "📨 ОТПРАВИТЬ",
                Location = new Point(520, 15),
                Size = new Size(140, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;

            inputPanel.Controls.Add(txtMessage);
            inputPanel.Controls.Add(btnSend);

            // Собираем форму
            this.Controls.Add(messagesPanel);
            this.Controls.Add(inputPanel);
            this.Controls.Add(infoPanel);
        }

        private void MessagesListBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index >= 0 && e.Index < messagesListBox.Items.Count)
            {
                var message = (ChatMessage)messagesListBox.Items[e.Index];
                int lines = (message.Content.Length / 50) + 1;
                e.ItemHeight = 50 + (lines * 20);
            }
        }

        private void MessagesListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var message = (ChatMessage)messagesListBox.Items[e.Index];
            bool isMyMessage = message.SenderId == currentUser.Id;

            e.DrawBackground();

            Color backColor = isMyMessage ?
                Color.FromArgb(212, 239, 223) :
                Color.FromArgb(232, 240, 254);

            using (var backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
            {
                e.Graphics.DrawRectangle(pen,
                    e.Bounds.X, e.Bounds.Y,
                    e.Bounds.Width - 1, e.Bounds.Height - 1);
            }

            string senderName = isMyMessage ? "Вы" : message.SenderName;
            using (var nameFont = new Font("Segoe UI", 9, FontStyle.Bold))
            using (var nameBrush = new SolidBrush(isMyMessage ? Color.Green : Color.FromArgb(0, 51, 102)))
            {
                e.Graphics.DrawString(senderName, nameFont, nameBrush,
                    e.Bounds.X + 10, e.Bounds.Y + 5);
            }

            using (var timeFont = new Font("Segoe UI", 8))
            using (var timeBrush = new SolidBrush(Color.Gray))
            {
                string timeStr = message.Timestamp.ToString("HH:mm");
                e.Graphics.DrawString(timeStr, timeFont, timeBrush,
                    e.Bounds.X + e.Bounds.Width - 60, e.Bounds.Y + 8);
            }

            using (var textFont = new Font("Segoe UI", 10))
            using (var textBrush = new SolidBrush(Color.Black))
            {
                var textRect = new RectangleF(
                    e.Bounds.X + 15,
                    e.Bounds.Y + 30,
                    e.Bounds.Width - 90,
                    e.Bounds.Height - 40);

                e.Graphics.DrawString(message.Content, textFont, textBrush, textRect);
            }

            if (isMyMessage && message.IsRead)
            {
                using (var readFont = new Font("Segoe UI", 7))
                using (var readBrush = new SolidBrush(Color.Green))
                {
                    e.Graphics.DrawString("✓ Прочитано", readFont, readBrush,
                        e.Bounds.X + e.Bounds.Width - 80,
                        e.Bounds.Y + e.Bounds.Height - 20);
                }
            }

            e.DrawFocusRectangle();
        }

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                SendMessage();
                e.SuppressKeyPress = true;
            }
        }

        private void BtnSend_Click(object sender, EventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text))
            {
                MessageBox.Show("Введите сообщение!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = new ChatMessage
            {
                SenderId = currentUser.Id,
                ReceiverId = chatUser.Id,
                Content = txtMessage.Text.Trim(),
                Timestamp = DateTime.Now,
                IsRead = false,
                TicketId = ticketId
            };

            dbContext.SendMessage(message);
            txtMessage.Clear();
            LoadMessages();
            txtMessage.Focus();
        }

        private void LoadMessages()
        {
            var messages = dbContext.GetMessages(ticketId);

            messagesListBox.BeginUpdate();
            messagesListBox.Items.Clear();

            foreach (var msg in messages)
            {
                messagesListBox.Items.Add(msg);
            }

            messagesListBox.EndUpdate();

            if (messagesListBox.Items.Count > 0)
            {
                messagesListBox.TopIndex = messagesListBox.Items.Count - 1;
            }

            dbContext.MarkMessagesAsRead(ticketId, currentUser.Id);
        }

        private void StartAutoRefresh()
        {
            refreshTimer = new Timer { Interval = 2000 };
            refreshTimer.Tick += (s, e) =>
            {
                var currentMessages = dbContext.GetMessages(ticketId);
                if (currentMessages.Count != messagesListBox.Items.Count)
                {
                    LoadMessages();
                }
            };
            refreshTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            refreshTimer?.Dispose();
            base.OnFormClosing(e);
        }

        private void ChatForm_Load(object sender, EventArgs e)
        {

        }
    }
}