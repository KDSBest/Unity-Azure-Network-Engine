using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.Enums;
using ReliableUdp.Logging;
using ReliableUdp.Packet;
using ReliableUdp.Utility;

using Utility;

namespace ReliableUdp.NetworkStatistic
{
	public class NetworkStatisticManagement
	{
		public const int FLOW_INCREASE_THRESHOLD = 4;
		public const int FLOW_UPDATE_TIME = 1000;
		private readonly List<FlowMode> flowModes;
		private int pingSendTimer;
		private SequenceNumber pingSequence = new SequenceNumber(0);
		private SequenceNumber remotePingSequence = new SequenceNumber(0);
		private int ping;
		private int rtt;
		private int avgRtt;
		private int rttCount;
		private int goodRttCount;
		public int TimeSinceLastPacket
		{
			get { return this.timeSinceLastPacket; }
		}

		public int Ping
		{
			get { return this.ping; }
		}

		public double ResendDelay
		{
			get { return this.avgRtt; }
		}

		private int currentFlowMode;
		private int sendedPacketsCount;
		private int flowTimer;

		private const int RTT_RESET_DELAY = 1000;
		private int rttResetTimer;

		private DateTime pingTimeStart;
		private int timeSinceLastPacket;


		public int CurrentFlowMode
		{
			get { return this.currentFlowMode; }
		}

		public int PingInterval { get; set; }

		public NetworkStatisticManagement(int intervalInMs = 1000)
		{
			this.flowModes = new List<FlowMode>();
			PingInterval = intervalInMs;

			// we start with an avgRtt because we don't want to have a resent delay of 0
			this.avgRtt = 27;
			this.rtt = 0;
			this.pingSendTimer = 0;
		}

		public void PacketReceived()
		{
			this.timeSinceLastPacket = 0;
		}

		public void AddFlowMode(int startRtt, int packetsPerSecond)
		{
			var fm = new FlowMode { PacketsPerSecond = packetsPerSecond, StartRtt = startRtt };

			if (this.flowModes.Count > 0 && startRtt < this.flowModes[0].StartRtt)
			{
				this.flowModes.Insert(0, fm);
			}
			else
			{
				this.flowModes.Add(fm);
			}
		}

		public int GetPacketsPerSecond(int flowMode)
		{
			if (flowMode < 0 || this.flowModes.Count == 0)
				return 0;
			return this.flowModes[flowMode].PacketsPerSecond;
		}

		public int GetMaxFlowMode()
		{
			return this.flowModes.Count - 1;
		}

		public int GetStartRtt(int flowMode)
		{
			if (flowMode < 0 || this.flowModes.Count == 0)
				return 0;
			return this.flowModes[flowMode].StartRtt;
		}

		private void UpdateRoundTripTime(int roundTripTime)
		{
			//Calc average round trip time
			this.rtt += roundTripTime;
			this.rttCount++;
			this.avgRtt = this.rtt / this.rttCount;

			//flowmode 0 = fastest
			//flowmode max = lowest

			if (this.avgRtt < GetStartRtt(this.currentFlowMode - 1))
			{
				if (this.currentFlowMode <= 0)
				{
					//Already maxed
					return;
				}

				this.goodRttCount++;
				if (this.goodRttCount > FLOW_INCREASE_THRESHOLD)
				{
					this.goodRttCount = 0;
					this.currentFlowMode--;

					Factory.Get<IUdpLogger>().Log($"Increased flow speed, RTT {this.avgRtt}, PPS {GetPacketsPerSecond(this.currentFlowMode)}");
				}
			}
			else if (this.avgRtt > GetStartRtt(this.currentFlowMode))
			{
				this.goodRttCount = 0;
				if (this.currentFlowMode < GetMaxFlowMode())
				{
					this.currentFlowMode++;
					Factory.Get<IUdpLogger>().Log($"Decreased flow speed, RTT {this.avgRtt}, PPS {GetPacketsPerSecond(this.currentFlowMode)}");
				}
			}

			//recalc resend delay
			double avgRtt = this.avgRtt;
			if (avgRtt <= 0.0)
				avgRtt = 0.1;
		}

		public void HandlePing(UdpPeer peer, UdpPacket packet)
		{
			if ((packet.Sequence - this.remotePingSequence).Value < 0)
			{
				peer.Recycle(packet);
				return;
			}

			Factory.Get<IUdpLogger>().Log("Ping receive... Send Pong...");
			this.remotePingSequence = packet.Sequence;
			peer.Recycle(packet);

			peer.CreateAndSend(PacketType.Pong, this.remotePingSequence);
		}

		public void HandlePong(UdpPeer peer, UdpPacket packet)
		{
			if ((packet.Sequence - this.pingSequence).Value < 0)
			{
				peer.Recycle(packet);
				return;
			}
			this.pingSequence = packet.Sequence;
			int rtt = (int)(DateTime.UtcNow - this.pingTimeStart).TotalMilliseconds;
			UpdateRoundTripTime(rtt);
			Factory.Get<IUdpLogger>().Log($"Ping {rtt}");
			peer.Recycle(packet);
		}

		public void Update(UdpPeer peer, int deltaTime, Action<UdpPeer, int> connectionLatencyUpdated)
		{
			ResetFlowTimer(deltaTime);
			this.timeSinceLastPacket += deltaTime;
			//Send ping
			this.pingSendTimer += deltaTime;
			if (this.pingSendTimer >= PingInterval)
			{
				Factory.Get<IUdpLogger>().Log("Send ping...");

				//reset timer
				this.pingSendTimer = 0;

				//send ping
				peer.CreateAndSend(PacketType.Ping, this.pingSequence);

				//reset timer
				this.pingTimeStart = DateTime.UtcNow;
			}

			//RTT - round trip time
			this.rttResetTimer += deltaTime;
			if (this.rttResetTimer >= RTT_RESET_DELAY)
			{
				this.rttResetTimer = 0;
				//Rtt update
				this.rtt = this.avgRtt;
				this.ping = this.avgRtt;
				connectionLatencyUpdated(peer, this.ping);
				this.rttCount = 1;
			}
		}

		public void ResetFlowTimer(int deltaTime)
		{
			this.flowTimer += deltaTime;
			if (this.flowTimer >= FLOW_UPDATE_TIME)
			{
				Factory.Get<IUdpLogger>().Log($"Reset flow timer, sended packets {this.sendedPacketsCount}");
				this.sendedPacketsCount = 0;
				this.flowTimer = 0;
			}
		}

		public int GetCurrentMaxSend(int deltaTime)
		{
			int maxSendPacketsCount = GetPacketsPerSecond(this.currentFlowMode);

			if (maxSendPacketsCount > 0)
			{
				int availableSendPacketsCount = maxSendPacketsCount - this.sendedPacketsCount;
				return Math.Min(availableSendPacketsCount, (maxSendPacketsCount * deltaTime) / FLOW_UPDATE_TIME);
			}
			else
			{
				return int.MaxValue;
			}
		}

		public void IncreaseSendedPacketCount(int currentSended)
		{
			this.sendedPacketsCount += currentSended;
		}
	}
}
