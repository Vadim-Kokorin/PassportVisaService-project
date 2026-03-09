using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using PassportVisaService.Data;
using PassportVisaService.Models;

namespace PassportVisaService.Forms
{
    public partial class NewRequestForm : Form
    {
        private User currentUser;
        private DatabaseContext dbContext;
        private string serviceType;
        private string uploadFolder;

        private Panel mainPanel;
        private FlowLayoutPanel formPanel;
        private ListView docsList;
        private List<RequestDocument> uploadedDocuments = new List<RequestDocument>();
        private Dictionary<string, Control> formFields = new Dictionary<string, Control>();

        public NewRequestForm(User user, string service)
        {
            currentUser = user;
            serviceType = service;
            dbContext = new DatabaseContext();

            uploadFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PassportVisaService", "Temp");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            InitializeComponent();
            InitializeCustomComponent();
            BuildFormByServiceType();
        }

        private void InitializeCustomComponent()
        {
            this.Text = $"Новая заявка - {serviceType}";
            this.Size = new Size(800, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 248, 255);

            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true,
                BackColor = Color.FromArgb(240, 248, 255)
            };

            // Заголовок
            var lblTitle = new Label
            {
                Text = serviceType.ToUpper(),
                Location = new Point(0, 0),
                Size = new Size(740, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };
            mainPanel.Controls.Add(lblTitle);

            // Панель для полей формы
            formPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 50),
                Size = new Size(740, 450),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            mainPanel.Controls.Add(formPanel);

            // Панель для документов
            var docsGroup = new GroupBox
            {
                Text = "📎 ПРИЛОЖЕННЫЕ ФАЙЛЫ",
                Location = new Point(0, 510),
                Size = new Size(740, 150),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102)
            };

            docsList = new ListView
            {
                Location = new Point(10, 25),
                Size = new Size(720, 80),
                View = View.Details,
                FullRowSelect = true
            };
            docsList.Columns.Add("Тип", 150);
            docsList.Columns.Add("Файл", 400);
            docsList.Columns.Add("Размер", 150);

            docsGroup.Controls.Add(docsList);

            // Кнопки
            var btnPanel = new Panel
            {
                Location = new Point(0, 670),
                Size = new Size(740, 50)
            };

