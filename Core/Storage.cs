using System.Reactive.Disposables;

namespace AlphaBee;

public class Storage
{
	public const Int32 PageSizeLog2 = 12;
	public const Int32 PageSize = 1 << PageSizeLog2;

	StorageImplementation implementation;

	SerialDisposable currentDisposable = new();

	public Storage(CreateStorageImplementation createImplementation, Boolean create)
	{
		implementation = createImplementation(PageSize);
		currentDisposable.Disposable = implementation;

		if (create)
		{
			Init();
		}
	}

	public HeaderPage HeaderPage => new HeaderPage(GetPageSpanAtOffset(0));

	public ref HeaderPageLayout GetHeader() => ref HeaderPage.header;

	void Init()
	{
		ref var header = ref GetHeader();

		header.IndexDepth = 0;
		header.IndexRootOffset = 1 << PageSizeLog2;
		header.AddressSpaceEnd = header.IndexRootOffset + PageSize;
	}

	void UpdateImplementation(StorageImplementation implementation)
	{
		this.implementation = implementation;
		currentDisposable.Disposable = implementation;
	}

	public Span<Byte> GetPageSpanAtOffset(UInt64 offset)
	{
		try
		{
			return implementation.GetPageAtOffset(offset);
		}
		catch (NoSuchPageException)
		{
			UpdateImplementation(implementation.Increase());

			return implementation.GetPageAtOffset(offset);
		}
	}

	public void Dispose()
	{
		currentDisposable.Dispose();
	}

	public UInt64 AllocatePageOffset()
	{
		return AllocationHelper.Allocate(this) << PageSizeLog2;
	}
}

struct AllocationHelper
{
	Storage storage;
	Boolean filled;

	public static UInt64 Allocate(Storage storage)
	{
		ref var header = ref storage.GetHeader();

		var rootPageSpan = storage.GetPageSpanAtOffset(header.IndexRootOffset);

		var rootIndexPage = new UInt64Page(rootPageSpan);

		if (rootIndexPage.IsFull)
		{
			var newIndexPageOffset = header.AddressSpaceEnd;

			rootIndexPage = new UInt64Page(storage.GetPageSpanAtOffset(newIndexPageOffset));

			rootIndexPage.Clear();
			rootIndexPage.SetUsedBit(0, true);
			rootIndexPage.Get(0) = header.IndexRootOffset;

			header.AddressSpaceEnd *= UInt64Page.ContentLength + 1;
			header.IndexDepth = header.IndexDepth + 1;
			header.IndexRootOffset = newIndexPageOffset;
		}

		var helper = new AllocationHelper(storage);

		return helper.Allocate(rootIndexPage, header.IndexDepth);
	}

	AllocationHelper(Storage storage)
	{
		this.storage = storage;
	}

	public UInt64 Allocate(UInt64Page page, Int32 depth)
	{
		if (depth > 0)
		{
			return AllocateAtBranch(page, depth);
		}
		else
		{
			return AllocateAtLeaf(page);
		}
	}

	public UInt64 AllocateAtBranch(UInt64Page branch, Int32 depth)
	{
		if (branch.TryIndexOfUnfull(out var i))
		{
			ref var word = ref branch.Get(i);

			var childPageSpan = storage.GetPageSpanAtOffset(word);

			var childIndex = Allocate(new UInt64Page(childPageSpan), depth - 1);

			branch.SetUsedBit(i, true);

			if (filled)
			{
				branch.SetFullBit(i, true);

				filled = branch.IsFull;
			}

			var ownIndex = (UInt64)i * UInt64Page.ContentLength;

			return ownIndex + childIndex;
		}
		else
		{
			throw new Exception();
		}
	}

	public UInt64 AllocateAtLeaf(UInt64Page leaf)
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



