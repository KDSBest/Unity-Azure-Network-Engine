namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkConnectedEvent : NetworkEvent
	{
		public NetworkConnectedEvent(UdpManager manager, UdpPeer peer)
			: base(manager, peer)
		{
			
		}
	}
}