            var btnAddFile = new Button
            {
                Text = "📎 Добавить файл",
                Location = new Point(10, 10),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAddFile.FlatAppearance.BorderSize = 0;
            btnAddFile.Click += BtnAddFile_Click;

            var btnSubmit = new Button
            {
                Text = "📨 ОТПРАВИТЬ НА ПРОВЕРКУ",
                Location = new Point(350, 10),
                Size = new Size(220, 30),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new Point(580, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            btnPanel.Controls.AddRange(new Control[] { btnAddFile, btnSubmit, btnCancel });

            mainPanel.Controls.Add(docsGroup);
            mainPanel.Controls.Add(btnPanel);
            this.Controls.Add(mainPanel);
        }

        // ============= МЕТОДЫ ДЛЯ ПОСТРОЕНИЯ ФОРМЫ =============
        private void BuildFormByServiceType()
        {
            formPanel.Controls.Clear();
            formFields.Clear();

            // Приводим к нижнему регистру для сравнения
            string service = serviceType.ToLower().Trim();

            // Загранпаспорт
            if (service.Contains("загран") || service == "загранпаспорт" || service.Contains("заграничный"))
            {
                BuildForeignPassportForm();
                this.Text = "Новая заявка - Загранпаспорт";
            }
            // Внутренний паспорт
            else if (service.Contains("внутренний") || service == "внутренний паспорт")
            {
                BuildInternalPassportForm();
                this.Text = "Новая заявка - Внутренний паспорт";
            }
            // Шенгенская виза
            else if (service.Contains("шенген") || service.Contains("шенгенская"))
            {
                BuildSchengenVisaForm();
                this.Text = "Новая заявка - Шенгенская виза";
            }
            // Виза США
            else if (service.Contains("сша") || service.Contains("usa") || service.Contains("америка"))
            {
                BuildUSVisaForm();
                this.Text = "Новая заявка - Виза США";
            }
            // Виза Великобритании
            else if (service.Contains("великобритания") || service.Contains("uk") || service.Contains("англия"))
            {
                BuildUKVisaForm();
                this.Text = "Новая заявка - Виза Великобритании";
            }
            // Если ничего не подошло
            else
            {
                BuildDefaultForm();
                this.Text = "Новая заявка - Другая услуга";
            }
        }
        // ============= ФОРМА ДЛЯ ЗАГРАНПАСПОРТА =============
        private void BuildForeignPassportForm()
        {
            // Личные данные
            AddSectionHeader("ЛИЧНЫЕ ДАННЫЕ");
            AddField("Фамилия", "lastName", true);
            AddField("Имя", "firstName", true);
            AddField("Отчество", "middleName", false);
            AddField("Дата рождения", "birthDate", true, FieldType.Date);
            AddField("Место рождения", "birthPlace", true);
            AddField("Пол", "gender", true, FieldType.Combo, new string[] { "Мужской", "Женский" });
            AddField("Семейное положение", "maritalStatus", true, FieldType.Combo,
                new string[] { "Холост/Не замужем", "Женат/Замужем", "Разведен(а)", "Вдовец/Вдова" });

            // Паспортные данные
            AddSectionHeader("ПАСПОРТНЫЕ ДАННЫЕ");
            AddField("Серия внутреннего паспорта", "passportSeries", true);
            AddField("Номер внутреннего паспорта", "passportNumber", true);
            AddField("Кем выдан", "passportIssuedBy", true);
            AddField("Дата выдачи", "passportIssueDate", true, FieldType.Date);
            AddField("Код подразделения", "passportDepartmentCode", true);

            // Адрес и контакты
            AddSectionHeader("АДРЕС И КОНТАКТЫ");
            AddField("Адрес регистрации", "registrationAddress", true);
            AddField("Фактический адрес проживания", "actualAddress", false);
            AddField("Телефон", "phone", true);
            AddField("Email", "email", true);

            // Для военнообязанных
            AddSectionHeader("ДЛЯ ВОЕННООБЯЗАННЫХ");
            AddField("Военнообязанный", "isMilitary", true, FieldType.Combo, new string[] { "Да", "Нет" });
            AddField("Военный билет (серия, номер)", "militaryId", false);
            AddField("Военкомат приписки", "militaryCommissariat", false);

            // Список необходимых документов
            AddDocsHintForForeignPassport();
        }

        // ============= ФОРМА ДЛЯ ВНУТРЕННЕГО ПАСПОРТА =============
        private void BuildInternalPassportForm()
        {
            AddSectionHeader("ЛИЧНЫЕ ДАННЫЕ");
            AddField("Фамилия", "lastName", true);
            AddField("Имя", "firstName", true);
            AddField("Отчество", "middleName", true);
            AddField("Дата рождения", "birthDate", true, FieldType.Date);
            AddField("Место рождения", "birthPlace", true);
            AddField("Пол", "gender", true, FieldType.Combo, new string[] { "Мужской", "Женский" });

            AddSectionHeader("ДОПОЛНИТЕЛЬНЫЕ ДАННЫЕ");
            AddField("СНИЛС", "snils", true);
            AddField("ИНН", "inn", false);
            AddField("Адрес регистрации", "registrationAddress", true);
            AddField("Телефон", "phone", true);
            AddField("Email", "email", true);

            AddSectionHeader("ПРИЧИНА ЗАМЕНЫ");
            AddField("Причина замены", "replacementReason", true, FieldType.Combo,
                new string[] {
                    "Достижение 14 лет",
                    "Достижение 20 лет",
                    "Достижение 45 лет",
                    "Смена фамилии",
                    "Утеря/порча",
                    "Иное"
                });

            AddDocsHintForInternalPassport();
        }

        // ============= ФОРМА ДЛЯ ШЕНГЕНСКОЙ ВИЗЫ =============
        private void BuildSchengenVisaForm()
        {
            AddSectionHeader("ЛИЧНЫЕ ДАННЫЕ (КАК В ЗАГРАНПАСПОРТЕ)");
            AddField("Фамилия", "lastName", true);
            AddField("Имя", "firstName", true);
            AddField("Дата рождения", "birthDate", true, FieldType.Date);
            AddField("Место рождения", "birthPlace", true);
            AddField("Гражданство", "citizenship", true);

            AddSectionHeader("ПАСПОРТНЫЕ ДАННЫЕ");
            AddField("Номер загранпаспорта", "passportNumber", true);
            AddField("Дата выдачи", "passportIssueDate", true, FieldType.Date);
            AddField("Срок действия до", "passportExpiry", true, FieldType.Date);
            AddField("Орган, выдавший паспорт", "passportAuthority", true);

            AddSectionHeader("ИНФОРМАЦИЯ О ПОЕЗДКЕ");
            AddField("Цель поездки", "purpose", true, FieldType.Combo,
                new string[] { "Туризм", "Бизнес", "Учеба", "Лечение", "Посещение родственников", "Транзит" });
            AddField("Страна въезда", "entryCountry", true);
            AddField("Дата въезда", "entryDate", true, FieldType.Date);
            AddField("Дата выезда", "exitDate", true, FieldType.Date);
            AddField("Количество въездов", "entries", true, FieldType.Combo,
                new string[] { "Однократная", "Двукратная", "Многократная" });

            AddSectionHeader("МЕСТО ПРОЖИВАНИЯ");
            AddField("Город/отель проживания", "accommodation", true);
            AddField("Адрес", "accommodationAddress", true);
            AddField("Телефон для связи", "contactPhone", true);

            AddSectionHeader("ДАННЫЕ О РАБОТЕ");
            AddField("Место работы/учебы", "workPlace", true);
            AddField("Должность", "position", true);
            AddField("Стаж работы", "workExperience", true);
            AddField("Среднемесячный доход", "monthlyIncome", true, FieldType.Text, null, "₽");

            AddDocsHintForSchengenVisa();
        }

        // ============= ФОРМА ДЛЯ ВИЗЫ США =============
        private void BuildUSVisaForm()
        {
            AddSectionHeader("ЛИЧНЫЕ ДАННЫЕ");
            AddField("Фамилия (как в загранпаспорте)", "lastName", true);
            AddField("Имя (как в загранпаспорте)", "firstName", true);
            AddField("Дата рождения", "birthDate", true, FieldType.Date);
            AddField("Место рождения", "birthPlace", true);
            AddField("Гражданство", "citizenship", true);

            AddSectionHeader("ПАСПОРТНЫЕ ДАННЫЕ");
            AddField("Номер загранпаспорта", "passportNumber", true);
            AddField("Срок действия до", "passportExpiry", true, FieldType.Date);

            AddSectionHeader("ИСТОРИЯ ПОЕЗДОК");
            AddField("Были ли ранее в США?", "visitedUS", true, FieldType.Combo, new string[] { "Да", "Нет" });
            AddField("Когда и сколько раз?", "previousVisits", false);
            AddField("Отказывали ли в визе США?", "wasRefused", true, FieldType.Combo, new string[] { "Да", "Нет" });

            AddSectionHeader("ПЛАНИРУЕМАЯ ПОЕЗДКА");
            AddField("Цель поездки", "purpose", true, FieldType.Combo,
                new string[] { "Туризм", "Бизнес", "Учеба", "Лечение", "Посещение родственников" });
            AddField("Планируемая дата въезда", "plannedEntryDate", true, FieldType.Date);
            AddField("Продолжительность пребывания", "stayDuration", true);
            AddField("Адрес в США", "usAddress", true);

            AddDocsHintForUSVisa();
        }

        // ============= ФОРМА ДЛЯ ВИЗЫ ВЕЛИКОБРИТАНИИ =============
        private void BuildUKVisaForm()
        {
            AddSectionHeader("ЛИЧНЫЕ ДАННЫЕ");
            AddField("Фамилия (как в загранпаспорте)", "lastName", true);
            AddField("Имя (как в загранпаспорте)", "firstName", true);
            AddField("Дата рождения", "birthDate", true, FieldType.Date);
            AddField("Место рождения", "birthPlace", true);
            AddField("Гражданство", "citizenship", true);

            AddSectionHeader("ПАСПОРТНЫЕ ДАННЫЕ");
            AddField("Номер загранпаспорта", "passportNumber", true);
            AddField("Дата выдачи", "passportIssueDate", true, FieldType.Date);
            AddField("Срок действия до", "passportExpiry", true, FieldType.Date);

            AddSectionHeader("ПЛАНИРУЕМАЯ ПОЕЗДКА");
            AddField("Цель поездки", "purpose", true, FieldType.Combo,
                new string[] { "Туризм", "Бизнес", "Учеба", "Работа", "Посещение родственников" });
            AddField("Дата въезда", "entryDate", true, FieldType.Date);
            AddField("Дата выезда", "exitDate", true, FieldType.Date);
            AddField("Адрес проживания в UK", "ukAddress", true);
            AddField("Телефон для связи в UK", "ukPhone", true);

            AddSectionHeader("ФИНАНСОВАЯ ИНФОРМАЦИЯ");
            AddField("Кто оплачивает поездку?", "sponsor", true, FieldType.Combo,
                new string[] { "Самостоятельно", "Родственники", "Работодатель", "Другое" });
            AddField("Наличие средств", "funds", true);
            AddField("Банковские выписки прилагаются?", "hasBankStatements", true, FieldType.Combo, new string[] { "Да", "Нет" });

            AddDocsHintForUKVisa();
        }

        // ============= СТАНДАРТНАЯ ФОРМА (ЕСЛИ ТИП НЕ ОПРЕДЕЛЕН) =============
        private void BuildDefaultForm()
        {
            AddSectionHeader("ОСНОВНЫЕ ДАННЫЕ");
            AddField("ФИО", "fullName", true);
            AddField("Краткое описание", "description", true);
            AddField("Дополнительная информация", "details", false);
        }

        // ============= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =============
        private void AddSectionHeader(string title)
        {
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                Size = new Size(700, 25),
                Margin = new Padding(10, 10, 10, 5),
                TextAlign = ContentAlignment.MiddleLeft
            };
            formPanel.Controls.Add(lbl);
        }

        private enum FieldType { Text, Date, Combo }

        private void AddField(string label, string fieldName, bool required, FieldType type = FieldType.Text, string[] comboItems = null, string suffix = "")
        {
            var panel = new Panel
            {
                Size = new Size(700, 35),
                Margin = new Padding(5)
            };

            var lbl = new Label
            {
                Text = required ? $"{label} *" : label,
                Location = new Point(10, 7),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9),
                ForeColor = required ? Color.Red : Color.Black
            };

            Control input = null;
            int inputWidth = string.IsNullOrEmpty(suffix) ? 450 : 350;

            switch (type)
            {
                case FieldType.Text:
                    input = new TextBox
                    {
                        Location = new Point(220, 5),
                        Size = new Size(inputWidth, 25),
                        Font = new Font("Segoe UI", 9),
                        Tag = fieldName
                    };
                    break;
                case FieldType.Date:
                    input = new DateTimePicker
                    {
                        Location = new Point(220, 5),
                        Size = new Size(200, 25),
                        Format = DateTimePickerFormat.Short,
                        Tag = fieldName
                    };
                    break;
                case FieldType.Combo:
                    var combo = new ComboBox
                    {
                        Location = new Point(220, 5),
                        Size = new Size(inputWidth, 25),
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Tag = fieldName
                    };
                    if (comboItems != null)
                        combo.Items.AddRange(comboItems);
                    combo.SelectedIndex = 0;
                    input = combo;
                    break;
            }

            panel.Controls.Add(lbl);
            panel.Controls.Add(input);

            if (!string.IsNullOrEmpty(suffix))
            {
                var lblSuffix = new Label
                {
                    Text = suffix,
                    Location = new Point(220 + inputWidth + 5, 7),
                    Size = new Size(50, 20),
                    Font = new Font("Segoe UI", 9)
                };
                panel.Controls.Add(lblSuffix);
            }

            formPanel.Controls.Add(panel);
            formFields[fieldName] = input;
        }

