using System;
using System.Collections.Generic;

namespace Arduino.Framework.Communs.Entities
{
    public class Dynamixel
    {
        public Dynamixel(UInt16 model, byte id,  UInt16 cwanglelimit, UInt16 ccwanglelimit)
        {
            this.Id = id;
            this.Model = model;
            this.CWAngleLimit = cwanglelimit;
            this.CCWAngleLimit= ccwanglelimit;
        }

        public byte Id { get; private set; }
        
        public UInt16 Model { get; private set; }

        public UInt16 CWAngleLimit { get; set; }

        public UInt16 CCWAngleLimit { get; set; }
    }
}

