using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using TelegramLite.Core;
using TelegramLite.Model;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;
using TelegramLibrary;
using System.IO;
using System.Threading;
using TelegramLite.Views;

namespace TelegramLite.ViewModel  
{
    public class EntryViewModel: ObservableObject
    {
        // Models
        private EntryWindowModel _entry;
        public EntryWindowModel Entry
        {
            get { return _entry; }
        }

        public string Username
        {
            get { return Entry.Username; }
            set
            {
                Entry.Username = value;
                OnPropertyChanged();
            }

        }

        public string PasswordText 
        {
            get { return Entry.PasswordText; }
            set
            {
                if(value.Length <= 30)
                {
                    Entry.PasswordText = value;
                }
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get { return Entry.Email; }
            set
            {
                Entry.Email = value;
                OnPropertyChanged();
            }
        }

        private string _resultText;
        public string ResultText
        {
            get => _resultText;
            set
            {
                _resultText = value;
                OnPropertyChanged();
            }
        }

        // Commands
        //1
        private RelayCommand _startCommand;
        public RelayCommand StartCommand
        {
            get
            {
                return _startCommand ??
                    (_startCommand = new RelayCommand(obj =>
                    {
                        Page page = obj as Page;
                        NavigationService ns = NavigationService.GetNavigationService(page);
                        ns.Navigate(new Uri(@"Views/EntryPages/Page2.xaml", UriKind.RelativeOrAbsolute));
                    }));
            }
        }

        //2
        private RelayCommand _backCommand;
        public RelayCommand BackCommand
        {
            get
            {
                return _backCommand ??
                    (_backCommand = new RelayCommand(obj =>
                    {
                        Page page = obj as Page;
                        NavigationService ns = NavigationService.GetNavigationService(page);
                        ns.Navigate(new Uri(@"Views/EntryPages/Page1.xaml", UriKind.RelativeOrAbsolute));
                    }));
            }
        }

        private RelayCommand _toRegisterCommand;
        public RelayCommand ToRegisterCommand
        {
            get
            {
                return _toRegisterCommand ??
                    (_toRegisterCommand = new RelayCommand(obj =>
                    {
                        Page page = obj as Page;
                        NavigationService ns = NavigationService.GetNavigationService(page);
                        ns.Navigate(new Uri(@"Views/EntryPages/Page3.xaml", UriKind.RelativeOrAbsolute));
                    }));
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand LoginIfPossibleCommand { get; }

        //3
        private RelayCommand _backToLoginCommand;
        public RelayCommand BackToLoginCommand
        {
            get
            {
                return _backToLoginCommand ??
                    (_backToLoginCommand = new RelayCommand(obj =>
                    {
                        Page page = obj as Page;
                        NavigationService ns = NavigationService.GetNavigationService(page);
                        ns.Navigate(new Uri(@"Views/EntryPages/Page2.xaml", UriKind.RelativeOrAbsolute));
                    }));
            }
        }

        // ClientTCP
        // ClientTCP
        private int _port;
        private IPAddress _addr;
        private IPEndPoint _ep;

        private TcpClientNew _client;
        private BinaryFormatter _formatter;

        private static bool checking = true;

        public EntryViewModel()
        {
            ResultText = "";
            _entry = new EntryWindowModel();
            LoginCommand = new RelayCommand(ExecuteLoginCommand, CanExecuteLoginCommand);
            RegisterCommand = new RelayCommand(ExecuteRegisterCommand, CanExecuteRegisterCommand);
            if (checking)
            {
                LoginIfPossibleCommand = new RelayCommand(ExecuteLoginIfPossibleCommand);
            }

            _formatter = new BinaryFormatter();
            _port = 9001;
            _addr = IPAddress.Parse("127.0.0.1");
            _ep = new IPEndPoint(_addr, _port);
        }

        private bool CanExecuteRegisterCommand(object arg)
        {
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3 || string.IsNullOrWhiteSpace(PasswordText) ||
                PasswordText.Length < 3)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool Register()
        {
            bool check = false;
            try
            {
                string Password = Core.SecurityManager.GetMd5Hash(PasswordText);
                MyRequest request = new MyRequest()
                {
                    Header = "AUTHREGISTER",
                    User = new UserModel()
                    {
                        Username = Username,
                        Password = Password,
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL")
                {
                    File.WriteAllText(@"..\..\Data\UserFile.txt", $"{response.User.Username}#{response.User.Password}#{response.User.Name}#{response.User.ImageSource}");
                    ResultText = response.Message;

                    check = true;
                }
                else
                {
                    if(response.Message == "FAILED ALREADY EXISTS")
                    {
                        ResultText = "EXISTS";
                    }
                    else
                    {
                        ResultText = "FAILED";
                    }
                    check = false;
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
            return check;
        }
        private async void ExecuteRegisterCommand(object obj)
        {
            bool result = await Task.Run(() => Register());

            if (result)
            {
                Page page = obj as Page;
                Window window = Window.GetWindow(page);

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                window.Close();
            }
        }

        private async void ExecuteLoginIfPossibleCommand(object obj)
        {
            bool result = await Task.Run(() => LoginIfPossible());

            checking = false;

            if (result)
            {
                Window window = obj as Window;
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

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
                                File.WriteAllText(@"..\..\Data\UserFile.txt", $"{response.User.Username}#{response.User.Password}#{response.User.Name}#{response.User.ImageSource}");

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

        private bool CanExecuteLoginCommand(object arg)
        {
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3 || string.IsNullOrWhiteSpace(PasswordText) ||
                PasswordText.Length < 3)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool Login()
        {
            bool check = false;
            try
            {
                string Password = Core.SecurityManager.GetMd5Hash(PasswordText);
                MyRequest request = new MyRequest()
                {
                    Header = "AUTHLOGIN",
                    User = new UserModel()
                    {
                        Username = Username,
                        Password = Password
                    }
                };

                _client = new TcpClientNew();
                _client.Connect(_ep);
                NetworkStream stream = _client.GetStream();
                _formatter.Serialize(stream, request);

                MyResponse response = (MyResponse)_formatter.Deserialize(stream);
                if (response.Message == "SUCCESSFUL")
                {
                    File.WriteAllText(@"..\..\Data\UserFile.txt", $"{response.User.Username}#{response.User.Password}#{response.User.Name}#{response.User.ImageSource}");

                    check = true;
                }
                else
                {
                    ResultText = "FAILED";
                    check = false;
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
            return check;
        }
        private async void ExecuteLoginCommand(object obj)
        {
            bool result = await Task.Run(() => Login());

            if (result)
            {
                Page page = obj as Page;
                Window window = Window.GetWindow(page);

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                window.Close();
            }
        }
    }
}
