using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.BitUtility;
using ReliableUdp.Enums;
using ReliableUdp.Logging;
using ReliableUdp.Packet;

using Utility;

namespace ReliableUdp.PacketHandler
{
	public class ConnectionRequestHandler
	{
		public const int PROTOCOL_ID = 1;

		private int connectAttempts;
		private int connectTimer;
		private long connectId;
		private ConnectionState connectionState = ConnectionState.InProgress;

		public ConnectionState ConnectionState
		{
			get { return this.connectionState; }
		}

		public long ConnectId
		{
			get { return this.connectId; }
		}

		public ConnectionRequestHandler()
		{
			this.connectAttempts = 0;
		}

		public void Initialize(UdpPeer peer, long connectId)
		{
			if (connectId == 0)
			{
				this.connectId = DateTime.UtcNow.Ticks;
				this.SendConnectRequest(peer);
			}
			else
			{
				this.connectId = connectId;
				this.connectionState = ConnectionState.Connected;
				this.SendConnectAccept(peer);
			}

			// Factory.Get<IUdpLogger>().Log($"Connection Id is {this.connectId}.");
		}

		private void SendConnectRequest(UdpPeer peer)
		{
			byte[] keyData = Encoding.UTF8.GetBytes(peer.Settings.ConnectKey);

			var connectPacket = peer.GetPacketFromPool(PacketType.ConnectRequest, 12 + keyData.Length);

			BitHelper.GetBytes(connectPacket.RawData, 1, PROTOCOL_ID);
			BitHelper.GetBytes(connectPacket.RawData, 5, this.connectId);
			Buffer.BlockCopy(keyData, 0, connectPacket.RawData, 13, keyData.Length);

			peer.SendRawAndRecycle(connectPacket, peer.EndPoint);
		}

		private void SendConnectAccept(UdpPeer peer)
		{
			peer.NetworkStatisticManagement.ResetTimeSinceLastPacket();

			var connectPacket = peer.GetPacketFromPool(PacketType.ConnectAccept, 8);
			BitHelper.GetBytes(connectPacket.RawData, 1, this.connectId);
			peer.SendRawAndRecycle(connectPacket, peer.EndPoint);
		}

		public bool ProcessConnectAccept(UdpPeer peer, UdpPacket packet)
		{
			if (this.connectionState != ConnectionState.InProgress)
				return false;

			// check connection id
			if (BitConverter.ToInt64(packet.RawData, 1) != this.connectId)
			{
				return false;
			}

			peer.NetworkStatisticManagement.ResetTimeSinceLastPacket();
			this.connectionState = ConnectionState.Connected;
			// Factory.Get<IUdpLogger>().Log("Received Connection accepted.");
			return true;
		}

		public void ProcessPacket(UdpPeer peer, UdpPacket packet)
		{
			long newId = BitConverter.ToInt64(packet.RawData, 1);
			if (newId > this.connectId)
			{
				this.connectId = newId;
			}

			// Factory.Get<IUdpLogger>().Log($"Connect Request Last Id {this.ConnectId} NewId {newId} EP {peer.EndPoint}");
			this.SendConnectAccept(peer);
			peer.Recycle(packet);
		}

		public bool Update(UdpPeer peer, int deltaTime)
		{
			if (this.connectionState == ConnectionState.Disconnected)
			{
				return false;
			}

			if (this.connectionState == ConnectionState.InProgress)
			{
				this.connectTimer += deltaTime;
				if (this.connectTimer > peer.Settings.ReconnectDelay)
				{
					this.connectTimer = 0;
					this.connectAttempts++;
					if (this.connectAttempts > peer.Settings.MaxConnectAttempts)
					{
						this.connectionState = ConnectionState.Disconnected;
						return false;
					}

					this.SendConnectRequest(peer);
				}

				return false;
			}

			return true;
		}

		public void ProcessAcceptPacket(UdpPeer peer, UdpPacket packet)
		{
			if (ProcessConnectAccept(peer, packet))
			{
				peer.UdpManager.CreateConnectEvent(peer);
			}

			peer.Recycle(packet);
		}
	}
}
