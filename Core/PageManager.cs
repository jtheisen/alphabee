namespace AlphaBee;

// TODO: Durability for the header.

public ref struct PageManager
{
	Storage storage;

	public PageManager(Storage storage)
	{
		this.storage = storage;
	}

	public HeaderPage HeaderPage => storage.HeaderPage;

	public void Init()
	{
		HeaderPage.IndexDepth = 0;
		HeaderPage.IndexRootOffset = Constants.PageSize;
		HeaderPage.NextPageOffset = HeaderPage.IndexRootOffset + Constants.PageSize;

		var indexRootPageSpan = storage.GetPageSpanAtOffset(HeaderPage.IndexRootOffset);

		indexRootPageSpan.Clear();

		var indexRootPage = new FieldBranchPage(indexRootPageSpan);

		indexRootPage.Init(PageType.Page, 0);
		indexRootPage.SetUsedBit(0, true);

		Debug.Assert(Constants.UnaccountedHeaderPages < 63);

		// All unaccounted header page and the index root are marked as used.
		for (var i = 0; i < Constants.UnaccountedHeaderPages + 1; ++i)
		{
			indexRootPage.Use(0, out _).SetBit(i, true);
		}
	}

	public void DeallocatePageOffset(UInt64 offset)
	{
		var rootPageSpan = storage.GetPageSpanAtOffset(HeaderPage.IndexRootOffset);

		var rootIndexPage = new FieldBranchPage(rootPageSpan);

		var bitField = new BitField<NoAllocationsAllocator>(new NoAllocationsAllocator(storage));

		Debug.Assert(offset % storage.PageSize == 0);

		try
		{
			var wasSet = bitField.GetAndSet(rootIndexPage, HeaderPage.IndexDepth, offset / storage.PageSize, false);

			if (!wasSet)
			{
				throw new InternalErrorException("An attempt was made to deallocate an that was not allocated");
			}
		}
		catch(UnexpectedAllocationException)
		{
			throw new InternalErrorException("An attempt was made to deallocate an that can't have been allocated");
		}
	}

	public UInt64 AllocatePageOffset()
	{
		var rootPageSpan = storage.GetPageSpanAtOffset(HeaderPage.IndexRootOffset);

		var rootIndexPage = new FieldBranchPage(rootPageSpan);

		var reserve = default(Int32);

		if (rootIndexPage.IsFull)
		{
			var newIndexPageOffset = AllocatePageOffsetAtEnd(storage);

			Debug.Assert(newIndexPageOffset == rootIndexPage.Layout.GetAddressSpaceSizeForDepth(HeaderPage.IndexDepth));

			var newIndexPageDepth = HeaderPage.IndexDepth + 1;

			rootPageSpan = storage.GetPageSpanAtOffset(newIndexPageOffset);

			rootPageSpan.Clear();

			rootIndexPage = new FieldBranchPage(rootPageSpan);

			rootIndexPage.Init(PageType.Page, newIndexPageDepth);
			ref var entry = ref rootIndexPage.AllocateFully(out var _);
			entry = HeaderPage.IndexRootOffset;
			rootIndexPage.Validate();

			++reserve;

			HeaderPage.IndexDepth = newIndexPageDepth;
			HeaderPage.IndexRootOffset = newIndexPageOffset;
			HeaderPage.NextPageOffset = newIndexPageOffset + Constants.PageSize;
		}

		var bitField = new BitField<ExtendingAllocator>(new ExtendingAllocator(storage, check: true));

		var pageOffset = bitField.Allocate(rootIndexPage, HeaderPage.IndexDepth, reserve) * Constants.PageSize;

		if (pageOffset >= HeaderPage.NextPageOffset)
		{
			HeaderPage.NextPageOffset = pageOffset + Constants.PageSize;
		}

		return pageOffset;
	}

	static UInt64 AllocatePageOffsetAtEnd(Storage storage)
	{
		var headerPage = storage.HeaderPage;

		var nextPageOffset = headerPage.NextPageOffset;

		headerPage.NextPageOffset += Constants.PageSize;

		return nextPageOffset;
	}

	class UnexpectedAllocationException : Exception { }

	struct NoAllocationsAllocator : IPageAllocator
	{
		Storage storage;

		public NoAllocationsAllocator(Storage storage)
		{
			this.storage = storage;
		}

		Boolean IPageAllocator.IsPageManagerBitField
			=> throw new InternalErrorException();

		ref TreeRoot IPageAllocator.Root
			=> throw new NotImplementedException();

		UInt64 IPageAllocator.AllocatePageOffset()
			=> throw new UnexpectedAllocationException();

		void IPageAllocator.AssertAllocatedPageIndex(UInt64 index)
			=> throw new InternalErrorException();

		Span<Byte> IPageAllocator.GetPageSpanAtOffset(UInt64 offset)
		{
			Debug.Assert(offset < storage.HeaderPage.NextPageOffset);

			return storage.GetPageSpanAtOffset(offset);
		}
	}

	struct ExtendingAllocator : IPageAllocator
	{
		Storage storage;
		Boolean check;
		UInt64 lastAllocatedPageOffset;

		public ExtendingAllocator(Storage storage, Boolean check)
		{
			this.storage = storage;
			this.check = check;
		}

		ref TreeRoot IPageAllocator.Root
			=> throw new NotImplementedException();

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

