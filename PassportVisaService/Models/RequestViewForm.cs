using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;
using PassportVisaService.Data;
using PassportVisaService.Models;

namespace PassportVisaService.Forms
{
    public partial class RequestViewForm : Form
    {
        private ServiceRequest request;
        private DatabaseContext dbContext;
        private List<RequestDocument> documents;

        private Label lblStatus;
        private Label lblComment;
        private ListView docsListView;
        private RichTextBox txtFormData;

        public RequestViewForm(ServiceRequest serviceRequest)
        {
            request = serviceRequest;
            dbContext = new DatabaseContext();
            documents = dbContext.GetRequestDocuments(request.Id);

            InitializeComponent();
            InitializeCustomComponent();
            LoadRequestData();
        }

        private void InitializeCustomComponent()
        {
            // УВЕЛИЧИВАЕМ РАЗМЕР ОКНА
            this.Text = $"Просмотр заявки REQ-{request.Id}";
            this.Size = new Size(1100, 800); // Было 900,700
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = true; // РАЗРЕШАЕМ РАЗВОРАЧИВАТЬ
            this.MinimizeBox = true;
            this.BackColor = Color.FromArgb(240, 248, 255);

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
                Text = $"ЗАЯВКА № REQ-{request.Id}",
                Location = new Point(0, yPos),
                Size = new Size(1040, 50), // УВЕЛИЧИВАЕМ
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18, FontStyle.Bold), // УВЕЛИЧИВАЕМ ШРИФТ
                ForeColor = Color.FromArgb(0, 51, 102)
            };
            mainPanel.Controls.Add(lblTitle);
            yPos += 60;

