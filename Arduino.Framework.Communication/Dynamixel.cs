using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino.Framework.Communication
{
    public delegate void RefreshValue(byte address, UInt16 value);

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

        private Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback WriteCallback(BioloidRegister register)
        {
            return new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] != 0)
                    {
                        OnWritingError(register.StartAdressRegister, y[2]);
                    }
                });
        }
        private Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback ReadCallback(BioloidRegister register)
        {
            return new Arduino.Communication.DataAccess.ArduinoBus.currentSysexCallback(
                (x, y) =>
                {
                    if (y[2] == 0)
                    {
                        if (register.Length == 1)
                            OnValueRefresh(register.StartAdressRegister, y[3]);
                        else
                            OnValueRefresh(register.StartAdressRegister, (ushort)(y[3] + (y[4] << 8)));
                    }
                    else
                    {
                        OnReadError(register.StartAdressRegister, y[2]);
                    }
                });
        }

        public void GetXVal(BioloidRegister register)
        {
            lock (this.lockref)
            {
                if (_responseWanted == true)
                {
                    _busarduino.SendReadInstructionMessage(_identifiant_dynamixel, register, 0
                        , this.ReadCallback(register));
                }
            }
        }

        /// <summary>
        /// Méthode générique pour modifier la valeur d'un registre
        /// </summary>
        /// <param name="add">Addresse du registre à modifier</param>
        /// <param name="val">Nouvelle valeur</param>
        public void SetXVal(BioloidRegister register, byte val)
        {
            lock (lockref)
            {
                if (_responseWanted == true)
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, register, new byte[] { val }
                        , this.WriteCallback(register));

                }
                else
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, register, new byte[] { val });
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="add"></param>
        /// <param name="val"></param>
        /// <param name="callback"></param>
        protected internal void SetXVal(BioloidRegister register, int val)
        {
            lock (this.lockref)
            {
                if (_responseWanted == true)
                {
                    // On désire obtenir une réponse 
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, register, new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) }
                    , this.WriteCallback(register));
                }
                else
                {
                    _busarduino.SendWriteInstructionMessage(_identifiant_dynamixel, register, new byte[] { (byte)(val & 0xFF), (byte)((val >> 8) & 0xFF) });
                }
            }
        }

        public void SetID(byte identifiant)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.ID), identifiant);
        }

        /// <summary>
        /// Obtient le model de l'actuator
        /// </summary>
        public void GetModel()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.ModelNumber));
        }

        /// <summary>
        /// Obtient la version du firmware qui est installée au niveau de l'actuator
        /// </summary>
        public void GetVersionFirmware()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.VersionFirmware));
        }

        /// <summary>
        /// Obtient la vitesse de transmission configurée au niveau de l'Actuator
        /// </summary>
        public void GetBaudRate()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.BaudRate));
        }

        /// <summary>
        /// Spécifie la vitesse de transmission à appliquer au niveau de l'actuator
        /// </summary>
        /// <param name="val"></param>
        public void SetBaudRate(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.BaudRate), val);
        }

        /// <summary>
        /// Obtient le Delai avant la transmission de la réponse par l'actuator
        /// </summary>
        public void GetReturnDelayTime()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.ReturnDelayTime));
        }

        /// <summary>
        /// Spécifie la valeur du délai de retour réponse par l'actuator
        /// </summary>
        /// <param name="val"></param>
        public void SetReturnDelayTime(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.ReturnDelayTime), val);
        }

        #region Method Method Declaration

        protected virtual void OnReadError(byte add, UInt16 value)
        {
            // Une copie temporaire est effectuée afin d'éviter une "race condition" si le dernier abonné se désabonne 
            // immédiatement après le control du null et avant levé de l'évènement
            RefreshValue handler = ReadError;
            if (handler != null)
            {
                handler(add, value);
            }
        }

        protected virtual void OnWritingError(byte add, UInt16 value)
        {
            // Une copie temporaire est effectuée afin d'éviter une "race condition" si le dernier abonné se désabonne 
            // immédiatement après le control du null et avant levé de l'évènement
            RefreshValue handler = WritingError;
            if (handler != null)
            {
                handler(add, value);
            }
        }

        protected virtual void OnValueRefresh(byte add, UInt16 value)
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

        internal AX12Dynamixel(ServiceFirmata buscom, byte identifiant_dynamixel)
            : base(buscom, identifiant_dynamixel)
        {
        }

        #region Override AXDynamixel

        protected override void OnReadError(byte add, ushort value)
        {
            // Do any cirle-specific processing here

            // call the base class event invocation method
            base.OnReadError(add, value);
        }

        protected override void OnValueRefresh(byte add, ushort value)
        {
            // Do any circle-specfic processing here


            // call the base class event invocation method
            base.OnValueRefresh(add, value);
        }

        protected override void OnWritingError(byte add, ushort value)
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
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.TorqueEnable));
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
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.TorqueEnable), status == true ? 1 : 0);
        }



        /// <summary>
        /// Obtient la valeur limite dans le sens des aiguilles d'une montre
        /// </summary>
        public void GetCWAngleLimit()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CWAngleLimit));
        }

        /// <summary>
        /// Spécifie la valeur limite dans le sens des aiguilles d'une montre
        /// </summary>
        public void SetCWAngleLimit(int value)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CWAngleLimit), value);
        }

        public void GetCCWAngleLimit()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CCWAngleLimit));
        }

        public void SetCCWAngleLimit(int value)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CCWAngleLimit), value);
        }

        public void GetHighestLimitTemperature()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.HighestLimitTemperature));
        }

        public void SetHighestLimitTemperature(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.HighestLimitTemperature), val);
        }

        public void GetLowestLimitVoltage()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.LowestLimitVoltage));
        }

        public void SetLowestLimitVoltage(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.LowestLimitVoltage), val);
        }

        public void GetHighestLimitVoltage()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.HighestLimitVoltage));
        }

        public void SetHighestLimitVoltage(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.HighestLimitTemperature), val);
        }

        public void GetMaxTorque()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.MaxTorque));
        }

        public void SetMaxTorque(int val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.MaxTorque), val);
        }

        public void GetStatusReturnLevel()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.StatusReturnLevel));
        }

        public void SetStatusReturnLevel(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.StatusReturnLevel), val);
        }

        public void GetAlarmLED()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.AlarmLED));
        }

        public void SetAlarmLED(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.AlarmLED), val);
        }

        public void GetAlarmShutdown()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.AlarmShutdown));
        }

        public void SetAlarmShutdown(byte val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.AlarmShutdown), val);
        }

        public void GetDownCalibration()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.DownCalibration));
        }

        public void SetDownCalibration(int val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.DownCalibration), val);
        }

        public void GetUpCalibration()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.UpCalibration));
        }

        public void SetUpCalibration(int val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.UpCalibration), val);
        }

        public void GetLED()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.LED));
        }

        public void SetLED(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.LED), val);
        }

        public void GetCWComplianceMargin()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.LED));
        }

        public void SetCWComplianceMargin(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CWComplianceMargin), val);
        }

        public void GetCCWComplianceMargin()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CCWComplianceMargin));
        }

        public void SetCCWComplianceMargin(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CCWComplianceMargin), val);
        }

        public void GetCWComplianceSlope()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CWComplianceSlope));
        }

        public void SetCWComplianceSlope(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CWComplianceSlope), val);
        }

        public void GetCCWComplianceSlope()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CCWComplianceSlope));
        }

        public void SetCCWComplianceSlope(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.CCWComplianceSlope), val);
        }

        public void GetGoalPosition()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.GoalPosition));
        }

        public void SetGoalPosition(int val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.GoalPosition), val);
        }

        public void GetMovingSpeed()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.MovingSpeed));
        }

        public void SetMovingSpeed(int val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.MovingSpeed), val);
        }

        public void GetTorqueLimit()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.TorqueLimit));
        }

        public void SetTorqueLimit(int val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.TorqueLimit), val);
        }

        public void GetPresentPosition()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.PresentPosition));
        }

        public void GetPresentSpeed()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.PresentSpeed));
        }

        public void GetPresentLoad()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.PresentLoad));
        }

        public void GetPresentVoltage()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.PresentVoltage));
        }

        public void GetPresentTemperature()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.PresentTemeprature));
        }

        public void GetRegisteredInstruction()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.RegisteredInstruction));
        }

        public void SetRegisteredInstruction(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.RegisteredInstruction), val);
        }

        public void GetMoving()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.Moving));
        }

        public void GetLock()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.Lock));
        }

        public void SetLock(byte val)
        {
                SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.Lock), val);
        }

        public void GetPunch()
        {
            GetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.Punch));
        }

        public void SetPunch(int val)
        {
            SetXVal(_busarduino.GetAX12Register(AX12RegisterAdd.Punch), val);
        }
    }

    public class AXS1Dynamixel : AXDynamixel
    {
        internal AXS1Dynamixel(ServiceFirmata buscom, byte identifiant_dynamixel)
            : base(buscom, identifiant_dynamixel)
        {
        }

        #region Override AXDynamixel

        protected override void OnReadError(byte add, ushort value)
        {
            // Do any cirle-specific processing here

            // call the base class event invocation method
            base.OnReadError(add, value);
        }

        protected override void OnValueRefresh(byte add, ushort value)
        {
            // Do any circle-specfic processing here


            // call the base class event invocation method
            base.OnValueRefresh(add, value);
        }

        protected override void OnWritingError(byte add, ushort value)
        {
            // Do any circle-specific processing here

            // call the base class event invocation method
            base.OnWritingError(add, value);
        }

        #endregion

    }

}
