using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YeelightOSC
{
    public class LogSystem
    {
        public enum MsgType
        {
            Information,
            Warning,
            Error,
            Fatal
        }

        private string WhoAmI = "Undefined";

        public LogSystem(string Source) 
        {
            WhoAmI = Source;
        }

        public bool PrintMessage(MsgType? Type, string Message, params object[] Values)
        {
            if (Message == null) 
                return false;

            switch (Type)
            {
                case MsgType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case MsgType.Error:
                case MsgType.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case MsgType.Information:
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.Write(string.Format("({0}) {1} >> {2}", (Type == MsgType.Fatal) ? WhoAmI.ToUpper() : WhoAmI, DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss:fff"), Message));

            if (Values.Length > 0)
            {
                Console.Write(" (Params: ");

                for (int i = 0; i < Values.Length; i++)
                    Console.Write(String.Format("{0}{1}", Values[i], (i == Values.Length - 1) ? null : ", "));

                Console.Write(")");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n");

            return true;
        }
    }
}
