using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


/*
* Arduino.cs - Arduino/firmata library for Visual C# .NET
* Copyright (C) 2009 Tim Farley
* 
* Special thanks to David A. Mellis, on whose Processing library
* this code is based.
*
* This library is free software; you can redistribute it and/or
* modify it under the terms of the GNU Lesser General Public
* License as published by the Free Software Foundation; either
* version 2.1 of the License, or (at your option) any later version.
*
* This library is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
* Lesser General Public License for more details.
*
* You should have received a copy of the GNU Lesser General
* Public License along with this library; if not, write to the
* Free Software Foundation, Inc., 59 Temple Place, Suite 330,
* Boston, MA  02111-1307  USA
*
*
* ***************** *
* TODO/KNOWN ISSUES *
* ***************** *
* Exception Handling: At this time there is no exception handling.
* It should be trivial to add exception handling as-needed.
* 
* $Id$
*/

namespace Arduino.Communication.DataAccess
{
 

    /**
     * ArduinoBus.
     * Description: Cette classe permet d'interagir avec une Ardiuno au
     * travers d'un port série, au travers le protocol Firmata
     * Source:Processing Arduino library
     */
    public class ArduinoBus : IDisposable
    {
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

        #region generic callback : implementation
        public currentAnalogCallback AnalogCallback;
        public currentDigitalCallback DigitalCallback;
        public currentPinModeCallback PinModeCallback;
        public currentPinReportAnalogCallback PinReportAnalogCallback;
        public currentReportDigitalCallback PinReportDigitalCallback;
        public currentReportVersionCallback ReportVersionCallback;
        #endregion

        #region specific callback : implementation
        public currentStringCallback StringCallback;
        internal currentSysexCallback SysexCallback;
        public currentSystemResetCallback SystemResetCallback;
        public currentDynamixelCallback DynamixelCallback;
        #endregion

        public static int INPUT = 0;
        public static int OUTPUT = 1;
        public static int LOW = 0;
        public static int HIGH = 1;

        #region Constantes Message
        private const int MAX_DATA_BYTES = 32;
        /// <summary>
        /// Digital message. 
        /// 1001000
        /// 0x90-0x9F => Gestion de 15 broche numérique 0 -> 15
        /// </summary>
        private const int ID_MESS_DIGITAL = 0x90; // send data for a digital port
        /// <summary>
        /// Analog Message.
        /// 11100000
        /// 0xE0-0xEF => Gestion de 15 broche analogique 0 -> 15
        /// </summary>
        private const int ID_MESS_ANALOG = 0xE0; // send data for an analog pin (or PWM)
        /// <summary>
        /// Enable/Disable Analog input by pin #.
        /// 11000000
        /// </summary>
        private const int REPORT_ANALOG = 0xC0; // enable analog input by pin #
        /// <summary>
        /// Enable/Disable Digital input by port
        /// 11010000
        /// </summary>
        private const int REPORT_DIGITAL = 0xD0; // enable digital input by port
        /// <summary>
        /// Set a pin INPUT/OUTPU/PWM/...
        /// 11110100
        /// </summary>
        private const int SET_PIN_MODE = 0xF4; // set a pin to INPUT/OUTPUT/PWM/etc
        /// <summary>
        /// Report firmware version
        /// 11111001
        /// </summary>
        private const int REPORT_VERSION = 0xF9; // report firmware version
        /// <summary>
        /// Reset From MIDI
        /// 11111111
        /// </summary>
        private const int SYSTEM_RESET = 0xFF; // reset from MIDI
        /// <summary>
        /// Start A midi SysEx Message
        /// 11110000
        /// </summary>
        public const int START_SYSEX = 0xF0; // start a MIDI SysEx message
        /// <summary>
        /// 11110111
        /// </summary>
        public const int END_SYSEX = 0xF7; // end a MIDI SysEx message

