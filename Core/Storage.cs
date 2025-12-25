using System.Reactive.Disposables;

namespace AlphaBee;

public class Storage
{
	StorageImplementation implementation;

	SerialDisposable currentDisposable = new();

	public Storage(StorageImplementation implementation)
	{
		this.implementation = implementation;
		this.currentDisposable.Disposable = implementation;
	}

	void SetImplementation(StorageImplementation implementation)
	{
		this.implementation = implementation;
		this.currentDisposable.Disposable = implementation;
	}

	public Span<Byte> GetPageAtOffset(UInt64 offset)
	{
		try
		{
			return implementation.GetPageAtOffset(offset);
		}
		catch (NoSuchPageException)
		{
			SetImplementation(implementation.Increase());

			return implementation.GetPageAtOffset(offset);
		}
	}

	public void Dispose()
	{
		currentDisposable.Dispose();
	}
}
