using System;
using System.Text;

namespace ReliableUdp.Utility
{
    public class UdpDataReader
	{
		protected byte[] _data;
		protected int _position;
		protected int DataSize;

		public byte[] Data
		{
			get { return this._data; }
		}

		public int Position
		{
			get { return this._position; }
		}

		public bool EndOfData
		{
			get { return this._position == this.DataSize; }
		}

		public int AvailableBytes
		{
			get { return this.DataSize - this._position; }
		}

		public void SetSource(UdpDataWriter dataWriter)
		{
			this._data = dataWriter.Data;
			this._position = 0;
			this.DataSize = dataWriter.Length;
		}

		public void SetSource(byte[] source)
		{
			this._data = source;
			this._position = 0;
			this.DataSize = source.Length;
		}

		public void SetSource(byte[] source, int offset)
		{
			this._data = source;
			this._position = offset;
			this.DataSize = source.Length;
		}

		public void SetSource(byte[] source, int offset, int dataSize)
		{
			this._data = source;
			this._position = offset;
			this.DataSize = dataSize;
		}

		public UdpDataReader()
		{

		}

		public UdpDataReader(byte[] source)
		{
			this.SetSource(source);
		}

		public UdpDataReader(byte[] source, int offset)
		{
			this.SetSource(source, offset);
		}

		public UdpDataReader(byte[] source, int offset, int maxSize)
		{
			this.SetSource(source, offset, maxSize);
		}

		public UdpEndPoint GetUdpEndPoint()
		{
			string host = this.GetString(1000);
			int port = this.GetInt();
			return new UdpEndPoint(host, port);
		}

		public byte GetByte()
		{
			byte res = this._data[this._position];
			this._position += 1;
			return res;
		}

		public byte PeekByte()
		{
			byte res = this._data[this._position];
			return res;
		}

		public sbyte GetSByte()
		{
			var b = (sbyte)this._data[this._position];
			this._position++;
			return b;
		}

		public bool[] GetBoolArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new bool[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetBool();
			}
			return arr;
		}

		public ushort[] GetUShortArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new ushort[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetUShort();
			}
			return arr;
		}

		public short[] GetShortArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new short[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetShort();
			}
			return arr;
		}

		public long[] GetLongArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new long[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetLong();
			}
			return arr;
		}

		public ulong[] GetULongArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new ulong[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetULong();
			}
			return arr;
		}

		public int[] GetIntArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new int[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetInt();
			}
			return arr;
		}

		public uint[] GetUIntArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new uint[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetUInt();
			}
			return arr;
		}

		public float[] GetFloatArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new float[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetFloat();
			}
			return arr;
		}

		public double[] GetDoubleArray()
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new double[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetDouble();
			}
			return arr;
		}

		public string[] GetStringArray(int maxLength)
		{
			ushort size = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			var arr = new string[size];
			for (int i = 0; i < size; i++)
			{
				arr[i] = this.GetString(maxLength);
			}
			return arr;
		}

		public bool GetBool()
		{
			bool res = this._data[this._position] > 0;
			this._position += 1;
			return res;
		}

		public ushort GetUShort()
		{
			ushort result = BitConverter.ToUInt16(this._data, this._position);
			this._position += 2;
			return result;
		}

		public short GetShort()
		{
			short result = BitConverter.ToInt16(this._data, this._position);
			this._position += 2;
			return result;
		}

		public long GetLong()
		{
			long result = BitConverter.ToInt64(this._data, this._position);
			this._position += 8;
			return result;
		}

		public ulong GetULong()
		{
			ulong result = BitConverter.ToUInt64(this._data, this._position);
			this._position += 8;
			return result;
		}

		public int GetInt()
		{
			int result = BitConverter.ToInt32(this._data, this._position);
			this._position += 4;
			return result;
		}

		public uint GetUInt()
		{
			uint result = BitConverter.ToUInt32(this._data, this._position);
			this._position += 4;
			return result;
		}

		public float GetFloat()
		{
			float result = BitConverter.ToSingle(this._data, this._position);
			this._position += 4;
			return result;
		}

		public double GetDouble()
		{
			double result = BitConverter.ToDouble(this._data, this._position);
			this._position += 8;
			return result;
		}

		public string GetString(int maxLength)
		{
			int bytesCount = this.GetInt();
			if (bytesCount <= 0 || bytesCount > maxLength * 2)
			{
				return string.Empty;
			}

			int charCount = Encoding.UTF8.GetCharCount(this._data, this._position, bytesCount);
			if (charCount > maxLength)
			{
				return string.Empty;
			}

			string result = Encoding.UTF8.GetString(this._data, this._position, bytesCount);
			this._position += bytesCount;
			return result;
		}

		public string GetString()
		{
			int bytesCount = this.GetInt();

			string result = Encoding.UTF8.GetString(this._data, this._position, bytesCount);
			this._position += bytesCount;
			return result;
		}

		public byte[] GetBytes()
		{
			byte[] outgoingData = new byte[this.AvailableBytes];
			Buffer.BlockCopy(this._data, this._position, outgoingData, 0, this.AvailableBytes);
			this._position = this._data.Length;
			return outgoingData;
		}

		public void GetBytes(byte[] destination)
		{
			Buffer.BlockCopy(this._data, this._position, destination, 0, this.AvailableBytes);
			this._position = this._data.Length;
		}

		public void GetBytes(byte[] destination, int lenght)
		{
			Buffer.BlockCopy(this._data, this._position, destination, 0, lenght);
			this._position += lenght;
		}

		public void Clear()
		{
			this._position = 0;
			this.DataSize = 0;
			this._data = null;
		}
	}
}