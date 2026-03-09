using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PassportVisaService.Data;
using PassportVisaService.Models;

namespace PassportVisaService.Forms
{
    public partial class RegisterForm : Form
    {
        private DatabaseContext dbContext;

        // Элементы управления (инициализируются в InitializeCustomComponent)
        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtConfirmPassword;
        private TextBox txtFullName;
        private TextBox txtEmail;
        private TextBox txtPhone;
        private ComboBox cmbCitizenship;
        private DateTimePicker dtpBirthDate;
        private CheckBox chkAgreeTerms;

        public RegisterForm()
        {
            dbContext = new DatabaseContext();
            InitializeComponent();
            InitializeCustomComponent();
        }

        private void InitializeCustomComponent()
        {
            // Настройка формы
            this.Text = "Регистрация в Паспортно-визовой службе";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(240, 248, 255);

            // Главная панель с прокруткой
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true,
                BackColor = Color.FromArgb(240, 248, 255)
            };

            int yPos = 10;

            // Заголовок
            var lblMainTitle = new Label
            {
                Text = "РЕГИСТРАЦИЯ НОВОГО ПОЛЬЗОВАТЕЛЯ",
                Location = new Point(50, yPos),
                Size = new Size(500, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };
            mainPanel.Controls.Add(lblMainTitle);
            yPos += 50;

            // ГРУППА 1: Данные для входа
            var loginGroup = CreateGroupBox("Данные для входа", yPos, 150);
            int gy = 25;

            // Логин
            var lblUsername = CreateLabel("Логин:", 20, gy, 120);
            txtUsername = CreateTextBox(150, gy, 350, true);
            AddHint(txtUsername, "От 4 до 20 символов (латиница, цифры)");
            loginGroup.Controls.AddRange(new Control[] { lblUsername, txtUsername });
            gy += 35;

            // Пароль
            var lblPassword = CreateLabel("Пароль:", 20, gy, 120);
            txtPassword = CreateTextBox(150, gy, 350, true);
            txtPassword.PasswordChar = '*';
            txtPassword.UseSystemPasswordChar = true;
            AddHint(txtPassword, "Минимум 6 символов, буквы и цифры");
            loginGroup.Controls.AddRange(new Control[] { lblPassword, txtPassword });
            gy += 35;

            // Подтверждение пароля
            var lblConfirmPassword = CreateLabel("Подтверждение:", 20, gy, 120);
            txtConfirmPassword = CreateTextBox(150, gy, 350, true);
            txtConfirmPassword.PasswordChar = '*';
            txtConfirmPassword.UseSystemPasswordChar = true;
            loginGroup.Controls.AddRange(new Control[] { lblConfirmPassword, txtConfirmPassword });

            mainPanel.Controls.Add(loginGroup);
            yPos += 160;

            // ГРУППА 2: Личные данные
            var personalGroup = CreateGroupBox("Личные данные", yPos, 150);
            gy = 25;

            // ФИО
            var lblFullName = CreateLabel("ФИО:", 20, gy, 120);
            txtFullName = CreateTextBox(150, gy, 350, true);
            AddHint(txtFullName, "Иванов Иван Иванович");
            personalGroup.Controls.AddRange(new Control[] { lblFullName, txtFullName });
            gy += 35;

            // Дата рождения
            var lblBirthDate = CreateLabel("Дата рождения:", 20, gy, 120);
            dtpBirthDate = new DateTimePicker
            {
                Location = new Point(150, gy),
                Size = new Size(350, 25),
                Format = DateTimePickerFormat.Short,
                MaxDate = DateTime.Now.AddYears(-18),
                MinDate = DateTime.Now.AddYears(-100)
            };
            personalGroup.Controls.AddRange(new Control[] { lblBirthDate, dtpBirthDate });
            gy += 35;

            // Гражданство
            var lblCitizenship = CreateLabel("Гражданство:", 20, gy, 120);
            cmbCitizenship = new ComboBox
            {
                Location = new Point(150, gy),
                Size = new Size(350, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCitizenship.Items.AddRange(new object[] {
                "Российская Федерация",
                "Республика Беларусь",
                "Республика Казахстан",
                "Другое"
            });
            cmbCitizenship.SelectedIndex = 0;
            personalGroup.Controls.AddRange(new Control[] { lblCitizenship, cmbCitizenship });

            mainPanel.Controls.Add(personalGroup);
            yPos += 160;

            // ГРУППА 3: Контактные данные
            var contactGroup = CreateGroupBox("Контактные данные", yPos, 120);
            gy = 25;

            // Email
            var lblEmail = CreateLabel("Email:", 20, gy, 120);
            txtEmail = CreateTextBox(150, gy, 350, true);
            AddHint(txtEmail, "example@mail.ru");
            contactGroup.Controls.AddRange(new Control[] { lblEmail, txtEmail });
            gy += 35;

            // Телефон
            var lblPhone = CreateLabel("Телефон:", 20, gy, 120);
            txtPhone = CreateTextBox(150, gy, 350, true);
            AddHint(txtPhone, "+7 (999) 999-99-99");
            contactGroup.Controls.AddRange(new Control[] { lblPhone, txtPhone });

            mainPanel.Controls.Add(contactGroup);
            yPos += 130;

            // СОГЛАСИЕ
            chkAgreeTerms = new CheckBox
            {
                Text = "Я согласен на обработку персональных данных",
                Location = new Point(50, yPos + 20),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            mainPanel.Controls.Add(chkAgreeTerms);

            // КНОПКИ
            var btnRegister = new Button
            {
                Text = "✅ ЗАРЕГИСТРИРОВАТЬСЯ",
                Location = new Point(120, yPos + 60),
                Size = new Size(200, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += BtnRegister_Click;

            var btnCancel = new Button
            {
                Text = "❌ ОТМЕНА",
                Location = new Point(330, yPos + 60),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            mainPanel.Controls.Add(btnRegister);
            mainPanel.Controls.Add(btnCancel);

            this.Controls.Add(mainPanel);
        }

        private GroupBox CreateGroupBox(string title, int yPos, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(20, yPos),
                Size = new Size(540, height),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };
        }

        private Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private TextBox CreateTextBox(int x, int y, int width, bool addToClass = false)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 25),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void AddHint(Control control, string hint)
        {
            var toolTip = new ToolTip();
            toolTip.SetToolTip(control, hint);
        }

        private bool ValidateEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true;
            return Regex.IsMatch(phone, @"^(\+7|8)[\s-]?\(?\d{3}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}$");
        }

        private bool ValidatePassword(string password)
        {
            return password.Length >= 6 &&
                   Regex.IsMatch(password, @"[a-zA-Z]") &&
                   Regex.IsMatch(password, @"[0-9]");
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text) ||
                string.IsNullOrWhiteSpace(txtFullName.Text) ||
                string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("Все обязательные поля должны быть заполнены!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtUsername.Text.Length < 4 || txtUsername.Text.Length > 20)
            {
                MessageBox.Show("Логин должен содержать от 4 до 20 символов!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidatePassword(txtPassword.Text))
            {
                MessageBox.Show("Пароль должен содержать минимум 6 символов, включая буквы и цифры!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("Пароли не совпадают!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateEmail(txtEmail.Text))
            {
                MessageBox.Show("Введите корректный email адрес!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtPhone.Text) && !ValidatePhone(txtPhone.Text))
            {
                MessageBox.Show("Введите корректный номер телефона!\nФормат: +7 (999) 999-99-99",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!chkAgreeTerms.Checked)
            {
                MessageBox.Show("Необходимо согласие на обработку персональных данных!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var newUser = new User
                {
                    Username = txtUsername.Text.Trim(),
                    Password = txtPassword.Text,
                    FullName = txtFullName.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim(),
                    Citizenship = cmbCitizenship.SelectedItem?.ToString(),
                    BirthDate = dtpBirthDate.Value,
                    Role = "Гражданин",
                    RegistrationDate = DateTime.Now
                };

                dbContext.AddUser(newUser);

                MessageBox.Show(
                    "✅ Регистрация прошла успешно!\n\n" +
                    "Теперь вы можете войти в систему, используя свои логин и пароль.",
                    "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UNIQUE"))
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка при регистрации: {ex.Message}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}