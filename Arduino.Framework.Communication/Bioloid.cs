using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arduino.Communication.DataAccess;

namespace Arduino.Framework.Communication
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

    [Flags]
    public enum AX12ErrorBit:byte
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
    public enum LoadDirection:byte
    {
        CCWLoad = 0x00,
        CWLoad = 0x01
    }
   
    public enum TurnDirection:byte
    {
        CCWDirectioNTurn = 0x00,
        CWDirectionTurn = 0x01
    }

    [Flags]
    public enum TypeDynamixel : byte
    {
        AX12 = 0x01,
        AXS1 = 0x02
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
        private string libelle;
        private TypeDynamixel typeactuator;

        public const TypeDynamixel AX12 = TypeDynamixel.AX12;
        public const TypeDynamixel AXS1 = TypeDynamixel.AXS1;
        public const TypeDynamixel ALL_AX = TypeDynamixel.AXS1 & TypeDynamixel.AX12;

        public BioloidRegister(byte startAdd, byte length, ModeAccessRegister modeaccess,TypeDynamixel typeactuator, string libelle, ushort minval = ushort.MaxValue, ushort maxval = ushort.MinValue)
        {
            this.startadresseregister = startAdd;
            this.length = length;
            this.minval = minval;
            this.maxval = maxval;
            this.accessmode = modeaccess;
            this.libelle = libelle;
            this.typeactuator = typeactuator;
        }


        public byte StartAdressRegister { get{return startadresseregister;} }
        public byte Length { get{return this.length;} }
        public ushort MinVal { get { return this.minval; } }
        public ushort MaxVal { get { return this.maxval; } }
        public string Libelle { get { return this.libelle; } }
        public TypeDynamixel TypeActuator { get { return this.typeactuator; } }
        public ModeAccessRegister AccessMode { get { return this.accessmode; } }

    }

    public class AXXRegisterInfo
    {
        private static AXXRegisterInfo _refInternal = new AXXRegisterInfo();

        private Dictionary<byte, BioloidRegister> ax12Registers = new Dictionary<byte, BioloidRegister>();

        private Dictionary<byte, BioloidRegister> axS1Registers = new Dictionary<byte, BioloidRegister>();

        private Dictionary<byte, BioloidRegister> comRegisters = new Dictionary<byte, BioloidRegister>();

        private AXXRegisterInfo()
        {
            this.LoadingInformationCommune();
            this.LoadingInformationAX12();
            this.LoadingInformationAXS1();
        }

        /// <summary>
        /// Chargement des paramètres AXS1
        /// </summary>
        private void LoadingInformationAXS1()
        {
            axS1Registers.Add((byte)AXS1RegisterAdd.ObstacleDetectedCompareValue
                , new BioloidRegister((byte)AXS1RegisterAdd.ObstacleDetectedCompareValue, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"Obstacle Detected CompareValue", 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.LightDetectedCompareValue
                , new BioloidRegister((byte)AXS1RegisterAdd.LightDetectedCompareValue, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"LightDetectedCompareValue", 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.LeftIRSensorData
                , new BioloidRegister((byte)AXS1RegisterAdd.LeftIRSensorData, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Left IR Sensor Data"));
            axS1Registers.Add((byte)AXS1RegisterAdd.CenterIRSensorData
                , new BioloidRegister((byte)AXS1RegisterAdd.CenterIRSensorData, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Center IR Sensor Data"));
            axS1Registers.Add((byte)AXS1RegisterAdd.RightIRSensorData
                , new BioloidRegister((byte)AXS1RegisterAdd.RightIRSensorData, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Right IR Sensor Data"));
            axS1Registers.Add((byte)AXS1RegisterAdd.LeftLuminosity
                , new BioloidRegister((byte)AXS1RegisterAdd.LeftLuminosity, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Left Luminosity"));
            axS1Registers.Add((byte)AXS1RegisterAdd.CenterLuminosity
                , new BioloidRegister((byte)AXS1RegisterAdd.CenterLuminosity, 1, ModeAccessRegister.RD,TypeDynamixel.AXS1, "Center Luminosity"));
            axS1Registers.Add((byte)AXS1RegisterAdd.RightLuminosity
                , new BioloidRegister((byte)AXS1RegisterAdd.RightLuminosity, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Right Luminosity"));
            axS1Registers.Add((byte)AXS1RegisterAdd.ObstacleDetectionFlag
                , new BioloidRegister((byte)AXS1RegisterAdd.ObstacleDetectionFlag, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Obstacle Detecion Flag"));
            axS1Registers.Add((byte)AXS1RegisterAdd.LuminosityDetectionFlag
                , new BioloidRegister((byte)AXS1RegisterAdd.LuminosityDetectionFlag, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Luminosity Detection Flag"));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundData
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundData, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Sound Data"));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundDataMaxHold
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundDataMaxHold, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Sound Data Max Hold", 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundDetectedCount
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundDetectedCount, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Sound Detected Count", 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.SoundDetectedTime
                , new BioloidRegister((byte)AXS1RegisterAdd.SoundDetectedTime, 2, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Sound Detected Time", 0, (ushort)0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.BuzzerIndex
                , new BioloidRegister((byte)AXS1RegisterAdd.BuzzerIndex, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Buzzer Index", 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.BuzzerTime
                , new BioloidRegister((byte)AXS1RegisterAdd.BuzzerTime, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Buzzer Time", 0, 0xFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.PresentVoltage
                , new BioloidRegister((byte)AXS1RegisterAdd.PresentVoltage, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Present Voltage"));
            axS1Registers.Add((byte)AXS1RegisterAdd.PresentTemperature
                , new BioloidRegister((byte)AXS1RegisterAdd.PresentTemperature, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "Present Temperature"));
            axS1Registers.Add((byte)AXS1RegisterAdd.RegisteredInstruction
                , new BioloidRegister((byte)AXS1RegisterAdd.RegisteredInstruction, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AXS1, "Registered Instruction", 0, 0x01));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconArrived
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconArrived, 1, ModeAccessRegister.RD, TypeDynamixel.AXS1, "IR Remocon Arrived"));
            axS1Registers.Add((byte)AXS1RegisterAdd.Lock
                , new BioloidRegister((byte)AXS1RegisterAdd.Lock, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"Lock", 0, 0x01));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconRXData0
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconRXData0,1, ModeAccessRegister.RD,TypeDynamixel.AXS1,"IR Remocon RXData0"));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconRXData1
                            , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconRXData1, 1, ModeAccessRegister.RD,TypeDynamixel.AXS1,"IR Remocon RXData0"));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconTXData0
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconTXData0, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"IR Remocon TxData0", 0,0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.IRRemoconTXData1
                , new BioloidRegister((byte)AXS1RegisterAdd.IRRemoconTXData1, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"IR Remocon TxData1",0, 0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.ObstacleDetectedCompare
                , new BioloidRegister((byte)AXS1RegisterAdd.ObstacleDetectedCompare, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"Obstacle Detected Compare",0, 0xFFFF));
            axS1Registers.Add((byte)AXS1RegisterAdd.LightDetectedCompare
                , new BioloidRegister((byte)AXS1RegisterAdd.LightDetectedCompare, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AXS1,"",0, 0xFFFF));
        }

        /// <summary>
        /// Chargement des paramètres AX12
        /// </summary>
        private void LoadingInformationAX12()
        {

            ax12Registers.Add((byte)AX12RegisterAdd.CWAngleLimit
                , new BioloidRegister((byte)AX12RegisterAdd.CWAngleLimit, 2, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"CW Angle Limit", 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.CCWAngleLimit
                , new BioloidRegister((byte)AX12RegisterAdd.CCWAngleLimit, 2, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"CCW Angle Limit", 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.MaxTorque
                , new BioloidRegister((byte)AX12RegisterAdd.MaxTorque, 2, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"Max Torque", 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.AlarmLED
                , new BioloidRegister((byte)AX12RegisterAdd.AlarmLED, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"Alarm Led", 0, 0x7F));
            ax12Registers.Add((byte)AX12RegisterAdd.AlarmShutdown
                , new BioloidRegister((byte)AX12RegisterAdd.AlarmShutdown, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"Alarm Shutdown", 0, 0x7F));
            ax12Registers.Add((byte)AX12RegisterAdd.DownCalibration
                , new BioloidRegister((byte)AX12RegisterAdd.DownCalibration, 2, ModeAccessRegister.RD,TypeDynamixel.AX12,"Down Calibration"));
            ax12Registers.Add((byte)AX12RegisterAdd.UpCalibration
                , new BioloidRegister((byte)AX12RegisterAdd.UpCalibration, 2, ModeAccessRegister.RD,TypeDynamixel.AX12,"Up Calibration"));
            ax12Registers.Add((byte)AX12RegisterAdd.TorqueEnable
                , new BioloidRegister((byte)AX12RegisterAdd.TorqueEnable, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"Torque Enable", 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.LED
                , new BioloidRegister((byte)AX12RegisterAdd.LED, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"LED", 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.CWComplianceMargin
                , new BioloidRegister((byte)AX12RegisterAdd.CWComplianceMargin, 1, ModeAccessRegister.RDetWR,TypeDynamixel.AX12,"CW Compliance Margin", 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CCWComplianceMargin
                , new BioloidRegister((byte)AX12RegisterAdd.CCWComplianceMargin, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "CCW Compliance Margin", 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CWComplianceSlope
                , new BioloidRegister((byte)AX12RegisterAdd.CWComplianceSlope, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "CWComplianceSlope", 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.CCWComplianceSlope
                , new BioloidRegister((byte)AX12RegisterAdd.CCWComplianceSlope, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "CCW Compliance Slope", 0, 0xFE));
            ax12Registers.Add((byte)AX12RegisterAdd.GoalPosition
                , new BioloidRegister((byte)AX12RegisterAdd.GoalPosition, 2, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "Goal Position", 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.MovingSpeed
                , new BioloidRegister((byte)AX12RegisterAdd.MovingSpeed, 2, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "Moving Speed", 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.TorqueLimit
                , new BioloidRegister((byte)AX12RegisterAdd.TorqueLimit, 2, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "Torque Limit", 0, 0x3FF));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentPosition
                , new BioloidRegister((byte)AX12RegisterAdd.PresentPosition, 2, ModeAccessRegister.RD,TypeDynamixel.AX12,"Present Position"));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentSpeed
                , new BioloidRegister((byte)AX12RegisterAdd.PresentSpeed, 2, ModeAccessRegister.RD,TypeDynamixel.AX12,"Present Speed"));
            ax12Registers.Add((byte)AX12RegisterAdd.PresentLoad
                , new BioloidRegister((byte)AX12RegisterAdd.PresentLoad, 1, ModeAccessRegister.RD,TypeDynamixel.AX12,"Present Load"));
            ax12Registers.Add((byte)AX12RegisterAdd.Moving
                , new BioloidRegister((byte)AX12RegisterAdd.Moving, 1, ModeAccessRegister.RD,TypeDynamixel.AX12,"Moving"));
            ax12Registers.Add((byte)AX12RegisterAdd.Lock
                , new BioloidRegister((byte)AX12RegisterAdd.Lock, 1, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "Lock", 0, 0x01));
            ax12Registers.Add((byte)AX12RegisterAdd.Punch
                , new BioloidRegister((byte)AX12RegisterAdd.Punch, 2, ModeAccessRegister.RDetWR, TypeDynamixel.AX12, "Punch", 0, 0x3FF));
        }

        private void LoadingInformationCommune()
        {
            comRegisters.Add((byte)AX12RegisterAdd.ModelNumber
                , new BioloidRegister((byte)AX12RegisterAdd.ModelNumber, 2, ModeAccessRegister.RD,BioloidRegister.ALL_AX,"Model"));
            comRegisters.Add((byte)AX12RegisterAdd.VersionFirmware
                , new BioloidRegister((byte)AX12RegisterAdd.VersionFirmware, 1, ModeAccessRegister.RD, BioloidRegister.ALL_AX,"Firmware"));
            comRegisters.Add((byte)AX12RegisterAdd.ID
                , new BioloidRegister((byte)AX12RegisterAdd.ID, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Identifiant", 0, 0xFD));
            comRegisters.Add((byte)AX12RegisterAdd.BaudRate
                , new BioloidRegister((byte)AX12RegisterAdd.BaudRate, 1, ModeAccessRegister.RDetWR, BioloidRegister.ALL_AX,"BaudRate",0, 0xFE));
            comRegisters.Add((byte)AX12RegisterAdd.ReturnDelayTime
                , new BioloidRegister((byte)AX12RegisterAdd.ReturnDelayTime, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Return Delay Time", 0, 0xFE));
            comRegisters.Add((byte)AX12RegisterAdd.HighestLimitTemperature
                , new BioloidRegister((byte)AX12RegisterAdd.HighestLimitTemperature, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Highest Limit Temperature", 0, 0x96));
            comRegisters.Add((byte)AX12RegisterAdd.LowestLimitVoltage
                , new BioloidRegister((byte)AX12RegisterAdd.LowestLimitVoltage, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Lowest Limit Voltage", 0x32, 0xFA));
            comRegisters.Add((byte)AX12RegisterAdd.HighestLimitVoltage
                , new BioloidRegister((byte)AX12RegisterAdd.HighestLimitVoltage, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Highest Limit Voltage", 0x32, 0xFA));
            comRegisters.Add((byte)AX12RegisterAdd.StatusReturnLevel
                , new BioloidRegister((byte)AX12RegisterAdd.StatusReturnLevel, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Status Return Level", 0, 0x02));
            comRegisters.Add((byte)AX12RegisterAdd.PresentVoltage
                , new BioloidRegister((byte)AX12RegisterAdd.PresentVoltage, 1, ModeAccessRegister.RD,BioloidRegister.ALL_AX,"Present Voltage"));
            comRegisters.Add((byte)AX12RegisterAdd.PresentTemeprature
                , new BioloidRegister((byte)AX12RegisterAdd.PresentTemeprature, 1, ModeAccessRegister.RD,BioloidRegister.ALL_AX,"Present Temperature"));
            comRegisters.Add((byte)AX12RegisterAdd.RegisteredInstruction
                , new BioloidRegister((byte)AX12RegisterAdd.RegisteredInstruction, 1, ModeAccessRegister.RDetWR,BioloidRegister.ALL_AX,"Registered Instruction", 0, 0x01));
        }

        public static AXXRegisterInfo GetInstance() { return _refInternal; }
    
        public BioloidRegister GetAX12Register(AX12RegisterAdd add)
        {
            if (ax12Registers.ContainsKey((byte)add) == true)
            {
                return ax12Registers[(byte)add];
            }
            else if (comRegisters.ContainsKey((byte)add)==true)
            {
                return comRegisters[(byte)add];
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
            else if (comRegisters.ContainsKey((byte)add) == true)
            {
                return comRegisters[(byte)add];
            }
            else
            {
                throw new Exception("Non encore implémentée");
            }
        }


    }

    /// <summary>
    /// Contient une série de méthode apportant une aide au niveau du protocole Dynamixel Bioloid(c)
    /// </summary>
    public static class BioloidCommunicationHelper
    {
        #region Constantes
        /// <summary>
        /// Identifiant dynamixel de broadcast
        /// </summary>
        internal const byte AX_BROADCAST_ID = 254;

        /// <summary>
        /// Contient l'ensemble des identifiants d'instruction dynamixel
        /// </summary>
        private enum DYNAMIXEL_INSTRUCTION : byte
        {
            INSTRUCTION_PING = 0x01,
            INSTRUCTION_READ_DATA = 0x02,
            INSTRUCTION_WRITE_DATA = 0x03,
            INSTRUCTION_REG_WRITE,
            INSTRUCTION_ACTION,
            INSTRUCTION_RESET,
            INSTRUCTION_SYNCWRITE
        }


        private const byte START_BYTE = 0xF0;
        private const byte END_BYTE = 0xF7;

        #endregion

        /// <summary>
        /// Retourne un message SYSEX DYNAMIXEL contenant une demande de lecture d'information pour un 
        /// actuator donné. 
        /// </summary>
        /// <param name="identifiant_dyna">Identifiant de l'actuator</param>
        /// <param name="identifiant_mess">Identifiant de message</param>
        /// <param name="startRegister">Adresse de début de la propriété</param>
        /// <param name="length">Nombre de byte à lire</param>
        /// <returns>Retourne un instruction packet sous format SYSEX</returns>
        public static byte[] CreateReadDataInstruction(byte identifiant_dyna, byte startRegister, byte length)
        {
            byte[] result = new byte[8] {0,0,0,0,0,0,0,0};
            ArduinoBus.Transform8BitTo7Bit((byte)DYNAMIXEL_INSTRUCTION.INSTRUCTION_READ_DATA).CopyTo(result, 0);
            ArduinoBus.Transform8BitTo7Bit(identifiant_dyna).CopyTo(result, 2);
            ArduinoBus.Transform8BitTo7Bit(startRegister).CopyTo(result, 4);
            ArduinoBus.Transform8BitTo7Bit(length).CopyTo(result, 6);
            return result;
        }

        /// <summary>
        /// Création d'un ping instruction. Ce message permet de déterminer si un dynamixel donné
        /// est présent ou non sur le réseau.
        /// Si un status packet est retourné alors on considère que le dynamixel n'est pas présent
        /// </summary>
        /// <param name="identifiant_mess">identifiant message</param>
        /// <param name="identifiant_dyna">identifiant dynamixel</param>
        /// <returns></returns>
        public static byte[] CreatePingInstruction( byte identifiant_dyna)
        {
            byte[] result = new byte[] { 0, 0, 0, 0, 0, 0};
            ArduinoBus.Transform8BitTo7Bit((byte)DYNAMIXEL_INSTRUCTION.INSTRUCTION_PING).CopyTo(result, 0);
            ArduinoBus.Transform8BitTo7Bit(identifiant_dyna).CopyTo(result, 2);
            ArduinoBus.Transform8BitTo7Bit(0).CopyTo(result, 4);
            return result;
        }

        /// <summary>
        /// Permet d'écrire des données au niveau d'un actuator dynamixel
        /// </summary>
        /// <param name="_identifiant_dyna">N° de l'actuator dynamixel</param>
        /// <param name="startRegister">adresse de départ</param>
        /// <param name="dataToWrite">ensembled des données à écrire</param>
        /// <returns></returns>
        public static byte[] WriteInstruction(byte _identifiant_dyna,byte startRegister, byte[] dataToWrite)
        {
            byte lenght = (byte)(6 + dataToWrite.Length * 2);
            byte[] result = new byte[lenght];
            ArduinoBus.Transform8BitTo7Bit((byte)DYNAMIXEL_INSTRUCTION.INSTRUCTION_WRITE_DATA).CopyTo(result, 0);
            ArduinoBus.Transform8BitTo7Bit(_identifiant_dyna).CopyTo(result, 2);
            ArduinoBus.Transform8BitTo7Bit(startRegister).CopyTo(result, 4);
            for (byte loop = 0; loop < (byte)dataToWrite.Length; ++loop)
            {
                ArduinoBus.Transform8BitTo7Bit(dataToWrite[loop]).CopyTo(result, 6 + 2 * loop);
            }
            return result;
        }
        
        /// <summary>
        /// Similar to write_data byt stays in standby mode until the action instruction is given
        /// </summary>
        /// <param name="identifiant_message"></param>
        /// <param name="identifiant_dyna"></param>
        /// <param name="startRegister"></param>
        /// <param name="dataToWrite"></param>
        /// <returns></returns>
        public static byte[] RegWriteInstruction(byte identifiant_dyna,byte startRegister, byte[] dataToWrite)
        {
            byte lenght = (byte)(6 + dataToWrite.Length * 2);
            byte[] result = new byte[lenght];
            ArduinoBus.Transform8BitTo7Bit((byte)DYNAMIXEL_INSTRUCTION.INSTRUCTION_REG_WRITE).CopyTo(result, 0);
            ArduinoBus.Transform8BitTo7Bit(identifiant_dyna).CopyTo(result, 2);
            ArduinoBus.Transform8BitTo7Bit(startRegister).CopyTo(result, 4);
            for (byte loop = 0; loop < (byte)dataToWrite.Length; ++loop)
            {
                ArduinoBus.Transform8BitTo7Bit(dataToWrite[loop]).CopyTo(result, 6 + 2 * loop);
            }
            return result;
        }

        /// <summary>
        /// Permet d'indiquer à l'ensemble des actuators dynamixel qu'ils doivent
        /// exécuter les commandes d'écritures en attente.
        /// - Evites d'allonger le temps d'attente de réponse qui peut être variable.
        /// </summary>
        /// <param name="identifiant_message"></param>
        /// <returns></returns>
        public static byte[] ActionInstruction()
        {
            byte[] result = new byte[] {  0, 0, 0, 0};
            ArduinoBus.Transform8BitTo7Bit((byte)DYNAMIXEL_INSTRUCTION.INSTRUCTION_ACTION).CopyTo(result, 0);
            ArduinoBus.Transform8BitTo7Bit((byte)AX_BROADCAST_ID).CopyTo(result,2);
            return result;
        }

        /// <summary>
        /// Réinitialisation des registres de l'actuator dynamixel à partir des valeurs d'usine.
        /// </summary>
        /// <param name="identifiant_message"></param>
        /// <param name="identifiant_dyna"></param>
        /// <returns></returns>
        public static byte[] ResetInstruction(byte identifiant_dyna)
        {
            byte[] result = new byte[] { 0, 0, 0, 0};
            ArduinoBus.Transform8BitTo7Bit((byte)DYNAMIXEL_INSTRUCTION.INSTRUCTION_RESET).CopyTo(result, 0);
            ArduinoBus.Transform8BitTo7Bit(identifiant_dyna).CopyTo(result, 2);
            return result;
        }
    }
}
