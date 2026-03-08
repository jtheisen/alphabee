namespace AlphaBee;

public static class GenericBinaryIntegerExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Log2<T>(this T value)
		where T : IBinaryInteger<T>, ISignedNumber<T>
	{
		return Int32.CreateTruncating(T.Log2(value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Log2Ceil<T>(this T value)
		where T : IBinaryInteger<T>, ISignedNumber<T>
	{
		return Int32.CreateTruncating(T.Log2(value - T.One) + T.One);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T CeilToPowerOfTwo<T>(this T value)
		where T : IBinaryInteger<T>, IShiftOperators<T, Int32, T>, ISignedNumber<T>
	{
		if (T.IsZero(value))
		{
			return T.Zero;
		}

		return T.One << value.Log2Ceil();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T CeilToPowerOfTwoAlignment<T>(this T value, T alignment)
		where T : IBinaryInteger<T>, IShiftOperators<T, Int32, T>, ISignedNumber<T>
	{
		Debug.Assert(T.PopCount(alignment) == T.One);

		var mask = alignment - T.One;

		var result = (value + mask) & ~mask;

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T CeilToAlignment<T>(this T value, T alignment)
		where T : IBinaryInteger<T>, IShiftOperators<T, Int32, T>
	{
		var m = (alignment + (-value) % alignment) % alignment;

		var result = value + m;

		return result;
	}
}
