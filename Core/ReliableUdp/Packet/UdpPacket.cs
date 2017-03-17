namespace ReliableUdp.Packet
{
	using System;

	using ReliableUdp.BitUtility;
	using ReliableUdp.Const;
	using ReliableUdp.Enums;
	using ReliableUdp.Utility;

	public class UdpPacket
	{
		public const int SIZE_LIMIT = ushort.MaxValue - HeaderSize.MAX_UDP;
		private const int LAST_PROPERTY = 19;

		//Header
		public PacketType Type
		{
			get { return (PacketType)(this.RawData[0] & 0x7F); }
			set { this.RawData[0] = (byte)((this.RawData[0] & 0x80) | ((byte)value & 0x7F)); }
		}

		public SequenceNumber Sequence
		{
			get { return new SequenceNumber(System.BitConverter.ToUInt16(this.RawData, 1)); }
			set { BitHelper.GetBytes(this.RawData, 1, (ushort)value.Value); }
		}

		public bool IsFragmented
		{
			get { return (this.RawData[0] & 0x80) != 0; }
			set
			{
				if (value)
					this.RawData[0] |= 0x80; //set first bit
				else
					this.RawData[0] &= 0x7F; //unset first bit
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

		//Data
		public readonly byte[] RawData;
		public int Size;

		public UdpPacket(int size)
		{
			this.RawData = new byte[size];
			this.Size = 0;
		}

		public static bool GetPacketProperty(byte[] data, out PacketType type)
		{
			byte properyByte = (byte)(data[0] & 0x7F);
			if (properyByte > LAST_PROPERTY)
			{
				type = PacketType.Unreliable;
				return false;
			}
			type = (PacketType)properyByte;
			return true;
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

		public bool IsClientData()
		{
			var property = this.Type;
			return property == PacketType.Reliable ||
					 property == PacketType.ReliableOrdered ||
					 property == PacketType.Unreliable ||
					 property == PacketType.UnreliableOrdered;
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

		//Packet contstructor from byte array
		public bool FromBytes(byte[] data, int start, int packetSize)
		{
			//Reading type
			byte property = (byte)(data[start] & 0x7F);
			bool fragmented = (data[start] & 0x80) != 0;
			int headerSize = GetHeaderSize((PacketType)property);

			if (property > LAST_PROPERTY ||
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
