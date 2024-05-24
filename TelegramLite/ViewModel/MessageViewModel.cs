using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLite.Core;
using TelegramLite.Model;
using TelegramLibrary;

namespace TelegramLite.ViewModel
{
    public class MessageViewModel: ObservableObject
    {
        private MessageModel _message;
        public MessageModel Message
        {
            get => _message;
        }

        public int Id
        {
            get => Message.Id;
            set
            {
                Message.Id = value;
                OnPropertyChanged();
            }
        }

        public int UserId
        {
            get => Message.UserId;
            set
            {
                Message.UserId = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get => Message.Username;
            set
            {
                Message.Username = value;
                OnPropertyChanged();
            }
        }

        public string MessageText
        {
            get => Message.MessageText;
            set
            {
                Message.MessageText = value;
                OnPropertyChanged();
            }
        }

        public DateTime Time
        {
            get => Message.Time;
            set
            {
                Message.Time = value;
                OnPropertyChanged();
            }
        }

        public bool IsGroupMessage
        {
            get => Message.IsGroupMessage;
            set
            {
                Message.IsGroupMessage = value;
                OnPropertyChanged();
            }
        }

        public string TimeHours => Time.ToString("t");

        public byte[] ImageSource
        {
            get => Message.ImageSource;
            set
            {
                Message.ImageSource = value;
                OnPropertyChanged();
            }
        }

        public string Color
        {
            get => Message.Color;
            set
            {
                Message.Color = value;
                OnPropertyChanged();
            }
        }

        public bool UserFirstMessage
        {
            get => Message.UserFirstMessage;
            set
            {
                Message.UserFirstMessage = value;
                OnPropertyChanged();
            }
        }

        public bool YourMessage
        {
            get => Message.YourMessage;
            set
            {
                Message.YourMessage = value;
                OnPropertyChanged();
            }
        }

        public bool FirstMessage
        {
            get => Message.FirstMessage;
            set
            {
                Message.FirstMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsLastMessage
        {
            get => Message.IsLastMessage;
            set
            {
                Message.IsLastMessage = value;
                OnPropertyChanged();
            }
        }

        public MessageViewModel(MessageModel message)
        {
            _message = message;
        }


    }
}
