using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTest
{
	using System.Threading;

	using Protocol.Language;

	using ReliableUdp;
	using ReliableUdp.Enums;
	using ReliableUdp.Utility;

	[TestClass]
	public class ProtocolGenerationTest
	{
		public string ExampleProtocol = @"message ChatMessage
	List<string> Message

message ChangeNick
	string newNick

message Whisper
	string receiver
	ChatMessage Message
";

		[TestMethod]
		public void TestParser()
		{
			Parser p = new Parser();
			var messages = p.Parse(this.ExampleProtocol);

			Assert.AreEqual(3, messages.Count);
		}

		[TestMethod]
		public void TestCodeGen()
		{
			Parser p = new Parser();
			var messages = p.Parse(this.ExampleProtocol);

			CodeGenerator codeGen = new CodeGenerator();
			string code = codeGen.GenerateCode(messages);
		}

	}
}
