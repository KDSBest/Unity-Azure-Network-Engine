using System;
using System.Collections.Generic;
using System.Threading;

using ReliableUdp.Const;
using ReliableUdp.Enums;
using ReliableUdp.Packet;

using ReliableUdp.Utility;

namespace ReliableUdp.Channel
{
    public class ReliableUnorderedChannel : IReliableChannel
	{
		private readonly Queue<UdpPacket> outgoingPackets;
		private readonly bool[] outgoingAcks;               //for send acks
		private readonly PendingPacket[] pendingPackets;    //for unacked packets and duplicates
		private readonly bool[] earlyReceived;              //for unordered

		private SequenceNumber localSeqence = new SequenceNumber(0);
		private SequenceNumber remoteSequence = new SequenceNumber(0);
		private SequenceNumber localWindowStart = new SequenceNumber(0);
		private SequenceNumber remoteWindowStart = new SequenceNumber(0);

		private UdpPeer peer;
		private bool mustSendAcks;

		private readonly int windowSize;
		private const int BITS_IN_BYTE = 8;

		private int queueIndex;

		public int PacketsInQueue
		{
			get { return this.outgoingPackets.Count; }
		}

		public ReliableUnorderedChannel(int windowSize)
		{
			this.windowSize = windowSize;

			this.outgoingPackets = new Queue<UdpPacket>(this.windowSize);

			this.outgoingAcks = new bool[this.windowSize];
			this.pendingPackets = new PendingPacket[this.windowSize];
			for (int i = 0; i < this.pendingPackets.Length; i++)
			{
				this.pendingPackets[i] = new PendingPacket();
			}

			this.earlyReceived = new bool[this.windowSize];
		}

		public void Initialize(UdpPeer peer)
		{
			this.peer = peer;
		}

		//ProcessAck in packet
		public void ProcessAck(UdpPacket packet)
		{
			int validPacketSize = (this.windowSize - 1) / BITS_IN_BYTE + 1 + HeaderSize.SEQUENCED;
			if (packet.Size != validPacketSize)
			{
				System.Diagnostics.Debug.WriteLine("Invalid Ack Packet Size.");
				return;
			}

			if (!packet.Sequence.IsValid)
			{
				System.Diagnostics.Debug.WriteLine("Sequence is Invalid.");
				return;
			}

			//check relevance
			if ((packet.Sequence - this.localWindowStart).Value <= -this.windowSize)
			{
				System.Diagnostics.Debug.WriteLine("Old Acks.");
				return;
			}

			byte[] acksData = packet.RawData;
			System.Diagnostics.Debug.WriteLine($"Acks beginning {packet.Sequence.Value}");
			int startByte = HeaderSize.SEQUENCED;

			Monitor.Enter(this.pendingPackets);
			for (int i = 0; i < this.windowSize; i++)
			{
				ushort ackSequenceValue = (ushort)((packet.Sequence.Value + i) % SequenceNumber.MAX_SEQUENCE);
				SequenceNumber ackSequence = new SequenceNumber(ackSequenceValue);
				if ((ackSequence - this.localWindowStart).Value < 0)
				{
					continue;
				}

				int currentByte = startByte + i / BITS_IN_BYTE;
				int currentBit = i % BITS_IN_BYTE;

				if ((acksData[currentByte] & (1 << currentBit)) == 0)
				{
					continue;
				}

				if (ackSequence == this.localWindowStart)
				{
					//Move window
					this.localWindowStart++;
				}

				UdpPacket removed = this.pendingPackets[ackSequence.Value % this.windowSize].GetAndClear();
				if (removed != null)
				{
					this.peer.AddIncomingAck(removed, ChannelType.Reliable);

					System.Diagnostics.Debug.WriteLine($"Removing reliableInOrder ack: {ackSequence.Value} - true.");
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Removing reliableInOrder ack: {ackSequence.Value} - false.");
				}
			}
			Monitor.Exit(this.pendingPackets);
		}

		public void AddToQueue(UdpPacket packet)
		{
			lock (this.outgoingPackets)
			{
				this.outgoingPackets.Enqueue(packet);
			}
		}

		private void ProcessQueuedPackets()
		{
			//get packets from queue
			while (this.outgoingPackets.Count > 0)
			{
				var relate = this.localSeqence - this.localWindowStart;
				if (relate.Value < this.windowSize)
				{
					UdpPacket packet;
					lock (this.outgoingPackets)
					{
						packet = this.outgoingPackets.Dequeue();
					}
					packet.Sequence = this.localSeqence;
					this.pendingPackets[this.localSeqence.Value % this.windowSize].Packet = packet;
					this.localSeqence++;
				}
				else //Queue filled
				{
					break;
				}
			}
		}

