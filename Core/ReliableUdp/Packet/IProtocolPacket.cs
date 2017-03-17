namespace ReliableUdp.Packet
{
	using ReliableUdp.Utility;

	public interface IProtocolPacket
	{
		byte PacketType { get; }

		void Serialize(UdpDataWriter writer);

		bool Deserialize(UdpDataReader reader);
	}
}
