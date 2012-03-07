using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino.Framework.Communication
{
    public delegate void RefreshValue(AX12RegisterAdd address, UInt16 value);

    public class DynamixelFactory
    {
        private static DynamixelFactory _internalRef;
        private ServiceFirmata _busCommunication;
        private static bool _waitResponse = false;

        public static DynamixelFactory GetInstance()
        {
            if (_internalRef == null)
                _internalRef = new DynamixelFactory();
            return _internalRef;
        }

        public ServiceFirmata BusCommunication { private get { return _busCommunication; } set { _busCommunication = value; } }

        public bool IsCorrectInitialized { get { return this.BusCommunication != null ? true : false; } }


        public bool Ping(byte identifiant)
        {
            bool result = false;
            _waitResponse = true;

            _busCommunication.SendPingInstructionMessage(identifiant, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    result = true;
                    _waitResponse = false;

                }));
            DateTime posted = DateTime.Now;
            while (_waitResponse == true && (DateTime.Now - posted).Ticks < 500000) ;
            return result;
        }

    }
    
    public abstract class AXDynamixel
    {
        #region Delegate Event Declaration
        
        public event RefreshValue ValueRefresh;
        public event RefreshValue ReadError;
        public event RefreshValue WritingError;
        
        #endregion

        private object lockref = new object();

        protected static ServiceFirmata _busarduino;

        protected internal byte _identifiant_dynamixel;
        protected internal bool _responseWanted = true;  // indique si on désire traiter les réponses réceptionnées.
        protected internal bool _responseWaited = false; // indique si une réponse est en attente de réception
    
        protected internal AXDynamixel(ServiceFirmata buscom, byte identifiant_dynamixel)
        {
            _busarduino = buscom;
            _identifiant_dynamixel = identifiant_dynamixel;
        }

        public byte ID { get { return _identifiant_dynamixel; } }

        public static bool Ping(byte identifiant)
        {
            bool result = false;
            bool responseWaited = true;
            _busarduino.SendPingInstructionMessage(identifiant, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    responseWaited = false;
                    result = true;
                }));
            while (responseWaited == true) ;
            return result;
        }

        public bool ResponseWanted { get { return _responseWanted; } set { _responseWanted = value; } }

        private Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback WriteCallback(AX12RegisterAdd add)
        {
            return new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] != 0)
                    {
                        OnWritingError(add, y[2]);
                    }
                });
        }

        protected internal void GetXVal(AX12RegisterAdd add, Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback callback)
        {
            lock (this.lockref)
            {
                _busarduino.SendReadInstructionMessage(_identifiant_dynamixel, _busarduino.GetAX12Register(add), 0, callback);
            }
        }

        protected internal void SetXVal(AX12RegisterAdd add, byte val, Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback callback)
        {
            lock (lockref)
            {
                if (callback == null)
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, _busarduino.GetAX12Register(add), new byte[] { val });
                }
                else
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, _busarduino.GetAX12Register(add), new byte[] { val }, callback);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="add"></param>
        /// <param name="val"></param>
        /// <param name="callback"></param>
        protected internal void SetXVal(AX12RegisterAdd add, int val, Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback callback)
        {
            lock (this.lockref)
            {
                if (_re == null)
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, _busarduino.GetAX12Register(add), new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) }, callback);
                }
                else
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, _busarduino.GetAX12Register(add), new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) });
                }
            }
        }

        public void SetID(byte identifiant)
        {
            if (!this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.ID, identifiant, null);
            }
            else
            {
                SetXVal(AX12RegisterAdd.ID, identifiant, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                         (x, y) =>
                         {
                             if (y[2] != 0)
                             {
                                 if (WritingError != null)
                                 {
                                     OnWritingError(AX12RegisterAdd.ID, y[2]);
                                 }
                             }
                         }));
            }
        }

        /// <summary>
        /// Obtient le model de l'actuator
        /// </summary>
        public void GetModel()
        {
            GetXVal(AX12RegisterAdd.ModelNumber, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] == 0)
                        {
                            if (ValueRefresh != null)
                                OnValueRefresh (AX12RegisterAdd.ModelNumber, (ushort)((y[4] << 8) + y[3]));
                        }
                        else if (ValueRefresh != null)
                        {
                            OnReadError(AX12RegisterAdd.ModelNumber, y[2]);
                        }
                    }));
        }

        #region Method Method Declaration
        
        protected virtual void OnReadError(AX12RegisterAdd add, UInt16 value)
        {
            // Une copie temporaire est effectuée afin d'éviter une "race condition" si le dernier abonné se désabonne 
            // immédiatement après le control du null et avant levé de l'évènement
            RefreshValue handler = ReadError;
            if (handler != null)
            {
                handler(add, value);
            }
        }

        protected virtual void OnWritingError(AX12RegisterAdd add, UInt16 value)
        {
            // Une copie temporaire est effectuée afin d'éviter une "race condition" si le dernier abonné se désabonne 
            // immédiatement après le control du null et avant levé de l'évènement
            RefreshValue handler = WritingError;
            if (handler != null)
            {
                handler(add, value);
            }
        }

        protected virtual void OnValueRefresh(AX12RegisterAdd add, UInt16 value)
        {
            // Une copie temporaire est effectuée afin d'éviter une "race condition" si le dernier abonné se désabonne 
            // immédiatement après le control du null et avant levé de l'évènement
            RefreshValue handler = ValueRefresh;
            if (handler != null)
            {
                handler(add, value);
            }
        }
        
        #endregion

    }

    public class AX12Dynamixel : AXDynamixel
    {

        internal AX12Dynamixel(ServiceFirmata buscom, byte identifiant_dynamixel):base(buscom,identifiant_dynamixel)
        {
        }


        #region Override AXDynamixel
        
        protected override void OnReadError(AX12RegisterAdd add, ushort value)
        {
            // Do any cirle-specific processing here

            // call the base class event invocation method
            base.OnReadError(add, value);
        }

        protected override void OnValueRefresh(AX12RegisterAdd add, ushort value)
        {
            // Do any circle-specfic processing here
            

            // call the base class event invocation method
            base.OnValueRefresh(add, value);
        }

        protected override void OnWritingError(AX12RegisterAdd add, ushort value)
        {
            // Do any circle-specific processing here

            // call the base class event invocation method
            base.OnWritingError(add, value);
        }

        #endregion



        /// <summary>
        /// Obtenir la valeur de la propriété TorqueEnable.
        /// </summary>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item>Si la lecture se passe correctement OnRefreshValue contient la propriété et la valeur</item>
        ///         <item>Si Une erreur est retournée alors OnReadError contient la propriété et la valeur de l'erreur</item>
        ///     </list>
        /// </remarks>
        public void GetTorqueEnable()
        {
            GetXVal(AX12RegisterAdd.TorqueEnable,
                  new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y.Length == 7)
                        {
                            if (y[2] == 0)
                            {
                                if (OnRefreshValue != null)
                                {
                                    OnRefreshValue(AX12RegisterAdd.TorqueEnable, y[3]);
                                }
                            }
                            else if (OnReadError != null)
                            {
                                OnReadError(AX12RegisterAdd.TorqueEnable, y[2]);
                            }
                        }
                    }));
        }

        /// <summary>
        /// Définit la valeur TorqueEnable
        /// </summary>
        /// <param name="status"></param>
        /// <remarks>
        ///     Si une erreur survient alors OnOnWritingError contient le code erreur
        /// </remarks>
        public void SetTorqueEnable(bool status)
        {
            if (!this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.TorqueEnable, (byte)(status == true ? 1 : 0),null);
            }
            else
            {
                SetXVal(AX12RegisterAdd.TorqueEnable, (byte)(status == true ? 1 : 0), new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                        (x, y) =>
                        {
                            if (y[2] != 0)
                            {
                                if (OnWritingError != null)
                                {
                                    OnWritingError(AX12RegisterAdd.TorqueEnable, y[2]);
                                }
                            }

                        }));
            }
        }

        /// <summary>
        /// Obtient la version du firmware qui est installée au niveau de l'actuator
        /// </summary>
        public void GetVersionFirmware()
        {
            GetXVal(AX12RegisterAdd.VersionFirmware, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] == 0)
                        {
                            if (OnRefreshValue != null)
                                OnRefreshValue(AX12RegisterAdd.VersionFirmware, y[3]);
                        }
                        else if (OnReadError != null)
                        {
                            OnReadError(AX12RegisterAdd.VersionFirmware, y[2]);
                        }
                    }));
        }
       
        /// <summary>
        /// Obtient la vitesse de transmission configurée au niveau de l'Actuator
        /// </summary>
        public void GetBaudRate()
        {
            GetXVal(AX12RegisterAdd.BaudRate, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                     (x, y) =>
                     {
                         if (y[2] == 0)
                         {
                             if (OnRefreshValue != null)
                             {
                                 OnRefreshValue(AX12RegisterAdd.BaudRate, y[3]);
                             }
                         }
                         else if (OnReadError != null)
                         {
                             OnReadError(AX12RegisterAdd.BaudRate, y[2]);
                         }
                     }));
        }

        /// <summary>
        /// Spécifie la vitesse de transmission à appliquer au niveau de l'actuator
        /// </summary>
        /// <param name="val"></param>
        public void SetBaudRate(byte val)
        {
            if (!this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.BaudRate, val, null);
            }
            else
            {
                SetXVal(AX12RegisterAdd.BaudRate, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                     (x, y) =>
                     {
                         if (y[2] != 0)
                         {
                                 OnWritingError(AX12RegisterAdd.BaudRate, y[2]);
                         }
                     }));
            }
        }

        /// <summary>
        /// Obtient le Delai avant la transmission de la réponse par l'actuator
        /// </summary>
        public void GetReturnDelayTime()
        {
            GetXVal(AX12RegisterAdd.ReturnDelayTime, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] == 0)
                    {
                        if (OnRefreshValue != null)
                        {
                            OnRefreshValue(AX12RegisterAdd.ReturnDelayTime, y[3]);
                        }
                    }
                    else if (OnReadError != null)
                    {
                        OnReadError(AX12RegisterAdd.ReturnDelayTime, y[2]);
                    }
                }));
        }

        /// <summary>
        /// Spécifie la valeur du délai de retour réponse par l'actuator
        /// </summary>
        /// <param name="val"></param>
        public void SetReturnDelayTime(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.ReturnDelayTime, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                                WritingError(AX12RegisterAdd.ReturnDelayTime, y[2]);
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.ReturnDelayTime, val, null);
            }
        }

        /// <summary>
        /// Obtient la valeur limite dans le sens des aiguilles d'une montre
        /// </summary>
        public void GetCWAngleLimit()
        {
            GetXVal(AX12RegisterAdd.CWAngleLimit, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] == 0)
                    {
                        if (OnRefreshValue != null)
                            OnRefreshValue(AX12RegisterAdd.CWAngleLimit, (ushort)((y[4] << 8) + y[3]));
                    }
                    else if (OnReadError != null)
                    {
                        OnReadError(AX12RegisterAdd.CWAngleLimit, y[2]);
                    }
                }));
        }

        /// <summary>
        /// Spécifie la valeur limite dans le sens des aiguilles d'une montre
        /// </summary>
        public void SetCWAngleLimit(int value)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.CWAngleLimit, value, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.CWAngleLimit, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.CWAngleLimit, value, null);
            }
        }

        public void GetCCWAngleLimit()
        {
            GetXVal(AX12RegisterAdd.CCWAngleLimit, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] == 0)
                    {
                        if (OnRefreshValue != null)
                        {
                            OnRefreshValue(AX12RegisterAdd.CCWAngleLimit, (ushort)((y[4] << 8) + y[3]));
                        }
                    }
                }));
        }

        public void SetCCWAngleLimit(int value)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.CCWAngleLimit, value, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] != 0)
                    {
                        if (WritingError != null)
                        {
                            WritingError(AX12RegisterAdd.CCWAngleLimit, y[2]);
                        }
                    }
                }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.CCWAngleLimit, value, null);
            }
        }

        public void GetHighestLimitTemperature()
        {
            GetXVal(AX12RegisterAdd.HighestLimitTemperature, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.HighestLimitTemperature, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.HighestLimitTemperature, y[2]);
                   }
               }));
        }

        public void SetHighestLimitTemperature(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.HighestLimitTemperature, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.HighestLimitTemperature, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.HighestLimitTemperature, val, null);
            }
        }

        public void GetLowestLimitVoltage()
        {
            GetXVal(AX12RegisterAdd.LowestLimitVoltage, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.LowestLimitVoltage, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.LowestLimitVoltage, y[2]);
                   }
               }));
        }

        public void SetLowestLimitVoltage(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.LowestLimitVoltage, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.LowestLimitVoltage, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.LowestLimitVoltage, val, null);
            }
        }

        public void GetHighestLimitVoltage()
        {
            GetXVal(AX12RegisterAdd.HighestLimitVoltage, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.HighestLimitVoltage, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.HighestLimitVoltage, y[2]);
                   }
               }));
        }

        public void SetHighestLimitVoltage(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.HighestLimitTemperature, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.HighestLimitTemperature, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.HighestLimitTemperature, val, null);
            }
        }

        public void GetMaxTorque()
        {
            GetXVal(AX12RegisterAdd.MaxTorque, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.MaxTorque, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.MaxTorque, y[2]);
                   }
               }));
        }

        public void SetMaxTorque(int val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.MaxTorque, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.MaxTorque, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.MaxTorque, val, null);
            }
        }

        public void GetStatusReturnLevel()
        {
            GetXVal(AX12RegisterAdd.StatusReturnLevel, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.StatusReturnLevel, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.StatusReturnLevel, y[2]);
                   }
               }));
        }

        public void SetStatusReturnLevel(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.StatusReturnLevel, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.StatusReturnLevel, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.StatusReturnLevel, val, null);
            }
        }

        public void GetAlarmLED()
        {
            GetXVal(AX12RegisterAdd.AlarmLED, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.AlarmLED, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.AlarmLED, y[2]);
                   }
               }));
        }

        public void SetAlarmLED(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.AlarmLED, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.AlarmLED, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.AlarmLED, val, null);
            }
        }

        public void GetAlarmShutdown()
        {
            GetXVal(AX12RegisterAdd.AlarmShutdown, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.AlarmShutdown, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.AlarmShutdown, y[2]);
                   }
               }));
        }

        public void SetAlarmShutdown(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.AlarmShutdown, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.AlarmShutdown, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.AlarmShutdown, val, null);
            }
        }

        public void GetDownCalibration()
        {
            GetXVal(AX12RegisterAdd.DownCalibration, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.DownCalibration, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.DownCalibration, y[2]);
                   }
               }));
        }

        public void SetDownCalibration(int val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.DownCalibration, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.DownCalibration, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.DownCalibration, val, null);
            }
        }

        public void GetUpCalibration()
        {
            GetXVal(AX12RegisterAdd.UpCalibration, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.UpCalibration, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.UpCalibration, y[2]);
                   }
               }));
        }

        public void SetUpCalibration(int val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.UpCalibration, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.UpCalibration, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.UpCalibration, val, null);
            }
        }

        public void GetLED()
        {
            GetXVal(AX12RegisterAdd.LED, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.LED, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.LED, y[2]);
                   }
               }));
        }

        public void SetLED(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.LED, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.LED, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.LED, val, null);
            }
        }

        public void GetCWComplianceMargin()
        {
            GetXVal(AX12RegisterAdd.LED, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.CWComplianceMargin, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.CWComplianceMargin, y[2]);
                   }
               }));
        }

        public void SetCWComplianceMargin(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.CWComplianceMargin, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.CWComplianceMargin, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.CWComplianceMargin, val, null);
            }
        }

        public void GetCCWComplianceMargin()
        {
            GetXVal(AX12RegisterAdd.CCWComplianceMargin, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.CCWComplianceMargin, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.CCWComplianceMargin, y[2]);
                   }
               }));
        }

        public void SetCCWComplianceMargin(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.CCWComplianceMargin, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.CCWComplianceMargin, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.CCWComplianceMargin, val, null);
            }
        }

        public void GetCWComplianceSlope()
        {
            GetXVal(AX12RegisterAdd.CWComplianceSlope, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.CWComplianceSlope, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.CWComplianceSlope, y[2]);
                   }
               }));
        }

        public void SetCWComplianceSlope(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.CWComplianceSlope, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.CWComplianceSlope, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.CWComplianceSlope, val, null);
            }
        }

        public void GetCCWComplianceSlope()
        {
            GetXVal(AX12RegisterAdd.CCWComplianceSlope, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.CCWComplianceSlope, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.CCWComplianceSlope, y[2]);
                   }
               }));
        }

        public void SetCCWComplianceSlope(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.CCWComplianceSlope, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.CCWComplianceSlope, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.CCWComplianceSlope, val, null);
            }
        }

        public void GetGoalPosition()
        {
            GetXVal(AX12RegisterAdd.GoalPosition, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.GoalPosition, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.GoalPosition, y[2]);
                   }
               }));
        }

        public void SetGoalPosition(int val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.GoalPosition, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.GoalPosition, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.GoalPosition, val, null);
            }
        }

        public void GetMovingSpeed()
        {
            GetXVal(AX12RegisterAdd.MovingSpeed, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.MovingSpeed, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.MovingSpeed, y[2]);
                   }
               }));
        }

        public void SetMovingSpeed(int val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.MovingSpeed, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.MovingSpeed, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.MovingSpeed, val, null);
            }
        }

        public void GetTorqueLimit()
        {
            GetXVal(AX12RegisterAdd.TorqueLimit, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.TorqueLimit, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.TorqueLimit, y[2]);
                   }
               }));
        }

        public void SetTorqueLimit(int val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.TorqueLimit, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.TorqueLimit, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.TorqueLimit, val, null);
            }
        }

        public void GetPresentPosition()
        {
            GetXVal(AX12RegisterAdd.PresentPosition, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.PresentPosition, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.PresentPosition, y[2]);
                   }
               }));
        }

        public void GetPresentSpeed()
        {
            GetXVal(AX12RegisterAdd.PresentSpeed, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.PresentSpeed, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.PresentSpeed, y[2]);
                   }
               }));
        }

        public void GetPresentLoad()
        {
            GetXVal(AX12RegisterAdd.PresentLoad, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.PresentLoad, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.PresentLoad, y[2]);
                   }
               }));
        }

        public void GetPresentVoltage()
        {
            GetXVal(AX12RegisterAdd.PresentVoltage, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.PresentVoltage, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.PresentVoltage, y[2]);
                   }
               }));
        }

        public void GetPresentTemperature()
        {
            GetXVal(AX12RegisterAdd.PresentTemeprature, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.PresentTemeprature, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.PresentTemeprature, y[2]);
                   }
               }));
        }

        public void GetRegisteredInstruction()
        {
            GetXVal(AX12RegisterAdd.RegisteredInstruction, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.RegisteredInstruction, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.RegisteredInstruction, y[2]);
                   }
               }));
        }

        public void SetRegisteredInstruction(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.RegisteredInstruction, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.RegisteredInstruction, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.RegisteredInstruction, val, null);
            }
        }

        public void GetMoving()
        {
            GetXVal(AX12RegisterAdd.Moving, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.Moving, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.Moving, y[2]);
                   }
               }));
        }

        public void GetLock()
        {
            GetXVal(AX12RegisterAdd.Lock, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.Lock, y[3]);
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.Lock, y[2]);
                   }
               }));
        }

        public void SetLock(byte val)
        {
            if (this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.Lock, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                    (x, y) =>
                    {
                        if (y[2] != 0)
                        {
                            if (WritingError != null)
                            {
                                WritingError(AX12RegisterAdd.Lock, y[2]);
                            }
                        }
                    }));
            }
            else
            {
                SetXVal(AX12RegisterAdd.Lock, val, null);
            }
        }

        public void GetPunch()
        {
            GetXVal(AX12RegisterAdd.Punch, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
               (x, y) =>
               {
                   if (y[2] == 0)
                   {
                       if (OnRefreshValue != null)
                           OnRefreshValue(AX12RegisterAdd.Punch, (ushort)((y[4] << 8) + y[3]));
                   }
                   else if (OnReadError != null)
                   {
                       OnReadError(AX12RegisterAdd.Punch, y[2]);
                   }
               }));
        }

        public void SetPunch(int val)
        {
            SetXVal(AX12RegisterAdd.Punch, val, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] != 0)
                    {
                        if (WritingError != null)
                        {
                            WritingError(AX12RegisterAdd.Punch, y[2]);
                        }
                    }
                }));
        }
    }

    public class AXS1Dynamixel : AXDynamixel
    {
        public override event RefreshValue OnReadError;
        public override event RefreshValue OnRefreshValue;
        public override event RefreshValue OnWritingError;

        internal AXS1Dynamixel(ServiceFirmata buscom, byte identifiant_dynamixel):base(buscom,identifiant_dynamixel)
        {
        }

        public void SetID(byte identifiant)
        {
            if (!this.ResponseWanted)
            {
                SetXVal(AX12RegisterAdd.ID, identifiant, null);
            }
            else
            {
                SetXVal(AX12RegisterAdd.ID, identifiant, new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                         (x, y) =>
                         {
                             if (y[2] != 0)
                             {
                                 if (OnWritingError != null)
                                 {
                                     OnWritingError(AX12RegisterAdd.ID, y[2]);
                                 }
                             }
                         }));
            }
        }


    }

}
