using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ReliableUdp.Packet;

namespace ReliableUdp
{
    public sealed class UdpSocket
	{
	    public const uint IOC_IN = 0x80000000;
	    public const uint IOC_VENDOR = 0x18000000;
	    public const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
        public const string MULTICAST_GROUP_I_PV4 = "224.0.0.1";
		public const string MULTICAST_GROUP_I_PV6 = "FF02:0:0:0:0:0:0:1";
		public const int SOCKET_BUFFER_SIZE = 1024 * 1024 * 2; //2mb
		public const int SOCKET_TTL = 255;

		private Socket udpSocketv4;
		private Socket udpSocketv6;
        private Thread threadv4;
		private Thread threadv6;
		private bool running;
		private readonly UdpManager.OnMessageReceived onMessageReceived;

		private static readonly IPAddress multicastAddressV6 = IPAddress.Parse(MULTICAST_GROUP_I_PV6);
		private static readonly bool ipv6Support;
		private const int SOCKET_RECEIVE_POLL_TIME = 100000;
		private const int SOCKET_SEND_POLL_TIME = 5000;

        public UdpEndPoint LocalEndPoint { get; private set; }

        static UdpSocket()
		{
			try
			{
				//Unity3d .NET 2.0 throws exception.
				ipv6Support = Socket.OSSupportsIPv6;
			}
			catch
			{
				ipv6Support = false;
			}
		}

		public UdpSocket(UdpManager.OnMessageReceived onMessageReceived)
		{
			this.onMessageReceived = onMessageReceived;
		}

		private void ReceiveLogic(object state)
		{
			Socket socket = (Socket)state;
			EndPoint bufferEndPoint = new IPEndPoint(socket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
			UdpEndPoint bufferNetEndPoint = new UdpEndPoint((IPEndPoint)bufferEndPoint);
			byte[] receiveBuffer = new byte[UdpPacket.SIZE_LIMIT];

			while (this.running)
			{
				if (!socket.Poll(SOCKET_RECEIVE_POLL_TIME, SelectMode.SelectRead))
				{
					continue;
				}

				int result;

				try
				{
					result = socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref bufferEndPoint);
					if (!bufferNetEndPoint.EndPoint.Equals(bufferEndPoint))
					{
						bufferNetEndPoint = new UdpEndPoint((IPEndPoint)bufferEndPoint);
					}
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.ConnectionReset ||
						 ex.SocketErrorCode == SocketError.MessageSize)
					{
						System.Diagnostics.Debug.WriteLine($"Ignored Error code {ex.SocketErrorCode} with execption {ex}.");
						continue;
					}

					System.Diagnostics.Debug.WriteLine($"Error code {ex.SocketErrorCode} with execption {ex}.");
					this.onMessageReceived(null, 0, (int)ex.SocketErrorCode, bufferNetEndPoint);
					continue;
				}

				System.Diagnostics.Debug.WriteLine($"Received data from {bufferNetEndPoint} with result {result}.");
				this.onMessageReceived(receiveBuffer, result, 0, bufferNetEndPoint);
			}
		}

		public bool Bind(int port, bool reuseAddress)
		{
			this.udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		    this.udpSocketv4.IOControl(unchecked((int)SIO_UDP_CONNRESET), new byte[] { Convert.ToByte(false) }, null);
			this.udpSocketv4.Blocking = false;
			this.udpSocketv4.ReceiveBufferSize = SOCKET_BUFFER_SIZE;
			this.udpSocketv4.SendBufferSize = SOCKET_BUFFER_SIZE;

			this.udpSocketv4.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, SOCKET_TTL);
			if (reuseAddress)
				this.udpSocketv4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            this.udpSocketv4.DontFragment = true;

			try
			{
				this.udpSocketv4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
			}
			catch (SocketException ex)
			{
                System.Diagnostics.Debug.WriteLine($"Broadcast error {ex}.");
            }

			if (!BindSocket(this.udpSocketv4, new IPEndPoint(IPAddress.Any, port)))
			{
				return false;
			}
			this.LocalEndPoint = new UdpEndPoint((IPEndPoint)this.udpSocketv4.LocalEndPoint);

			this.running = true;
			this.threadv4 = new Thread(ReceiveLogic);
			this.threadv4.Name = "SocketThreadv4(" + port + ")";
			this.threadv4.IsBackground = true;
			this.threadv4.Start(this.udpSocketv4);

