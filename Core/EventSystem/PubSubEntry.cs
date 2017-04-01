namespace EventSystem
{
	using System;

	public class PubSubEntry<T>
	{
		public Action<T> Action { get; set; }
	}
}