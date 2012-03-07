using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arduino.Communication.DataAccess;

namespace Arduino.Framework.Communication
{
    public class ServiceFirmata
    {
        #region DataMembers
        
        private AXXRegisterInfo registersActuatorDynamixel;

        private OrchestraMessage _orchestramessage;
        
        private ArduinoBus _arduinoBus;

        private static ServiceFirmata _internalRef;
        
        #endregion

        private ServiceFirmata(string portname)
        {
            this._orchestramessage = OrchestraMessage.GetInstance();
            this._arduinoBus = new ArduinoBus(portname, 57600,false,8000,true);
            this._arduinoBus.SysexCallback = sysexTraitment;
            this._arduinoBus.Open(true);
            this.registersActuatorDynamixel = AXXRegisterInfo.GetInstance();
        }

        #region Methods Static

        public static ServiceFirmata GetInstance(string portName)
        {
            if (_internalRef == null)
            {
                _internalRef = new ServiceFirmata(portName);
            }
            return _internalRef;
        }

        /// <summary>
        /// Encapsulation d'un message dans un paquet Firmata de type SYSEX
        /// </summary>
        /// <param name="dyna_paquet">données à encapsuler</param>
        /// <param name="identifiant_message">code message</param>
        /// <param name="cmd_sysex">code de la commande sysex</param>
        /// <returns></returns>
        private static byte[] EncapsulationMessage(byte[] dyna_paquet, byte identifiant_message, byte cmd_sysex)
        {
            byte[] result = new byte[dyna_paquet.Length + 5];
            result[0] = ArduinoBus.START_SYSEX;
            result[1] = cmd_sysex;
            ArduinoBus.Transform8BitTo7Bit(identifiant_message).CopyTo(result, 2);
            dyna_paquet.CopyTo(result, 4);
            result[result.Length - 1] = ArduinoBus.END_SYSEX;
            return result;
        }

        /// <summary>
        /// Encapsulation d'un message d'un paquet Firmata de typ Sysex
        /// Sans gestion de message;
        /// </summary>
        /// <param name="dyna_paquet">donnée à encapsuler</param>
        /// <param name="cmd_sysex">code de la commande sysex qui sera traitée</param>
        /// <returns></returns>
        private static byte[] EncapsulationMessage(byte[] dyna_paquet, byte cmd_sysex)
        {
            byte[] result = new byte[dyna_paquet.Length + 3];
            result[0] = ArduinoBus.START_SYSEX;
            result[1] = cmd_sysex;
            dyna_paquet.CopyTo(result, 2);
            result[result.Length - 1] = ArduinoBus.END_SYSEX;
            return result;
        }
        #endregion

        #region Methods Private

        /// <summary>
        /// C'est à ce niveau que l'on va implémenter l'ensemble des traitements suite
        /// à commande de type sysex
        /// </summary>
        /// <param name="command"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        private void sysexTraitment(byte command, byte[] data)
        {
            switch (command)
            {
                case ArduinoBus.SYSEXCMD_DYNAMIXEL_STATUS_PACKET:
                    if ((data != null) && (data.Length > 1))
                    {
                        byte identifiant_message = data[0];
                        byte identifiant_dynamixel = data[1];
                        byte error = data[2];
                        if (error == 0)
                        {
                            //> On recherche s'il y a une méthode pour ce traitement;
                            _orchestramessage.ValidateResponseMessage(command
                                , ((identifiant_dynamixel << 8) + identifiant_message), data);
                        }
                        else
                        {
                            //> Todo mettre en place la gestion d'erreur 
                        }
                    }
                    break;
            }
        }

        #endregion

        public AX12Dynamixel CreateDynamixel(byte identifiant_dynamixel)
        {
            AX12Dynamixel result = new AX12Dynamixel(this, identifiant_dynamixel);
            return result;
        }
        
        public BioloidRegister GetAX12Register(AX12RegisterAdd register) { return this.registersActuatorDynamixel.GetAX12Register(register); }

