using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using ReliableUdp.Enums;

namespace GameActor.Interfaces
{
	[DataContract]
	public class RawPacket
	{
		[DataMember]
		public ChannelType Channel { get; set; }

		[DataMember]
		public byte[] RawData { get; set; }
	}
}
