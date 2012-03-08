using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arduino.Communication.DataAccess;

namespace Arduino.Framework.Communication
{
    /// <summary>
    /// Instance qui s'occupe de gérer les différents traitements suites à la réception d'un message données.
    /// -Attention pour le moment cette classe n'est pensée que pour la gestion des messages provenant du bus Firmata
    /// </summary>
    public class OrchestraMessage
    {

        private static OrchestraMessage _refInternal = new OrchestraMessage();
        private byte idMessage;
        private static object lockref = new object();

        private Dictionary<int, ArduinoBus.currentSysexCallback> delegateSysexResponse 
            = new Dictionary<int, ArduinoBus.currentSysexCallback>();

        private Dictionary<int, ArduinoBus.currentAnalogCallback> delegateAnalogRequest
            = new Dictionary<int, ArduinoBus.currentAnalogCallback>();

        private OrchestraMessage() { idMessage = 0; }

        public static OrchestraMessage GetInstance()
        {
            lock (lockref)
            {
                if (_refInternal != null)
                {
                    return _refInternal;
                }
                else
                {
                    _refInternal = new OrchestraMessage();
                    return _refInternal;
                }
            }
        }

        #region AnalogMessageTraitement

        /// <summary>
        /// Permet de stocker un traitement pour un message donné sera identifié par 
        /// idMessage et Seed (dans le cadre des message analog cela correspondra à l'identifiant
        /// de ce dernier).
        /// </summary>
        /// <param name="pin">Pin Analogique</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public byte? GetAnalogReferenceMessage(byte pin, ArduinoBus.currentAnalogCallback callback)
        {
            if (callback != null)
            {
                lock (lockref)
                {
                    while (delegateAnalogRequest.ContainsKey((pin << 8) | idMessage))
                    {
                        if (idMessage == byte.MaxValue)
                            idMessage = 0;
                        else
                            ++idMessage;
                    }
                    delegateAnalogRequest.Add((pin << 8) | idMessage, callback);
                    return idMessage;
                }
            }
            else
                return null;
        }

        /// <summary>
        /// Cette méthode se charge d'appeler la fonction à exécuter lors de l'arrivée de la réponse
        /// à un précédent message retourné
        /// </summary>
        /// <param name="pin"></param>
        /// <param name="identifiantMessage"></param>
        /// <param name="val"></param>
        public void ValidateAnalogResponseMessage(int identifiantMessage, UInt16 val)
        {
            lock(lockref)
            {
                byte pin = 0;
                byte idmessage = 0;
                pin = (byte)((identifiantMessage >> 8) & 0xFF);
                idmessage = (byte)(identifiantMessage & 0xFF);

                if(delegateAnalogRequest.ContainsKey(identifiantMessage) && delegateAnalogRequest[identifiantMessage]!=null)
                {
                    ArduinoBus.currentAnalogCallback tmp = delegateAnalogRequest[identifiantMessage];
                    delegateAnalogRequest.Remove(identifiantMessage);
                    tmp(pin,val);
                }
            }
        }

        public void DeleteAnalogResponseMessage(int identifiantMessage)
        {
            lock (lockref)
            {
                if (delegateAnalogRequest.ContainsKey(identifiantMessage))
                {
                    ArduinoBus.currentAnalogCallback tmp = delegateAnalogRequest[identifiantMessage];
                    if (delegateAnalogRequest.Remove(identifiantMessage))
                    {
                        tmp = null;
                    }
                }
            }
        }

        #endregion

        #region SysexMessageTraitement

        /// <summary>
        /// Permet de stocker un traitement pour un message donné qui sera identifié par 
        /// idMessage et Seed (dans le cadre des messages dynamixel cela correspondra à l'identifiant
        /// de ce dernier).
        /// </summary>
        /// <param name="identifiant_dynamixel">Identifiant dynamixel concerné par le message</param>
        /// <param name="callback">Méthode qui doit être appelée après réception d'un message </param>
        /// <returns>Identifiant du message ou bien null</returns>
        public byte? GetSysexReferenceMessage(byte identifiant_dynamixel,ArduinoBus.currentSysexCallback callback)
        {
            if (callback != null)
            {
                lock (lockref)
                {
                    while (delegateSysexResponse.ContainsKey((identifiant_dynamixel << 8) | idMessage))
                    {
                        if (idMessage == byte.MaxValue)
                            idMessage = 0;
                        else
                            ++idMessage;
                    }
                    delegateSysexResponse.Add((identifiant_dynamixel << 8) | idMessage, callback);
                    return idMessage;
                }
            }
            else
                return null;
        }

        /// <summary>
        /// Cette méthode se charge d'appeler la fonction a exécuté lors de l'arrivée de la réponse 
        /// à un précédent message retourné.
        /// </summary>
        /// <param name="identifiantMessage">Identifiant du message</param>
        /// <param name="sysexCmd">Code de la commande sysex</param>
        /// <param name="datas">ensemble des données du message</param>
        public void ValidateSysexResponseMessage(byte cmdsysex,int identifiantMessage,byte[] datas)
        {
            lock (lockref)
            {
                if (delegateSysexResponse.ContainsKey(identifiantMessage) && delegateSysexResponse[identifiantMessage] != null)
                {
                    ArduinoBus.currentSysexCallback tmp = delegateSysexResponse[identifiantMessage];
                    delegateSysexResponse.Remove(identifiantMessage);
                    if (datas != null)
                    {
                        tmp(cmdsysex,datas);
                    }
                    else
                    {
                        tmp(cmdsysex, null);
                    }
                }
            }
        }
    
    
        public void DeleteSysexReferenceMessage(int identifiant_message)
        {
            lock (lockref)
            {
                if (delegateSysexResponse.ContainsKey(identifiant_message))
                {
                    ArduinoBus.currentSysexCallback tmp = delegateSysexResponse[identifiant_message];
                    if (delegateSysexResponse.Remove(identifiant_message))
                    {
                        tmp = null;
                    }
                    else
                    {
                        // voir ce que l'on fait dans ce cas.
                    }
                }
            }
        }

        #endregion
    }
}
