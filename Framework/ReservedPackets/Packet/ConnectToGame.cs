using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Protocol;

using ReliableUdp.Packet;
using ReliableUdp.Utility;

using Utility;

namespace Packets
{
	/// <summary>
	/// Connect To Game
	/// </summary>
	/// <seealso cref="ReliableUdp.Packet.IProtocolPacket" />
	public class ConnectToGame : IProtocolPacket
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
				return 200;
			}
		}

		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		/// <value>
		/// The identifier.
		/// </value>
		public Guid Id { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectToGame"/> class.
		/// </summary>
		public ConnectToGame()
		{
			this.Id = Guid.NewGuid();
		}

		/// <summary>
		/// Serializes the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Serialize(UdpDataWriter writer)
		{
			writer.Put(this.PacketType);
			Factory.Get<IProtocolSerializable<List<byte>>>().Serialize(this.Id.ToByteArray().ToList(), writer);
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

			List<byte> bytes = new List<byte>();
			Factory.Get<IProtocolSerializable<List<byte>>>().Deserialize(ref bytes, reader);
			this.Id = new Guid(bytes.ToArray());

			return true;
		}
	}
}