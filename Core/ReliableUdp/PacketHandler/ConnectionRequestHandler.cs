﻿using System;
using System.Text;

using ReliableUdp.BitUtility;
using ReliableUdp.Enums;
using ReliableUdp.Packet;

namespace ReliableUdp.PacketHandler
{
    public class ConnectionRequestHandler
	{
		public const int PROTOCOL_ID = 1;

		private int connectAttempts;
		private int connectTimer;

        public ConnectionState ConnectionState { get; private set; } = ConnectionState.InProgress;

        public long ConnectId { get; private set; }

        public ConnectionRequestHandler()
		{
			this.connectAttempts = 0;
		}

		public void Initialize(UdpPeer peer, long connectId)
		{
			if (connectId == 0)
			{
				this.ConnectId = DateTime.UtcNow.Ticks;
				this.SendConnectRequest(peer);
			}
			else
			{
				this.ConnectId = connectId;
				this.ConnectionState = ConnectionState.Connected;
				this.SendConnectAccept(peer);
			}

			System.Diagnostics.Debug.WriteLine($"Connection Id is {this.ConnectId}.");
		}

		private void SendConnectRequest(UdpPeer peer)
		{
			byte[] keyData = Encoding.UTF8.GetBytes(peer.Settings.ConnectKey);

			var connectPacket = peer.GetPacketFromPool(PacketType.ConnectRequest, 12 + keyData.Length);

			BitHelper.GetBytes(connectPacket.RawData, 1, PROTOCOL_ID);
			BitHelper.GetBytes(connectPacket.RawData, 5, this.ConnectId);
			Buffer.BlockCopy(keyData, 0, connectPacket.RawData, 13, keyData.Length);

			peer.SendRawAndRecycle(connectPacket, peer.EndPoint);
		}

		private void SendConnectAccept(UdpPeer peer)
		{
			peer.NetworkStatisticManagement.ResetTimeSinceLastPacket();

			var connectPacket = peer.GetPacketFromPool(PacketType.ConnectAccept, 8);
			BitHelper.GetBytes(connectPacket.RawData, 1, this.ConnectId);
			peer.SendRawAndRecycle(connectPacket, peer.EndPoint);
		}

		public bool ProcessConnectAccept(UdpPeer peer, UdpPacket packet)
		{
			if (this.ConnectionState != ConnectionState.InProgress)
				return false;

			// check connection id
			if (BitConverter.ToInt64(packet.RawData, 1) != this.ConnectId)
			{
				return false;
			}

			peer.NetworkStatisticManagement.ResetTimeSinceLastPacket();
			this.ConnectionState = ConnectionState.Connected;
			System.Diagnostics.Debug.WriteLine("Received Connection accepted.");
			return true;
		}

		public void ProcessPacket(UdpPeer peer, UdpPacket packet)
		{
			long newId = BitConverter.ToInt64(packet.RawData, 1);
			if (newId > this.ConnectId)
			{
				this.ConnectId = newId;
			}

			System.Diagnostics.Debug.WriteLine($"Connect Request Last Id {this.ConnectId} NewId {newId} EP {peer.EndPoint}");
			this.SendConnectAccept(peer);
			peer.Recycle(packet);
		}

		public bool Update(UdpPeer peer, int deltaTime)
		{
			if (this.ConnectionState == ConnectionState.Disconnected)
			{
				return false;
			}

			if (this.ConnectionState == ConnectionState.InProgress)
			{
				this.connectTimer += deltaTime;
				if (this.connectTimer > peer.Settings.ReconnectDelay)
				{
					this.connectTimer = 0;
					this.connectAttempts++;
					if (this.connectAttempts > peer.Settings.MaxConnectAttempts)
					{
						this.ConnectionState = ConnectionState.Disconnected;
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
