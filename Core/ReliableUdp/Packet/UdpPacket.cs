using System;

using ReliableUdp.BitUtility;
using ReliableUdp.Const;
using ReliableUdp.Enums;
using ReliableUdp.Utility;

namespace ReliableUdp.Packet
{
	public class UdpPacket
	{
		public const int SIZE_LIMIT = ushort.MaxValue - HeaderSize.MAX_UDP;
        public const byte FRAGMENTED_BIT = 0x80;
        public const byte PACKET_TYPE_MASK = 0x7F;

		public PacketType Type
		{
			get { return (PacketType)(this.RawData[0] & PACKET_TYPE_MASK); }
			set { this.RawData[0] = (byte)((this.RawData[0] & FRAGMENTED_BIT) | ((byte)value & PACKET_TYPE_MASK)); }
		}

		public SequenceNumber Sequence
		{
			get { return new SequenceNumber(System.BitConverter.ToUInt16(this.RawData, 1)); }
			set { BitHelper.GetBytes(this.RawData, 1, (ushort)value.Value); }
		}

		public bool IsFragmented
		{
			get { return (this.RawData[0] & FRAGMENTED_BIT) != 0; }
			set
			{
				if (value)
					this.RawData[0] |= FRAGMENTED_BIT;
				else
					this.RawData[0] &= PACKET_TYPE_MASK;
			}
		}

		public ushort FragmentId
		{
			get { return System.BitConverter.ToUInt16(this.RawData, 3); }
			set { BitHelper.GetBytes(this.RawData, 3, value); }
		}

		public ushort FragmentPart
		{
			get { return System.BitConverter.ToUInt16(this.RawData, 5); }
			set { BitHelper.GetBytes(this.RawData, 5, value); }
		}

		public ushort FragmentsTotal
		{
			get { return System.BitConverter.ToUInt16(this.RawData, 7); }
			set { BitHelper.GetBytes(this.RawData, 7, value); }
		}

		public readonly byte[] RawData;
		public int Size;

		public UdpPacket(int size)
		{
			this.RawData = new byte[size];
			this.Size = 0;
		}

		public static int GetHeaderSize(PacketType type)
		{
			return IsSequenced(type)
				 ? HeaderSize.SEQUENCED
				 : HeaderSize.DEFAULT;
		}

		public int GetHeaderSize()
		{
			return GetHeaderSize(this.Type);
		}

		public byte[] GetPacketData()
		{
			int headerSize = GetHeaderSize(this.Type);
			int dataSize = this.Size - headerSize;
			byte[] data = new byte[dataSize];
			Buffer.BlockCopy(this.RawData, headerSize, data, 0, dataSize);
			return data;
		}

		public static bool IsSequenced(PacketType type)
		{
			return type == PacketType.ReliableOrdered ||
				 type == PacketType.Reliable ||
				 type == PacketType.UnreliableOrdered ||
				 type == PacketType.Ping ||
				 type == PacketType.Pong ||
				 type == PacketType.AckReliable ||
				 type == PacketType.AckReliableOrdered;
		}

		public bool FromBytes(byte[] data, int start, int packetSize)
		{
			byte property = (byte)(data[start] & PACKET_TYPE_MASK);
			bool fragmented = (data[start] & FRAGMENTED_BIT) != 0;
			int headerSize = GetHeaderSize((PacketType)property);

			if (property >= (byte)PacketType.PacketTypeTooHigh ||
				 packetSize > SIZE_LIMIT ||
				 packetSize < headerSize ||
				 (fragmented && packetSize < headerSize + HeaderSize.FRAGMENT))
			{
				return false;
			}

			Buffer.BlockCopy(data, start, this.RawData, 0, packetSize);
			this.Size = packetSize;
			return true;
		}
	}

}
