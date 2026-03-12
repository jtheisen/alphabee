namespace AlphaBee;

public static class SpanLikes
{
	public static Type? GetTypeFromSpanlikeOrNull(Type type)
	{
		if (!type.IsGenericType)
		{
			return null;
		}

		var genericTypeDefinition = type.GetGenericTypeDefinition();

		var genericArguments = type.GetGenericArguments();

		if (genericArguments.Length != 1)
		{
			return null;
		}

		var genericArgument = genericArguments[0];

		if (genericTypeDefinition == typeof(Span<>))
		{
			return genericArgument;
		}
		else if (genericTypeDefinition == typeof(ReadOnlySpan<>))
		{
			return genericArgument;
		}
		else
		{
			return null;
		}
	}
}
