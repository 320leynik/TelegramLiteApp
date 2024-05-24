using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibrary
{
    [Serializable]
    public class ChatModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WorkingName { get; set; }
        public byte[] ImageSource { get; set; }
        public int IsGroupChat { get; set; }
        public int IsClean { get; set; }

        public List<MessageModel> Messages { get; set; }

        public MessageModel LastMessage { get; set; }
        public DateTime TimeLastMessage { get; set; }
    }
}
