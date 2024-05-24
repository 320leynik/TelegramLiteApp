using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;

namespace TelegramLite.Core
{
    public class MessageComparer: IEqualityComparer<MessageModel>
    {
        public bool Equals(MessageModel x, MessageModel y)
        {
            if (x.Id == y.Id) return true;
            return false;
        }

        public int GetHashCode(MessageModel obj)
        {
            if (obj == null) return 0;
            return obj.Id.GetHashCode();
        }
    }
}
