namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkDisconnectedEvent : NetworkEvent
	{
		public NetworkDisconnectedEvent(UdpManager manager, UdpPeer peer)
			: base(manager, peer)
		{

		}
	}
}