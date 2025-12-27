namespace AlphaBee;

public static class SpanExtensions
{
	public static Span<T> InterpretAs<T>(this Span<Byte> span)
		where T : unmanaged
	{
		return MemoryMarshal.Cast<Byte, T>(span);
	}

	public static Span<Byte> Deinterpret<T>(this Span<T> span)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(span);
	}

	public static ReadOnlySpan<T> InterpretAs<T>(this ReadOnlySpan<Byte> span)
		where T : unmanaged
	{
		return MemoryMarshal.Cast<Byte, T>(span);
	}

	public static ReadOnlySpan<Byte> Deinterpret<T>(this ReadOnlySpan<T> span)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(span);
	}

	public static Boolean GetBit(this ref UInt64 word, Int32 i)
	{
		var offset = i >> 6;
		var mask = 1ul << (i & 0b111111);
		return (word & mask) != 0;
	}

	public static void SetBit(this ref UInt64 word, Int32 i, Boolean value)
	{
		var offset = i >> 6;
		var mask = 1ul << (i & 0b111111);
		if (value)
		{
			word |= mask;
		}
		else
		{
			word &= ~mask;
		}
	}

	public static void SetBit(this Span<UInt64> span, Int32 i, Boolean value)
	{
		var h = i >> 6;
		var l = i & 0b111111;
		span[h].SetBit(l, value);
	}

	public static Boolean GetBit(this Span<UInt64> span, Int32 i)
	{
		var h = i >> 6;
		var l = i & 0b111111;
		return span[h].GetBit(l);
	}

	public static Boolean TryIndexOfBitCore(this UInt64 word, UInt64 pattern, out Int32 i)
	{
		i = BitOperations.TrailingZeroCount(word ^ pattern);

		return i < 64;
	}

	public static Boolean TryIndexOfBitZero(this UInt64 word, out Int32 i)
	{
		return word.TryIndexOfBitCore(UInt64.MaxValue, out i);
	}

	public static Boolean TryIndexOfBitOne(this UInt64 word, out Int32 i)
	{
		return word.TryIndexOfBitCore(0, out i);
	}

	public static Boolean TryIndexOfBitCore(this Span<UInt64> words, UInt64 pattern, out Int32 i)
	{
		i = 0;

		foreach (var word in words)
		{
			var count = BitOperations.TrailingZeroCount(word ^ pattern);

			if (count < 64)
			{
				i += count;

				return true;
			}

			i += 64;
		}

		return false;
	}

	public static Boolean TryIndexOfBitZero(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(UInt64.MaxValue, out i);

	public static Boolean TryIndexOfBitOne(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(0, out i);

	public static Int32 IndexOfBitZero(this Span<UInt64> words)
		=> words.TryIndexOfBitZero(out var i) ? i : -1;

	public static Int32 TryIndexOfBitOne(this Span<UInt64> words)
		=> words.TryIndexOfBitOne(out var i) ? i : -1;

	public static ref Byte GetByte(this UInt64 value, Int32 i)
	{
		return ref MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1))[i];
	}

	public static ref UInt16 GetUInt16(this UInt64 value, Int32 i)
	{
		return ref MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)).InterpretAs<UInt16>()[i];
	}

	public static Byte GetPageType(this Span<Byte> page)
	{
		return page[0];
	}
}
