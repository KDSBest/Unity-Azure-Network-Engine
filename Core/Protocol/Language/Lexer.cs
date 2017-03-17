using System;
using System.Collections.Generic;
using System.Linq;

namespace Protocol.Language
{
	public class Lexer
	{
		private char[] splitChars = new char[] { '\n', '\r', '\t', ' ' };

		public List<string> GetTokens(string code)
		{
			return code.Split(this.splitChars, StringSplitOptions.RemoveEmptyEntries).ToList();
		}
	}
}
