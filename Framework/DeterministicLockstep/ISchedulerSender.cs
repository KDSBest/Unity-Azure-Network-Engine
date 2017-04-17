using ReliableUdp.Packet;

namespace DeterministicLockstep
{
	public interface ISchedulerSender
	{
		void SendAsClient(IProtocolPacket packet);

		void SendAsServer(IProtocolPacket packet);

		void PollEvents();
	}
}