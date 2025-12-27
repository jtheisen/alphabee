using System.Reactive.Disposables;

namespace AlphaBee;

public class Storage : IDisposable
{
	StorageImplementation implementation;

	SerialDisposable currentDisposable = new();

	PageManager pageManager;

	public Int32 PageSizeLog2 => Constants.PageSizeLog2;
	public UInt64 PageSize => Constants.PageSize;

	public Storage(CreateStorageImplementation createImplementation, Boolean create)
	{
		implementation = createImplementation(PageSize);
		currentDisposable.Disposable = implementation;

		pageManager = new PageManager(this);

		if (create)
		{
			Init();
		}
	}

	public HeaderPage HeaderPage => new HeaderPage(GetPageSpanAtOffset(0));

	public ref HeaderPageLayout GetHeader() => ref HeaderPage.header;

	void Init()
	{
		pageManager.Init();
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
		return pageManager.AllocatePageOffset();
	}
}



