namespace AlphaBee;

public class BitArrayFullException : Exception { }

public interface IPageAllocator
{
	Boolean IsPageManagerBitField { get; }

	Span<Byte> GetPageSpanAtOffset(UInt64 offset);

	UInt64 AllocatePageOffset();
}

public ref struct BitField<AllocatorT>
	where AllocatorT : struct, IPageAllocator, allows ref struct
{
	BitFieldPageLayout layout;
	AllocatorT allocator;
	Boolean filled;
	Int32 reserve;
	UInt64 factor;

	public BitField(AllocatorT allocator)
	{
		this.allocator = allocator;
	}

	public UInt64 Allocate(BitFieldPage page, Int32 depth)
	{
		filled = false;
		reserve = 0;
		factor = 0;

		var index = AllocateCore(page, depth);

		Debug.Assert(reserve == 0);

		return index;
	}

	UInt64 AllocateCore(BitFieldPage page, Int32 depth)
	{
		Debug.Assert(!page.IsFull);

		page.Validate();
		page.ValidateFieldPage(asBitFieldLeaf: depth == 0);

		var pageNo = depth > 0 ? AllocateAtBranch(page, depth) : AllocateAtLeaf(page);

		page.Validate();
		page.ValidateFieldPage(asBitFieldLeaf: depth == 0);

		return pageNo;
	}

	UInt64 AllocateAtBranch(BitFieldPage branch, Int32 depth)
	{
		ref var childPageOffset = ref branch.AllocatePartially(out var i, out var unused);

		if (unused)
		{
			// If this is the page manager bit field, we never "unuse" branch
			// entries, so it is guaranteed that this is the end of the used address space.
			childPageOffset = allocator.AllocatePageOffset();

			if (allocator.IsPageManagerBitField)
			{
				// When allocating the page we're asked to, we first need to
				// skip the index pages allocated previously this way.
				++reserve;
			}
		}

		var childPageSpan = allocator.GetPageSpanAtOffset(childPageOffset);

		var childPage = new BitFieldPage(childPageSpan);

		if (unused)
		{
			childPage.Init(PageType.PageIndex, depth);
		}

		var childIndex = AllocateCore(childPage, depth - 1);

		if (filled)
		{
			branch.SetFullBit(i, true);

			filled = branch.IsFull;
		}

		var ownIndex = (UInt64)i;

		var result = ownIndex * factor + childIndex;

		factor *= layout.FieldLength.ToUInt64();

		return result;
	}

	UInt64 AllocateAtLeafCore(BitFieldPage leaf)
	{
		ref var word = ref leaf.AllocatePartially(out var i, out _);

		var j = word.Allocate();

		word.SetBit(j, true);

		if (word.IsAllOne())
		{
			leaf.SetFullBit(i, true);

			filled = leaf.IsFull;
		}

		factor = layout.ContentBitSize.ToUInt64();

		return (((UInt64)i) << 6) + (UInt64)j;
	}

	UInt64 AllocateAtLeaf(BitFieldPage leaf)
	{
		if (allocator.IsPageManagerBitField && reserve > 0)
		{
			// We come from new index pages, therefore we also must have
			// a new leaf. We need to mark the index page entries in the
			// new leaf.

			Debug.Assert(leaf.IsEmpty);

			for (; reserve > 0; --reserve)
			{
				AllocateAtLeafCore(leaf);
			}
		}

		factor = layout.ContentBitSize.ToUInt64();

		return AllocateAtLeafCore(leaf);
	}
}



