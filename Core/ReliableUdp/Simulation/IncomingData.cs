using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReliableUdp.Simulation
{
	public struct IncomingData
	{
		public byte[] Data;
		public UdpEndPoint EndPoint;
		public DateTime Time;
	}

}
