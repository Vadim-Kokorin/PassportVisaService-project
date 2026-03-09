using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PassportVisaService.Data;
using PassportVisaService.Models;
using ChatMessage = PassportVisaService.Models.Message;

namespace PassportVisaService.Forms
{
    public partial class MainForm : Form
    {
        private User currentUser;
        private DatabaseContext dbContext;
        private string uploadFolder;

        // Элементы интерфейса
        private TabControl tabControl;
        private Panel topPanel;
        private Label userInfoLabel;
        private Label notificationLabel;
        private Label lastUpdateLabel = new Label();
        private Button refreshButton;
        private Button profileButton;
        private Button logoutButton;
        private MenuStrip menuStrip;

        // Таймеры
        private Timer documentsTimer;
        private Timer reviewTimer;
        private Timer usersTimer;
        private Timer unreadMessagesTimer;

        // Флаги обновления
        private bool isUpdatingDocuments = false;
        private bool isUpdatingReview = false;
        private bool isUpdatingUsers = false;
        private bool autoRefreshEnabled = true;

        // Списки
        private ListView documentsListView;
        private ListView reviewListView;
        private ListView usersListView;
        private ListView ticketsListView;
        private ListView requestsListView; 

        public MainForm(User user)
        {
            currentUser = user;
            dbContext = new DatabaseContext();

            // Создание папки для документов
            uploadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PassportVisaService", "Documents");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            InitializeComponent(); // автоматический метод
            InitializeCustomComponent(); // наш метод
            LoadUserInterface();
            StartAutoRefresh();
            StartUnreadMessagesTimer();
        }

        // Переименовали с InitializeComponent на InitializeCustomComponent
        private void InitializeCustomComponent()
        {
            this.Text = $"Паспортно-визовая служба - {currentUser?.FullName} ({currentUser?.Role})";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 600);
            this.BackColor = Color.FromArgb(240, 248, 255);
            this.FormClosing += MainForm_FormClosing;
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                ManualRefreshAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Останавливаем все таймеры
            documentsTimer?.Stop();
            reviewTimer?.Stop();
            usersTimer?.Stop();
            unreadMessagesTimer?.Stop();

            documentsTimer?.Dispose();
            reviewTimer?.Dispose();
            usersTimer?.Dispose();
            unreadMessagesTimer?.Dispose();

            if (e.CloseReason == CloseReason.UserClosing)
            {
                Application.Exit();
            }
        }

        private void LoadUserInterface()
        {
            // Верхняя панель
            CreateTopPanel();

            // Меню
            CreateMenuStrip();

            // Вкладки
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            // Вкладка документов (для всех)
            var documentsTab = new TabPage("📄 МОИ ДОКУМЕНТЫ");
            CreateDocumentsTab(documentsTab);
            tabControl.TabPages.Add(documentsTab);

            // Вкладка чата (для всех)
            var chatTab = new TabPage("💬 ЧАТ ПОДДЕРЖКИ");
            CreateChatTab(chatTab);
            tabControl.TabPages.Add(chatTab);

            // Вкладка проверки (для проверяющих и админов)
            if (currentUser.Role == "Проверяющий" || currentUser.Role == "Администратор")
            {
                var reviewTab = new TabPage("✅ ПРОВЕРКА ДОКУМЕНТОВ");
                CreateReviewTab(reviewTab);
                tabControl.TabPages.Add(reviewTab);
            }

            // Вкладка управления пользователями (только для админов)
            if (currentUser.Role == "Администратор")
            {
                var usersTab = new TabPage("👥 УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ");
                CreateUsersTab(usersTab);
                tabControl.TabPages.Add(usersTab);
            }

            // Добавляем элементы на форму
            this.Controls.Add(tabControl);
            this.Controls.Add(topPanel);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;
        }


