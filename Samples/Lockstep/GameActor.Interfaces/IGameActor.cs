using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace GameActor.Interfaces
{
	/// <summary>
	/// This interface defines the methods exposed by an actor.
	/// Clients use this interface to interact with the actor that implements it.
	/// </summary>
	public interface IGameActor : IActor
	{

		Task<RawPacket> GetPacket(Guid pId);

		Task PlayerConnect(Guid pId);

		Task PlayerDisconnect(Guid pId);

		Task ReceivePacket(Guid pId, byte[] data);
	}
}
