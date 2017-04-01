using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkNotConnectedEvent
	{
		public UdpManager Manager { get; set; }

		public UdpEndPoint EndPoint { get; set; }

		public NetworkNotConnectedEvent(UdpManager manager, UdpEndPoint endPoint)
		{
			Manager = manager;
			EndPoint = endPoint;
		}
	}
}
