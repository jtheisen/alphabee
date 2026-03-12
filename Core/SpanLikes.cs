using System.Diagnostics.CodeAnalysis;

namespace AlphaBee;

public static class Spanlikes
{
	public static Type? GetTypeFromSpanlikeOrNull(Type type)
	{
		return IsSpanlike(type, out _, out var genericTypeArgument) ? genericTypeArgument : null;
	}

	public static Boolean IsSpanlike(Type type, [NotNullWhen(true)] out Type? genericTypeDefinition, [NotNullWhen(true)] out Type? genericArgument)
	{
		genericTypeDefinition = null;
		genericArgument = null;

		if (!type.IsGenericType)
		{
			return false;
		}

		genericTypeDefinition = type.GetGenericTypeDefinition();

		var genericArguments = type.GetGenericArguments();

		if (genericArguments.Length != 1)
		{
			return false;
		}

		genericArgument = genericArguments[0];

		if (genericTypeDefinition == typeof(Span<>))
		{
			return true;
		}
		else if (genericTypeDefinition == typeof(ReadOnlySpan<>))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
