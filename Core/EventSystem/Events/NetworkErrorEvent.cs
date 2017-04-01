namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkErrorEvent : NetworkNotConnectedEvent
	{
		public int ErrorCode { get; set; }

		public NetworkErrorEvent(int errorCode, UdpManager manager, UdpEndPoint endPoint) : base(manager, endPoint)
		{
			this.ErrorCode = errorCode;
		}
	}
}