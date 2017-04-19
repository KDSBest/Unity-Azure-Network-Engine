using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.NetworkStatistic;
using ReliableUdp.PacketHandler;

using Factory = Utility.Factory;

namespace ReliableUdp
{
	using System.Runtime.Serialization;

	using BitUtility;
	using Channel;
	using Const;
	using Enums;
	using Logging;
	using Packet;
	using Utility;

	public class UdpPeer
	{
		// Common            
		private readonly UdpManager peerListener;
		private readonly UdpPacketPool packetPool;
		private readonly object flushLock = new object();

		public UdpEndPoint EndPoint { get; private set; }

		// Channels
		public Dictionary<ChannelType, IChannel> Channels = new Dictionary<ChannelType, IChannel>();

		public NetworkStatisticManagement NetworkStatisticManagement { get; set; }
		public PingPongHandler PacketPingPongHandler { get; set; }
		public MtuHandler PacketMtuHandler { get; set; }
		public MergeHandler PacketMergeHandler { get; set; }

		public const int PROTOCOL_ID = 1;

		private ushort fragmentId;
		private readonly Dictionary<ushort, IncomingFragments> holdedFragments = new Dictionary<ushort, IncomingFragments>();
		private readonly Dictionary<ushort, IncomingFragments> holdedFragmentsForAck = new Dictionary<ushort, IncomingFragments>();

		// Connection
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

		public UdpManager UdpManager
		{
			get { return this.peerListener; }
		}

		public UdpPeer(UdpManager peerListener, UdpEndPoint endPoint, long connectId)
		{
			this.packetPool = peerListener.PacketPool;
			this.peerListener = peerListener;
			this.EndPoint = endPoint;

			this.NetworkStatisticManagement = new NetworkStatisticManagement();
			this.PacketPingPongHandler = new PingPongHandler();
			this.PacketMtuHandler = new MtuHandler();
			this.PacketMergeHandler = new MergeHandler();
			this.PacketMergeHandler.Initialize(this);

			this.Channels.Add(ChannelType.Unreliable, Factory.Get<IUnreliableChannel>());
			this.Channels.Add(ChannelType.UnreliableOrdered, Factory.Get<IUnreliableOrderedChannel>());
			this.Channels.Add(ChannelType.Reliable, Factory.Get<IReliableChannel>());
			this.Channels.Add(ChannelType.ReliableOrdered, Factory.Get<IReliableOrderedChannel>());

			foreach (var chan in this.Channels.Values)
			{
				chan.Initialize(this);
			}

			this.connectAttempts = 0;
			if (connectId == 0)
			{
				this.connectId = DateTime.UtcNow.Ticks;
				this.SendConnectRequest();
			}
			else
			{
				this.connectId = connectId;
				this.connectionState = ConnectionState.Connected;
				this.SendConnectAccept();
			}

			Factory.Get<IUdpLogger>().Log($"Connection Id is {this.connectId}.");
		}

		private void SendConnectRequest()
		{
			byte[] keyData = Encoding.UTF8.GetBytes(this.peerListener.ConnectKey);

			var connectPacket = this.packetPool.Get(PacketType.ConnectRequest, 12 + keyData.Length);

			BitHelper.GetBytes(connectPacket.RawData, 1, PROTOCOL_ID);
			BitHelper.GetBytes(connectPacket.RawData, 5, this.connectId);
			Buffer.BlockCopy(keyData, 0, connectPacket.RawData, 13, keyData.Length);

			this.peerListener.SendRawAndRecycle(connectPacket, this.EndPoint);
		}

		private void SendConnectAccept()
		{
			this.NetworkStatisticManagement.PacketReceived();

			var connectPacket = this.packetPool.Get(PacketType.ConnectAccept, 8);
			BitHelper.GetBytes(connectPacket.RawData, 1, this.connectId);
			this.peerListener.SendRawAndRecycle(connectPacket, this.EndPoint);
		}

