using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
