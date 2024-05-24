using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;
using TelegramLite.Core;

namespace TelegramLite.ViewModel
{
    public class UserViewModel: ObservableObject
    {
        private UserModel _user;
        public UserModel User
        {
            get => _user;
        }

        public int Id
        {
            get => User.Id;
            set
            {
                User.Id = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get => User.Username;
            set
            {
                User.Username = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => User.Password;
            set
            {
                User.Password = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => User.Name; 
            set
            {
                User.Name = value;
                OnPropertyChanged();
            }
        }

        public byte[] ImageSource
        {
            get => User.ImageSource; 
            set
            {
                User.ImageSource = value;
                OnPropertyChanged();
            }
        }

        public int OfflineOnline
        {
            get => User.OfflineOnline;
            set
            {
                User.OfflineOnline = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastSeen
        {
            get => User.LastSeen;
            set
            {
                User.LastSeen = value;
                OnPropertyChanged();
            }
        }

        public UserViewModel(UserModel user)
        {
            _user = user;
        }
    }
}
