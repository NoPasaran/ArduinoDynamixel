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

        private Dictionary<int, ArduinoBus.currentSysexCallback> delegateResponse 
            = new Dictionary<int, ArduinoBus.currentSysexCallback>();
        
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

        /// <summary>
        /// Permet de stocker un traitement pour un message donné qui sera identifié par 
        /// idMessage et Seed (dans le cadre des messages dynamixel cela correspondra à l'identifiant
        /// de ce dernier).
        /// </summary>
        /// <param name="identifiant_dynamixel">Identifiant dynamixel concerné par le message</param>
        /// <param name="callback">Méthode qui doit être appelée après réception d'un message </param>
        /// <returns>Identifiant du message ou bien null</returns>
        public byte? GetReferenceMessage(byte identifiant_dynamixel,ArduinoBus.currentSysexCallback callback)
        {
            if (callback != null)
            {
                lock (lockref)
                {
                    while (delegateResponse.ContainsKey((identifiant_dynamixel << 8) | idMessage))
                    {
                        if (idMessage == byte.MaxValue)
                            idMessage = 0;
                        else
                            ++idMessage;
                    }
                    delegateResponse.Add((identifiant_dynamixel << 8) | idMessage, callback);
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
        public void ValidateResponseMessage(byte cmdsysex,int identifiantMessage,byte[] datas)
        {
            lock (lockref)
            {
                if (delegateResponse.ContainsKey(identifiantMessage) && delegateResponse[identifiantMessage] != null)
                {
                    ArduinoBus.currentSysexCallback tmp = delegateResponse[identifiantMessage];
                    delegateResponse.Remove(identifiantMessage);
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
    
    
        public void DeleteReferenceMessage(int identifiant_message)
        {
            lock (lockref)
            {
                if (delegateResponse.ContainsKey(identifiant_message))
                {
                    ArduinoBus.currentSysexCallback tmp = delegateResponse[identifiant_message];
                    if (delegateResponse.Remove(identifiant_message))
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
    }
}
