using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DeterministicLockstep.Packets;

using EventSystem;

namespace DeterministicLockstep.Protocol
{
	using EventSystem.Events;

	using ReliableUdp;
	using ReliableUdp.Enums;
	using ReliableUdp.Packet;
	using ReliableUdp.Utility;
	using Protocol;
	public class ProtocolListener<T> : IUdpEventListener where T : IProtocolPacket, new()
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
			if ((packet = new ClientCommand<T>()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ClientCommand<T>>>.Publish(new NetworkReceiveEvent<ClientCommand<T>>(packet as ClientCommand<T>, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerCommand<T>()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ServerCommand<T>>>.Publish(new NetworkReceiveEvent<ServerCommand<T>>(packet as ServerCommand<T>, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerTellId()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ServerTellId>>.Publish(new NetworkReceiveEvent<ServerTellId>(packet as ServerTellId, channel, UdpManager, peer));
				return;
			}


			PubSub<NetworkReceiveEvent<byte[]>>.Publish(new NetworkReceiveEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}

		public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			IProtocolPacket packet = null;
			if ((packet = new ClientCommand<T>()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ClientCommand<T>>>.Publish(new NetworkReceiveAckEvent<ClientCommand<T>>(packet as ClientCommand<T>, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerCommand<T>()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ServerCommand<T>>>.Publish(new NetworkReceiveAckEvent<ServerCommand<T>>(packet as ServerCommand<T>, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerTellId()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ServerTellId>>.Publish(new NetworkReceiveAckEvent<ServerTellId>(packet as ServerTellId, channel, UdpManager, peer));
				return;
			}


			PubSub<NetworkReceiveAckEvent<byte[]>>.Publish(new NetworkReceiveAckEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}

		public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader)
		{
			IProtocolPacket packet = null;
			if ((packet = new ClientCommand<T>()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ClientCommand<T>>>.Publish(new NetworkReceiveUnconnectedEvent<ClientCommand<T>>(packet as ClientCommand<T>, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ServerCommand<T>()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ServerCommand<T>>>.Publish(new NetworkReceiveUnconnectedEvent<ServerCommand<T>>(packet as ServerCommand<T>, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ServerTellId()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ServerTellId>>.Publish(new NetworkReceiveUnconnectedEvent<ServerTellId>(packet as ServerTellId, UdpManager, remoteEndPoint));
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