        public BioloidRegister GetAXS1Register(AXS1RegisterAdd register) { return this.registersActuatorDynamixel.GetAXS1Register(register); }

        #region Methods SendXInstructionMessage
    
        /// <summary>
        /// Permet de transmettre un message de type READ
        /// </summary>
        /// <param name="identifiant_dynamixel">identifiant dynamixel concerné</param>
        /// <param name="add">Adresse de début du registre à lire</param>
        /// <param name="length">Nombre de byte à lire</param>
        /// <param name="traitement">Méthode qui sera appelée lors du retour</param>
        internal void SendReadInstructionMessage(byte identifiant_dynamixel,BioloidRegister add,byte length, ArduinoBus.currentSysexCallback traitement)
        {
            //> Création d'un paquet ReadInstructionMessage
            byte[] datas = BioloidCommunicationHelper.CreateReadDataInstruction(identifiant_dynamixel, add.StartAdressRegister
                                                                       , (byte)(add.Length + length));
            //> Recuperation d'un nouvel index de message
            byte? identifiant_message = _orchestramessage.GetReferenceMessage(identifiant_dynamixel,traitement);
            if (identifiant_message.HasValue)
            {
                try
                {
                    byte[] message = ServiceFirmata.EncapsulationMessage(datas, identifiant_message.Value, ArduinoBus.SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET);
                    this._arduinoBus.writeBytes(message);
                }
                catch (Exception ex)
                {
                    _orchestramessage.DeleteReferenceMessage(identifiant_message.Value);
                }
            }
            else
            {
                // prevoir ce que l'on fait dans ce cas
            }
        }

        /// <summary>
        /// Permet de transmettre un message de type PING
        /// </summary>
        /// <param name="identifiant_dynamixel">identifiant dynamixel concerné</param>
        /// <param name="traitement_result">Méthode qui sera appelée lors du retour</param>
        internal void SendPingInstructionMessage(byte identifiant_dynamixel, ArduinoBus.currentSysexCallback traitement_result)
        {
            byte[] datas = BioloidCommunicationHelper.CreatePingInstruction(identifiant_dynamixel);
            //> Récupération d'un nouvel index de message
            byte? identifiant_message = _orchestramessage.GetReferenceMessage(identifiant_dynamixel, traitement_result);
            if (identifiant_message.HasValue)
            {
                try
                {
                    byte[] message = ServiceFirmata.EncapsulationMessage(datas, identifiant_message.Value, ArduinoBus.SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET);
                    this._arduinoBus.writeBytes(message);
                }
                catch (Exception ex)
                {
                    _orchestramessage.DeleteReferenceMessage(identifiant_message.Value);
                }
            }
            else
            {
                //> TODO prévoir ce que l'on fait dans ce cas
            }
        }
        
        /// <summary>
        /// Permet de transmettre un mesage de type Write
        /// </summary>
        /// <param name="identifiant_dynamixel">identifiant dynamixel concerné</param>
        /// <param name="add">Registre de début concerné par l'écriture</param>
        /// <param name="datas">Ensemble des données inscrites à partir de <paramref name="idenfiant_dyanmixel"/>l'adresse de début</param>
        /// <param name="traitement_result">Méthode qui sera appelée lors du retour</param>
        internal void SendWriteInstructionMessage(byte identifiant_dynamixel, BioloidRegister add, byte[] datas, ArduinoBus.currentSysexCallback traitement_result)
        {
            //> Création du message dynamixel
            byte[] dyna_paquet = BioloidCommunicationHelper.WriteInstruction(identifiant_dynamixel, add.StartAdressRegister, datas);
            //> Récupération d'un nouvel index de message
            byte? identifiant_message = _orchestramessage.GetReferenceMessage(identifiant_dynamixel, traitement_result);
            if (identifiant_message.HasValue)
            {
                try
                {
                    byte[] message = ServiceFirmata.EncapsulationMessage(dyna_paquet, identifiant_message.Value, ArduinoBus.SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET);
                    this._arduinoBus.writeBytes(message);
                }
                catch (Exception ex)
                {
                    _orchestramessage.DeleteReferenceMessage(identifiant_message.Value);
                }
            }
            else
            {
                //> Prévoir ce que l'on fait à ce niveau 
            }
        }

