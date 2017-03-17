using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utility
{
	public static class Factory
	{
		private static Dictionary<Type, FactoryBuilder> factories = new Dictionary<Type, FactoryBuilder>();

		public static void Register<T>(Func<T> creationFunc, FactoryLifespan lifespan = FactoryLifespan.AlwaysNew, FactoryOverwrite overwrite = FactoryOverwrite.Overwrite)
		{
			Type t = typeof(T);
			if (factories.ContainsKey(t))
			{
				if (overwrite == FactoryOverwrite.Exception)
					throw new FactoryRegisterException(t);

				factories.Remove(t);
			}

			factories.Add(t, new FactoryBuilder<T>(creationFunc, lifespan));
		}

		public static T Get<T>()
		{
			Type t = typeof(T);
			if (factories.ContainsKey(t))
			{
				var factoryBuilder = factories[t] as FactoryBuilder<T>;

				if (factoryBuilder != null)
					return factoryBuilder.Create();
			}


			throw new FactoryGetException(t);
		}
	}
}
