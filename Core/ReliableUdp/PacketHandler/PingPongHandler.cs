using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.Enums;
using ReliableUdp.Logging;
using ReliableUdp.Packet;
using ReliableUdp.Utility;

using Utility;

namespace ReliableUdp.PacketHandler
{
	public class PingPongHandler
	{
		private int pingSendTimer = 0;
		private SequenceNumber pingSequence = new SequenceNumber(0);
		private SequenceNumber remotePingSequence = new SequenceNumber(0);
		private DateTime pingTimeStart;

		public int PingInterval { get; set; }

		public PingPongHandler(int pingIntervalInMs = 1000)
		{
			PingInterval = pingIntervalInMs;
		}

		public void HandlePing(UdpPeer peer, UdpPacket packet)
		{
			if ((packet.Sequence - this.remotePingSequence).Value < 0)
			{
				peer.Recycle(packet);
				return;
			}

			// Factory.Get<IUdpLogger>().Log("Ping receive... Send Pong...");
			this.remotePingSequence = packet.Sequence;
			peer.Recycle(packet);

			peer.CreateAndSend(PacketType.Pong, this.remotePingSequence);
		}

		public void HandlePong(UdpPeer peer, UdpPacket packet)
		{
			if ((packet.Sequence - this.pingSequence).Value < 0)
			{
				peer.Recycle(packet);
				return;
			}
			this.pingSequence = packet.Sequence;
			int rtt = (int)(DateTime.UtcNow - this.pingTimeStart).TotalMilliseconds;
			peer.NetworkStatisticManagement.UpdateRoundTripTime(rtt);
			// Factory.Get<IUdpLogger>().Log($"Ping {rtt}");
			peer.Recycle(packet);
		}

		public void Update(UdpPeer peer, int deltaTime)
		{
			//Send ping
			this.pingSendTimer += deltaTime;
			if (this.pingSendTimer >= PingInterval)
			{
				// Factory.Get<IUdpLogger>().Log("Send ping...");

				//reset timer
				this.pingSendTimer = 0;

				//send ping
				peer.CreateAndSend(PacketType.Ping, this.pingSequence);

				//reset timer
				this.pingTimeStart = DateTime.UtcNow;
			}
		}

	}
}
