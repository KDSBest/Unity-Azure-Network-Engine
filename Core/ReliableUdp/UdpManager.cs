using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.PacketHandler;

using Factory = Utility.Factory;

namespace ReliableUdp
{
	using System.Threading;

	using ReliableUdp.BitUtility;
	using ReliableUdp.Const;
	using ReliableUdp.Enums;
	using ReliableUdp.Logging;
	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	public sealed class UdpManager
	{
		public const int DEFAULT_UPDATE_TIME = 15;

		public delegate void OnMessageReceived(byte[] data, int length, int errorCode, UdpEndPoint remoteEndPoint);

		private readonly UdpSocket socket;

		private readonly UdpThread logicThread;

		private readonly Queue<UdpEvent> netEventsQueue;
		private readonly Stack<UdpEvent> netEventsPool;
		private readonly IUdpEventListener netEventListener;

		private readonly UdpPeerCollection peers;
		private readonly int maxConnections;

		private readonly UdpPacketPool netPacketPool;

		public int UpdateTime { get { return this.logicThread.SleepTime; } set { this.logicThread.SleepTime = value; } }

		public UdpSettings Settings = new UdpSettings();

		//stats
		public ulong PacketsSent { get; private set; }
		public ulong PacketsReceived { get; private set; }
		public ulong BytesSent { get; private set; }
		public ulong BytesReceived { get; private set; }

		/// <summary>
		/// Returns true if socket listening and update thread is running
		/// </summary>
		public bool IsRunning
		{
			get { return this.logicThread.IsRunning; }
		}

		/// <summary>
		/// Local EndPoint (host and port)
		/// </summary>
		public UdpEndPoint LocalEndPoint
		{
			get { return this.socket.LocalEndPoint; }
		}

		/// <summary>
		/// Connected peers count
		/// </summary>
		public int PeersCount
		{
			get { return this.peers.Count; }
		}

		public UdpPacketPool PacketPool
		{
			get { return this.netPacketPool; }
		}

		/// <summary>
		/// NetManager constructor
		/// </summary>
		/// <param name="listener">Network events listener</param>
		/// <param name="maxConnections">Maximum connections (incoming and outcoming)</param>
		/// <param name="connectKey">Application key (must be same with remote host for establish connection)</param>
		public UdpManager(IUdpEventListener listener, string connectKey, int maxConnections = int.MaxValue, int updateTime = DEFAULT_UPDATE_TIME)
		{
			this.logicThread = new UdpThread("LogicThread", updateTime, this.Update);
			this.socket = new UdpSocket(this.HandlePacket);
			this.netEventListener = listener;
			this.netEventsQueue = new Queue<UdpEvent>();
			this.netEventsPool = new Stack<UdpEvent>();
			this.netPacketPool = new UdpPacketPool();

			this.Settings.ConnectKey = connectKey;
			this.peers = new UdpPeerCollection(maxConnections);
			this.maxConnections = maxConnections;
			listener.UdpManager = this;
		}

		public void ConnectionLatencyUpdated(UdpPeer fromPeer, int latency)
		{
			var evt = CreateEvent(UdpEventType.ConnectionLatencyUpdated);
			evt.Peer = fromPeer;
			evt.AdditionalData = latency;
			EnqueueEvent(evt);
		}

		public bool SendRawAndRecycle(UdpPacket packet, UdpEndPoint remoteEndPoint)
		{
			var result = SendRaw(packet.RawData, 0, packet.Size, remoteEndPoint);
			this.netPacketPool.Recycle(packet);
			return result;
		}

		public bool SendRaw(byte[] message, int start, int length, UdpEndPoint remoteEndPoint)
		{
			if (!IsRunning)
				return false;

			int errorCode = 0;
			bool result = this.socket.SendTo(message, start, length, remoteEndPoint, ref errorCode) > 0;

			//10040 message to long... need to check
			//10065 no route to host
			if (errorCode != 0 && errorCode != 10040 && errorCode != 10065)
			{
				//Send error
				UdpPeer fromPeer;
				if (this.peers.TryGetValue(remoteEndPoint, out fromPeer))
				{
					DisconnectPeer(fromPeer, DisconnectReason.SocketSendError, errorCode, false, null, 0, 0);
				}
				var netEvent = CreateEvent(UdpEventType.Error);
				netEvent.RemoteEndPoint = remoteEndPoint;
				netEvent.AdditionalData = errorCode;
				EnqueueEvent(netEvent);
				return false;
			}
			if (errorCode == 10040)
			{
				Factory.Get<IUdpLogger>().Log($"10040, datalen {length}");
				return false;
			}

			PacketsSent++;
			BytesSent += (uint)length;

			return result;
		}

		private void DisconnectPeer(
			 UdpPeer peer,
			 DisconnectReason reason,
			 int socketErrorCode,
			 bool sendDisconnectPacket,
			 byte[] data,
			 int start,
			 int count)
		{
			if (sendDisconnectPacket)
			{
				if (count + 8 >= peer.PacketMtuHandler.Mtu)
				{
					//Drop additional data
					data = null;
					count = 0;
					Factory.Get<IUdpLogger>().Log("Disconnect data size is more than MTU");
				}

				var disconnectPacket = this.netPacketPool.Get(PacketType.Disconnect, 8 + count);
				BitHelper.GetBytes(disconnectPacket.RawData, 1, peer.ConnectId);
				if (data != null)
				{
					Buffer.BlockCopy(data, start, disconnectPacket.RawData, 9, count);
				}
				SendRawAndRecycle(disconnectPacket, peer.EndPoint);
			}
			var netEvent = CreateEvent(UdpEventType.Disconnect);
			netEvent.Peer = peer;
			netEvent.AdditionalData = socketErrorCode;
			netEvent.DisconnectReason = reason;
			EnqueueEvent(netEvent);
			RemovePeer(peer.EndPoint);
		}

		private void ClearPeers()
		{
			lock (this.peers)
			{
				this.peers.Clear();
			}
		}

		private void RemovePeer(UdpEndPoint endPoint)
		{
			this.peers.Remove(endPoint);
		}

		private void RemovePeerAt(int idx)
		{
			this.peers.RemoveAt(idx);
		}

		public UdpEvent CreateEvent(UdpEventType type)
		{
			UdpEvent evt = null;

			lock (this.netEventsPool)
			{
				if (this.netEventsPool.Count > 0)
				{
					evt = this.netEventsPool.Pop();
				}
			}
			if (evt == null)
			{
				evt = new UdpEvent();
			}
			evt.Type = type;
			return evt;
		}

		public void EnqueueEvent(UdpEvent evt)
		{
			lock (this.netEventsQueue)
			{
				this.netEventsQueue.Enqueue(evt);
			}
		}

		private void ProcessEvent(UdpEvent evt)
		{
			switch (evt.Type)
			{
				case UdpEventType.Connect:
					this.netEventListener.OnPeerConnected(evt.Peer);
					break;
				case UdpEventType.Disconnect:
					var info = new DisconnectInfo
					{
						Reason = evt.DisconnectReason,
						AdditionalData = evt.DataReader,
						SocketErrorCode = evt.AdditionalData
					};
					this.netEventListener.OnPeerDisconnected(evt.Peer, info);
					break;
				case UdpEventType.Receive:
					this.netEventListener.OnNetworkReceive(evt.Peer, evt.DataReader, evt.Channel);
					break;
				case UdpEventType.ReceiveUnconnected:
					this.netEventListener.OnNetworkReceiveUnconnected(evt.RemoteEndPoint, evt.DataReader);
					break;
				case UdpEventType.ReceiveAck:
					this.netEventListener.OnNetworkReceiveAck(evt.Peer, evt.DataReader, evt.Channel);
					break;
				case UdpEventType.Error:
					this.netEventListener.OnNetworkError(evt.RemoteEndPoint, evt.AdditionalData);
					break;
				case UdpEventType.ConnectionLatencyUpdated:
					this.netEventListener.OnNetworkLatencyUpdate(evt.Peer, evt.AdditionalData);
					break;
			}

			//Recycle
			evt.DataReader.Clear();
			evt.Peer = null;
			evt.AdditionalData = 0;
			evt.RemoteEndPoint = null;

			lock (this.netEventsPool)
			{
				this.netEventsPool.Push(evt);
			}
		}

		private void Update()
		{
			if (this.Settings.NetworkSimulation != null)
			{
				this.Settings.NetworkSimulation.Update(this.DataReceived);
			}

			//Process acks
			lock (this.peers)
			{
				int delta = this.logicThread.SleepTime;
				for (int i = 0; i < this.peers.Count; i++)
				{
					var udpPeer = this.peers[i];
					if (udpPeer.ConnectionState == ConnectionState.Connected && udpPeer.NetworkStatisticManagement.TimeSinceLastPacket > this.Settings.DisconnectTimeout)
					{
						Factory.Get<IUdpLogger>().Log($"Disconnect by timeout {udpPeer.NetworkStatisticManagement.TimeSinceLastPacket} > {this.Settings.DisconnectTimeout}");
						var netEvent = CreateEvent(UdpEventType.Disconnect);
						netEvent.Peer = udpPeer;
						netEvent.DisconnectReason = DisconnectReason.Timeout;
						EnqueueEvent(netEvent);

						RemovePeerAt(i);
						i--;
					}
					else if (udpPeer.ConnectionState == ConnectionState.Disconnected)
					{
						var netEvent = CreateEvent(UdpEventType.Disconnect);
						netEvent.Peer = udpPeer;
						netEvent.DisconnectReason = DisconnectReason.ConnectionFailed;
						EnqueueEvent(netEvent);

						RemovePeerAt(i);
						i--;
					}
					else
					{
						udpPeer.Update(delta);
					}
				}
			}
		}

		private void HandlePacket(byte[] data, int length, int errorCode, UdpEndPoint remoteEndPoint)
		{
			//Receive some info
			if (errorCode == 0)
			{
				bool receivePacket = true;

				if (this.Settings.NetworkSimulation != null)
				{
					receivePacket = this.Settings.NetworkSimulation.HandlePacket(data, length, remoteEndPoint);
				}

				if (receivePacket)
					DataReceived(data, length, remoteEndPoint);
			}
			else
			{
				ClearPeers();
				var netEvent = CreateEvent(UdpEventType.Error);
				netEvent.AdditionalData = errorCode;
				EnqueueEvent(netEvent);
			}
		}

		private void DataReceived(byte[] reusableBuffer, int count, UdpEndPoint remoteEndPoint)
		{
			PacketsReceived++;
			BytesReceived += (uint)count;

			//Try read packet
			UdpPacket packet = this.netPacketPool.GetAndRead(reusableBuffer, 0, count);
			if (packet == null)
			{
				Factory.Get<IUdpLogger>().Log($"Data Received but packet is null.");
				return;
			}

			//Check unconnected
			if (packet.Type == PacketType.UnconnectedMessage)
			{
				UdpEvent netEvent = CreateEvent(UdpEventType.ReceiveUnconnected);
				netEvent.RemoteEndPoint = remoteEndPoint;
				netEvent.DataReader.SetSource(packet.RawData, HeaderSize.DEFAULT);
				EnqueueEvent(netEvent);
				return;
			}

			UdpPeer udpPeer;

			Monitor.Enter(this.peers);
			int peersCount = this.peers.Count;

			if (this.peers.TryGetValue(remoteEndPoint, out udpPeer))
			{
				Monitor.Exit(this.peers);
				if (packet.Type == PacketType.Disconnect)
				{
					if (System.BitConverter.ToInt64(packet.RawData, 1) != udpPeer.ConnectId)
					{
						this.netPacketPool.Recycle(packet);
						return;
					}

					var netEvent = CreateEvent(UdpEventType.Disconnect);
					netEvent.Peer = udpPeer;
					netEvent.DataReader.SetSource(packet.RawData, 5, packet.Size - 5);
					netEvent.DisconnectReason = DisconnectReason.RemoteConnectionClose;
					EnqueueEvent(netEvent);

					this.peers.Remove(udpPeer.EndPoint);
				}
				else
				{
					udpPeer.ProcessPacket(packet);
				}
				return;
			}

			try
			{
				if (peersCount < this.maxConnections && packet.Type == PacketType.ConnectRequest)
				{
					int protoId = System.BitConverter.ToInt32(packet.RawData, 1);
					if (protoId != ConnectionRequestHandler.PROTOCOL_ID)
					{
						Factory.Get<IUdpLogger>().Log($"Peer connect rejected. Invalid Protocol Id.");
						return;
					}

					string peerKey = Encoding.UTF8.GetString(packet.RawData, 13, packet.Size - 13);
					if (peerKey != this.Settings.ConnectKey)
					{
						Factory.Get<IUdpLogger>().Log($"Peer connect rejected. Invalid key {peerKey}.");
						return;
					}

					//Getting new id for peer
					long connectionId = System.BitConverter.ToInt64(packet.RawData, 5);
					//response with id
					udpPeer = new UdpPeer(this, remoteEndPoint, connectionId);
					Factory.Get<IUdpLogger>().Log($"Received Peer connect request Id {udpPeer.ConnectId} EP {remoteEndPoint}.");

					//clean incoming packet
					this.netPacketPool.Recycle(packet);

					this.peers.Add(remoteEndPoint, udpPeer);

					var netEvent = CreateEvent(UdpEventType.Connect);
					netEvent.Peer = udpPeer;
					EnqueueEvent(netEvent);
				}
			}
			finally
			{
				Monitor.Exit(this.peers);
			}
		}

		public void ReceiveFromPeer(UdpPacket packet, UdpEndPoint remoteEndPoint, ChannelType channel)
		{
			UdpPeer fromPeer;
			if (this.peers.TryGetValue(remoteEndPoint, out fromPeer))
			{
				Factory.Get<IUdpLogger>().Log($"Received message.");
				var netEvent = CreateEvent(UdpEventType.Receive);
				netEvent.Peer = fromPeer;
				netEvent.RemoteEndPoint = fromPeer.EndPoint;
				netEvent.DataReader.SetSource(packet.GetPacketData());
				netEvent.Channel = channel;
				EnqueueEvent(netEvent);
			}
		}

		public void ReceiveAckFromPeer(UdpPacket packet, UdpEndPoint remoteEndPoint, ChannelType channel)
		{
			UdpPeer fromPeer;
			if (this.peers.TryGetValue(remoteEndPoint, out fromPeer))
			{
				Factory.Get<IUdpLogger>().Log($"Received ack message.");
				var netEvent = CreateEvent(UdpEventType.ReceiveAck);
				netEvent.Peer = fromPeer;
				netEvent.RemoteEndPoint = fromPeer.EndPoint;
				netEvent.DataReader.SetSource(packet.GetPacketData());
				netEvent.Channel = channel;
				EnqueueEvent(netEvent);
			}
		}

		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="writer">DataWriter with data</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		public void SendToAll(UdpDataWriter writer, Enums.ChannelType channelType)
		{
			SendToAll(writer.Data, 0, writer.Length, channelType);
		}

		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		public void SendToAll(byte[] data, Enums.ChannelType channelType)
		{
			SendToAll(data, 0, data.Length, channelType);
		}

		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="start">Start of data</param>
		/// <param name="length">Length of data</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		public void SendToAll(byte[] data, int start, int length, Enums.ChannelType channelType)
		{
			lock (this.peers)
			{
				for (int i = 0; i < this.peers.Count; i++)
				{
					this.peers[i].Send(data, start, length, channelType);
				}
			}
		}

		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="packet">The packet.</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		public void SendToAll(IProtocolPacket packet, ChannelType channelType)
		{
			lock (this.peers)
			{
				for (int i = 0; i < this.peers.Count; i++)
				{
					this.peers[i].Send(packet, channelType);
				}
			}
		}
		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="writer">DataWriter with data</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		/// <param name="excludePeer">Excluded peer</param>
		public void SendToAll(UdpDataWriter writer, Enums.ChannelType channelType, UdpPeer excludePeer)
		{
			SendToAll(writer.Data, 0, writer.Length, channelType, excludePeer);
		}

		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		/// <param name="excludePeer">Excluded peer</param>
		public void SendToAll(byte[] data, Enums.ChannelType channelType, UdpPeer excludePeer)
		{
			SendToAll(data, 0, data.Length, channelType, excludePeer);
		}

		/// <summary>
		/// Send data to all connected peers
		/// </summary>
		/// <param name="data">Data</param>
		/// <param name="start">Start of data</param>
		/// <param name="length">Length of data</param>
		/// <param name="channelType">Send options (reliable, unreliable, etc.)</param>
		/// <param name="excludePeer">Excluded peer</param>
		public void SendToAll(byte[] data, int start, int length, Enums.ChannelType channelType, UdpPeer excludePeer)
		{
			lock (this.peers)
			{
				for (int i = 0; i < this.peers.Count; i++)
				{
					var udpPeer = this.peers[i];
					if (udpPeer != excludePeer)
					{
						udpPeer.Send(data, start, length, channelType);
					}
				}
			}
		}

		/// <summary>
		/// Start logic thread and listening on available port
		/// </summary>
		public bool Start()
		{
			return Start(0);
		}

		/// <summary>
		/// Start logic thread and listening on selected port
		/// </summary>
		/// <param name="port">port to listen</param>
		public bool Start(int port)
		{
			if (IsRunning)
			{
				return false;
			}

			this.netEventsQueue.Clear();
			if (!this.socket.Bind(port, this.Settings.ReuseAddress))
				return false;

			this.logicThread.Start();
			return true;
		}

		/// <summary>
		/// Send message without connection
		/// </summary>
		/// <param name="message">Raw data</param>
		/// <param name="remoteEndPoint">Packet destination</param>
		/// <returns>Operation result</returns>
		public bool SendUnconnectedMessage(byte[] message, UdpEndPoint remoteEndPoint)
		{
			return SendUnconnectedMessage(message, 0, message.Length, remoteEndPoint);
		}

		/// <summary>
		/// Send message without connection
		/// </summary>
		/// <param name="writer">Data serializer</param>
		/// <param name="remoteEndPoint">Packet destination</param>
		/// <returns>Operation result</returns>
		public bool SendUnconnectedMessage(UdpDataWriter writer, UdpEndPoint remoteEndPoint)
		{
			return SendUnconnectedMessage(writer.Data, 0, writer.Length, remoteEndPoint);
		}

		/// <summary>
		/// Send message without connection
		/// </summary>
		/// <param name="message">Raw data</param>
		/// <param name="start">data start</param>
		/// <param name="length">data length</param>
		/// <param name="remoteEndPoint">Packet destination</param>
		/// <returns>Operation result</returns>
		public bool SendUnconnectedMessage(byte[] message, int start, int length, UdpEndPoint remoteEndPoint)
		{
			if (!IsRunning)
				return false;
			var packet = this.netPacketPool.GetWithData(PacketType.UnconnectedMessage, message, start, length);
			bool result = SendRawAndRecycle(packet, remoteEndPoint);
			return result;
		}

		/// <summary>
		/// Receive all pending events. Call this in game update code
		/// </summary>
		public void PollEvents()
		{
			while (this.netEventsQueue.Count > 0)
			{
				UdpEvent evt;
				lock (this.netEventsQueue)
				{
					evt = this.netEventsQueue.Dequeue();
				}
				ProcessEvent(evt);
			}
		}

		/// <summary>
		/// Connect to remote host
		/// </summary>
		/// <param name="address">Server IP or hostname</param>
		/// <param name="port">Server Port</param>
		public void Connect(string address, int port)
		{
			//Create target endpoint
			UdpEndPoint ep = new UdpEndPoint(address, port);
			Connect(ep);
		}

		/// <summary>
		/// Connect to remote host
		/// </summary>
		/// <param name="target">Server end point (ip and port)</param>
		public void Connect(UdpEndPoint target)
		{
			if (!IsRunning)
			{
				if (!this.Start())
					throw new Exception("Client is not running");
			}
			lock (this.peers)
			{
				if (this.peers.ContainsAddress(target) || this.peers.Count >= this.maxConnections)
				{
					//Already connected
					return;
				}

				//Create reliable connection
				//And request connection
				var newPeer = new UdpPeer(this, target, 0);
				this.peers.Add(target, newPeer);
			}
		}

		/// <summary>
		/// Force closes connection and stop all threads.
		/// </summary>
		public void Stop()
		{
			//Send disconnect packets
			lock (this.peers)
			{
				for (int i = 0; i < this.peers.Count; i++)
				{
					var disconnectPacket = this.netPacketPool.Get(PacketType.Disconnect, 8);
					BitHelper.GetBytes(disconnectPacket.RawData, 1, this.peers[i].ConnectId);
					SendRawAndRecycle(disconnectPacket, this.peers[i].EndPoint);
				}
			}

			//Clear
			ClearPeers();

			//Stop
			if (IsRunning)
			{
				this.logicThread.Stop();
				this.socket.Close();
			}
		}

		/// <summary>
		/// Get first peer. Usefull for Client mode
		/// </summary>
		/// <returns></returns>
		public UdpPeer GetFirstPeer()
		{
			lock (this.peers)
			{
				if (this.peers.Count > 0)
				{
					return this.peers[0];
				}
			}
			return null;
		}

		/// <summary>
		/// Get copy of current connected peers
		/// </summary>
		/// <returns>Array with connected peers</returns>
		public UdpPeer[] GetPeers()
		{
			UdpPeer[] peers;
			lock (this.peers)
			{
				peers = this.peers.ToArray();
			}
			return peers;
		}

		/// <summary>
		/// Get copy of current connected peers (without allocations)
		/// </summary>
		/// <param name="peers">List that will contain result</param>
		public void GetPeersNonAlloc(List<UdpPeer> peers)
		{
			peers.Clear();
			lock (this.peers)
			{
				for (int i = 0; i < this.peers.Count; i++)
				{
					peers.Add(this.peers[i]);
				}
			}
		}

		/// <summary>
		/// Disconnect peer from server
		/// </summary>
		/// <param name="peer">peer to disconnect</param>
		public void DisconnectPeer(UdpPeer peer)
		{
			DisconnectPeer(peer, null, 0, 0);
		}

		/// <summary>
		/// Disconnect peer from server and send additional data (Size must be less or equal MTU - 8)
		/// </summary>
		/// <param name="peer">peer to disconnect</param>
		/// <param name="data">additional data</param>
		public void DisconnectPeer(UdpPeer peer, byte[] data)
		{
			DisconnectPeer(peer, data, 0, data.Length);
		}

		/// <summary>
		/// Disconnect peer from server and send additional data (Size must be less or equal MTU - 8)
		/// </summary>
		/// <param name="peer">peer to disconnect</param>
		/// <param name="writer">additional data</param>
		public void DisconnectPeer(UdpPeer peer, UdpDataWriter writer)
		{
			DisconnectPeer(peer, writer.Data, 0, writer.Length);
		}

		/// <summary>
		/// Disconnect peer from server and send additional data (Size must be less or equal MTU - 8)
		/// </summary>
		/// <param name="peer">peer to disconnect</param>
		/// <param name="data">additional data</param>
		/// <param name="start">data start</param>
		/// <param name="count">data length</param>
		public void DisconnectPeer(UdpPeer peer, byte[] data, int start, int count)
		{
			if (peer != null && this.peers.ContainsAddress(peer.EndPoint))
			{
				DisconnectPeer(peer, DisconnectReason.DisconnectPeerCalled, 0, true, data, start, count);
			}
		}
	}

}
