using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using Arduino.Framework.Communication;
using Arduino.Framework.Communs.Entities;


namespace ConsoleArduinoDynamixel01
{

    class Program
    {
        private static ServiceFirmata svcArduinoExchange;
        private static MyConsole console;

        private static Dictionary<int, AX12Dynamixel> poolAX12 = new Dictionary<int, AX12Dynamixel>();
        private static Dictionary<int, AX12Dynamixel> poolAXS1 = new Dictionary<int, AX12Dynamixel>();
        private static BioloidRegister[] allregistersAX12 = null;

        private static AnalogPin analogPin = 

        private static int maxmnenuitems;

        private const int sousMenuColortx = (int)(MyConsole.ColorText.Cyan & MyConsole.ColorText.Brillant);
        private const int sousMenuColorbg = (int)(MyConsole.ColorBackground.Magenta);

        private const int menuColortx = (int)(MyConsole.ColorText.Cyan & MyConsole.ColorText.Brillant);
        private const int menuColorbg = (int)(MyConsole.ColorBackground.Magenta);

        private static int selectedItems;

        static void Main(string[] args)
        {
            initialisation_console();
            initialisation_bus();

            allregistersAX12 = svcArduinoExchange.GetAllAX12Register();

            int selector = 0;
            bool good = false;

            while (selector != 1)
            {
                ShowMenu();
                good = int.TryParse(Console.ReadLine(), out selector);
                if (good)
                {
                    switch (selector)
                    {
                        case 1:
                            console.Write("Sortie demande", (int)MyConsole.ColorText.Rouge, (int)(MyConsole.ColorBackground.Jaune & MyConsole.ColorBackground.Brillant));
                            menuExit();
                            break;
                        case 2:
                            console.Write("Affichage List Dynamixel", (int)(MyConsole.ColorText.Bleu & MyConsole.ColorText.Brillant), (int)(MyConsole.ColorBackground.Jaune));
                            menuShowDynamixelNetwork();
                            break;
                        case 3:
                            console.Write("Modification de position", (int)(MyConsole.ColorText.Bleu), (int)(MyConsole.ColorBackground.Magenta));
                            menuChangePositionDynamixel();
                            break;
                        case 4:
                            console.Write("Lecture de paramètre pour un dynamixel AX12 donné", (int)(MyConsole.ColorText.Bleu), (int)(MyConsole.ColorBackground.Magenta));
                            menuReadDynamixelAX12();
                            break;
                        case 5:
                            console.Write("Lecture de la tension sur une broche analogique", menuColortx, menuColorbg);
                            menuReadAnalogiquePin();
                        default:
                            console.WriteError("Choix inconnu", true);
                            break;
                    }
                }
            }
        }

        private static void ShowMenu()
        {
            maxmnenuitems = 6;
            selectedItems = -1;
            console.WriteNormal("=================================================================");
            console.WriteNormal("MENU - DYNAMINEL@ARDUINO");
            console.WriteNormal("=================================================================");
            //>MENU EXIT
            console.WriteNormal("1 => Exit");
            //>MENU AFFICHER LISTE DYNAMIXEL
            console.WriteNormal("2 => Afficher List Dynamixel");
            //>MENU MODIFIER POSITION DYNAMIXEL 
            console.WriteNormal("3 => Modifier position dynamixel AX12");
            console.WriteNormal("4 => Lire Registre dynamixel AX12");
            console.WriteNormal("5 => Lire Tension sur broche analogique");
            console.WriteNormal("=================================================================");
        }

        private static void menuExit()
        {
            console.WriteNormal("==> Fermeture de l'application");
            svcArduinoExchange = null;
            console.WriteNormal("Déchargement BioloidBus");
        }

