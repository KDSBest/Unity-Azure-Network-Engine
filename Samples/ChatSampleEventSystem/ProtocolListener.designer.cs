
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EventSystem.Events;

using ReliableUdp;
using ReliableUdp.Enums;
using ReliableUdp.Packet;
using ReliableUdp.Utility;
using Protocol;

namespace EventSystem
{

	public class ProtocolListener : IUdpEventListener
	{
		public UdpManager UdpManager { get; set; }

		public void OnPeerConnected(UdpPeer peer)
		{
			PubSub<NetworkConnectedEvent>.Publish(new NetworkConnectedEvent(UdpManager, peer));
		}

		public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
		{
			PubSub<NetworkDisconnectedEvent>.Publish(new NetworkDisconnectedEvent(UdpManager, peer));
		}

		public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
		{
			PubSub<NetworkErrorEvent>.Publish(new NetworkErrorEvent(socketErrorCode, UdpManager, endPoint));
		}

		public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			IProtocolPacket packet = null;
						if ((packet = new ClientChatMessage()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ClientChatMessage>>.Publish(new NetworkReceiveEvent<ClientChatMessage>(packet as ClientChatMessage, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerChatMessage()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ServerChatMessage>>.Publish(new NetworkReceiveEvent<ServerChatMessage>(packet as ServerChatMessage, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ClientChangeNick()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ClientChangeNick>>.Publish(new NetworkReceiveEvent<ClientChangeNick>(packet as ClientChangeNick, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerChangeNick()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ServerChangeNick>>.Publish(new NetworkReceiveEvent<ServerChangeNick>(packet as ServerChangeNick, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ClientWhisper()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ClientWhisper>>.Publish(new NetworkReceiveEvent<ClientWhisper>(packet as ClientWhisper, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerWhisper()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ServerWhisper>>.Publish(new NetworkReceiveEvent<ServerWhisper>(packet as ServerWhisper, channel, UdpManager, peer));
				return;
			}


			PubSub<NetworkReceiveEvent<byte[]>>.Publish(new NetworkReceiveEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}

		public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			IProtocolPacket packet = null;
						if ((packet = new ClientChatMessage()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ClientChatMessage>>.Publish(new NetworkReceiveAckEvent<ClientChatMessage>(packet as ClientChatMessage, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerChatMessage()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ServerChatMessage>>.Publish(new NetworkReceiveAckEvent<ServerChatMessage>(packet as ServerChatMessage, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ClientChangeNick()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ClientChangeNick>>.Publish(new NetworkReceiveAckEvent<ClientChangeNick>(packet as ClientChangeNick, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerChangeNick()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ServerChangeNick>>.Publish(new NetworkReceiveAckEvent<ServerChangeNick>(packet as ServerChangeNick, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ClientWhisper()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ClientWhisper>>.Publish(new NetworkReceiveAckEvent<ClientWhisper>(packet as ClientWhisper, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerWhisper()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ServerWhisper>>.Publish(new NetworkReceiveAckEvent<ServerWhisper>(packet as ServerWhisper, channel, UdpManager, peer));
				return;
			}


			PubSub<NetworkReceiveAckEvent<byte[]>>.Publish(new NetworkReceiveAckEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}

		public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader, UnconnectedMessageType messageType)
		{
			IProtocolPacket packet = null;
						if ((packet = new ClientChatMessage()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ClientChatMessage>>.Publish(new NetworkReceiveUnconnectedEvent<ClientChatMessage>(packet as ClientChatMessage, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ServerChatMessage()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ServerChatMessage>>.Publish(new NetworkReceiveUnconnectedEvent<ServerChatMessage>(packet as ServerChatMessage, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ClientChangeNick()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ClientChangeNick>>.Publish(new NetworkReceiveUnconnectedEvent<ClientChangeNick>(packet as ClientChangeNick, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ServerChangeNick()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ServerChangeNick>>.Publish(new NetworkReceiveUnconnectedEvent<ServerChangeNick>(packet as ServerChangeNick, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ClientWhisper()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ClientWhisper>>.Publish(new NetworkReceiveUnconnectedEvent<ClientWhisper>(packet as ClientWhisper, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ServerWhisper()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ServerWhisper>>.Publish(new NetworkReceiveUnconnectedEvent<ServerWhisper>(packet as ServerWhisper, UdpManager, remoteEndPoint));
				return;
			}


			PubSub<NetworkReceiveUnconnectedEvent<byte[]>>.Publish(new NetworkReceiveUnconnectedEvent<byte[]>(reader.GetBytes(), UdpManager, remoteEndPoint));
		}

		public void OnNetworkLatencyUpdate(UdpPeer peer, int latency)
		{
			PubSub<NetworkLatencyEvent>.Publish(new NetworkLatencyEvent(latency, UdpManager, peer));
		}
	}
}
