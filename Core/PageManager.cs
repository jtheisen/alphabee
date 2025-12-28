using System.Reflection.PortableExecutable;

namespace AlphaBee;

public ref struct PageManager : IPageAllocator
{
	FieldPageLayout<UInt64> layout;

	Storage storage;

	public PageManager(Storage storage)
	{
		this.storage = storage;
	}

	public void Init()
	{
		ref var header = ref storage.Header;

		header.IndexDepth = 0;
		header.IndexRootOffset = layout.Size64;
		header.AddressSpaceEnd = header.IndexRootOffset + layout.Size64;

		var indexRootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		indexRootPageSpan.Clear();

		var indexRootPage = new BitFieldPage(indexRootPageSpan);

		indexRootPage.Init(PageType.PageIndex, 0);
		indexRootPage.SetUsedBit(0, true);
		indexRootPage.At(0).SetBit(0, true);
	}

	public UInt64 AllocatePageOffset()
	{
		ref var header = ref storage.Header;

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
			ref var entry = ref rootIndexPage.AllocateFully(out var _);
			entry = header.IndexRootOffset;
			rootIndexPage.Validate();

			header.AddressSpaceEnd = GetAddressSpaceSizeForDepth(newIndePageDepth);
			header.IndexDepth = newIndePageDepth;
			header.IndexRootOffset = newIndexPageOffset;
		}

		var bitField = new BitField<PageManager>(this);

		var pageOffset = bitField.Allocate(rootIndexPage, header.IndexDepth) * layout.Size64;

		if (pageOffset >= header.NextPageOffset)
		{
			header.NextPageOffset = pageOffset + Constants.PageSize;
		}

		return pageOffset;
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

	Span<Byte> IPageAllocator.GetPageSpanAtOffset(UInt64 offset)
		=> storage.GetPageSpanAtOffset(offset);

	UInt64 IPageAllocator.AllocatePageOffset()
	{
		ref var header = ref storage.Header;

		var nextPageOffset = header.NextPageOffset;

		header.NextPageOffset += Constants.PageSize;

		return nextPageOffset;
	}
}



