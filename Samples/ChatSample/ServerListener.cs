namespace ChatSample
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Protocol;

	using ReliableUdp;
	using ReliableUdp.Enums;
	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	public class ServerListener : IUdpEventListener
	{
		public UdpManager UdpManager { get; set; }

		public void OnPeerConnected(UdpPeer peer)
		{
			Console.WriteLine("[Server] Peer connected: " + peer.EndPoint);
		}

		public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
		{
			Console.WriteLine("[Server] Peer disconnected: " + peer.EndPoint + ", reason: " + disconnectInfo.Reason);
		}

		public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
		{
			Console.WriteLine("[Server] error: " + socketErrorCode);
		}

		private Dictionary<long, string> Names = new Dictionary<long, string>();

		public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			IProtocolPacket packet = null;
			if ((packet = new ClientChatMessage()) != null && packet.Deserialize(reader))
			{
				var fullPacket = packet as ClientChatMessage;
				var message = new ServerChatMessage() { Sender = peer.ConnectId, Message = fullPacket.Message };
				foreach (var remotePerr in UdpManager.GetPeers())
				{
					remotePerr.Send(message, channel);
				}
			}
			else if ((packet = new ClientChangeNick()) != null && packet.Deserialize(reader))
			{
				var fullPacket = packet as ClientChangeNick;
				if (this.Names.ContainsKey(peer.ConnectId))
					this.Names[peer.ConnectId] = fullPacket.NewNick;
				else
					this.Names.Add(peer.ConnectId, fullPacket.NewNick);

				var message = new ServerChangeNick() { Sender = peer.ConnectId, NewNick = fullPacket.NewNick };
				foreach (var remotePerr in UdpManager.GetPeers())
				{
					remotePerr.Send(message, channel);
				}
			}
			else if ((packet = new ClientWhisper()) != null && packet.Deserialize(reader))
			{
				var fullPacket = packet as ClientWhisper;
				var receiver = this.Names.FirstOrDefault(x => x.Value == fullPacket.Receiver);

				if (receiver.Key == 0)
				{
					peer.Send(new ServerChatMessage()
					{
						Message = new List<string>()
									{
										"Receiver is offline or not availble"
									},
						Sender = -1
					}, channel);
				}
				var receiverPeer = UdpManager.GetPeers().FirstOrDefault(x => x.ConnectId == receiver.Key);
				if (receiverPeer == null)
				{
					peer.Send(new ServerChatMessage()
					{
						Message = new List<string>()
									{
										"Receiver is offline or not availble"
									},
						Sender = -1
					}, channel);
				}

				var message = new ServerWhisper()
				{
					Sender = receiver.Key,
					Message = fullPacket.Message
				};
				receiverPeer.Send(message, channel);
			}
			else
			{
				var rawData = reader.GetBytes();
				foreach (var remotePerr in UdpManager.GetPeers())
				{
					remotePerr.Send(rawData, channel);
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