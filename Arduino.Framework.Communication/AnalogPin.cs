using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino.Framework.Communication
{
    public delegate void RefreshAnalogValue(byte pin, UInt16 value);


    public class AnalogPin
    {
        #region Delegate Event Declaration

        public event RefreshAnalogValue AnalogValueReading;
        public event RefreshAnalogValue AnalogReadError;
        public event RefreshAnalogValue AnalogWritingError;

        #endregion

        private object lockref = new object();

        protected static ServiceFirmata _busarduino;

        protected internal bool _responseWanted = true;  // indiqe si on désire traiter les réponses réceptionnées
        protected internal bool _responseWaited = false; // indique si une réponse est en attente de réception

        private byte _analogPin;

        internal AnalogPin(ServiceFirmata buscom, byte analogpin)
        {
            _busarduino = buscom;
            _analogPin = analogpin;
        }

        #region Declaration Method Event

        protected virtual void OnReadingError(byte pin, UInt16 value)
        {
            RefreshAnalogValue  handler = AnalogReadError;
            if (handler != null)
            {
                handler(pin, value);
            }
        }

        protected virtual void OnValueRefresh(byte pin, UInt16 value)
        {
            RefreshAnalogValue handler = AnalogValueReading;
            if (handler != null)
            {
                handler(pin, value);
            }
        }

        #endregion
        private Arduino.Communication.DataAccess.ArduinoBus.currentAnalogCallback ReadCallback(byte pin)
        {
            return new Arduino.Communication.DataAccess.ArduinoBus.currentAnalogCallback(
                (pinNumber, val) =>
                {
                    OnValueRefresh(pinNumber, val);
                });
        }

        /// <summary>
        /// lecture de la valeur se trouvant sur la broche <paramref name="analogpin"/>
        /// </summary>
        /// <param name="analogpin"></param>
        public void ReadValue()
        {
            lock (lockref)
            {
                if (_responseWanted == true)
                {
                    _busarduino.SendAnalogReadMessage(this._analogPin, this.ReadCallback(this._analogPin));
                }
            }
        }
    }
}