		public bool ProcessConnectAccept(UdpPacket packet)
		{
			if (this.connectionState != ConnectionState.InProgress)
				return false;

			// check connection id
			if (BitConverter.ToInt64(packet.RawData, 1) != this.connectId)
			{
				return false;
			}

			Factory.Get<IUdpLogger>().Log("Received Connection accepted.");
			this.NetworkStatisticManagement.PacketReceived();
			this.connectionState = ConnectionState.Connected;
			return true;
		}

		private static PacketType SendOptionsToProperty(ChannelType options)
		{
			switch (options)
			{
				case ChannelType.Reliable:
					return PacketType.Reliable;
				case ChannelType.UnreliableOrdered:
					return PacketType.UnreliableOrdered;
				case ChannelType.ReliableOrdered:
					return PacketType.ReliableOrdered;
				default:
					return PacketType.Unreliable;
			}
		}

		public int GetMaxSinglePacketSize(ChannelType options)
		{
			return this.PacketMtuHandler.Mtu - UdpPacket.GetHeaderSize(SendOptionsToProperty(options));
		}

		public void Send(byte[] data, ChannelType channelType)
		{
			this.Send(data, 0, data.Length, channelType);
		}

		public void Send(UdpDataWriter dataWriter, ChannelType channelType)
		{
			this.Send(dataWriter.Data, 0, dataWriter.Length, channelType);
		}

		public void Send(IProtocolPacket packet, ChannelType channelType)
		{
			var dataWriter = new UdpDataWriter();
			dataWriter.Reset();
			packet.Serialize(dataWriter);
			this.Send(dataWriter, channelType);
		}

		public void Send(byte[] data, int start, int length, ChannelType options)
		{
			PacketType type = SendOptionsToProperty(options);
			int headerSize = UdpPacket.GetHeaderSize(type);

			if (length + headerSize > this.PacketMtuHandler.Mtu)
			{
				if (options == ChannelType.UnreliableOrdered || options == ChannelType.Unreliable)
				{
					throw new Exception("Unreliable packet size > allowed (" + (this.PacketMtuHandler.Mtu - headerSize) + ")");
				}

				int packetFullSize = this.PacketMtuHandler.Mtu - headerSize;
				int packetDataSize = packetFullSize - HeaderSize.FRAGMENT;

				int fullPacketsCount = length / packetDataSize;
				int lastPacketSize = length % packetDataSize;
				int totalPackets = fullPacketsCount + (lastPacketSize == 0 ? 0 : 1);

				Factory.Get<IUdpLogger>().Log(string.Format("FragmentSend:\n" +
							  " MTU: {0}\n" +
							  " headerSize: {1}\n" +
							  " packetFullSize: {2}\n" +
							  " packetDataSize: {3}\n" +
							  " fullPacketsCount: {4}\n" +
							  " lastPacketSize: {5}\n" +
							  " totalPackets: {6}",
					 this.PacketMtuHandler.Mtu, headerSize, packetFullSize, packetDataSize, fullPacketsCount, lastPacketSize, totalPackets));

				if (totalPackets > ushort.MaxValue)
				{
					throw new Exception("Too many fragments: " + totalPackets + " > " + ushort.MaxValue);
				}

				int dataOffset = headerSize + HeaderSize.FRAGMENT;
				for (ushort i = 0; i < fullPacketsCount; i++)
				{
					UdpPacket p = this.packetPool.Get(type, packetFullSize);
					p.FragmentId = this.fragmentId;
					p.FragmentPart = i;
					p.FragmentsTotal = (ushort)totalPackets;
					p.IsFragmented = true;
					Buffer.BlockCopy(data, i * packetDataSize, p.RawData, dataOffset, packetDataSize);
					this.SendPacket(p);
				}

				if (lastPacketSize > 0)
				{
					UdpPacket p = this.packetPool.Get(type, lastPacketSize + HeaderSize.FRAGMENT);
					p.FragmentId = this.fragmentId;
					p.FragmentPart = (ushort)fullPacketsCount; // last
					p.FragmentsTotal = (ushort)totalPackets;
					p.IsFragmented = true;
					Buffer.BlockCopy(data, fullPacketsCount * packetDataSize, p.RawData, dataOffset, lastPacketSize);
					this.SendPacket(p);
				}

				this.fragmentId++;
				return;
			}

			UdpPacket packet = this.packetPool.GetWithData(type, data, start, length);
			this.SendPacket(packet);
		}

