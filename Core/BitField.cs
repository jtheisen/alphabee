using System.Net.Http.Headers;

namespace AlphaBee;

public class PageFullException : Exception { }

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
			ref var word = ref branch.UseItem(i);

			var childPageSpan = storage.GetPageSpanAtOffset(word);

			var childIndex = Allocate(new BitFieldPage(childPageSpan), depth - 1);

			if (filled)
			{
				branch.SetFullBit(i, true);

				filled = branch.IsFull;
			}

			branch.Validate();
			branch.ValidateFieldPage(asBitFieldLeaf: false);

			var ownIndex = (UInt64)i;

			return ownIndex + childIndex;
		}
		else
		{
			throw new PageFullException();
		}
	}

	public UInt64 AllocateAtLeaf(BitFieldPage leaf)
	{
		if (leaf.TryIndexOfUnfull(out var i))
		{
			ref var line = ref leaf.UseItem(i);

			if (line.TryIndexOfBitZero(out var j))
			{
				line.SetBit(j, true);

				if (line == UInt64.MaxValue)
				{
					leaf.SetFullBit(i, true);

					filled = leaf.IsFull;
				}

				leaf.Validate();
				leaf.ValidateFieldPage(asBitFieldLeaf: true);

				return (((UInt64)i) << 6) + (UInt64)j;
			}
			else
			{
				throw new PageFullException();
			}
		}
		else
		{
			throw new PageFullException();
		}
	}
}



