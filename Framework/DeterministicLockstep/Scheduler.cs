using System;
using System.Collections.Generic;
using System.Linq;

using DeterministicLockstep.Packets;

using EventSystem;
using EventSystem.Events;

using ReliableUdp;
using ReliableUdp.Enums;
using ReliableUdp.Packet;

namespace DeterministicLockstep
{
	public class Scheduler<T> where T : IProtocolPacket, new()
	{
		private object lockObject = new object();

		private Dictionary<int, List<ClientCommand<T>>> steps = new Dictionary<int, List<ClientCommand<T>>>();

		public int FrameBufferSize { get; private set; }
		public int PlayerCount { get; private set; }
		public bool IsClient { get; private set; }
		public T CurrentCommand { get; set; }

		public int ClientId { get; set; }
		private Dictionary<long, int> connectionIdToClientId = new Dictionary<long, int>();

		private ISchedulerSender sender;

		private int CurrentFrame = 0;

		private int LastSendFrame = 0;

		public LockstepGameSimulation<T> Simulation { get; private set; }

		public Scheduler(ISchedulerSender sender, LockstepGameSimulation<T> simulation, bool isClient = true, int playerCount = 2, int frameBufferSize = 2)
		{
			this.Simulation = simulation;
			this.sender = sender;
			IsClient = isClient;
			FrameBufferSize = frameBufferSize;
			PlayerCount = playerCount;
			CurrentCommand = new T();
			ClientId = 0;

			if (isClient)
			{
				PubSub<NetworkReceiveEvent<ServerCommand<T>>>.Subscribe("Scheduler", ReceiveClient);
				PubSub<NetworkReceiveEvent<ServerTellId>>.Subscribe("Scheduler", ReceiveClientId);
			}
			else
			{
				PubSub<NetworkConnectedEvent>.Subscribe("Scheduler", ServerConnected);
				PubSub<NetworkReceiveEvent<ClientCommand<T>>>.Subscribe("Scheduler", ReceiveServer);
			}

			this.FillEmptyFrames();
		}

		private void FillEmptyFrames()
		{
			for (int i = 0; i < FrameBufferSize; i++)
			{
				EnsureFrame(i);
				for (int ii = 0; ii < PlayerCount; ii++)
				{
					this.steps[i][ii] = new ClientCommand<T>()
					{
						Frame = i
					};
				}
			}
			this.CurrentFrame = FrameBufferSize;
			this.LastSendFrame = this.CurrentFrame - 1;
		}

		private void ServerConnected(NetworkConnectedEvent networkConnectedEvent)
		{
			lock (this.lockObject)
			{
				ClientId++;
				this.connectionIdToClientId.Add(networkConnectedEvent.Peer.ConnectId, ClientId);
				networkConnectedEvent.Peer.Send(new ServerTellId() { Id = ClientId }, ChannelType.ReliableOrdered);
			}
		}

		private void ReceiveClientId(NetworkReceiveEvent<ServerTellId> networkReceiveEvent)
		{
			ClientId = networkReceiveEvent.Packet.Id;
		}

		private void ReceiveServer(NetworkReceiveEvent<ClientCommand<T>> networkReceiveEvent)
		{
			int id = this.connectionIdToClientId[networkReceiveEvent.Peer.ConnectId];
			this.EnsureFrame(networkReceiveEvent.Packet.Frame);
			this.steps[networkReceiveEvent.Packet.Frame][id - 1] = networkReceiveEvent.Packet;
		}

		private void ReceiveClient(NetworkReceiveEvent<ServerCommand<T>> networkReceiveEvent)
		{
			this.EnsureFrame(networkReceiveEvent.Packet.Frame);

			this.steps[networkReceiveEvent.Packet.Frame] =
				networkReceiveEvent.Packet.Cmds.Select(x => new ClientCommand<T>() { Frame = networkReceiveEvent.Packet.Frame, Cmd = x }).ToList();

			if (this.steps[this.CurrentFrame].All(x => x != null))
			{
				this.CurrentFrame++;
			}
		}

		private void EnsureFrame(int frame)
		{
			if (!this.steps.ContainsKey(frame))
			{
				this.steps.Add(frame, new List<ClientCommand<T>>(PlayerCount));
				for (int i = 0; i < PlayerCount; i++)
				{
					this.steps[frame].Add(null);
				}
			}
		}

		public List<ClientCommand<T>> GetFrame(int frame)
		{
			this.EnsureFrame(frame);

			if (this.steps[frame].Any(x => x == null))
				return new List<ClientCommand<T>>();

			return this.steps[frame];
		}

		public void FixedUpdate()
		{
			int frame = this.Simulation.CurrentSimulationFrame;
			if (this.steps[frame].All(x => x != null))
			{
				this.Simulation.FixedUpdate(frame, this.steps[frame].Select(x => x.Cmd).ToList());
			}
		}

		public void Update()
		{
			this.EnsureFrame(this.CurrentFrame);

			if (this.CurrentFrame > this.LastSendFrame)
			{
				if (IsClient)
				{
					this.sender.SendAsClient(new ClientCommand<T>()
					{
						Frame = this.CurrentFrame,
						Cmd = CurrentCommand
					});
					CurrentCommand = new T();
				}
				else if (this.steps[this.CurrentFrame].All(x => x != null))
				{
					sender.SendAsServer(new ServerCommand<T>()
					{
						Frame = this.CurrentFrame,
						Cmds = this.steps[this.CurrentFrame].Select(x => x.Cmd).ToList()
					});

					this.CurrentFrame++;
				}

				this.LastSendFrame++;
			}

			this.sender.PollEvents();
		}
	}
}
