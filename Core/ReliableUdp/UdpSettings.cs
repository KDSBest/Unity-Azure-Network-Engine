using ReliableUdp.Simulation;

namespace ReliableUdp
{
	public class UdpSettings
	{
		public long DisconnectTimeout = 5000;

		public int ReconnectDelay = 500;

		public int MaxConnectAttempts = 10;

		public bool ReuseAddress = false;

		public INetworkSimulation NetworkSimulation = null;

		public string ConnectKey = "kdsbest";
	}
}