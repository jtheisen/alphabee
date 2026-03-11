using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AlphaBee;

public abstract class AbstractTestStorage
{
	public const Int32 FundamentalAlignment = 8;

	public abstract Boolean IsEmpty { get; }

	public abstract Int64 Position { get; }

	public abstract T GetValue<T>(Int64 offset) where T : unmanaged;

	public abstract void SetValue<T>(Int64 offset, T value) where T : unmanaged;

	public ObjectHeader GetHeader(Int64 address) => GetValue<ObjectHeader>(address);

	public abstract ref T GetObject<T>(Int64 address) where T : unmanaged;

	public abstract Span<T> GetArrayObject<T>(Int64 address) where T : unmanaged;

	public abstract void AllocateObject(ObjectHeader header, out Int64 address);

	public abstract ref T AllocateObject<T>(ObjectHeader header, out Int64 address) where T : unmanaged;

	public abstract void AllocateArrayObject<T>(ObjectHeader header, out Int64 address, out Span<T> content) where T : unmanaged;
}

public class TestStorage : AbstractTestStorage
{
	private Int64 reserved;

	private Int64 position = 0;

	private Byte[] data;

	public Byte[] Data => data;

	public TestStorage(Int32 reserved = FundamentalAlignment)
	{
		this.reserved = reserved;
		position = reserved;
		data = new Byte[Math.Max(reserved, 4)];
	}

	public override Boolean IsEmpty => position == reserved;

	public override Int64 Position => position;

	public override T GetValue<T>(Int64 offset) => GetSpanCore<T>(offset, 1)[0];

	public override void SetValue<T>(Int64 offset, T value) => GetSpanCore<T>(offset, 1)[0] = value;

	ref T GetCore<T>(Int64 address) where T : unmanaged
	{
		return ref GetSpanCore<T>(address, 1)[0];
	}

	Span<T> GetContentSpan<T>(Int64 address, ObjectHeader header) where T : unmanaged
	{
		Trace.Assert(header.UnitSize == Unsafe.SizeOf<T>());

		return GetSpanCore<T>(address + ObjectHeader.Size, header.ContentLength);
	}

	Span<T> GetSpanCore<T>(Int64 address, Int32 length) where T : unmanaged
	{
		Trace.Assert(address < Int32.MaxValue);

		var offset32 = (Int32)address;

		var max = address + Unsafe.SizeOf<T>() * length;

		if (max > position)
		{
			throw new Exception("Accessing invalid address space");
		}

		return data.AsSpan()[offset32..].InterpretAs<T>()[..length];
	}

	Int64 EnsureSpace(Int32 size)
	{
		var address = position;

		position += size.CeilToPowerOfTwoAlignment(FundamentalAlignment);

		while (position > data.Length)
		{
			Double();
		}

		return address;
	}

	void Double()
	{
		var copy = new Byte[data.Length * 2];
		data.CopyTo(copy, 0);
		data = copy;
	}

	public override ref T GetObject<T>(Int64 address)
	{
		ref var header = ref GetCore<ObjectHeader>(address);

		Trace.Assert(!header.IsArray);

		var content = GetContentSpan<T>(address, header);

		Trace.Assert(content.Length == 1);

		return ref content[0];
	}

	public override Span<T> GetArrayObject<T>(Int64 address)
	{
		ref var header = ref GetCore<ObjectHeader>(address);

		Trace.Assert(header.IsArray);

		var content = GetContentSpan<T>(address, header);

		return content;
	}

	public override void AllocateObject(ObjectHeader header, out Int64 address)
	{
		var size = header.EntireSize;

		address = EnsureSpace(size);

		SetValue(address, header);
	}

	public override ref T AllocateObject<T>(ObjectHeader header, out Int64 address)
	{
		Trace.Assert(!header.IsArray);

		AllocateArrayObjectInternal<T>(header, out address, out var span);

		return ref span[0];
	}

	public override void AllocateArrayObject<T>(ObjectHeader header, out Int64 address, out Span<T> content)
	{
		Trace.Assert(header.IsArray);

		AllocateArrayObjectInternal(header, out address, out content);
	}

	void AllocateArrayObjectInternal<T>(ObjectHeader header, out Int64 address, out Span<T> content) where T : unmanaged
	{
		AllocateObject(header, out address);

		content = GetContentSpan<T>(address, header);
	}
}
