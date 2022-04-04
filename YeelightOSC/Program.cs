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

        [STAThread]
        static int Main(string[] Args)
        {
            Yeebulb.Dispose();

            Console.CancelKeyPress += delegate {
                Yeebulb.Disconnect();
                Yeebulb.Dispose();
            };

            IPAddress Dummy;
            bool Check = false;

            if (Args.Length > 0)
                Check = IPAddress.TryParse(Args[0], out Dummy);

            if (!Check)
            {
                Console.WriteLine("IP not specified or not valid.");
                Console.ReadKey();
                return -1;
            }

            Yeebulb = new YeelightAPI.Device(Args[0]);
            Yeebulb.Connect().ContinueWith(t =>
            {
                Console.WriteLine(String.Format("Connected to {0} (Hostname: {1})", Yeebulb.Name, Yeebulb.Hostname));

                TranslationLayer.Init(new EventHandler<OscMessageReceivedEventArgs>(MessageF), Methods);

                TranslationLayer.SendMsg(Methods[0], TranslationLayer.VRChat, 1.0f);
                TranslationLayer.SendMsg(Methods[1], TranslationLayer.VRChat, 0.542f);
                TranslationLayer.SendMsg(Methods[2], TranslationLayer.VRChat, 1.0f);
                TranslationLayer.SendMsg(Methods[3], TranslationLayer.VRChat, 1.0f);
                TranslationLayer.SendMsg(Methods[4], TranslationLayer.VRChat, 1.0f);
                TranslationLayer.SendMsg(Methods[5], TranslationLayer.VRChat, 0);
                TranslationLayer.SendMsg(Methods[6], TranslationLayer.VRChat, true);
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
                        Console.WriteLine(String.Format("{0} has been turned on", Yeebulb.Name, Value));
                        break;

                    case "o":
                    case "off":
                        Yeebulb.SetPower(false, Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("{0} has been turned off", Yeebulb.Name, Value));
                        break;

                    case "br":
                    case "bright":
                    case "brightness":
                        Yeebulb.SetBrightness(Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("{0}'s brightness set to BR{1}", Yeebulb.Name, Value));
                        break;

                    case "te":
                    case "temp":
                    case "temperature":
                        Yeebulb.SetColorTemperature(Value);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("{0}'s temperature set to TE{1}", Yeebulb.Name, Value));
                        break;

                    case "rgb":
                        Yeebulb.SetRGBColor(RGB[0], RGB[1], RGB[2]);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(String.Format("{0}'s RGB values set to R{1} G{2} B{3}", Yeebulb.Name, RGB[0], RGB[1], RGB[2]));
                        break;

                    default:
                        break;
                }

                Console.ResetColor();
            };

            return 0;
        }

        static void MessageF(object? sender, OscMessageReceivedEventArgs Var)
        {
            if (sender == null) return;

            try
            {
                string Variable = Var.Message.Address.Substring(Var.Message.Address.LastIndexOf("/") + 1);

                switch (Variable)
                {
                    case "Brightness":
                        Brightness = (float)Var.Message.Data[0];
                        Console.WriteLine(String.Format("Brightness set to {0} (BR{1}, {1:X})", Brightness, MFuncs.FtoI(Brightness, 100)));
                        break;

                    case "Temperature":
                        Temperature = MFuncs.Lerp((float)Var.Message.Data[0], 1700, 6500);
                        Console.WriteLine(String.Format("Temperature set to {0} (TE{1}, {1:X})", Temperature, (int)Temperature));
                        break;

                    case "ColorR":
                        R = (float)Var.Message.Data[0];
                        Console.WriteLine(String.Format("Set ColorR to value R{0} ({0:X})", MFuncs.FtoI(R, 255)));
                        break;

                    case "ColorG":
                        G = (float)Var.Message.Data[0];
                        Console.WriteLine(String.Format("Set ColorG to value G{0} ({0:X})", MFuncs.FtoI(G, 255)));
                        break;

                    case "ColorB":
                        B = (float)Var.Message.Data[0];
                        Console.WriteLine(String.Format("Set ColorB to value B{0} ({0:X})", MFuncs.FtoI(B, 255)));
                        break;

                    case "SendUpdate":
                        switch ((int)Var.Message.Data[0])
                        {
                            case 1:
                                Yeebulb.SetBrightness(MFuncs.FtoI(Brightness, 100));
                                Yeebulb.SetColorTemperature((int)Temperature);

                                Console.WriteLine(String.Format("Set temperature to value TE{0} ({0:X}), with BR{1} ({1:X})", (int)Temperature, MFuncs.FtoI(Brightness, 100)));
                                break;

                            case 2:
                                Yeebulb.SetBrightness(MFuncs.FtoI(Brightness, 100));
                                Yeebulb.SetRGBColor(MFuncs.FtoI(R, 255), MFuncs.FtoI(G, 255), MFuncs.FtoI(B, 255));

                                Console.WriteLine(
                                    String.Format("Set color to values R{0} ({0:X}), G{1} ({1:X}), B{2} ({2:X}), with BR{3} ({3:X})", 
                                    MFuncs.FtoI(R, 255), MFuncs.FtoI(G, 255), MFuncs.FtoI(B, 255), MFuncs.FtoI(Brightness, 100)));
                                break;

                            default:
                                break;
                        }
                        break;

                    case "LightToggle":
                        Yeebulb.SetPower((bool)Var.Message.Data[0]);
                        Console.WriteLine(String.Format("Power status set to {0}", (bool)Var.Message.Data[0] ? "on" : "off"));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}