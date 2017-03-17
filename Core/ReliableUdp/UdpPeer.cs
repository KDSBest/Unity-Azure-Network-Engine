using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Factory = Utility.Factory;

namespace ReliableUdp
{
	using System.Runtime.Serialization;

	using ReliableUdp.BitUtility;
	using ReliableUdp.Channel;
	using ReliableUdp.Const;
	using ReliableUdp.Enums;
	using ReliableUdp.Logging;
	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	public sealed class UdpPeer
	{
		//Flow control
		private int currentFlowMode;
		private int sendedPacketsCount;
		private int flowTimer;

		//Ping and RTT
		private int ping;
		private int rtt;
		private int avgRtt;
		private int rttCount;
		private int goodRttCount;
		private SequenceNumber pingSequence = new SequenceNumber(0);
		private SequenceNumber remotePingSequence = new SequenceNumber(0);
		private double resendDelay = 27.0;

		private int pingSendTimer;
		private const int RTT_RESET_DELAY = 1000;
		private int rttResetTimer;

		private DateTime pingTimeStart;
		private int timeSinceLastPacket;

		//Common            
		private readonly UdpEndPoint remoteEndPoint;
		private readonly UdpManager peerListener;
		private readonly UdpPacketPool packetPool;
		private readonly object flushLock = new object();

		//Channels
		public Dictionary<ChannelType, IChannel> Channels = new Dictionary<ChannelType, IChannel>();

		//MTU
		private int mtu = Const.Mtu.PossibleValues[0];
		private int mtuIdx;
		private bool finishMtu;
		private int mtuCheckTimer;
		private int mtuCheckAttempts;
		private const int MTU_CHECK_DELAY = 1000;
		private const int MAX_MTU_CHECK_ATTEMPTS = 4;

		public const int PROTOCOL_ID = 1;

		public const int FLOW_INCREASE_THRESHOLD = 4;
		public const int FLOW_UPDATE_TIME = 1000;

		private readonly object mtuMutex = new object();

		private ushort fragmentId;
		private readonly Dictionary<ushort, IncomingFragments> holdedFragments = new Dictionary<ushort, IncomingFragments>();
		private readonly Dictionary<ushort, IncomingFragments> holdedFragmentsForAck = new Dictionary<ushort, IncomingFragments>();

		//Merging
		private readonly UdpPacket mergeData;
		private int mergePos;
		private int mergeCount;

		//Connection
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

		public UdpEndPoint EndPoint
		{
			get { return this.remoteEndPoint; }
		}

		public int Ping
		{
			get { return this.ping; }
		}

		public int CurrentFlowMode
		{
			get { return this.currentFlowMode; }
		}

		public int Mtu
		{
			get { return this.mtu; }
		}

		public int TimeSinceLastPacket
		{
			get { return this.timeSinceLastPacket; }
		}

		public UdpManager UdpManager
		{
			get { return this.peerListener; }
		}

		public double ResendDelay
		{
			get { return this.resendDelay; }
		}

		/// <summary>
		/// Application defined object containing data about the connection
		/// </summary>
		public object Tag;

		public UdpPeer(UdpManager peerListener, UdpEndPoint remoteEndPoint, long connectId)
		{
			this.packetPool = peerListener.PacketPool;
			this.peerListener = peerListener;
			this.remoteEndPoint = remoteEndPoint;

			this.avgRtt = 0;
			this.rtt = 0;
			this.pingSendTimer = 0;

			Channels.Add(ChannelType.Unreliable, Factory.Get<IUnreliableChannel>());
			Channels.Add(ChannelType.UnreliableOrdered, Factory.Get<IUnreliableOrderedChannel>());
			Channels.Add(ChannelType.Reliable, Factory.Get<IReliableChannel>());
			Channels.Add(ChannelType.ReliableOrdered, Factory.Get<IReliableOrderedChannel>());

			foreach (var chan in this.Channels.Values)
			{
				chan.Initialize(this);
			}

			this.mergeData = this.packetPool.Get(PacketType.Merged, Const.Mtu.MaxPacketSize);

			//if ID != 0 then we already connected
			this.connectAttempts = 0;
			if (connectId == 0)
			{
				this.connectId = DateTime.UtcNow.Ticks;
				SendConnectRequest();
			}
			else
			{
				this.connectId = connectId;
				this.connectionState = ConnectionState.Connected;
				SendConnectAccept();
			}

			Factory.Get<IUdpLogger>().Log($"Connection Id is {this.connectId}.");
		}

		private void SendConnectRequest()
		{
			//Get connect key bytes
			byte[] keyData = Encoding.UTF8.GetBytes(this.peerListener.ConnectKey);

			//Make initial packet
			var connectPacket = this.packetPool.Get(PacketType.ConnectRequest, 12 + keyData.Length);

			//Add data
			BitHelper.GetBytes(connectPacket.RawData, 1, PROTOCOL_ID);
			BitHelper.GetBytes(connectPacket.RawData, 5, this.connectId);
			Buffer.BlockCopy(keyData, 0, connectPacket.RawData, 13, keyData.Length);

			//Send raw
			this.peerListener.SendRawAndRecycle(connectPacket, this.remoteEndPoint);
		}

		private void SendConnectAccept()
		{
			//Reset connection timer
			this.timeSinceLastPacket = 0;

			//Make initial packet
			var connectPacket = this.packetPool.Get(PacketType.ConnectAccept, 8);

			//Add data
			BitHelper.GetBytes(connectPacket.RawData, 1, this.connectId);

			//Send raw
			this.peerListener.SendRawAndRecycle(connectPacket, this.remoteEndPoint);
		}

		public bool ProcessConnectAccept(UdpPacket packet)
		{
			if (this.connectionState != ConnectionState.InProgress)
				return false;

			//check connection id
			if (System.BitConverter.ToInt64(packet.RawData, 1) != this.connectId)
			{
				return false;
			}

			Factory.Get<IUdpLogger>().Log("Received Connection accepted.");
			this.timeSinceLastPacket = 0;
			this.connectionState = ConnectionState.Connected;
			return true;
		}

		private static PacketType SendOptionsToProperty(Enums.ChannelType options)
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

		public int GetMaxSinglePacketSize(Enums.ChannelType options)
		{
			return this.mtu - UdpPacket.GetHeaderSize(SendOptionsToProperty(options));
		}

		public void Send(byte[] data, Enums.ChannelType channelType)
		{
			Send(data, 0, data.Length, channelType);
		}

		public void Send(UdpDataWriter dataWriter, Enums.ChannelType channelType)
		{
			Send(dataWriter.Data, 0, dataWriter.Length, channelType);
		}

		public void Send(IProtocolPacket packet, Enums.ChannelType channelType)
		{
			var dataWriter = new UdpDataWriter();
			dataWriter.Reset();
			packet.Serialize(dataWriter);
			Send(dataWriter, channelType);
		}

		public void Send(byte[] data, int start, int length, Enums.ChannelType options)
		{
			//Prepare
			PacketType type = SendOptionsToProperty(options);
			int headerSize = UdpPacket.GetHeaderSize(type);

			//Check fragmentation
			if (length + headerSize > this.mtu)
			{
				if (options == Enums.ChannelType.UnreliableOrdered || options == Enums.ChannelType.Unreliable)
				{
					throw new Exception("Unreliable packet size > allowed (" + (this.mtu - headerSize) + ")");
				}

				int packetFullSize = this.mtu - headerSize;
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
					 this.mtu, headerSize, packetFullSize, packetDataSize, fullPacketsCount, lastPacketSize, totalPackets));

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
					SendPacket(p);
				}

