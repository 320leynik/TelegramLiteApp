using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLite.Model;
using System.Windows;
using TelegramLite.Core;
using System.Runtime.Remoting.Messaging;
using TelegramLite.Net;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Input;
using System.Net.Sockets;
using TelegramLibrary;
using System.IO;
using TelegramLite.Views;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Runtime.CompilerServices;

namespace TelegramLite.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        // Lists for chats and messages
        public List<MessageModel> Messages2 { get; set; }
        public List<ChatModel> Chats2 { get; set; }
        public ObservableCollection<MessageViewModel> Messages { get; set; }
        public ObservableCollection<ChatViewModel> Chats { get; set; }

        // Current User
        private UserViewModel _user;
        public UserViewModel User
        {
            get => _user;
            set
            {
                _user = value;
                OnPropertyChanged();
            }
        }

        // Targeted User(in Chats)
        private UserViewModel _userChat;
        public UserViewModel UserChat
        {
            get => _userChat;
            set
            {
                _userChat = value;
                OnPropertyChanged();
            }
        }

        // Determines if selected chat is Group
        private bool _isGroupChat = false;
        public bool IsGroupChat
        {
            get => _isGroupChat;
            set
            {
                _isGroupChat = value;
                OnPropertyChanged();
            }
        }

        // ViewModels-2
        private bool _isChatOnline = false;
        public bool IsChatOnline
        {
            get => _isChatOnline;
            set
            {
                _isChatOnline = value;
                OnPropertyChanged();
            }
        }
        private string _memberGroupChat = "";
        public string MemberGroupChat
        {
            get => _memberGroupChat;
            set
            {
                _memberGroupChat = value;
                OnPropertyChanged();
            }
        }

        private string _selectedChatLastSeen = "";
        public string SelectedChatLastSeen
        {
            get
            {
                return _selectedChatLastSeen;
            }
            set
            {
                _selectedChatLastSeen = value;
                OnPropertyChanged();
            }
        }

        // Used for AutoSelection when Chats list changed
        private int _selectedChatIdTemp = -1;
        public int SelectedChatIdTemp
        {
            get => _selectedChatIdTemp;
            set
            {
                _selectedChatIdTemp = value;
            }
        }
        private ChatViewModel _selectedChat;
        public ChatViewModel SelectedChat
        {
            get => _selectedChat;
            set
            {
                _selectedChat = value;
                OnPropertyChanged();
            }
        }

        private int _selectedChatIndex;
        public int SelectedChatIndex
        {
            get => _selectedChatIndex;
            set
            {
                _selectedChatIndex = value;
                
                if(_isSearching == true)
                {
                    if(SelectedChatIndex != -1)
                    {
                        SelectedChat = Chats[SelectedChatIndex];
                        IsGroupChat = SelectedChat.IsGroupChat == 1;
                        IsChatSelected = true;
                        UserChat = new UserViewModel(new UserModel());
                        SelectedChatLastSeen = "";
                        Messages.Clear();
                    }
                    else
                    {
                        IsChatSelected = false;
                        SelectedChat = null;
                        _LoadChat = false;
                        IsGroupChat = false;
                    }
                }
                else
                {
                    if (SelectedChatIndex != -1)
                    {
                        if(SelectedChatIdTemp != Chats[SelectedChatIndex].Id)
                        {
                            Message = "";
                        }
                        SelectedChat = Chats[SelectedChatIndex];
                        IsGroupChat = SelectedChat.IsGroupChat == 1;
                        //IsChatSelected = true;
                        ExecuteLoadChatCommand();
                    }
                    else
                    {
                        IsChatSelected = false;
                        SelectedChat = null;
                        _LoadChat = false;
                        IsGroupChat = false;
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "";
                        /*Messages = new ObservableCollection<MessageViewModel>();
                        Messages2 = new List<MessageModel>();
                        PreviousMessages = new List<MessageModel>();*/
                    }
                }
                

                OnPropertyChanged();
            }
        }

        private bool _isChatSelected;
        public bool IsChatSelected
        {
            get => _isChatSelected;
            set
            {
                _isChatSelected = value;
                OnPropertyChanged();
            }

        }

        private bool _openMenu = false;
        public bool OpenMenu
        {
            get => _openMenu;
            set
            {
                _openMenu = value;
                OnPropertyChanged();
            }
        }

        //private List<Tuple<ChatViewModel, string>> _draftMessages;

        private string _message;

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand LoginIfPossibleCommand { get; }
        public ICommand GetChatsCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand OpenMenuCommand { get; }
        public ICommand SearchNewContactCommand { get; set; }
        public ICommand LoadChatCommand { get; set; }
        public ICommand ThreadControllerCommand { get; set; }
        public ICommand ChangeAvatarCommand { get; set; }
        public ICommand ActivateWindowCommand { get; set; }
        public ICommand ClosingWindowCommand { get; set; }
        public ICommand MessagesUpdatedCommand { get; set; }
        public ICommand LogOutCommand { get; set; }
        public ICommand CloseMenuBorderCommand { get; set; }
        public ICommand AddNewMemberCommand { get; set; }
        public ICommand DelMemberCommand { get; set; }
        public ICommand CreateNewGroupCommand { get; set; }

        // ClientTCP
        // ClientTCP
        private int _port;
        private IPAddress _addr;
        private IPEndPoint _ep;

        private TcpClientNew _client;
        private BinaryFormatter _formatter;
        private Thread _clientThreadSendMessage;
        private Thread _clientThreadSearchNewContacts;
        private Thread _clientThreadController;
        private Thread _clientThreadChangeAvatar;
        private Thread _clientThreadMessagesUpdated;
        private Thread _clientThreadAddNewMember;
        private Thread _clientThreadDelMember;
        private Thread _clientThreadCreateGroup;

        private static bool checkUser = true;

        public MainViewModel()
        {
            SearchText = "";
            SelectedChatIndex = -1;
            _isChatSelected = false;
            Chats = new ObservableCollection<ChatViewModel>();
            Messages = new ObservableCollection<MessageViewModel>();
            Messages2 = new List<MessageModel>();
            PreviousMessages = new List<MessageModel>();
            ChatsTemp = new List<ChatModel>();

            LoginIfPossibleCommand = new RelayCommand(ExecuteLoginIfPossibleCommand);
            GetChatsCommand = new RelayCommand(ExecuteGetChatsCommand);
            OpenMenuCommand = new RelayCommand(ExecuteOpenMenuCommand);
            SearchNewContactCommand = new RelayCommand(ExecuteSearchNewContactCommand, CanExecuteSearchNewContactCommand);
            //LoadChatCommand = new RelayCommand(ExecuteLoadChatCommand);
            SendCommand = new RelayCommand(ExecuteSendCommand);
            ThreadControllerCommand = new RelayCommand(ExecuteThreadControllerCommand);
            ChangeAvatarCommand = new RelayCommand(ExecuteChangeAvatarCommand);
            ActivateWindowCommand = new RelayCommand(ExecuteActivateWindowCommand);
            ClosingWindowCommand = new RelayCommand(ExecuteClosingWindowCommand);
            MessagesUpdatedCommand = new RelayCommand(ExecuteMessagesUpdatedCommand);
            LogOutCommand = new RelayCommand(ExecuteLogOutCommand);
            CloseMenuBorderCommand = new RelayCommand(ExecuteCloseMenuBorderCommand);
            AddNewMemberCommand = new RelayCommand(ExecuteAddNewMemberCommand);
            DelMemberCommand = new RelayCommand(ExecuteDelMemberCommand);
            CreateNewGroupCommand = new RelayCommand(ExecuteCreateNewGroupCommand);

            UserChat = new UserViewModel(new UserModel());
            User = new UserViewModel(new UserModel());

            _formatter = new BinaryFormatter();
            _port = 9001;
            _addr = IPAddress.Parse("127.0.0.1");
            _ep = new IPEndPoint(_addr, _port);
        }

        // Create Group Chat
        private void ExecuteCreateNewGroupCommand(object obj)
        {
            _isGetChats = false;
            _LoadChat = false;
            _isSendingMessage = false;

            _clientThreadCreateGroup = new Thread(() => CreateGroup(obj))
            {
                IsBackground = true
            };
            _clientThreadCreateGroup.Start();
        }

        private void CreateGroup(object obj)
        {
            while (!IsThreadControllerFinished) { }
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "CREATENEWGROUP",
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username,
                        Password = User.Password,
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL-CREATED GROUP CHAT")
                {

                }
                else
                {
                    
                }

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _isGetChats = true;
                if (SelectedChatIndex != -1)
                {
                    _LoadChat = true;
                }
            });
        }

        // Member Group Chat
        private string _memberGroupErrorResponse = "";
        public string MemberGroupErrorResponse
        {
            get => _memberGroupErrorResponse;
            set
            {
                _memberGroupErrorResponse = value;
                OnPropertyChanged();
            }
        }
        private string _memberGroupSuccessResponse = "";
        public string MemberGroupSuccessResponse
        {
            get => _memberGroupSuccessResponse;
            set
            {
                _memberGroupSuccessResponse = value;
                OnPropertyChanged();
            }
        }
        private void ExecuteDelMemberCommand(object obj)
        {
            _isGetChats = false;
            _LoadChat = false;
            _isSendingMessage = false;

            _clientThreadDelMember = new Thread(() => DelMember(obj))
            {
                IsBackground = true
            };
            _clientThreadDelMember.Start();
        }

        private void DelMember(object obj)
        {
            while (!IsThreadControllerFinished) { }
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "DELMEMBER",
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username,
                        Password = User.Password,
                    },
                    FindUser = new UserModel()
                    {
                        Username = MemberGroupChat
                    },
                    Chat = new ChatModel()
                    {
                        Id = SelectedChat.Id
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL-DELETED GROUP" || response.Message == "FAILED-NOT DELETED GROUP")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "";
                        SelectedChatIndex = -1;
                        SelectedChatIdTemp = -1;
                    });

                }
                else if (response.Message == "FAILED-CAN'T DELETE CREATOR FIRSTLY DELETE OTHER MEMBERS")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "Creator must be deleted last";
                    });

                }
                else if (response.Message == "SUCCESSFUL-DELETED MEMBER")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "Deleted";
                        MemberGroupErrorResponse = "";
                    });
                }
                else if(response.Message == "FAILED-USER NOT IN GROUP CHAT" || response.Message == "FAILED-NO SUCH USER")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "User is not in group chat";
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "Failed";
                    });
                }

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _isGetChats = true;
                if (SelectedChatIndex != -1)
                {
                    _LoadChat = true;
                }
            });
        }

        private void ExecuteAddNewMemberCommand(object obj)
        {
            _isGetChats = false;
            _LoadChat = false;
            _isSendingMessage = false;

            _clientThreadAddNewMember = new Thread(() => AddNewMember(obj)) 
            { 
                IsBackground = true 
            };
            _clientThreadAddNewMember.Start();
        }

        private void AddNewMember(object obj)
        {
            while (!IsThreadControllerFinished) { }
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "ADDNEWMEMBER",
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username,
                        Password = User.Password,
                    },
                    FindUser = new UserModel()
                    {
                        Username = MemberGroupChat
                    },
                    Chat = new ChatModel()
                    {
                        Id = SelectedChat.Id
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL-ADDED NEW MEMBER")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "Added";
                        MemberGroupErrorResponse = "";
                    });
                }
                else if (response.Message == "FAILED-USER ALREADY IN GROUP CHAT")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "User already in chat";
                    });
                }
                else if (response.Message == "FAILED-NO SUCH USER")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "No User with such nickname";
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        MemberGroupSuccessResponse = "";
                        MemberGroupErrorResponse = "Failed";
                    });
                }

                _client.GetStream().Close();
                _client.Close();
            }
            catch(Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch(Exception err2)
                {

                }
                _client.Close();
            }

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _isGetChats = true;
                if (SelectedChatIndex != -1)
                {
                    _LoadChat = true;
                }
            });
        }

        // Close Menu Border Click
        private void ExecuteCloseMenuBorderCommand(object obj)
        {
            OpenMenu = false;
        }

        // Log Out
        private void ExecuteLogOutCommand(object obj)
        {
            try
            {
                IsThreadController = false;
                SelectedChatIndex = -1;
                SearchText = "";
                _isGetChats = false;
                _isSearching = false;
                _isSendingMessage = false;
                _LoadChat = false;
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err)
                {

                }
                finally
                {
                    try
                    {
                        _client.Close();
                    }
                    catch (Exception err)
                    {

                    }
                }
                UserActivity = 0;
                LastSeenUser = DateTime.Now;
                ChangeActivity();
                File.Create(@"..\..\Data\UserFile.txt").Close();
                ExecuteLoginIfPossibleCommand(obj);
            }
            catch(Exception err)
            {

            }
        }

        // When Updating Scroll Down
        private bool UpdatingScrollDown = false;
        private void ExecuteMessagesUpdatedCommand(object obj)
        {
            if (SelectedChatIndex != -1 && SelectedChat.IsClean == 0 && SelectedChatIdTemp != Chats[SelectedChatIndex].Id)
            {
                IsChatSelected = false;
                UpdatingScrollDown = true;
                LoadChatChekingMessagesAddingFinished = false;
                _clientThreadMessagesUpdated = new Thread(() => MessagesUpdated(obj))
                {
                    IsBackground = true
                };
                _clientThreadMessagesUpdated.Start();
            }
        }

        private void MessagesUpdated(object obj)
        {
            while (!LoadChatChekingMessagesAddingFinished)
            {

            }

            double vo = 0;
            double sh = 1;
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                System.Windows.Controls.ListView messages = obj as System.Windows.Controls.ListView;
                messages.ScrollIntoView(Messages.Last());
                ScrollViewer sv = ViewsWork.GetChildOfType<ScrollViewer>(messages);
                vo = sv.VerticalOffset;
                sh = sv.ScrollableHeight;
            });
            
            while(vo != sh)
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    System.Windows.Controls.ListView messages = obj as System.Windows.Controls.ListView;
                    ScrollViewer sv = ViewsWork.GetChildOfType<ScrollViewer>(messages);
                    vo = sv.VerticalOffset;
                    sh = sv.ScrollableHeight;
                });
            }
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                IsChatSelected = true;
            });
            UpdatingScrollDown = false;
        }

        // Closing Window
        private void ExecuteClosingWindowCommand(object obj)
        {
            IsThreadController = false;
            SelectedChatIndex = -1;
            SearchText = "";
            _isGetChats = false;
            _isSearching = false;
            _isSendingMessage = false;
            _LoadChat = false;
            try
            {
                _client.GetStream().Close();
            }
            catch (Exception err)
            {

            }
            finally
            {
                try
                {
                    _client.Close();
                }
                catch(Exception err)
                {

                }
            }
            UserActivity = 0;
            LastSeenUser = DateTime.Now;
            ChangeActivity();
        }

        // Thread Controller
        private bool IsThreadController = true;
        private bool IsThreadControllerFinished = true;
        private void ExecuteThreadControllerCommand(object obj)
        {
            _clientThreadController = new Thread(() => ThreadController(obj))
            {
                IsBackground = true
            };
            _clientThreadController.Start();
        }

        private bool ChangingActivity = false;
        public bool AddedNewMessage = false;
        private void ThreadController(object obj)
        {
            LoginIfPossibleCommand.Execute(obj);
            while (IsThreadController)
            {
                IsThreadControllerFinished = false;
                try
                {
                    if (ShouldChangeActivity && !ChangingActivity)
                    {
                        ChangingActivity = true;
                        ShouldChangeActivity = false;
                        ChangeActivity();
                    }
                    if (_isGetChats)
                    {
                        GetChatsLoop();
                    }
                    if (_isSendingMessage)
                    {
                        if(Message.Length > 0)
                        {
                            AddNewMessage();
                            AddedNewMessage = true;
                        }
                        _isSendingMessage = false;
                    }
                    if (_LoadChat)
                    {
                        if(SelectedChatIndex != -1)
                        {
                            LoadChat();
                        }
                    }
                    
                }
                catch (Exception err)
                {
                    try
                    {
                        _client.GetStream().Close();
                    }
                    catch (Exception err2)
                    {

                    }
                    _client.Close();
                }
                if (IsThreadController == false)
                {
                    Thread.Sleep(100);
                }
                IsThreadControllerFinished = true;
                Thread.Sleep(100);
            }
        }

        // Activity of user
        private int UserActivity = 0;
        private DateTime LastSeenUser;
        private bool ShouldChangeActivity = false;
        private void ExecuteActivateWindowCommand(object obj)
        {
            if (UserActivity == 0)
            {
                UserActivity = 1;
                LastSeenUser = DateTime.Now;
                ShouldChangeActivity = true;

            }
            else if (UserActivity == 1)
            {
                UserActivity = 0;
                LastSeenUser = DateTime.Now;
                ShouldChangeActivity = true;
            }
        }

        private void ChangeActivity()
        {
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "CHANGEACTIVITY",
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username,
                        Password = User.Password,
                        OfflineOnline = UserActivity,
                        LastSeen = LastSeenUser
                    },
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
            finally
            {
                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    if(IsSearching == false)
                    {
                        _isGetChats = true;
                    }
                    User.OfflineOnline = UserActivity;
                    User.LastSeen = LastSeenUser;
                }));
                ChangingActivity = false;
            }
        }

        // Change Avatar
        private byte[] CurrentImageSource;
        private void ExecuteChangeAvatarCommand(object obj)
        {
            OpenFileDialog op = new OpenFileDialog
            {
                Title = "Select your avatar",
                Filter = "Image Files|*.jpg;*.jpeg;*.png",
                Multiselect = false
            };

            if (op.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(op.FileName, FileMode.Open, FileAccess.Read);
                CurrentImageSource = new byte[fs.Length];
                fs.Read(CurrentImageSource, 0, Convert.ToInt32(fs.Length));
                fs.Close();
            }
            if(CurrentImageSource != User.ImageSource)
            {
                _isGetChats = false;
                IsSearching = false;
                _LoadChat = false;
                SelectedChatIndex = -1;
                SearchText = "";
                _clientThreadChangeAvatar = new Thread(() => ChangeAvatar(obj))
                {
                    IsBackground = true
                };
                _clientThreadChangeAvatar.Start();
            }
        }

        private void ChangeAvatar(object obj)
        {
            while (!IsThreadControllerFinished) { }
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "CHANGEAVATAR",
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username,
                        Password = User.Password,
                        ImageSource = CurrentImageSource
                    },
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
            finally
            {
                App.Current.Dispatcher.Invoke(new Action(() =>
                {
                    _isGetChats = true;
                    IsSearching = false;
                    _LoadChat = false;
                    SelectedChatIndex = -1;
                    SearchText = "";
                    User.ImageSource = CurrentImageSource;
                }));
            }
        }

        // Send Message
        private bool _isCreatingGroupChat = false;
        private bool _isSendingMessage = false;
        private void ExecuteSendCommand(object obj)
        {
            if (_isGetChats == false && SelectedChat.IsClean == 1 && _isCreatingGroupChat == false)
            {
                _clientThreadSendMessage = new Thread(() => AddNewMessageNewContact(obj))
                {
                    IsBackground = true
                };
                _clientThreadSendMessage.Start();
            }
            else if (_isGetChats == true && UserChat != null)
            {
                _isSendingMessage = true;
            }
        }

        private void AddNewMessage()
        {
            try
            {
                int selectedChatId = SelectedChat.Id;
                MyRequest request = new MyRequest()
                {
                    Header = "SENDMESSAGE",
                    Chat = new ChatModel()
                    {
                        Id = SelectedChat.Id
                    },
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username
                    },
                    SendMessage = new MessageModel()
                    {
                        UserId = User.Id,
                        MessageText = Message,
                        Time = DateTime.Now,
                        ImageSource = User.ImageSource
                    }
                };
                Message = "";
                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL-SEND NEWMESSAGE")
                {

                }
                else
                {

                }

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
        }

        private void AddNewMessageNewContact(object obj)
        {
            try
            {
                int selectedChatId = SelectedChat.Id;
                MyRequest request = new MyRequest()
                {
                    Header = "SENDMESSAGENEWCONTACT",
                    Chat = new ChatModel()
                    {
                        Name = SelectedChat.Name,
                        IsGroupChat = 0,
                        WorkingName = SelectedChat.WorkingName
                    },
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username
                    },
                    SendMessage = new MessageModel()
                    {
                        UserId = User.Id,
                        MessageText = Message,
                        Time = DateTime.Now,
                        ImageSource = User.ImageSource
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL-SEND NEWMESSAGE NEWCONTACT")
                {
                    _client.GetStream().Close();
                    _client.Close();
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        SearchNewContactCommand.Execute(obj);
                        
                        SelectedChatIdTemp = -1;
                        SelectedChatIndex = -1;
                    });
                }
                else
                {
                    _client.GetStream().Close();
                    _client.Close();
                }
                
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
        }

        private void RefreshMessages()
        {
            if (Messages.Count > 0)
            {
                Messages[0].FirstMessage = true;
                Messages[0].UserFirstMessage = true;
                if(Messages.Count > 1)
                {
                    if (Messages[0].UserId != Messages[1].UserId)
                    {
                        Messages[0].IsLastMessage = true;
                        Messages[1].UserFirstMessage = true;
                        Messages[1].FirstMessage = true;
                    }
                    if ((Messages[0].Time - Messages[1].Time).TotalMinutes >= 5)
                    {
                        Messages[0].IsLastMessage = true;
                        Messages[1].FirstMessage = true;
                        Messages[1].UserFirstMessage = true;
                    }
                }
                for (int i = 1; i < Messages.Count - 1; i++)
                {
                    if (Messages[i].UserId == Messages[i - 1].UserId && Messages[i].UserId == Messages[i + 1].UserId) // y-y-y
                    {
                        Messages[i].IsLastMessage = false;
                        Messages[i].FirstMessage = false;
                        Messages[i].UserFirstMessage = false;

                        if ((Messages[i].Time - Messages[i - 1].Time).TotalMinutes >= 5)
                        {
                            Messages[i - 1].IsLastMessage = true;
                            Messages[i].FirstMessage = true;
                            Messages[i].UserFirstMessage = true;
                        }
                    }
                    else if (Messages[i].UserId == Messages[i - 1].UserId && Messages[i].UserId != Messages[i + 1].UserId) // y-y-n
                    {
                        Messages[i].IsLastMessage = true;
                        Messages[i].FirstMessage = false;
                        Messages[i].UserFirstMessage = false;

                        if ((Messages[i].Time - Messages[i - 1].Time).TotalMinutes >= 5)
                        {
                            Messages[i - 1].IsLastMessage = true;
                            Messages[i].FirstMessage = true;
                            Messages[i].UserFirstMessage = true;
                        }
                    }
                    else if (Messages[i].UserId != Messages[i - 1].UserId && Messages[i].UserId == Messages[i + 1].UserId) // n-y-y
                    {
                        Messages[i].IsLastMessage = false;
                        Messages[i].FirstMessage = true;
                        Messages[i].UserFirstMessage = true;
                    }
                    else if (Messages[i].UserId != Messages[i - 1].UserId && Messages[i].UserId != Messages[i + 1].UserId) // n-y-n
                    {
                        Messages[i].IsLastMessage = true;
                        Messages[i].FirstMessage = true;
                        Messages[i].UserFirstMessage = true;
                    }
                }
                if(Messages.Count > 1)
                {
                    if(SelectedChat != null)
                    {
                        if (Messages[Messages.Count-2].UserId != Messages[Messages.Count-1].UserId)
                        {
                            Messages[Messages.Count- 2].IsLastMessage = true;
                            Messages[Messages.Count - 1].FirstMessage = true;
                            Messages[Messages.Count - 1].UserFirstMessage = true;
                        }
                    }
                    
                    if ((Messages[Messages.Count-1].Time - Messages[Messages.Count-2].Time).TotalMinutes >= 5)
                    {
                        Messages[Messages.Count-2].IsLastMessage = true;
                        Messages[Messages.Count-1].FirstMessage = true;
                        Messages[Messages.Count-1].UserFirstMessage = true;
                    }
                }

                Messages[Messages.Count - 1].IsLastMessage = true;
            }
        }

        // Load Chat
        private bool _LoadChat = false;
        public List<MessageModel> MessagesTemp { get; set; }
        public List<MessageModel> PreviousMessages { get; set; }
        private void ExecuteLoadChatCommand() 
        {
            if (SelectedChat.IsClean == 0)
            {
                _LoadChat = true;
            }
        }

        public bool LoadChatChekingMessagesAddingFinished = false;

        private void LoadChat()
        {
            try
            {
                if(SelectedChat.Id != SelectedChatIdTemp)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Messages.Clear();
                        PreviousMessages.Clear();
                    });
                    SelectedChatIdTemp = SelectedChat.Id;
                }
                MyRequest request = new MyRequest()
                {
                    Header = "LOADCHAT",
                    Chat = new ChatModel()
                    {
                        Id = SelectedChat.Id
                    },
                    User = new UserModel()
                    {
                        Id = User.Id,
                        Username = User.Username
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL-LOADPAIRCHAT")
                {
                    Messages2 = response.Messages;
                    MessagesTemp = Messages2.Concat(PreviousMessages).Except(Messages2.Intersect(PreviousMessages, new MessageComparer()), new MessageComparer()).ToList();
                    PreviousMessages = Messages2;
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        UserChat = new UserViewModel(response.User);
                    });
                    foreach (MessageModel message in MessagesTemp)
                    {
                        if (message.UserId == User.Id) message.YourMessage = true;
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Messages.Add(new MessageViewModel(message));
                        });
                    }
                    LoadChatChekingMessagesAddingFinished = true;
                    RefreshMessages();


                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (SelectedChatIndex != -1 && _isSearching == false)
                        {
                            if (UserChat.OfflineOnline == 1)
                            {
                                SelectedChatLastSeen = "Online";
                                IsChatOnline = true;
                            }
                            else
                            {
                                IsChatOnline = false;
                                TimeSpan ts = DateTime.Now - UserChat.LastSeen;
                                if (ts.TotalSeconds < 60) SelectedChatLastSeen = "Last seen just now";
                                else if (ts.TotalMinutes >= 1.0 && ts.TotalMinutes < 2.0) SelectedChatLastSeen = "Last seen 1 minute ago";
                                else if (ts.TotalMinutes >= 2.0 && ts.TotalMinutes < 60.0) SelectedChatLastSeen = $"Last seen {(int)ts.TotalMinutes} minutes ago";
                                else if (ts.TotalHours >= 1.0 && ts.TotalHours < 2.0) SelectedChatLastSeen = "Last seen 1 hour ago";
                                else if (ts.TotalHours >= 2.0 && ts.TotalHours < 24.0) SelectedChatLastSeen = $"Last seen {(int)ts.TotalHours} hours ago";
                                else if (ts.TotalDays >= 1.0 && ts.TotalDays < 2.0) SelectedChatLastSeen = "Last seen 1 day ago";
                                else if (ts.TotalDays >= 2.0 && ts.TotalDays < 29.0) SelectedChatLastSeen = $"Last seen {(int)ts.TotalDays} days ago";
                                else SelectedChatLastSeen = $"Last seen month or more ago";
                            }
                        }
                        else SelectedChatLastSeen = "";
                    });
                }
                else if (response.Message == "SUCCESSFUL-LOADGROUPCHAT")
                {
                    Messages2 = response.Messages;
                    for(int i = 0; i<Messages2.Count; i++)
                    {
                        Messages2[i].IsGroupMessage = true;
                    }
                    MessagesTemp = Messages2.Concat(PreviousMessages).Except(Messages2.Intersect(PreviousMessages, new MessageComparer()), new MessageComparer()).ToList();
                    PreviousMessages = Messages2;
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        UserChat = new UserViewModel(new UserModel()
                        {
                            Id = -1,
                            Username = "#GroupChat",
                            Name = "Group Chat",
                            ImageSource = SelectedChat.ImageSource,
                            OfflineOnline = 2,
                            LastSeen = DateTime.Now
                        });
                    });

                    UserModel members = response.User;
                    int membersCount = members.Id;
                    foreach (MessageModel message in MessagesTemp)
                    {
                        if (message.UserId == User.Id) message.YourMessage = true;
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Messages.Add(new MessageViewModel(message));
                        });
                    }
                    LoadChatChekingMessagesAddingFinished = true;
                    RefreshMessages();

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (SelectedChatIndex != -1 && _isSearching == false)
                        {
                            if (UserChat.OfflineOnline == 2)
                            {
                                if (membersCount == 1) SelectedChatLastSeen = $"Group chat of {membersCount} member";
                                else SelectedChatLastSeen = $"Group chat of {membersCount} members";
                            }
                        }
                        else SelectedChatLastSeen = "";
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Messages.Clear();
                    });
                }

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
        }

        // Search
        private bool _isGetChats = true;
        private bool _isSearching = false;
        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                _isSearching = value;
                OnPropertyChanged();
            }
        }
        private bool CanExecuteSearchNewContactCommand(object arg)
        {
            if (SearchText.Length < 40)
            {
                return true;
            }
            return false;
        }

        private void ExecuteSearchNewContactCommand(object obj)
        {
            if (SearchText.Length > 0 && !IsSearching)
            {
                _isGetChats = false;
                _LoadChat = false;
                IsSearching = true;
                SelectedChatIndex = -1;
                _clientThreadSearchNewContacts = new Thread(() => SearchNewContact(obj))
                {
                    IsBackground = true
                };
                _clientThreadSearchNewContacts.Start();
            }
           else
            {
                if (_isGetChats == false)
                {
                    _isGetChats = true;
                    _LoadChat = false;
                    IsSearching = false;
                    SelectedChatIndex = -1;
                    SearchText = "";
                    Message = "";
                }
                
            }


        }

        private void SearchNewContact(object obj)
        {
            while (!IsThreadControllerFinished) { }
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "FINDNEWCONTACT",
                    User = new UserModel()
                    {
                        Username = User.Username,
                        Password = User.Password
                    },
                    FindUser = new UserModel()
                    {
                        Username = SearchText
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL")
                {
                    ChatsTemp = response.Chats;

                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Chats.Clear();
                    });
                    foreach (ChatModel chat in ChatsTemp)
                    {
                        App.Current.Dispatcher.Invoke((Action)delegate
                        {
                            Chats.Add(new ChatViewModel(chat));
                        });
                    }
                }
                else if (response.Message == "SUCCESS NO USERS" || response.Message == "SUCCESS NO USERNAMES")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Chats.Clear();
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Chats.Clear();
                    });
                }

                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
        }

        // Menu
        private void ExecuteOpenMenuCommand(object obj)
        {
            if (OpenMenu) OpenMenu = false;
            else OpenMenu = true;
        }

        // Getting chats
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(true);
        public List<ChatModel> ChatsTemp { get; set; }
        private void ExecuteGetChatsCommand(object obj)
        {
            
        }

        private void GetChatsLoop()
        {
            try
            {
                MyRequest request = new MyRequest()
                {
                    Header = "CHATSLOAD",
                    User = new UserModel()
                    {
                        Username = User.Username,
                        Password = User.Password
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL")
                {
                    Chats2 = response.Chats;
                    Chats2 = Chats2.OrderByDescending(o => o.TimeLastMessage).ToList();
                    if (!ChatsComparer.IsEquals(ChatsTemp, Chats2))
                    {
                        if(ChatsComparer.IsOnlyFirst(ChatsTemp, Chats2))
                        {
                            App.Current.Dispatcher.Invoke((Action)delegate
                            {
                                Chats[0] = new ChatViewModel(Chats2[0]);
                                SelectedChatIndex = Chats.ToList().FindIndex(o => o.Id == SelectedChatIdTemp);
                                if(SelectedChatIndex != -1)
                                {
                                    IsChatSelected = true;
                                }
                            });
                        }
                        else
                        {
                            App.Current.Dispatcher.Invoke((Action)delegate
                            {
                                Chats.Clear();
                                foreach(ChatModel chat in Chats2)
                                {
                                    Chats.Add(new ChatViewModel(chat));
                                }
                                SelectedChatIndex = Chats.ToList().FindIndex(o => o.Id == SelectedChatIdTemp);
                                if(SelectedChatIndex != -1)
                                {
                                    IsChatSelected = true;
                                    SelectedChatIdTemp = Chats[SelectedChatIndex].Id;
                                }

                            });
                        }
                    }
                    else
                    {
                        if(Chats.Count < Chats2.Count)
                        {
                            App.Current.Dispatcher.Invoke((Action)delegate
                            {
                                Chats.Clear();
                                foreach (ChatModel chat in Chats2)
                                {
                                    Chats.Add(new ChatViewModel(chat));
                                }
                                SelectedChatIndex = Chats.ToList().FindIndex(o => o.Id == SelectedChatIdTemp);
                                if(SelectedChatIndex != -1)
                                {
                                    IsChatSelected = true;
                                    SelectedChatIdTemp = Chats[SelectedChatIndex].Id;
                                }
                            });
                        }
                    }
                    ChatsTemp = Chats2;
                }
                else if (response.Message == "SUCCESS NO CHATS")
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Chats.Clear();
                    });
                }
                else
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Chats.Clear();
                    });
                }
                _client.GetStream().Close();
                _client.Close();
            }
            catch (Exception err)
            {
                try
                {
                    _client.GetStream().Close();
                }
                catch (Exception err2)
                {

                }
                _client.Close();
            }
        }

        private void ExecuteLoginIfPossibleCommand(object obj)
        {
            checkUser = LoginIfPossible();

            if (!checkUser)
            {
                Window window = obj as Window;
                EntryWindow entryWindow = new EntryWindow();
                entryWindow.Show();

                window.Close();
            }
        }
        private bool LoginIfPossible()
        {
            if (File.Exists(@"..\..\Data\UserFile.txt"))
            {
                bool check = false;
                try
                {
                    var UserDataFile = File.ReadLines(@"..\..\Data\UserFile.txt").ToList();
                    string FirstLine = "#";
                    if (UserDataFile.Count > 0)
                    {
                        FirstLine = UserDataFile[0];
                    }

                    List<string> UserData = FirstLine.Split('#').ToList();
                    if (UserData.Count > 1)
                    {
                        if (!String.IsNullOrWhiteSpace(UserData[0]) && !String.IsNullOrWhiteSpace(UserData[1]))
                        {
                            MyRequest request = new MyRequest()
                            {
                                Header = "AUTHLOGIN",
                                User = new UserModel()
                                {
                                    Username = UserData[0],
                                    Password = UserData[1]
                                }
                            };

                            _client = new TcpClientNew();
                            _client.Connect(_ep);
                            NetworkStream stream = _client.GetStream();
                            _formatter.Serialize(stream, request);

                            MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                            if (response.Message == "SUCCESSFUL")
                            {
                                App.Current.Dispatcher.Invoke((Action)delegate
                                {
                                    CurrentImageSource = response.User.ImageSource;
                                    User = new UserViewModel(new UserModel()
                                    {
                                        Id = response.User.Id,
                                        Username = response.User.Username,
                                        Password = response.User.Password,
                                        Name = response.User.Name,
                                        ImageSource = response.User.ImageSource,
                                        Color = response.User.Color
                                    });
                                });
                                check = true;
                            }

                            _client.GetStream().Close();
                            _client.Close();
                        }
                        else
                        {
                            File.WriteAllText(@"..\..\Data\UserFile.txt", "#");
                        }
                    }
                }
                catch (Exception err)
                {
                    try
                    {
                        _client.GetStream().Close();
                    }
                    catch (Exception err2)
                    {

                    }
                    _client.Close();
                }
                return check;
            }
            else
            {
                File.Create(@"..\..\Data\UserFile.txt");
                File.WriteAllText(@"..\..\Data\UserFile.txt", "#");
                return false;
            }
        }

        /*private ObservableCollection<MessageModel> ReturnModelMessages(ObservableCollection<MessageViewModel> messages)
        {
            
        }*/
    }
}