        /// <summary>
        /// Envoi un message à destination du bus dynamixel sans traitement de la réponse
        /// </summary>
        /// <param name="identifiant_dynamixel"></param>
        /// <param name="add"></param>
        /// <param name="datas"></param>
        internal void SendWriteInstructionMessage(byte identifiant_dynamixel, BioloidRegister add, byte[] datas)
        {
            byte[] dyna_paquet = BioloidCommunicationHelper.WriteInstruction(identifiant_dynamixel, add.StartAdressRegister, datas);
            try
            {
                byte[] message = ServiceFirmata.EncapsulationMessage(dyna_paquet, ArduinoBus.SYSEXCMD_DYNAMIXEL_VOID_GENERIC);
                this._arduinoBus.writeBytes(message);
            }
            catch (Exception ex)
            {

            }
        }

        internal void SendRegWriteInstructionMessage(byte identifiant_dynamixel, BioloidRegister add, byte[] datas, ArduinoBus.currentSysexCallback traitement_result)
        {
            byte[] dyna_paquet = BioloidCommunicationHelper.RegWriteInstruction(identifiant_dynamixel, add.StartAdressRegister, datas);
            byte? identifiant_message = _orchestramessage.GetReferenceMessage(identifiant_dynamixel, traitement_result);
            if (identifiant_message.HasValue)
            {
                try
                {
                    byte[] message = ServiceFirmata.EncapsulationMessage(dyna_paquet, identifiant_message.Value, ArduinoBus.SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET);
                    this._arduinoBus.writeBytes(message);
                }
                catch (Exception ex)
                {
                    _orchestramessage.DeleteReferenceMessage(identifiant_message.Value);
                }
            }
            else
            {
                //> Prévoir ce que l'on fait à ce niveau
            }
        }

        internal void SendActionInstructionMessage(ArduinoBus.currentSysexCallback traitement_result)
        {
            byte[] dyna_paquet = BioloidCommunicationHelper.ActionInstruction();
            byte? identifiant_message = _orchestramessage.GetReferenceMessage(BioloidCommunicationHelper.AX_BROADCAST_ID,traitement_result);
            if (identifiant_message.HasValue)
            {
                try
                {
                    byte[] message = ServiceFirmata.EncapsulationMessage(dyna_paquet, identifiant_message.Value, ArduinoBus.SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET);
                    this._arduinoBus.writeBytes(message);
                }
                catch (Exception ex)
                {
                    _orchestramessage.DeleteReferenceMessage(identifiant_message.Value);
                }
            }
            else
            {
                //> prévoir ce que l'on fait à ce niveau
            }
        }

        internal void SendResetInstructionMessage(byte identifiant_dynamixel, ArduinoBus.currentSysexCallback traitement_result)
        {
            byte[] dyna_paquet = BioloidCommunicationHelper.ResetInstruction(identifiant_dynamixel);
            byte? identifiant_message = _orchestramessage.GetReferenceMessage(identifiant_dynamixel, traitement_result);
            if (identifiant_message.HasValue)
            {
                try
                {
                    byte[] message = ServiceFirmata.EncapsulationMessage(dyna_paquet, identifiant_message.Value, ArduinoBus.SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET);
                    this._arduinoBus.writeBytes(message);
                }
                catch (Exception ex)
                {
                    _orchestramessage.DeleteReferenceMessage(identifiant_message.Value);
                }
            }
            else
            {
                //> prévoir ce que l'on fait à ce niveau
            }
        }
        
        #endregion

    }
}
