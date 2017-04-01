using System.Collections.Generic;

namespace Protocol.Language
{
	using System.Linq;

	public class CodeGenerator
	{
		public static readonly string[] DefaultTypes = new string[]
		{
		"int", "uint", "long", "ulong", "short", "ushort", "byte", "string", "bool", "double", "float",
		"List<int>", "List<uint>", "List<long>", "List<ulong>", "List<short>", "List<ushort>", "List<byte>", "List<string>", "List<bool>", "List<double>", "List<float>"
		};

		public const string FileTemplate = @"namespace Protocol
{{
	using System.Collections.Generic;

	using Protocol.Language;

	using ReliableUdp.Packet;
	using ReliableUdp.Utility;

	using Utility;

	{0}
}}";

		public const string ClassTemplate = @"public class {0} : IProtocolPacket
{{
			public byte PacketType
			{{
				get
				{{
					return {2};
				}}
			}}

			{1}

			public {0}()
			{{
				{3}
			}}

			public void Serialize(UdpDataWriter writer)
			{{
				writer.Put(PacketType);
				{4}
			}}

			public bool Deserialize(UdpDataReader reader)
			{{
				if (reader.PeekByte() != PacketType)
					return false;

				reader.GetByte();

				{5}

				return true;
			}}
}}";

		public const string ParameterTemplate = @"public {0} {1} {{ get; set; }}
";

		public const string ParameterCreateTemplate = @"{1} = new {0}();
";

		public const string ParameterSerializeDefaultTypeTemplate = @"Factory.Get<IProtocolSerializable<{0}>>().Serialize({1}, writer);
";

		public const string ParameterSerializeTemplate = @"{1}.Serialize(writer);
";

		public const string ParameterDeserializeTemplate = @"{0} local{1} = new {0}();
local{1}.Deserialize(reader);
{1} = local{1};
";
		public const string ParameterDeserializeDefaultTypeTemplate = @"{0} local{1} = new {0}();
Factory.Get<IProtocolSerializable<{0}>>().Deserialize(ref local{1}, reader);
{1} = local{1};
";

		public string GenerateCode(List<Message> messages)
		{
			string code = string.Empty;

			for(int i = 0; i < messages.Count; i++)
			{
				var message = messages[i];
				string parameterCode = string.Empty;
				string parameterCreateCode = string.Empty;
				string parameterSerializeCode = string.Empty;
				string parameterDeserializeCode = string.Empty;

				foreach (var parameter in message.Entries)
				{
					bool isDefaultType = DefaultTypes.Contains(parameter.Type);
					parameterCode += string.Format(ParameterTemplate, parameter.Type, parameter.Name);
					parameterCreateCode += string.Format(ParameterCreateTemplate, parameter.Type, parameter.Name);
					parameterSerializeCode += string.Format(isDefaultType ? ParameterSerializeDefaultTypeTemplate : ParameterSerializeTemplate, parameter.Type, parameter.Name);
					parameterDeserializeCode += string.Format(isDefaultType ? ParameterDeserializeDefaultTypeTemplate : ParameterDeserializeTemplate, parameter.Type, parameter.Name);
				}

				code += string.Format(ClassTemplate, message.Name, parameterCode, i + 1, parameterCreateCode, parameterSerializeCode, parameterDeserializeCode);
			}

			return string.Format(FileTemplate, code).Replace("new string();", "string.Empty;");
		}
	}
}
