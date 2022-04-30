using Bespoke.Osc;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace YeelightOSC
{
    public class VRLayer
    {
        public readonly IPEndPoint VRChat = new IPEndPoint(IPAddress.Loopback, 9000);
        public readonly IPEndPoint VRServer = new IPEndPoint(IPAddress.Loopback, 9001);
        private OscServer VRMaster = new OscServer(Bespoke.Common.Net.TransportType.Udp, IPAddress.Loopback, 9001);

        private string Base = @"/avatar/parameters/";
        private string[] Methods = { "Brightness", "Temperature", "ColorR", "ColorG", "ColorB", "SendUpdate", "LightToggle", "VRCEmote" };
        private System.Timers.Timer Heartbeat = new System.Timers.Timer(5000);

        public void Init(EventHandler<OscMessageReceivedEventArgs> MessageF, string[] SMethods)
        {
            OscPacket.UdpClient = new UdpClient(9005);

            VRMaster.MessageReceived += MessageF;
            Methods = SMethods;

            VRMaster.RegisterMethod("/*");

            foreach (string Method in Methods)
            {
                VRMaster.RegisterMethod(String.Format("{0}{1}", Base, Method));
                Console.WriteLine(String.Format("Registered {0}", Method));
            }

            // Tell VRChat that we're ready to receive messages
            SendVRMsg("OSCWakeUp", VRChat);

            Heartbeat.Elapsed += HeartbeatEvent;
            Heartbeat.AutoReset = true;
            Heartbeat.Start();

            VRMaster.Start();
        }

        // This is used in case VRChat is restarted while the program is running!
        // VRChat won't send packets unless it receives one first.
        private void HeartbeatEvent(object? sender, ElapsedEventArgs e)
        {
            SendVRMsg("OSCHeartbeat", VRChat);
        }

        private void BundleReceived(object? sender, OscBundleReceivedEventArgs e)
        {
            Console.WriteLine("E");
        }

        // Send a message to VRChat
        public void SendMsg(string Target, IPEndPoint NetTarget, object? Value = null)
        {
            OscBundle Bundle = new OscBundle(NetTarget);
            OscMessage Message;

            Message = new OscMessage(NetTarget, Target);

            Message.Append(Value);
            Bundle.Append(Message);
            Bundle.Send(NetTarget);

            Console.WriteLine(String.Format("Sent bundle to {0} (port {1})", NetTarget.Address.ToString(), NetTarget.Port.ToString()));
        }

        public void SendVRMsg(string Target, IPEndPoint NetTarget, object? Value = null)
        {
            SendMsg(String.Format("{0}{1}", Base, Target), NetTarget, Value);
        }
    }
}
