using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableUdp.Packet;

namespace DeterministicLockstep
{
	public abstract class LockstepGameSimulation<T> where T : IProtocolPacket, new()
	{
		public int CurrentSimulationFrame { get; private set; }

		public void FixedUpdate(int frame, List<T> cmds)
		{
			if (this.CurrentSimulationFrame == frame)
			{
				SimulateStep(this.CurrentSimulationFrame, cmds);
				this.CurrentSimulationFrame++;
			}
		}

		public abstract void SimulateStep(int frame, List<T> cmds);
	}
}
