using System;

using Protocol;

using ReliableUdp.Packet;
using ReliableUdp.Utility;

using Utility;

namespace DeterministicLockstep.Packets
{
	/// <summary>
	/// Server Tell Id
	/// </summary>
	/// <seealso cref="ReliableUdp.Packet.IProtocolPacket" />
	public class ServerTellId : IProtocolPacket
	{
		/// <summary>
		/// Gets the type of the packet.
		/// </summary>
		/// <value>
		/// The type of the packet.
		/// </value>
		public byte PacketType
		{
			get
			{
				return 255;
			}
		}

		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		/// <value>
		/// The identifier.
		/// </value>
		public int Id { get; set; }


		/// <summary>
		/// Initializes a new instance of the <see cref="ServerTellId"/> class.
		/// </summary>
		public ServerTellId()
		{
			this.Id = 0;
		}

		/// <summary>
		/// Serializes the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Serialize(UdpDataWriter writer)
		{
			writer.Put(this.PacketType);
			Factory.Get<IProtocolSerializable<int>>().Serialize(this.Id, writer);
		}

		/// <summary>
		/// Deserializes the specified reader.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns></returns>
		public bool Deserialize(UdpDataReader reader)
		{
			if (reader.PeekByte() != this.PacketType)
				return false;

			reader.GetByte();

			int localId = new int();
			Factory.Get<IProtocolSerializable<int>>().Deserialize(ref localId, reader);
			this.Id = localId;


			return true;
		}
	}
}