		public bool SendNextPacket()
		{
			//check sending acks
			DateTime currentTime = DateTime.UtcNow;

			Monitor.Enter(this.pendingPackets);
			ProcessQueuedPackets();

			//send
			PendingPacket currentPacket;
			bool packetFound = false;
			int startQueueIndex = this.queueIndex;
			do
			{
				currentPacket = this.pendingPackets[this.queueIndex];
				if (currentPacket.Packet != null)
				{
					//check send time
					if (currentPacket.TimeStamp.HasValue)
					{
						double packetHoldTime = (currentTime - currentPacket.TimeStamp.Value).TotalMilliseconds;
						if (packetHoldTime > this.peer.NetworkStatisticManagement.ResendDelay)
						{
							System.Diagnostics.Debug.WriteLine($"Resend: {(int)packetHoldTime} > {this.peer.NetworkStatisticManagement.ResendDelay}.");
							packetFound = true;
						}
					}
					else //Never sended
					{
						packetFound = true;
					}
				}

				this.queueIndex = (this.queueIndex + 1) % this.windowSize;
			} while (!packetFound && this.queueIndex != startQueueIndex);

			if (packetFound)
			{
				currentPacket.TimeStamp = DateTime.Now;
				this.peer.SendRawData(currentPacket.Packet);
				System.Diagnostics.Debug.WriteLine($"Sended.");
			}
			Monitor.Exit(this.pendingPackets);
			return packetFound;
		}

		public void SendAcks()
		{
			if (!this.mustSendAcks)
				return;
			this.mustSendAcks = false;

            System.Diagnostics.Debug.WriteLine($"Send Acks.");

            //Init packet
            int bytesCount = (this.windowSize - 1) / BITS_IN_BYTE + 1;
			PacketType packetType = PacketType.AckReliable;
			var acksPacket = this.peer.GetPacketFromPool(packetType, bytesCount);

			//For quick access
			byte[] data = acksPacket.RawData; //window start + acks size

			//Put window start
			Monitor.Enter(this.outgoingAcks);
			acksPacket.Sequence = this.remoteWindowStart;

			//Put acks
			int startAckIndex = this.remoteWindowStart.Value % this.windowSize;
			int currentAckIndex = startAckIndex;
			int currentBit = 0;
			int currentByte = HeaderSize.SEQUENCED;
			do
			{
				if (this.outgoingAcks[currentAckIndex])
				{
					data[currentByte] |= (byte)(1 << currentBit);
				}

				currentBit++;
				if (currentBit == BITS_IN_BYTE)
				{
					currentByte++;
					currentBit = 0;
				}
				currentAckIndex = (currentAckIndex + 1) % this.windowSize;
			} while (currentAckIndex != startAckIndex);
			Monitor.Exit(this.outgoingAcks);

			this.peer.SendRawData(acksPacket);
			this.peer.Recycle(acksPacket);
		}

		//Process incoming packet
		public void ProcessPacket(UdpPacket packet)
		{
			if (!packet.Sequence.IsValid)
			{
                System.Diagnostics.Debug.WriteLine("Bad Sequence.");
                return;
			}

			SequenceNumber relate = packet.Sequence - this.remoteWindowStart;
			SequenceNumber relateSeq = packet.Sequence - this.remoteSequence;

			if (relateSeq.Value > this.windowSize)
			{
                System.Diagnostics.Debug.WriteLine("Bad Sequence for window size.");
                return;
			}

			//Drop bad packets
			if (relate.Value < 0)
			{
                System.Diagnostics.Debug.WriteLine("Reliable in order too old.");
                return;
			}
			if (relate.Value >= this.windowSize * 2)
			{
                System.Diagnostics.Debug.WriteLine("Reliable in order too new.");
                return;
			}

			//If very new - move window
			Monitor.Enter(this.outgoingAcks);
			if (relate.Value >= this.windowSize)
			{
				//New window position
				int newWindowStart = (this.remoteWindowStart.Value + relate.Value - this.windowSize + 1) % SequenceNumber.MAX_SEQUENCE;

				//Clean old data
				while (this.remoteWindowStart.Value != newWindowStart)
				{
					this.outgoingAcks[this.remoteWindowStart.Value % this.windowSize] = false;
					this.remoteWindowStart++;
				}
			}

			this.mustSendAcks = true;

			if (this.outgoingAcks[packet.Sequence.Value % this.windowSize])
			{
                System.Diagnostics.Debug.WriteLine("Reliable in order duplicate.");
                Monitor.Exit(this.outgoingAcks);
				return;
			}

			//save ack
			this.outgoingAcks[packet.Sequence.Value % this.windowSize] = true;
			Monitor.Exit(this.outgoingAcks);

			//detailed check
			if (packet.Sequence == this.remoteSequence)
			{
                System.Diagnostics.Debug.WriteLine("Reliable in order packet success.");
                this.peer.AddIncomingPacket(packet, ChannelType.Reliable);
				this.remoteSequence++;

				while (this.earlyReceived[this.remoteSequence.Value % this.windowSize])
				{
					//process early packet
					this.earlyReceived[this.remoteSequence.Value % this.windowSize] = false;
					this.remoteSequence++;
				}

				return;
			}

			this.earlyReceived[packet.Sequence.Value % this.windowSize] = true;
			this.peer.AddIncomingPacket(packet, ChannelType.Reliable);
		}
	}
}
