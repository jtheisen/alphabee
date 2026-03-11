using System.Reflection;
using System.Runtime.InteropServices;

namespace AlphaBee;

public static class NullableStruct
{
	public static Type MakeNullableType(Type baseType)
	{
		return typeof(NullableStruct<>).MakeGenericType(baseType);
	}

	public static Type? GetUnderlyingType(Type type)
	{
		if (type.GetGenericTypeDefinition() != typeof(NullableStruct<>))
		{
			return null;
		}

		var args = type.GetGenericArguments();

		return args[0];
	}

	public static void FixToClrNullableIfAppropriate(ref Object? value)
	{
		if (value is INullableStruct nullable)
		{
			value = nullable.Get();
		}
	}

	public static void CopyFrom<T>(this Span<NullableStruct<T>> target, Span<T?> source)
		where T : unmanaged
	{
		Debug.Assert(source.Length == target.Length);

		for (var i = 0; i < source.Length; ++i)
		{
			target[i] = source[i];
		}
	}

	public static void CopyTo<T>(this Span<NullableStruct<T>> source, Span<T?> target)
		where T : unmanaged
	{
		Debug.Assert(source.Length == target.Length);

		for (var i = 0; i < source.Length; ++i)
		{
			target[i] = source[i];
		}
	}
}

public interface INullableStruct
{
	Object? Get();
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct NullableStruct<T> : INullableStruct
	where T : unmanaged
{
	readonly T value;
	readonly Boolean hasValue;

	public static implicit operator T?(NullableStruct<T> self)
	{
		return self.hasValue ? self.value : null;
	}

	public static implicit operator NullableStruct<T>(T? value)
	{
		return new NullableStruct<T>(value);
	}

	public NullableStruct(T? value)
	{
		if (value is T notnull)
		{
			this.value = notnull;
			hasValue = true;
		}
	}

	Object? INullableStruct.Get() => (T?)this;
}