        private void CreateTopPanel()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(0, 51, 102)
            };

            // Информация о пользователе
            userInfoLabel = new Label
            {
                Text = $"👤 {currentUser.FullName} | {currentUser.Role}",
                Location = new Point(10, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White
            };

            // Кнопка обновления - СМЕЩАЕМ ЕЩЁ ПРАВЕЕ (было 300, стало 350)
            refreshButton = new Button
            {
                Text = "🔄 ОБНОВИТЬ (F5)",
                Location = new Point(350, 12), // Сместили с 300 на 350
                Size = new Size(150, 35),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += (s, e) => ManualRefreshAll();

            // Уведомления
            notificationLabel = new Label
            {
                Text = "",
                Location = new Point(520, 20), // Сместили соответственно
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.LightGreen
            };

            // Кнопка профиля
            profileButton = new Button
            {
                Text = "👤 ЛИЧНЫЙ КАБИНЕТ",
                Location = new Point(800, 12), // Сместили
                Size = new Size(160, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            profileButton.FlatAppearance.BorderSize = 0;
            profileButton.Click += (s, e) =>
            {
                var profileForm = new ProfileForm(currentUser);
                profileForm.ShowDialog();
                var updatedUser = dbContext.GetUserById(currentUser.Id);
                if (updatedUser != null)
                {
                    currentUser = updatedUser;
                    userInfoLabel.Text = $"👤 {currentUser.FullName} | {currentUser.Role}";
                }
            };

            // Кнопка выхода
            logoutButton = new Button
            {
                Text = "🚪 ВЫЙТИ",
                Location = new Point(970, 12), // Сместили
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += LogoutButton_Click;

            topPanel.Controls.AddRange(new Control[] {
        userInfoLabel, refreshButton,
        notificationLabel, profileButton, logoutButton
    });
        }


        private void ViewSelectedRequest()
        {
            if (requestsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите заявку для просмотра!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string requestIdText = requestsListView.SelectedItems[0].Text;
            if (requestIdText.StartsWith("REQ-"))
            {
                requestIdText = requestIdText.Replace("REQ-", "");
            }

            if (int.TryParse(requestIdText, out int id))
            {
                // Получаем заявку из БД
                var requests = dbContext.GetUserRequests(currentUser.Id);
                var request = requests.FirstOrDefault(r => r.Id == id);

                if (request != null)
                {
                    var viewForm = new RequestViewForm(request);
                    viewForm.ShowDialog();

                    // Обновляем список после просмотра (на случай изменения статуса)
                    _ = LoadUserRequestsAsync(requestsListView);
                }
                else
                {
                    MessageBox.Show("Заявка не найдена!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void CreateMenuStrip()
        {
            menuStrip = new MenuStrip();

            // УБИРАЕМ "Файл", оставляем только нужные пункты
            var homeMenu = new ToolStripMenuItem("🏠 Главная");
            homeMenu.DropDownItems.Add("👤 Личный кабинет", null, (s, e) =>
            {
                var profileForm = new ProfileForm(currentUser);
                profileForm.ShowDialog();
            });
            homeMenu.DropDownItems.Add(new ToolStripSeparator());
            homeMenu.DropDownItems.Add("🚪 Выход", null, (s, e) => LogoutButton_Click(null, null));

            var refreshMenu = new ToolStripMenuItem("🔄 Обновление");
            refreshMenu.DropDownItems.Add("Ручное обновление (F5)", null, (s, e) => ManualRefreshAll());

            var autoRefreshItem = new ToolStripMenuItem("⚡ Автообновление: Вкл");
            autoRefreshItem.Click += (s, e) =>
            {
                ToggleAutoRefresh();
                autoRefreshItem.Text = autoRefreshEnabled ? "⚡ Автообновление: Вкл" : "⚡ Автообновление: Выкл";
            };
            refreshMenu.DropDownItems.Add(autoRefreshItem);

            var helpMenu = new ToolStripMenuItem("❓ Помощь");
            helpMenu.DropDownItems.Add("О программе", null, (s, e) =>
            {
                MessageBox.Show(
                    "🏛 ПАСПОРТНО-ВИЗОВАЯ СЛУЖБА\n" +
                    "Версия 2.0\n\n" +
                    "Единый портал для подачи документов\n" +
                    "и общения с техподдержкой.\n\n" +
                    "© 2025 Все права защищены",
                    "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            menuStrip.Items.Add(homeMenu);
            menuStrip.Items.Add(refreshMenu);
            menuStrip.Items.Add(helpMenu);
        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Останавливаем таймеры
                documentsTimer?.Stop();
                reviewTimer?.Stop();
                usersTimer?.Stop();
                unreadMessagesTimer?.Stop();

                // Открываем форму входа
                var loginForm = new LoginForm();
                loginForm.Show();

                // Закрываем текущую форму
                this.Close();
            }
        }

        private void ToggleAutoRefresh()
        {
            autoRefreshEnabled = !autoRefreshEnabled;

            if (autoRefreshEnabled)
            {
                documentsTimer?.Start();
                reviewTimer?.Start();
                usersTimer?.Start();
                ShowNotification("✅ Автообновление включено", Color.Green);
            }
            else
            {
                documentsTimer?.Stop();
                reviewTimer?.Stop();
                usersTimer?.Stop();
                ShowNotification("⏸ Автообновление выключено", Color.Orange);
            }
        }

        private void StartAutoRefresh()
        {
            documentsTimer = new Timer { Interval = 5000 }; // Увеличьте интервал до 5 секунд
            documentsTimer.Tick += async (s, e) =>
            {
                if (!isUpdatingDocuments)
                {
                    await RefreshDocumentsAsync();
                }
            };
            documentsTimer.Start();

            if (currentUser.Role == "Проверяющий" || currentUser.Role == "Администратор")
            {
                reviewTimer = new Timer { Interval = 5000 };
                reviewTimer.Tick += async (s, e) =>
                {
                    if (!isUpdatingReview)
                    {
                        await RefreshReviewAsync();
                    }
                };
                reviewTimer.Start();
            }

            if (currentUser.Role == "Администратор")
            {
                usersTimer = new Timer { Interval = 7000 }; // Еще больше для админов
                usersTimer.Tick += async (s, e) =>
                {
                    if (!isUpdatingUsers)
                    {
                        await RefreshUsersAsync();
                    }
                };
                usersTimer.Start();
            }
        }

        public static void ClearPool()
        {
            SQLiteConnection.ClearAllPools();
        }
        private void StartUnreadMessagesTimer()
        {
            unreadMessagesTimer = new Timer { Interval = 5000 };
            unreadMessagesTimer.Tick += (s, e) =>
            {
                int unreadCount = dbContext.GetUnreadMessagesCount(currentUser.Id);
                if (unreadCount > 0)
                {
                    // Обновляем заголовок вкладки чата
                    if (tabControl.TabPages.Count > 1)
                    {
                        tabControl.TabPages[1].Text = $"💬 ЧАТ ПОДДЕРЖКИ ({unreadCount})";
                    }
                }
                else
                {
                    if (tabControl.TabPages.Count > 1)
                    {
                        tabControl.TabPages[1].Text = "💬 ЧАТ ПОДДЕРЖКИ";
                    }
                }
            };
            unreadMessagesTimer.Start();
        }

        private void ShowNotification(string message, Color color)
        {
            if (notificationLabel != null && !notificationLabel.IsDisposed)
            {
                notificationLabel.Text = message;
                notificationLabel.ForeColor = color;

                var timer = new Timer { Interval = 2000 };
                timer.Tick += (s, e) =>
                {
                    notificationLabel.Text = "";
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private void ManualRefreshAll()
        {
            refreshButton.Enabled = false;
            refreshButton.Text = "⏳ ОБНОВЛЕНИЕ...";
            refreshButton.BackColor = Color.Gray;

            Task.Run(async () =>
            {
                await RefreshDocumentsAsync();

                if (currentUser.Role == "Проверяющий" || currentUser.Role == "Администратор")
                {
                    await RefreshReviewAsync();
                }

                if (currentUser.Role == "Администратор")
                {
                    await RefreshUsersAsync();
                }

                this.Invoke((MethodInvoker)delegate
                {
                    refreshButton.Enabled = true;
                    refreshButton.Text = "🔄 ОБНОВИТЬ (F5)";
                    refreshButton.BackColor = Color.FromArgb(52, 152, 219);
                    lastUpdateLabel.Text = $"Обновлено: {DateTime.Now:HH:mm:ss}";
                    ShowNotification("✅ Все данные обновлены", Color.Green);
                });
            });
        }

        // Асинхронные методы обновления
        private async Task RefreshDocumentsAsync()
        {
            if (isUpdatingDocuments || documentsListView == null || documentsListView.IsDisposed)
                return;

            try
            {
                isUpdatingDocuments = true;
                var documents = await Task.Run(() => dbContext.GetUserDocuments(currentUser.Id));

                this.Invoke((MethodInvoker)delegate
                {
                    documentsListView.BeginUpdate();
                    documentsListView.Items.Clear();

                    foreach (var doc in documents)
                    {
                        var item = new ListViewItem(doc.DocumentType);
                        item.SubItems.Add(doc.DocumentSubType ?? "-");
                        item.SubItems.Add(doc.UploadDate.ToString("dd.MM.yyyy HH:mm"));

                        string status = doc.Status;
                        if (status.Contains("Принят"))
                        {
                            status = "✅ Принят";
                            item.ForeColor = Color.Green;
                        }
                        else if (status.Contains("Отклонен"))
                        {
                            status = "❌ Отклонен";
                            item.ForeColor = Color.Red;
                        }
                        else
                        {
                            status = "⏳ На рассмотрении";
                            item.ForeColor = Color.Orange;
                        }
                        item.SubItems.Add(status);

                        item.SubItems.Add(doc.ReviewComment ?? "-");
                        item.SubItems.Add($"{doc.FileSize / 1024} КБ");
                        item.SubItems.Add(doc.FileExtension?.ToUpper() ?? "");

                        documentsListView.Items.Add(item);
                    }

                    documentsListView.EndUpdate();
                });
            }
            finally
            {
                isUpdatingDocuments = false;
            }
        }

        private async Task RefreshReviewAsync()
        {
            if (isUpdatingReview || reviewListView == null || reviewListView.IsDisposed)
                return;

            try
            {
                isUpdatingReview = true;
                var documents = await Task.Run(() => dbContext.GetAllDocuments());
                var users = await Task.Run(() => dbContext.GetAllUsers());

                this.Invoke((MethodInvoker)delegate
                {
                    reviewListView.BeginUpdate();
                    reviewListView.Items.Clear();

                    foreach (var doc in documents.Where(d => d.Status == "На рассмотрении" || d.Status.Contains("Отклонен")))
                    {
                        var user = users.FirstOrDefault(u => u.Id == doc.UserId);
                        if (user == null) continue;

                        var item = new ListViewItem(doc.Id.ToString());
                        item.SubItems.Add(user.FullName);
                        item.SubItems.Add(doc.DocumentType);
                        item.SubItems.Add(doc.DocumentSubType ?? "-");
                        item.SubItems.Add(doc.UploadDate.ToString("dd.MM.yyyy HH:mm"));

                        string status = doc.Status;
                        if (status.Contains("Отклонен"))
                        {
                            status = "❌ Отклонен";
                            item.BackColor = Color.LightPink;
                        }
                        else
                        {
                            status = "⏳ На рассмотрении";
                            item.BackColor = Color.LightYellow;
                        }
                        item.SubItems.Add(status);

                        item.SubItems.Add($"{doc.FileSize / 1024} КБ");
                        item.SubItems.Add(doc.FileExtension?.ToUpper() ?? "");

                        reviewListView.Items.Add(item);
                    }

                    reviewListView.EndUpdate();
                });
            }
            finally
            {
                isUpdatingReview = false;
            }
        }

        private async Task RefreshUsersAsync()
        {
            if (isUpdatingUsers || usersListView == null || usersListView.IsDisposed)
                return;

            try
            {
                isUpdatingUsers = true;
                var users = await Task.Run(() => dbContext.GetAllUsers());

                this.Invoke((MethodInvoker)delegate
                {
                    usersListView.BeginUpdate();
                    usersListView.Items.Clear();

                    foreach (var user in users)
                    {
                        var item = new ListViewItem(user.Id.ToString());
                        item.SubItems.Add(user.FullName);
                        item.SubItems.Add(user.Username);
                        item.SubItems.Add(user.Email);
                        item.SubItems.Add(user.Phone ?? "-");
                        item.SubItems.Add(user.Role);
                        item.SubItems.Add(user.RegistrationDate.ToString("dd.MM.yyyy"));
                        item.SubItems.Add(user.LastLoginDate?.ToString("dd.MM.yyyy") ?? "Никогда");

                        if (user.Role == "Администратор")
                            item.BackColor = Color.LightBlue;
                        else if (user.Role == "Проверяющий")
                            item.BackColor = Color.LightGreen;

                        usersListView.Items.Add(item);
                    }

                    usersListView.EndUpdate();
                });
            }
            finally
            {
                isUpdatingUsers = false;
            }
        }

        // ========== ВКЛАДКА ДОКУМЕНТОВ ==========
        private void CreateDocumentsTab(TabPage tabPage)
        {
            tabPage.Text = "📄 МОИ ЗАЯВКИ";
            tabPage.AutoScroll = true;

            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            // Группа с услугами - УВЕЛИЧИВАЕМ ВЫСОТУ
            var servicesGroup = CreateServicesGroup();
            panel.Controls.Add(servicesGroup);

            // Группа с моими заявками
            var requestsGroup = CreateRequestsGroup();
            panel.Controls.Add(requestsGroup);

            tabPage.Controls.Add(panel);
        }

        private GroupBox CreateServicesGroup()
        {
            var group = new GroupBox
            {
                Text = "📋 ДОСТУПНЫЕ УСЛУГИ",
                Size = new Size(1150, 350), // УВЕЛИЧИЛИ ВЫСОТУ С 200 ДО 350
                Font = new Font("Segoe UI", 12, FontStyle.Bold), // УВЕЛИЧИЛИ ШРИФТ
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Расширенный список услуг
            string[] services = new string[]
            {
        "🛂 Загранпаспорт (новый)",
        "🛂 Загранпаспорт (замена)",
        "🛂 Загранпаспорт (продление)",
        "🆔 Внутренний паспорт (получение)",
        "🆔 Внутренний паспорт (замена)",
        "🎫 Шенгенская виза (туристическая)",
        "🎫 Шенгенская виза (деловая)",
        "🎫 Шенгенская виза (рабочая)",
        "🌍 Виза США",
        "🌍 Виза Великобритании",
        "🌍 Виза Китай",
        "📄 Справка об отсутствии судимости",
        "🏥 Медицинская страховка",
        "✈️ Разрешение на выезд ребенка"
            };

            string[] descriptions = new string[]
            {
        "Оформление нового загранпаспорта (срок 5 лет)",
        "Замена загранпаспорта в связи с окончанием срока",
        "Продление срока действия загранпаспорта",
        "Первичное получение паспорта в 14 лет",
        "Замена паспорта в 20/45 лет или при смене фамилии",
        "Туристическая виза до 90 дней",
        "Деловая виза для бизнес-поездок",
        "Рабочая виза для трудоустройства",
        "Виза США (туристическая/деловая)",
        "Виза Великобритании (стандартная)",
        "Виза Китай (туристическая/деловая)",
        "Для трудоустройства или выезда за границу",
        "Страхование для выезжающих за рубеж",
        "Согласие на выезд ребенка за границу"
            };

            int x = 20, y = 40;
            int colWidth = 550;
            int rowHeight = 80; // УВЕЛИЧИЛИ ВЫСОТУ С 70 ДО 80

            for (int i = 0; i < services.Length; i++)
            {
                int row = i / 2;
                int col = i % 2;

                var servicePanel = new Panel
                {
                    Location = new Point(x + col * (colWidth + 20), y + row * rowHeight),
                    Size = new Size(colWidth, 70),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White,
                    Cursor = Cursors.Hand
                };

                // Иконка и заголовок
                var lblTitle = new Label
                {
                    Text = services[i],
                    Location = new Point(10, 5),
                    Size = new Size(350, 25),
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 51, 102)
                };

                // Описание
                var lblDesc = new Label
                {
                    Text = descriptions[i],
                    Location = new Point(10, 30),
                    Size = new Size(400, 35),
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.Gray
                };

                // Кнопка выбора
                var btnSelect = new Button
                {
                    Text = "Выбрать →",
                    Location = new Point(420, 20),
                    Size = new Size(100, 30),
                    BackColor = Color.FromArgb(52, 152, 219),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };
                btnSelect.FlatAppearance.BorderSize = 0;

                int serviceIndex = i;
                btnSelect.Click += (s, e) =>
                {
                    // Очищаем название от иконок для передачи в форму
                    string serviceName = services[serviceIndex]
                        .Replace("🛂 ", "")
                        .Replace("🆔 ", "")
                        .Replace("🎫 ", "")
                        .Replace("🌍 ", "")
                        .Replace("📄 ", "")
                        .Replace("🏥 ", "")
                        .Replace("✈️ ", "");

                    var requestForm = new NewRequestForm(currentUser, serviceName);
                    requestForm.ShowDialog();
                };

                servicePanel.Controls.Add(lblTitle);
                servicePanel.Controls.Add(lblDesc);
                servicePanel.Controls.Add(btnSelect);

                // Клик по панели тоже выбирает услугу
                servicePanel.Click += (s, e) => btnSelect.PerformClick();

                group.Controls.Add(servicePanel);
            }

            return group;
        }

        private GroupBox CreateRequestsGroup()
        {
            var group = new GroupBox
            {
                Text = "📋 МОИ ЗАЯВКИ",
                Size = new Size(1150, 320),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Панель с кнопками
            var buttonPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(1130, 35)
            };

            // Кнопка обновления
            var btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Location = new Point(0, 0),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) =>
            {
                btnRefresh.Enabled = false;
                btnRefresh.Text = "⏳";
                await LoadUserRequestsAsync(requestsListView);
                btnRefresh.Enabled = true;
                btnRefresh.Text = "🔄 Обновить";
            };

            // Кнопка просмотра заявки
            var btnView = new Button
            {
                Text = "👁 Просмотреть заявку",
                Location = new Point(110, 0),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnView.FlatAppearance.BorderSize = 0;
            btnView.Click += (s, e) => ViewSelectedRequest();

            buttonPanel.Controls.Add(btnRefresh);
            buttonPanel.Controls.Add(btnView);

            requestsListView = new ListView
            {
                Location = new Point(10, 65),
                Size = new Size(1130, 235),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9)
            };

            requestsListView.Columns.Add("№ заявки", 100);
            requestsListView.Columns.Add("Услуга", 200);
            requestsListView.Columns.Add("Дата подачи", 120);
            requestsListView.Columns.Add("Статус", 120);
            requestsListView.Columns.Add("Файлы", 80);
            requestsListView.Columns.Add("Комментарий", 450);

            requestsListView.DoubleClick += (s, e) => ViewSelectedRequest();

            group.Controls.Add(buttonPanel);
            group.Controls.Add(requestsListView);

            // Загружаем заявки
            _ = LoadUserRequestsAsync(requestsListView);

            return group;
        }

        private async Task LoadUserRequestsAsync(ListView listView)
        {
            try
            {
                var requests = await Task.Run(() => dbContext.GetUserRequests(currentUser.Id));

                listView.BeginUpdate();
                listView.Items.Clear();

                if (requests == null || requests.Count == 0)
                {
                    var emptyItem = new ListViewItem("Нет заявок");
                    emptyItem.SubItems.Add("");
                    emptyItem.SubItems.Add("");
                    emptyItem.SubItems.Add("");
                    emptyItem.SubItems.Add("");
                    emptyItem.SubItems.Add("Нажмите 'Выбрать' на вкладке 'Услуги' чтобы создать новую заявку");
                    listView.Items.Add(emptyItem);
                }
                else
                {
                    foreach (var request in requests)
                    {
                        var item = new ListViewItem($"REQ-{request.Id}");
                        item.SubItems.Add(request.ServiceType);
                        item.SubItems.Add(request.CreatedAt.ToString("dd.MM.yyyy HH:mm"));

                        string status = request.Status;
                        if (status == "На проверке")
                        {
                            status = "⏳ На проверке";
                            item.BackColor = Color.LightYellow;
                        }
                        else if (status == "Одобрено")
                        {
                            status = "✅ Одобрено";
                            item.BackColor = Color.LightGreen;
                        }
                        else if (status == "Отказано")
                        {
                            status = "❌ Отказано";
                            item.BackColor = Color.LightPink;
                        }
                        else if (status == "Требует доработки")
                        {
                            status = "📝 Требует доработки";
                            item.BackColor = Color.LightSalmon;
                        }

                        item.SubItems.Add(status);

                        // Получаем количество файлов
                        var docs = dbContext.GetRequestDocuments(request.Id);
                        item.SubItems.Add(docs.Count.ToString());

                        item.SubItems.Add(request.ReviewComment ?? "-");

                        listView.Items.Add(item);
                    }
                }

                listView.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private async Task LoadUserRequests()
        {
            try
            {
                // Ищем ListView на форме
                ListView listView = FindRequestsListView();
                if (listView == null) return;

                // Здесь должен быть метод в DatabaseContext для получения заявок пользователя
                // Пока создаем тестовые данные для демонстрации
                await Task.Delay(100); // Имитация асинхронной операции

                var requests = new List<ServiceRequest>(); // Замените на реальный запрос к БД

                listView.BeginUpdate();
                listView.Items.Clear();

                foreach (var request in requests)
                {
                    var item = new ListViewItem(request.Id.ToString());
                    item.SubItems.Add(request.ServiceType);
                    item.SubItems.Add(request.CreatedAt.ToString("dd.MM.yyyy"));

                    string status = request.Status;
                    if (status == "На проверке") status = "⏳ На проверке";
                    else if (status == "Одобрено") status = "✅ Одобрено";
                    else if (status == "Отказано") status = "❌ Отказано";
                    else if (status == "Требует доработки") status = "📝 Требует доработки";

                    item.SubItems.Add(status);
                    item.SubItems.Add(request.ReviewerName ?? "Не назначен");
                    item.SubItems.Add(request.ReviewComment ?? "-");

                    listView.Items.Add(item);
                }

                listView.EndUpdate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private ListView FindRequestsListView()
        {
            // Ищем ListView на форме
            foreach (Control control in this.Controls)
            {
                if (control is TabControl tabControl)
                {
                    foreach (TabPage tab in tabControl.TabPages)
                    {
                        if (tab.Text.Contains("МОИ ЗАЯВКИ") || tab.Text.Contains("ДОКУМЕНТЫ"))
                        {
                            return FindListViewInControl(tab);
                        }
                    }
                }
            }
            return null;
        }

        private ListView FindListViewInControl(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is ListView listView)
                    return listView;

                if (control.HasChildren)
                {
                    var found = FindListViewInControl(control);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }
        
        private GroupBox CreateUploadGroup()
        {
            var group = new GroupBox
            {
                Text = "📤 ЗАГРУЗКА НОВОГО ДОКУМЕНТА",
                Size = new Size(1150, 200),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Тип документа
            var lblType = new Label
            {
                Text = "Тип документа:",
                Location = new Point(20, 30),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9)
            };

            var cmbType = new ComboBox
            {
                Location = new Point(150, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbType.Items.AddRange(new object[] {
                "Заграничный паспорт",
                "Внутренний паспорт",
                "Визовая анкета",
                "Фотография",
                "Медицинская справка",
                "Страховой полис",
                "Приглашение",
                "Другое"
            });

            // Подтип документа
            var lblSubType = new Label
            {
                Text = "Подтип:",
                Location = new Point(370, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };

            var cmbSubType = new ComboBox
            {
                Location = new Point(460, 30),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                Enabled = false
            };

            cmbType.SelectedIndexChanged += (s, e) =>
            {
                cmbSubType.Items.Clear();
                switch (cmbType.SelectedItem?.ToString())
                {
                    case "Заграничный паспорт":
                        cmbSubType.Items.AddRange(new object[] { "Новый", "Замена", "Продление", "Утеря" });
                        cmbSubType.Enabled = true;
                        break;
                    case "Внутренний паспорт":
                        cmbSubType.Items.AddRange(new object[] { "Получение в 14 лет", "Замена в 20 лет", "Замена в 45 лет", "Смена фамилии" });
                        cmbSubType.Enabled = true;
                        break;
                    case "Визовая анкета":
                        cmbSubType.Items.AddRange(new object[] { "Туристическая", "Деловая", "Рабочая", "Учебная" });
                        cmbSubType.Enabled = true;
                        break;
                    default:
                        cmbSubType.Enabled = false;
                        break;
                }
                if (cmbSubType.Items.Count > 0) cmbSubType.SelectedIndex = 0;
            };

            // Выбор файла
            var btnChooseFile = new Button
            {
                Text = "📁 Выбрать файл",
                Location = new Point(20, 70),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChooseFile.FlatAppearance.BorderSize = 0;

            var lblFile = new Label
            {
                Text = "Файл не выбран",
                Location = new Point(150, 70),
                Size = new Size(400, 30),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            };

            var lblFileInfo = new Label
            {
                Text = "",
                Location = new Point(150, 95),
                Size = new Size(400, 20),
                ForeColor = Color.DarkGreen,
                Font = new Font("Segoe UI", 8)
            };

            btnChooseFile.Click += (s, e) =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Все файлы|*.jpg;*.jpeg;*.png;*.bmp;*.pdf;*.doc;*.docx|" +
                                   "Изображения|*.jpg;*.jpeg;*.png;*.bmp|" +
                                   "PDF|*.pdf|" +
                                   "Word|*.doc;*.docx";
                    dialog.Title = "Выберите файл документа";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        lblFile.Text = dialog.FileName;
                        lblFile.ForeColor = Color.Black;

                        var fi = new FileInfo(dialog.FileName);
                        lblFileInfo.Text = $"Размер: {fi.Length / 1024} КБ | Изменен: {fi.LastWriteTime:dd.MM.yyyy}";
                    }
                }
            };

            // Комментарий
            var lblComment = new Label
            {
                Text = "Комментарий:",
                Location = new Point(20, 120),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };

            var txtComment = new TextBox
            {
                Location = new Point(150, 120),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Кнопка загрузки
            var btnUpload = new Button
            {
                Text = "⬆ ЗАГРУЗИТЬ ДОКУМЕНТ",
                Location = new Point(20, 160),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnUpload.FlatAppearance.BorderSize = 0;

            btnUpload.Click += (s, e) =>
            {
                if (cmbType.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип документа!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (lblFile.Text == "Файл не выбран" || !File.Exists(lblFile.Text))
                {
                    MessageBox.Show("Выберите файл для загрузки!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var fi = new FileInfo(lblFile.Text);
                    var fileName = $"{currentUser.Id}_{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(lblFile.Text)}";
                    var destPath = Path.Combine(uploadFolder, fileName);

                    File.Copy(lblFile.Text, destPath);

                    var document = new Document
                    {
                        UserId = currentUser.Id,
                        DocumentType = cmbType.SelectedItem.ToString(),
                        DocumentSubType = cmbSubType.Enabled ? cmbSubType.SelectedItem?.ToString() : null,
                        FileName = Path.GetFileName(lblFile.Text),
                        FilePath = destPath,
                        FileSize = fi.Length,
                        FileExtension = fi.Extension,
                        Status = "На рассмотрении",
                        UploadDate = DateTime.Now,
                        LastModifiedDate = DateTime.Now,
                        Comment = txtComment.Text,
                        Version = 1,
                        IsActive = true
                    };

                    dbContext.AddDocument(document);

                    MessageBox.Show("✅ Документ успешно загружен!", "Успешно",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Очистка
                    cmbType.SelectedIndex = -1;
                    cmbSubType.Items.Clear();
                    cmbSubType.Enabled = false;
                    txtComment.Clear();
                    lblFile.Text = "Файл не выбран";
                    lblFile.ForeColor = Color.Gray;
                    lblFileInfo.Text = "";

                    _ = RefreshDocumentsAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            group.Controls.AddRange(new Control[] {
                lblType, cmbType, lblSubType, cmbSubType,
                btnChooseFile, lblFile, lblFileInfo,
                lblComment, txtComment, btnUpload
            });

            return group;
        }

        private GroupBox CreateDocumentsListGroup()
        {
            var group = new GroupBox
            {
                Text = "📋 МОИ ДОКУМЕНТЫ",
                Size = new Size(1150, 350),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Кнопки
            var btnPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(1130, 40)
            };

            var btnView = new Button
            {
                Text = "👁 Просмотреть",
                Location = new Point(0, 5),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnView.FlatAppearance.BorderSize = 0;
            btnView.Click += (s, e) => ViewSelectedDocument();

            var btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Location = new Point(130, 5),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshDocumentsAsync();

            btnPanel.Controls.AddRange(new Control[] { btnView, btnRefresh });

            // Список документов
            documentsListView = new ListView
            {
                Location = new Point(10, 70),
                Size = new Size(1130, 260),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9)
            };

            documentsListView.Columns.Add("Тип документа", 150);
            documentsListView.Columns.Add("Подтип", 120);
            documentsListView.Columns.Add("Дата загрузки", 130);
            documentsListView.Columns.Add("Статус", 120);
            documentsListView.Columns.Add("Комментарий", 200);
            documentsListView.Columns.Add("Размер", 80);
            documentsListView.Columns.Add("Формат", 80);

            documentsListView.DoubleClick += (s, e) => ViewSelectedDocument();

            group.Controls.Add(btnPanel);
            group.Controls.Add(documentsListView);

            _ = RefreshDocumentsAsync();

            return group;
        }

        private void ViewSelectedDocument()
        {
            if (documentsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите документ для просмотра!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var docs = dbContext.GetUserDocuments(currentUser.Id);
            int index = documentsListView.SelectedIndices[0];

            if (index < docs.Count)
            {
                var doc = docs[index];
                if (File.Exists(doc.FilePath))
                {
                    try
                    {
                        System.Diagnostics.Process.Start(doc.FilePath);
                    }
                    catch
                    {
                        MessageBox.Show("Не удалось открыть файл!", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Файл не найден на диске!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ========== ВКЛАДКА ПРОВЕРКИ ==========
        private void CreateReviewTab(TabPage tabPage)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            var group = new GroupBox
            {
                Text = "✅ ДОКУМЕНТЫ НА ПРОВЕРКЕ",
                Size = new Size(1150, 500),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Кнопки
            var btnPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(1130, 40)
            };

            var btnView = new Button
            {
                Text = "👁 Просмотреть",
                Location = new Point(0, 5),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnView.FlatAppearance.BorderSize = 0;

            var btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Location = new Point(130, 5),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshReviewAsync();

            btnPanel.Controls.AddRange(new Control[] { btnView, btnRefresh });

            // Список документов
            reviewListView = new ListView
            {
                Location = new Point(10, 70),
                Size = new Size(1130, 300),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9)
            };

            reviewListView.Columns.Add("ID", 50);
            reviewListView.Columns.Add("ФИО", 200);
            reviewListView.Columns.Add("Тип", 150);
            reviewListView.Columns.Add("Подтип", 120);
            reviewListView.Columns.Add("Дата", 130);
            reviewListView.Columns.Add("Статус", 120);
            reviewListView.Columns.Add("Размер", 80);
            reviewListView.Columns.Add("Формат", 80);

            btnView.Click += (s, e) =>
            {
                if (reviewListView.SelectedItems.Count > 0)
                {
                    int docId = int.Parse(reviewListView.SelectedItems[0].Text);
                    var docs = dbContext.GetAllDocuments();
                    var doc = docs.FirstOrDefault(d => d.Id == docId);
                    if (doc != null && File.Exists(doc.FilePath))
                    {
                        System.Diagnostics.Process.Start(doc.FilePath);
                    }
                }
            };

            // Комментарий
            var lblComment = new Label
            {
                Text = "Комментарий:",
                Location = new Point(10, 380),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };

            var txtComment = new TextBox
            {
                Location = new Point(120, 380),
                Size = new Size(700, 25),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Кнопки действий
            var btnApprove = new Button
            {
                Text = "✅ ПРИНЯТЬ",
                Location = new Point(10, 420),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApprove.FlatAppearance.BorderSize = 0;

            var btnReject = new Button
            {
                Text = "❌ ОТКЛОНИТЬ",
                Location = new Point(170, 420),
                Size = new Size(150, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReject.FlatAppearance.BorderSize = 0;

            btnApprove.Click += (s, e) =>
            {
                if (reviewListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Выберите документ!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int docId = int.Parse(reviewListView.SelectedItems[0].Text);
                dbContext.UpdateDocumentStatus(docId, "Принят", txtComment.Text, currentUser.Id);
                MessageBox.Show("✅ Документ принят!", "Успешно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _ = RefreshReviewAsync();
                _ = RefreshDocumentsAsync();
            };

            btnReject.Click += (s, e) =>
            {
                if (reviewListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Выберите документ!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtComment.Text))
                {
                    MessageBox.Show("Укажите причину отклонения!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int docId = int.Parse(reviewListView.SelectedItems[0].Text);
                dbContext.UpdateDocumentStatus(docId, "Отклонен", txtComment.Text, currentUser.Id);
                MessageBox.Show("❌ Документ отклонен!", "Успешно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _ = RefreshReviewAsync();
                _ = RefreshDocumentsAsync();
            };

            group.Controls.AddRange(new Control[] {
                btnPanel, reviewListView,
                lblComment, txtComment,
                btnApprove, btnReject
            });

            panel.Controls.Add(group);
            tabPage.Controls.Add(panel);

            _ = RefreshReviewAsync();
        }

        // ========== ВКЛАДКА УПРАВЛЕНИЯ ПОЛЬЗОВАТЕЛЯМИ ==========
        private void CreateUsersTab(TabPage tabPage)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            var group = new GroupBox
            {
                Text = "👥 ПОЛЬЗОВАТЕЛИ СИСТЕМЫ",
                Size = new Size(1150, 400),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Кнопки
            var btnPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(1130, 40)
            };

            var btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Location = new Point(0, 5),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshUsersAsync();

            btnPanel.Controls.Add(btnRefresh);

            // Список пользователей
            usersListView = new ListView
            {
                Location = new Point(10, 70),
                Size = new Size(1130, 200),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9)
            };

            usersListView.Columns.Add("ID", 50);
            usersListView.Columns.Add("ФИО", 200);
            usersListView.Columns.Add("Логин", 120);
            usersListView.Columns.Add("Email", 180);
            usersListView.Columns.Add("Телефон", 120);
            usersListView.Columns.Add("Роль", 120);
            usersListView.Columns.Add("Регистрация", 100);
            usersListView.Columns.Add("Последний вход", 120);

            // Изменение роли
            var lblRole = new Label
            {
                Text = "Новая роль:",
                Location = new Point(10, 280),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };

            var cmbRole = new ComboBox
            {
                Location = new Point(120, 280),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbRole.Items.AddRange(new object[] { "Гражданин", "Проверяющий", "Администратор" });

            var btnChangeRole = new Button
            {
                Text = "✏ ИЗМЕНИТЬ РОЛЬ",
                Location = new Point(280, 280),
                Size = new Size(150, 25),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnChangeRole.FlatAppearance.BorderSize = 0;

            btnChangeRole.Click += (s, e) =>
            {
                if (usersListView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Выберите пользователя!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (cmbRole.SelectedItem == null)
                {
                    MessageBox.Show("Выберите роль!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int userId = int.Parse(usersListView.SelectedItems[0].Text);
                string newRole = cmbRole.SelectedItem.ToString();

                if (userId == currentUser.Id)
                {
                    MessageBox.Show("Нельзя изменить свою роль!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = MessageBox.Show($"Изменить роль на '{newRole}'?",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    dbContext.UpdateUserRole(userId, newRole);
                    MessageBox.Show("✅ Роль изменена!", "Успешно",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _ = RefreshUsersAsync();
                }
            };

            group.Controls.AddRange(new Control[] {
                btnPanel, usersListView,
                lblRole, cmbRole, btnChangeRole
            });

            panel.Controls.Add(group);
            tabPage.Controls.Add(panel);

            _ = RefreshUsersAsync();
        }

        // ========== ВКЛАДКА ЧАТА ==========
        private void CreateChatTab(TabPage tabPage)
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };

            if (currentUser.Role == "Гражданин")
            {
                // Группа создания обращения
                var createGroup = CreateTicketGroup();
                panel.Controls.Add(createGroup);
            }

            // Группа списка обращений
            var ticketsGroup = CreateTicketsGroup();
            panel.Controls.Add(ticketsGroup);

            tabPage.Controls.Add(panel);

            _ = RefreshTicketsAsync();
        }

        private GroupBox CreateTicketGroup()
        {
            var group = new GroupBox
            {
                Text = "📝 НОВОЕ ОБРАЩЕНИЕ",
                Size = new Size(1150, 150),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            var lblSubject = new Label
            {
                Text = "Тема:",
                Location = new Point(20, 30),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };

            var txtSubject = new TextBox
            {
                Location = new Point(110, 30),
                Size = new Size(500, 25),
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblPriority = new Label
            {
                Text = "Приоритет:",
                Location = new Point(20, 70),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9)
            };

            var cmbPriority = new ComboBox
            {
                Location = new Point(110, 70),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbPriority.Items.AddRange(new object[] { "Низкий", "Средний", "Высокий" });
            cmbPriority.SelectedIndex = 1;

            var btnCreate = new Button
            {
                Text = "📨 СОЗДАТЬ",
                Location = new Point(20, 110),
                Size = new Size(150, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;

            btnCreate.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSubject.Text))
                {
                    MessageBox.Show("Введите тему обращения!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var ticket = new Ticket
                {
                    UserId = currentUser.Id,
                    Subject = txtSubject.Text.Trim(),
                    Status = "Открыт",
                    CreatedAt = DateTime.Now,
                    Priority = cmbPriority.SelectedItem.ToString(),
                    AssignedToId = null
                };

                dbContext.CreateTicket(ticket);
                MessageBox.Show("✅ Обращение создано!", "Успешно",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtSubject.Clear();
                _ = RefreshTicketsAsync();
            };

            group.Controls.AddRange(new Control[] {
                lblSubject, txtSubject,
                lblPriority, cmbPriority,
                btnCreate
            });

            return group;
        }

        private GroupBox CreateTicketsGroup()
        {
            var group = new GroupBox
            {
                Text = "📋 СПИСОК ОБРАЩЕНИЙ",
                Size = new Size(1150, 350),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            // Кнопки
            var btnPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(1130, 40)
            };

            var btnRefresh = new Button
            {
                Text = "🔄 Обновить",
                Location = new Point(0, 5),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += async (s, e) => await RefreshTicketsAsync();

            var btnOpenChat = new Button
            {
                Text = "💬 ОТКРЫТЬ ЧАТ",
                Location = new Point(110, 5),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOpenChat.FlatAppearance.BorderSize = 0;
            btnOpenChat.Click += (s, e) => OpenSelectedTicket();

            btnPanel.Controls.AddRange(new Control[] { btnRefresh, btnOpenChat });

            // Список обращений
            ticketsListView = new ListView
            {
                Location = new Point(10, 70),
                Size = new Size(1130, 250),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9)
            };

            if (currentUser.Role == "Гражданин")
            {
                ticketsListView.Columns.Add("ID", 50);
                ticketsListView.Columns.Add("Дата", 130);
                ticketsListView.Columns.Add("Тема", 300);
                ticketsListView.Columns.Add("Приоритет", 100);
                ticketsListView.Columns.Add("Статус", 120);
                ticketsListView.Columns.Add("Ответственный", 150);
            }
            else
            {
                ticketsListView.Columns.Add("ID", 50);
                ticketsListView.Columns.Add("Пользователь", 200);
                ticketsListView.Columns.Add("Дата", 130);
                ticketsListView.Columns.Add("Тема", 250);
                ticketsListView.Columns.Add("Приоритет", 100);
                ticketsListView.Columns.Add("Статус", 120);
                ticketsListView.Columns.Add("Ответственный", 150);
            }

            ticketsListView.DoubleClick += (s, e) => OpenSelectedTicket();

            group.Controls.Add(btnPanel);
            group.Controls.Add(ticketsListView);

            return group;
        }

        private async Task RefreshTicketsAsync()
        {
            if (ticketsListView == null || ticketsListView.IsDisposed)
                return;

            var tickets = await Task.Run(() => dbContext.GetTickets(currentUser.Id, currentUser.Role));

            this.Invoke((MethodInvoker)delegate
            {
                ticketsListView.BeginUpdate();
                ticketsListView.Items.Clear();

                foreach (var t in tickets)
                {
                    var item = new ListViewItem(t.Id.ToString());

                    if (currentUser.Role == "Гражданин")
                    {
                        item.SubItems.Add(t.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
                        item.SubItems.Add(t.Subject);

                        string priority = t.Priority == "Высокий" ? "🔴 Высокий" :
                                         t.Priority == "Средний" ? "🟡 Средний" : "🟢 Низкий";
                        item.SubItems.Add(priority);

                        string status = t.Status == "Открыт" ? "🟡 Открыт" :
                                       t.Status == "В работе" ? "🔵 В работе" : "✅ Закрыт";
                        item.SubItems.Add(status);

                        item.SubItems.Add(t.AssignedToName ?? "Ожидает");
                    }
                    else
                    {
                        item.SubItems.Add(t.UserName);
                        item.SubItems.Add(t.CreatedAt.ToString("dd.MM.yyyy HH:mm"));
                        item.SubItems.Add(t.Subject);

                        string priority = t.Priority == "Высокий" ? "🔴 Высокий" :
                                         t.Priority == "Средний" ? "🟡 Средний" : "🟢 Низкий";
                        item.SubItems.Add(priority);

                        string status = t.Status == "Открыт" ? "🟡 Открыт" :
                                       t.Status == "В работе" ? "🔵 В работе" : "✅ Закрыт";
                        item.SubItems.Add(status);

                        item.SubItems.Add(t.AssignedToName ?? "Не назначен");
                    }

                    ticketsListView.Items.Add(item);
                }

                ticketsListView.EndUpdate();
            });
        }

        private void OpenSelectedTicket()
        {
            if (ticketsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите обращение!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int ticketId = int.Parse(ticketsListView.SelectedItems[0].Text);
            string subject = ticketsListView.SelectedItems[0].SubItems[2].Text;

            var tickets = dbContext.GetTickets(currentUser.Id, currentUser.Role);
            var ticket = tickets.FirstOrDefault(t => t.Id == ticketId);

            if (ticket != null)
            {
                var otherUser = dbContext.GetUserById(
                    currentUser.Id == ticket.UserId ? ticket.AssignedToId ?? ticket.UserId : ticket.UserId
                );

                if (otherUser != null)
                {
                    var chatForm = new ChatForm(currentUser, otherUser, ticketId, subject);
                    chatForm.ShowDialog();
                    _ = RefreshTicketsAsync();
                }
            }
        }

        private void RequestsListView_DoubleClick(object sender, EventArgs e)
        {
            if (requestsListView.SelectedItems.Count == 0)
                return;

            string requestIdText = requestsListView.SelectedItems[0].Text;
            if (requestIdText.StartsWith("REQ-"))
            {
                requestIdText = requestIdText.Replace("REQ-", "");
            }

            if (int.TryParse(requestIdText, out int id))
            {
                MessageBox.Show($"Просмотр заявки #{id}\nФункция просмотра в разработке", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
