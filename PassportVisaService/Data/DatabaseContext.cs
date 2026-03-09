using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using PassportVisaService.Models;

namespace PassportVisaService.Data
{
    public class DatabaseContext
    {
        private static readonly object _lockObject = new object();
        private const int MaxRetryCount = 3;
        private const int RetryDelayMs = 100;
        private readonly string connectionString;
        private readonly string dbPath;

        public DatabaseContext()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PassportVisaService");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            dbPath = Path.Combine(folder, "PassportVisa.db");
            connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            bool dbExists = File.Exists(dbPath);

            if (!dbExists)
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Создаем таблицу Users со всеми полями
                string createUsersTable = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT UNIQUE NOT NULL,
                Password TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Email TEXT NOT NULL,
                Phone TEXT,
                PassportSeries TEXT,
                PassportNumber TEXT,
                PassportIssuedBy TEXT,
                PassportIssueDate DATETIME,
                Citizenship TEXT,
                BirthDate DATETIME,
                BirthPlace TEXT,
                RegistrationAddress TEXT,
                Role TEXT NOT NULL,
                RegistrationDate DATETIME NOT NULL,
                LastLoginDate DATETIME
            )";
                using (var cmd = new SQLiteCommand(createUsersTable, connection))
                    cmd.ExecuteNonQuery();

