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
						if ((packet = new MQuaternion()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<MQuaternion>>.Publish(new NetworkReceiveEvent<MQuaternion>(packet as MQuaternion, channel, UdpManager, peer));
				return;
			}
			if ((packet = new MVector3()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<MVector3>>.Publish(new NetworkReceiveEvent<MVector3>(packet as MVector3, channel, UdpManager, peer));
				return;
			}
			if ((packet = new Shot()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<Shot>>.Publish(new NetworkReceiveEvent<Shot>(packet as Shot, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ClientUpdate()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ClientUpdate>>.Publish(new NetworkReceiveEvent<ClientUpdate>(packet as ClientUpdate, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerUpdate()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveEvent<ServerUpdate>>.Publish(new NetworkReceiveEvent<ServerUpdate>(packet as ServerUpdate, channel, UdpManager, peer));
				return;
			}


			PubSub<NetworkReceiveEvent<byte[]>>.Publish(new NetworkReceiveEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}

		public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			IProtocolPacket packet = null;
						if ((packet = new MQuaternion()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<MQuaternion>>.Publish(new NetworkReceiveAckEvent<MQuaternion>(packet as MQuaternion, channel, UdpManager, peer));
				return;
			}
			if ((packet = new MVector3()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<MVector3>>.Publish(new NetworkReceiveAckEvent<MVector3>(packet as MVector3, channel, UdpManager, peer));
				return;
			}
			if ((packet = new Shot()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<Shot>>.Publish(new NetworkReceiveAckEvent<Shot>(packet as Shot, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ClientUpdate()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ClientUpdate>>.Publish(new NetworkReceiveAckEvent<ClientUpdate>(packet as ClientUpdate, channel, UdpManager, peer));
				return;
			}
			if ((packet = new ServerUpdate()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveAckEvent<ServerUpdate>>.Publish(new NetworkReceiveAckEvent<ServerUpdate>(packet as ServerUpdate, channel, UdpManager, peer));
				return;
			}


			PubSub<NetworkReceiveAckEvent<byte[]>>.Publish(new NetworkReceiveAckEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}

		public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader)
		{
			IProtocolPacket packet = null;
						if ((packet = new MQuaternion()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<MQuaternion>>.Publish(new NetworkReceiveUnconnectedEvent<MQuaternion>(packet as MQuaternion, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new MVector3()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<MVector3>>.Publish(new NetworkReceiveUnconnectedEvent<MVector3>(packet as MVector3, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new Shot()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<Shot>>.Publish(new NetworkReceiveUnconnectedEvent<Shot>(packet as Shot, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ClientUpdate()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ClientUpdate>>.Publish(new NetworkReceiveUnconnectedEvent<ClientUpdate>(packet as ClientUpdate, UdpManager, remoteEndPoint));
				return;
			}
			if ((packet = new ServerUpdate()) != null && packet.Deserialize(reader))
			{
				PubSub<NetworkReceiveUnconnectedEvent<ServerUpdate>>.Publish(new NetworkReceiveUnconnectedEvent<ServerUpdate>(packet as ServerUpdate, UdpManager, remoteEndPoint));
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
