namespace ReliableUdp.Channel
{
	using System;

	using ReliableUdp.Packet;

	public class PendingPacket
	{
		public UdpPacket Packet;
		public DateTime? TimeStamp;

		public UdpPacket GetAndClear()
		{
			var packet = this.Packet;
			this.Packet = null;
			this.TimeStamp = null;
			return packet;
		}
	}
}