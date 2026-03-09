using System;
using System.Drawing;
using System.Windows.Forms;
using PassportVisaService.Data;
using PassportVisaService.Models;

namespace PassportVisaService.Forms
{
    public partial class LoginForm : Form
    {
        private DatabaseContext dbContext;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnRegister;
        private Panel mainPanel;

        public LoginForm()
        {
            dbContext = new DatabaseContext();
            InitializeComponent();
            InitializeCustomComponent();
        }

        private void InitializeCustomComponent()
        {
            // Настройка формы - УВЕЛИЧИВАЕМ РАЗМЕР
            this.Text = "Вход в систему - Паспортно-визовая служба";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 248, 255);

            // Главная панель
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(240, 248, 255)
            };

            // Заголовок - УВЕЛИЧИВАЕМ РАЗМЕР
            var lblTitle = new Label
            {
                Text = "ПАСПОРТНО-ВИЗОВАЯ СЛУЖБА\nЕдиный портал государственных услуг",
                Location = new Point(50, 30),
                Size = new Size(400, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Метка Логин
            var lblUsername = new Label
            {
                Text = "Логин:",
                Location = new Point(80, 120),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Поле Логин - УВЕЛИЧИВАЕМ
            txtUsername = new TextBox
            {
                Location = new Point(190, 120),
                Size = new Size(220, 30),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Метка Пароль
            var lblPassword = new Label
            {
                Text = "Пароль:",
                Location = new Point(80, 170),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleRight
            };

            // Поле Пароль - УВЕЛИЧИВАЕМ
            txtPassword = new TextBox
            {
                Location = new Point(190, 170),
                Size = new Size(220, 30),
                Font = new Font("Segoe UI", 11),
                PasswordChar = '*',
                UseSystemPasswordChar = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Кнопка Войти - УВЕЛИЧИВАЕМ И СМЕЩАЕМ
            btnLogin = new Button
            {
                Text = "✅ ВОЙТИ",
                Location = new Point(120, 240),
                Size = new Size(120, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Кнопка Регистрация - УВЕЛИЧИВАЕМ И ДЕЛАЕМ ВИДИМОЙ
            btnRegister = new Button
            {
                Text = "📝 ЗАРЕГИСТРИРОВАТЬСЯ",
                Location = new Point(250, 240),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += BtnRegister_Click;

            // Добавление элементов на панель
            mainPanel.Controls.AddRange(new Control[] {
                lblTitle, lblUsername, txtUsername,
                lblPassword, txtPassword, btnLogin, btnRegister
            });

            // Добавление панели на форму
            this.Controls.Add(mainPanel);

            // Обработка нажатия Enter
            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var user = dbContext.GetUser(txtUsername.Text.Trim(), txtPassword.Text);

                if (user != null)
                {
                    MessageBox.Show($"Добро пожаловать, {user.FullName}!\n\nРоль: {user.Role}",
                                   "Успешный вход",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);

                    var mainForm = new MainForm(user);
                    mainForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            var registerForm = new RegisterForm();
            registerForm.ShowDialog();
        }
    }
}