		public void CreateAndSend(PacketType type, SequenceNumber sequence)
		{
			UdpPacket packet = this.packetPool.Get(type, 0);
			packet.Sequence = sequence;
			this.SendPacket(packet);
		}

		public void SendPacket(UdpPacket packet)
		{
			Factory.Get<IUdpLogger>().Log($"Packet type {packet.Type}");
			switch (packet.Type)
			{
				case PacketType.Unreliable:
					this.Channels[ChannelType.Unreliable].AddToQueue(packet);
					break;
				case PacketType.UnreliableOrdered:
					this.Channels[ChannelType.UnreliableOrdered].AddToQueue(packet);
					break;
				case PacketType.Reliable:
					this.Channels[ChannelType.Reliable].AddToQueue(packet);
					break;
				case PacketType.ReliableOrdered:
					this.Channels[ChannelType.ReliableOrdered].AddToQueue(packet);
					break;
				case PacketType.MtuCheck:
					this.PacketMtuHandler.SendPacket(this, packet);
					break;
				case PacketType.AckReliable:
				case PacketType.AckReliableOrdered:
				case PacketType.Ping:
				case PacketType.Pong:
				case PacketType.Disconnect:
				case PacketType.MtuOk:
					this.SendRawData(packet);
					this.packetPool.Recycle(packet);
					break;
				default:
					throw new Exception("Unknown packet type: " + packet.Type);
			}
		}

		public void AddIncomingAck(UdpPacket p, ChannelType channel)
		{
			if (p.IsFragmented)
			{
				Factory.Get<IUdpLogger>().Log($"Fragment. Id: {p.FragmentId}, Part: {p.FragmentPart}, Total: {p.FragmentsTotal}");

				// Get needed array from dictionary
				ushort packetFragId = p.FragmentId;
				IncomingFragments incomingFragments;
				if (!this.holdedFragmentsForAck.TryGetValue(packetFragId, out incomingFragments))
				{
					incomingFragments = new IncomingFragments
					{
						Fragments = new UdpPacket[p.FragmentsTotal]
					};
					this.holdedFragmentsForAck.Add(packetFragId, incomingFragments);
				}

				// Cache
				var fragments = incomingFragments.Fragments;

				// Error check
				if (p.FragmentPart >= fragments.Length || fragments[p.FragmentPart] != null)
				{
					Factory.Get<IUdpLogger>().Log($"Invalid fragment packet.");
					return;
				}

				// Fill array
				fragments[p.FragmentPart] = p;

				// Increase received fragments count
				incomingFragments.ReceivedCount++;

				// Increase total size
				int dataOffset = p.GetHeaderSize() + HeaderSize.FRAGMENT;
				incomingFragments.TotalSize += p.Size - dataOffset;

				// Check for finish
				if (incomingFragments.ReceivedCount != fragments.Length)
				{
					return;
				}

				Factory.Get<IUdpLogger>().Log($"Received all fragments.");
				UdpPacket resultingPacket = this.packetPool.Get(p.Type, incomingFragments.TotalSize);

				int resultingPacketOffset = resultingPacket.GetHeaderSize();
				int firstFragmentSize = fragments[0].Size - dataOffset;
				for (int i = 0; i < incomingFragments.ReceivedCount; i++)
				{
					// Create resulting big packet
					int fragmentSize = fragments[i].Size - dataOffset;
					Buffer.BlockCopy(
						 fragments[i].RawData,
						 dataOffset,
						 resultingPacket.RawData,
						 resultingPacketOffset + firstFragmentSize * i,
						 fragmentSize);

					this.packetPool.Recycle(fragments[i]);
					fragments[i] = null;
				}

				// Send to process
				this.peerListener.ReceiveAckFromPeer(resultingPacket, this.EndPoint, channel);

				// Clear memory
				this.packetPool.Recycle(resultingPacket);
				this.holdedFragmentsForAck.Remove(packetFragId);
			}
			else
			{
				// Just simple packet
				this.peerListener.ReceiveAckFromPeer(p, this.EndPoint, channel);
				this.packetPool.Recycle(p);
			}
		}

