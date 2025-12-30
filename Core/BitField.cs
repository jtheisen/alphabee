using System;

namespace AlphaBee;

public class BitArrayFullException : Exception { }

public interface IPageAllocator
{
	Boolean IsPageManagerBitField { get; }

	Span<Byte> GetPageSpanAtOffset(UInt64 offset);

	UInt64 AllocatePageOffset();

	Span<Byte> AllocatePageSpan() => GetPageSpanAtOffset(AllocatePageOffset());

	void AssertAllocatedPageIndex(UInt64 index);
}

public ref struct BitField<AllocatorT>
	where AllocatorT : struct, IPageAllocator, allows ref struct
{
	public static readonly IndexPageLayout layout;

	AllocatorT allocator;
	Boolean filled;
	Int32 reserve;
	UInt64 factor;

#if DEBUG
	UInt64 sizeForDepth;
	UInt64 index;
#endif

	public IndexPageLayout Layout => layout;

	public BitField(AllocatorT allocator)
	{
		this.allocator = allocator;
	}

	public Boolean Get(IndexPage page, Int32 depth, UInt64 i)
	{
		ref var word = ref GetCore(page, depth, i, out var i0);

		return word.GetBit(i0);
	}

	public void Set(IndexPage page, Int32 depth, UInt64 i, Boolean value)
	{
		ref var word = ref GetCore(page, depth, i, out var i0);

		word.SetBit(i0, value);
	}

	public Boolean GetAndSet(IndexPage page, Int32 depth, UInt64 i, Boolean value)
	{
		ref var word = ref GetCore(page, depth, i, out var i0);

		var result = word.GetBit(i0);

		word.SetBit(i0, value);

		return result;
	}

	public void AssertOneAndSetZero(IndexPage page, Int32 depth, UInt64 i)
	{
		ref var word = ref GetCore(page, depth, i, out var i0);

		Debug.Assert(word.GetBit(i0));

		word.SetBit(i0, false);
	}

	ref UInt64 GetCore(IndexPage page, Int32 depth, UInt64 i, out Int32 i0)
	{
		if (depth > 0)
		{
			return ref GetFromBranch(page, depth, i, out i0);
		}
		else
		{
			return ref GetFromLeaf(page, i, out i0);
		}
	}

	ref UInt64 GetFromBranch(IndexPage branch, Int32 depth, UInt64 i, out Int32 i0)
	{
		var size = layout.GetBitSpaceSizeForDepth(depth);

		var i2 = i / size;
		var i1 = i % size;

		ref var childPageOffset = ref branch.Use(i2.ToInt32(), out var unused);

		if (unused)
		{
			childPageOffset = allocator.AllocatePageOffset();
		}

		var childPageSpan = allocator.GetPageSpanAtOffset(childPageOffset);

		var childPage = new IndexPage(childPageSpan);


		return ref GetCore(childPage, depth - 1, i1, out i0);
	}

	ref UInt64 GetFromLeaf(IndexPage leaf, UInt64 i64, out Int32 i0)
	{
		Debug.Assert(i64 < Int32.MaxValue);

		var i = (Int32)i64;

		var i1 = i / layout.FieldLength;
		i0 = i % layout.FieldLength;

		ref var word = ref leaf.Use(i1, out _);

		return ref word;
	}

	public UInt64 Allocate(IndexPage page, Int32 depth, Int32 initialReserve)
	{
		filled = false;
		reserve = initialReserve;
		factor = 0;

#if DEBUG
		sizeForDepth = layout.GetBitSpaceSizeForDepth(depth);
		index = 0;
#endif

		var result = AllocateCore(page, depth);

		Debug.Assert(reserve == 0);

		return result;
	}

	UInt64 AllocateCore(IndexPage page, Int32 depth)
	{
		Debug.Assert(!page.IsFull);

		page.Validate();
		page.ValidateBitFieldPage(asBitFieldLeaf: depth == 0);

		var result = depth > 0 ? AllocateAtBranch(page, depth) : AllocateAtLeaf(page);

		page.Validate();
		page.ValidateBitFieldPage(asBitFieldLeaf: depth == 0);

		return result;
	}

	UInt64 AllocateAtBranch(IndexPage branch, Int32 depth)
	{
		ref var childPageOffset = ref branch.AllocatePartially(out var i, out var unused);

		var ownIndex = (UInt64)i;

#if DEBUG
		sizeForDepth /= layout.FieldLength.ToUInt64();
		index += ownIndex * sizeForDepth;
#endif

		if (unused)
		{
			// If this is the page manager bit field, we never "unuse" branch
			// entries, so it is guaranteed that this is the end of the used address space.
			childPageOffset = allocator.AllocatePageOffset();

#if DEBUG
			allocator.AssertAllocatedPageIndex(index + reserve.ToUInt64());
#endif

			if (allocator.IsPageManagerBitField)
			{
				// When allocating the page we're asked to, we first need to
				// skip the index pages allocated previously this way.
				++reserve;
			}
		}

		var childPageSpan = allocator.GetPageSpanAtOffset(childPageOffset);

		var childPage = new IndexPage(childPageSpan);

		if (unused)
		{
			childPage.Init(PageType.Page, depth - 1);
		}

		var childIndex = AllocateCore(childPage, depth - 1);

		if (filled)
		{
			branch.SetFullBit(i, true);

			filled = branch.IsFull;
		}

		var result = ownIndex * factor + childIndex;

		factor *= layout.FieldLength.ToUInt64();

		return result;
	}

	UInt64 AllocateAtLeafCore(IndexPage leaf)
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

	UInt64 AllocateAtLeaf(IndexPage leaf)
	{
#if DEBUG
		sizeForDepth /= layout.ContentBitSize.ToUInt64();
#endif

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



