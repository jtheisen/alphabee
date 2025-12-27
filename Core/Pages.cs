namespace AlphaBee;

public interface IPageLayout
{
	Int32 Size { get; }
}

public interface IFieldPageLayout : IPageLayout
{
	Int32 LeadWords { get; }

	Int32 FieldLength { get; }

	UInt64 AllPattern { get; }
}

public enum PageType
{
	Unkown = '?',
	PageIndex = 'p'
}

public struct FieldPageLayout<T> : IFieldPageLayout
	where T : unmanaged
{
	const Int32 BitsPerByteLog2 = 3;
	const Int32 BitsPerByte = 1 << 3;
	const Int32 WordSizeLog2 = 3;
	const Int32 WordSize = 1 << 3;

	public Int32 SizeLog2 => 12;
	public Int32 Size => 1 << SizeLog2;

	public UInt64 Size64 => (UInt64)Size;

	public Int32 LeadWordsLog2 => 2;
	public Int32 LeadWords => 1 << LeadWordsLog2;

	public Int32 BitsPerWordLog2 => WordSizeLog2 + BitsPerByteLog2;

	public Int32 LeadSize => LeadWords * WordSize;

	public Int32 ContentSize => Size - LeadSize;

	public Int32 ContentBitSize => ContentSize * WordSize * BitsPerByte;

	public UInt64 AllPattern => UInt64.MaxValue >> LeadWordsLog2;

	public Int32 FieldLength => ContentSize / Unsafe.SizeOf<T>();
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

	public ref Byte PageTypeByte => ref data.GetByte(0);
	public ref Byte PageDepthByte => ref data.GetByte(1);

	public String PageCharPair => $"{PageTypeChar}{PageDepthChar}";

	public PageType PageTypeChar => (PageType)PageTypeByte;
	public Char PageDepthChar => PageDepthByte < 10 ? (Char)('0' + PageDepthByte) : '+';
}

[DebuggerDisplay("{ToString()}")]
public ref struct FieldPage<L>
	where L : struct, IFieldPageLayout
{
	L layout;

	PageHeader header;
	ref UInt64 used;
	ref UInt64 full;
	Span<UInt64> content;

	public override String ToString()
	{
		return $"{header.PageCharPair} {full:x}";
	}

	public FieldPage(Span<Byte> page)
	{
		var words = page.InterpretAs<UInt64>();
		content = words[layout.LeadWords..];
		header = new PageHeader(ref words[0]);
		used = ref words[1];
		full = ref words[2];
	}

	public void Init(PageType pageType, Int32 pageDepth)
	{
		header.Init(pageType, pageDepth);
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

	public Boolean IsFull => full == layout.AllPattern;

	public ref UInt64 Get(Int32 i)
	{
		return ref content[i];
	}
}
