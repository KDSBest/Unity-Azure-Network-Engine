namespace Utility
{
	using System;

	public class FactoryBuilder
	{		
	}
	public class FactoryBuilder<T> : FactoryBuilder
	{
		public Func<T> CreationFunc { get; set; }

		public FactoryLifespan Lifespan { get; set; }

		private static T instance;

		private static object lockObject = new object();

		public FactoryBuilder(Func<T> creationFunc, FactoryLifespan lifespan)
		{
			this.CreationFunc = creationFunc;
			this.Lifespan = lifespan;
		}

		public T Create()
		{
			if (Lifespan == FactoryLifespan.Singleton)
			{
				// Double Check Lock
				if (instance == null)
				{
					lock (lockObject)
					{
						if (instance == null)
						{
							instance = CreationFunc();
						}
					}
				}

				return instance;
			}

			return CreationFunc();
		}
	}

}