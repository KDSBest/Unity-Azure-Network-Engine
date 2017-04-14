using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTest
{
	using System.Threading;

	using ReliableUdp;
	using ReliableUdp.Enums;
	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	[TestClass]
	public class ProtocolMessagesTest
	{
		private class ClientListener : IUdpEventListener
		{
			public UdpManager UdpManager { get; set; }

			private ChannelType channel;

			public ClientListener(ChannelType channel)
			{
				this.channel = channel;
			}

			public void OnPeerConnected(UdpPeer peer)
			{
				Console.WriteLine("[Client] connected to: {0}:{1}", peer.EndPoint.Host, peer.EndPoint.Port);

				var packet = new TestPackage();
				for (int i = 0; i < 5; i++)
				{
					packet.StrTest = "Test String " + i;
					packet.ByteTest = (byte)(i + 100);
					packet.DoubleTest = (double)i / 10.0;
					packet.IntTest = i + 1000;

					peer.Send(packet, channel);
				}
			}

			public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
			{
				Console.WriteLine("[Client] disconnected: " + disconnectInfo.Reason);
			}

			public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
			{
				Console.WriteLine("[Client] error! " + socketErrorCode);
			}

			public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
			{
				Assert.AreEqual(this.channel, channel);
				var packet = new TestPackage();
				if (packet.Deserialize(reader))
					Console.WriteLine($"[Client] Packet Received: {packet.StrTest} - {packet.ByteTest} - {packet.IntTest} - {packet.DoubleTest}");
			}

			public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
			{
				var packet = new TestPackage();
				if (packet.Deserialize(reader))
				{
					Console.WriteLine($"[Client] Packet Ack Received: {packet.StrTest} - {packet.ByteTest} - {packet.IntTest} - {packet.DoubleTest}");
				}
			}

			public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader, UnconnectedMessageType messageType)
			{

			}

			public void OnNetworkLatencyUpdate(UdpPeer peer, int latency)
			{

			}
		}

		private class ServerListener : IUdpEventListener
		{
			public UdpManager UdpManager { get; set; }

			public UdpManager Server;

			public void OnPeerConnected(UdpPeer peer)
			{
				Console.WriteLine("[Server] Peer connected: " + peer.EndPoint);
				var peers = Server.GetPeers();
				foreach (var netPeer in peers)
				{
					Console.WriteLine("ConnectedPeersList: id={0}, ep={1}", netPeer.ConnectId, netPeer.EndPoint);
				}
			}

			public void OnPeerDisconnected(UdpPeer peer, DisconnectInfo disconnectInfo)
			{
				Console.WriteLine("[Server] Peer disconnected: " + peer.EndPoint + ", reason: " + disconnectInfo.Reason);
			}

			public void OnNetworkError(UdpEndPoint endPoint, int socketErrorCode)
			{
				Console.WriteLine("[Server] error: " + socketErrorCode);
			}

			public void OnNetworkReceive(UdpPeer peer, UdpDataReader reader, ChannelType channel)
			{
				peer.Send(reader.Data, channel);

				var packet = new TestPackage();
				if (packet.Deserialize(reader))
					Console.WriteLine($"[Server] Packet Received: {packet.StrTest} - {packet.ByteTest} - {packet.IntTest} - {packet.DoubleTest}");
			}

			public void OnNetworkReceiveAck(UdpPeer peer, UdpDataReader reader, ChannelType channel)
			{
				var packet = new TestPackage();
				if (packet.Deserialize(reader))
					Console.WriteLine($"[Server] Packet Ack Received: {packet.StrTest} - {packet.ByteTest} - {packet.IntTest} - {packet.DoubleTest}");
			}

			public void OnNetworkReceiveUnconnected(UdpEndPoint remoteEndPoint, UdpDataReader reader, UnconnectedMessageType messageType)
			{
				Console.WriteLine("[Server] ReceiveUnconnected: {0}", reader.GetString(100));
			}

			public void OnNetworkLatencyUpdate(UdpPeer peer, int latency)
			{

			}
		}

		private ClientListener clientListener;
		private ServerListener serverListener;

		public class TestPackage : IProtocolPacket
		{
			public byte PacketType
			{
				get
				{
					return 1;
				}
			}

			public string StrTest { get; set; }

			public int IntTest { get; set; }

			public byte ByteTest { get; set; }

			public double DoubleTest { get; set; }

			public void Serialize(UdpDataWriter writer)
			{
				writer.Put(PacketType);
				writer.Put(StrTest);
				writer.Put(IntTest);
				writer.Put(ByteTest);
				writer.Put(DoubleTest);
			}

			public bool Deserialize(UdpDataReader reader)
			{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				StrTest = reader.GetString();
				IntTest = reader.GetInt();
				ByteTest = reader.GetByte();
				DoubleTest = reader.GetDouble();

				return true;
			}
		}

		[TestMethod]
		public void TestProtocolPackageUnreliable()
		{
			TestProtocolPackage(ChannelType.Unreliable);
		}

		[TestMethod]
		public void TestProtocolPackageUnreliableOrdered()
		{
			TestProtocolPackage(ChannelType.UnreliableOrdered);
		}

		[TestMethod]
		public void TestProtocolPackageReliable()
		{
			TestProtocolPackage(ChannelType.Reliable);
		}

		[TestMethod]
		public void TestProtocolPackageReliableOrdered()
		{
			TestProtocolPackage(ChannelType.ReliableOrdered);
		}

		public void TestProtocolPackage(ChannelType channel)
		{
			FactoryRegistrations.Register();

			//Server
			this.serverListener = new ServerListener();

			UdpManager server = new UdpManager(this.serverListener, 2, "myapp1");
			//server.ReuseAddress = true;
			if (!server.Start(9051))
			{
				Console.WriteLine("Server start failed");
				Assert.Fail("Server start failed");
			}
			this.serverListener.Server = server;

			//Client
			this.clientListener = new ClientListener(channel);

			UdpManager client1 = new UdpManager(this.clientListener, "myapp1");
			//client1.SimulateLatency = true;
			client1.SimulationMaxLatency = 1500;
			client1.MergeEnabled = true;
			if (!client1.Start())
			{
				Console.WriteLine("Client1 start failed");
				return;
			}
			client1.Connect("127.0.0.1", 9051);

			UdpManager client2 = new UdpManager(this.clientListener, "myapp1");
			//client2.SimulateLatency = true;
			client2.SimulationMaxLatency = 1500;
			client2.Start();
			client2.Connect("::1", 9051);

			for (int i = 0; i < 100; i++)
			{
				client1.PollEvents();
				client2.PollEvents();
				server.PollEvents();
				Thread.Sleep(15);
			}

			client1.Stop();
			client2.Stop();
			server.Stop();

			Console.WriteLine("ServStats:\n BytesReceived: {0}\n PacketsReceived: {1}\n BytesSent: {2}\n PacketsSent: {3}",
				 server.BytesReceived,
				 server.PacketsReceived,
				 server.BytesSent,
				 server.PacketsSent);
			Console.WriteLine("Client1Stats:\n BytesReceived: {0}\n PacketsReceived: {1}\n BytesSent: {2}\n PacketsSent: {3}",
				 client1.BytesReceived,
				 client1.PacketsReceived,
				 client1.BytesSent,
				 client1.PacketsSent);
			Console.WriteLine("Client2Stats:\n BytesReceived: {0}\n PacketsReceived: {1}\n BytesSent: {2}\n PacketsSent: {3}",
				 client2.BytesReceived,
				 client2.PacketsReceived,
				 client2.BytesSent,
				 client2.PacketsSent);
		}
	}
}