            // Информация о заявке - УВЕЛИЧИВАЕМ
            var infoGroup = new GroupBox
            {
                Text = "ИНФОРМАЦИЯ О ЗАЯВКЕ",
                Location = new Point(0, yPos),
                Size = new Size(1040, 140),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            int iy = 25;

            AddInfoRow(infoGroup, "Услуга:", request.ServiceType, 20, iy);
            iy += 30;
            AddInfoRow(infoGroup, "Дата подачи:", request.CreatedAt.ToString("dd.MM.yyyy HH:mm"), 20, iy);
            iy += 30;

            string statusText = request.Status;
            Color statusColor = Color.Black;

            if (request.Status == "На проверке")
            {
                statusText = "⏳ На проверке";
                statusColor = Color.Orange;
            }
            else if (request.Status == "Одобрено")
            {
                statusText = "✅ Одобрено";
                statusColor = Color.Green;
            }
            else if (request.Status == "Отказано")
            {
                statusText = "❌ Отказано";
                statusColor = Color.Red;
            }
            else if (request.Status == "Требует доработки")
            {
                statusText = "📝 Требует доработки";
                statusColor = Color.Brown;
            }

            var lblStatusLabel = new Label
            {
                Text = "Статус:",
                Location = new Point(20, iy),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            lblStatus = new Label
            {
                Text = statusText,
                Location = new Point(130, iy),
                Size = new Size(300, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = statusColor
            };

            infoGroup.Controls.Add(lblStatusLabel);
            infoGroup.Controls.Add(lblStatus);

            mainPanel.Controls.Add(infoGroup);
            yPos += 150;

            // Данные анкеты - УВЕЛИЧИВАЕМ
            var dataGroup = new GroupBox
            {
                Text = "ДАННЫЕ АНКЕТЫ",
                Location = new Point(0, yPos),
                Size = new Size(1040, 300),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            txtFormData = new RichTextBox
            {
                Location = new Point(10, 30),
                Size = new Size(1020, 260),
                Font = new Font("Segoe UI", 10),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            dataGroup.Controls.Add(txtFormData);
            mainPanel.Controls.Add(dataGroup);
            yPos += 310;

            // Приложенные файлы - УВЕЛИЧИВАЕМ
            var docsGroup = new GroupBox
            {
                Text = "ПРИЛОЖЕННЫЕ ФАЙЛЫ",
                Location = new Point(0, yPos),
                Size = new Size(1040, 180),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            docsListView = new ListView
            {
                Location = new Point(10, 30),
                Size = new Size(1020, 100),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Segoe UI", 9)
            };
            docsListView.Columns.Add("Тип", 150);
            docsListView.Columns.Add("Имя файла", 500);
            docsListView.Columns.Add("Размер", 100);
            docsListView.Columns.Add("Дата", 200);

            docsListView.DoubleClick += (s, e) => OpenSelectedFile();

            // ОДНА КНОПКА (убрали вторую)
            var btnOpenFile = new Button
            {
                Text = "📂 ОТКРЫТЬ ФАЙЛ",
                Location = new Point(10, 140),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnOpenFile.FlatAppearance.BorderSize = 0;
            btnOpenFile.Click += (s, e) => OpenSelectedFile();

            docsGroup.Controls.Add(docsListView);
            docsGroup.Controls.Add(btnOpenFile);
            mainPanel.Controls.Add(docsGroup);
            yPos += 190;

            // Комментарий проверяющего (если есть)
            if (!string.IsNullOrEmpty(request.ReviewComment))
            {
                var commentGroup = new GroupBox
                {
                    Text = "КОММЕНТАРИЙ ПРОВЕРЯЮЩЕГО",
                    Location = new Point(0, yPos),
                    Size = new Size(1040, 80),
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 51, 102)
                };

                lblComment = new Label
                {
                    Text = request.ReviewComment,
                    Location = new Point(10, 30),
                    Size = new Size(1020, 40),
                    Font = new Font("Segoe UI", 10),
                    ForeColor = request.Status == "Отказано" ? Color.Red : Color.Brown
                };

                commentGroup.Controls.Add(lblComment);
                mainPanel.Controls.Add(commentGroup);
                yPos += 90;
            }

            // Кнопка закрытия
            var btnClose = new Button
            {
                Text = "ЗАКРЫТЬ",
                Location = new Point(450, yPos + 20),
                Size = new Size(150, 40),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            mainPanel.Controls.Add(btnClose);
            this.Controls.Add(mainPanel);
        }

        private void AddInfoRow(GroupBox group, string label, string value, int x, int y)
        {
            var lbl = new Label
            {
                Text = label,
                Location = new Point(x, y),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight
            };

            var val = new Label
            {
                Text = value,
                Location = new Point(x + 110, y),
                Size = new Size(600, 25),
                Font = new Font("Segoe UI", 10)
            };

            group.Controls.Add(lbl);
            group.Controls.Add(val);
        }

        private void LoadRequestData()
        {
            // Загружаем данные анкеты
            if (!string.IsNullOrEmpty(request.FormData))
            {
                try
                {
                    var formData = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.FormData);
                    string displayText = "";

                    foreach (var field in formData)
                    {
                        string fieldName = field.Key switch
                        {
                            "lastName" => "Фамилия",
                            "firstName" => "Имя",
                            "middleName" => "Отчество",
                            "birthDate" => "Дата рождения",
                            "birthPlace" => "Место рождения",
                            "gender" => "Пол",
                            "maritalStatus" => "Семейное положение",
                            "registrationAddress" => "Адрес регистрации",
                            "actualAddress" => "Фактический адрес",
                            "phone" => "Телефон",
                            "email" => "Email",
                            "snils" => "СНИЛС",
                            "inn" => "ИНН",
                            "passportSeries" => "Серия паспорта",
                            "passportNumber" => "Номер паспорта",
                            "passportIssuedBy" => "Кем выдан",
                            "passportIssueDate" => "Дата выдачи",
                            "passportDepartmentCode" => "Код подразделения",
                            "isMilitary" => "Военнообязанный",
                            "militaryId" => "Военный билет",
                            "militaryCommissariat" => "Военкомат",
                            "replacementReason" => "Причина замены",
                            // Убрали дубликат "passportNumber" - он уже есть выше
                            "passportExpiry" => "Срок действия",
                            "passportAuthority" => "Орган выдачи",
                            "purpose" => "Цель поездки",
                            "entryCountry" => "Страна въезда",
                            "entryDate" => "Дата въезда",
                            "exitDate" => "Дата выезда",
                            "entries" => "Количество въездов",
                            "accommodation" => "Проживание",
                            "accommodationAddress" => "Адрес проживания",
                            "contactPhone" => "Контактный телефон",
                            "workPlace" => "Место работы",
                            "position" => "Должность",
                            "workExperience" => "Стаж работы",
                            "monthlyIncome" => "Доход",
                            "visitedUS" => "Были в США",
                            "previousVisits" => "Предыдущие поездки",
                            "wasRefused" => "Отказ в визе",
                            "plannedEntryDate" => "Плановая дата въезда",
                            "stayDuration" => "Длительность",
                            "usAddress" => "Адрес в США",
                            "ukAddress" => "Адрес в UK",
                            "ukPhone" => "Телефон в UK",
                            "sponsor" => "Спонсор",
                            "funds" => "Наличие средств",
                            "hasBankStatements" => "Выписки из банка",
                            _ => field.Key
                        };

                        displayText += $"{fieldName}: {field.Value}\n";
                    }

                    txtFormData.Text = displayText;
                }
                catch
                {
                    txtFormData.Text = request.FormData;
                }
            }
            else
            {
                txtFormData.Text = "Нет данных анкеты";
            }

            // Загружаем файлы
            docsListView.Items.Clear();
            foreach (var doc in documents)
            {
                var item = new ListViewItem(doc.DocumentType);
                item.SubItems.Add(doc.FileName);
                item.SubItems.Add($"{doc.FileSize / 1024} КБ");
                item.SubItems.Add(doc.UploadDate.ToString("dd.MM.yyyy HH:mm"));
                item.Tag = doc;
                docsListView.Items.Add(item);
            }
        }

        private void OpenSelectedFile()
        {
            if (docsListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите файл для открытия!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var doc = (RequestDocument)docsListView.SelectedItems[0].Tag;

            if (File.Exists(doc.FilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = doc.FilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть файл: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Файл не найден на диске!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // RequestViewForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "RequestViewForm";
            this.Load += new System.EventHandler(this.RequestViewForm_Load);
            this.ResumeLayout(false);

        }

        private void RequestViewForm_Load(object sender, EventArgs e)
        {

        }
    }
}