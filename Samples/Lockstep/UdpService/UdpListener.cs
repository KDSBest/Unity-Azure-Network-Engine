using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GameActor.Interfaces;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

using ReliableUdp;
using ReliableUdp.Enums;
using ReliableUdp.Utility;
using Packets;

namespace UdpService
{
	public class UdpListener : IUdpEventListener
	{
		public UdpManager UdpManager { get; set; }

		public Dictionary<long, Guid> GameIds = new Dictionary<long, Guid>();
		public Dictionary<long, Guid> PIds = new Dictionary<long, Guid>();

		public void Update()
		{
			var peers = UdpManager.GetPeers();

			foreach (var peer in peers)
			{
				var closurePeer = peer;
				if(!this.GameIds.ContainsKey(closurePeer.ConnectId))
					continue;

				Guid gameId = this.GameIds[closurePeer.ConnectId];
				if (!this.PIds.ContainsKey(closurePeer.ConnectId))
					continue;

				Guid pId = this.PIds[closurePeer.ConnectId];
				IGameActor gActor = ActorProxy.Create<IGameActor>(new ActorId(gameId));
				ThreadPool.QueueUserWorkItem(
													 (state) =>
														 {
															 var packet = gActor.GetPacket(pId).Result;
															 while (packet != null)
															 {
																 closurePeer.Send(packet.RawData, packet.Channel);
																 packet = gActor.GetPacket(pId).Result;
															 }
														 });
			}
		}

		public void OnPeerConnected(UdpPeer peer)
		{
		}

		public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
		{
			this.ClearConnectionId(peer);
		}

		private void ClearConnectionId(UdpPeer peer)
		{
			if (this.GameIds.ContainsKey(peer.ConnectId))
			{
				ActorProxy.Create<IGameActor>(new ActorId(this.GameIds[peer.ConnectId])).PlayerDisconnect(PIds[peer.ConnectId]);
				this.GameIds.Remove(peer.ConnectId);
				this.PIds.Remove(peer.ConnectId);
			}
		}

		public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
		{
		}

		public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			if (this.GameIds.ContainsKey(peer.ConnectId))
			{
				if (reader.Data != null)
					ActorProxy.Create<IGameActor>(new ActorId(this.GameIds[peer.ConnectId])).ReceivePacket(PIds[peer.ConnectId], reader.Data);
			}
			else
			{
				var connPacket = new ConnectToGame();
				if (connPacket.Deserialize(reader))
				{
					PIds.Add(peer.ConnectId, Guid.NewGuid());
					this.GameIds.Add(peer.ConnectId, connPacket.Id);
					ActorProxy.Create<IGameActor>(new ActorId(connPacket.Id)).PlayerConnect(PIds[peer.ConnectId]);
				}
			}
		}

		public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
		}

		public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader, UnconnectedMessageType messageType)
		{
		}

		public void OnNetworkLatencyUpdate(UdpPeer peer, int latency)
		{
		}
	}
}
