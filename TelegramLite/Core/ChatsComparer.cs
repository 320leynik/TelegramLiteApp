using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelegramLibrary;

namespace TelegramLite.Core
{
    public static class ChatsComparer
    {
        public static bool IsEquals(List<ChatModel> a, List<ChatModel> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].TimeLastMessage != b[i].TimeLastMessage) return false;
            }
            return true;
        }
        public static bool IsOnlyFirst(List<ChatModel> a, List<ChatModel> b)
        {
            if (a.Count != b.Count) return false;
            if (a.Count > 0 && b.Count > 0)
            {
                if (a[0].TimeLastMessage == b[0].TimeLastMessage) return false;
                for (int i = 1; i < a.Count; i++)
                {
                    if (a[i].TimeLastMessage != b[i].TimeLastMessage) return false;
                }
                return true;
            }
            return false;
            
        }
    }
}
