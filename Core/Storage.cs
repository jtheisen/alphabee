using System.Reactive.Disposables;

namespace AlphaBee;

public class Storage : IDisposable
{
	StorageImplementation implementation;

	SerialDisposable currentDisposable = new();

	public UInt64 PageSize => Constants.PageSize;

	public static Storage CreateTestStorage()
		=> new Storage(pageSize => new InMemoryTestStorageImplementation(pageSize), true);

	public static Storage CreateTestFileStorage()
		=> new Storage(pageSize => new MemoryMappedFileStorageImplementation("test.ab", pageSize), true);

	public Storage(CreateStorageImplementation createImplementation, Boolean create)
	{
		implementation = createImplementation(Constants.PageSize32);
		currentDisposable.Disposable = implementation;

		if (create)
		{
			Init();
		}
	}

	PageManager PageManager => new PageManager(this);

	public HeaderPage HeaderPage => new HeaderPage(GetPageSpanAtOffset(0));

	public ref HeaderPageLayout Header => ref HeaderPage.header;

	void Init()
	{
		PageManager.Init();
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

	public void DeallocatePageOffset(UInt64 offset)
		=> PageManager.DeallocatePageOffset(offset);

	public UInt64 AllocatePageOffset()
		=> PageManager.AllocatePageOffset();
}



