using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibrary
{
    [Serializable]
    public class MessageModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string MessageText { get; set; }
        public DateTime Time { get; set; }
        public bool IsGroupMessage { get; set; }

        public byte[] ImageSource { get; set; }
        public string Color { get; set; }
        public bool UserFirstMessage { get; set; }
        public bool YourMessage { get; set; }
        public bool FirstMessage { get; set; }
        public bool IsLastMessage { get; set; }
    }
}
