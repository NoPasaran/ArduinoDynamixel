/*
 * FirstExemple.pde
 * 1er Test d'implementation du protocole d'echange bioloid sur le protocole firmata
 */
 
 /* Description :  
  * Le montage utilisé est celui basé sur le composant 74HC126. Ce dernier de placer
  * une patte à une haute impédence.
  * Les broches de direction sont 52 pour Txd et 53 pour Rxd : (xOE)
  * Les broches de données TxD et Rxd doivent être placées sur le port série UART3 
  * de l'arduino Mega (sinon il faut modifier le fichier DynamixelSerial3.h/cpp)
  */
  
 #include <DynamixelSerial3.h>
 #include <Firmata.h>
 
 /*============================================================================
  * SETUP()
  *==========================================================================*/
 void setup()
 {
   Firmata.attach(START_SYSEX,dynamixelCallback);
   Firmata.attach(START_SYSEX,sysexCallback);
   Firmata.begin(57600);
   Dynamixel.begin(1000000,52,53);
 }
 
 /*============================================================================
  * LOOP()
  *==========================================================================*/
 void loop()
 {
    //> Traitement de nouvelles commandes en attente
    while(Firmata.available())
    {
       Firmata.processInput();
    }
 }
 
  /*============================================================================
  * SYSEX COMMAND()
  *==========================================================================*/

  void dynamixelCallback(byte command, byte idmessage, byte argc, byte*argv)
  {
    switch(command)
    {
      case SYSEXCMD_ANALOG_READ:
        byte analogpacket[4];
        int analogresult;
        analogresult = analogRead(argv[0]);                            // récupération de la valeur
        analogpacket[0] = idmessage;				                   // identifiant message
        analogpacket[1] = argv[0];                                     // numéro pin
        analogpacket[2] = analogresult & 0xFF;                         // LSB value
        analogpacket[3] = (analogresult >> 8) & 0xFF;                  // MSB value
        Firmata.sendSysex(SYSEXCMD_ANALOG_READ,(byte)3,analogpacket);
        break;
     case SYSEXCMD_DYNAMIXEL_INSTRUCTION_PACKET:
        byte *status_packet;
        Dynamixel.sendInstructionPacket(argc,argv);
        byte length = Dynamixel.readStatusPacket(idmessage,&status_packet);
        if(length>0)
        {
          Firmata.sendSysex(SYSEXCMD_DYNAMIXEL_STATUS_PACKET,length,status_packet);
        }
        break;
    }
  }

  void sysexCallback(byte command, byte argc, byte*argv)
  {
    switch(command)
   {
      case SYSEXCMD_DYNAMIXEL_VOID_GENERIC :
        Dynamixel.sendInstructionPacket(argc,argv);
        break;
   }
  }
