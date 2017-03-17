using Utility;

namespace ReliableUdp
{
	using ReliableUdp.Channel;
	using ReliableUdp.Logging;

	public static class FactoryRegistrations
	{
		public const int DEFAULT_WINDOW_SIZE = 64;

		public static void Register()
		{
			Factory.Register<IUdpLogger>(() => new DebugLogger(), FactoryLifespan.Singleton);
			Factory.Register<IUnreliableChannel>(() => new UnreliableUnorderedChannel(), FactoryLifespan.AlwaysNew);
			Factory.Register<IUnreliableOrderedChannel>(() => new UnreliableOrderedChannel(), FactoryLifespan.AlwaysNew);
			Factory.Register<IReliableChannel>(() => new ReliableUnorderedChannel(DEFAULT_WINDOW_SIZE), FactoryLifespan.AlwaysNew);
			Factory.Register<IReliableOrderedChannel>(() => new ReliableOrderedChannel(DEFAULT_WINDOW_SIZE), FactoryLifespan.AlwaysNew);
		}
	}
}
