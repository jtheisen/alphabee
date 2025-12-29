namespace AlphaBee;

// TODO: Durability for the header.

public ref struct PageManager
{
	BitFieldPageLayout layout;

	ref HeaderPageLayout header;

	Storage storage;

	public PageManager(Storage storage)
	{
		this.storage = storage;

		this.header = ref storage.Header;
	}

	public void Init()
	{
		header.IndexDepth = 0;
		header.IndexRootOffset = Constants.PageSize;
		header.NextPageOffset = header.IndexRootOffset + Constants.PageSize;

		var indexRootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		indexRootPageSpan.Clear();

		var indexRootPage = new BitFieldPage(indexRootPageSpan);

		indexRootPage.Init(PageType.PageIndex, 0);
		indexRootPage.SetUsedBit(0, true);
		indexRootPage.At(0).SetBit(0, true);
	}

	public UInt64 AllocatePageOffset()
	{
		var rootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		var rootIndexPage = new BitFieldPage(rootPageSpan);

		if (rootIndexPage.IsFull)
		{
			var newIndexPageOffset = AllocatePageOffsetAtEnd(storage);

			Debug.Assert(newIndexPageOffset == GetAddressSpaceSizeForDepth(header.IndexDepth));

			var newIndePageDepth = header.IndexDepth + 1;

			rootPageSpan = storage.GetPageSpanAtOffset(newIndexPageOffset);

			rootPageSpan.Clear();

			rootIndexPage = new BitFieldPage(rootPageSpan);

			rootIndexPage.Init(PageType.PageIndex, newIndePageDepth);
			ref var entry = ref rootIndexPage.AllocateFully(out var _);
			entry = header.IndexRootOffset;
			rootIndexPage.Validate();

			header.IndexDepth = newIndePageDepth;
			header.IndexRootOffset = newIndexPageOffset;
		}

		var bitField = new BitField<Allocator>(new Allocator(storage));

		var pageOffset = bitField.Allocate(rootIndexPage, header.IndexDepth) * Constants.PageSize;

		if (pageOffset >= header.NextPageOffset)
		{
			header.NextPageOffset = pageOffset + Constants.PageSize;
		}

		return pageOffset;
	}

	static UInt64 AllocatePageOffsetAtEnd(Storage storage)
	{
		ref var header = ref storage.Header;

		var nextPageOffset = header.NextPageOffset;

		header.NextPageOffset += Constants.PageSize;

		return nextPageOffset;
	}

	public UInt64 GetAddressSpaceSizeForDepth(Int32 depth)
	{
		var result = (UInt64)layout.ContentBitSize;

		for (var i = 0; i < depth; i++)
		{
			result *= (UInt64)layout.FieldLength;
		}

		return result * layout.PageSize.ToUInt64();
	}

	struct Allocator : IPageAllocator
	{
		Storage storage;

		public Allocator(Storage storage)
		{
			this.storage = storage;
		}

		Span<Byte> IPageAllocator.GetPageSpanAtOffset(UInt64 offset)
			=> storage.GetPageSpanAtOffset(offset);

		UInt64 IPageAllocator.AllocatePageOffset()
			=> AllocatePageOffsetAtEnd(storage);

		Boolean IPageAllocator.IsPageManagerBitField => true;
	}
}