        private static void menuShowDynamixelNetwork()
        {
            if (poolAXS1.Count > 0)
            {
                console.Write("Liste des AXS1 trouvés :", (int)MyConsole.ColorText.Rouge, (int)MyConsole.ColorBackground.Jaune);
                foreach (KeyValuePair<int, AX12Dynamixel> item in poolAXS1)
                {
                    console.Write("Identifiant " + item.Value.ID, (int)MyConsole.ColorText.Bleu, (int)MyConsole.ColorBackground.Jaune);
                    //					console.Write("> Model" + item.Value.Model, (int)MyConsole.ColorText.Bleu, (int)MyConsole.ColorBackground.Jaune);
                }

            }

            if (poolAX12.Count > 0)
            {
                console.Write("Liste des AX12 trouvés :", (int)MyConsole.ColorText.Rouge, (int)MyConsole.ColorBackground.Jaune);
                foreach (KeyValuePair<int, AX12Dynamixel> item in poolAX12)
                {
                    console.Write("Identifiant " + item.Value.ID, (int)MyConsole.ColorText.Bleu, (int)MyConsole.ColorBackground.Jaune);
                    //					console.Write("> Model" + item.Value.Model, (int)MyConsole.ColorText.Bleu, (int)MyConsole.ColorBackground.Jaune);
                }
            }
        }

        private static void menuChangePositionDynamixel()
        {
            byte id = 0;
            byte nbLoop = 0;
            int position = 0;
            bool good = false;
            while (nbLoop < 4 && good == false)
            {
                console.Write(">>> Veuillez saisir l'identifiant du dynamixel : ", (int)(MyConsole.ColorText.Jaune & MyConsole.ColorText.Brillant), (int)(MyConsole.ColorBackground.Rouge));
                if (byte.TryParse(Console.ReadLine(), out id))
                {
                    console.Write(">>> Veuillez saisir la nouvelle position [0-1023] : ", (int)(MyConsole.ColorText.Jaune & MyConsole.ColorText.Brillant), (int)(MyConsole.ColorBackground.Rouge));
                    if (int.TryParse(Console.ReadLine(), out position))
                    {
                        good = true;
                    }
                }
            }
            if (good == true)
            {
                if (poolAX12.ContainsKey(id))
                {

                    poolAX12[id].SetGoalPosition(position);
                }
            }
        }

        private static void menuReadDynamixelAX12()
        {
            byte id = 0;
            byte nbLoop = 0;
            byte codeRegister = 0;
            bool good = false;
            while (nbLoop < 4 && good == false)
            {
                console.Write(">>> Veuillez saisir l'identifiant du dynamixel : ", (int)(MyConsole.ColorText.Jaune & MyConsole.ColorText.Brillant), (int)(MyConsole.ColorBackground.Rouge));
                if (byte.TryParse(Console.ReadLine(), out id))
                {
                    write_ax12_register();
                    console.Write(">>> Veuillez choisir la propriété à lire :", menuColortx, menuColorbg);
                    if (byte.TryParse(Console.ReadLine(), out codeRegister))
                        good = true;
                    else
                        nbLoop++;
                }
                else
                {
                    nbLoop++;
                }
            }
            if (good == true)
            {
                if (poolAX12.ContainsKey(id))
                {
                    RefreshValue method = null;
                    method = new RefreshValue((addregister, value) =>
                        {
                            console.Write("Résultat " + allregistersAX12[codeRegister - 1].Libelle + ":" + value, sousMenuColortx, sousMenuColorbg);
                            poolAX12[id].ValueRefresh -= method;
                        });
                    AX12Dynamixel dyna = poolAX12[id];
                    dyna.ValueRefresh += method;
                    dyna.GetXVal(allregistersAX12[codeRegister - 1]);
                }
            }
        }

        private static void menuReadAnalogiquePin()
        {
            byte idpin = 0;
            byte nbloop = 0;
            bool good = false;
            while (nbloop < 4 && good == false)
            {
                console.Write(">>> Veuillez saisir le numéro de la broche [1-14] :", sousMenuColortx, sousMenuColorbg);
                if (byte.TryParse(Console.ReadLine(), out idpin))
                {
                    if (idpin < 14 && idpin > 0)
                    {
                        good = true;
                    }
                    else
                        nbloop++;
                }
                else
                    nbloop++;
            }
            if (good == true)
            {
                
            }
        }

