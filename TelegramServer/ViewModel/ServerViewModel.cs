using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramServer.Core;
using TelegramServer.Model;
using TelegramLibrary;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using System.Data.Common;
using MaterialDesignThemes.Wpf;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Data;
using System.Windows.Input;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.Windows.Media;
using System.Runtime.InteropServices.ComTypes;

namespace TelegramServer.ViewModel
{
    public class ServerViewModel: ObservableObject
    {
        private int _port;
        private IPAddress _addr;
        private IPEndPoint _ep;

        private TcpListenerNew _listener;
        private BinaryFormatter _formatter;
        private Thread _listenerThread;

        string _connectionString;
        SqlConnection _connection;

        // 
        public ObservableCollection<LogViewModel> Logs { get; set; }
        public ObservableCollection<LogViewModel> LogsError { get; set; }
        public ObservableCollection<LogViewModel> LogsMain { get; set; }

        //
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }

        //
        public List<string> Colors { get; set; }

        public ServerViewModel()
        {
            StartServerCommand = new RelayCommand(ExecuteStartServerCommand, CanExecuteStartServerCommand);
            StopServerCommand = new RelayCommand(ExecuteStopServerCommand, CanExecuteStopServerCommand);

            Logs = new ObservableCollection<LogViewModel>();
            LogsError = new ObservableCollection<LogViewModel>();
            LogsMain = new ObservableCollection<LogViewModel>();
            _formatter = new BinaryFormatter();
            _port = 9001;
            _addr = IPAddress.Parse("127.0.0.1");
            _ep = new IPEndPoint(_addr, _port);

            _listener = new TcpListenerNew(_ep);

            _connectionString = ConfigurationManager.ConnectionStrings["Default"].ConnectionString;
            _connection = new SqlConnection(_connectionString);

            MainUser();

            Colors = new List<string>
            {
                "#F3B083",
                "#F6CA72",
                "#9AA0F4",
                "#A6E48E",
                "#82E1DB",
                "#89C0F6",
                "#E57D9A"
            };
        }

        // Making Main Telegram User
        // Now is only used in group chats

        private int TelegramId = -1;
        private string TelegramUsername = "#Telegram#";
        private string TelegramPassword = "d4cf97a6fb49327e6b404d2245f5eaa4";
        private byte[] TelegramAvatar = null;
        private string TelegramColor = "#38B0E3";
        private void MainUser()
        {
            FileStream fs = new FileStream(@"..\..\Avatar\avatartelegram.png", FileMode.Open, FileAccess.Read);
            TelegramAvatar = new byte[fs.Length];
            fs.Read(TelegramAvatar, 0, Convert.ToInt32(fs.Length));
            fs.Close();

            string query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username";
            _connection.Open();
            SqlCommand cmd = new SqlCommand(query, _connection);
            cmd.Parameters.Add(new SqlParameter("@username", TelegramUsername));

            if((int)cmd.ExecuteScalar() == 0)
            {
                query = $"INSERT INTO [User] (Username, Password, Name, ImageSource, Color) VALUES (@username, @password, @name, @imagesource, @color)";

                cmd = new SqlCommand(query, _connection);
                cmd.Parameters.Add(new SqlParameter("@username", TelegramUsername));
                cmd.Parameters.Add(new SqlParameter("@password", TelegramPassword));
                cmd.Parameters.Add(new SqlParameter("@name", ""));
                cmd.Parameters.Add(new SqlParameter("@imagesource", TelegramAvatar));
                cmd.Parameters.Add(new SqlParameter("@color", TelegramColor));

                int rowsAdded = cmd.ExecuteNonQuery();
                if (rowsAdded > 0)
                {
                    query = $"SELECT Id FROM [User] WHERE Username=@username";

                    cmd = new SqlCommand(query, _connection);
                    cmd.Parameters.Add(new SqlParameter("@username", TelegramUsername));

                    TelegramId = (int)cmd.ExecuteScalar();
                }
            }
            else
            {
                query = @"SELECT Id FROM [User] WHERE Username=@username";
                cmd = new SqlCommand(query, _connection);
                cmd.Parameters.Add(new SqlParameter("@username", TelegramUsername));

                TelegramId = (int)cmd.ExecuteScalar();
            }
        }

        // For logs

