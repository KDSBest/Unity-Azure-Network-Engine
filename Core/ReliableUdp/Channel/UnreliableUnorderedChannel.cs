using System;
using System.Linq;
using System.Text;
using Utility;

namespace ReliableUdp.Channel
{
	using ReliableUdp.Enums;
	using ReliableUdp.Packet;

	public class UnreliableUnorderedChannel : IUnreliableChannel
	{
		private readonly ConcurrentQueue<UdpPacket> outgoingPackets = new ConcurrentQueue<UdpPacket>();
		private UdpPeer peer;

		public void Initialize(UdpPeer peer)
		{
			this.peer = peer;
		}

		public void AddToQueue(UdpPacket packet)
		{
			this.outgoingPackets.Enqueue(packet);
		}

		public bool SendNextPacket()
		{
			UdpPacket packet = this.outgoingPackets.Dequeue();

			if (packet == null)
				return false;

			this.peer.SendRawData(packet);
			this.peer.Recycle(packet);

			return true;
		}

		public void ProcessPacket(UdpPacket packet)
		{
			this.peer.AddIncomingPacket(packet, ChannelType.Unreliable);
		}

		public int PacketsInQueue
		{
			get
			{
				return 0;
			}
		}

		public void ProcessAck(UdpPacket packet)
		{
		}

		public void SendAcks()
		{
		}
	}
}
