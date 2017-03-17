namespace Protocol
{
	using Utility;
	using System.Collections.Generic;

	public static partial class FactoryRegistrations
	{
		private static void RegisterGenerated()
		{
			DefaultSerializer serializer = new DefaultSerializer();
				Factory.Register<IProtocolSerializable<int>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<int>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<uint>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<uint>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<long>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<long>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<ulong>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<ulong>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<short>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<short>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<ushort>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<ushort>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<byte>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<byte>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<string>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<string>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<bool>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<bool>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<double>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<double>>>(() => serializer, FactoryLifespan.Singleton);
				Factory.Register<IProtocolSerializable<float>>(() => serializer, FactoryLifespan.Singleton);
			Factory.Register<IProtocolSerializable<List<float>>>(() => serializer, FactoryLifespan.Singleton);
			}
	}
}
