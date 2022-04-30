using Bespoke.Osc;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Timers;

namespace YeelightOSC
{
    public class VRLayer
    {
        // The endpoint is the target passed to the OSC message/bundle
        // The VRChat endpoint is the OSC server hosted by your VRChat process
        // The VRServer endpoint is the OSC server hosted by this application

        // VRChat server
        public readonly IPEndPoint VRChat = new IPEndPoint(IPAddress.Loopback, 9000);

        // YeelightOSC server
        public readonly IPEndPoint VRServer = new IPEndPoint(IPAddress.Loopback, 9001);
        private OscServer VRMaster = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, 9001);

        // You can also use this app as a convenient way to control your Yeelight bulb from the outside world,
        // using OSC messages as a way to communicate with them!
        private OscServer ExternalInput;

        private string Base = @"/avatar/parameters/";
        private string[] Methods;
        private System.Timers.Timer Heartbeat = new System.Timers.Timer(5000);

        public void Init(EventHandler<OscMessageReceivedEventArgs> Message, EventHandler<OscBundleReceivedEventArgs> Bundle, string[] SMethods)
        {
            // Dummy UDP client for OSC packets
            OscPacket.UdpClient = new UdpClient(9005);

            Methods = SMethods;

            try
            {
                // We need to get the local IP of the network interface we're using, to be able to access packets
                // from the Internet. IPAddress.Loopback will only accept packets from the local network.
                foreach (NetworkInterface Interface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (Interface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        foreach (UnicastIPAddressInformation IP in Interface.GetIPProperties().UnicastAddresses)
                        {
                            if (IP.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                // We got the IP, let's start the server
                                ExternalInput = new OscServer(Bespoke.Common.Net.TransportType.Udp, IP.Address, 9500);

                                // Register all the parameters that we will use
                                foreach (string Method in Methods)
                                {
                                    ExternalInput.RegisterMethod(Method);
                                    Console.WriteLine(String.Format("Registered {0} for ExternalInput", Method));
                                }

                                // Attach the event handlers to the OSC server, then start it
                                ExternalInput.MessageReceived += Message;
                                ExternalInput.BundleReceived += Bundle;
                                ExternalInput.Start();

                                Console.WriteLine(String.Format("ExternalInput -> Listening on {0}", IP.Address.ToString()));
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            // Register all the parameters that we will use
            foreach (string Method in Methods)
            {
                VRMaster.RegisterMethod(String.Format("{0}{1}", Base, Method));
                Console.WriteLine(String.Format("Registered {0} for VRServer", Method));
            }

            // Send a dummy packet to VRChat to ready up its OSC server
            SendVRMsg("OSCWakeUp", VRChat, true, true);
            
            // Heartbeat event, to avoid timeouts in case VRChat's OSC server goes to sleep
            Heartbeat.Elapsed += HeartbeatEvent;
            Heartbeat.AutoReset = true;
            Heartbeat.Start();

            // Attach the event handlers to the OSC server, then start it
            VRMaster.MessageReceived += Message;
            VRMaster.BundleReceived += Bundle;
            VRMaster.Start();
        }

        // This is used in case VRChat is restarted while the program is running!
        // VRChat won't send packets unless it receives one first.
        private void HeartbeatEvent(object? sender, ElapsedEventArgs e)
        {
            SendVRMsg("OSCHeartbeat", VRChat, true, true);
        }

        // Send a message to the target OSC server
        private void SendMsg(string Target, IPEndPoint NetTarget, object? Value = null, bool? Silent = false)
        {
            OscMessage Message = new OscMessage(NetTarget, Target);
            Message.Append(Value);

            Message.Send(NetTarget);
            if (Silent == false) Console.WriteLine(String.Format("Sent message {0} to {1} (port {2})", Message.Address, NetTarget.Address.ToString(), NetTarget.Port.ToString()));
        }

        // Send a bundle to the target OSC server
        private void SendBndl(IPEndPoint NetTarget, List<OscMessage>? MsgArray = null, bool? Silent = false)
        {
            OscBundle Bundle = new OscBundle(NetTarget);

            foreach (OscMessage Msg in MsgArray)
            {
                Bundle.Append(Msg);
            }

            foreach (Byte B in Bundle.ToByteArray())
            {
                Console.Write(String.Format("{0}", char.ConvertFromUtf32(B)));
            }
            Console.Write("\n");

            Bundle.Send(NetTarget);
            if (Silent == false) Console.WriteLine(String.Format("Sent bundle to {0} (port {1})", NetTarget.Address.ToString(), NetTarget.Port.ToString()));
        }

        // Send a message to VRChat
        public void SendVRMsg(string Target, IPEndPoint NetTarget, object? Value = null, bool? Silent = false)
        {
            SendMsg(String.Format("{0}{1}", Base, Target), NetTarget, Value, Silent);
        }

        // Send a bundle to VRChat
        public void SendVRBndl(IPEndPoint NetTarget, List<OscMessage>? Value = null, bool? Silent = false)
        {
            SendBndl(NetTarget, Value, Silent);
        }
    }
}
