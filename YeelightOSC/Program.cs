using System.Net;
using Bespoke.Osc;

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

                TranslationLayer.Init(new EventHandler<OscMessageReceivedEventArgs>(MessageF), new EventHandler<OscBundleReceivedEventArgs>(BundleF), Methods);

                TranslationLayer.SendVRMsg(Methods[0], TranslationLayer.VRChat, 1.0f, true);
                TranslationLayer.SendVRMsg(Methods[1], TranslationLayer.VRChat, 0.542f, true);
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
            if (Data.GetType() != DataType)
            {
                Console.WriteLine(String.Format("The value received for {0} is not of type {1}", Variable, DataType.Name));
                return false;
            }

            return true;
        }

        static void AnalyzeData(object? sender, IPEndPoint Source, string Address, IList<object> Data)
        {
            if (sender == null) return;

            try
            {            
                string Variable = Address.Substring(Address.LastIndexOf("/") + 1); ;

                Console.Write(String.Format("Message received from {0}:{1} -> ", Source.Address, Source.Port));

                switch (Variable)
                {
                    case "Brightness":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        Brightness = (float)Data[0];
                        Console.WriteLine(String.Format("Brightness set to {0} (BR{1}, {1:X})", Brightness, MFuncs.FtoI(Brightness, 100)));
                        break;

                    case "Temperature":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        Temperature = MFuncs.Lerp((float)Data[0], 1700, 6500);
                        Console.WriteLine(String.Format("Temperature set to {0} (TE{1}, {1:X})", Temperature, (int)Temperature));
                        break;

                    case "ColorR":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        R = (float)Data[0];
                        Console.WriteLine(String.Format("Set ColorR to value R{0} ({0:X})", MFuncs.FtoI(R, 255)));
                        break;

                    case "ColorG":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        G = (float)Data[0];
                        Console.WriteLine(String.Format("Set ColorG to value G{0} ({0:X})", MFuncs.FtoI(G, 255)));
                        break;

                    case "ColorB":
                        if (!CastCheck(Variable, Data[0], typeof(float))) break;

                        B = (float)Data[0];
                        Console.WriteLine(String.Format("Set ColorB to value B{0} ({0:X})", MFuncs.FtoI(B, 255)));
                        break;

                    case "SendUpdate":
                        if (!CastCheck(Variable, Data[0], typeof(int))) break;

                        switch ((int)Data[0])
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
                        if (!CastCheck(Variable, Data[0], typeof(bool))) break;

                        Yeebulb.SetPower((bool)Data[0]);
                        Console.WriteLine(String.Format("Power status set to {0}", (bool)Data[0] ? "on" : "off"));
                        break;

                    case "#bundle":
                        break;

                    default:
                        Console.WriteLine(String.Format("Unknown address: {0}", Address));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void BundleF(object? sender, OscBundleReceivedEventArgs Var) 
        {
            AnalyzeData(sender, Var.Bundle.SourceEndPoint, Var.Bundle.Address, Var.Bundle.Data);
        }

        static void MessageF(object? sender, OscMessageReceivedEventArgs Var)
        {
            AnalyzeData(sender, Var.Message.SourceEndPoint, Var.Message.Address, Var.Message.Data);
        }
    }
}