        private static void initialisation_bus()
        {
            svcArduinoExchange = ServiceFirmata.GetInstance("COM11");
            BioloidRegister registerModel = svcArduinoExchange.GetAX12Register(AX12RegisterAdd.ModelNumber);
            List<byte[]> lstMessage = new List<byte[]>();
            DynamixelFactory factoryDynamixel = DynamixelFactory.GetInstance();
            factoryDynamixel.BusCommunication = svcArduinoExchange;

            //> Recherche des actuators présents - comme on désire obtenir le model on effectue la lecture directement
            for (byte loop = 1; loop < 254; ++loop)
            {
                if (factoryDynamixel.Ping(loop) == true)
                {
                    poolAX12.Add(loop, svcArduinoExchange.CreateDynamixel(loop));
                }
                System.Threading.Thread.Sleep(20);
            }
        }


        private static void initialisation_console()
        {
            console = MyConsole.GetInstance();
            console.fgErrorColor = 5;
            console.bgErrorColor = 0x0080 + 0x0060;
            console.fgNormalColor = 1;
        }


        private static void traitement_accessregiste(AX12RegisterAdd add, int val)
        {
            switch (add)
            {
                case AX12RegisterAdd.ModelNumber:
                    console.Write("Model : " + val.ToString(), (int)(MyConsole.ColorText.Vert & MyConsole.ColorText.Brillant), (int)MyConsole.ColorBackground.Noir);
                    break;
                case AX12RegisterAdd.VersionFirmware:
                    break;
                case AX12RegisterAdd.ID:
                    break;
                case AX12RegisterAdd.BaudRate:
                    break;
                case AX12RegisterAdd.ReturnDelayTime:
                    break;
                case AX12RegisterAdd.CWAngleLimit:
                    break;
                case AX12RegisterAdd.CCWAngleLimit:
                    break;
                case AX12RegisterAdd.HighestLimitTemperature:
                    break;
                case AX12RegisterAdd.LowestLimitVoltage:
                    break;
                case AX12RegisterAdd.HighestLimitVoltage:
                    break;
                case AX12RegisterAdd.MaxTorque:
                    break;
                case AX12RegisterAdd.StatusReturnLevel:
                    break;
                case AX12RegisterAdd.AlarmLED:
                    break;
                case AX12RegisterAdd.AlarmShutdown:
                    break;
                case AX12RegisterAdd.DownCalibration:
                    break;
                case AX12RegisterAdd.UpCalibration:
                    break;
                case AX12RegisterAdd.TorqueEnable:
                    break;
                case AX12RegisterAdd.LED:
                    break;
                case AX12RegisterAdd.CWComplianceMargin:
                    break;
                case AX12RegisterAdd.CCWComplianceMargin:
                    break;
                case AX12RegisterAdd.CWComplianceSlope:
                    break;
                case AX12RegisterAdd.CCWComplianceSlope:
                    break;
                case AX12RegisterAdd.GoalPosition:
                    break;
                case AX12RegisterAdd.MovingSpeed:
                    break;
                case AX12RegisterAdd.TorqueLimit:
                    break;
                case AX12RegisterAdd.PresentPosition:
                    break;
                case AX12RegisterAdd.PresentSpeed:
                    break;
                case AX12RegisterAdd.PresentLoad:
                    break;
                case AX12RegisterAdd.PresentVoltage:
                    break;
                case AX12RegisterAdd.PresentTemeprature:
                    break;
                case AX12RegisterAdd.RegisteredInstruction:
                    break;
                case AX12RegisterAdd.Moving:
                    break;
                case AX12RegisterAdd.Lock:
                    break;
                case AX12RegisterAdd.Punch:
                    break;
                default:
                    break;
            }
        }

        private static void write_ax12_register()
        {
            int nbloop = allregistersAX12.Length / 2;

            for (int iloop = 0; iloop < nbloop; iloop++)
            {
                console.Write(string.Format(">{0,2} {1,-25} | {2,2} {3,25}", (2 * iloop + 1), allregistersAX12[2*iloop].Libelle, ((iloop+1) * 2), allregistersAX12[2*iloop + 1].Libelle), sousMenuColortx, sousMenuColorbg);
            }
            if ((allregistersAX12.Length % 2) != 1)
            {
                console.Write(string.Format(">{0,2} {1,-25}", nbloop, allregistersAX12[nbloop - 1].Libelle), sousMenuColortx, sousMenuColorbg);
            }
        }
    }
}