                // Создаем таблицу Documents
                string createDocumentsTable = @"
            CREATE TABLE IF NOT EXISTS Documents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                DocumentType TEXT NOT NULL,
                DocumentSubType TEXT,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                FileSize INTEGER,
                FileExtension TEXT,
                Status TEXT NOT NULL,
                UploadDate DATETIME NOT NULL,
                LastModifiedDate DATETIME,
                Comment TEXT,
                ReviewedBy INTEGER,
                ReviewDate DATETIME,
                ReviewComment TEXT,
                Version INTEGER DEFAULT 1,
                IsActive INTEGER DEFAULT 1,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            )";
                using (var cmd = new SQLiteCommand(createDocumentsTable, connection))
                    cmd.ExecuteNonQuery();

                // Создаем таблицу Tickets
                string createTicketsTable = @"
            CREATE TABLE IF NOT EXISTS Tickets (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                AssignedToId INTEGER,
                Subject TEXT NOT NULL,
                Status TEXT NOT NULL,
                CreatedAt DATETIME NOT NULL,
                UpdatedAt DATETIME,
                Priority TEXT NOT NULL,
                FOREIGN KEY(UserId) REFERENCES Users(Id)
            )";
                using (var cmd = new SQLiteCommand(createTicketsTable, connection))
                    cmd.ExecuteNonQuery();

                // Создаем таблицу Messages
                string createMessagesTable = @"
            CREATE TABLE IF NOT EXISTS Messages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SenderId INTEGER NOT NULL,
                ReceiverId INTEGER NOT NULL,
                Content TEXT NOT NULL,
                Timestamp DATETIME NOT NULL,
                IsRead INTEGER DEFAULT 0,
                TicketId INTEGER,
                FOREIGN KEY(TicketId) REFERENCES Tickets(Id)
            )";


                    // Таблица заявок на услуги
                    string createRequestsTable = @"
        CREATE TABLE IF NOT EXISTS ServiceRequests (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            ServiceType TEXT NOT NULL,
            Status TEXT NOT NULL,
            CreatedAt DATETIME NOT NULL,
            UpdatedAt DATETIME,
            ReviewedBy INTEGER,
            ReviewDate DATETIME,
            ReviewComment TEXT,
            FormData TEXT,
            FOREIGN KEY(UserId) REFERENCES Users(Id),
            FOREIGN KEY(ReviewedBy) REFERENCES Users(Id)
        )";
                    using (var cmd = new SQLiteCommand(createRequestsTable, connection))
                        cmd.ExecuteNonQuery();

                    // Таблица документов к заявкам
                    string createRequestDocsTable = @"
        CREATE TABLE IF NOT EXISTS RequestDocuments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RequestId INTEGER NOT NULL,
            DocumentType TEXT NOT NULL,
            FileName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            FileSize INTEGER NOT NULL,
            UploadDate DATETIME NOT NULL,
            FOREIGN KEY(RequestId) REFERENCES ServiceRequests(Id) ON DELETE CASCADE
        )";
                    using (var cmd = new SQLiteCommand(createRequestDocsTable, connection))
                        cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createMessagesTable, connection))
                    cmd.ExecuteNonQuery();

                // Создаем администратора, если таблица только что создана
                if (!dbExists)
                {
                    CreateDefaultAdmin();
                }
                else
                {
                    // Проверяем, есть ли админ, если нет - создаем
                    CheckAndCreateAdmin();
                }
            }
        }

        private void CreateDefaultAdmin()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string insertAdmin = @"
            INSERT INTO Users (Username, Password, FullName, Email, Role, RegistrationDate)
            VALUES ('admin', 'admin123', 'Администратор', 'admin@passport.ru', 'Администратор', @date)";
                using (var insertCmd = new SQLiteCommand(insertAdmin, connection))
                {
                    insertCmd.Parameters.AddWithValue("@date", DateTime.Now);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
        private void CheckAndCreateAdmin()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // Проверяем, существует ли колонка Phone
                bool phoneColumnExists = false;
                string pragmaQuery = "PRAGMA table_info(Users)";
                using (var cmd = new SQLiteCommand(pragmaQuery, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["name"].ToString() == "Phone")
                        {
                            phoneColumnExists = true;
                            break;
                        }
                    }
                }

                // Если колонки Phone нет, добавляем её
                if (!phoneColumnExists)
                {
                    try
                    {
                        string alterQuery = "ALTER TABLE Users ADD COLUMN Phone TEXT";
                        using (var cmd = new SQLiteCommand(alterQuery, connection))
                            cmd.ExecuteNonQuery();
                    }
                    catch { }
                }

                // Создаем администратора, если его нет
                string checkAdmin = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'";
                using (var cmd = new SQLiteCommand(checkAdmin, connection))
                {
                    int count = Convert.ToInt32(cmd.ExecuteScalar());
                    if (count == 0)
                    {
                        string insertAdmin = @"
                    INSERT INTO Users (Username, Password, FullName, Email, Role, RegistrationDate)
                    VALUES ('admin', 'admin123', 'Администратор', 'admin@passport.ru', 'Администратор', @date)";
                        using (var insertCmd = new SQLiteCommand(insertAdmin, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@date", DateTime.Now);
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        // Методы для пользователей
        public void AddUser(User user)
        {
            lock (_lockObject)
            {
                for (int retry = 0; retry < MaxRetryCount; retry++)
                {
                    try
                    {
                        using (var connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            using (var transaction = connection.BeginTransaction())
                            {
                                string query = @"
                            INSERT INTO Users (
                                Username, Password, FullName, Email, Phone, 
                                PassportSeries, PassportNumber, PassportIssuedBy, PassportIssueDate,
                                Citizenship, BirthDate, BirthPlace, RegistrationAddress,
                                Role, RegistrationDate, LastLoginDate
                            )
                            VALUES (
                                @username, @password, @fullname, @email, @phone,
                                @passportseries, @passportnumber, @passportissuedby, @passportissuedate,
                                @citizenship, @birthdate, @birthplace, @registrationaddress,
                                @role, @regdate, @lastlogin
                            )";

                                using (var cmd = new SQLiteCommand(query, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@username", user.Username);
                                    cmd.Parameters.AddWithValue("@password", user.Password);
                                    cmd.Parameters.AddWithValue("@fullname", user.FullName);
                                    cmd.Parameters.AddWithValue("@email", user.Email);
                                    cmd.Parameters.AddWithValue("@phone", user.Phone ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@passportseries", user.PassportSeries ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@passportnumber", user.PassportNumber ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@passportissuedby", user.PassportIssuedBy ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@passportissuedate", user.PassportIssueDate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@citizenship", user.Citizenship ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@birthdate", user.BirthDate ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@birthplace", user.BirthPlace ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@registrationaddress", user.RegistrationAddress ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@role", user.Role);
                                    cmd.Parameters.AddWithValue("@regdate", user.RegistrationDate);
                                    cmd.Parameters.AddWithValue("@lastlogin", user.LastLoginDate ?? (object)DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                break; // Успешно - выходим из цикла
                            }
                        }
                    }
                    catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy ||
                                                     ex.ResultCode == SQLiteErrorCode.Locked)
                    {
                        if (retry == MaxRetryCount - 1)
                            throw; // Последняя попытка - пробрасываем исключение

                        System.Threading.Thread.Sleep(RetryDelayMs * (retry + 1));
                    }
                }
            }
        }

        public User GetUser(string username, string password)
        {
            lock (_lockObject)
            {
                for (int retry = 0; retry < MaxRetryCount; retry++)
                {
                    try
                    {
                        using (var connection = new SQLiteConnection(connectionString))
                        {
                            connection.Open();
                            string query = "SELECT * FROM Users WHERE Username = @u AND Password = @p";
                            using (var cmd = new SQLiteCommand(query, connection))
                            {
                                cmd.Parameters.AddWithValue("@u", username);
                                cmd.Parameters.AddWithValue("@p", password);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        var user = MapUser(reader);
                                        return user;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Busy ||
                                                     ex.ResultCode == SQLiteErrorCode.Locked)
                    {
                        if (retry == MaxRetryCount - 1)
                            throw;

                        System.Threading.Thread.Sleep(RetryDelayMs * (retry + 1));
                    }
                }
            }
            return null;
        }
        public User GetUserById(int id)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Users WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return MapUser(reader);
                    }
                }
            }
            return null;
        }

        public List<User> GetAllUsers()
        {
            var users = new List<User>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Users ORDER BY RegistrationDate DESC";
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        users.Add(MapUser(reader));
                }
            }
            return users;
        }

        private User MapUser(SQLiteDataReader reader)
        {
            return new User
            {
                Id = Convert.ToInt32(reader["Id"]),
                Username = reader["Username"].ToString(),
                Password = reader["Password"].ToString(),
                FullName = reader["FullName"].ToString(),
                Email = reader["Email"].ToString(),
                Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null,
                PassportSeries = reader["PassportSeries"] != DBNull.Value ? reader["PassportSeries"].ToString() : null,
                PassportNumber = reader["PassportNumber"] != DBNull.Value ? reader["PassportNumber"].ToString() : null,
                PassportIssuedBy = reader["PassportIssuedBy"] != DBNull.Value ? reader["PassportIssuedBy"].ToString() : null,
                PassportIssueDate = reader["PassportIssueDate"] != DBNull.Value ? Convert.ToDateTime(reader["PassportIssueDate"]) : (DateTime?)null,
                Citizenship = reader["Citizenship"] != DBNull.Value ? reader["Citizenship"].ToString() : null,
                BirthDate = reader["BirthDate"] != DBNull.Value ? Convert.ToDateTime(reader["BirthDate"]) : (DateTime?)null,
                BirthPlace = reader["BirthPlace"] != DBNull.Value ? reader["BirthPlace"].ToString() : null,
                RegistrationAddress = reader["RegistrationAddress"] != DBNull.Value ? reader["RegistrationAddress"].ToString() : null,
                Role = reader["Role"].ToString(),
                RegistrationDate = Convert.ToDateTime(reader["RegistrationDate"]),
                LastLoginDate = reader["LastLoginDate"] != DBNull.Value ? Convert.ToDateTime(reader["LastLoginDate"]) : (DateTime?)null
            };
        }

        public void UpdateLastLoginDate(int userId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Users SET LastLoginDate = @d WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@d", DateTime.Now);
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserProfile(User user)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    UPDATE Users SET FullName = @f, Email = @e, Phone = @ph,
                        PassportSeries = @ps, PassportNumber = @pn, PassportIssuedBy = @pi,
                        PassportIssueDate = @pid, Citizenship = @c, BirthDate = @bd,
                        BirthPlace = @bp, RegistrationAddress = @ra
                    WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@f", user.FullName);
                    cmd.Parameters.AddWithValue("@e", user.Email);
                    cmd.Parameters.AddWithValue("@ph", user.Phone ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ps", user.PassportSeries ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pn", user.PassportNumber ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pi", user.PassportIssuedBy ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@pid", user.PassportIssueDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@c", user.Citizenship ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@bd", user.BirthDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@bp", user.BirthPlace ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ra", user.RegistrationAddress ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", user.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool ChangePassword(int userId, string oldPassword, string newPassword)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string check = "SELECT COUNT(*) FROM Users WHERE Id = @id AND Password = @old";
                using (var cmd = new SQLiteCommand(check, connection))
                {
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.Parameters.AddWithValue("@old", oldPassword);
                    if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                        return false;
                }

                string update = "UPDATE Users SET Password = @new WHERE Id = @id";
                using (var cmd = new SQLiteCommand(update, connection))
                {
                    cmd.Parameters.AddWithValue("@new", newPassword);
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        public void UpdateUserRole(int userId, string newRole)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Users SET Role = @r WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@r", newRole);
                    cmd.Parameters.AddWithValue("@id", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Методы для документов
        public void AddDocument(Document doc)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    INSERT INTO Documents (UserId, DocumentType, DocumentSubType, FileName, FilePath,
                        FileSize, FileExtension, Status, UploadDate, LastModifiedDate, Comment, Version, IsActive)
                    VALUES (@uid, @dt, @dst, @fn, @fp, @fs, @fe, @s, @ud, @lmd, @c, @v, @ia)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@uid", doc.UserId);
                    cmd.Parameters.AddWithValue("@dt", doc.DocumentType);
                    cmd.Parameters.AddWithValue("@dst", doc.DocumentSubType ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fn", doc.FileName);
                    cmd.Parameters.AddWithValue("@fp", doc.FilePath);
                    cmd.Parameters.AddWithValue("@fs", doc.FileSize);
                    cmd.Parameters.AddWithValue("@fe", doc.FileExtension);
                    cmd.Parameters.AddWithValue("@s", doc.Status);
                    cmd.Parameters.AddWithValue("@ud", doc.UploadDate);
                    cmd.Parameters.AddWithValue("@lmd", doc.LastModifiedDate ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@c", doc.Comment ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@v", doc.Version);
                    cmd.Parameters.AddWithValue("@ia", doc.IsActive ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Document> GetUserDocuments(int userId)
        {
            var docs = new List<Document>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Documents WHERE UserId = @uid AND IsActive = 1 ORDER BY UploadDate DESC";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            docs.Add(MapDocument(reader));
                    }
                }
            }
            return docs;
        }

        public List<Document> GetAllDocuments()
        {
            var docs = new List<Document>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Documents WHERE IsActive = 1 ORDER BY UploadDate DESC";
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        docs.Add(MapDocument(reader));
                }
            }
            return docs;
        }

        private Document MapDocument(SQLiteDataReader reader)
        {
            return new Document
            {
                Id = Convert.ToInt32(reader["Id"]),
                UserId = Convert.ToInt32(reader["UserId"]),
                DocumentType = reader["DocumentType"].ToString(),
                DocumentSubType = reader["DocumentSubType"] != DBNull.Value ? reader["DocumentSubType"].ToString() : null,
                FileName = reader["FileName"].ToString(),
                FilePath = reader["FilePath"].ToString(),
                FileSize = reader["FileSize"] != DBNull.Value ? Convert.ToInt64(reader["FileSize"]) : 0,
                FileExtension = reader["FileExtension"] != DBNull.Value ? reader["FileExtension"].ToString() : null,
                Status = reader["Status"].ToString(),
                UploadDate = Convert.ToDateTime(reader["UploadDate"]),
                LastModifiedDate = reader["LastModifiedDate"] != DBNull.Value ? Convert.ToDateTime(reader["LastModifiedDate"]) : (DateTime?)null,
                Comment = reader["Comment"] != DBNull.Value ? reader["Comment"].ToString() : null,
                ReviewedBy = reader["ReviewedBy"] != DBNull.Value ? Convert.ToInt32(reader["ReviewedBy"]) : (int?)null,
                ReviewDate = reader["ReviewDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReviewDate"]) : (DateTime?)null,
                ReviewComment = reader["ReviewComment"] != DBNull.Value ? reader["ReviewComment"].ToString() : null,
                Version = reader["Version"] != DBNull.Value ? Convert.ToInt32(reader["Version"]) : 1,
                IsActive = Convert.ToInt32(reader["IsActive"]) == 1
            };
        }

        public void UpdateDocumentStatus(int docId, string status, string comment, int reviewerId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    UPDATE Documents SET Status = @s, ReviewComment = @c, 
                        ReviewedBy = @rid, ReviewDate = @rd, LastModifiedDate = @lmd
                    WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@s", status);
                    cmd.Parameters.AddWithValue("@c", comment);
                    cmd.Parameters.AddWithValue("@rid", reviewerId);
                    cmd.Parameters.AddWithValue("@rd", DateTime.Now);
                    cmd.Parameters.AddWithValue("@lmd", DateTime.Now);
                    cmd.Parameters.AddWithValue("@id", docId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Методы для обращений
        public void CreateTicket(Ticket ticket)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    INSERT INTO Tickets (UserId, AssignedToId, Subject, Status, CreatedAt, UpdatedAt, Priority)
                    VALUES (@uid, @aid, @s, @st, @ca, @ua, @p)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@uid", ticket.UserId);
                    cmd.Parameters.AddWithValue("@aid", ticket.AssignedToId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@s", ticket.Subject);
                    cmd.Parameters.AddWithValue("@st", ticket.Status);
                    cmd.Parameters.AddWithValue("@ca", ticket.CreatedAt);
                    cmd.Parameters.AddWithValue("@ua", ticket.UpdatedAt ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@p", ticket.Priority);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Ticket> GetTickets(int userId, string userRole)
        {
            var tickets = new List<Ticket>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query;
                if (userRole == "Гражданин")
                {
                    query = @"
                        SELECT t.*, u.FullName as UserName, a.FullName as AssignedToName 
                        FROM Tickets t
                        LEFT JOIN Users u ON t.UserId = u.Id
                        LEFT JOIN Users a ON t.AssignedToId = a.Id
                        WHERE t.UserId = @uid
                        ORDER BY t.CreatedAt DESC";
                }
                else
                {
                    query = @"
                        SELECT t.*, u.FullName as UserName, a.FullName as AssignedToName 
                        FROM Tickets t
                        LEFT JOIN Users u ON t.UserId = u.Id
                        LEFT JOIN Users a ON t.AssignedToId = a.Id
                        ORDER BY t.CreatedAt DESC";
                }

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    if (userRole == "Гражданин")
                        cmd.Parameters.AddWithValue("@uid", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tickets.Add(new Ticket
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                UserId = Convert.ToInt32(reader["UserId"]),
                                AssignedToId = reader["AssignedToId"] != DBNull.Value ? Convert.ToInt32(reader["AssignedToId"]) : (int?)null,
                                Subject = reader["Subject"].ToString(),
                                Status = reader["Status"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : (DateTime?)null,
                                Priority = reader["Priority"].ToString(),
                                UserName = reader["UserName"]?.ToString() ?? "Неизвестно",
                                AssignedToName = reader["AssignedToName"]?.ToString() ?? "Не назначен"
                            });
                        }
                    }
                }
            }
            return tickets;
        }

        public void UpdateTicketStatus(int ticketId, string status, int? assignedToId = null)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    UPDATE Tickets SET Status = @s, UpdatedAt = @ua, 
                        AssignedToId = COALESCE(@aid, AssignedToId)
                    WHERE Id = @id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@s", status);
                    cmd.Parameters.AddWithValue("@ua", DateTime.Now);
                    cmd.Parameters.AddWithValue("@aid", assignedToId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", ticketId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Методы для сообщений
        public void SendMessage(Message msg)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    INSERT INTO Messages (SenderId, ReceiverId, Content, Timestamp, IsRead, TicketId)
                    VALUES (@sid, @rid, @c, @ts, @ir, @tid)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@sid", msg.SenderId);
                    cmd.Parameters.AddWithValue("@rid", msg.ReceiverId);
                    cmd.Parameters.AddWithValue("@c", msg.Content);
                    cmd.Parameters.AddWithValue("@ts", msg.Timestamp);
                    cmd.Parameters.AddWithValue("@ir", msg.IsRead ? 1 : 0);
                    cmd.Parameters.AddWithValue("@tid", msg.TicketId ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Message> GetMessages(int ticketId)
        {
            var msgs = new List<Message>();
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT m.*, s.FullName as SenderName, r.FullName as ReceiverName 
                    FROM Messages m
                    LEFT JOIN Users s ON m.SenderId = s.Id
                    LEFT JOIN Users r ON m.ReceiverId = r.Id
                    WHERE m.TicketId = @tid
                    ORDER BY m.Timestamp ASC";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@tid", ticketId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            msgs.Add(new Message
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                SenderId = Convert.ToInt32(reader["SenderId"]),
                                ReceiverId = Convert.ToInt32(reader["ReceiverId"]),
                                Content = reader["Content"].ToString(),
                                Timestamp = Convert.ToDateTime(reader["Timestamp"]),
                                IsRead = Convert.ToInt32(reader["IsRead"]) == 1,
                                TicketId = reader["TicketId"] != DBNull.Value ? Convert.ToInt32(reader["TicketId"]) : (int?)null,
                                SenderName = reader["SenderName"]?.ToString() ?? "Неизвестно",
                                ReceiverName = reader["ReceiverName"]?.ToString() ?? "Неизвестно"
                            });
                        }
                    }
                }
            }
            return msgs;
        }

        public int GetUnreadMessagesCount(int userId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM Messages WHERE ReceiverId = @uid AND IsRead = 0";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void MarkMessagesAsRead(int ticketId, int receiverId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string query = "UPDATE Messages SET IsRead = 1 WHERE TicketId = @tid AND ReceiverId = @rid";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@tid", ticketId);
                    cmd.Parameters.AddWithValue("@rid", receiverId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        // ============= МЕТОДЫ ДЛЯ РАБОТЫ С ЗАЯВКАМИ =============

        // ============= МЕТОДЫ ДЛЯ РАБОТЫ С ЗАЯВКАМИ =============

        public void CreateServiceRequest(ServiceRequest request)
        {
            lock (_lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                INSERT INTO ServiceRequests (
                    UserId, ServiceType, Status, CreatedAt, UpdatedAt, 
                    ReviewedBy, ReviewDate, ReviewComment, FormData
                )
                VALUES (
                    @userid, @servicetype, @status, @createdat, @updatedat,
                    @reviewedby, @reviewdate, @reviewcomment, @formdata
                );
                SELECT last_insert_rowid();";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userid", request.UserId);
                        cmd.Parameters.AddWithValue("@servicetype", request.ServiceType);
                        cmd.Parameters.AddWithValue("@status", request.Status);
                        cmd.Parameters.AddWithValue("@createdat", request.CreatedAt);
                        cmd.Parameters.AddWithValue("@updatedat", request.UpdatedAt ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@reviewedby", request.ReviewedBy ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@reviewdate", request.ReviewDate ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@reviewcomment", request.ReviewComment ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@formdata", request.FormData ?? (object)DBNull.Value);

                        request.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
        }

        public void AddRequestDocument(RequestDocument doc)
        {
            lock (_lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                INSERT INTO RequestDocuments (
                    RequestId, DocumentType, FileName, FilePath, FileSize, UploadDate
                )
                VALUES (
                    @requestid, @doctype, @filename, @filepath, @filesize, @uploaddate
                )";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@requestid", doc.RequestId);
                        cmd.Parameters.AddWithValue("@doctype", doc.DocumentType);
                        cmd.Parameters.AddWithValue("@filename", doc.FileName);
                        cmd.Parameters.AddWithValue("@filepath", doc.FilePath);
                        cmd.Parameters.AddWithValue("@filesize", doc.FileSize);
                        cmd.Parameters.AddWithValue("@uploaddate", doc.UploadDate);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<ServiceRequest> GetUserRequests(int userId)
        {
            var requests = new List<ServiceRequest>();

            lock (_lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT sr.*, u.FullName as UserName, r.FullName as ReviewerName 
                FROM ServiceRequests sr
                LEFT JOIN Users u ON sr.UserId = u.Id
                LEFT JOIN Users r ON sr.ReviewedBy = r.Id
                WHERE sr.UserId = @userid
                ORDER BY sr.CreatedAt DESC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userid", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                requests.Add(new ServiceRequest
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    UserId = Convert.ToInt32(reader["UserId"]),
                                    ServiceType = reader["ServiceType"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                    UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : (DateTime?)null,
                                    ReviewedBy = reader["ReviewedBy"] != DBNull.Value ? Convert.ToInt32(reader["ReviewedBy"]) : (int?)null,
                                    ReviewDate = reader["ReviewDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReviewDate"]) : (DateTime?)null,
                                    ReviewComment = reader["ReviewComment"] != DBNull.Value ? reader["ReviewComment"].ToString() : null,
                                    FormData = reader["FormData"] != DBNull.Value ? reader["FormData"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }

            return requests;
        }

        public List<RequestDocument> GetRequestDocuments(int requestId)
        {
            var docs = new List<RequestDocument>();

            lock (_lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM RequestDocuments WHERE RequestId = @requestid ORDER BY UploadDate DESC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@requestid", requestId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                docs.Add(new RequestDocument
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    RequestId = Convert.ToInt32(reader["RequestId"]),
                                    DocumentType = reader["DocumentType"].ToString(),
                                    FileName = reader["FileName"].ToString(),
                                    FilePath = reader["FilePath"].ToString(),
                                    FileSize = Convert.ToInt64(reader["FileSize"]),
                                    UploadDate = Convert.ToDateTime(reader["UploadDate"])
                                });
                            }
                        }
                    }
                }
            }

            return docs;
        }

        public List<ServiceRequest> GetAllRequestsForReview()
        {
            var requests = new List<ServiceRequest>();

            lock (_lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT sr.*, u.FullName as UserName, r.FullName as ReviewerName 
                FROM ServiceRequests sr
                LEFT JOIN Users u ON sr.UserId = u.Id
                LEFT JOIN Users r ON sr.ReviewedBy = r.Id
                WHERE sr.Status IN ('На проверке', 'Требует доработки')
                ORDER BY sr.CreatedAt ASC";

                    using (var cmd = new SQLiteCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            requests.Add(new ServiceRequest
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                UserId = Convert.ToInt32(reader["UserId"]),
                                ServiceType = reader["ServiceType"].ToString(),
                                Status = reader["Status"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                                UpdatedAt = reader["UpdatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedAt"]) : (DateTime?)null,
                                ReviewedBy = reader["ReviewedBy"] != DBNull.Value ? Convert.ToInt32(reader["ReviewedBy"]) : (int?)null,
                                ReviewDate = reader["ReviewDate"] != DBNull.Value ? Convert.ToDateTime(reader["ReviewDate"]) : (DateTime?)null,
                                ReviewComment = reader["ReviewComment"] != DBNull.Value ? reader["ReviewComment"].ToString() : null,
                                FormData = reader["FormData"] != DBNull.Value ? reader["FormData"].ToString() : null,
                                UserName = reader["UserName"] != DBNull.Value ? reader["UserName"].ToString() : null,
                                ReviewerName = reader["ReviewerName"] != DBNull.Value ? reader["ReviewerName"].ToString() : null
                            });
                        }
                    }
                }
            }

            return requests;
        }

        public void UpdateRequestStatus(int requestId, string status, string comment, int reviewerId)
        {
            lock (_lockObject)
            {
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                UPDATE ServiceRequests 
                SET Status = @status, 
                    ReviewComment = @comment, 
                    ReviewedBy = @reviewerid,
                    ReviewDate = @reviewdate,
                    UpdatedAt = @updatedat
                WHERE Id = @id";

                    using (var cmd = new SQLiteCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@status", status);
                        cmd.Parameters.AddWithValue("@comment", comment);
                        cmd.Parameters.AddWithValue("@reviewerid", reviewerId);
                        cmd.Parameters.AddWithValue("@reviewdate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@updatedat", DateTime.Now);
                        cmd.Parameters.AddWithValue("@id", requestId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


    }
}