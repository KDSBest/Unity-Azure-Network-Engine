using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocol
{
	using Utility;
	using ReliableUdp.Utility;

	public partial class DefaultSerializer
	
			: IProtocolSerializable<int>
		, IProtocolSerializable<List<int>>
			, IProtocolSerializable<uint>
		, IProtocolSerializable<List<uint>>
			, IProtocolSerializable<long>
		, IProtocolSerializable<List<long>>
			, IProtocolSerializable<ulong>
		, IProtocolSerializable<List<ulong>>
			, IProtocolSerializable<short>
		, IProtocolSerializable<List<short>>
			, IProtocolSerializable<ushort>
		, IProtocolSerializable<List<ushort>>
			, IProtocolSerializable<byte>
		, IProtocolSerializable<List<byte>>
			, IProtocolSerializable<string>
		, IProtocolSerializable<List<string>>
			, IProtocolSerializable<bool>
		, IProtocolSerializable<List<bool>>
			, IProtocolSerializable<double>
		, IProtocolSerializable<List<double>>
			, IProtocolSerializable<float>
		, IProtocolSerializable<List<float>>
		{
			public void Serialize(int value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref int value, UdpDataReader reader)
		{
			value = reader.GetInt();
		}

		public void Serialize(List<int> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<int> value, UdpDataReader reader)
		{
			value = new List<int>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetInt();
				value.Add(val);
			}
		}
			public void Serialize(uint value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref uint value, UdpDataReader reader)
		{
			value = reader.GetUInt();
		}

		public void Serialize(List<uint> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<uint> value, UdpDataReader reader)
		{
			value = new List<uint>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetUInt();
				value.Add(val);
			}
		}
			public void Serialize(long value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref long value, UdpDataReader reader)
		{
			value = reader.GetLong();
		}

		public void Serialize(List<long> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<long> value, UdpDataReader reader)
		{
			value = new List<long>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetLong();
				value.Add(val);
			}
		}
			public void Serialize(ulong value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref ulong value, UdpDataReader reader)
		{
			value = reader.GetULong();
		}

		public void Serialize(List<ulong> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<ulong> value, UdpDataReader reader)
		{
			value = new List<ulong>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetULong();
				value.Add(val);
			}
		}
			public void Serialize(short value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref short value, UdpDataReader reader)
		{
			value = reader.GetShort();
		}

		public void Serialize(List<short> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<short> value, UdpDataReader reader)
		{
			value = new List<short>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetShort();
				value.Add(val);
			}
		}
			public void Serialize(ushort value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref ushort value, UdpDataReader reader)
		{
			value = reader.GetUShort();
		}

		public void Serialize(List<ushort> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<ushort> value, UdpDataReader reader)
		{
			value = new List<ushort>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetUShort();
				value.Add(val);
			}
		}
			public void Serialize(byte value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref byte value, UdpDataReader reader)
		{
			value = reader.GetByte();
		}

		public void Serialize(List<byte> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<byte> value, UdpDataReader reader)
		{
			value = new List<byte>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetByte();
				value.Add(val);
			}
		}
			public void Serialize(string value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref string value, UdpDataReader reader)
		{
			value = reader.GetString();
		}

		public void Serialize(List<string> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<string> value, UdpDataReader reader)
		{
			value = new List<string>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetString();
				value.Add(val);
			}
		}
			public void Serialize(bool value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref bool value, UdpDataReader reader)
		{
			value = reader.GetBool();
		}

		public void Serialize(List<bool> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<bool> value, UdpDataReader reader)
		{
			value = new List<bool>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetBool();
				value.Add(val);
			}
		}
			public void Serialize(double value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref double value, UdpDataReader reader)
		{
			value = reader.GetDouble();
		}

		public void Serialize(List<double> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<double> value, UdpDataReader reader)
		{
			value = new List<double>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetDouble();
				value.Add(val);
			}
		}
			public void Serialize(float value, UdpDataWriter writer)
		{
			writer.Put(value);
		}

		public void Deserialize(ref float value, UdpDataReader reader)
		{
			value = reader.GetFloat();
		}

		public void Serialize(List<float> value, UdpDataWriter writer)
		{
			writer.Put(value.Count);
			foreach(var val in value)
			{
				writer.Put(val);
			}
		}

		public void Deserialize(ref List<float> value, UdpDataReader reader)
		{
			value = new List<float>();
			int count = reader.GetInt();

			for(int i = 0; i < count; i++)
			{
				var val = reader.GetFloat();
				value.Add(val);
			}
		}
		}
}

