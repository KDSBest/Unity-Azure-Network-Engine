namespace EventSystem.Events
{
	using ReliableUdp;
	using ReliableUdp.Enums;

	public class NetworkReceiveAckEvent<T> : NetworkEvent
	{
		public T Packet { get; set; }

		public ChannelType Channel { get; set; }

		public NetworkReceiveAckEvent(T packet, ChannelType channel, UdpManager manager, UdpPeer peer)
			: base(manager, peer)
		{
			Packet = packet;
			Channel = channel;
		}
	}
}