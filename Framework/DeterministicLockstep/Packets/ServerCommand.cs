using System.Collections.Generic;

using ReliableUdp.Packet;
using ReliableUdp.Utility;

namespace DeterministicLockstep.Packets
{
	/// <summary>
	/// Server Command
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <seealso cref="ReliableUdp.Packet.IProtocolPacket" />
	public class ServerCommand<T> : IProtocolPacket where T : IProtocolPacket, new()
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
				return 254;
			}
		}

		/// <summary>
		/// Gets or sets the CMDS.
		/// </summary>
		/// <value>
		/// The CMDS.
		/// </value>
		public List<T> Cmds { get; set; }

		/// <summary>
		/// Gets or sets the frame.
		/// </summary>
		/// <value>
		/// The frame.
		/// </value>
		public int Frame { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerCommand{T}"/> class.
		/// </summary>
		public ServerCommand()
		{
			this.Cmds = new List<T>();

		}

		/// <summary>
		/// Serializes the specified writer.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Serialize(UdpDataWriter writer)
		{
			writer.Put(this.PacketType);
			writer.Put(this.Frame);
			writer.Put(this.Cmds.Count);

			foreach (var cmd in this.Cmds)
			{
				cmd.Serialize(writer);
			}
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
			this.Frame = reader.GetInt();
			List<T> localCmds = new List<T>();
			int c = reader.GetInt();

			for (int i = 0; i < c; i++)
			{
				T localCmd = new T();
				if (!localCmd.Deserialize(reader))
					break;

				localCmds.Add(localCmd);
			}

			this.Cmds = localCmds;

			return true;
		}
	}
}