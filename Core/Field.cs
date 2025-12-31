namespace AlphaBee;

public ref struct Field<ItemT, ItemIndexT, IndexIndexT, AllocatorT>(AllocatorT allocator)
	where ItemT : unmanaged
	where ItemIndexT : unmanaged, IBitArray
	where IndexIndexT : unmanaged, IBitArray
	where AllocatorT : struct, IPageAllocator, allows ref struct
{
	public static readonly IndexPageLayout layout;

	AllocatorT allocator = allocator;
	Boolean filled;
	UInt64 factor;
	UInt64 index;

	public IndexPageLayout Layout => layout;

	public ref ItemT Get(UInt64 i)
	{
		ref var root = ref allocator.Root;

		return ref GetCore(root.offset, root.depth, i);
	}

	ref ItemT GetCore(UInt64 offset, Int32 depth, UInt64 i)
	{
		if (depth > 0)
		{
			var pageSpan = allocator.GetPageSpanAtOffset(offset);

			var page = new IndexPage(pageSpan);

			return ref GetFromBranch(page, depth, i);
		}
		else
		{
			var pageSpan = allocator.GetPageSpanAtOffset(offset);

			var page = new FieldPage<ItemT, ItemIndexT>(pageSpan);

			return ref GetFromLeaf(page, i);
		}
	}

	ref ItemT GetFromBranch(IndexPage branch, Int32 depth, UInt64 i)
	{
		var size = GetSpaceSizeForDepth(depth);

		var i2 = i / size;
		var i1 = i % size;

		ref var childPageOffset = ref branch.Use(i2.ToInt32(), out var unused);

		if (unused)
		{
			childPageOffset = allocator.AllocatePageOffset();
		}

		return ref GetCore(childPageOffset, depth - 1, i1);
	}

	ref ItemT GetFromLeaf(FieldPage<ItemT, ItemIndexT> leaf, UInt64 i64)
	{
		Debug.Assert(i64 < Int32.MaxValue);

		var i = (Int32)i64;

		return ref leaf.Use(i, out _);
	}

	public ref ItemT Allocate(out UInt64 i)
	{
		filled = false;
		index = 0;
		factor = 0;

		ref var root = ref allocator.Root;

		ref var result = ref AllocateCore(root.offset, root.depth, init: false);

		i = index;

		return ref result;
	}

	ref ItemT AllocateWithHigherIndex(UInt64 previewRootPageOffset, Int32 depth)
	{
		ref var newRoot = ref allocator.Root;

		Debug.Assert(newRoot.offset == previewRootPageOffset);

		newRoot.offset = allocator.AllocatePageOffset();
		newRoot.depth = depth + 1;

		var newIndexPageSpan = allocator.GetPageSpanAtOffset(newRoot.offset);

		var newIndexPage = new IndexPage(newIndexPageSpan);

		newIndexPage.Init(PageType.Field, newRoot.depth);

		newIndexPage.AllocateFully(out _) = previewRootPageOffset;

		var newPageOffset = allocator.AllocatePageOffset();

		return ref AllocateCore(newPageOffset, newRoot.depth, init: true);
	}

	ref ItemT AllocateCore(UInt64 offset, Int32 depth, Boolean init)
	{
		if (depth > 0)
		{
			var page = GetChildPage<UInt64, IndexIndexT>(offset, depth, init);

			if (page.IsFull)
			{
				return ref AllocateWithHigherIndex(offset, depth);
			}

			return ref AllocateAtBranch(page, depth);
		}
		else
		{
			var page = GetChildPage<ItemT, ItemIndexT>(offset, depth, init);

			if (page.IsFull)
			{
				return ref AllocateWithHigherIndex(offset, depth);
			}

			return ref AllocateAtLeaf(page);
		}
	}

	ref ItemT AllocateAtBranch(FieldPage<UInt64, IndexIndexT> branch, Int32 depth)
	{
		ref var childPageOffset = ref branch.AllocatePartially(out var i, out var unused);

		var ownIndex = (UInt64)i;

		if (unused)
		{
			childPageOffset = allocator.AllocatePageOffset();
		}

		ref var result = ref AllocateCore(childPageOffset, depth - 1, unused);

		if (filled)
		{
			branch.SetFullBit(i, true);

			filled = branch.IsFull;
		}

		index += ownIndex * factor;

		factor *= layout.FieldLength.ToUInt64();

		return ref result;
	}

	ref ItemT AllocateAtLeaf(FieldPage<ItemT, ItemIndexT> leaf)
	{
		ref var word = ref leaf.AllocateFully(out var i);

		index = i.ToUInt64();

		factor = layout.ContentBitSize.ToUInt64();

		return ref word;
	}

	FieldPage<T, I> GetChildPage<T, I>(UInt64 offset, Int32 depth, Boolean init)
		where T : unmanaged
		where I : unmanaged
	{
		var pageSpan = allocator.GetPageSpanAtOffset(offset);

		var page = default(FieldPageLayout<T, I>).Create(pageSpan);

		if (init)
		{
			page.Init(PageType.Field, depth);
		}

		page.Validate();

		return page;
	}

	static UInt64 GetSpaceSizeForDepth(Int32 depth)
	{
		var result = default(FieldPage<ItemT, ItemIndexT>).Layout.FieldLength.ToUInt64();

		for (var i = 0; i < depth; i++)
		{
			result *= default(IndexPage).Layout.FieldLength.ToUInt64();
		}

		return result;
	}
}



