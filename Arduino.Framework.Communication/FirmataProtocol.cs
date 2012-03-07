using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arduino.Framework.Communication.Firmata
{

    #region definition firmata callback : definition
    #region generic callback : definition

    /// <summary>
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="value"></param>
    public delegate void currentDigitalCallback(byte pin, int value);

    /// <summary>
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="value"></param>
    public delegate void currentAnalogCallback(byte pin, int value);

    /// <summary>
    /// </summary>
    /// <param name="pin"></param>
    /// <param name="value"></param>
    public delegate void currentPinModeCallback(byte pin, byte value);

    public delegate void currentPinReportAnalogCallback(byte pin, byte value);

    public delegate void currentReportDigitalCallback(byte pin, byte value);

    public delegate void currentReportVersionCallback(byte minorversion, byte majorversion);
    #endregion

    #region specific callback : definition
    public delegate void currentStringCallback(string myString);
    public delegate void currentSysexCallback(byte sysexCmd, byte[] data);
    public delegate void currentSystemResetCallback();
    public delegate void currentDynamixelCallback(byte[] data);
    #endregion
    #endregion

    public enum TypeMessage : byte
    {
        /// <summary>
        /// Digital message. 1001000
        /// 0x90-0x9F : Gestion de 16 broches numériques
        /// </summary>
        MESSAGE_DIGITAL = 0x90,
        /// <summary>
        /// Ananlog message. 11100000
        /// 0xE0-0xEF : Gestion de 15 broches analogique
        /// </summary>
        MESSAGE_ANALOG = 0xE0,
        /// <summary>
        /// Enable/Disable analog input by pin. 11000000
        /// </summary>
        REPORT_ANALOG = 0xC0,
        /// <summary>
        /// Enable/Disable digital input by port. 11010000
        /// </summary>
        REPORT_DIGITAL = 0xD0,
        /// <summary>
        /// Set a pin input/output/pwm
        /// </summary>
        SET_PIN_MODE = 0xF4,
        /// <summary>
        /// Report firmware veresion 11111001
        /// </summary>
        REPORT_VERSION = 0xF9,
        SYSTEM_RESET = 0xFF,
        SYSEXCMD_DYNAMIXEL_VOID_GENERIC = 0x20,
        SYSEXCMD_DYNAMIXEL_NOVOID_GENERIC = 0x21,
        SYSEXCMD_DYNAMIXEL_STATUS_PACKET = 0x23,
        SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET = 0x24
    }

    public class FirmataProtocol
    {
        /// <summary>
        /// Byte de début de message
        /// </summary>
        public const byte START_SYSEX = 0xF0;
        /// <summary>
        /// Byte de fin de message
        /// </summary>
        public const byte END_SYSEX = 0xF7;

        public const byte MAJORVERSION = 2;
        public const byte MINORVERSION = 2;

        /*
         * Au niveau de firmata les données sont envoyées sur la forme de 7bit
         */
        #region Méthodes de transformation en données firmata

        public static byte[] Transform8BitTo7Bit(byte val)
        {
            return new byte[] { (byte)(val & 0x7F), (byte)((val >> 7) & 0x7F) };
        }

        public static byte Transform7bitTo8bit(byte[] val)
        {
            byte result = 0;
            if (val.Length != 2)
                return byte.MinValue;
            else
            {
                result = (byte)(val[0] + val[1] << 7);
                return result;
            }
        }

        public static byte[] Transform16BitTo7Bit(Int16 val)
        {
            return new byte[] { (byte)((val & 0xFF) & 0x7F)
                             , (byte)(((val & 0xFF) >> 7) & 0x7F)
                             , (byte)((val >> 8) & 0x7F)
                             , (byte)(((val >> 8) >> 7) & 0x7F) 
            };
        }

        public static Int16 Transform7bitTo16bit(byte[] val)
        {
            Int16 result = 0;
            if (val.Length != 4)
            {
                return Int16.MinValue;
            }
            else
            {
                result = (short)(val[0] + (val[1] << 7));
                result += (short)((val[2] << 8) + ((val[3] << 8) << 7));
                return result;
            }
        }

        public static byte[] Transform32BitTo7Bit(Int32 val)
        {
            return new byte[] { (byte)((val & 0xFF) & 0x7F)
                             , (byte)(((val & 0xFF) >> 7) & 0x7F)
                             , (byte)((val >> 8)   & 0x7F)
                             , (byte)(((val >> 8) >> 7) & 0x7F) 
                             , (byte)((val >> 16)   & 0x7F)
                             , (byte)(((val >> 16) >> 7) & 0x7F) 
                             , (byte)((val >> 24)   & 0x7F)
                             , (byte)(((val >> 24) >> 7) & 0x7F)
            };
        }

        public static Int32 Transform7bitTo32bit(byte[] val)
        {
            Int32 result = 0;
            if (val.Length != 8)
                return Int32.MinValue;
            else
            {
                result = val[0] + (val[1] << 7);
                result += (val[2] << 8) + ((val[3] << 8) << 7);
                result += (val[4] << 16) + ((val[5] << 16) << 7);
                result += (val[6] << 24) + ((val[7] << 24) << 7);
                return result;
            }
        }

        public static byte[] Transform64BitTo7Bit(Int64 val)
        {
            return new byte[] { (byte)((val & 0xFF) & 0x7F)
                             , (byte)(((val & 0xFF) >> 7) & 0x7F)
                             , (byte)((val >> 8)   & 0x7F)
                             , (byte)(((val >> 8) >> 7) & 0x7F) 
                             , (byte)((val >> 16)   & 0x7F)
                             , (byte)(((val >> 16) >> 7) & 0x7F) 
                             , (byte)((val >> 24)   & 0x7F)
                             , (byte)(((val >> 24) >> 7) & 0x7F)
                             , (byte)((val >> 32)   & 0x7F)
                             , (byte)(((val >> 32) >> 7) & 0x7F)
                             , (byte)((val >> 40)   & 0x7F)
                             , (byte)(((val >> 40) >> 7) & 0x7F)
                             , (byte)((val >> 48)   & 0x7F)
                             , (byte)(((val >> 48) >> 7) & 0x7F)
                             , (byte)((val >> 56)   & 0x7F)
                             , (byte)(((val >> 56) >> 7) & 0x7F)
            };
        }

        /// <summary>
        /// Transformation d'un integer 64 encodé sous le format sysex en un integer 64
        /// </summary>
        /// <param name="val">integer encodé firmata. attention doit être constitué de 16 éléments</param>
        /// <returns></returns>
        public static Int64 Transform7bitTo64bit(byte[] val)
        {
            Int64 result = 0;
            if (val.Length != 16)
                return Int64.MinValue;
            else
            {
                result = val[0] + (val[1] << 7);
                result += (val[2] << 8) + ((val[3] << 8) << 7);
                result += (val[4] << 16) + ((val[5] << 16) << 7);
                result += (val[6] << 24) + ((val[7] << 24) << 7);
                result += (val[8] << 32) + ((val[9] << 32) << 7);
                result += (val[10] << 40) + ((val[11] << 40) << 7);
                result += (val[12] << 48) + ((val[13] << 48) << 7);
                result += (val[14] << 56) + ((val[15] << 56) << 7);
                return result;
            }
        }

        #endregion

        /// <summary>
        /// Encapsulation d'un message dans un paquet firmata de type SYSEX.
        /// </summary>
        /// <param name="identifiant_message">identifiant du message  - sert pour identifier la réponse</param>
        /// <param name="cmd_sysex">code de l'instruction sysex</param>
        /// <param name="datas">données du message - attention à ce niveau les bytes sont codés sur 8bits</param>
        /// <returns>Tableau de byte représentant le message au format firmata.</returns>
        public static byte[] EncapsulationFirmataMessage(byte identifiant_message, byte cmd_sysex, byte[] datas)
        {
            byte[] result = new byte[datas.Length + 5];
            result[0] = START_SYSEX;
            result[1] = cmd_sysex;
            Transform8BitTo7Bit(identifiant_message).CopyTo(result, 2);
            datas.CopyTo(result, 4);
            result[result.Length - 1] = END_SYSEX;
            return result;
        }

        #region event handler
        public event currentAnalogCallback AnalogEvent;
        public event currentDigitalCallback DigitalEvent;
        public event currentPinModeCallback PinModeEvent;
        public event currentPinReportAnalogCallback PinReportAnalogEvent;
        public event currentReportDigitalCallback PinReportDigitalEvent;
        public event currentReportVersionCallback VersionReportEvent;
        #endregion

    }

}
