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
        private string[] Methods = { "Brightness", "Temperature", "ColorR", "ColorG", "ColorB", "SendUpdate", "LightToggle" };
        private System.Timers.Timer Heartbeat = new System.Timers.Timer(5000);

        public void Init(EventHandler<OscMessageReceivedEventArgs> MessageF, string[] SMethods)
        {
            OscPacket.UdpClient = new UdpClient(9005);

            VRMaster.Start();
            VRMaster.MessageReceived += MessageF;
            Methods = SMethods;

            foreach (string Method in Methods)
            {
                VRMaster.RegisterMethod(String.Format("{0}{1}", Base, Method));
                Console.WriteLine(String.Format("Registered {0}", Method));
            }

            // Tell VRChat that we're ready to receive messages
            SendMsg("OSCWakeUp", VRChat);

            Heartbeat.Elapsed += HeartbeatEvent;
            Heartbeat.AutoReset = true;
            Heartbeat.Start();
        }

        // This is used in case VRChat is restarted while the program is running!
        // VRChat won't send packets unless it receives one first.
        private void HeartbeatEvent(object? sender, ElapsedEventArgs e)
        {
            SendMsg("OSCHeartbeat", VRChat);
        }

        // Send a message to VRChat
        public void SendMsg(string Target, IPEndPoint NetTarget, object? Value = null)
        {
            OscBundle VRBundle = new OscBundle(NetTarget);
            OscMessage Message;

            Message = new OscMessage(VRChat, String.Format("{0}{1}", Base, Target));

            Message.Append(Value);
            VRBundle.Append(Message);
            VRBundle.Send(VRChat);
        }
    }
}
