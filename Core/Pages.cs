using System.Diagnostics.Tracing;
using System.Drawing;

namespace AlphaBee;

public enum PageType
{
	Unkown = '.',
	PageIndex = 'p'
}

public interface IPageLayout
{
	Int32 Size { get; }
}

public interface IFieldPageLayout : IPageLayout
{
	Int32 LeadSize { get; }

	Int32 LeadWords { get; }

	Int32 FieldLength { get; }

	UInt64 AllPattern { get; }
}

public static class PageExtensions
{
	public static void ValidateFieldPage(this BitFieldPage page, Boolean asBitFieldLeaf)
	{
		page.Validate();

		for (var i = 0; i < 64; ++i)
		{
			if (page.GetUsedBit(i))
			{
				var word = page.At(i);

				Debug.Assert(word != 0);

				if (asBitFieldLeaf)
				{
					Debug.Assert(word != UInt64.MaxValue || page.GetFullBit(i));
				}
			}
		}
	}
}

public struct FieldPageLayout<T> : IFieldPageLayout
	where T : unmanaged
{
	const Int32 BitsPerByteLog2 = 3;
	const Int32 BitsPerByte = 1 << BitsPerByteLog2;
	const Int32 WordSizeLog2 = 3;
	const Int32 WordSize = 1 << WordSizeLog2;

	public Int32 SizeLog2 => 12;
	public Int32 Size => 1 << SizeLog2;

	public UInt64 Size64 => (UInt64)Size;

	public Int32 LeadWordsLog2 => 2;
	public Int32 LeadWords => 1 << LeadWordsLog2;
	public Int32 LeadSize => LeadWords * WordSize;

	public Int32 ItemSize => Unsafe.SizeOf<T>();

	public Int32 BitsPerWordLog2 => WordSizeLog2 + BitsPerByteLog2;

	public Int32 ContentSize => Size - LeadSize;

	public Int32 ContentBitSize => ContentSize * WordSize * BitsPerByte;

	public UInt64 AllPattern => UInt64.MaxValue >> LeadWordsLog2;

	public Int32 FieldLength => ContentSize / ItemSize;
}

public struct HeaderPageLayout
{
	public UInt64 IndexRootOffset;
	public Int32 IndexDepth;
	public UInt64 NextPageOffset;
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
public ref struct BitsWord
{
	ref UInt64 word;

	public BitsWord(ref UInt64 word)
	{
		this.word = ref word;
	}

	public override String ToString()
	{
		return word.ToBrailleString();
	}
}

public ref struct PageHeader
{
	ref UInt64 data;

	public PageHeader(ref UInt64 data)
	{
		this.data = ref data;
	}

	public void Init(PageType pageType, Int32 pageDepth)
	{
		Debug.Assert(PageTypeByte == 0, "Wont initialize used page");

		PageTypeByte = (Byte)pageType;
		PageDepthByte = pageDepth < Byte.MaxValue ? (Byte)pageDepth : Byte.MaxValue;
	}

	public void Validate()
	{
		Debug.Assert(PageTypeByte != 0);
	}

	public ref Byte PageTypeByte => ref data.AtByte(0);
	public ref Byte PageDepthByte => ref data.AtByte(1);

	public String PageCharPair => $"{PageTypeChar}{PageDepthChar}";

	public PageType PageType => (PageType)PageTypeByte;

	public Char PageTypeChar => (Char)PageTypeByte;
	public Char PageDepthChar => PageDepthByte < 10 ? (Char)('0' + PageDepthByte) : '+';
}


struct Layout<T, I>
{
	public Int32 PageSize => Constants.PageSize32;

	public Int32 IndexSize => Unsafe.SizeOf<I>();
	public Int32 ItemSize => Unsafe.SizeOf<T>();
	public Int32 PaddedItemSize => 1 << Unsafe.SizeOf<T>().ToUInt64().Log2Ceil();

	public Int32 LeadSize => IndexSize * 4;

	public Int32 ContentSize => PageSize - LeadSize;

	public Int32 ContentBitSize => ContentSize * Constants.BitsPerByte;

	public Int32 FieldLength => ContentSize / PaddedItemSize;
}

[DebuggerDisplay("{ToString()}")]
public ref struct FieldPage<T, I>
	where T : unmanaged
	where I : unmanaged
{
	Layout<T, I> layout;

	PageHeader header;
	ref I used;
	ref I full;
	Span<T> content;

	public Int32 IndexSize => Unsafe.SizeOf<I>();
	public Int32 PaddedItemSize => 1 << Unsafe.SizeOf<T>().ToUInt64().Log2Ceil();

	public UInt64 PageSize => Constants.PageSize;

	public override String ToString()
	{
		return $"[{header.PageCharPair}|{used.ToBrailleString()}|{full.ToBrailleString()}]";
	}

	public FieldPage(Span<Byte> page)
	{
		//Debug.Assert();

		var bitArrays = page.InterpretAs<I>();

		content = page[layout.LeadSize..].InterpretAs<T>();
		header = new PageHeader(ref page.InterpretAs<UInt64>()[0]);
		used = ref bitArrays[1];
		full = ref bitArrays[2];
	}

	public void Init(PageType pageType, Int32 pageDepth)
	{
		header.Init(pageType, pageDepth);
		used = full = default;
	}

	public void Validate()
	{
		header.Validate();

		Debug.Assert(full.BitImplies(ref used));
	}

	public void SetFullBit(Int32 i, Boolean value)
	{
		Debug.Assert(i < layout.FieldLength);

		full.SetBit(i, value);
	}

	public Boolean GetFullBit(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		return full.GetBit(i);
	}

	public void SetUsedBit(Int32 i, Boolean value)
	{
		Debug.Assert(i < layout.FieldLength);

		used.SetBit(i, value);
	}

	public Boolean GetUsedBit(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		return used.GetBit(i);
	}

	ref T Allocate(out Int32 i, out Boolean unused)
	{
		i = full.IndexOfBitZero();

		if (i >= layout.FieldLength)
		{
			throw new BitArrayFullException();
		}

		ref var item = ref content[i];

		unused = !GetUsedBit(i);

		if (unused)
		{
			SetUsedBit(i, true);

			item = default;
		}

		return ref item;
	}

	public ref T AllocatePartially(out Int32 i, out Boolean unused)
	{
		return ref Allocate(out i, out unused);
	}

	public ref T AllocateFully(out Int32 i)
	{
		ref var item = ref Allocate(out i, out var unused);

		Debug.Assert(unused);

		SetFullBit(i, true);

		Validate();

		return ref item;
	}

	//public Boolean TryIndexOfUnfull(out Int32 i)
	//{
	//	i = full.IndexOfBitZero();

	//	return i < content.Length;
	//}

	public Boolean IsFull => full.IndexOfBitZero() >= layout.FieldLength;

	public Boolean IsEmpty => used.IsAllZero();

	public ref T At(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		return ref content[i];
	}
}
