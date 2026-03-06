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
}