        // ============= ПОДСКАЗКИ ПО ДОКУМЕНТАМ =============
        private void AddDocsHintForForeignPassport()
        {
            var lblDocs = new Label
            {
                Text = "📋 НЕОБХОДИМЫЕ ДОКУМЕНТЫ:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                Size = new Size(700, 25),
                Margin = new Padding(10, 15, 10, 5)
            };
            formPanel.Controls.Add(lblDocs);

            var docsList = new ListBox
            {
                Size = new Size(700, 120),
                Margin = new Padding(30, 0, 10, 10),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            docsList.Items.AddRange(new object[]
            {
                "✓ Фотография 35x45 мм (матовая, цветная) - 2 шт",
                "✓ Копия внутреннего паспорта (все страницы с отметками)",
                "✓ Квитанция об оплате госпошлины (3500₽)",
                "✓ Действующий загранпаспорт (при наличии)",
                "✓ Трудовая книжка (копия) или выписка с сайта СФР",
                "✓ Военнообязанным: документы воинского учета",
                "✓ СНИЛС",
                "✓ ИНН (при наличии)",
                "✓ Заявление по форме (заполняется автоматически)"
            });

            formPanel.Controls.Add(docsList);

            var lblNote = new Label
            {
                Text = "❗ Срок оформления: до 1 месяца | Госпошлина: 3500₽",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.DarkRed,
                Size = new Size(700, 20),
                Margin = new Padding(30, 0, 10, 10)
            };
            formPanel.Controls.Add(lblNote);
        }

        private void AddDocsHintForInternalPassport()
        {
            var lblDocs = new Label
            {
                Text = "📋 НЕОБХОДИМЫЕ ДОКУМЕНТЫ:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                Size = new Size(700, 25),
                Margin = new Padding(10, 15, 10, 5)
            };
            formPanel.Controls.Add(lblDocs);

            var docsList = new ListBox
            {
                Size = new Size(700, 120),
                Margin = new Padding(30, 0, 10, 10),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            docsList.Items.AddRange(new object[]
            {
                "✓ Фотография 35x45 мм (матовая, черно-белая) - 2 шт",
                "✓ Свидетельство о рождении",
                "✓ Квитанция об оплате госпошлины (300₽)",
                "✓ Для мужчин: документ воинского учета",
                "✓ СНИЛС",
                "✓ Заявление по форме 1П",
                "✓ Документы, подтверждающие причину замены"
            });

            formPanel.Controls.Add(docsList);

            var lblNote = new Label
            {
                Text = "❗ Срок оформления: 10 дней | Госпошлина: 300₽",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.DarkRed,
                Size = new Size(700, 20),
                Margin = new Padding(30, 0, 10, 10)
            };
            formPanel.Controls.Add(lblNote);
        }

        private void AddDocsHintForSchengenVisa()
        {
            var lblDocs = new Label
            {
                Text = "📋 НЕОБХОДИМЫЕ ДОКУМЕНТЫ:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                Size = new Size(700, 25),
                Margin = new Padding(10, 15, 10, 5)
            };
            formPanel.Controls.Add(lblDocs);

            var docsList = new ListBox
            {
                Size = new Size(700, 150),
                Margin = new Padding(30, 0, 10, 10),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            docsList.Items.AddRange(new object[]
            {
                "✓ Загранпаспорт (срок действия не менее 3 месяцев после выезда)",
                "✓ Копии всех заполненных страниц загранпаспорта",
                "✓ Фотография 35x45 мм (цветная, на белом фоне) - 2 шт",
                "✓ Медицинская страховка (покрытие от €30,000)",
                "✓ Подтверждение брони отеля",
                "✓ Подтверждение финансовой состоятельности (справка с работы, выписка со счета)",
                "✓ Авиабилеты (бронь)",
                "✓ Справка с места работы (с указанием должности и дохода)",
                "✓ Заполненная визовая анкета",
                "✓ Согласие на обработку персональных данных"
            });

            formPanel.Controls.Add(docsList);

            var lblNote = new Label
            {
                Text = "❗ Консульский сбор: 80€ | Срок рассмотрения: 10-15 рабочих дней",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.DarkRed,
                Size = new Size(700, 20),
                Margin = new Padding(30, 0, 10, 10)
            };
            formPanel.Controls.Add(lblNote);
        }

        private void AddDocsHintForUSVisa()
        {
            var lblDocs = new Label
            {
                Text = "📋 НЕОБХОДИМЫЕ ДОКУМЕНТЫ:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                Size = new Size(700, 25),
                Margin = new Padding(10, 15, 10, 5)
            };
            formPanel.Controls.Add(lblDocs);

            var docsList = new ListBox
            {
                Size = new Size(700, 120),
                Margin = new Padding(30, 0, 10, 10),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            docsList.Items.AddRange(new object[]
            {
                "✓ Загранпаспорт (срок действия не менее 6 месяцев)",
                "✓ Фотография 50x50 мм",
                "✓ Заполненная форма DS-160",
                "✓ Подтверждение оплаты консульского сбора ($160)",
                "✓ Подтверждение цели поездки",
                "✓ Подтверждение финансовой состоятельности",
                "✓ Документы о трудоустройстве",
                "✓ Справка с места работы"
            });

            formPanel.Controls.Add(docsList);

            var lblNote = new Label
            {
                Text = "❗ Консульский сбор: $160 | Необходимо личное собеседование в посольстве",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.DarkRed,
                Size = new Size(700, 20),
                Margin = new Padding(30, 0, 10, 10)
            };
            formPanel.Controls.Add(lblNote);
        }

        private void AddDocsHintForUKVisa()
        {
            var lblDocs = new Label
            {
                Text = "📋 НЕОБХОДИМЫЕ ДОКУМЕНТЫ:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 51, 102),
                Size = new Size(700, 25),
                Margin = new Padding(10, 15, 10, 5)
            };
            formPanel.Controls.Add(lblDocs);

            var docsList = new ListBox
            {
                Size = new Size(700, 120),
                Margin = new Padding(30, 0, 10, 10),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            docsList.Items.AddRange(new object[]
            {
                "✓ Загранпаспорт",
                "✓ Фотография 45x35 мм",
                "✓ Подтверждение оплаты консульского сбора",
                "✓ Подтверждение цели поездки",
                "✓ Подтверждение финансовой состоятельности",
                "✓ Биометрические данные",
                "✓ Приглашение (если есть)",
                "✓ Бронь отеля или адрес проживания"
            });

            formPanel.Controls.Add(docsList);

            var lblNote = new Label
            {
                Text = "❗ Консульский сбор: зависит от типа визы | Срок рассмотрения: 15 рабочих дней",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.DarkRed,
                Size = new Size(700, 20),
                Margin = new Padding(30, 0, 10, 10)
            };
            formPanel.Controls.Add(lblNote);
        }

        // ============= МЕТОДЫ ДЛЯ РАБОТЫ С ФАЙЛАМИ =============
        private void BtnAddFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Все файлы|*.*|Изображения|*.jpg;*.jpeg;*.png|PDF|*.pdf|Word|*.doc;*.docx";
                dialog.FilterIndex = 1;
                dialog.Title = "Выберите файл";
                dialog.Multiselect = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string fileName in dialog.FileNames)
                    {
                        AddFileToList(fileName);
                    }
                }
            }
        }

        private void AddFileToList(string filePath)
        {
            var fi = new FileInfo(filePath);
            string type = "Документ";

            string ext = fi.Extension.ToLower();
            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
                type = "Изображение";
            else if (ext == ".pdf")
                type = "PDF";
            else if (ext == ".doc" || ext == ".docx")
                type = "Word";
            else if (ext == ".xls" || ext == ".xlsx")
                type = "Excel";

            // Проверяем, не добавлен ли уже такой файл
            foreach (ListViewItem item in docsList.Items)
            {
                if (item.SubItems[1].Text == fi.Name)
                {
                    MessageBox.Show($"Файл '{fi.Name}' уже добавлен!", "Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var listItem = new ListViewItem(type);
            listItem.SubItems.Add(fi.Name);
            listItem.SubItems.Add($"{fi.Length / 1024} КБ");
            docsList.Items.Add(listItem);

            // Копируем файл во временную папку
            string fileName = $"{Guid.NewGuid()}{fi.Extension}";
            string destPath = Path.Combine(uploadFolder, fileName);
            File.Copy(filePath, destPath);

            uploadedDocuments.Add(new RequestDocument
            {
                DocumentType = type,
                FileName = fi.Name,
                FilePath = destPath,
                FileSize = fi.Length
            });
        }

        // ============= МЕТОД ОТПРАВКИ ЗАЯВКИ =============
        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            // Проверка обязательных полей
            foreach (var field in formFields)
            {
                if (field.Value is TextBox txt && string.IsNullOrWhiteSpace(txt.Text))
                {
                    var panel = (Panel)field.Value.Parent;
                    var lbl = (Label)panel.Controls[0];
                    if (lbl.ForeColor == Color.Red)
                    {
                        MessageBox.Show($"Заполните поле {lbl.Text.Replace(" *", "")}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            try
            {
                // Собираем данные формы
                var formData = new Dictionary<string, object>();
                foreach (var field in formFields)
                {
                    if (field.Value is TextBox txt)
                        formData[field.Key] = txt.Text;
                    else if (field.Value is DateTimePicker dtp)
                        formData[field.Key] = dtp.Value.ToString("yyyy-MM-dd");
                    else if (field.Value is ComboBox combo)
                        formData[field.Key] = combo.SelectedItem?.ToString();
                }

                // Создаем папку для документов пользователя
                string userDocsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PassportVisaService",
                    "Requests",
                    $"{currentUser.Id}_{DateTime.Now:yyyyMMdd_HHmmss}");

                if (!Directory.Exists(userDocsFolder))
                    Directory.CreateDirectory(userDocsFolder);

                // Создаем заявку
                var request = new ServiceRequest
                {
                    UserId = currentUser.Id,
                    ServiceType = serviceType,
                    Status = "На проверке",
                    CreatedAt = DateTime.Now,
                    FormData = JsonConvert.SerializeObject(formData, Formatting.Indented)
                };

                // Сохраняем заявку в БД
                await Task.Run(() => dbContext.CreateServiceRequest(request));

                // Сохраняем документы и привязываем к заявке
                int savedFiles = 0;
                foreach (var tempDoc in uploadedDocuments)
                {
                    try
                    {
                        string fileName = $"{Guid.NewGuid()}_{tempDoc.FileName}";
                        string destPath = Path.Combine(userDocsFolder, fileName);

                        if (File.Exists(tempDoc.FilePath))
                        {
                            File.Copy(tempDoc.FilePath, destPath);

                            var doc = new RequestDocument
                            {
                                RequestId = request.Id,
                                DocumentType = tempDoc.DocumentType,
                                FileName = tempDoc.FileName,
                                FilePath = destPath,
                                FileSize = tempDoc.FileSize,
                                UploadDate = DateTime.Now
                            };

                            await Task.Run(() => dbContext.AddRequestDocument(doc));
                            savedFiles++;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при сохранении файла {tempDoc.FileName}: {ex.Message}",
                            "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                // Очищаем временные файлы
                try
                {
                    foreach (var tempDoc in uploadedDocuments)
                    {
                        if (File.Exists(tempDoc.FilePath))
                            File.Delete(tempDoc.FilePath);
                    }
                }
                catch { }

                MessageBox.Show($"✅ ЗАЯВКА УСПЕШНО ОТПРАВЛЕНА!\n\n" +
                               $"Номер заявки: REQ-{request.Id}\n" +
                               $"Услуга: {serviceType}\n" +
                               $"Дата: {request.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                               $"Статус: На проверке\n" +
                               $"Приложено файлов: {savedFiles} из {uploadedDocuments.Count}",
                               "Успешно", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.DialogResult != DialogResult.OK)
            {
                try
                {
                    foreach (var tempDoc in uploadedDocuments)
                    {
                        if (File.Exists(tempDoc.FilePath))
                            File.Delete(tempDoc.FilePath);
                    }
                }
                catch { }
            }
            base.OnFormClosing(e);
        }

        private void NewRequestForm_Load(object sender, EventArgs e)
        {

        }
    }
}