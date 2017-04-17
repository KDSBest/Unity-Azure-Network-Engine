using ReliableUdp.Packet;
using ReliableUdp.Utility;

namespace DeterministicLockstep.Packets
{

	/// <summary>
	/// Client Command
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <seealso cref="ReliableUdp.Packet.IProtocolPacket" />
	public class ClientCommand<T> : IProtocolPacket where T : IProtocolPacket, new()
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
				return 253;
			}
		}

		/// <summary>
		/// Gets or sets the command.
		/// </summary>
		/// <value>
		/// The command.
		/// </value>
		public T Cmd { get; set; }

		/// <summary>
		/// Gets or sets the frame.
		/// </summary>
		/// <value>
		/// The frame.
		/// </value>
		public int Frame { get; set; }


		/// <summary>
		/// Initializes a new instance of the <see cref="ClientCommand{T}"/> class.
		/// </summary>
		public ClientCommand()
		{
			Cmd = new T();

		}

		/// <summary>
		/// Serializes the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Serialize(UdpDataWriter writer)
		{
			writer.Put(PacketType);
			writer.Put(Frame);
			Cmd.Serialize(writer);

		}

		/// <summary>
		/// Deserializes the specified reader.
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns></returns>
		public bool Deserialize(UdpDataReader reader)
		{
			if (reader.PeekByte() != PacketType)
				return false;

			reader.GetByte();
			Frame = reader.GetInt();
			T localCmd = new T();
			localCmd.Deserialize(reader);
			Cmd = localCmd;


			return true;
		}
	}
}