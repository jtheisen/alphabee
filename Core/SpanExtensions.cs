namespace AlphaBee;

public static class SpanExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean GetBit(this ref UInt64 word, Int32 i)
	{
		Debug.Assert(i < 64);

		var mask = 1ul << (i & 0b111111);
		return (word & mask) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetBit(this ref UInt64 word, Int32 i, Boolean value)
	{
		Debug.Assert(i < 64);

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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean GetBit<T>(this ref T bits, Int32 i)
		where T : unmanaged
	{
		Debug.Assert(Unsafe.SizeOf<T>() >= 8);
		Debug.Assert(i < Unsafe.SizeOf<T>() * 8);

		var i0 = i / 64;
		var i1 = i % 64;

		return bits.AsUInt64s()[i0].GetBit(i1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetBit<T>(this ref T bits, Int32 i, Boolean value)
		where T : unmanaged
	{
		Debug.Assert(Unsafe.SizeOf<T>() >= 8);
		Debug.Assert(i < Unsafe.SizeOf<T>() * 8);

		var i0 = i / 64;
		var i1 = i % 64;

		bits.AsUInt64s()[i0].SetBit(i1, value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static Boolean IsAllCore<T>(this ref T bits, UInt64 pattern)
		where T : unmanaged
	{
		Debug.Assert(Unsafe.SizeOf<T>() >= 8);

		var span = bits.AsUInt64s();

		var i = span.IndexOfAnyExcept(pattern);

		return i < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean IsAllOne<T>(this ref T bits) where T : unmanaged
		=> bits.IsAllCore(UInt64.MaxValue);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean IsAllZero<T>(this ref T bits) where T : unmanaged
		=> bits.IsAllCore(0ul);

	public static Boolean BitImplies<T>(this ref T condition, ref T conclusion)
		where T : unmanaged
	{
		Debug.Assert(Unsafe.SizeOf<T>() >= 8);

		var conditionSpan = condition.AsUInt64s();
		var conclusionSpan = conclusion.AsUInt64s();

		for (var i = 0; i < conditionSpan.Length; ++i)
		{
			var word = conditionSpan[i] & ~conclusionSpan[i];

			if (word != 0ul) return false;
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 IndexOfBitCore<T>(this ref T bits, UInt64 pattern)
		where T : unmanaged
	{
		Debug.Assert(Unsafe.SizeOf<T>() >= 8);

		var span = bits.AsUInt64s();

		return span.IndexOfBitCore(pattern);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 IndexOfBitZero<T>(this ref T bits) where T : unmanaged 
		=> bits.IndexOfBitCore(UInt64.MaxValue);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 IndexOfBitOne<T>(this ref T bits) where T : unmanaged
		=> bits.IndexOfBitCore(0ul);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean TryIndexOfBitZero<T>(this ref T word, out Int32 i) where T : unmanaged
	{
		i = word.IndexOfBitZero();
		return i < Unsafe.SizeOf<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean TryIndexOfBitOne<T>(this ref T word, out Int32 i) where T : unmanaged
	{
		i = word.IndexOfBitOne();
		return i < Unsafe.SizeOf<T>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Allocate<T>(this ref T bits) where T : unmanaged
	{
		var i = bits.IndexOfBitZero();

		if (i == Unsafe.SizeOf<T>() * 8)
		{
			throw new BitArrayFullException();
		}

		bits.SetBit(i, true);

		return i;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> InterpretAs<T>(this Span<Byte> span)
		where T : unmanaged
	{
		return MemoryMarshal.Cast<Byte, T>(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<Byte> Deinterpret<T>(this Span<T> span)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<T> InterpretAs<T>(this ReadOnlySpan<Byte> span)
		where T : unmanaged
	{
		return MemoryMarshal.Cast<Byte, T>(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ReadOnlySpan<Byte> Deinterpret<T>(this ReadOnlySpan<T> span)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(span);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean TryIndexOfBitCore(this Span<UInt64> words, UInt64 pattern, out Int32 i)
	{
		i = IndexOfBitCore(words, pattern);

		return i < words.Length * 8 * 64;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 IndexOfBitCore(this Span<UInt64> words, UInt64 pattern)
	{
		var n = words.Length;

		var result = 0;

		for (var i = 0; i < n; ++i)
		{
			var j = BitOperations.TrailingZeroCount(words[i] ^ pattern);

			result += j;

			if (j < 64)
			{
				break;
			}
		}

		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean TryIndexOfBitZero(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(UInt64.MaxValue, out i);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean TryIndexOfBitOne(this Span<UInt64> words, out Int32 i)
		=> words.TryIndexOfBitCore(0, out i);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 IndexOfBitZero(this Span<UInt64> words)
		=> words.IndexOfBitCore(UInt64.MaxValue);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 IndexOfBitOne(this Span<UInt64> words)
		=> words.IndexOfBitCore(0ul);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<Byte> AsBytes<T>(this ref T value)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<UInt64> AsUInt64s<T>(this ref T value)
		where T : unmanaged
	{
		return MemoryMarshal.CreateSpan(ref value, 1).Deinterpret().InterpretAs<UInt64>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref Byte AtByte<T>(this ref T value, Int32 i)
		where T : unmanaged
	{
		return ref value.AsBytes()[i];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref UInt16 AtUInt16(this ref UInt64 value, Int32 i)
	{
		return ref value.AsBytes().InterpretAs<UInt16>()[i];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static UInt64 ToUInt64(this Int32 source)
	{
		Debug.Assert(source >= 0);

		return (UInt32)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 ToInt32(this UInt64 source)
	{
		Trace.Assert(source < Int32.MaxValue);

		return (Int32)source;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Log2(this UInt64 value)
	{
		var zeroes = BitOperations.LeadingZeroCount(value);

		var length = Unsafe.SizeOf<UInt64>() * 8;

		return length - zeroes - 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Log2Ceil(this UInt64 value)
	{
		var log2 = value.Log2();

		if ((value & ((1ul << log2) - 1ul)) != 0ul)
		{
			++log2;
		}

		return log2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Log2(this UInt32 value) => ((UInt64)value).Log2();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Int32 Log2(this Int32 value) => ((UInt64)value).Log2();

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
