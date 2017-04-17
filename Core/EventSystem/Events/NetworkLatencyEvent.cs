namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkLatencyEvent : NetworkEvent
	{
		public int Latency { get; set; }

		public NetworkLatencyEvent(int latency, UdpManager manager, UdpPeer peer)
			: base(manager, peer)
		{
			Latency = latency;
		}
	}
}
