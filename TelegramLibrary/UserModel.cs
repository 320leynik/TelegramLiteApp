using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLibrary
{
    [Serializable]
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public byte[] ImageSource { get; set; }
        public string Color { get; set; }
        public int OfflineOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
