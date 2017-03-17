namespace Utility
{
	using System;

	public class FactoryRegisterException : Exception
	{
		public Type Type { get; set; }

		public FactoryRegisterException(Type t)
		{
			this.Type = t;
		}

		public override string ToString()
		{
			return $"Factory Register Exception {this.Type.FullName}: {base.ToString()}";
		}
	}
}