        private void WriteMain(string text)
        {
            var AddItem = new Action(() => LogsMain.Add(new LogViewModel(new LogModel() { Time = DateTime.Now, Text = text })));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, AddItem);
            WriteText(text);
        }
        private void WriteError(string text)
        {
            var AddItem = new Action(() => LogsError.Add(new LogViewModel(new LogModel() { Time = DateTime.Now, Text = text })));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, AddItem);
            WriteText(text);
        }
        private void WriteText(string text)
        {
            var AddItem = new Action(() => Logs.Add(new LogViewModel(new LogModel() { Time = DateTime.Now, Text = text })));
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, AddItem);
        }

        private bool CanExecuteStartServerCommand(object arg)
        {
            if (_listener.Active && _listenerThread.IsAlive)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void ExecuteStartServerCommand(object obj)
        {
            _listener.Start();
            WriteMain("* Server has been started");
            _listenerThread = new Thread(new ThreadStart(WorkLoop))
            {
                IsBackground = true
            };
            _listenerThread.Start();
        }

        private bool CanExecuteStopServerCommand(object arg)
        {
            if (_listener.Active && _listenerThread.IsAlive)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void ExecuteStopServerCommand(object obj)
        {
            try
            {
                _listener.Stop();
                WriteMain("* Server has been stopped");
            }
            catch (Exception err)
            {
                WriteError(err.Message);
            }
        }

        private byte[] ObjectToByteArray(object obj)
        {
            if (obj == System.DBNull.Value)
                return null;

            byte[] imagedata = (byte[])obj;
            return imagedata;
        }

        private void WorkLoop()
        {
            string query = "";
            string message = "";
            try
            {
                _connection.Close();
            }
            catch(Exception err)
            {

            }
            while (true)
            {
                TcpClient acceptor = new TcpClient();
                NetworkStream stream;
                try
                {
                    // 1
                    acceptor = _listener.AcceptTcpClient();
                    stream = acceptor.GetStream();
                    MyRequest request = (MyRequest)_formatter.Deserialize(stream);
                    // 2
                    switch (request.Header)
                    {
                        case "AUTHREGISTER":
                            {
                                UserModel user = request.User;
                                string username = user.Username;
                                //string email = user.Email;
                                string password = user.Password;
                                //string name = user.Name;

                                if(username == TelegramUsername)
                                {
                                    message = "FAILED";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                    throw new Exception("Can't register #Telegram# acount");
                                }

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = $"SELECT COUNT(*) FROM [User] WHERE Username=@username";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                //cmd.Parameters.Add(new SqlParameter("@email", email));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if (UserExists > 0)
                                {
                                    message = "FAILED ALREADY EXISTS";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                else
                                {
                                    query = $"INSERT INTO [User] (Username, Password, Name, ImageSource, Color) VALUES (@username, @password, @name, @imagesource, @color)";

                                    FileStream fs = new FileStream(@"..\..\Avatar\avatar.png", FileMode.Open, FileAccess.Read);
                                    byte[] imgByteArr = new byte[fs.Length];
                                    fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));
                                    fs.Close();

                                    Random rand = new Random();
                                    int colorId = rand.Next(Colors.Count);
                                    string color = Colors[colorId];

                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@username", username));
                                    cmd.Parameters.Add(new SqlParameter("@password", password));
                                    cmd.Parameters.Add(new SqlParameter("@name", ""));
                                    cmd.Parameters.Add(new SqlParameter("@imagesource", imgByteArr));
                                    cmd.Parameters.Add(new SqlParameter("@color", color));

                                    int rowsAdded = cmd.ExecuteNonQuery();
                                    if (rowsAdded > 0)
                                    {
                                        query = $"SELECT Id FROM [User] WHERE Username=@username";

                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@username", username));

                                        int userId = (int)cmd.ExecuteScalar();

                                        message = "SUCCESSFUL";
                                        UserModel newUser = new UserModel()
                                        {
                                            Id = userId,
                                            Username = username,
                                            Password = password,
                                            Name = "",
                                            ImageSource = imgByteArr
                                        };
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message,
                                            User = newUser
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                    else
                                    {
                                        message = "FAILED";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }

                                }
                                WriteMain($"{username} -> Registration {message}");

                                _connection.Close();
                                stream.Close();


                                break;
                            }
                        case "AUTHLOGIN":
                            {
                                UserModel user = request.User;
                                string username = user.Username;
                                string password = user.Password;

                                if (username == TelegramUsername)
                                {
                                    message = "FAILED";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                    throw new Exception("Can't login #Telegram# acount");
                                }

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT Id, Username, Password, Name, ImageSource FROM [User] WHERE Username=@username and Password=@password;";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));

                                SqlDataReader reader = cmd.ExecuteReader();
                                if (reader.Read())
                                {
                                    message = "SUCCESSFUL";
                                    UserModel MyUser = new UserModel()
                                    {
                                        Id = reader.GetInt32(0),
                                        Username = username,
                                        Password = password,
                                        Name = reader["Name"].ToString(),
                                        ImageSource = ObjectToByteArray(reader["ImageSource"])
                                    };
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message,
                                        User = MyUser
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                else
                                {
                                    message = "FAILED";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                WriteMain($"{username} -> Login {message}");

                                reader.Close();
                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "CHATSLOAD":
                            {
                                UserModel user = request.User;
                                string username = user.Username;
                                string password = user.Password;

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if (UserExists == 1)
                                {
                                    message = "SUCCESSFUL";

                                    // UserId
                                    query = @"SELECT Id FROM [User] WHERE Username=@username AND Password=@password";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@username", username));
                                    cmd.Parameters.Add(new SqlParameter("@password", password));

                                    int userId = (int)cmd.ExecuteScalar();

                                    // ChatId
                                    query = @"SELECT ChatId FROM [ChatUsers] WHERE UserId=@userid";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@userid", userId));

                                    bool check = false;

                                    // Checking if chats exists
                                    SqlDataReader reader = cmd.ExecuteReader();
                                    List<ChatModel> chats = new List<ChatModel>();
                                    while(reader.Read())
                                    {
                                        check = true;

                                        FileStream fs = new FileStream(@"..\..\Avatar\avatar.png", FileMode.Open, FileAccess.Read);
                                        byte[] imgByteArr = new byte[fs.Length];
                                        fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));
                                        fs.Close();

                                        int chatId = Convert.ToInt32(reader["ChatId"]);

                                        // Chat
                                        query = @"SELECT Id, Name, ImageSource, IsGroupChat FROM [Chat] WHERE Id=@chatid";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@chatid", chatId));

                                        SqlDataReader reader2 = cmd.ExecuteReader();

                                        query = @"SELECT Id, UserId, MessageText, MessageTime, ImageSource FROM [Message] WHERE Id = (SELECT LastMessageId FROM [Chat] WHERE Id = @chatId)";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                        SqlDataReader readerLastMessage = cmd.ExecuteReader();
                                        readerLastMessage.Read();

                                        while (reader2.Read())
                                        {
                                            // Not group chat
                                            if (reader2.GetInt32(3) == 0)
                                            {
                                                query = @"SELECT * FROM [User] WHERE Id IN (SELECT UserId FROM [ChatUsers] WHERE ChatId = @chatid)";
                                                cmd = new SqlCommand(query, _connection);
                                                cmd.Parameters.Add(new SqlParameter("@chatid", chatId));

                                                SqlDataReader reader3 = cmd.ExecuteReader();

                                                while(reader3.Read())
                                                {
                                                    if (userId != reader3.GetInt32(0))
                                                    {
                                                        bool isYours = Convert.ToInt32(readerLastMessage["UserId"]) == userId;
                                                        chats.Add(new ChatModel()
                                                        {
                                                            Id = reader2.GetInt32(0),
                                                            Name = reader3["Username"].ToString(),
                                                            WorkingName = reader2.GetString(1),
                                                            ImageSource = ObjectToByteArray(reader3["ImageSource"]),
                                                            IsGroupChat = reader2.GetInt32(3),
                                                            LastMessage = new MessageModel()
                                                            {
                                                                Id = readerLastMessage.GetInt32(0),
                                                                UserId = readerLastMessage.GetInt32(1),
                                                                MessageText = readerLastMessage["MessageText"].ToString(),
                                                                Time = Convert.ToDateTime(readerLastMessage["MessageTime"]),
                                                                YourMessage = isYours,
                                                                IsLastMessage = true
                                                            },
                                                            TimeLastMessage = Convert.ToDateTime(readerLastMessage["MessageTime"])
                                                        });
                                                    }
                                                }
                                                reader3.Close();
                                            }
                                            // Group chat
                                            else
                                            {
                                                string name = reader2.GetString(1);
                                                int creatorId = Int32.Parse(name.Substring(6, name.IndexOf('#') - 6));
                                                int chatNumber = Int32.Parse(name.Substring(name.IndexOf('#') + 1));

                                                query = @"SELECT Username FROM [User] WHERE Id = @creatorId";
                                                cmd = new SqlCommand(query, _connection);
                                                cmd.Parameters.Add(new SqlParameter("@creatorId", creatorId));

                                                string creatorUsername = cmd.ExecuteScalar().ToString();

                                                name = $"{creatorUsername} Group Chat {chatNumber}";

                                                bool isYours = Convert.ToInt32(readerLastMessage["UserId"]) == userId;
                                                chats.Add(new ChatModel()
                                                {
                                                    Id = reader2.GetInt32(0),
                                                    Name = name,
                                                    WorkingName = reader2.GetString(1),
                                                    ImageSource = ObjectToByteArray(reader2["ImageSource"]),
                                                    IsGroupChat = reader2.GetInt32(3),
                                                    LastMessage = new MessageModel()
                                                    {
                                                        Id = readerLastMessage.GetInt32(0),
                                                        UserId = readerLastMessage.GetInt32(1),
                                                        MessageText = readerLastMessage["MessageText"].ToString(),
                                                        Time = Convert.ToDateTime(readerLastMessage["MessageTime"]),
                                                        YourMessage = isYours,
                                                        IsLastMessage = true
                                                    },
                                                    TimeLastMessage = Convert.ToDateTime(readerLastMessage["MessageTime"])
                                                });
                                            }
                                        }
                                        
                                        readerLastMessage.Close();
                                        reader2.Close();
                                    }
                                    reader.Close();
                                    if (check)
                                    {
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message,
                                            Chats = chats
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                    else if (!check)
                                    {
                                        message = "SUCCESS NO CHATS";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                }
                                else
                                {
                                    message = "FAILED";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                WriteText($"{username} -> GetChats {message}");

                                _connection.Close();
                                stream.Close();
                                break;
                            }
                        case "FINDNEWCONTACT":
                            {
                                UserModel user = request.User;
                                string username = user.Username;
                                string password = user.Password;

                                UserModel findUser = request.FindUser;
                                string findUserName = findUser.Username;
                                if(findUserName == TelegramUsername)
                                {
                                    message = "FAILED";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                    throw new Exception("Can't find #Telegram# user");
                                }

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if (UserExists == 1)
                                {
                                    message = "SUCCESSFUL";

                                    // UserId
                                    query = @"SELECT Id FROM [User] WHERE Username=@username AND Password=@password";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@username", username));
                                    cmd.Parameters.Add(new SqlParameter("@password", password));

                                    int userId = (int)cmd.ExecuteScalar();

                                    // GetFindUserIds
                                    query = @"SELECT Id FROM [User] WHERE Username LIKE @username";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@username", findUserName));

                                    SqlDataReader reader = cmd.ExecuteReader();

                                    // CHECKING IF FINDUSERID NOT EQUALS TO USERID
                                    List<int> Ids = new List<int>();
                                    while (reader.Read())
                                    {
                                        int FindId = Convert.ToInt32(reader["Id"]);

                                        if (FindId != userId)
                                        {
                                            Ids.Add(FindId);
                                        }
                                    }
                                    reader.Close();

                                    // IF WE FOUND USERS
                                    if (Ids.Count > 0)
                                    {
                                        List<ChatModel> chats = new List<ChatModel>();
                                        // Cheking if there is no SINGLE chats between UsersIds
                                        foreach (int findUserId in Ids)
                                        {
                                            query = @"SELECT COUNT(Id) FROM [Chat] WHERE IsGroupChat = 0 AND Id IN ( SELECT ChatId FROM [ChatUsers] WHERE UserId = @finduserid AND ChatId IN (SELECT ChatId FROM [ChatUsers] WHERE UserId = @userid))";
                                            cmd = new SqlCommand(query, _connection);
                                            cmd.Parameters.Add(new SqlParameter("@userid", userId));
                                            cmd.Parameters.Add(new SqlParameter("@finduserid", findUserId));

                                            int check = (int)cmd.ExecuteScalar();
                                            if (check == 0)
                                            {
                                                query = @"SELECT Username, ImageSource FROM [User] WHERE Id=@userid";
                                                cmd = new SqlCommand(query, _connection);
                                                cmd.Parameters.Add(new SqlParameter("@userid", findUserId));

                                                reader = cmd.ExecuteReader();

                                                if (reader.Read())
                                                {
                                                    string workingName = "";
                                                    if (userId < findUserId)
                                                    {
                                                        workingName = $"{userId}#{findUserId}";
                                                    }
                                                    else
                                                    {
                                                        workingName = $"{findUserId}#{userId}";
                                                    }
                                                    chats.Add(new ChatModel()
                                                    {
                                                        Id = 0,
                                                        ImageSource = ObjectToByteArray(reader["ImageSource"]),
                                                        Name = reader.GetString(0),
                                                        WorkingName = workingName,
                                                        LastMessage = new MessageModel()
                                                        {
                                                            MessageText = "No messages yet",
                                                            IsLastMessage = true,
                                                            Id = 0,
                                                            UserId = userId,
                                                            YourMessage = true,
                                                            Time = DateTime.Now
                                                        },
                                                        IsGroupChat = 0,
                                                        IsClean = 1
                                                    });
                                                }

                                                reader.Close();
                                            }
                                        }

                                        if (chats.Count > 0)
                                        {
                                            message = "SUCCESSFUL";
                                            MyResponse response = new MyResponse()
                                            {
                                                Message = message,
                                                Chats = chats
                                            };
                                            _formatter.Serialize(stream, response);
                                        }
                                        else
                                        {
                                            message = "SUCCESS NO USERS";
                                            MyResponse response = new MyResponse()
                                            {
                                                Message = message
                                            };
                                            _formatter.Serialize(stream, response);
                                        }
                                    }
                                    else
                                    {
                                        message = "SUCCESS NO USERNAMES";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                }
                                else
                                {
                                    message = "FAILED";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }

                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "LOADCHAT":
                            {
                                ChatModel chat = request.Chat;
                                int chatId = chat.Id;

                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;

                                // -> Cheking if Chat exists and User has access to it
                                // ---------------------------------------------------
                                query = @"SELECT COUNT(*) FROM [ChatUsers] WHERE UserId=@userId AND ChatId=@chatId";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                int ChatExists = (int)cmd.ExecuteScalar();
                                if (ChatExists == 1)
                                {
                                    message = "SUCCESSFUL";

                                    // GetMessages
                                    query = @"SELECT Id, UserId, MessageText, MessageTime, ImageSource FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId)";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                    SqlDataReader reader = cmd.ExecuteReader();

                                    List<MessageModel> messages = new List<MessageModel>();
                                    while (reader.Read())
                                    {
                                        int userMessageId = reader.GetInt32(1);
                                        query = @"SELECT Color FROM [User] WHERE Id = @userMessageId";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@userMessageId", userMessageId));

                                        string color = cmd.ExecuteScalar().ToString();

                                        query = @"SELECT Username FROM [User] WHERE Id = @userMessageId";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@userMessageId", userMessageId));

                                        string usernameMessage = cmd.ExecuteScalar().ToString();

                                        messages.Add(new MessageModel()
                                        {
                                            Id = reader.GetInt32(0),
                                            UserId = reader.GetInt32(1),
                                            MessageText = reader.GetString(2),
                                            Time = reader.GetDateTime(3),
                                            ImageSource = ObjectToByteArray(reader["ImageSource"]),
                                            IsGroupMessage = false,
                                            Color = color,
                                            Username = usernameMessage
                                        });
                                    }
                                    reader.Close();
                                    if (messages.Count > 0)
                                    {
                                        query = @"SELECT IsGroupChat FROM [Chat] WHERE Id = @chatId";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                        int isGroupChat = (int)cmd.ExecuteScalar();
                                        if(isGroupChat == 1)
                                        {
                                            query = @"SELECT COUNT(*) FROM [ChatUsers] WHERE ChatId = @chatId";
                                            cmd = new SqlCommand(query, _connection);
                                            cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                            int membersCount = (int)cmd.ExecuteScalar();

                                            message = "SUCCESSFUL-LOADGROUPCHAT";
                                            MyResponse response = new MyResponse()
                                            {
                                                Message = message,
                                                Messages = messages,
                                                User = new UserModel()
                                                {
                                                    Id = membersCount
                                                }
                                            };
                                            _formatter.Serialize(stream, response);
                                        }
                                        else
                                        {
                                            query = @"SELECT Id, Username, Name, ImageSource, OfflineOnline, LastSeen FROM [User] WHERE Id IN (SELECT UserId FROM [ChatUsers] WHERE ChatId = @chatId) AND Id != @userId";
                                            cmd = new SqlCommand(query, _connection);
                                            cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                            cmd.Parameters.Add(new SqlParameter("@userId", userId));

                                            reader = cmd.ExecuteReader();

                                            if(reader.Read())
                                            {
                                                FileStream fs = new FileStream(@"..\..\Avatar\avatar.png", FileMode.Open, FileAccess.Read);
                                                byte[] imgByteArr = new byte[fs.Length];
                                                fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));
                                                fs.Close();

                                                message = "SUCCESSFUL-LOADPAIRCHAT";
                                                int oo = 0;
                                                DateTime ls = DateTime.ParseExact("01.01.2023 00:00:00", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                                                if (reader["OfflineOnline"] == System.DBNull.Value) oo = 0;
                                                else oo = Convert.ToInt32(reader["OfflineOnline"]);
                                                if (reader["LastSeen"] == System.DBNull.Value) { }
                                                else ls = Convert.ToDateTime(reader["LastSeen"]);
                                                if (reader["ImageSource"] == System.DBNull.Value) {  }
                                                else imgByteArr = ObjectToByteArray(reader["ImageSource"]);

                                                UserModel userChat = new UserModel()
                                                {
                                                    Id = Convert.ToInt32(reader["Id"]),
                                                    Username = reader["Username"].ToString(),
                                                    Name = reader["Name"].ToString(),
                                                    ImageSource = imgByteArr,
                                                    OfflineOnline = oo,
                                                    LastSeen = ls
                                                };
                                                MyResponse response = new MyResponse()
                                                {
                                                    Message = message,
                                                    Messages = messages,
                                                    User = userChat
                                                };
                                                _formatter.Serialize(stream, response);
                                            }
                                        }
                                    }
                                    else // Highly unlikely
                                    {
                                        message = "SUCCESS NO MESSAGES";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message,
                                            Messages = messages
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                }
                                else
                                {
                                    message = "FAILED CHAT NOT EXISTS";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                WriteText($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "SENDMESSAGENEWCONTACT":
                            {
                                ChatModel chat = request.Chat;
                                int isGroupChat = chat.IsGroupChat;
                                string workingName = chat.WorkingName;

                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;

                                MessageModel newMessage = request.SendMessage;
                                string messageText = newMessage.MessageText;
                                DateTime messageTime = newMessage.Time;
                                byte[] imageSource = newMessage.ImageSource;

                                // -> Cheking if Chat exists
                                // -------------------------
                                query = @"SELECT COUNT(*) FROM [Chat] WHERE Name = @name";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@name", workingName));

                                int ChatExists = (int)cmd.ExecuteScalar();
                                if(ChatExists == 0)
                                {
                                    // Send new message and create connections
                                    // To Message
                                    query = @"INSERT INTO [Message](UserId, MessageText, MessageTime, ImageSource) VALUES (@userId, @messageText, @messageTime, @imageSource); SELECT SCOPE_IDENTITY();";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                    cmd.Parameters.Add(new SqlParameter("@messageText", messageText));
                                    cmd.Parameters.Add(new SqlParameter("@messageTime", messageTime));
                                    cmd.Parameters.Add(new SqlParameter("@imageSource", imageSource));

                                    int messageId = Convert.ToInt32(cmd.ExecuteScalar());

                                    // To Chat
                                    int isClean = 0;
                                    query = @"INSERT INTO [Chat](Name, IsGroupChat, IsClean) VALUES (@name, @isGroupChat, @isClean); SELECT SCOPE_IDENTITY();";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@name", workingName));
                                    cmd.Parameters.Add(new SqlParameter("@isGroupChat", isGroupChat));
                                    cmd.Parameters.Add(new SqlParameter("@isClean", isClean));

                                    int chatId = Convert.ToInt32(cmd.ExecuteScalar());

                                    // To ChatMessages
                                    query = @"INSERT INTO [ChatMessages](ChatId, MessageId) VALUES (@chatId, @messageId)";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                    cmd.Parameters.Add(new SqlParameter("@messageId", messageId));

                                    cmd.ExecuteNonQuery();

                                    // To ChatUsers
                                    List<int> ids = workingName.Split('#').Select(Int32.Parse).ToList();

                                    query = @"INSERT INTO [ChatUsers](UserId, ChatId) VALUES (@userId1, @chatId); INSERT INTO [ChatUsers] (UserId, ChatId) VALUES (@userId2, @chatId)";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                    cmd.Parameters.Add(new SqlParameter("@userId1", ids[0]));
                                    cmd.Parameters.Add(new SqlParameter("@userId2", ids[1]));

                                    cmd.ExecuteNonQuery();

                                    // Update Chat
                                    query = @"UPDATE [Chat] SET LastMessage = (SELECT TOP 1 MessageText FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId) ORDER BY MessageTime DESC), TimeLastMessage = (SELECT TOP 1 MessageTime FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId) ORDER BY MessageTime DESC), LastMessageId = (SELECT TOP 1 Id FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId) ORDER BY MessageTime DESC) WHERE Id = @chatId";

                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                    int rowsUpdated = (int)cmd.ExecuteNonQuery();
                                    
                                    if(rowsUpdated == 1)
                                    {
                                        message = "SUCCESSFUL-SEND NEWMESSAGE NEWCONTACT";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                    else
                                    {
                                        message = "SUCCESSFUL-SEND NEWMESSAGE NEWCONTACT WITHOUT MESSAGE";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                }
                                else
                                {
                                    message = "FAILED-CHAT ALREADY EXISTS";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "SENDMESSAGE":
                            {
                                ChatModel chat = request.Chat;
                                int chatId = chat.Id;

                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;

                                MessageModel newMessage = request.SendMessage;
                                int messageUserId = newMessage.UserId;
                                string messageText = newMessage.MessageText;
                                DateTime messageTime = newMessage.Time;
                                byte[] imageSource = newMessage.ImageSource;

                                // -> Cheking if Chat exists
                                // -------------------------
                                query = @"SELECT COUNT(*) FROM [Chat] WHERE Id = @id";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@id", chatId)); 

                                int ChatExists = (int)cmd.ExecuteScalar();
                                if (ChatExists == 1)
                                {
                                    // Send new message and create connections
                                    // To Message
                                    query = @"INSERT INTO [Message](UserId, MessageText, MessageTime, ImageSource) VALUES (@userId, @messageText, @messageTime, @imageSource); SELECT SCOPE_IDENTITY();";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                    cmd.Parameters.Add(new SqlParameter("@messageText", messageText));
                                    cmd.Parameters.Add(new SqlParameter("@messageTime", messageTime));
                                    cmd.Parameters.Add(new SqlParameter("@imageSource", imageSource));

                                    int messageId = Convert.ToInt32(cmd.ExecuteScalar());

                                    // To ChatMessages
                                    query = @"INSERT INTO [ChatMessages](ChatId, MessageId) VALUES (@chatId, @messageId)";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                    cmd.Parameters.Add(new SqlParameter("@messageId", messageId));

                                    cmd.ExecuteNonQuery();

                                    // Update Chat
                                    query = @"UPDATE [Chat] SET LastMessage = (SELECT TOP 1 MessageText FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId) ORDER BY MessageTime DESC), TimeLastMessage = (SELECT TOP 1 MessageTime FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId) ORDER BY MessageTime DESC), LastMessageId = (SELECT TOP 1 Id FROM [Message] WHERE Id IN (SELECT MessageId FROM [ChatMessages] WHERE ChatId = @chatId) ORDER BY MessageTime DESC) WHERE Id = @chatId";

                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                    int rowsUpdated = (int)cmd.ExecuteNonQuery();

                                    if (rowsUpdated == 1)
                                    {
                                        message = "SUCCESSFUL-SEND NEWMESSAGE";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                    else
                                    {
                                        message = "SUCCESSFUL-SEND NEWMESSAGEWITHOUT MESSAGE";
                                        MyResponse response = new MyResponse()
                                        {
                                            Message = message
                                        };
                                        _formatter.Serialize(stream, response);
                                    }
                                }
                                else
                                {
                                    message = "FAILED-CHAT NOT EXISTS";
                                    MyResponse response = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, response);
                                }
                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "CHANGEAVATAR":
                            {
                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;
                                string password = user.Password;
                                byte[] imageSource = user.ImageSource;

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if (UserExists == 1)
                                {
                                    // Update image
                                    query = @"UPDATE [Message] SET ImageSource = @imageSource WHERE UserId = @userId";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@imageSource", imageSource));
                                    cmd.Parameters.Add(new SqlParameter("@userId", userId));

                                    cmd.ExecuteNonQuery();

                                    query = @"UPDATE [User] SET ImageSource = @imageSource WHERE Id = @userId";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@imageSource", imageSource));
                                    cmd.Parameters.Add(new SqlParameter("@userId", userId));

                                    int rowsUpdated = cmd.ExecuteNonQuery();
                                    if (rowsUpdated == 1)
                                    {
                                        message = "SUCCESSFULL-CHANGE AVATAR";
                                    }
                                    else
                                    {
                                        message = "FAILED-NOT CHANGED AVATAR";
                                    }
                                }
                                else
                                {
                                    message = "FAILED";
                                }
                                MyResponse response = new MyResponse()
                                {
                                    Message = message
                                };
                                _formatter.Serialize(stream, response);

                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "CHANGEACTIVITY":
                            {
                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;
                                string password = user.Password;
                                int oo = user.OfflineOnline;
                                DateTime lastSeen = user.LastSeen;

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if(UserExists == 1)
                                {
                                    // Update User
                                    query = @"UPDATE [User] SET OfflineOnline = @oo, LastSeen = @lastSeen WHERE Id = @userId";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@oo", oo));
                                    cmd.Parameters.Add(new SqlParameter("@lastSeen", lastSeen));
                                    cmd.Parameters.Add(new SqlParameter("@userId", userId));

                                    int rowsUpdated = cmd.ExecuteNonQuery();
                                    if(rowsUpdated == 1)
                                    {
                                        message = "SUCCESSFUL-CHANGE ACTIVITY";
                                    }
                                    else
                                    {
                                        message = "FAILED-NOT CHANGED ACTIVITY";
                                    }
                                }
                                else
                                {
                                    message = "FAILED";
                                }
                                MyResponse response = new MyResponse()
                                {
                                    Message = message
                                };
                                _formatter.Serialize(stream, response);

                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "CREATENEWGROUP":
                            {
                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;
                                string password = user.Password;

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if (UserExists == 1)
                                {
                                    int rowsFound = 0;
                                    string groupName = "";
                                    for (int i = 0; i < 5; i++)
                                    {
                                        groupName = $"GROUP-{userId}#{i + 1}";
                                        query = @"SELECT COUNT(*) FROM [Chat] WHERE Name = @groupName AND Id IN (SELECT ChatId FROM [ChatUsers] WHERE UserId = @userId)";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@groupName", groupName));
                                        cmd.Parameters.Add(new SqlParameter("@userId", userId));

                                        rowsFound += (int)cmd.ExecuteScalar();

                                        if ((int)cmd.ExecuteScalar() == 0) break;
                                    }
                                    if(rowsFound < 5)
                                    {
                                        FileStream fs = new FileStream(@"..\..\Avatar\avatargroup.png", FileMode.Open, FileAccess.Read);
                                        byte[] imgByteArr = new byte[fs.Length];
                                        fs.Read(imgByteArr, 0, Convert.ToInt32(fs.Length));
                                        fs.Close();

                                        string lm = $"{username} created group chat"; 
                                        DateTime tlm = DateTime.Now;

                                        query = @"INSERT INTO [Message](UserId, MessageText, MessageTime, ImageSource) VALUES(@userId, @messageText, @messageTime, @imageSource); SELECT SCOPE_IDENTITY()";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@userId", TelegramId));
                                        cmd.Parameters.Add(new SqlParameter("@messageText", lm));
                                        cmd.Parameters.Add(new SqlParameter("@messageTime", tlm));
                                        cmd.Parameters.Add(new SqlParameter("@imageSource", TelegramAvatar));

                                        int isGroupChat = 1;
                                        int messageId = Convert.ToInt32(cmd.ExecuteScalar());
                                        int ic = 0;

                                        groupName = $"GROUP-{userId}#{rowsFound + 1}";
                                        query = @"INSERT INTO [Chat](Name, ImageSource, IsGroupChat, LastMessage, LastMessageId, TimeLastMessage, IsClean) VALUES(@name, @is, @igc, @lm, @lmi, @tlm, @ic); SELECT SCOPE_IDENTITY()";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@name", groupName));
                                        cmd.Parameters.Add(new SqlParameter("@is", imgByteArr));
                                        cmd.Parameters.Add(new SqlParameter("@igc", isGroupChat));
                                        cmd.Parameters.Add(new SqlParameter("@lm", lm));
                                        cmd.Parameters.Add(new SqlParameter("@lmi", messageId));
                                        cmd.Parameters.Add(new SqlParameter("@tlm", tlm));
                                        cmd.Parameters.Add(new SqlParameter("@ic", ic));

                                        int chatId = Convert.ToInt32(cmd.ExecuteScalar());

                                        query = @"INSERT INTO [ChatUsers](UserId, ChatId) VALUES(@userId, @chatId)";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                        cmd.ExecuteNonQuery();

                                        query = @"INSERT INTO [ChatMessages](ChatId, MessageId) VALUES(@chatId, @messageId)";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                        cmd.Parameters.Add(new SqlParameter("@messageId", messageId));

                                        cmd.ExecuteNonQuery();

                                        message = "SUCCESSFUL-CREATED GROUP CHAT";
                                    }
                                    else
                                    {
                                        message = "FAILED-USER HAS 5(MAX) CHATS";
                                    }
                                    
                                }
                                else
                                {
                                    message = "FAILED";
                                }
                                MyResponse response = new MyResponse()
                                {
                                    Message = message
                                };
                                _formatter.Serialize(stream, response);

                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "ADDNEWMEMBER":
                            {
                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;
                                string password = user.Password;

                                UserModel userAdd = request.FindUser;
                                string usernameAdd = userAdd.Username;

                                if (usernameAdd == TelegramUsername)
                                {
                                    message = "FAILED-NO SUCH USER";
                                    MyResponse responseOk = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, responseOk);
                                    throw new Exception("Can't add #Telegram# to chat");
                                }

                                ChatModel chat = request.Chat;
                                int chatId = chat.Id;

                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password AND Id IN (SELECT UserId FROM [ChatUsers] WHERE UserId=@userId AND ChatId=@chatId)";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));
                                cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if(UserExists == 1)
                                {
                                    query = @"SELECT Id FROM [User] WHERE Username=@usernameAdd";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@usernameAdd", usernameAdd));

                                    if(cmd.ExecuteScalar() != null)
                                    {
                                        int userAddId = (int)cmd.ExecuteScalar();

                                        query = @"SELECT COUNT(*) FROM [ChatUsers] WHERE UserId = @userAddId AND ChatId = @chatId";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@userAddId", userAddId));
                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                        if((int)cmd.ExecuteScalar() == 0)
                                        {
                                            query = @"SELECT IsGroupChat FROM [Chat] WHERE Id = @chatId";
                                            cmd = new SqlCommand(query, _connection);
                                            cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                            if (cmd.ExecuteScalar() != null)
                                            {
                                                if ((int)cmd.ExecuteScalar() == 1)
                                                {
                                                    query = @"INSERT INTO [ChatUsers](UserId, ChatId) VALUES(@userAddId, @chatId)";
                                                    cmd = new SqlCommand(query, _connection);
                                                    cmd.Parameters.Add(new SqlParameter("@userAddId", userAddId));
                                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                                    int rowsAdded = cmd.ExecuteNonQuery();
                                                    if (rowsAdded == 1)
                                                    {
                                                        string messageText = $"{username} added new member -> {usernameAdd}";
                                                        DateTime messageTime = DateTime.Now;

                                                        query = @"INSERT INTO [Message](UserId, MessageText, MessageTime, ImageSource) VALUES(@userId, @messageText, @messageTime, @imageSource); SELECT SCOPE_IDENTITY()";
                                                        cmd = new SqlCommand(query, _connection);
                                                        cmd.Parameters.Add(new SqlParameter("@userId", TelegramId));
                                                        cmd.Parameters.Add(new SqlParameter("@messageText", messageText));
                                                        cmd.Parameters.Add(new SqlParameter("@messageTime", messageTime));
                                                        cmd.Parameters.Add(new SqlParameter("@imageSource", TelegramAvatar));

                                                        int messageId = Convert.ToInt32(cmd.ExecuteScalar());

                                                        query = @"INSERT INTO [ChatMessages](ChatId, MessageId) VALUES(@chatId, @messageId)";
                                                        cmd = new SqlCommand(query, _connection);
                                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                                        cmd.Parameters.Add(new SqlParameter("@messageId", messageId));

                                                        cmd.ExecuteNonQuery();

                                                        message = "SUCCESSFUL-ADDED NEW MEMBER";
                                                    }
                                                }
                                                else
                                                {
                                                    message = "FAILED-NOT GROUP CHAT";
                                                }
                                            }
                                            else
                                            {
                                                message = "FAILED-NO GROUP CHAT";
                                            }
                                        }
                                        else
                                        {
                                            message = "FAILED-USER ALREADY IN GROUP CHAT";
                                        }
                                    }
                                    else
                                    {
                                        message = "FAILED-NO SUCH USER";
                                    }
                                }
                                else
                                {
                                    message = "FAILED";
                                }
                                MyResponse response = new MyResponse()
                                {
                                    Message = message
                                };
                                _formatter.Serialize(stream, response);

                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                        case "DELMEMBER":
                            {
                                UserModel user = request.User;
                                int userId = user.Id;
                                string username = user.Username;
                                string password = user.Password;

                                UserModel userDel = request.FindUser;
                                string usernameDel = userDel.Username;

                                if (usernameDel == TelegramUsername)
                                {
                                    message = "FAILED-NO SUCH USER";
                                    MyResponse responseOk = new MyResponse()
                                    {
                                        Message = message
                                    };
                                    _formatter.Serialize(stream, responseOk);
                                    throw new Exception("Can't del #Telegram# from chat");
                                }

                                ChatModel chat = request.Chat;
                                int chatId = chat.Id;


                                // -> Login and Password check in database ...
                                // -------------------------------------------
                                query = @"SELECT COUNT(*) FROM [User] WHERE Username=@username AND Password=@password AND Id IN (SELECT UserId FROM [ChatUsers] WHERE UserId=@userId AND ChatId=@chatId)";

                                _connection.Open();
                                SqlCommand cmd = new SqlCommand(query, _connection);
                                cmd.Parameters.Add(new SqlParameter("@username", username));
                                cmd.Parameters.Add(new SqlParameter("@password", password));
                                cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                int UserExists = (int)cmd.ExecuteScalar();
                                if(UserExists == 1)
                                {
                                    query = @"SELECT Id FROM [User] WHERE Username=@usernameDel";
                                    cmd = new SqlCommand(query, _connection);
                                    cmd.Parameters.Add(new SqlParameter("@usernameDel", usernameDel));

                                    if (cmd.ExecuteScalar() != null)
                                    {
                                        int userDelId = (int)cmd.ExecuteScalar();

                                        query = @"SELECT COUNT(*) FROM [ChatUsers] WHERE UserId = @userDelId AND ChatId = @chatId";
                                        cmd = new SqlCommand(query, _connection);
                                        cmd.Parameters.Add(new SqlParameter("@userDelId", userDelId));
                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                        if ((int)cmd.ExecuteScalar() == 1)
                                        {
                                            query = @"SELECT IsGroupChat FROM [Chat] WHERE Id = @chatId";
                                            cmd = new SqlCommand(query, _connection);
                                            cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                            if (cmd.ExecuteScalar() != null)
                                            {
                                                if ((int)cmd.ExecuteScalar() == 1)
                                                {
                                                    query = @"SELECT Name FROM [Chat] WHERE Id = @chatId";
                                                    cmd = new SqlCommand(query, _connection);
                                                    cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                                    string chatName = cmd.ExecuteScalar().ToString();

                                                    if(chatName.Contains($"GROUP-{userDelId}"))
                                                    {
                                                        query = @"SELECT COUNT(*) FROM [ChatUsers] WHERE ChatId = @chatId";
                                                        cmd = new SqlCommand(query, _connection);
                                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                                        int usersInChat = (int)cmd.ExecuteScalar();
                                                        if (usersInChat > 1)
                                                        {
                                                            message = "FAILED-CAN'T DELETE CREATOR FIRSTLY DELETE OTHER MEMBERS";
                                                        }
                                                        else if(usersInChat == 1)
                                                        {
                                                            query = @"DELETE FROM [ChatUsers] WHERE UserId = @userId AND ChatId = @chatId; DELETE FROM [ChatMessages] WHERE ChatId = @chatId; DELETE FROM [Chat] WHERE Id = @chatId; ";
                                                            cmd = new SqlCommand(query, _connection);
                                                            cmd.Parameters.Add(new SqlParameter("@userId", userId));
                                                            cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                                            int rowsDeleted = cmd.ExecuteNonQuery();
                                                            if(rowsDeleted > 1) 
                                                            {
                                                                message = "SUCCESSFUL-DELETED GROUP";
                                                            }
                                                            else
                                                            {
                                                                message = "FAILED-NOT DELETED GROUP";
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        query = @"DELETE FROM [ChatUsers] WHERE UserId = @userDelId AND ChatId = @chatId";
                                                        cmd = new SqlCommand(query, _connection);
                                                        cmd.Parameters.Add(new SqlParameter("@userDelId", userDelId));
                                                        cmd.Parameters.Add(new SqlParameter("@chatId", chatId));

                                                        int rowsDeleted = cmd.ExecuteNonQuery();
                                                        if (rowsDeleted == 1)
                                                        {
                                                            string messageText = $"{username} deleted member -> {usernameDel}";
                                                            DateTime messageTime = DateTime.Now;

                                                            query = @"INSERT INTO [Message](UserId, MessageText, MessageTime, ImageSource) VALUES(@userId, @messageText, @messageTime, @imageSource); SELECT SCOPE_IDENTITY()";
                                                            cmd = new SqlCommand(query, _connection);
                                                            cmd.Parameters.Add(new SqlParameter("@userId", TelegramId));
                                                            cmd.Parameters.Add(new SqlParameter("@messageText", messageText));
                                                            cmd.Parameters.Add(new SqlParameter("@messageTime", messageTime));
                                                            cmd.Parameters.Add(new SqlParameter("@imageSource", TelegramAvatar));

                                                            int messageId = Convert.ToInt32(cmd.ExecuteScalar());

                                                            query = @"INSERT INTO [ChatMessages](ChatId, MessageId) VALUES(@chatId, @messageId)";
                                                            cmd = new SqlCommand(query, _connection);
                                                            cmd.Parameters.Add(new SqlParameter("@chatId", chatId));
                                                            cmd.Parameters.Add(new SqlParameter("@messageId", messageId));

                                                            cmd.ExecuteNonQuery();

                                                            message = "SUCCESSFUL-DELETED MEMBER";
                                                        }
                                                        else
                                                        {
                                                            message = "FAILED-NOT DELETED MEMBER";
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    message = "FAILED-NOT GROUP CHAT";
                                                }
                                            }
                                            else
                                            {
                                                message = "FAILED-NO GROUP CHAT";
                                            }
                                        }
                                        else
                                        {
                                            message = "FAILED-USER NOT IN GROUP CHAT";
                                        }
                                    }
                                    else
                                    {
                                        message = "FAILED-NO SUCH USER";
                                    }
                                }
                                else
                                {
                                    message = "FAILED";
                                }
                                MyResponse response = new MyResponse()
                                {
                                    Message = message
                                };
                                _formatter.Serialize(stream, response);

                                WriteMain($"{username} -> {message}");

                                _connection.Close();
                                stream.Close();

                                break;
                            }
                    }
                }
                catch (Exception err)
                {
                    if (_listener.Active)
                    {
                        WriteError($"Expcetion Workloop -> {err.Message}");
                    }
                    else
                    {
                        break;
                    }
                    try
                    {
                        acceptor.GetStream().Close();
                    }
                    catch(Exception err2)
                    {
                        
                    }
                    try
                    {
                        _connection.Close();
                    }
                    catch(Exception err3)
                    {

                    }
                    acceptor.Close();
                }
            }
        }
    }
}
