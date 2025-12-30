namespace AlphaBee;

public ref struct FieldManager<T, I>
	where T : unmanaged
	where I : unmanaged
{
	ref HeaderPageLayout header;

	Storage storage;

	public FieldManager(Storage storage)
	{
		this.storage = storage;

		this.header = ref storage.Header;
	}

	public void Init()
	{
	}

	public void Deallocate(UInt64 offset)
	{
	}

	public ref T Allocate(out UInt64 offset)
	{
		var rootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset); // FIXME

		var rootIndexPage = new IndexPage(rootPageSpan);

		if (rootIndexPage.IsFull)
		{
			var newIndexPageOffset = storage.AllocatePageOffset();

			var newIndexPageDepth = header.IndexDepth + 1; // FIXME

			rootPageSpan = storage.GetPageSpanAtOffset(newIndexPageOffset);

			rootPageSpan.Clear();

			rootIndexPage = new IndexPage(rootPageSpan);

			rootIndexPage.Init(PageType.Page, newIndexPageDepth);
			ref var entry = ref rootIndexPage.AllocateFully(out var _);
			entry = header.IndexRootOffset;
			rootIndexPage.Validate();

			header.IndexDepth = newIndexPageDepth;
			header.IndexRootOffset = newIndexPageOffset;
			header.NextPageOffset = newIndexPageOffset + Constants.PageSize;
		}

		//var bitField = new BitField<ExtendingAllocator>(new ExtendingAllocator(storage, check: true));

		//var pageOffset = bitField.Allocate(rootIndexPage, header.IndexDepth, reserve) * Constants.PageSize;

		//if (pageOffset >= header.NextPageOffset)
		//{
		//	header.NextPageOffset = pageOffset + Constants.PageSize;
		//}

		//return pageOffset;

		throw new NotImplementedException(); // FIXME
	}
}

