
using ReliableUdp.Const;
using ReliableUdp.Enums;
using ReliableUdp.Packet;

namespace ReliableUdp.PacketHandler
{
    public class MtuHandler
	{
        private int mtuIdx;
		private bool finishMtu;
		private int mtuCheckTimer;
		private int mtuCheckAttempts;
		private const int MTU_CHECK_DELAY = 1000;
		private const int MAX_MTU_CHECK_ATTEMPTS = 4;
		private readonly object lockObject = new object();

        public int Mtu { get; private set; } = Const.Mtu.PossibleValues[0];

        public MtuHandler()
		{
		}

		public void ProcessMtuPacket(UdpPeer peer, UdpPacket packet)
		{
			if (packet.Size == 1 ||
					 packet.RawData[1] >= Const.Mtu.PossibleValues.Length)
				return;

			if (packet.Type == PacketType.MtuCheck)
			{
				if (packet.Size != Const.Mtu.PossibleValues[packet.RawData[1]])
				{
					return;
				}
				this.mtuCheckAttempts = 0;

				System.Diagnostics.Debug.WriteLine($"MTU check. Resend {packet.RawData[1]}");
				var mtuOkPacket = peer.GetPacketFromPool(PacketType.MtuOk, 1);
				mtuOkPacket.RawData[1] = packet.RawData[1];
				peer.SendPacket(mtuOkPacket);
			}
			else if (packet.RawData[1] > this.mtuIdx)
			{
				lock (this.lockObject)
				{
					this.mtuIdx = packet.RawData[1];
					this.Mtu = Const.Mtu.PossibleValues[this.mtuIdx];
				}

				if (this.mtuIdx == Const.Mtu.PossibleValues.Length - 1)
				{
					this.finishMtu = true;
				}

				System.Diagnostics.Debug.WriteLine($"MTU is set to {this.Mtu}");
			}
		}

		public void Update(UdpPeer peer, int deltaTime)
		{
			if (!this.finishMtu)
			{
				this.mtuCheckTimer += deltaTime;
				if (this.mtuCheckTimer >= MTU_CHECK_DELAY)
				{
					this.mtuCheckTimer = 0;
					this.mtuCheckAttempts++;
					if (this.mtuCheckAttempts >= MAX_MTU_CHECK_ATTEMPTS)
					{
						this.finishMtu = true;
					}
					else
					{
						lock (this.lockObject)
						{
							if (this.mtuIdx < Const.Mtu.PossibleValues.Length - 1)
							{
								int newMtu = Const.Mtu.PossibleValues[this.mtuIdx + 1] - HeaderSize.DEFAULT;
								var p = peer.GetPacketFromPool(PacketType.MtuCheck, newMtu);
								p.RawData[1] = (byte)(this.mtuIdx + 1);
								peer.SendPacket(p);
							}
						}
					}
				}
			}
		}

		public void SendPacket(UdpPeer peer, UdpPacket packet)
		{
			if (!peer.SendRawAndRecycle(packet, peer.EndPoint))
			{
				this.finishMtu = true;
			}
		}
	}
}