        /// <summary>
        /// Message de type dynamixel sans attente de la réponse
        /// </summary>
        public const byte SYSEXCMD_DYNAMIXEL_VOID_GENERIC = 0x20;
        /// <summary>
        /// Message de type dynamixel. Correspond à la trame réponse retournée par l'actuator
        /// </summary>
        public const byte SYSEXCMD_DYNAMIXEL_STATUS_PACKET = 0x23;
        /// <summary>
        /// Message de type dynamixel. Correspond à une trame envoyée sur le réseau dynamixel à destination 
        /// de 1/n actuator.
        /// </summary>
        public const byte SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET = 0x24;

        #endregion 

        private SerialPort _serialPort;
        private int delay;

        private int waitForData = 0;
        private int executeMultiByteCommand = 0;
        private byte multiByteChannel = 0;
        private byte[] storedInputData = new byte[MAX_DATA_BYTES];
        private bool parsingSysex;
        private int sysexBytesRead;

        private volatile int[] digitalOutputData = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private volatile int[] digitalInputData = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private volatile int[] analogInputData = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private int majorVersion = 0;
        private int minorVersion = 0;
        

        private Task readTask = null;

        private volatile bool _shouldStop; // indique si l'on arrête la surveillance du port de communication

        private object locker = new object();

        public void FreeToSendMessage()
        {
            while (_serialPort.BytesToRead > 0) ;
        }


        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serialPortName">String specifying the name of the serial port. eg COM4</param>
        /// <param name="baudRate">The baud rate of the communication. Default 115200</param>
        /// <param name="autoStart">Determines whether the serial port should be opened automatically.
        ///                     use the Open() method to open the connection manually.</param>
        /// <param name="delay">Time delay that may be required to allow some arduino models
        ///                     to reboot after opening a serial connection. The delay will only activate
        ///                     when autoStart is true.</param>
        /// <param name="startSurvey">Indique si la surveillance du bus de données démarre automatiquement à la fin
        /// de l'initialisation de l'instance</param>
        public ArduinoBus(string serialPortName, Int32 baudRate, bool autoStart, int delay,bool startSurvey)
        {
            _serialPort = new SerialPort(serialPortName, baudRate);
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;

            if (autoStart)
            {
                this.delay = delay;
                this.Open(startSurvey);
            }
        }

        /// <summary>
        /// Creates an instance of the Arduino object, based on a user-specified serial port.
        /// Assumes default values for baud rate (115200) and reboot delay (8 seconds)
        /// and automatically opens the specified serial connection.
        /// </summary>
        /// <param name="serialPortName">String specifying the name of the serial port. eg COM4</param>
        public ArduinoBus(string serialPortName) : this(serialPortName, 115200, true, 8000,false) { }

        /// <summary>
        /// Creates an instance of the Arduino object, based on user-specified serial port and baud rate.
        /// Assumes default value for reboot delay (8 seconds).
        /// and automatically opens the specified serial connection.
        /// </summary>
        /// <param name="serialPortName">String specifying the name of the serial port. eg COM4</param>
        /// <param name="baudRate">Baud rate.</param>
        public ArduinoBus(string serialPortName, Int32 baudRate) : this(serialPortName, baudRate, true, 8000,false) { }

        /// <summary>
        /// Creates an instance of the Arduino object using default arguments.
        /// Assumes the arduino is connected as the HIGHEST serial port on the machine,
        /// default baud rate (115200), and a reboot delay (8 seconds).
        /// and automatically opens the specified serial connection.
        /// </summary>
        public ArduinoBus() : this(ArduinoBus.list().ElementAt(list().Length - 1), 115200, true, 8000,false) { }
        #endregion

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
        /// Active la surveillance sur le bus de données
        /// </summary>
        public void StartSurveyDataArrived()
        {
            _shouldStop = true;
        }
        /// <summary>
        /// Désactive la surveillance
        /// </summary>
        public void StopSurveyDataArrived()
        {
            _shouldStop = false;
                
        }
        /// <summary>
        /// Opens the serial port connection, should it be required. By default the port is
        /// opened when the object is first created.
        /// </summary>
        /// <param name="startSurveyBus">Indique si la surveillance des données
        /// arrivant sur le bus de données démarre à la fin de cette méthode</param>
        public void Open(bool startSurveyBus)
        {
            try
            {
                _serialPort.Open();
            }
            catch(System.Exception ex)
            {
                throw new System.Exception("erreur",ex);
            }
            Thread.Sleep(delay);

            byte[] command = new byte[2];

            // Par défaut l'ensemble des ports analog sont surveillés
            for (int i = 0; i < 6; i++)
            {
                command[0] = (byte)(REPORT_ANALOG | i);
                command[1] = (byte)1;
                _serialPort.Write(command, 0, 2);
            }

            for (int i = 0; i < 2; i++)
            {
                command[0] = (byte)(REPORT_DIGITAL | i);
                command[1] = (byte)1;
                _serialPort.Write(command, 0, 2);
            }
            command = null;

            //Le processus de traitement des messages arrivée se fait via autre thread
            if (readTask == null && startSurveyBus == true)
            {
                readTask = new Task(processInput);
                readTask.Start();
            }
        }
        
