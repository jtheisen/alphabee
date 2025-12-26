
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
		i = BitOperations.LeadingZeroCount(word ^ pattern);

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
			var count = BitOperations.LeadingZeroCount(word ^ pattern);

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

	public static Byte GetPageType(this Span<Byte> page)
	{
		return page[0];
	}
}

public static class Extensions
{
	public static ref T GetFieldObject<T>(this FieldPage<UInt64> root, Int32 level, Int32 i)
		where T : unmanaged
	{
		root.Get(i);
	}
}

public interface IPageLayout
{
	Int32 Size { get; }
}

public interface IFieldPageLayout : IPageLayout
{
	Int32 HeaderSize { get; }
	Int32 BitFieldSize { get; }
	Int32 FieldLength { get; }
}

public static class FieldPageType
{
	public const Byte Unkown = 0;
	public const Byte PageIndexBranch = (Byte)'p';
	public const Byte PageIndexLeaf = (Byte)'P';
}


public struct FieldPageLayout4K<T> : IFieldPageLayout
	where T : unmanaged
{
	public Int32 SizeLog2 => 12;

	public Int32 Size => 1 << SizeLog2;

	public Int32 HeaderSize => Unsafe.SizeOf<UInt64>();

	public Int32 BitFieldSize => Size / Unsafe.SizeOf<UInt64>() / 8;

	public Int32 UsedBitFieldOffset => HeaderSize + BitFieldSize;

	public Int32 ContentOffset => HeaderSize + BitFieldSize * 2;

	public Int32 FieldLength => (Size - ContentOffset) / Unsafe.SizeOf<T>();

	public FieldPage<T> Create(Span<Byte> page) => new FieldPage<T>
	{
		header = page[..HeaderSize],
		content = page[ContentOffset..].InterpretAs<T>(),
		full = page[HeaderSize..UsedBitFieldOffset].InterpretAs<UInt64>(),
		used = page[UsedBitFieldOffset..ContentOffset].InterpretAs<UInt64>()
	};
}

public struct HeaderPageLayout
{
	public UInt64 IndexRootOffset;
	public Int32 IndexDepth;
	public UInt64 AddressSpaceEnd;
}

public ref struct HeaderPage
{
	public ref HeaderPageLayout header;

	public HeaderPage(Span<Byte> page)
	{
		header = ref page.InterpretAs<HeaderPageLayout>()[0];
	}
}

public ref struct UInt64Page
{
	const Int32 SizeLog2 = 12;
	const Int32 Size = 1 << SizeLog2;
	const Int32 WordSize = 8;
	const Int32 HeaderWords = 2;

	public const Int32 ContentLength = Size / WordSize - HeaderWords;
	public const UInt64 TotalBits = ContentLength * WordSize * 8;

	const UInt64 AllPattern = UInt64.MaxValue & ~3ul;

	ref UInt64 used;
	ref UInt64 full;
	Span<UInt64> content;

	public UInt64Page(Span<Byte> page)
	{
		var words = page.InterpretAs<UInt64>();
		content = words[2..];
		full = ref words[0];
		used = ref words[1];
	}

	public void Clear()
	{
		used = full = default;
	}

	public void SetFullBit(Int32 i, Boolean value) => full.SetBit(i, value);
	public Boolean GetFullBit(Int32 i) => full.GetBit(i);

	public void SetUsedBit(Int32 i, Boolean value) => used.SetBit(i, value);
	public Boolean GetUsedBit(Int32 i) => used.GetBit(i);

	public Boolean TryIndexOfUnfull(out Int32 i)
	{
		var found = full.TryIndexOfBitZero(out i);

		return found && i < content.Length;
	}

	public Boolean IsFull => full == AllPattern;

	public ref UInt64 Get(Int32 i)
	{
		return ref content[i];
	}
}


public ref struct FieldPage<T>
	where T : unmanaged
{
	public Span<Byte> header;
	public Span<T> content;
	public Span<UInt64> full;
	public Span<UInt64> used;

	public Byte PageType { get => header[0]; set => header[0] = value; }
	public Int32 Depth { get => header[1]; set => header[1] = (Byte)value; }

	public void SetUsedBit(Int32 i, Boolean value) => used.SetBit(i, value);
	public Boolean GetUsedBit(Int32 i) => used.GetBit(i);

	public Boolean SetFullBit(Int32 i, Boolean value)
	{
		used.SetBit(i, value);
		
	}

	public Boolean GetFullBit(Int32 i) => used.GetBit(i);

	public Boolean TryIndexOfUnfull(out Int32 i)
	{
		var found = full.TryIndexOfBitZero(out i);

		return found && i < content.Length;
	}

	public ref T Get(Int32 i)
	{
		return ref content[i];
	}
}
