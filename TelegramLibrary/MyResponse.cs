using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibrary
{
    [Serializable]
    public class MyResponse
    {
        public string Message { get; set; }
        public UserModel User { get; set; }
        public List<ChatModel> Chats { get; set; }
        public List<MessageModel> Messages { get; set; }
    }
}
