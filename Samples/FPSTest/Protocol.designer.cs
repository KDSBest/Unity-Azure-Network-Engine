
using System;

using FPSTest.Serialization;
namespace Protocol
{
	using System.Collections.Generic;

	using Protocol.Language;

	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	using Utility;

	public class MQuaternion : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 1;
				}
			}

			public float X { get; set; }
public float Y { get; set; }
public float Z { get; set; }
public float w { get; set; }


			public MQuaternion()
			{
				X = new float();
Y = new float();
Z = new float();
w = new float();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<float>>().Serialize(X, writer);
Factory.Get<IProtocolSerializable<float>>().Serialize(Y, writer);
Factory.Get<IProtocolSerializable<float>>().Serialize(Z, writer);
Factory.Get<IProtocolSerializable<float>>().Serialize(w, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				float localX = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localX, reader);
X = localX;
float localY = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localY, reader);
Y = localY;
float localZ = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localZ, reader);
Z = localZ;
float localw = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localw, reader);
w = localw;


				return true;
			}
}public class MVector3 : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 2;
				}
			}

			public float X { get; set; }
public float Y { get; set; }
public float Z { get; set; }


			public MVector3()
			{
				X = new float();
Y = new float();
Z = new float();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<float>>().Serialize(X, writer);
Factory.Get<IProtocolSerializable<float>>().Serialize(Y, writer);
Factory.Get<IProtocolSerializable<float>>().Serialize(Z, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				float localX = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localX, reader);
X = localX;
float localY = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localY, reader);
Y = localY;
float localZ = new float();
Factory.Get<IProtocolSerializable<float>>().Deserialize(ref localZ, reader);
Z = localZ;


				return true;
			}
}public class MGuid : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 3;
				}
			}

			public List<byte> Id { get; set; }


			public MGuid()
			{
				Id = new List<byte>();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<List<byte>>>().Serialize(Id, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				List<byte> localId = new List<byte>();
Factory.Get<IProtocolSerializable<List<byte>>>().Deserialize(ref localId, reader);
Id = localId;


				return true;
			}
}public class Shot : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 4;
				}
			}

			public MVector3 Position { get; set; }
public MVector3 Direction { get; set; }


			public Shot()
			{
				Position = new MVector3();
Direction = new MVector3();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Position.Serialize(writer);
Direction.Serialize(writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				MVector3 localPosition = new MVector3();
localPosition.Deserialize(reader);
Position = localPosition;
MVector3 localDirection = new MVector3();
localDirection.Deserialize(reader);
Direction = localDirection;


				return true;
			}
}public class ClientUpdate : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 5;
				}
			}

			public MGuid Id { get; set; }
public MVector3 Position { get; set; }
public MQuaternion Rotation { get; set; }
public List<Shot> Shots { get; set; }


			public ClientUpdate()
			{
				Id = new MGuid();
Position = new MVector3();
Rotation = new MQuaternion();
Shots = new List<Shot>();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Id.Serialize(writer);
Position.Serialize(writer);
Rotation.Serialize(writer);
Shots.Serialize(writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				MGuid localId = new MGuid();
localId.Deserialize(reader);
Id = localId;
MVector3 localPosition = new MVector3();
localPosition.Deserialize(reader);
Position = localPosition;
MQuaternion localRotation = new MQuaternion();
localRotation.Deserialize(reader);
Rotation = localRotation;
List<Shot> localShots = new List<Shot>();
localShots.Deserialize(reader);
Shots = localShots;


				return true;
			}
}public class ServerUpdate : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 6;
				}
			}

			public List<ClientUpdate> Clients { get; set; }


			public ServerUpdate()
			{
				Clients = new List<ClientUpdate>();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Clients.Serialize(writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				List<ClientUpdate> localClients = new List<ClientUpdate>();
localClients.Deserialize(reader);
Clients = localClients;


				return true;
			}
}
}