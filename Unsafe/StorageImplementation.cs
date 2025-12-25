using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace AlphaBee;

public abstract class StorageImplementation : IDisposable
{
	protected abstract void Dispose();

	void IDisposable.Dispose() => Dispose();

	public abstract Span<Byte> GetPageAtOffset(UInt64 offset);
}

public class Storage : StorageImplementation
{
	private readonly String name;

	MemoryMappedFileStorageImplementation current;

	public Storage(String name)
	{
		this.name = name;
	}

	public override Span<Byte> GetPageAtOffset(UInt64 offset)
	{
		return current.GetPageAtOffset(offset);
	}

	protected override void Dispose()
	{
		throw new NotImplementedException();
	}
}

public unsafe class MemoryMappedFileStorageImplementation : StorageImplementation
{
	const Int32 PageSizeLog2 = 12;
	const Int32 PageSize = 1 << PageSizeLog2;

	MemoryMappedFile mmf;
	MemoryMappedViewAccessor mmv;

	Byte* ptr;
	UInt64 size;
	UInt64 lastPageOffset;

	public MemoryMappedFileStorageImplementation(String name, UInt64 size)
	{
		this.size = size;
		this.lastPageOffset = size - PageSize;
		mmf = MemoryMappedFile.CreateFromFile(name, FileMode.OpenOrCreate, null, (Int64)this.size);
		mmv = mmf.CreateViewAccessor();
		mmv.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
	}

	public override Span<Byte> GetPageAtOffset(UInt64 offset)
	{
		Debug.Assert(offset <= lastPageOffset);

		return new Span<Byte>(ptr + offset, PageSize);
	}

	protected override void Dispose()
	{
		if (ptr != null)
		{
			mmv.SafeMemoryMappedViewHandle.ReleasePointer();
			ptr = null;
		}
		if (mmv != null)
		{
			mmv.Flush();
			mmv.Dispose();
			mmv = null!;
		}
		if (mmf != null)
		{
			mmf.Dispose();
			mmf = null!;
		}
	}
}
