using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EventSystem;
using EventSystem.Events;

using Protocol;

using ReliableUdp.Enums;

namespace Assets
{
	public class Server
	{
		public Server()
		{
			PubSub<NetworkReceiveEvent<ClientUpdate>>.Subscribe("Server", ClientUpdate);
		}

		private Dictionary<Guid, List<ClientUpdate>> Positions = new Dictionary<Guid, List<ClientUpdate>>();

		private void ClientUpdate(NetworkReceiveEvent<ClientUpdate> packet)
		{
			var clientUpdate = packet.Packet;
			var id = new Guid(clientUpdate.Id.Id.ToArray());

			if(!this.Positions.ContainsKey(id))
				this.Positions.Add(id, new List<ClientUpdate>());

			this.Positions[id].Add(clientUpdate);
		}

		public void Update()
		{
			var p = new ServerUpdate();
			p.Clients = this.Positions.Values.Where(x => x.Count > 0).Select(x => x[x.Count - 1]).ToList();
			foreach (var peer in ManageStuff.Udp.GetPeers())
			{
				peer.Send(p, ChannelType.UnreliableOrdered);
			}
		}
	}
}
