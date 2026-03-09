using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PassportVisaService.Data;
using PassportVisaService.Models;

namespace PassportVisaService.Forms
{
    public partial class ProfileForm : Form
    {
        private User currentUser;
        private DatabaseContext dbContext;

        // Элементы управления
        private TextBox txtFullName;
        private TextBox txtEmail;
        private TextBox txtPhone;
        private TextBox txtPassportSeries;
        private TextBox txtPassportNumber;
        private TextBox txtPassportIssuedBy;
        private DateTimePicker dtpPassportIssueDate;
        private TextBox txtCitizenship;
        private DateTimePicker dtpBirthDate;
        private TextBox txtBirthPlace;
        private TextBox txtRegistrationAddress;
        private Label lblLastLogin;

        public ProfileForm(User user)
        {
            currentUser = user;
            dbContext = new DatabaseContext();
            InitializeComponent();
            InitializeCustomComponent();
            LoadUserData();
        }

        private void InitializeCustomComponent()
        {
            // Настройка формы
            this.Text = "👤 Личный кабинет";
            this.Size = new Size(700, 700);
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
            var lblTitle = new Label
            {
                Text = "ЛИЧНЫЙ КАБИНЕТ",
                Location = new Point(50, yPos),
                Size = new Size(600, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };
            mainPanel.Controls.Add(lblTitle);
            yPos += 50;

            // ИНФОРМАЦИЯ ОБ АККАУНТЕ
            var infoGroup = CreateGroupBox("Информация об аккаунте", yPos, 150);
            int gy = 25;

            // Логин
            var lblUsername = CreateLabel("Логин:", 20, gy, 120);
            var lblUsernameValue = CreateValueLabel(currentUser.Username, 150, gy);
            infoGroup.Controls.AddRange(new Control[] { lblUsername, lblUsernameValue });
            gy += 30;

            // Роль
            var lblRole = CreateLabel("Роль:", 20, gy, 120);
            var lblRoleValue = CreateValueLabel(currentUser.Role, 150, gy);
            infoGroup.Controls.AddRange(new Control[] { lblRole, lblRoleValue });
            gy += 30;

            // Дата регистрации
            var lblRegDate = CreateLabel("Дата регистрации:", 20, gy, 120);
            var lblRegDateValue = CreateValueLabel(currentUser.RegistrationDate.ToString("dd.MM.yyyy"), 150, gy);
            infoGroup.Controls.AddRange(new Control[] { lblRegDate, lblRegDateValue });
            gy += 30;

            // Последний вход
            var lblLastLoginLabel = CreateLabel("Последний вход:", 20, gy, 120);
            lblLastLogin = CreateValueLabel("", 150, gy);
            infoGroup.Controls.AddRange(new Control[] { lblLastLoginLabel, lblLastLogin });

            mainPanel.Controls.Add(infoGroup);
            yPos += 160;

            // ЛИЧНЫЕ ДАННЫЕ
            var personalGroup = CreateGroupBox("Личные данные", yPos, 200);
            gy = 25;

            // ФИО
            var lblFullName = CreateLabel("ФИО:", 20, gy, 120);
            txtFullName = CreateTextBox(150, gy, 400);
            personalGroup.Controls.AddRange(new Control[] { lblFullName, txtFullName });
            gy += 30;

            // Дата рождения
            var lblBirthDate = CreateLabel("Дата рождения:", 20, gy, 120);
            dtpBirthDate = new DateTimePicker
            {
                Location = new Point(150, gy),
                Size = new Size(400, 25),
                Format = DateTimePickerFormat.Short
            };
            personalGroup.Controls.AddRange(new Control[] { lblBirthDate, dtpBirthDate });
            gy += 30;

            // Место рождения
            var lblBirthPlace = CreateLabel("Место рождения:", 20, gy, 120);
            txtBirthPlace = CreateTextBox(150, gy, 400);
            personalGroup.Controls.AddRange(new Control[] { lblBirthPlace, txtBirthPlace });
            gy += 30;

            // Гражданство
            var lblCitizenship = CreateLabel("Гражданство:", 20, gy, 120);
            txtCitizenship = CreateTextBox(150, gy, 400);
            personalGroup.Controls.AddRange(new Control[] { lblCitizenship, txtCitizenship });

            mainPanel.Controls.Add(personalGroup);
            yPos += 210;

            // ПАСПОРТНЫЕ ДАННЫЕ
            var passportGroup = CreateGroupBox("Паспортные данные", yPos, 160);
            gy = 25;

            // Серия
            var lblPassportSeries = CreateLabel("Серия:", 20, gy, 120);
            txtPassportSeries = CreateTextBox(150, gy, 100);
            passportGroup.Controls.AddRange(new Control[] { lblPassportSeries, txtPassportSeries });
            gy += 30;

            // Номер
            var lblPassportNumber = CreateLabel("Номер:", 20, gy, 120);
            txtPassportNumber = CreateTextBox(150, gy, 150);
            passportGroup.Controls.AddRange(new Control[] { lblPassportNumber, txtPassportNumber });
            gy += 30;

            // Кем выдан
            var lblPassportIssuedBy = CreateLabel("Кем выдан:", 20, gy, 120);
            txtPassportIssuedBy = CreateTextBox(150, gy, 400);
            passportGroup.Controls.AddRange(new Control[] { lblPassportIssuedBy, txtPassportIssuedBy });
            gy += 30;

            // Дата выдачи
            var lblPassportIssueDate = CreateLabel("Дата выдачи:", 20, gy, 120);
            dtpPassportIssueDate = new DateTimePicker
            {
                Location = new Point(150, gy),
                Size = new Size(400, 25),
                Format = DateTimePickerFormat.Short
            };
            passportGroup.Controls.AddRange(new Control[] { lblPassportIssueDate, dtpPassportIssueDate });

            mainPanel.Controls.Add(passportGroup);
            yPos += 170;

            // АДРЕС РЕГИСТРАЦИИ
            var addressGroup = CreateGroupBox("Адрес регистрации", yPos, 70);
            gy = 25;

            var lblRegistrationAddress = CreateLabel("Адрес:", 20, gy, 120);
            txtRegistrationAddress = CreateTextBox(150, gy, 400);
            AddHint(txtRegistrationAddress, "Индекс, город, улица, дом, квартира");
            addressGroup.Controls.AddRange(new Control[] { lblRegistrationAddress, txtRegistrationAddress });

            mainPanel.Controls.Add(addressGroup);
            yPos += 80;

            // КОНТАКТНЫЕ ДАННЫЕ
            var contactGroup = CreateGroupBox("Контактные данные", yPos, 100);
            gy = 25;

            // Email
            var lblEmail = CreateLabel("Email:", 20, gy, 120);
            txtEmail = CreateTextBox(150, gy, 400);
            contactGroup.Controls.AddRange(new Control[] { lblEmail, txtEmail });
            gy += 30;

            // Телефон
            var lblPhone = CreateLabel("Телефон:", 20, gy, 120);
            txtPhone = CreateTextBox(150, gy, 400);
            AddHint(txtPhone, "+7 (999) 999-99-99");
            contactGroup.Controls.AddRange(new Control[] { lblPhone, txtPhone });

            mainPanel.Controls.Add(contactGroup);
            yPos += 110;

            // КНОПКИ
            var btnSave = new Button
            {
                Text = "💾 СОХРАНИТЬ ИЗМЕНЕНИЯ",
                Location = new Point(150, yPos + 20),
                Size = new Size(250, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnChangePassword = new Button
            {
                Text = "🔐 СМЕНИТЬ ПАРОЛЬ",
                Location = new Point(410, yPos + 20),
                Size = new Size(150, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChangePassword.FlatAppearance.BorderSize = 0;
            btnChangePassword.Click += BtnChangePassword_Click;

            mainPanel.Controls.Add(btnSave);
            mainPanel.Controls.Add(btnChangePassword);

            this.Controls.Add(mainPanel);
        }

        private GroupBox CreateGroupBox(string title, int yPos, int height)
        {
            return new GroupBox
            {
                Text = title,
                Location = new Point(20, yPos),
                Size = new Size(640, height),
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

        private Label CreateValueLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
        }

        private TextBox CreateTextBox(int x, int y, int width)
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

        private void LoadUserData()
        {
            var freshUser = dbContext.GetUserById(currentUser.Id);
            if (freshUser != null)
            {
                currentUser = freshUser;
            }

            txtFullName.Text = currentUser.FullName;
            dtpBirthDate.Value = currentUser.BirthDate ?? DateTime.Now.AddYears(-30);
            txtBirthPlace.Text = currentUser.BirthPlace ?? "";
            txtCitizenship.Text = currentUser.Citizenship ?? "Российская Федерация";

            txtPassportSeries.Text = currentUser.PassportSeries ?? "";
            txtPassportNumber.Text = currentUser.PassportNumber ?? "";
            txtPassportIssuedBy.Text = currentUser.PassportIssuedBy ?? "";
            dtpPassportIssueDate.Value = currentUser.PassportIssueDate ?? DateTime.Now.AddYears(-5);

            txtRegistrationAddress.Text = currentUser.RegistrationAddress ?? "";

            txtEmail.Text = currentUser.Email;
            txtPhone.Text = currentUser.Phone ?? "";

            lblLastLogin.Text = currentUser.LastLoginDate?.ToString("dd.MM.yyyy HH:mm") ?? "Первый вход";
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

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("ФИО обязательно для заполнения!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateEmail(txtEmail.Text))
            {
                MessageBox.Show("Введите корректный email адрес!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidatePhone(txtPhone.Text))
            {
                MessageBox.Show("Введите корректный номер телефона!\nФормат: +7 (999) 999-99-99",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                currentUser.FullName = txtFullName.Text.Trim();
                currentUser.Email = txtEmail.Text.Trim();
                currentUser.Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim();
                currentUser.PassportSeries = txtPassportSeries.Text.Trim();
                currentUser.PassportNumber = txtPassportNumber.Text.Trim();
                currentUser.PassportIssuedBy = txtPassportIssuedBy.Text.Trim();
                currentUser.PassportIssueDate = dtpPassportIssueDate.Value;
                currentUser.Citizenship = txtCitizenship.Text.Trim();
                currentUser.BirthDate = dtpBirthDate.Value;
                currentUser.BirthPlace = txtBirthPlace.Text.Trim();
                currentUser.RegistrationAddress = txtRegistrationAddress.Text.Trim();

                dbContext.UpdateUserProfile(currentUser);

                MessageBox.Show("✅ Данные успешно сохранены!", "Успешно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnChangePassword_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = "Смена пароля";
                form.Size = new Size(400, 250);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = Color.FromArgb(240, 248, 255);

                var lblOld = new Label { Text = "Старый пароль:", Location = new Point(20, 30), Size = new Size(120, 25) };
                var txtOld = new TextBox { Location = new Point(150, 30), Size = new Size(200, 25), PasswordChar = '*' };

                var lblNew = new Label { Text = "Новый пароль:", Location = new Point(20, 70), Size = new Size(120, 25) };
                var txtNew = new TextBox { Location = new Point(150, 70), Size = new Size(200, 25), PasswordChar = '*' };

                var lblConfirm = new Label { Text = "Подтвердите:", Location = new Point(20, 110), Size = new Size(120, 25) };
                var txtConfirm = new TextBox { Location = new Point(150, 110), Size = new Size(200, 25), PasswordChar = '*' };

                var btnOk = new Button
                {
                    Text = "Сменить пароль",
                    Location = new Point(100, 160),
                    Size = new Size(150, 30),
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnOk.FlatAppearance.BorderSize = 0;

                var btnCancel = new Button
                {
                    Text = "Отмена",
                    Location = new Point(260, 160),
                    Size = new Size(100, 30),
                    BackColor = Color.FromArgb(231, 76, 60),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                btnCancel.FlatAppearance.BorderSize = 0;

                btnOk.Click += (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(txtOld.Text) || string.IsNullOrWhiteSpace(txtNew.Text) || string.IsNullOrWhiteSpace(txtConfirm.Text))
                    {
                        MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (txtNew.Text.Length < 6)
                    {
                        MessageBox.Show("Новый пароль должен содержать минимум 6 символов!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (txtNew.Text != txtConfirm.Text)
                    {
                        MessageBox.Show("Новый пароль и подтверждение не совпадают!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (dbContext.ChangePassword(currentUser.Id, txtOld.Text, txtNew.Text))
                    {
                        MessageBox.Show("✅ Пароль успешно изменен!", "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        form.Close();
                    }
                    else
                    {
                        MessageBox.Show("Неверный старый пароль!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                btnCancel.Click += (s, args) => form.Close();

                form.Controls.AddRange(new Control[] { lblOld, txtOld, lblNew, txtNew, lblConfirm, txtConfirm, btnOk, btnCancel });
                form.ShowDialog();
            }
        }
    }
}