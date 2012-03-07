using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino.Framework.Communication
{
    public abstract class Message
    {
        private byte[] datas;

        public Message(byte length)
        {
            datas = new byte[length];
        }
        
    }
}
