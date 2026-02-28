using System.Diagnostics;

namespace AlphaBee;

public static class TypeCodeExtensions
{
	public static Boolean IsSupportedStruct(this TypeCode code)
	{
		switch (code)
		{
			case TypeCode.Empty:
			case TypeCode.DBNull:
			case TypeCode.String: // handled via Char
			case TypeCode.Object: // represents a reference
			case (TypeCode)17:
				return false;
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.Byte:
			case TypeCode.Int16:
			case TypeCode.UInt16:
			case TypeCode.Int32:
			case TypeCode.UInt32:
			case TypeCode.Int64:
			case TypeCode.UInt64:
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.Decimal:
			case TypeCode.DateTime:
				return true;
			default:
				return false;
		}
	}

	public static Type FindType(this TypeCode code)
	{
		Trace.Assert(code.IsSupportedStruct(), $"Can't get a Type for {code}");

		var name = $"System.{code}";

		return Type.GetType(name) ?? throw new Exception($"Can't find type {name}");
	}
}
