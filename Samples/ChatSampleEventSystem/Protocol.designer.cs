
namespace Protocol
{
	using System.Collections.Generic;

	using Protocol.Language;

	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	using Utility;

	public class ClientChatMessage : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 1;
				}
			}

			public List<string> Message { get; set; }


			public ClientChatMessage()
			{
				Message = new List<string>();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<List<string>>>().Serialize(Message, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				List<string> localMessage = new List<string>();
Factory.Get<IProtocolSerializable<List<string>>>().Deserialize(ref localMessage, reader);
Message = localMessage;


				return true;
			}
}public class ServerChatMessage : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 2;
				}
			}

			public long Sender { get; set; }
public List<string> Message { get; set; }


			public ServerChatMessage()
			{
				Sender = new long();
Message = new List<string>();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<long>>().Serialize(Sender, writer);
Factory.Get<IProtocolSerializable<List<string>>>().Serialize(Message, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				long localSender = new long();
Factory.Get<IProtocolSerializable<long>>().Deserialize(ref localSender, reader);
Sender = localSender;
List<string> localMessage = new List<string>();
Factory.Get<IProtocolSerializable<List<string>>>().Deserialize(ref localMessage, reader);
Message = localMessage;


				return true;
			}
}public class ClientChangeNick : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 3;
				}
			}

			public string NewNick { get; set; }


			public ClientChangeNick()
			{
				NewNick = string.Empty;

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<string>>().Serialize(NewNick, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				string localNewNick = string.Empty;
Factory.Get<IProtocolSerializable<string>>().Deserialize(ref localNewNick, reader);
NewNick = localNewNick;


				return true;
			}
}public class ServerChangeNick : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 4;
				}
			}

			public long Sender { get; set; }
public string NewNick { get; set; }


			public ServerChangeNick()
			{
				Sender = new long();
NewNick = string.Empty;

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<long>>().Serialize(Sender, writer);
Factory.Get<IProtocolSerializable<string>>().Serialize(NewNick, writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				long localSender = new long();
Factory.Get<IProtocolSerializable<long>>().Deserialize(ref localSender, reader);
Sender = localSender;
string localNewNick = string.Empty;
Factory.Get<IProtocolSerializable<string>>().Deserialize(ref localNewNick, reader);
NewNick = localNewNick;


				return true;
			}
}public class ClientWhisper : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 5;
				}
			}

			public string Receiver { get; set; }
public ClientChatMessage Message { get; set; }


			public ClientWhisper()
			{
				Receiver = string.Empty;
Message = new ClientChatMessage();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<string>>().Serialize(Receiver, writer);
Message.Serialize(writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				string localReceiver = string.Empty;
Factory.Get<IProtocolSerializable<string>>().Deserialize(ref localReceiver, reader);
Receiver = localReceiver;
ClientChatMessage localMessage = new ClientChatMessage();
localMessage.Deserialize(reader);
Message = localMessage;


				return true;
			}
}public class ServerWhisper : IProtocolPacket
{
			public byte PacketType
			{
				get
				{
					return 6;
				}
			}

			public long Sender { get; set; }
public ClientChatMessage Message { get; set; }


			public ServerWhisper()
			{
				Sender = new long();
Message = new ClientChatMessage();

			}

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				Factory.Get<IProtocolSerializable<long>>().Serialize(Sender, writer);
Message.Serialize(writer);

			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				long localSender = new long();
Factory.Get<IProtocolSerializable<long>>().Deserialize(ref localSender, reader);
Sender = localSender;
ClientChatMessage localMessage = new ClientChatMessage();
localMessage.Deserialize(reader);
Message = localMessage;


				return true;
			}
}
}