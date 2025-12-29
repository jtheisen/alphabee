namespace AlphaBee;

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