        /// <summary>
        /// Closes the serial port.
        /// </summary>
        public void Close()
        {
            Task.WaitAll(readTask);
            readTask.Dispose();
            _serialPort.Close();
        }

        /// <summary>
        /// Lists all available serial ports on current system.
        /// </summary>
        /// <returns>An array of strings containing all available serial ports.</returns>
        public static string[] list()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Permet d'envoyer une trame de byte sur le bus Arduino
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool writeBytes(byte[] data)
        {
            try
            {
                _serialPort.Write(data, 0, data.Length);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the last known state of the digital pin.
        /// </summary>
        /// <param name="pin">The arduino digital input pin.</param>
        /// <returns>Arduino.HIGH or Arduino.LOW</returns>
        public int digitalRead(int pin)
        {
            return (digitalInputData[pin >> 3] >> (pin & 0x07)) & 0x01;
        }

        /// <summary>
        /// Returns the last known state of the analog pin.
        /// </summary>
        /// <param name="pin">The arduino analog input pin.</param>
        /// <returns>A value representing the analog value between 0 (0V) and 1023 (5V).</returns>
        public int analogRead(int pin)
        {
            return analogInputData[pin];
        }

        /// <summary>
        /// Sets the mode of the specified pin (INPUT or OUTPUT).
        /// </summary>
        /// <param name="pin">The arduino pin.</param>
        /// <param name="mode">Mode Arduino.INPUT or Arduino.OUTPUT.</param>
        public void pinMode(int pin, int mode)
        {
            byte[] message = new byte[3];
            message[0] = (byte)(SET_PIN_MODE);
            message[1] = (byte)(pin);
            message[2] = (byte)(mode);
            _serialPort.Write(message, 0, 3);
            message = null;
        }

        /// <summary>
        /// Write to a digital pin that has been toggled to output mode with pinMode() method.
        /// </summary>
        /// <param name="pin">The digital pin to write to.</param>
        /// <param name="value">Value either Arduino.LOW or Arduino.HIGH.</param>
        public void digitalWrite(byte pin, int value)
        {
            int portNumber = (pin >> 3) & 0x0F;
            byte[] message = new byte[3];

            if (value == 0)
                digitalOutputData[portNumber] &= ~(1 << (pin & 0x07));
            else
                digitalOutputData[portNumber] |= (1 << (pin & 0x07));

            message[0] = (byte)(ID_MESS_DIGITAL | portNumber);
            message[1] = (byte)(digitalOutputData[portNumber] & 0x7F);
            message[2] = (byte)(digitalOutputData[portNumber] >> 7);
            _serialPort.Write(message, 0, 3);
        }

        /// <summary>
        /// Indique l'état de la broche numérique qui sera transféré.
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="value"></param>
        public void digitalValue(byte pin, int value)
        {
            int portNumber = (pin >> 3) & 0x0F;
            
            if (value == 0)
                digitalOutputData[portNumber] &= ~(1 << (pin & 0x07));
            else
                digitalOutputData[portNumber] |= (1 << (pin & 0x07));
        }

        /// <summary>
        /// Envoie l'état de l'ensemble des broches numériques.
        /// </summary>
        /// <param name="pin"></param>
        public void digitalWrite(byte pin)
        {
            byte[] message = new byte[3];
            int portNumber = (pin >> 3) & 0x0F;
            message[0] = (byte)(ID_MESS_DIGITAL | portNumber);
            message[1] = (byte)(digitalOutputData[portNumber] & 0x7F);
            message[2] = (byte)(digitalOutputData[portNumber] >> 7);
            if ((_serialPort != null) && (_serialPort.IsOpen))
                _serialPort.Write(message, 0, 3);
            else
                throw new Exception("Problème au niveau du bus de données");
        }

        /// <summary>
        /// Write to an analog pin using Pulse-width modulation (PWM).
        /// </summary>
        /// <param name="pin">Analog output pin.</param>
        /// <param name="value">PWM frequency from 0 (always off) to 255 (always on).</param>
        public void analogWrite(int pin, int value)
        {
            byte[] message = new byte[3];
            message[0] = (byte)(ID_MESS_ANALOG | (pin & 0x0F));
            message[1] = (byte)(value & 0x7F);
            message[2] = (byte)(value >> 7);
            if ((_serialPort != null) && (_serialPort.IsOpen))
                _serialPort.Write(message, 0, 3);
            else
                throw new Exception("Erreur au niveau du bus de communication");
        }

        // Définit la valeur pour un port numérique donné.
        // Cette méthode est appelée après la réception d'un message sur le bus concernant
        // la valeur pour une broche numérique (0 ou 1) 
        private void setDigitalInputs(byte portNumber, int portData)
        {
            digitalInputData[portNumber] = portData;
        }

        /// <summary>
        ///  Définit la valuer pour une broche analogique donnée.
        ///  Cette méthode est appelée lors de la réception d'un message sur le bus concernant
        ///  la valeur pour une broche analogique (0 - 1014 fonction de la précision)
        /// </summary>
        /// <param name="pin">Numéro de la broche lue</param>
        /// <param name="value">Valeur analogique lue</param>
        private void setAnalogInput(byte pin, int value)
        {
            analogInputData[pin] = value;
        }

        /// <summary>
        /// Définit la version du firmewar avec lequel fonctionne cette version de bus
        /// </summary>
        /// <param name="majorVersion"></param>
        /// <param name="minorVersion"></param>
        private void setVersion(int majorVersion, int minorVersion)
        {
            this.majorVersion = majorVersion;
            this.minorVersion = minorVersion;
        }

        /// <summary>
        /// Indique s'il y a des données disponible sur le bus de données.
        /// </summary>
        /// <returns>Nombre de byte en attente de lecture au niveau du buffer</returns>
        private int available()
        {
            return _serialPort.BytesToRead;
        }

        #region Traitement des messages qui arrivent sur le bus de données

        /// <summary>
        /// Méthode qui s'occupe des messages de types SYSEX.
        /// Ce sont des messages complexes
        /// </summary>
        private void processSysexMessage()
        {
            switch (storedInputData[0]) // first byte in buffer is command
            {
                case SYSEXCMD_DYNAMIXEL_STATUS_PACKET: // correspond à un status packet
                    if (this.SysexCallback != null)
                    {
                        byte bufferLength =(byte)( (sysexBytesRead - 1) / 2);
                        byte[] buffer = new byte[bufferLength];
                        byte i = 1;
                        byte j = 0;
                        while (j < bufferLength) // Traitement car transmission sur 7-bit
                        {
                            buffer[j] = (byte)storedInputData[i];
                            i++;
                            buffer[j] += (byte)(storedInputData[i]<<7);
                            i++;
                            j++;
                        }
                        this.SysexCallback(SYSEXCMD_DYNAMIXEL_STATUS_PACKET,buffer);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Procédure d'analyse des bytes arrivant sur le bus de données
        /// </summary>
        public void processInput()
        {
            while (_serialPort.IsOpen && !_shouldStop)
            {
                //> Vérification qu'il y a des don  nées arrivées
                if (_serialPort.BytesToRead > 0)
                {
                    lock (this)
                    {
                        //lecture d'un byte
                        byte inputData = (byte)_serialPort.ReadByte();
                        byte command;

                        //>> vérification si on est en cours de réception d'un message
                        // particulier (sysex)
                        if (parsingSysex)
                        {
                            //> vérification si on vient de recevoir l'indicateur de fin de message
                            if (inputData == END_SYSEX)
                            {
                                // stop sysex byte
                                parsingSysex = false;
                                processSysexMessage();
                            }
                            else
                            {
                                //> on stocke la donnée et on incrémente l'index
                                storedInputData[sysexBytesRead] = inputData;
                                sysexBytesRead++;
                            }
                        }
                        else if (waitForData > 0 && inputData < 0x80)
                        {
                            waitForData--; //> 
                            storedInputData[waitForData] = inputData;

                            if (executeMultiByteCommand != 0 && waitForData == 0)
                            {
                                //Nous venons de recevoir le dernier byte donc exécution de la fonction
                                switch (executeMultiByteCommand)
                                {
                                    case ID_MESS_DIGITAL:
                                        if (this.DigitalCallback != null)
                                            DigitalCallback(multiByteChannel, (storedInputData[0] << 7) + storedInputData[1]);
                                        setDigitalInputs(multiByteChannel, (storedInputData[0] << 7) + storedInputData[1]);
                                        break;
                                    case ID_MESS_ANALOG:
                                        if(this.AnalogCallback != null )
                                            AnalogCallback(multiByteChannel,(storedInputData[0] << 7) + storedInputData[1]);
                                        setAnalogInput(multiByteChannel, (storedInputData[0] << 7) + storedInputData[1]);
                                        break;
                                    case SET_PIN_MODE :
                                        if (this.PinModeCallback != null)
                                            PinModeCallback(storedInputData[1], storedInputData[0]);
                                        break;
                                    case REPORT_ANALOG:
                                        if (this.PinReportAnalogCallback!=null)
                                            PinReportAnalogCallback(multiByteChannel, storedInputData[0]);
                                        break;
                                    case REPORT_DIGITAL:
                                        if (this.PinReportDigitalCallback != null)
                                            PinReportDigitalCallback(multiByteChannel, storedInputData[0]);
                                        break;
                                    case REPORT_VERSION:
                                        if (ReportVersionCallback !=null)
                                            ReportVersionCallback(storedInputData[1], storedInputData[0]);
                                        break;
                                }
                                executeMultiByteCommand = 0;
                            }
                        }
                        else // -- nouveau message 
                        {
                            if (inputData < START_SYSEX)
                            {
                                //> pas message sysex.
                                //> On récupère le numéro de la commande et le numéro byte
                                command = (byte)(inputData & START_SYSEX);
                                multiByteChannel = (byte)(inputData & START_SYSEX);
                            }
                            else
                            {
                                //> message sysex
                                command = inputData;
                                // commands in the 0xF* range don't use channel data
                            }
                            //> En fonction du message on initialise le nombre de byte attendus...
                            switch (command)
                            {
                                case ID_MESS_ANALOG:
                                case ID_MESS_DIGITAL:
                                case SET_PIN_MODE:
                                case REPORT_VERSION:
                                    waitForData = 2; //> Message de deux bytes
                                    executeMultiByteCommand = command;
                                    break;
                                case REPORT_ANALOG:
                                case REPORT_DIGITAL:
                                    waitForData = 1; //> Message de un byte
                                    executeMultiByteCommand = command;
                                    break;
                                case START_SYSEX:
                                    parsingSysex = true; //> Message de type SysEx
                                    sysexBytesRead = 0; //> Indique le type de byte lus
                                    break;                                    
                            } // endif END_SYSEX
                        } // endif PARSINGSYSEX
                    } // endlock
                } // endif
            } // endwhile
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Libération des instances managees
                //this._serialPort.Dispose();
            }
            // Liberation des instances non managées
            this._serialPort.Dispose();
        }
        
        /// <summary>
        /// Représente la méthode finalize. 
        /// Cette méthode est appelée lorsque l'instance est libérée alors qu'il
        /// n'y a pas eu appelle à la méthode Dispose de l'instance.
        /// </summary>
        ~ArduinoBus()
        {
            Dispose(false);
        }
    } // End Arduino class

}