			if (!ipv6Support)
				return true;

			port = this.LocalEndPoint.Port;

			this.udpSocketv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
		    this.udpSocketv6.IOControl(unchecked((int) SIO_UDP_CONNRESET), new byte[] {Convert.ToByte(false)}, null);
			this.udpSocketv6.Blocking = false;
			this.udpSocketv6.ReceiveBufferSize = SOCKET_BUFFER_SIZE;
			this.udpSocketv6.SendBufferSize = SOCKET_BUFFER_SIZE;
			if (reuseAddress)
				this.udpSocketv6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (BindSocket(this.udpSocketv6, new IPEndPoint(IPAddress.IPv6Any, port)))
			{
				this.LocalEndPoint = new UdpEndPoint((IPEndPoint)this.udpSocketv6.LocalEndPoint);

				try
				{
					this.udpSocketv6.SetSocketOption(
						 SocketOptionLevel.IPv6,
						 SocketOptionName.AddMembership,
						 new IPv6MulticastOption(multicastAddressV6));
				}
				catch
				{
					// Unity3d throws exception - ignored
				}

				this.threadv6 = new Thread(ReceiveLogic);
				this.threadv6.Name = "SocketThreadv6(" + port + ")";
				this.threadv6.IsBackground = true;
				this.threadv6.Start(this.udpSocketv6);
			}

			return true;
		}

		private bool BindSocket(Socket socket, IPEndPoint ep)
		{
			try
			{
				socket.Bind(ep);
				System.Diagnostics.Debug.WriteLine($"Successfully binded to port {((IPEndPoint)socket.LocalEndPoint).Port}.");
			}
			catch (SocketException ex)
			{
				System.Diagnostics.Debug.WriteLine($"Bind error {ex}");

				if (ex.SocketErrorCode == SocketError.AddressFamilyNotSupported)
				{
					return true;
				}
				return false;
			}
			return true;
		}

		public bool SendBroadcast(byte[] data, int offset, int size, int port)
		{
			try
			{
				int result = this.udpSocketv4.SendTo(data, offset, size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port));
				if (result <= 0)
					return false;
				if (ipv6Support)
				{
					result = this.udpSocketv6.SendTo(data, offset, size, SocketFlags.None, new IPEndPoint(multicastAddressV6, port));
					if (result <= 0)
						return false;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				return false;
			}
			return true;
		}

		public int SendTo(byte[] data, int offset, int size, UdpEndPoint remoteEndPoint, ref int errorCode)
		{
			try
			{
				int result = 0;
				if (remoteEndPoint.EndPoint.AddressFamily == AddressFamily.InterNetwork)
				{
					if (!this.udpSocketv4.Poll(SOCKET_SEND_POLL_TIME, SelectMode.SelectWrite))
						return -1;
					result = this.udpSocketv4.SendTo(data, offset, size, SocketFlags.None, remoteEndPoint.EndPoint);
				}
				else if (ipv6Support)
				{
					if (!this.udpSocketv6.Poll(SOCKET_SEND_POLL_TIME, SelectMode.SelectWrite))
						return -1;
					result = this.udpSocketv6.SendTo(data, offset, size, SocketFlags.None, remoteEndPoint.EndPoint);
				}

				System.Diagnostics.Debug.WriteLine($"Send packet to {remoteEndPoint.EndPoint} with result {result}");
				return result;
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode != SocketError.MessageSize)
				{
					System.Diagnostics.Debug.WriteLine(ex.ToString());
				}

				errorCode = (int)ex.SocketErrorCode;
				return -1;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.ToString());
				return -1;
			}
		}

		private void CloseSocket(Socket s)
		{
#if NETCORE
            s.Dispose();
#else
			s.Close();
#endif
		}

		public void Close()
		{
			this.running = false;

			if (Thread.CurrentThread != this.threadv4)
			{
				this.threadv4.Join();
			}
			this.threadv4 = null;
			if (this.udpSocketv4 != null)
			{
				CloseSocket(this.udpSocketv4);
				this.udpSocketv4 = null;
			}

			if (this.udpSocketv6 == null)
				return;

			if (Thread.CurrentThread != this.threadv6)
			{
				this.threadv6.Join();
			}
			this.threadv6 = null;
			if (this.udpSocketv6 != null)
			{
				CloseSocket(this.udpSocketv6);
				this.udpSocketv6 = null;
			}
		}
	}

}
