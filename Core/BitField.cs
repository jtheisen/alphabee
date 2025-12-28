namespace AlphaBee;

public class BitArrayFullException : Exception { }

public interface IPageAllocator
{
	Span<Byte> GetPageSpanAtOffset(UInt64 offset);

	Span<Byte> AllocatePageSpan();
}

public ref struct BitField<AllocatorT>
	where AllocatorT : struct, IPageAllocator, allows ref struct
{
	AllocatorT allocator;
	Boolean filled;

	public BitField(AllocatorT allocator)
	{
		this.allocator = allocator;
	}

	public UInt64 AllocateCore(BitFieldPage page, Int32 depth)
	{
		Debug.Assert(!page.IsFull);

		page.Validate();
		page.ValidateFieldPage(asBitFieldLeaf: depth == 0);

		var pageNo = depth > 0 ? AllocateAtBranch(page, depth) : AllocateAtLeaf(page);

		page.Validate();
		page.ValidateFieldPage(asBitFieldLeaf: depth == 0);

		return pageNo;
	}

	public UInt64 AllocateAtBranch(BitFieldPage branch, Int32 depth)
	{
		ref var childPageOffset = ref branch.AllocatePartially(out var i, out var unused);

		if (unused)
		{
			// Since we never "unuse" branch entries, it is guaranteed that
			// this is the end of the used address space.

			childPageOffset = 
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

		return ownIndex + childIndex;
	}

	public UInt64 AllocateAtLeaf(BitFieldPage leaf)
	{
		ref var word = ref leaf.AllocatePartially(out var i, out _);

		var j = word.Allocate();

		word.SetBit(j, true);

		if (word.IsAllOne())
		{
			leaf.SetFullBit(i, true);

			filled = leaf.IsFull;
		}

		return (((UInt64)i) << 6) + (UInt64)j;
	}
}



