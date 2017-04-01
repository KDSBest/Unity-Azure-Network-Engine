namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkReceiveUnconnectedEvent<T> : NetworkNotConnectedEvent
	{
		public T Packet { get; set; }

		public NetworkReceiveUnconnectedEvent(T packet, UdpManager manager, UdpEndPoint endPoint) : base(manager, endPoint)
		{
			this.Packet = packet;
		}
	}
}