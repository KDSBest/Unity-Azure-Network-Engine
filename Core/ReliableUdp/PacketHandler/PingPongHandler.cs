using System.Diagnostics;
using ReliableUdp.Enums;
using ReliableUdp.Packet;
using ReliableUdp.Utility;

namespace ReliableUdp.PacketHandler
{
    public class PingPongHandler
	{
		private int pingSendTimer = 0;
		private UdpPacket pingPaket = new UdpPacket(PacketType.Ping, 0);
		private UdpPacket pongPaket = new UdpPacket(PacketType.Ping, 0);
		private readonly Stopwatch stopwatch = new Stopwatch();

		public int PingInterval { get; set; }

		public PingPongHandler(int pingIntervalInMs = 1000)
		{
			PingInterval = pingIntervalInMs;
            pingPaket.Sequence = new SequenceNumber(1);
        }

		public void HandlePing(UdpPeer peer, UdpPacket packet)
		{
			if ((packet.Sequence - this.pongPaket.Sequence).Value > 0)
			{
                Debug.WriteLine("Ping receive... Send Pong...");
                this.pongPaket.Sequence = packet.Sequence;
                peer.SendRawData(this.pongPaket);
            }

            peer.Recycle(packet);
		}

		public void HandlePong(UdpPeer peer, UdpPacket packet)
		{
			if (packet.Sequence == pingPaket.Sequence)
			{
                stopwatch.Stop();
                int rtt = (int)stopwatch.ElapsedMilliseconds;
                peer.NetworkStatisticManagement.UpdateRoundTripTime(rtt);
                Debug.WriteLine($"Ping {rtt}");
            }

            peer.Recycle(packet);
		}

		public void Update(UdpPeer peer, int deltaTime)
		{
			this.pingSendTimer += deltaTime;
			if (this.pingSendTimer >= PingInterval)
			{
                this.pingSendTimer = 0;

                Debug.WriteLine("Send ping...");
                if (stopwatch.IsRunning)
                    peer.NetworkStatisticManagement.UpdateRoundTripTime((int)stopwatch.ElapsedMilliseconds);
                stopwatch.Restart();

                pingPaket.Sequence++;
                peer.SendRawData(this.pingPaket);
			}
		}

	}
}
