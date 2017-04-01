using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSystem
{
	public static class PubSub<T>
	{
		private static readonly Dictionary<string, PubSubEntry<T>> entries = new Dictionary<string, PubSubEntry<T>>();

		public static void Subscribe(string name, Action<T> action)
		{
			var newEntry = new PubSubEntry<T>() { Action = action };
			if (entries.ContainsKey(name))
				entries[name] = newEntry;
			else
				entries.Add(name, newEntry);
		}

		public static void Unsubscribe(string name)
		{
			if (entries.ContainsKey(name))
				entries.Remove(name);
		}

		public static void Publish(T value)
		{
			foreach (var entry in entries)
			{
				entry.Value.Action(value);
			}
		}
	}
}
