using System;
using System.Runtime.InteropServices;


namespace ConsoleArduinoDynamixel01
{

     
    class MyConsole
    {
        [Flags]
        public enum ColorText : int
        {
            Noir = 0x0000,
            Bleu = 0x0001,
            Vert = 0x0002,
            Cyan = 0x0003,
            Rouge = 0x0004,
            Magenta = 0x0005,
            Jaune = 0x0006,
            Blanc = 0x0007,
            Brillant = 0x0008
        }

        public enum ColorBackground : int 
        {
            Noir = 0x0000,
            Bleu = 0x0010,
            Vert = 0x0020,
            Cyan = 0x0030,
            Rouge = 0x0040,
            Magenta = 0x0050,
            Jaune = 0x0060,
            Blanc = 0x0070,
            Brillant = 0x0080
        }

        private static object lockObject = new object();

        const int STD_OUTPUT_HANDLE = -11;

        private int hanldeConsole;

        private static MyConsole internalRef;

        public int bgErrorColor { get; set; }

        public int fgErrorColor { get; set; }

        public int bgNormalColor { get; set; }

        public int fgNormalColor { get; set; }

        private MyConsole()
        {
            hanldeConsole = GetStdHandle(STD_OUTPUT_HANDLE);
        }

        public static MyConsole GetInstance()
        {
            lock (lockObject)
            {
                if (internalRef == null)
                    internalRef = new MyConsole();
                return internalRef;
            }
        }

        [DllImportAttribute("kernel32.dll")]
        private static extern int GetStdHandle(int nStdHandle);
        [DllImportAttribute("kernel32.dll")]
        private static extern int SetConsoleTextAttribute(
            int hConsoleOutput, int wAttributes);

        public void WriteError(string message, bool withbg)
        {
            if (withbg)
            {
                SetConsoleTextAttribute(hanldeConsole, fgErrorColor + bgErrorColor);
            }
            else
            {
                SetConsoleTextAttribute(hanldeConsole, fgErrorColor);
            }
            Console.WriteLine("Erreur:\r\n{0}", message);
            SetConsoleTextAttribute(hanldeConsole, fgNormalColor);
        }

        public void WriteNormal(string message)
        {
            SetConsoleTextAttribute(hanldeConsole, fgNormalColor);
            Console.WriteLine(message);
        }

        public void Write(string message, int fgcolor, int bgcolor)
        {
            SetConsoleTextAttribute(hanldeConsole, fgcolor + bgcolor);
            Console.WriteLine(message);
        }
    }
}