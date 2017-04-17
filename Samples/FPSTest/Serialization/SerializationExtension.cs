using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Protocol;

using ReliableUdp.Utility;

namespace FPSTest.Serialization
{
	public static class SerializationExtension
	{
		public static void Serialize(this List<ClientUpdate> list, UdpDataWriter writer)
		{
			writer.Put(list.Count);

			foreach (var shot in list)
			{
				shot.Serialize(writer);
			}
		}

		public static void Deserialize(this List<ClientUpdate> list, UdpDataReader reader)
		{
			int count = reader.GetInt();

			for (int i = 0; i < count; i++)
			{
				ClientUpdate s = new ClientUpdate();
				if (!s.Deserialize(reader))
				{
					break;
				}
				list.Add(s);
			}
		}

		public static void Serialize(this List<Shot> list, UdpDataWriter writer)
		{
			writer.Put(list.Count);

			foreach (var shot in list)
			{
				shot.Serialize(writer);
			}
		}

		public static void Deserialize(this List<Shot> list, UdpDataReader reader)
		{
			int count = reader.GetInt();

			for (int i = 0; i < count; i++)
			{
				Shot s = new Shot();
				if (!s.Deserialize(reader))
				{
					break;
				}
				list.Add(s);
			}
		}
	}
}
