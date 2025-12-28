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
		var mask = 1ul << (i & 0b111111);
		return (word & mask) != 0;
	}

	public static void SetBit(this ref UInt64 word, Int32 i, Boolean value)
	{
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
		i = words.IndexOfAnyExcept(pattern);

		if (i < 0)
		{
			return false;
		}

		var o = BitOperations.TrailingZeroCount(words[i] ^ pattern);

		i = i * 64 + o;

		return true;
	}

	public static Boolean TryIndexOfBitZero(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(UInt64.MaxValue, out i);

	public static Boolean TryIndexOfBitOne(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(0, out i);

	public static Int32 IndexOfBitZero(this Span<UInt64> words)
		=> words.TryIndexOfBitZero(out var i) ? i : -1;

	public static Int32 TryIndexOfBitOne(this Span<UInt64> words)
		=> words.TryIndexOfBitOne(out var i) ? i : -1;

	public static Span<Byte> AsBytes<T>(this ref T value)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
	}

	public static ref Byte AtByte<T>(this ref T value, Int32 i)
		where T : unmanaged
	{
		return ref value.AsBytes()[i];
	}

	public static ref UInt16 AtUInt16(this ref UInt64 value, Int32 i)
	{
		return ref value.AsBytes().InterpretAs<UInt16>()[i];
	}

	public static Char ToBrailleChar(Byte value)
	{
		return (Char)(0x2800 + value);
	}

	public static String ToBrailleString(this Span<Byte> value)
	{
		return String.Join("", value.ToArray().Select(ToBrailleChar).ToArray());
	}

	public static String ToBrailleString<T>(this T value)
		where T : unmanaged
	{
		return value.AsBytes().ToBrailleString();
	}

	public static Byte GetPageType(this Span<Byte> page)
	{
		return page[0];
	}
}

public struct AllBitsSet<T>
	where T : unmanaged
{
	public static readonly UInt512 Value;

	static AllBitsSet()
	{
		Value.AsBytes().Fill(Byte.MaxValue);
	}
}

[InlineArray(8)]
public struct UInt512
{
	private UInt64 _element0;
}
