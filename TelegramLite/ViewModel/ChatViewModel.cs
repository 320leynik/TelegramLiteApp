using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using TelegramLite.Core;
using TelegramLite.Model;
using TelegramLibrary;

namespace TelegramLite.ViewModel
{
    public class ChatViewModel: ObservableObject
    {
        private ChatModel _chat;
        public ChatModel Chat
        {
            get => _chat;
        }

        public int Id
        {
            get => Chat.Id;
            set
            {
                Chat.Id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => Chat.Name;
            set
            {
                Chat.Name = value;
                OnPropertyChanged();
            }
        }

        public byte[] ImageSource
        {
            get => Chat.ImageSource;
            set
            {
                Chat.ImageSource = value;
                OnPropertyChanged();
            }
        }

        public string WorkingName
        {
            get => Chat.WorkingName;
            set
            {
                Chat.WorkingName = value;
                OnPropertyChanged();
            }
        }

        public int IsClean
        {
            get => Chat.IsClean;
            set
            {
                Chat.IsClean = value;
                OnPropertyChanged();
            }
        }

        public int IsGroupChat
        {
            get => Chat.IsGroupChat;
            set
            {
                Chat.IsGroupChat = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<MessageModel> Messages
        {
            get
            {
                if (Chat.Messages != null) return new ObservableCollection<MessageModel>(Chat.Messages);
                return new ObservableCollection<MessageModel>();
            }
            set
            {
                Chat.Messages = value.ToList();
                OnPropertyChanged();
            }
        }

        public MessageModel LastMessage
        {
            get
            {
                if(Chat.LastMessage == null) return new MessageModel() { Time=DateTime.Now};
                return Chat.LastMessage;
            }
            set
            {
                Chat.LastMessage = value;
                OnPropertyChanged();
            }
        }
        public DateTime TimeLastMessage
        {
            get => LastMessage.Time;
            set
            {
                LastMessage.Time = value;
                OnPropertyChanged();
            }
        }
        public string DateTimeLastMessageDate
        {
            get
            {
                try
                {
                    DateTime compare1 = DateTime.Now.Date.AddDays(-1);
                    DateTime compare2 = DateTime.Now.Date.AddDays(-7);
                    if (LastMessage.Time.Date >= compare1)
                    {
                        if (DateTime.Now.TimeOfDay.Subtract(LastMessage.Time.TimeOfDay) < TimeSpan.FromHours(18))
                        {
                            return LastMessage.Time.ToString("HH:mm");
                        }
                        else
                        {
                            return LastMessage.Time.ToString("ddd");
                        }
                    }
                    else if (LastMessage.Time.Date >= compare2)
                    {
                        return LastMessage.Time.ToString("ddd");
                    }
                    else
                    {
                        return LastMessage.Time.ToString("d");
                    }
                }
                catch (Exception err)
                {

                }
                return DateTime.Now.ToString();
            }
        }

        public ChatViewModel(ChatModel chat)
        {
            _chat = chat;
        }
    }
}
