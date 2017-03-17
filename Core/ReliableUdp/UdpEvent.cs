using ReliableUdp.Utility;

namespace ReliableUdp
{
	using ReliableUdp.Enums;

	public sealed class UdpEvent
	{
		public UdpPeer Peer;
		public readonly UdpDataReader DataReader = new UdpDataReader();
		public UdpEventType Type;
		public UdpEndPoint RemoteEndPoint;
		public int AdditionalData;
		public DisconnectReason DisconnectReason;
		public ChannelType Channel;
	}
}