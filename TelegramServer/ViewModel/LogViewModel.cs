using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramServer.Core;
using TelegramServer.Model;

namespace TelegramServer.ViewModel
{
    public class LogViewModel: ObservableObject
    {
        private LogModel _log;
        public LogModel Log
        {
            get => _log;
        }

        public DateTime Time
        {
            get => Log.Time;
            set
            {
                Log.Time = value;
                OnPropertyChanged();
            }
        }
        public string TimeStr
        {
            get => Time.ToString("G");
        }

        public string Text
        {
            get => Log.Text;
            set
            {
                Log.Text = value;
                OnPropertyChanged();
            }
        }

        public LogViewModel(LogModel log)
        {
            _log = log;
        }
    }
}
