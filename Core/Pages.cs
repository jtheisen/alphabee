using System.Diagnostics.Tracing;

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
				var word = page.Get(i);

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

[DebuggerDisplay("{ToString()}")]
public ref struct FieldPage<T, L>
	where T : unmanaged
	where L : struct, IFieldPageLayout
{
	L layout;

	PageHeader header;
	ref UInt64 used;
	ref UInt64 full;
	Span<T> content;

	public override String ToString()
	{
		return $"[{header.PageCharPair}|{used.ToBrailleString()}|{full.ToBrailleString()}]";
	}

	public FieldPage(Span<Byte> page)
	{
		var words = page.InterpretAs<UInt64>();
		content = page[layout.LeadSize..].InterpretAs<T>();
		header = new PageHeader(ref words[0]);
		used = ref words[1];
		full = ref words[2];
	}

	public void Init(PageType pageType, Int32 pageDepth)
	{
		header.Init(pageType, pageDepth);
		used = full = default;
	}

	public void Validate()
	{
		header.Validate();

		Debug.Assert((used | ~full) == UInt64.MaxValue);
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

	public ref T UseItem(Int32 i)
	{
		if (!GetUsedBit(i))
		{
			SetUsedBit(i, true);
			ClearItem(i);
		}

		return ref content[i];
	}

	public void ClearItem(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		content[i] = default;
	}

	public Boolean TryIndexOfUnfull(out Int32 i)
	{
		var found = full.TryIndexOfBitZero(out i);

		return found && i < content.Length;
	}

	public Boolean IsFull => full == layout.AllPattern;

	public ref T Get(Int32 i)
	{
		Debug.Assert(i < layout.FieldLength);

		return ref content[i];
	}
}
