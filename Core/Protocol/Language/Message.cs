using System.Collections.Generic;

namespace Protocol.Language
{
	public class Message
	{
		public string Name { get; set; }

		public List<MessageEntry> Entries { get; set; }

		public Message()
		{
			Entries = new List<MessageEntry>();
		}
	}
}
