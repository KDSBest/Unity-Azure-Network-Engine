using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSystem.Events
{
	using ReliableUdp;

	public class NetworkEvent
	{
		public UdpManager Manager { get; set; }

		public UdpPeer Peer { get; set; }

		public NetworkEvent(UdpManager manager, UdpPeer peer)
		{
			Manager = manager;
			Peer = peer;
		}
	}
}
