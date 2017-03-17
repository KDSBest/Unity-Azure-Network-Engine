using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utility
{
	public class ConcurrentQueue<T> where T : class
	{
		private Queue<T> queue = new Queue<T>();

		public void Enqueue(T obj)
		{
			lock (this.queue)
			{
				this.queue.Enqueue(obj);
			}
		}

		public T Dequeue()
		{
			lock (this.queue)
			{
				if (this.queue.Count == 0)
					return null;

				return this.queue.Dequeue();
			}
		}
	}
}
