using System;
using System.Collections.Generic;

namespace Protocol.Language
{
	public class Parser
	{
		public const string MessageToken = "message";

		public List<Message> Parse(string code)
		{
			Lexer lexer = new Lexer();
			var tokens = lexer.GetTokens(code);

			List<Message> messages = new List<Message>();
			Message message = null;

			if (tokens.Count % 2 != 0)
			{
				throw new ArgumentException("code has token count error.");
			}

			for (int i = 0; i < tokens.Count; i += 2)
			{
				string name = tokens[i + 1];
				if (tokens[i].ToLower() == MessageToken)
				{
					if (message != null)
						messages.Add(message);

					message = new Message() { Name = name };
					continue;
				}

				if (message == null)
				{
					throw new ArgumentException("first token has to be a message.");
				}

				message.Entries.Add(new MessageEntry()
				{
					Type = tokens[i],
					Name = name
				});
			}

			if (message != null)
				messages.Add(message);

			return messages;
		}
	}
}
