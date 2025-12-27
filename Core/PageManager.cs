using BitFieldPage = AlphaBee.FieldPage<AlphaBee.FieldPageLayout<System.UInt64>>;

namespace AlphaBee;

public partial struct PageManager
{
	FieldPageLayout<UInt64> layout;

	Storage storage;

	public PageManager(Storage storage)
	{
		this.storage = storage;
	}

	public void Init()
	{
		ref var header = ref storage.GetHeader();

		header.IndexDepth = 0;
		header.IndexRootOffset = layout.Size64;
		header.AddressSpaceEnd = header.IndexRootOffset + layout.Size64;

		var indexRootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		indexRootPageSpan.Clear();

		var indexRootPage = new UInt64Page(indexRootPageSpan);

		indexRootPage.Init(PageType.PageIndex, 0);
		indexRootPage.SetUsedBit(0, true);
		indexRootPage.Get(0).SetBit(0, true);
	}

	public UInt64 AllocatePageOffset()
	{
		ref var header = ref storage.GetHeader();

		var rootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		var rootIndexPage = new BitFieldPage(rootPageSpan);

		if (rootIndexPage.IsFull)
		{
			var newIndexPageOffset = header.AddressSpaceEnd;

			var newIndePageDepth = header.IndexDepth + 1;

			rootPageSpan = storage.GetPageSpanAtOffset(newIndexPageOffset);

			rootPageSpan.Clear();

			rootIndexPage = new BitFieldPage(rootPageSpan);

			rootIndexPage.Init(PageType.PageIndex, newIndePageDepth);
			rootIndexPage.SetFullBit(0, true);
			rootIndexPage.SetUsedBit(0, true);
			rootIndexPage.Get(0) = header.IndexRootOffset;

			header.AddressSpaceEnd = GetAddressSpaceSizeForDepth(newIndePageDepth);
			header.IndexDepth = newIndePageDepth;
			header.IndexRootOffset = newIndexPageOffset;
		}

		var bitField = new BitField(storage);

		return bitField.Allocate(rootIndexPage, header.IndexDepth) * layout.Size64;
	}

	public UInt64 GetAddressSpaceSizeForDepth(Int32 depth)
	{
		var result = (UInt64)layout.ContentBitSize;

		for (var i = 0; i < depth; i++)
		{
			result *= (UInt64)layout.FieldLength;
		}

		return result * layout.Size64;
	}
}



