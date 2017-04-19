namespace ReliableUdp.PacketHandler
{
	using ReliableUdp.Packet;

	public class IncomingFragments
	{
		public UdpPacket[] Fragments;
		public int ReceivedCount;
		public int TotalSize;
	}
}