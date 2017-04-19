namespace ChatSample
{
	using System;

	using Protocol;

	using ReliableUdp;
	using ReliableUdp.Enums;
	using ReliableUdp.Utility;

	public class ClientListener : IUdpEventListener
	{
		private readonly Action<bool> connEvent;

		public Action<ServerChatMessage, UdpPeer, ChannelType> HandleChatMessage { get; set; }

		public Action<ServerChangeNick, UdpPeer, ChannelType> HandleNickMessage { get; set; }

		public Action<ServerWhisper, UdpPeer, ChannelType> HandleWhisperMessage { get; set; }

		public Action<byte[], UdpPeer, ChannelType> HandleRawBytes { get; set; }

		public ClientListener(Action<bool> connEvent)
		{
			this.connEvent = connEvent;
		}

		public UdpManager UdpManager { get; set; }

		public void OnPeerConnected(UdpPeer peer)
		{
			Console.WriteLine("Connected. (" + UdpManager.GetFirstPeer().ConnectId + ")");
			connEvent(true);
		}

		public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
		{
			Console.WriteLine("Disconnected");
			connEvent(false);
		}

		public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
		{
			Console.WriteLine("[Client] error: " + socketErrorCode);
			connEvent(false);
		}

		public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
			var chatPacket = new ServerChatMessage();
			var wPacket = new ServerWhisper();
			var nickPacket = new ServerChangeNick();
			if (HandleChatMessage != null && chatPacket.Deserialize(reader))
			{
				HandleChatMessage(chatPacket, peer, channel);
			}
			else if (HandleNickMessage != null && nickPacket.Deserialize(reader))
			{
				HandleNickMessage(nickPacket, peer, channel);
			}
			else if (HandleWhisperMessage != null && wPacket.Deserialize(reader))
			{
				HandleWhisperMessage(wPacket, peer, channel);
			}
			else if (HandleRawBytes != null)
			{
					HandleRawBytes(reader.GetBytes(), peer, channel);
			}
		}

		public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
		{
		}

		public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader)
		{
		}

		public void OnNetworkLatencyUpdate(UdpPeer peer, int latency)
		{
		}
	}
}