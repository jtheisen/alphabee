using BitFieldPage = AlphaBee.FieldPage<AlphaBee.FieldPageLayout<System.UInt64>>;

namespace AlphaBee;

public struct BitField
{
	Storage storage;
	Boolean filled;

	public BitField(Storage storage)
	{
		this.storage = storage;
	}

	public UInt64 Allocate(BitFieldPage page, Int32 depth)
	{
		Debug.Assert(!page.IsFull);

		if (depth > 0)
		{
			return AllocateAtBranch(page, depth);
		}
		else
		{
			return AllocateAtLeaf(page);
		}
	}

	public UInt64 AllocateAtBranch(BitFieldPage branch, Int32 depth)
	{
		if (branch.TryIndexOfUnfull(out var i))
		{
			ref var word = ref branch.Get(i);

			var childPageSpan = storage.GetPageSpanAtOffset(word);

			var childIndex = Allocate(new BitFieldPage(childPageSpan), depth - 1);

			branch.SetUsedBit(i, true);

			if (filled)
			{
				branch.SetFullBit(i, true);

				filled = branch.IsFull;
			}

			var ownIndex = (UInt64)i;

			return ownIndex + childIndex;
		}
		else
		{
			throw new Exception();
		}
	}

	public UInt64 AllocateAtLeaf(BitFieldPage leaf)
	{
		if (leaf.TryIndexOfUnfull(out var i))
		{
			ref var word = ref leaf.Get(i);

			if (word.TryIndexOfBitZero(out var j))
			{
				word.SetBit(j, true);

				leaf.SetUsedBit(i, true);

				if (word == UInt64.MaxValue)
				{
					leaf.SetFullBit(i, true);

					filled = leaf.IsFull;
				}

				return (UInt64)((i << 6) + j);
			}
			else
			{
				throw new Exception();
			}
		}
		else
		{
			throw new Exception();
		}
	}
}



