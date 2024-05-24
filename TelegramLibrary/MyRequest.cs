using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibrary
{
    [Serializable]
    public class MyRequest
    {
        public string Header { get; set; }
        public UserModel User { get; set; }
        public UserModel FindUser { get; set; }
        public MessageModel SendMessage { get; set; }
        public ChatModel Chat { get; set; }

    }
}
