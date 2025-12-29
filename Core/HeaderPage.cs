namespace AlphaBee;

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
