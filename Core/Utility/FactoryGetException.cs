namespace Utility
{
	using System;

	public class FactoryGetException : Exception
	{
		public Type Type { get; set; }

		public FactoryGetException(Type t)
		{
			this.Type = t;
		}

		public override string ToString()
		{
			return $"Factory Get Exception {this.Type.FullName}: {base.ToString()}";
		}
	}
}