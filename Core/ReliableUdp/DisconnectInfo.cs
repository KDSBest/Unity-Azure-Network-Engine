namespace ReliableUdp
{
	using ReliableUdp.Enums;
	using ReliableUdp.Utility;

	public struct DisconnectInfo
	{
		public DisconnectReason Reason;
		public int SocketErrorCode;
		public UdpDataReader AdditionalData;
	}
}