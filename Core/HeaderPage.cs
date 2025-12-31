namespace AlphaBee;

public struct HeaderCore
{
	public TreeRoot indexRoot;
	public UInt64 nextPageOffset;

	public TreeRoot fieldRootsRoot;
}

public ref struct HeaderPage
{
	ref HeaderCore header;

	public ref HeaderCore HeaderCore => ref header;

	public ref UInt64 IndexRootOffset => ref HeaderCore.indexRoot.offset;
	public ref Int32 IndexDepth => ref HeaderCore.indexRoot.depth;
	public ref UInt64 NextPageOffset => ref HeaderCore.nextPageOffset;

	public HeaderPage(Span<Byte> page)
	{
		header = ref page.InterpretAs<HeaderCore>()[0];
	}
}
