using System.Collections.Generic;

namespace EventSystem
{
	using System.Linq;

	using Protocol.Language;

	public class CodeGenerator
	{
		public const string FileTemplate = @"using System;
using System.Collections.Generic;
using System.Linq;

using EventSystem.Events;

using ReliableUdp;
using ReliableUdp.Enums;
using ReliableUdp.Packet;
using ReliableUdp.Utility;
using Protocol;

namespace EventSystem
{{

	public class ProtocolListener : IUdpEventListener
	{{
		public UdpManager UdpManager {{ get; set; }}

		public void OnPeerConnected(UdpPeer peer)
		{{
			PubSub<NetworkConnectedEvent>.Publish(new NetworkConnectedEvent(UdpManager, peer));
		}}

		public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
		{{
			PubSub<NetworkDisconnectedEvent>.Publish(new NetworkDisconnectedEvent(UdpManager, peer));
		}}

		public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
		{{
			PubSub<NetworkErrorEvent>.Publish(new NetworkErrorEvent(socketErrorCode, UdpManager, endPoint));
		}}

		public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{{
			IProtocolPacket packet = null;
			{0}

			PubSub<NetworkReceiveEvent<byte[]>>.Publish(new NetworkReceiveEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}}

		public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{{
			IProtocolPacket packet = null;
			{1}

			PubSub<NetworkReceiveAckEvent<byte[]>>.Publish(new NetworkReceiveAckEvent<byte[]>(reader.GetBytes(), channel, UdpManager, peer));
		}}

		public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader)
		{{
			IProtocolPacket packet = null;
			{2}

			PubSub<NetworkReceiveUnconnectedEvent<byte[]>>.Publish(new NetworkReceiveUnconnectedEvent<byte[]>(reader.GetBytes(), UdpManager, remoteEndPoint));
		}}

		public void OnNetworkLatencyUpdate(UdpPeer peer, int latency)
		{{
			PubSub<NetworkLatencyEvent>.Publish(new NetworkLatencyEvent(latency, UdpManager, peer));
		}}
	}}
}}
";

		public const string PacketReceiveTemplate = @"			if ((packet = new {0}()) != null && packet.Deserialize(reader))
			{{
				PubSub<NetworkReceiveEvent<{0}>>.Publish(new NetworkReceiveEvent<{0}>(packet as {0}, channel, UdpManager, peer));
				return;
			}}
";

		public const string PacketReceiveAckTemplate = @"			if ((packet = new {0}()) != null && packet.Deserialize(reader))
			{{
				PubSub<NetworkReceiveAckEvent<{0}>>.Publish(new NetworkReceiveAckEvent<{0}>(packet as {0}, channel, UdpManager, peer));
				return;
			}}
";

		public const string PacketReceiveUnconnectedTemplate = @"			if ((packet = new {0}()) != null && packet.Deserialize(reader))
			{{
				PubSub<NetworkReceiveUnconnectedEvent<{0}>>.Publish(new NetworkReceiveUnconnectedEvent<{0}>(packet as {0}, UdpManager, remoteEndPoint));
				return;
			}}
";

		public string GenerateCode(List<Message> messages)
		{
			string code = string.Empty;
			string codeAck = string.Empty;
			string codeUnconnected = string.Empty;

			for (int i = 0; i < messages.Count; i++)
			{
				var message = messages[i];
				code += string.Format(PacketReceiveTemplate, message.Name);
				codeAck += string.Format(PacketReceiveAckTemplate, message.Name);
				codeUnconnected += string.Format(PacketReceiveUnconnectedTemplate, message.Name);
			}

			return string.Format(FileTemplate, code, codeAck, codeUnconnected);
		}
	}
}
