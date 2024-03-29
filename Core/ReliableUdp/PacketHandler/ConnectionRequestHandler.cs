﻿using System;
using System.Text;

using ReliableUdp.BitUtility;
using ReliableUdp.Encryption;
using ReliableUdp.Enums;
using ReliableUdp.Packet;

namespace ReliableUdp.PacketHandler
{
	public class ConnectionRequestHandler
	{
		public const int PROTOCOL_ID = 2;
		private readonly PacketEncryptionSystem packetEncryptionSystem;
		private int connectAttempts;
		private int connectTimer = 0;

		public ConnectionState ConnectionState { get; private set; } = ConnectionState.InProgress;

		public long ConnectId { get; private set; }

		public ConnectionRequestHandler(PacketEncryptionSystem packetEncryptionSystem)
		{
			this.packetEncryptionSystem = packetEncryptionSystem;
			this.connectAttempts = 0;
		}

		public void Initialize(UdpPeer peer, long connectId, byte[] encKey = null)
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
				this.SendConnectAccept(peer, encKey);
			}

#if UDP_DEBUGGING
			Console.WriteLine($"Connection Id is {this.ConnectId}.");
#endif
		}

		private void SendConnectRequest(UdpPeer peer)
		{
			int encExtraSize = 0;
			byte[] encKey = null;

			if (this.packetEncryptionSystem != null)
			{
				encKey = packetEncryptionSystem.InitializeAes();
				encExtraSize = encKey.Length;
			}

			var connectPacket = peer.GetPacketFromPool(PacketType.ConnectRequest, 12 + encExtraSize);

			BitHelper.Write(connectPacket.RawData, 1, PROTOCOL_ID);
			BitHelper.Write(connectPacket.RawData, 5, this.ConnectId);

			if (encExtraSize > 0 && encKey != null)
			{
				Buffer.BlockCopy(encKey, 0, connectPacket.RawData, 13, encKey.Length);
			}

			peer.SendRawAndRecycleForceNoEncryption(connectPacket, peer.EndPoint);
		}

		private void SendConnectAccept(UdpPeer peer, byte[] encKey)
		{
			if(encKey != null && this.packetEncryptionSystem != null)
			{
				this.packetEncryptionSystem.InitializeAes(encKey);
			}

			peer.NetworkStatisticManagement.ResetTimeSinceLastPacket();

			var connectPacket = peer.GetPacketFromPool(PacketType.ConnectAccept, 8);
			BitHelper.Write(connectPacket.RawData, 1, this.ConnectId);
			peer.SendRawAndRecycleForceNoEncryption(connectPacket, peer.EndPoint);
		}

		public bool ProcessConnectAccept(UdpPeer peer, UdpPacket packet)
		{
			if (this.ConnectionState != ConnectionState.InProgress)
				return false;

			if (BitConverter.ToInt64(packet.RawData, 1) != this.ConnectId)
			{
				return false;
			}

			peer.NetworkStatisticManagement.ResetTimeSinceLastPacket();
			this.ConnectionState = ConnectionState.Connected;
#if UDP_DEBUGGING
			Console.WriteLine("Received Connection accepted.");
#endif
			return true;
		}

		public void ProcessPacket(UdpPeer peer, UdpPacket packet)
		{
			long newId = BitConverter.ToInt64(packet.RawData, 5);
			if (newId > this.ConnectId)
			{
				this.ConnectId = newId;
			}

#if UDP_DEBUGGING
			Console.WriteLine($"Connect Request Last Id {this.ConnectId} NewId {newId} EP {peer.EndPoint}");
#endif
			this.SendConnectAccept(peer, packet.GetEncKey());
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
				this.connectTimer += deltaTime;

				return false;
			}

			return true;
		}

		public void ProcessAcceptPacket(UdpPeer peer, UdpPacket packet)
		{
			if (ProcessConnectAccept(peer, packet))
			{
				peer.PacketEncryptionSystem.ConnectionAccepted();
				peer.UdpManager.CreateConnectEvent(peer);
			}

			peer.Recycle(packet);
		}
	}
}
