using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AlphaBee;

public abstract class AbstractPeachyContext
{
	public abstract T GetValue<T>(Int64 offset) where T : unmanaged;

	public abstract void SetValue<T>(Int64 offset, T value) where T : unmanaged;

	public abstract T? GetObject<T>(Int64 offset) where T : class;

	public abstract void SetObject<T>(Int64 offset, T? value) where T : class;
}

public abstract class AbstractTestStorage
{
	public abstract Span<T> GetSpan<T>(Int64 offset, Int32 length) where T : unmanaged;

	public abstract Span<T> AllocateSpan<T>(out Int64 address, Int32 length) where T : unmanaged;

	public T GetValue<T>(Int64 offset) where T : unmanaged => GetSpan<T>(offset, 1)[0];

	public void SetValue<T>(Int64 offset, T value) where T : unmanaged => GetSpan<T>(offset, 1)[0] = value;

	public ref T AllocateValue<T>(out Int64 address) where T : unmanaged => ref AllocateSpan<T>(out address, 1)[0];

	public ref ObjectHeader GetObject(Int64 address, out Span<Byte> content)
	{
		var headerSize = Unsafe.SizeOf<ObjectHeader>();

		ref var header = ref GetSpan<ObjectHeader>(address, 1)[0];

		content = GetSpan<Byte>(address + headerSize, header.size);

		return ref header;
	}

	public void AllocateObject(ObjectHeader header, out Int64 address, out Span<Byte> content)
	{
		var headerSize = Unsafe.SizeOf<ObjectHeader>();

		var span = AllocateSpan<Byte>(out address, headerSize + header.size);

		span.InterpretAs<ObjectHeader>()[0] = header;

		content = span[headerSize..];
	}
}

public class TestStorage : AbstractTestStorage
{
	private Int64 position = 0;

	private Byte[] data;

	public Byte[] Data => data;

	public TestStorage(Int32 reserved = 0)
	{
		position = reserved;
		data = new Byte[Math.Max(reserved, 4)];
	}

	Span<T> GetSpanCore<T>(Int64 offset, Int32 length, Boolean extend = false) where T : unmanaged
	{
		Trace.Assert(offset < Int32.MaxValue);

		var offset32 = (Int32)offset;

		var max = offset + Unsafe.SizeOf<T>() * length;

		if (!extend && max > position)
		{
			throw new Exception("Accessing invalid address space");
		}

		while (max > data.Length)
		{
			Double();
		}

		return data.AsSpan()[offset32..].InterpretAs<T>()[..length];
	}

	void Double()
	{
		var copy = new Byte[data.Length * 2];
		data.CopyTo(copy, 0);
		data = copy;
	}

	public override Span<T> AllocateSpan<T>(out Int64 referenceAddress, Int32 length)
	{
		referenceAddress = position;

		position += Unsafe.SizeOf<T>() * length;

		return GetSpanCore<T>(referenceAddress, length, extend: true);
	}

	public override Span<T> GetSpan<T>(Int64 referenceAddress, Int32 length)
	{
		return GetSpanCore<T>(referenceAddress, length);
	}
}
