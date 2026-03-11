namespace AlphaBee;

public static class TypeCodeExtensions
{
	public const Int32 FirstExtraTypeCodeInt = 20;
	public const TypeCode FirstExtraTypeCode = (TypeCode)FirstExtraTypeCodeInt;

	static Type[] extraTypes = new Type[32 - FirstExtraTypeCodeInt];
	static TypeCode extraTypeCodesEnd;

	static TypeCodeExtensions()
	{
		PrepareExtraTypes([ typeof(ObjectHeader), typeof(PropNo) ]);
	}

	static void PrepareExtraTypes(Type[] types)
	{
		Trace.Assert(types.Length < extraTypes.Length);

		for (var i = 0; i < types.Length; ++i)
		{
			extraTypes[i] = types[i];
		}

		extraTypeCodesEnd = FirstExtraTypeCode + types.Length;
	}

	public static String ToStringExtended(this TypeCode code)
	{
		if (code == TypeCode.String)
		{
			return "S-Char";
		}
		else if (code < FirstExtraTypeCode)
		{
			return code.ToString();
		}
		else if (code < extraTypeCodesEnd)
		{
			return FindType(code).Name;
		}
		else
		{
			return code.ToString();
		}
	}

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
			case >= FirstExtraTypeCode:
				return extraTypes[code - FirstExtraTypeCode] is not null;
			default:
				return false;
		}
	}

	public static TypeCode GetTypeCode(this Type type)
	{
		var typeCode = Type.GetTypeCode(type);

		if (typeCode != TypeCode.Object || type == typeof(Object))
		{
			return typeCode;
		}

		var i = Array.IndexOf(extraTypes, type);

		if (i >= 0)
		{
			return FirstExtraTypeCode + i;
		}

		throw new ArgumentException($"There is no TypeCode for type {type}");
	}

	public static Type FindType(this TypeCode code)
	{
		Trace.Assert(code.IsSupportedStruct(), $"Can't get a Type for {code}");

		var i = code - FirstExtraTypeCode;

		if (i >= 0)
		{
			if (extraTypes[i] is Type type)
			{
				return type;
			}
			else
			{
				throw new Exception($"Unknown special type code {code}");
			}
		}
		else
		{
			var name = $"System.{code}";

			return Type.GetType(name) ?? throw new Exception($"Can't find type {name}");
		}
	}
}
