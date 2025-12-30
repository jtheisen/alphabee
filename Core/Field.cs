namespace AlphaBee;

public ref struct Field<T, I, AllocatorT>
	where T : unmanaged
	where I : unmanaged
	where AllocatorT : struct, IPageAllocator, allows ref struct
{
	public static readonly WordPageLayout layout;

	AllocatorT allocator;
	Boolean filled;
	UInt64 factor;
	UInt64 index;

	public WordPageLayout Layout => layout;

	public Field(AllocatorT allocator)
	{
		this.allocator = allocator;
	}

	public ref T Get(UInt64 offset, Int32 depth, UInt64 i)
	{
		return ref GetCore(offset, depth, i);
	}

	ref T GetCore(UInt64 offset, Int32 depth, UInt64 i)
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

			var page = new FieldPage<T, I>(pageSpan);

			return ref GetFromLeaf(page, i);
		}
	}

	ref T GetFromBranch(IndexPage branch, Int32 depth, UInt64 i)
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

	ref T GetFromLeaf(FieldPage<T, I> leaf, UInt64 i64)
	{
		Debug.Assert(i64 < Int32.MaxValue);

		var i = (Int32)i64;

		return ref leaf.Use(i, out _);
	}

	public ref T Allocate(UInt64 offset, Int32 depth, out UInt64 i)
	{
		filled = false;
		index = 0;
		factor = 0;

		ref var result = ref AllocateCore(offset, depth, init: false);

		i = index;

		return ref result;
	}

	ref T AllocateWithHigherIndex(UInt64 previewRootPageOffset, Int32 depth)
	{
		var newIndexPageSpan = allocator.AllocatePageSpan();

		var newIndexPage = new IndexPage(newIndexPageSpan);

		newIndexPage.Init(PageType.Field, depth + 1);

		newIndexPage.AllocateFully(out _) = previewRootPageOffset;

		var newPageOffset = allocator.AllocatePageOffset();

		return ref AllocateCore(newPageOffset, depth + 1, init: true);
	}

	FieldPage<T2, I2> Foo<T2, I2>(UInt64 offset, Int32 depth, Boolean init)
		where T2 : unmanaged
		where I2 : unmanaged
	{
		var pageSpan = allocator.GetPageSpanAtOffset(offset);

		var page = default(FieldPageLayout<T2, I2>).Create(pageSpan);

		if (init)
		{
			page.Init(PageType.Field, depth);
		}

		page.Validate();

		return page;
	}

	ref T AllocateCore(UInt64 offset, Int32 depth, Boolean init)
	{
		if (depth > 0)
		{
			var pageSpan = allocator.GetPageSpanAtOffset(offset);

			var page = new IndexPage(pageSpan);

			if (init)
			{
				page.Init(PageType.Field, depth);
			}

			if (page.IsFull)
			{
				return ref AllocateWithHigherIndex(offset, depth);
			}

			page.Validate();
			page.ValidateWordPage(asBitFieldLeaf: false);

			return ref AllocateAtBranch(page, depth);
		}
		else
		{
			var pageSpan = allocator.GetPageSpanAtOffset(offset);

			var page = new FieldPage<T, I>(pageSpan);

			if (init)
			{
				page.Init(PageType.Field, depth);
			}

			if (page.IsFull)
			{
				return ref AllocateWithHigherIndex(offset, depth);
			}

			page.Validate();

			return ref AllocateAtLeaf(page);
		}
	}

	ref T AllocateAtBranch(IndexPage branch, Int32 depth)
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

	ref T AllocateAtLeaf(FieldPage<T, I> leaf)
	{
		ref var word = ref leaf.AllocateFully(out var i);

		index = i.ToUInt64();

		factor = layout.ContentBitSize.ToUInt64();

		return ref word;
	}

	static UInt64 GetSpaceSizeForDepth(Int32 depth)
	{
		var result = default(FieldPage<T, I>).Layout.FieldLength.ToUInt64();

		for (var i = 0; i < depth; i++)
		{
			result *= default(IndexPage).Layout.FieldLength.ToUInt64();
		}

		return result;
	}
}



