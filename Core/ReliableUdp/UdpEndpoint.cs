﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReliableUdp
{
	using System.Net;
	using System.Net.Sockets;

	public sealed class UdpEndPoint
	{
		public string Host { get { return EndPoint.Address.ToString(); } }
		public int Port { get { return EndPoint.Port; } }

		public readonly IPEndPoint EndPoint;

		public UdpEndPoint(IPEndPoint ipEndPoint)
		{
			EndPoint = ipEndPoint;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is UdpEndPoint))
			{
				return false;
			}
			return EndPoint.Equals(((UdpEndPoint)obj).EndPoint);
		}

		public override string ToString()
		{
			return EndPoint.ToString();
		}

		public override int GetHashCode()
		{
			return EndPoint.GetHashCode();
		}

		public UdpEndPoint(string hostStr, int port)
		{
			IPAddress ipAddress;
			if (!IPAddress.TryParse(hostStr, out ipAddress))
			{
				if (Socket.OSSupportsIPv6)
				{
					if (hostStr == "localhost")
					{
						ipAddress = IPAddress.IPv6Loopback;
					}
					else
					{
						ipAddress = ResolveAddress(hostStr, AddressFamily.InterNetworkV6);
					}
				}
				if (ipAddress == null)
				{
					ipAddress = ResolveAddress(hostStr, AddressFamily.InterNetwork);
				}
			}
			if (ipAddress == null)
			{
				throw new Exception("Invalid address: " + hostStr);
			}
			EndPoint = new IPEndPoint(ipAddress, port);
		}

		private IPAddress ResolveAddress(string hostStr, AddressFamily addressFamily)
		{
			var host = Dns.GetHostEntry(hostStr);

			foreach (IPAddress ip in host.AddressList)
			{
				if (ip.AddressFamily == addressFamily)
				{
					return ip;
				}
			}
			return null;
		}

		public long GetId()
		{
			byte[] addr = EndPoint.Address.GetAddressBytes();
			long id = 0;

			if (addr.Length == 4) //IPv4
			{
				id = addr[0];
				id |= (long)addr[1] << 8;
				id |= (long)addr[2] << 16;
				id |= (long)addr[3] << 24;
				id |= (long)EndPoint.Port << 32;
			}
			else if (addr.Length == 16) //IPv6
			{
				id = addr[0] ^ addr[8];
				id |= (long)(addr[1] ^ addr[9]) << 8;
				id |= (long)(addr[2] ^ addr[10]) << 16;


				id |= (long)(addr[3] ^ addr[11]) << 24;
				id |= (long)(addr[4] ^ addr[12]) << 32;
				id |= (long)(addr[5] ^ addr[13]) << 40;
				id |= (long)(addr[6] ^ addr[14]) << 48;
				id |= (long)(Port ^ addr[7] ^ addr[15]) << 56;
			}

			return id;
		}
	}

}
