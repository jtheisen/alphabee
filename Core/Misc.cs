
using System.Reflection.PortableExecutable;

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

public enum PageType
{
	Unkown = '?',
	PageIndex = 'p'
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

[DebuggerDisplay("{ToString()}")]
public ref struct UInt64Page
{
	const Int32 SizeLog2 = 12;
	const Int32 Size = 1 << SizeLog2;
	const Int32 WordSize = 8;
	const Int32 LeadWordsLog2 = 2;
	const Int32 LeadWords = 1 << LeadWordsLog2;

	public const UInt64 ContentLength = Size / WordSize - LeadWords;
	public const UInt64 TotalBits = ContentLength * WordSize * 8;

	const UInt64 AllPattern = UInt64.MaxValue >> 2;

	ref UInt64 header;
	ref UInt64 used;
	ref UInt64 full;
	Span<UInt64> content;

	public ref Byte PageTypeByte => ref header.GetByte(0);
	public ref Byte PageDepthByte => ref header.GetByte(1);

	public override String ToString()
	{
		return $"P{PageDepthChar} {full:x}";
	}

	public String PageCharPair => $"{PageTypeChar}{PageDepthChar}";

	public PageType PageTypeChar => (PageType)PageTypeByte;
	public Char PageDepthChar => PageDepthByte < 10 ? (Char)('0' + PageDepthByte) : '+';

	public UInt64Page(Span<Byte> page)
	{
		var words = page.InterpretAs<UInt64>();
		content = words[2..];
		header = ref words[0];
		used = ref words[1];
		full = ref words[2];
	}

	public void Init(PageType pageType, Int32 pageDepth)
	{
		used = full = default;
		PageTypeByte = (Byte)pageType;
		PageDepthByte = pageDepth < Byte.MaxValue ? (Byte)pageDepth : Byte.MaxValue;
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
