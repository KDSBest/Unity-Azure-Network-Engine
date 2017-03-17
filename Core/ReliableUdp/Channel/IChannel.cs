namespace ReliableUdp.Channel
{
	using ReliableUdp.Packet;

	public interface IChannel
	{
		void AddToQueue(UdpPacket packet);

		bool SendNextPacket();

		void ProcessPacket(UdpPacket packet);

		int PacketsInQueue { get; }

		void ProcessAck(UdpPacket packet);

		void SendAcks();

		void Initialize(UdpPeer peer);
	}
}