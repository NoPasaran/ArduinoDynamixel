using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino.Framework.Communs.EnumConstants
{
    public enum AX12RegisterAdd : byte
    {
        //EEPROM ZONE
        ModelNumber = 0x00,
        VersionFirmware = 0x02,
        ID = 0x03,
        BaudRate = 0x04,
        ReturnDelayTime = 0x05,
        CWAngleLimit = 0x06,
        CCWAngleLimit = 0x08,
        HighestLimitTemperature = 0x0B,
        LowestLimitVoltage = 0x0C,
        HighestLimitVoltage = 0x3D,
        MaxTorque = 0x0E,
        StatusReturnLevel = 0x10,
        AlarmLED = 0x11,
        AlarmShutdown = 0x12,
        DownCalibration = 0x14,
        UpCalibration = 0x16,
        //RAM ZONE
        TorqueEnable = 0x18,
        LED = 0x19,
        CWComplianceMargin = 0x1A,
        CCWComplianceMargin = 0x1B,
        CWComplianceSlope = 0x1C,
        CCWComplianceSlope = 0x1D,
        GoalPosition = 0x1E,
        MovingSpeed = 0x20,
        TorqueLimit = 0x22,
        PresentPosition = 0x24,
        PresentSpeed = 0x26,
        PresentLoad = 0x28,
        PresentVoltage = 0x2A,
        PresentTemeprature = 0x2B,
        RegisteredInstruction = 0x2C,
        Moving = 0x2E,
        Lock = 0x2F,
        Punch = 0x30
    }

    [Flags]
    public enum AX12ErrorBit : byte
    {
        NoneError = 0x00,
        InstructionError = 0x20,
        OverloadError = 0x10,
        ChecksumError = 0x08,
        RangeError = 0x04,
        OverheatingError = 0x02,
        AngleLimitError = 0x01

    }

    /// <summary>
    /// Permet d'indiquer le sens de rotation. (Dans le sens des aiguilles d'une montre ou bien le sens contraire)
    /// </summary>
    public enum LoadDirection : byte
    {
        CCWLoad = 0x00,
        CWLoad = 0x01
    }

    public enum TurnDirection : byte
    {
        CCWDirectioNTurn = 0x00,
        CWDirectionTurn = 0x01
    }

    public enum AXS1RegisterAdd : byte
    {
        //EEPROM ZONE
        ModelNumber = 0x00,
        VersionFirmware = 0x02,
        ID = 0x03,
        BaudRate = 0x04,
        ReturnDelayTime = 0x05,
        HighestLimitTemperature = 0x0B,
        LowestLimitVoltage = 0x0C,
        HighestLimitVoltage = 0x0D,
        StatusReturnLevel = 0x10,
        ObstacleDetectedCompareValue = 0x14,
        LightDetectedCompareValue = 0x15,
        //RAM ZONE,
        LeftIRSensorData = 0x1A,
        CenterIRSensorData = 0x1B,
        RightIRSensorData = 0x1C,
        LeftLuminosity = 0x1D,
        CenterLuminosity = 0x1E,
        RightLuminosity = 0x1F,
        ObstacleDetectionFlag = 0x20,
        LuminosityDetectionFlag = 0x21,
        SoundData = 0x23,
        SoundDataMaxHold = 0x24,
        SoundDetectedCount = 0x25,
        SoundDetectedTime = 0x26,
        BuzzerIndex = 0x28,
        BuzzerTime = 0x29,
        PresentVoltage = 0x2A,
        PresentTemperature = 0x2B,
        RegisteredInstruction = 0x2C,
        IRRemoconArrived = 0x2E,
        Lock = 0x2F,
        IRRemoconRXData0 = 0x30,
        IRRemoconRXData1 = 0x31,
        IRRemoconTXData0 = 0x32,
        IRRemoconTXData1 = 0x33,
        ObstacleDetectedCompare = 0x34,
        LightDetectedCompare = 0x35

    }

    public enum ModeAccessRegister : byte
    {
        RD = 0,
        WR = 1,
        RDetWR = 2
    }

    public struct BioloidRegister
    {
        private byte startadresseregister;
        private byte length;
        private ushort minval;
        private ushort maxval;
        private ModeAccessRegister accessmode;
        private ushort initialvalue; //TODO mettre ce paramètre en argument du constructor + mise à jour de la phase d'initialisation.
        private bool hasinitialvalue;


        public BioloidRegister(byte startAdd, byte length, ushort initialvalue,ModeAccessRegister modeaccess, ushort minval = ushort.MaxValue, ushort maxval = ushort.MinValue )
        {
            this.initialvalue = initialvalue;
            this.hasinitialvalue = true;
            this.startadresseregister = startAdd;
            this.length = length;
            this.minval = minval;
            this.maxval = maxval;
            this.accessmode = modeaccess;

        }
        public BioloidRegister(byte startAdd, byte length, ModeAccessRegister modeaccess, ushort minval = ushort.MaxValue, ushort maxval = ushort.MinValue)
        {
            this.initialvalue = 0;
            this.hasinitialvalue = false;
            this.startadresseregister = startAdd;
            this.length = length;
            this.minval = minval;
            this.maxval = maxval;
            this.accessmode = modeaccess;
        }


        public byte StartAdressRegister { get { return startadresseregister; } }
        public byte Length { get { return this.length; } }
        public ushort MinVal { get { return this.minval; } }
        public ushort MaxVal { get { return this.maxval; } }
        public ModeAccessRegister AccessMode { get { return this.accessmode; } }
        /// <summary>
        /// Retoune la valeur initiale si elle existe. Dans le cas contraire retourne toujours 0.
        /// Il faut donc s'assurer de vérifier l'existence d'une valeur par défaut avant d'y faire appel (sauf si nous sommes certains).
        /// </summary>
        public ushort InitialValue { get { return this.hasinitialvalue==true?this.initialvalue:(ushort)0; } }
        public bool HasInitialValue { get { return this.hasinitialvalue; } }

    }

    public class AXXRegisterInfo
    {
        private static AXXRegisterInfo _refInternal = new AXXRegisterInfo();

        private Dictionary<byte, BioloidRegister> ax12Registers = new Dictionary<byte, BioloidRegister>();

        private Dictionary<byte, BioloidRegister> axS1Registers = new Dictionary<byte, BioloidRegister>();

        private AXXRegisterInfo()
        {
            this.LoadingInformationAX12();
            this.LoadingInformationAXS1();
        }

        private void LoadingInformationAXS1()
        {
            axS1Registers.Add((byte)AXS1RegisterAdd.ModelNumber
                , new BioloidRegister((byte)AXS1RegisterAdd.ModelNumber, 2, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.VersionFirmware
                , new BioloidRegister((byte)AXS1RegisterAdd.VersionFirmware, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.ID
                , new BioloidRegister((byte)AXS1RegisterAdd.ID, 1, ModeAccessRegister.RDetWR, 0, 0xFD));
            axS1Registers.Add((byte)AXS1RegisterAdd.BaudRate
                , new BioloidRegister((byte)AXS1RegisterAdd.BaudRate, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            axS1Registers.Add((byte)AXS1RegisterAdd.ReturnDelayTime
                , new BioloidRegister((byte)AXS1RegisterAdd.ReturnDelayTime, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            axS1Registers.Add((byte)AXS1RegisterAdd.HighestLimitTemperature
                , new BioloidRegister((byte)AXS1RegisterAdd.HighestLimitTemperature, 1, ModeAccessRegister.RDetWR, 0, 0x96));
            axS1Registers.Add((byte)AXS1RegisterAdd.LowestLimitVoltage
                , new BioloidRegister((byte)AXS1RegisterAdd.LowestLimitVoltage, 1, ModeAccessRegister.RDetWR, 0x32, 0xFA));
            axS1Registers.Add((byte)AXS1RegisterAdd.HighestLimitVoltage
                , new BioloidRegister((byte)AXS1RegisterAdd.HighestLimitVoltage, 1, ModeAccessRegister.RDetWR, 0x32, 0xFA));
            axS1Registers.Add((byte)AXS1RegisterAdd.StatusReturnLevel
                , new BioloidRegister((byte)AXS1RegisterAdd.StatusReturnLevel, 1, ModeAccessRegister.RDetWR, 0, 0x02));
            axS1Registers.Add((byte)AXS1RegisterAdd.ObstacleDetectedCompareValue
                , new BioloidRegister((byte)AXS1RegisterAdd.ObstacleDetectedCompareValue, 1, ModeAccessRegister.RDetWR, 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.LightDetectedCompareValue
                , new BioloidRegister((byte)AXS1RegisterAdd.LightDetectedCompareValue, 1, ModeAccessRegister.RDetWR, 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.LeftIRSensorData
                , new BioloidRegister((byte)AXS1RegisterAdd.LeftIRSensorData, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.CenterIRSensorData
                , new BioloidRegister((byte)AXS1RegisterAdd.CenterIRSensorData, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.RightIRSensorData
                , new BioloidRegister((byte)AXS1RegisterAdd.RightIRSensorData, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.LeftLuminosity
                , new BioloidRegister((byte)AXS1RegisterAdd.LeftLuminosity, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.CenterLuminosity
                , new BioloidRegister((byte)AXS1RegisterAdd.CenterLuminosity, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.RightLuminosity
                , new BioloidRegister((byte)AXS1RegisterAdd.RightLuminosity, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.ObstacleDetectionFlag
                , new BioloidRegister((byte)AXS1RegisterAdd.ObstacleDetectionFlag, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.LuminosityDetectionFlag
                , new BioloidRegister((byte)AXS1RegisterAdd.LuminosityDetectionFlag, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundData
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundData, 1, ModeAccessRegister.RDetWR));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundDataMaxHold
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundDataMaxHold, 1, ModeAccessRegister.RDetWR, 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundDetectedCount
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundDetectedCount, 1, ModeAccessRegister.RDetWR, 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundDetectedTime
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundDetectedTime, 2, ModeAccessRegister.RDetWR, 0, (ushort)0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.BuzzerIndex
                , new BioloidRegister((byte)AXS1RegisterAdd.BuzzerIndex, 1, ModeAccessRegister.RDetWR, 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.BuzzerTime
                , new BioloidRegister((byte)AXS1RegisterAdd.BuzzerTime, 1, ModeAccessRegister.RDetWR, 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.PresentVoltage
                , new BioloidRegister((byte)AXS1RegisterAdd.PresentVoltage, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.PresentTemperature
                , new BioloidRegister((byte)AXS1RegisterAdd.PresentTemperature, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.RegisteredInstruction
                , new BioloidRegister((byte)AXS1RegisterAdd.RegisteredInstruction, 1, ModeAccessRegister.RDetWR, 0, 0x01));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconArrived
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconArrived, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.Lock
                , new BioloidRegister((byte)AXS1RegisterAdd.Lock, 1, ModeAccessRegister.RDetWR, 0, 0x01));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconRXData0
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconRXData0, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconRXData1
                            , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconRXData1, 1, ModeAccessRegister.RD));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconTXData0
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconTXData0, 1, ModeAccessRegister.RDetWR, 0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconTXData1
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconTXData1, 1, ModeAccessRegister.RDetWR, 0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.ObstacleDetectedCompare
                , new BioloidRegister((byte)AXS1RegisterAdd.ObstacleDetectedCompare, 1, ModeAccessRegister.RDetWR, 0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.LightDetectedCompare
                , new BioloidRegister((byte)AXS1RegisterAdd.LightDetectedCompare, 1, ModeAccessRegister.RDetWR, 0xFFFF));
        }

        private void LoadingInformationAX12()
        {
            ax12Registers.Add((byte)AX12RegisterAdd.ModelNumber
                , new BioloidRegister((byte)AX12RegisterAdd.ModelNumber, 2, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.VersionFirmware
                , new BioloidRegister((byte)AX12RegisterAdd.VersionFirmware, 1, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.ID
                , new BioloidRegister((byte)AX12RegisterAdd.ID, 1, ModeAccessRegister.RDetWR, 0, 0xFD));
            ax12Registers.Add((byte)AX12RegisterAdd.BaudRate
                , new BioloidRegister((byte)AX12RegisterAdd.BaudRate, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.ReturnDelayTime
                , new BioloidRegister((byte)AX12RegisterAdd.ReturnDelayTime, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CWAngleLimit
                , new BioloidRegister((byte)AX12RegisterAdd.CWAngleLimit, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.CCWAngleLimit
                , new BioloidRegister((byte)AX12RegisterAdd.CCWAngleLimit, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.HighestLimitTemperature
                , new BioloidRegister((byte)AX12RegisterAdd.HighestLimitTemperature, 1, ModeAccessRegister.RDetWR, 0, 0x96));
            ax12Registers.Add((byte)AX12RegisterAdd.LowestLimitVoltage
                , new BioloidRegister((byte)AX12RegisterAdd.LowestLimitVoltage, 1, ModeAccessRegister.RDetWR, 0x32, 0xFA));
            ax12Registers.Add((byte)AX12RegisterAdd.HighestLimitVoltage
                , new BioloidRegister((byte)AX12RegisterAdd.HighestLimitVoltage, 1, ModeAccessRegister.RDetWR, 0x32, 0xFA));
            ax12Registers.Add((byte)AX12RegisterAdd.MaxTorque
                , new BioloidRegister((byte)AX12RegisterAdd.MaxTorque, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.StatusReturnLevel
                , new BioloidRegister((byte)AX12RegisterAdd.StatusReturnLevel, 1, ModeAccessRegister.RDetWR, 0, 0x02));
            ax12Registers.Add((byte)AX12RegisterAdd.AlarmLED
                , new BioloidRegister((byte)AX12RegisterAdd.AlarmLED, 1, ModeAccessRegister.RDetWR, 0, 0x7F));
            ax12Registers.Add((byte)AX12RegisterAdd.AlarmShutdown
                , new BioloidRegister((byte)AX12RegisterAdd.AlarmShutdown, 1, ModeAccessRegister.RDetWR, 0, 0x7F));
            ax12Registers.Add((byte)AX12RegisterAdd.DownCalibration
                , new BioloidRegister((byte)AX12RegisterAdd.DownCalibration, 2, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.UpCalibration
                , new BioloidRegister((byte)AX12RegisterAdd.ReturnDelayTime, 2, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.TorqueEnable
                , new BioloidRegister((byte)AX12RegisterAdd.TorqueEnable, 1, ModeAccessRegister.RDetWR, 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.LED
                , new BioloidRegister((byte)AX12RegisterAdd.LED, 1, ModeAccessRegister.RDetWR, 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.CWComplianceMargin
                , new BioloidRegister((byte)AX12RegisterAdd.CWComplianceMargin, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CCWComplianceMargin
                , new BioloidRegister((byte)AX12RegisterAdd.CCWComplianceMargin, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CWComplianceSlope
                , new BioloidRegister((byte)AX12RegisterAdd.CWComplianceSlope, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CCWComplianceSlope
                , new BioloidRegister((byte)AX12RegisterAdd.CCWComplianceSlope, 1, ModeAccessRegister.RDetWR, 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.GoalPosition
                , new BioloidRegister((byte)AX12RegisterAdd.GoalPosition, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.MovingSpeed
                , new BioloidRegister((byte)AX12RegisterAdd.MovingSpeed, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.TorqueLimit
                , new BioloidRegister((byte)AX12RegisterAdd.TorqueLimit, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentPosition
                , new BioloidRegister((byte)AX12RegisterAdd.PresentPosition, 2, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentSpeed
                , new BioloidRegister((byte)AX12RegisterAdd.PresentSpeed, 2, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentLoad
                , new BioloidRegister((byte)AX12RegisterAdd.PresentLoad, 1, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentVoltage
                , new BioloidRegister((byte)AX12RegisterAdd.PresentVoltage, 1, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentTemeprature
                , new BioloidRegister((byte)AX12RegisterAdd.PresentTemeprature, 1, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.RegisteredInstruction
                , new BioloidRegister((byte)AX12RegisterAdd.RegisteredInstruction, 1, ModeAccessRegister.RDetWR, 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.Moving
                , new BioloidRegister((byte)AX12RegisterAdd.Moving, 1, ModeAccessRegister.RD));
            ax12Registers.Add((byte)AX12RegisterAdd.Lock
                , new BioloidRegister((byte)AX12RegisterAdd.Lock, 1, ModeAccessRegister.RDetWR, 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.Punch
                , new BioloidRegister((byte)AX12RegisterAdd.Punch, 2, ModeAccessRegister.RDetWR, 0, 0x3FF));
        }

        public static AXXRegisterInfo GetInstance() { return _refInternal; }

        public BioloidRegister GetAX12Register(AX12RegisterAdd add)
        {
            if (ax12Registers.ContainsKey((byte)add) == true)
            {
                return ax12Registers[(byte)add];
            }
            else
            {
                throw new Exception("Non encore implémentée");
            }
        }

        public BioloidRegister GetAXS1Register(AXS1RegisterAdd add)
        {
            if (axS1Registers.ContainsKey((byte)add) == true)
            {
                return axS1Registers[(byte)add];
            }
            else
            {
                throw new Exception("Non encore implémentée");
            }
        }

        public BioloidRegister[] GetAX12Registers() { return this.ax12Registers.Values.ToArray<BioloidRegister>(); }

        public BioloidRegister[] GetAXS1Registers() { return this.axS1Registers.Values.ToArray<BioloidRegister>(); }
    }
}
