using ReliableUdp;
using ReliableUdp.Enums;
using ReliableUdp.Packet;

namespace DeterministicLockstep
{
	public class SchedulerSender : ISchedulerSender
	{
		private readonly UdpManager udp;

		public SchedulerSender(UdpManager udp)
		{
			this.udp = udp;
		}

		#region Implementation of ISchedulerSender
		public void SendAsClient(IProtocolPacket packet)
		{
			this.udp.SendToAll(packet, ChannelType.ReliableOrdered);
		}

		public void SendAsServer(IProtocolPacket packet)
		{
			foreach (var peer in this.udp.GetPeers())
			{
				peer.Send(packet, ChannelType.ReliableOrdered);
			}
		}

		public void PollEvents()
		{
			this.udp.PollEvents();
		}
		#endregion
	}
}