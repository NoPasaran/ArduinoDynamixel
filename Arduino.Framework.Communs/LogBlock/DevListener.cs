using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Arduino.Framework.Communs.LogBlock
{
    public class DevListener : TraceListener
    {
        public override void Write(string message)
        {            
            
            throw new NotImplementedException();
        }

        public override void WriteLine(string message)
        {
            throw new NotImplementedException();
        }
    }
}
