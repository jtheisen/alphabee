using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AlphaBee;

public static class Spanlikes
{
	public static Type? GetTypeFromSpanlikeOrNull(Type type)
	{
		return IsSpanlike(type, out _, out var genericTypeArgument) ? genericTypeArgument : null;
	}

	public static Boolean IsInlineSpanlike(PropertyInfo property, out ObjectExtent extent)
	{
		extent = default;

		var type = property.PropertyType;

		if (InlineSpanAttribute.IsInlineSpan(property, out var length))
		{
			if (IsSpanlike(type, out _, out var genericArgument))
			{
				var unitSize = genericArgument.SizeOf();

				extent = new(length, unitSize);

				return true;
			}
			else
			{
				throw new Exception($"Property {property} has {nameof(InlineSpanAttribute)} but is of unsuited property type {type}");
			}
		}
		else
		{
			return false;
		}
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
