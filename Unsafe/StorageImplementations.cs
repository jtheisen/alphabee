using Microsoft.VisualBasic;
using System.Collections;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace AlphaBee;

public delegate StorageImplementation CreateStorageImplementation(Int32 pageSize);

public abstract class StorageImplementation : IDisposable
{
	protected abstract void Dispose();

	void IDisposable.Dispose() => Dispose();

	public abstract Span<Byte> GetPageAtOffset(UInt64 offset);

	public abstract StorageImplementation Increase();
}


public class NoSuchPageException : Exception
{

}

public class InMemoryTestStorageImplementation : StorageImplementation
{
	Int32 pageSize;

	Dictionary<UInt64, Byte[]> pages = new();

	Random random = new();

	public InMemoryTestStorageImplementation(Int32 pageSize)
	{
		this.pageSize = pageSize;
	}

	public override Span<Byte> GetPageAtOffset(UInt64 offset)
	{
		if (!pages.TryGetValue(offset, out var bytes))
		{
			pages.Add(offset, bytes = Enumerable.Range(0, pageSize).Select(b => (Byte)b).ToArray());
		}

		return bytes;
	}

	public override StorageImplementation Increase()
	{
		return this;
	}

	protected override void Dispose()
	{
	}
}

public unsafe class MemoryMappedFileStorageImplementation : StorageImplementation
{
	MemoryMappedFile mmf;
	MemoryMappedViewAccessor mmv;

	String name;
	UInt64 size;
	Int32 pageSize32;
	UInt64 pageSize;
	Byte* ptr;
	UInt64 lastPageOffset;

	public MemoryMappedFileStorageImplementation(String name, Int32 pageSize, UInt64? size = null)
	{
		Trace.Assert(pageSize < Int32.MaxValue);

		this.name = name;
		this.pageSize = (UInt64)pageSize;
		this.pageSize32 = pageSize;
		this.size = size ?? this.pageSize * 4;
		this.lastPageOffset = this.size - this.pageSize;
		mmf = MemoryMappedFile.CreateFromFile(name, FileMode.OpenOrCreate, null, (Int64)this.size);
		mmv = mmf.CreateViewAccessor();
		mmv.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
	}

	public override StorageImplementation Increase()
	{
		return new MemoryMappedFileStorageImplementation(name, pageSize32, size << 1);
	}

	public override Span<Byte> GetPageAtOffset(UInt64 offset)
	{
		if (offset > lastPageOffset)
		{
			throw new NoSuchPageException();
		}

		return new Span<Byte>(ptr + offset, pageSize32);
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
