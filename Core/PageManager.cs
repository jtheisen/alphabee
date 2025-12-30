namespace AlphaBee;

// TODO: Durability for the header.

public ref struct PageManager
{
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

		Debug.Assert(Constants.UnaccountedHeaderPages < 63);

		// All unaccounted header page and the index root are marked as used.
		for (var i = 0; i < Constants.UnaccountedHeaderPages + 1; ++i)
		{
			indexRootPage.At(0).SetBit(i, true);
		}
	}

	public UInt64 AllocatePageOffset()
	{
		var rootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		var rootIndexPage = new BitFieldPage(rootPageSpan);

		var reserve = default(Int32);

		if (rootIndexPage.IsFull)
		{
			var newIndexPageOffset = AllocatePageOffsetAtEnd(storage);

			Debug.Assert(newIndexPageOffset == rootIndexPage.Layout.GetAddressSpaceSizeForDepth(header.IndexDepth));

			var newIndexPageDepth = header.IndexDepth + 1;

			rootPageSpan = storage.GetPageSpanAtOffset(newIndexPageOffset);

			rootPageSpan.Clear();

			rootIndexPage = new BitFieldPage(rootPageSpan);

			rootIndexPage.Init(PageType.PageIndex, newIndexPageDepth);
			ref var entry = ref rootIndexPage.AllocateFully(out var _);
			entry = header.IndexRootOffset;
			rootIndexPage.Validate();

			++reserve;

			header.IndexDepth = newIndexPageDepth;
			header.IndexRootOffset = newIndexPageOffset;
			header.NextPageOffset = newIndexPageOffset + Constants.PageSize;
		}

		var bitField = new BitField<Allocator>(new Allocator(storage, check: true));

		var pageOffset = bitField.Allocate(rootIndexPage, header.IndexDepth, reserve) * Constants.PageSize;

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

	struct Allocator : IPageAllocator
	{
		Storage storage;
		Boolean check;
		UInt64 lastAllocatedPageOffset;

		public Allocator(Storage storage, Boolean check)
		{
			this.storage = storage;
			this.check = check;
		}

		Span<Byte> IPageAllocator.GetPageSpanAtOffset(UInt64 offset)
			=> storage.GetPageSpanAtOffset(offset);

		UInt64 IPageAllocator.AllocatePageOffset()
			=> lastAllocatedPageOffset = AllocatePageOffsetAtEnd(storage);

		void IPageAllocator.AssertAllocatedPageIndex(UInt64 index)
		{
			if (!check) return;

			var lastAllocatedPageNo = lastAllocatedPageOffset / storage.PageSize;

			Debug.Assert(lastAllocatedPageNo == index + Constants.UnaccountedHeaderPages - 1);
		}

		Boolean IPageAllocator.IsPageManagerBitField => true;
	}
}