		public void AddIncomingPacket(UdpPacket p, ChannelType channel)
		{
			if (p.IsFragmented)
			{
				Factory.Get<IUdpLogger>().Log($"Fragment. Id: {p.FragmentId}, Part: {p.FragmentPart}, Total: {p.FragmentsTotal}");

				// Get needed array from dictionary
				ushort packetFragId = p.FragmentId;
				IncomingFragments incomingFragments;
				if (!this.holdedFragments.TryGetValue(packetFragId, out incomingFragments))
				{
					incomingFragments = new IncomingFragments { Fragments = new UdpPacket[p.FragmentsTotal] };
					this.holdedFragments.Add(packetFragId, incomingFragments);
				}

				// Cache
				var fragments = incomingFragments.Fragments;

				// Error check
				if (p.FragmentPart >= fragments.Length || fragments[p.FragmentPart] != null)
				{
					this.packetPool.Recycle(p);
					Factory.Get<IUdpLogger>().Log($"Invalid fragment packet.");
					return;
				}

				// Fill array
				fragments[p.FragmentPart] = p;

				// Increase received fragments count
				incomingFragments.ReceivedCount++;

				// Increase total size
				int dataOffset = p.GetHeaderSize() + HeaderSize.FRAGMENT;
				incomingFragments.TotalSize += p.Size - dataOffset;

				// Check for finish
				if (incomingFragments.ReceivedCount != fragments.Length)
				{
					return;
				}

				Factory.Get<IUdpLogger>().Log($"Received all fragments.");
				UdpPacket resultingPacket = this.packetPool.Get(p.Type, incomingFragments.TotalSize);

				int resultingPacketOffset = resultingPacket.GetHeaderSize();
				int firstFragmentSize = fragments[0].Size - dataOffset;
				for (int i = 0; i < incomingFragments.ReceivedCount; i++)
				{
					// Create resulting big packet
					int fragmentSize = fragments[i].Size - dataOffset;
					Buffer.BlockCopy(fragments[i].RawData, dataOffset, resultingPacket.RawData, resultingPacketOffset + firstFragmentSize * i, fragmentSize);

					// Free memory
					this.packetPool.Recycle(fragments[i]);
					fragments[i] = null;
				}

				// Send to process
				this.peerListener.ReceiveFromPeer(resultingPacket, this.EndPoint, channel);

				// Clear memory
				this.packetPool.Recycle(resultingPacket);
				this.holdedFragments.Remove(packetFragId);
			}
			else
			{
				// Just simple packet
				this.peerListener.ReceiveFromPeer(p, this.EndPoint, channel);
				this.packetPool.Recycle(p);
			}
		}

		// Process incoming packet
		public void ProcessPacket(UdpPacket packet)
		{
			this.NetworkStatisticManagement.PacketReceived();

			Factory.Get<IUdpLogger>().Log($"Packet type {packet.Type}");
			switch (packet.Type)
			{
				case PacketType.ConnectRequest:
					// response with connect
					long newId = BitConverter.ToInt64(packet.RawData, 1);
					if (newId > this.connectId)
					{
						this.connectId = newId;
					}

					Factory.Get<IUdpLogger>().Log($"Connect Request Last Id {this.ConnectId} NewId {newId} EP {this.EndPoint}");
					this.SendConnectAccept();
					this.packetPool.Recycle(packet);
					break;

				case PacketType.Merged:
					this.PacketMergeHandler.ProcessPacket(this, packet);
					break;

				case PacketType.Ping:
					this.PacketPingPongHandler.HandlePing(this, packet);
					break;

				case PacketType.Pong:
					this.PacketPingPongHandler.HandlePong(this, packet);
					break;

				// Process ack
				case PacketType.AckReliable:
					this.Channels[ChannelType.Reliable].ProcessAck(packet);
					this.packetPool.Recycle(packet);
					break;

				case PacketType.AckReliableOrdered:
					this.Channels[ChannelType.ReliableOrdered].ProcessAck(packet);
					this.packetPool.Recycle(packet);
					break;
				case PacketType.UnreliableOrdered:
					this.Channels[ChannelType.UnreliableOrdered].ProcessPacket(packet);
					break;
				case PacketType.Reliable:
					this.Channels[ChannelType.Reliable].ProcessPacket(packet);
					break;
				case PacketType.ReliableOrdered:
					this.Channels[ChannelType.ReliableOrdered].ProcessPacket(packet);
					break;
				case PacketType.Unreliable:
					this.Channels[ChannelType.Unreliable].ProcessPacket(packet);
					return;

				case PacketType.MtuCheck:
				case PacketType.MtuOk:
					this.PacketMtuHandler.ProcessMtuPacket(this, packet);
					break;

				default:
					Factory.Get<IUdpLogger>().Log($"Error! Unexpected packet type {packet.Type}");
					break;
			}
		}

