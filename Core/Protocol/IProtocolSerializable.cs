namespace Protocol
{
	using ReliableUdp.Utility;

	public interface IProtocolSerializable<T>
	{
		void Serialize(T value, UdpDataWriter writer);

		void Deserialize(ref T value, UdpDataReader reader);
	}
}