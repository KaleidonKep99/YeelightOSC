using Bespoke.Osc;
using System.Net;

namespace YeelightOSC
{
    static class Program
    {
        static YeelightAPI.Device Yeebulb = new YeelightAPI.Device("::1");

        static float Brightness = 100.0f, Temperature = 4300.0f;
        static float R = 1.0f, G = 1.0f, B = 1.0f;

        static VRLayer TranslationLayer = new VRLayer();
        static MathFuncs MFuncs = new MathFuncs();
        static string[] Methods = new string[] { "Brightness", "Temperature", "ColorR", "ColorG", "ColorB", "SendUpdate", "LightToggle" };
        private static LogSystem MainLog = new LogSystem("MainPro");

        [STAThread]
        static int Main(string[] Args)
        {
            Yeebulb.Dispose();

            Console.CancelKeyPress += delegate
            {
                Yeebulb.Disconnect();
                Yeebulb.Dispose();
            };

            IPAddress Dummy;
            bool Check = false;

            if (Args.Length > 0)
                Check = IPAddress.TryParse(Args[0], out Dummy);

            if (!Check)
            {
                MainLog.PrintMessage(LogSystem.MsgType.Fatal, "IP not specified or not valid.");
                Console.ReadKey();
                return -1;
            }

            Yeebulb = new YeelightAPI.Device(Args[0]);
            Yeebulb.Connect().ContinueWith(t =>
            {
                MainLog.PrintMessage(LogSystem.MsgType.Information, "Connected to smart device.", Yeebulb.Name, Yeebulb.Hostname);

                TranslationLayer.Init(new EventHandler<OscMessageReceivedEventArgs>(MessageF), new EventHandler<OscBundleReceivedEventArgs>(BundleF), Methods);

                TranslationLayer.SendVRMsg(Methods[0], TranslationLayer.VRChat, 1.0f, true);
                TranslationLayer.SendVRMsg(Methods[1], TranslationLayer.VRChat, 0.540f, true);
                TranslationLayer.SendVRMsg(Methods[2], TranslationLayer.VRChat, 1.0f, true);
                TranslationLayer.SendVRMsg(Methods[3], TranslationLayer.VRChat, 1.0f, true);
                TranslationLayer.SendVRMsg(Methods[4], TranslationLayer.VRChat, 1.0f, true);
                TranslationLayer.SendVRMsg(Methods[5], TranslationLayer.VRChat, 0, true);
                TranslationLayer.SendVRMsg(Methods[6], TranslationLayer.VRChat, true, true);
                TranslationLayer.SendVRMsg(Methods[7], TranslationLayer.VRChat, true, true);
            });

            while (true)
            {
                while (!Yeebulb.IsConnected) ;

                string[] CArgs = Console.ReadLine().ToLower().Split(' ');
                int Value = 250;
                int[] RGB = new int[3];

                if (CArgs.Length > 1)
                    int.TryParse(CArgs[1], out Value);

                if (CArgs.Length == 4)
                {
                    int.TryParse(CArgs[1], out RGB[0]);
                    int.TryParse(CArgs[2], out RGB[1]);
                    int.TryParse(CArgs[3], out RGB[2]);
                }

                switch (CArgs[0])
                {
                    case "i":
                    case "on":
                        Yeebulb.SetPower(true, Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        MainLog.PrintMessage(LogSystem.MsgType.Information, "Device turned on.", Yeebulb.Name);
                        break;

                    case "o":
                    case "off":
                        Yeebulb.SetPower(false, Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        MainLog.PrintMessage(LogSystem.MsgType.Information, "Device turned off.", Yeebulb.Name);
                        break;

                    case "br":
                    case "bright":
                    case "brightness":
                        Yeebulb.SetBrightness(Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Brightness set to BR{0}", Value), Yeebulb.Name);
                        break;

                    case "te":
                    case "temp":
                    case "temperature":
                        Yeebulb.SetColorTemperature(Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Temperature set to TE{0}", Value), Yeebulb.Name);
                        break;

                    case "rgb":
                        Yeebulb.SetRGBColor(RGB[0], RGB[1], RGB[2]);

                        Console.ForegroundColor = ConsoleColor.Green;
                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("RGB values set to R{0} G{1} B{2}", RGB[0], RGB[1], RGB[2]), Yeebulb.Name);
                        break;

                    // Test
                    case "vibe":
                        HttpClient Client = new HttpClient();
                        var Response = Client.GetStringAsync("http://ip:port/command?v=0&t=id");
                        break;

                    default:
                        break;
                }

                Console.ResetColor();
            };

            return 0;
        }

        static bool CastCheck(string Variable, object Data, Type DataType)
        {
            // Check if the type of the object is of the type we want
            if (Data.GetType() != DataType)
            {
                // If not, die
                MainLog.PrintMessage(LogSystem.MsgType.Error, "The object received for the variable is not of the right type.", Variable, DataType.Name);
                return false;
            }

            // Otherwise, live
            return true;
        }

        static void AnalyzeData(object? sender, IPEndPoint Source, string Address, IList<object> Data, Type DataType)
        {
            if (sender == null) return;

            try
            {
                string Variable = Address.Substring(Address.LastIndexOf("/") + 1); ;
                int FtoI = 0;

                switch (Variable)
                {
                    // Changes the brightness of the lamp, from 0 to 100
                    case "Brightness":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        Brightness = (float)Data[0];
                        FtoI = MFuncs.FtoI(Brightness, 100);

                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Brightness set to {0}%.", FtoI), FtoI.ToString("X"));
                        break;

                    // Changes the temperature of the lamp, from 1700K to 6500K
                    case "Temperature":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        Temperature = MFuncs.Lerp((float)Data[0], 1700, 6500);
                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Temperature set to {0}K.", MFuncs.RoundNum((int)Temperature)), ((int)Temperature).ToString("X"));
                        break;

                    // Changes the colors of the lamp, from 0 to 255 (8-bit color)
                    case "ColorR":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        R = (float)Data[0];
                        FtoI = MFuncs.FtoI(R, 255);

                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Set red to {0}.", FtoI), FtoI.ToString("X"));
                        break;

                    case "ColorG":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        G = (float)Data[0];
                        FtoI = MFuncs.FtoI(G, 255);

                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Set green to {0}.", FtoI), FtoI.ToString("X"));
                        break;

                    case "ColorB":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        B = (float)Data[0];
                        FtoI = MFuncs.FtoI(B, 255);

                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Set blue to {0}.", FtoI), FtoI.ToString("X"));
                        break;

                    // Sends the update to the lamp, since we don't want to spam it
                    // whenever we make a change to one variable
                    case "SendUpdate":
                        if (!CastCheck(Variable, Data[0], typeof(int))) break;

                        int bFtoI = MFuncs.FtoI(Brightness, 100), 
                            ReFtoI = MFuncs.FtoI(R, 255), 
                            GrFtoI = MFuncs.FtoI(G, 255), 
                            BlFtoI = MFuncs.FtoI(B, 255);

                        switch ((int)Data[0])
                        {
                            case 1:
                                Yeebulb.SetBrightness(MFuncs.FtoI(Brightness, 100));
                                Yeebulb.SetColorTemperature((int)Temperature);

                                MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Set temperature to {0}K, with brightness of {1}%.",
                                    MFuncs.RoundNum((int)Temperature), bFtoI), ((int)Temperature).ToString("X"), bFtoI.ToString("X"));
                                break;
                                
                            case 2:
                                Yeebulb.SetBrightness(MFuncs.FtoI(Brightness, 100));
                                Yeebulb.SetRGBColor(MFuncs.FtoI(R, 255), MFuncs.FtoI(G, 255), MFuncs.FtoI(B, 255));

                                MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("Set color to values R{0}, G{1}, B{2}, with {3}%.",
                                    ReFtoI, GrFtoI, BlFtoI, bFtoI),
                                    ReFtoI.ToString("X"), GrFtoI.ToString("X"), BlFtoI.ToString("X"), bFtoI.ToString("X"));

                                break;

                            default:
                                break;
                        }
                        break;

                    // Toggles the light on or off
                    case "LightToggle":
                        if (!CastCheck(Variable, Data[0], typeof(bool))) break;

                        Yeebulb.SetPower((bool)Data[0]);
                        MainLog.PrintMessage(LogSystem.MsgType.Information, "Changed power status.", (bool)Data[0] ? "ON" : "OFF");
                        break;

                    // Ignore this
                    case "#bundle":
                        break;

                    // Unrecognized address
                    default:
                        MainLog.PrintMessage(LogSystem.MsgType.Information, String.Format("{0} received, but the address wasn't of a recognized type.", DataType == typeof(OscBundle) ? "Bundle" : "Message"), Source.Address, Source.Port, Address);
                        break;
                }
            }
            catch (Exception ex)
            {
                MainLog.PrintMessage(LogSystem.MsgType.Error, "An error has occured.", ex.ToString());
            }
        }

        static void BundleF(object? sender, OscBundleReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Bundle.SourceEndPoint, Var.Bundle.Address, Var.Bundle.Data, Var.GetType());
        }

        static void MessageF(object? sender, OscMessageReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Message.SourceEndPoint, Var.Message.Address, Var.Message.Data, Var.GetType());
        }
    }
}