		public void SendRawData(UdpPacket packet)
		{
			if (this.PacketMergeHandler.SendRawData(this, packet))
			{
				return;
			}

			Factory.Get<IUdpLogger>().Log($"Sending Packet {packet.Type}");
			this.peerListener.SendRaw(packet.RawData, 0, packet.Size, this.EndPoint);
		}

		public bool SendRaw(byte[] message, int start, int length, UdpEndPoint endPoint)
		{
			return this.peerListener.SendRaw(message, start, length, endPoint);
		}

		private void SendQueuedPackets(int currentMaxSend)
		{
			int currentSended = 0;
			while (currentSended < currentMaxSend)
			{
				// Get one of packets
				if (this.Channels[ChannelType.ReliableOrdered].SendNextPacket() ||
					 this.Channels[ChannelType.Reliable].SendNextPacket() ||
					 this.Channels[ChannelType.UnreliableOrdered].SendNextPacket() ||
					 this.Channels[ChannelType.Unreliable].SendNextPacket())
				{
					currentSended++;
				}
				else
				{
					// no outgoing packets
					break;
				}
			}

			this.NetworkStatisticManagement.FlowManagement.IncreaseSendedPacketCount(currentSended);

			this.PacketMergeHandler.SendQueuedPackets(this);
		}

		public void Flush()
		{
			lock (this.flushLock)
			{
				this.SendQueuedPackets(int.MaxValue);
			}
		}

		public void Update(int deltaTime)
		{
			if (this.connectionState == ConnectionState.Disconnected)
			{
				return;
			}

			if (this.connectionState == ConnectionState.InProgress)
			{
				this.connectTimer += deltaTime;
				if (this.connectTimer > this.peerListener.ReconnectDelay)
				{
					this.connectTimer = 0;
					this.connectAttempts++;
					if (this.connectAttempts > this.peerListener.MaxConnectAttempts)
					{
						this.connectionState = ConnectionState.Disconnected;
						return;
					}

					// else send connect again
					this.SendConnectRequest();
				}

				return;
			}

			int currentMaxSend = this.NetworkStatisticManagement.FlowManagement.GetCurrentMaxSend(deltaTime);

			foreach (var chan in this.Channels.Values)
			{
				chan.SendAcks();
			}

			this.NetworkStatisticManagement.Update(this, deltaTime, this.peerListener.ConnectionLatencyUpdated);
			this.PacketPingPongHandler.Update(this, deltaTime);

			this.PacketMtuHandler.Update(this, deltaTime);

			// Pending send
			lock (this.flushLock)
			{
				this.SendQueuedPackets(currentMaxSend);
			}
		}

		// For channels
		public void Recycle(UdpPacket packet)
		{
			this.packetPool.Recycle(packet);
		}

		public UdpPacket GetPacketFromPool(PacketType type, int bytesCount)
		{
			return this.packetPool.Get(type, bytesCount);
		}

		public bool SendRawAndRecycle(UdpPacket packet, UdpEndPoint peerEndPoint)
		{
			return this.peerListener.SendRawAndRecycle(packet, peerEndPoint);
		}

		public UdpPacket GetAndRead(byte[] packetRawData, int pos, ushort size)
		{
			return this.packetPool.GetAndRead(packetRawData, pos, size);
		}
	}
}
