namespace ReliableUdp.Enums
{
	public enum PacketType : byte
	{
		Unreliable,
		Reliable,
		UnreliableOrdered,
		ReliableOrdered,
		AckReliable,
		AckReliableOrdered,
		Ping,
		Pong,
		ConnectRequest,
		ConnectAccept,
		Disconnect,
		UnconnectedMessage,
		MtuCheck,
		MtuOk,
		DiscoveryRequest,
		DiscoveryResponse,
		Merged
	}
}