				if (lastPacketSize > 0)
				{
					UdpPacket p = this.packetPool.Get(type, lastPacketSize + HeaderSize.FRAGMENT);
					p.FragmentId = this.fragmentId;
					p.FragmentPart = (ushort)fullPacketsCount; //last
					p.FragmentsTotal = (ushort)totalPackets;
					p.IsFragmented = true;
					Buffer.BlockCopy(data, fullPacketsCount * packetDataSize, p.RawData, dataOffset, lastPacketSize);
					SendPacket(p);
				}

				this.fragmentId++;
				return;
			}

			//Else just send
			UdpPacket packet = this.packetPool.GetWithData(type, data, start, length);
			SendPacket(packet);
		}

		private void CreateAndSend(PacketType type, SequenceNumber sequence)
		{
			UdpPacket packet = this.packetPool.Get(type, 0);
			packet.Sequence = sequence;
			SendPacket(packet);
		}

		//from user thread, our thread, or recv?
		private void SendPacket(UdpPacket packet)
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
					//Must check result for MTU fix
					if (!this.peerListener.SendRawAndRecycle(packet, this.remoteEndPoint))
					{
						this.finishMtu = true;
					}
					break;
				case PacketType.AckReliable:
				case PacketType.AckReliableOrdered:
				case PacketType.Ping:
				case PacketType.Pong:
				case PacketType.Disconnect:
				case PacketType.MtuOk:
					SendRawData(packet);
					this.packetPool.Recycle(packet);
					break;
				default:
					throw new Exception("Unknown packet type: " + packet.Type);
			}
		}

		private void UpdateRoundTripTime(int roundTripTime)
		{
			//Calc average round trip time
			this.rtt += roundTripTime;
			this.rttCount++;
			this.avgRtt = this.rtt / this.rttCount;

			//flowmode 0 = fastest
			//flowmode max = lowest

			if (this.avgRtt < this.peerListener.GetStartRtt(this.currentFlowMode - 1))
			{
				if (this.currentFlowMode <= 0)
				{
					//Already maxed
					return;
				}

				this.goodRttCount++;
				if (this.goodRttCount > FLOW_INCREASE_THRESHOLD)
				{
					this.goodRttCount = 0;
					this.currentFlowMode--;

					Factory.Get<IUdpLogger>().Log($"Increased flow speed, RTT {this.avgRtt}, PPS {this.peerListener.GetPacketsPerSecond(this.currentFlowMode)}");
				}
			}
			else if (this.avgRtt > this.peerListener.GetStartRtt(this.currentFlowMode))
			{
				this.goodRttCount = 0;
				if (this.currentFlowMode < this.peerListener.GetMaxFlowMode())
				{
					this.currentFlowMode++;
					Factory.Get<IUdpLogger>().Log($"Decreased flow speed, RTT {this.avgRtt}, PPS {this.peerListener.GetPacketsPerSecond(this.currentFlowMode)}");
				}
			}

			//recalc resend delay
			double avgRtt = this.avgRtt;
			if (avgRtt <= 0.0)
				avgRtt = 0.1;
			this.resendDelay = 25 + (avgRtt * 2.1); // 25 ms + double rtt
		}

		public void AddIncomingAck(UdpPacket p, ChannelType channel)
		{
			if (p.IsFragmented)
			{
				Factory.Get<IUdpLogger>().Log($"Fragment. Id: {p.FragmentId}, Part: {p.FragmentPart}, Total: {p.FragmentsTotal}");

				//Get needed array from dictionary
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

				//Cache
				var fragments = incomingFragments.Fragments;

				//Error check
				if (p.FragmentPart >= fragments.Length || fragments[p.FragmentPart] != null)
				{
					Factory.Get<IUdpLogger>().Log($"Invalid fragment packet.");
					return;
				}

				//Fill array
				fragments[p.FragmentPart] = p;

				//Increase received fragments count
				incomingFragments.ReceivedCount++;

				//Increase total size
				int dataOffset = p.GetHeaderSize() + HeaderSize.FRAGMENT;
				incomingFragments.TotalSize += p.Size - dataOffset;

				//Check for finish
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
					//Create resulting big packet
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

				//Send to process
				this.peerListener.ReceiveAckFromPeer(resultingPacket, this.remoteEndPoint, channel);

				//Clear memory
				this.packetPool.Recycle(resultingPacket);
				this.holdedFragmentsForAck.Remove(packetFragId);
			}
			else //Just simple packet
			{
				this.peerListener.ReceiveAckFromPeer(p, this.remoteEndPoint, channel);
				this.packetPool.Recycle(p);
			}
		}

		public void AddIncomingPacket(UdpPacket p, ChannelType channel)
		{
			if (p.IsFragmented)
			{
				Factory.Get<IUdpLogger>().Log($"Fragment. Id: {p.FragmentId}, Part: {p.FragmentPart}, Total: {p.FragmentsTotal}");

				//Get needed array from dictionary
				ushort packetFragId = p.FragmentId;
				IncomingFragments incomingFragments;
				if (!this.holdedFragments.TryGetValue(packetFragId, out incomingFragments))
				{
					incomingFragments = new IncomingFragments
					{
						Fragments = new UdpPacket[p.FragmentsTotal]
					};
					this.holdedFragments.Add(packetFragId, incomingFragments);
				}

				//Cache
				var fragments = incomingFragments.Fragments;

				//Error check
				if (p.FragmentPart >= fragments.Length || fragments[p.FragmentPart] != null)
				{
					this.packetPool.Recycle(p);
					Factory.Get<IUdpLogger>().Log($"Invalid fragment packet.");
					return;
				}
				//Fill array
				fragments[p.FragmentPart] = p;

				//Increase received fragments count
				incomingFragments.ReceivedCount++;

				//Increase total size
				int dataOffset = p.GetHeaderSize() + HeaderSize.FRAGMENT;
				incomingFragments.TotalSize += p.Size - dataOffset;

				//Check for finish
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
					//Create resulting big packet
					int fragmentSize = fragments[i].Size - dataOffset;
					Buffer.BlockCopy(
						 fragments[i].RawData,
						 dataOffset,
						 resultingPacket.RawData,
						 resultingPacketOffset + firstFragmentSize * i,
						 fragmentSize);

					//Free memory
					this.packetPool.Recycle(fragments[i]);
					fragments[i] = null;
				}

				//Send to process
				this.peerListener.ReceiveFromPeer(resultingPacket, this.remoteEndPoint, channel);

				//Clear memory
				this.packetPool.Recycle(resultingPacket);
				this.holdedFragments.Remove(packetFragId);
			}
			else //Just simple packet
			{
				this.peerListener.ReceiveFromPeer(p, this.remoteEndPoint, channel);
				this.packetPool.Recycle(p);
			}
		}

		private void ProcessMtuPacket(UdpPacket packet)
		{
			if (packet.Size == 1 ||
				 packet.RawData[1] >= Const.Mtu.PossibleValues.Length)
				return;

			//MTU auto increase
			if (packet.Type == PacketType.MtuCheck)
			{
				if (packet.Size != Const.Mtu.PossibleValues[packet.RawData[1]])
				{
					return;
				}
				this.mtuCheckAttempts = 0;

				Factory.Get<IUdpLogger>().Log($"MTU check. Resend {packet.RawData[1]}");
				var mtuOkPacket = this.packetPool.Get(PacketType.MtuOk, 1);
				mtuOkPacket.RawData[1] = packet.RawData[1];
				SendPacket(mtuOkPacket);
			}
			else if (packet.RawData[1] > this.mtuIdx) //MtuOk
			{
				lock (this.mtuMutex)
				{
					this.mtuIdx = packet.RawData[1];
					this.mtu = Const.Mtu.PossibleValues[this.mtuIdx];
				}
				//if maxed - finish.
				if (this.mtuIdx == Const.Mtu.PossibleValues.Length - 1)
				{
					this.finishMtu = true;
				}

				Factory.Get<IUdpLogger>().Log($"MTU is set to {this.mtu}");
			}
		}

		//Process incoming packet
		public void ProcessPacket(UdpPacket packet)
		{
			this.timeSinceLastPacket = 0;

			Factory.Get<IUdpLogger>().Log($"Packet type {packet.Type}");
			switch (packet.Type)
			{
				case PacketType.ConnectRequest:
					//response with connect
					long newId = System.BitConverter.ToInt64(packet.RawData, 1);
					if (newId > this.connectId)
					{
						this.connectId = newId;
					}

					Factory.Get<IUdpLogger>().Log($"Connect Request Last Id {ConnectId} NewId {newId} EP {this.remoteEndPoint}");
					SendConnectAccept();
					this.packetPool.Recycle(packet);
					break;

				case PacketType.Merged:
					int pos = HeaderSize.DEFAULT;
					while (pos < packet.Size)
					{
						ushort size = System.BitConverter.ToUInt16(packet.RawData, pos);
						pos += 2;
						UdpPacket mergedPacket = this.packetPool.GetAndRead(packet.RawData, pos, size);
						if (mergedPacket == null)
						{
							this.packetPool.Recycle(packet);
							break;
						}
						pos += size;
						ProcessPacket(mergedPacket);
					}
					break;
				//If we get ping, send pong
				case PacketType.Ping:
					if ((packet.Sequence - this.remotePingSequence).Value < 0)
					{
						this.packetPool.Recycle(packet);
						break;
					}

					Factory.Get<IUdpLogger>().Log("Ping receive... Send Pong...");
					this.remotePingSequence = packet.Sequence;
					this.packetPool.Recycle(packet);

					//send
					CreateAndSend(PacketType.Pong, this.remotePingSequence);
					break;

				//If we get pong, calculate ping time and rtt
				case PacketType.Pong:
					if ((packet.Sequence - this.pingSequence).Value < 0)
					{
						this.packetPool.Recycle(packet);
						break;
					}
					this.pingSequence = packet.Sequence;
					int rtt = (int)(DateTime.UtcNow - this.pingTimeStart).TotalMilliseconds;
					UpdateRoundTripTime(rtt);
					Factory.Get<IUdpLogger>().Log($"Ping {rtt}");
					this.packetPool.Recycle(packet);
					break;

				//Process ack
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
					ProcessMtuPacket(packet);
					break;

				default:
					Factory.Get<IUdpLogger>().Log($"Error! Unexpected packet type {packet.Type}");
					break;
			}
		}

		private static bool CanMerge(PacketType type)
		{
			switch (type)
			{
				case PacketType.ConnectAccept:
				case PacketType.ConnectRequest:
				case PacketType.MtuOk:
				case PacketType.Pong:
				case PacketType.Disconnect:
					return false;
				default:
					return true;
			}
		}

		public void SendRawData(UdpPacket packet)
		{
			//2 - merge byte + minimal packet size + datalen(ushort)
			if (this.peerListener.MergeEnabled &&
				 CanMerge(packet.Type) &&
				 this.mergePos + packet.Size + HeaderSize.DEFAULT * 2 + 2 < this.mtu)
			{
				BitHelper.GetBytes(this.mergeData.RawData, this.mergePos + HeaderSize.DEFAULT, (ushort)packet.Size);
				Buffer.BlockCopy(packet.RawData, 0, this.mergeData.RawData, this.mergePos + HeaderSize.DEFAULT + 2, packet.Size);
				this.mergePos += packet.Size + 2;
				this.mergeCount++;

				//DebugWriteForce("Merged: " + _mergePos + "/" + (_mtu - 2) + ", count: " + _mergeCount);
				return;
			}

			Factory.Get<IUdpLogger>().Log($"Sending Packet {packet.Type}");
			this.peerListener.SendRaw(packet.RawData, 0, packet.Size, this.remoteEndPoint);
		}

		private void SendQueuedPackets(int currentMaxSend)
		{
			int currentSended = 0;
			while (currentSended < currentMaxSend)
			{
				//Get one of packets
				if (this.Channels[ChannelType.ReliableOrdered].SendNextPacket() ||
					 this.Channels[ChannelType.Reliable].SendNextPacket() ||
					 this.Channels[ChannelType.UnreliableOrdered].SendNextPacket() ||
					 this.Channels[ChannelType.Unreliable].SendNextPacket())
				{
					currentSended++;
				}
				else
				{
					//no outgoing packets
					break;
				}
			}

			//Increase counter
			this.sendedPacketsCount += currentSended;

			//If merging enabled
			if (this.mergePos > 0)
			{
				if (this.mergeCount > 1)
				{
					Factory.Get<IUdpLogger>().Log($"Send merged {this.mergePos}, count {this.mergeCount}");
					this.peerListener.SendRaw(this.mergeData.RawData, 0, HeaderSize.DEFAULT + this.mergePos, this.remoteEndPoint);
				}
				else
				{
					//Send without length information and merging
					this.peerListener.SendRaw(this.mergeData.RawData, HeaderSize.DEFAULT + 2, this.mergePos - 2, this.remoteEndPoint);
				}
				this.mergePos = 0;
				this.mergeCount = 0;
			}
		}

		public void Flush()
		{
			lock (this.flushLock)
			{
				SendQueuedPackets(int.MaxValue);
			}
		}

		public void Update(int deltaTime)
		{
			if (this.connectionState == ConnectionState.Disconnected)
			{
				return;
			}

			this.timeSinceLastPacket += deltaTime;
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

					//else send connect again
					SendConnectRequest();
				}
				return;
			}

			//Get current flow mode
			int maxSendPacketsCount = this.peerListener.GetPacketsPerSecond(this.currentFlowMode);
			int currentMaxSend;

			if (maxSendPacketsCount > 0)
			{
				int availableSendPacketsCount = maxSendPacketsCount - this.sendedPacketsCount;
				currentMaxSend = Math.Min(availableSendPacketsCount, (maxSendPacketsCount * deltaTime) / FLOW_UPDATE_TIME);
			}
			else
			{
				currentMaxSend = int.MaxValue;
			}

			//Pending acks
			foreach (var chan in this.Channels.Values)
			{
				chan.SendAcks();
			}

			//ResetFlowTimer
			this.flowTimer += deltaTime;
			if (this.flowTimer >= FLOW_UPDATE_TIME)
			{
				Factory.Get<IUdpLogger>().Log($"Reset flow timer, sended packets {this.sendedPacketsCount}");
				this.sendedPacketsCount = 0;
				this.flowTimer = 0;
			}

			//Send ping
			this.pingSendTimer += deltaTime;
			if (this.pingSendTimer >= this.peerListener.PingInterval)
			{
				Factory.Get<IUdpLogger>().Log("Send ping...");

				//reset timer
				this.pingSendTimer = 0;

				//send ping
				CreateAndSend(PacketType.Ping, this.pingSequence);

				//reset timer
				this.pingTimeStart = DateTime.UtcNow;
			}

			//RTT - round trip time
			this.rttResetTimer += deltaTime;
			if (this.rttResetTimer >= RTT_RESET_DELAY)
			{
				this.rttResetTimer = 0;
				//Rtt update
				this.rtt = this.avgRtt;
				this.ping = this.avgRtt;
				this.peerListener.ConnectionLatencyUpdated(this, this.ping);
				this.rttCount = 1;
			}

			//MTU - Maximum transmission unit
			if (!this.finishMtu)
			{
				this.mtuCheckTimer += deltaTime;
				if (this.mtuCheckTimer >= MTU_CHECK_DELAY)
				{
					this.mtuCheckTimer = 0;
					this.mtuCheckAttempts++;
					if (this.mtuCheckAttempts >= MAX_MTU_CHECK_ATTEMPTS)
					{
						this.finishMtu = true;
					}
					else
					{
						lock (this.mtuMutex)
						{
							//Send increased packet
							if (this.mtuIdx < Const.Mtu.PossibleValues.Length - 1)
							{
								int newMtu = Const.Mtu.PossibleValues[this.mtuIdx + 1] - HeaderSize.DEFAULT;
								var p = this.packetPool.Get(PacketType.MtuCheck, newMtu);
								p.RawData[1] = (byte)(this.mtuIdx + 1);
								SendPacket(p);
							}
						}
					}
				}
			}
			//MTU - end

			//Pending send
			lock (this.flushLock)
			{
				SendQueuedPackets(currentMaxSend);
			}
		}

		//For channels
		public void Recycle(UdpPacket packet)
		{
			this.packetPool.Recycle(packet);
		}

		public UdpPacket GetPacketFromPool(PacketType type, int bytesCount)
		{
			return this.packetPool.Get(type, bytesCount);
		}
	}
}
