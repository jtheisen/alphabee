using AlphaBee;
using Moldinium.Baking;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Alphabee;

public abstract class AbstractPeachyContext
{
	public abstract T GetValue<T>(UInt64 offset) where T : unmanaged;

	public abstract void SetValue<T>(UInt64 offset, T value) where T : unmanaged;

	public abstract T GetObject<T>(UInt64 offset) where T : class;

	public abstract T SetObject<T>(UInt64 offset, Object value) where T : class;
}

public abstract class AbstractTestStorage
{
	public abstract Span<T> GetSpan<T>(UInt64 offset, Int32 length) where T : unmanaged;

	public abstract Span<T> AllocateSpan<T>(out UInt64 address, Int32 length) where T : unmanaged;

	public T GetValue<T>(UInt64 offset) where T : unmanaged => GetSpan<T>(offset, 1)[0];

	public void SetValue<T>(UInt64 offset, T value) where T : unmanaged => GetSpan<T>(offset, 1)[0] = value;

	public ref T AllocateValue<T>(out UInt64 address) where T : unmanaged => ref AllocateSpan<T>(out address, 1)[0];
}

public class TestStorage : AbstractTestStorage
{
	private UInt64 position = 0;

	private Byte[] data = new Byte[4];

	Span<T> GetSpanCore<T>(UInt64 offset, Int32 length, Boolean extend = false) where T : unmanaged
	{
		Trace.Assert(offset < Int32.MaxValue);

		var max = (Int32)offset + Unsafe.SizeOf<T>() * length;

		while (max >= data.Length)
		{
			if (!extend)
			{
				throw new Exception("Accessing invalid address space");
			}

			Double();
		}

		return data.AsSpan()[(Int32)offset..].InterpretAs<T>();
	}

	void Double()
	{
		var copy = new Byte[data.Length * 2];
		data.CopyTo(copy, 0);
		data = copy;
	}

	public override Span<T> AllocateSpan<T>(out UInt64 address, Int32 length)
	{
		address = position;

		position += (UInt64)Unsafe.SizeOf<T>();

		return GetSpanCore<T>(address, length, extend: true);
	}

	public override Span<T> GetSpan<T>(UInt64 offset, Int32 length)
	{
		return GetSpanCore<T>(offset, length);
	}
}

public class PeachyContext : AbstractPeachyContext
{
	private readonly AbstractTestStorage storage;

	public PeachyContext(AbstractTestStorage storage)
	{
		this.storage = storage;
	}


	public override T GetValue<T>(UInt64 offset) => storage.GetValue<T>(offset);

	public override void SetValue<T>(UInt64 offset, T value) => storage.SetValue<T>(offset, value);

	public override T GetObject<T>(UInt64 offset)
	{
		
	}

	public override T SetObject<T>(UInt64 offset, Object value)
	{
		
	}
}

public abstract class AbstractTypeHandler
{
	public abstract Object Get(AbstractTestStorage storage, UInt64 offset);

	public abstract void Set(AbstractTestStorage storage, Object untyped);
}

public class StringTypeHandler : AbstractTypeHandler
{
	public override Object Get(AbstractTestStorage storage, UInt64 offset)
	{
		
	}

	public override void Set(AbstractTestStorage storage, Object value)
	{
		
	}
}

public interface IPeachyMixin
{
	UInt64 Address { get; set; }

	void Init(AbstractPeachyContext context);
}

[IgnoreForBaking]
public interface IPeachyInternalMixin : IPeachyMixin
{
	T Get<T>(Int32 offset) where T : unmanaged;

	void Set<T>(Int32 offset, T value) where T : unmanaged;
}

public struct ExamplePeachyMixin : IPeachyInternalMixin
{
	AbstractPeachyContext context;

	public void Init(AbstractPeachyContext context)
	{
		this.context = context;
	}

	public UInt64 Address { get; set; }

	UInt64 GetFieldAddress(Int32 offset) => Address + (UInt64)offset;

	public T Get<T>(Int32 offset) where T : unmanaged
	{
		return context.GetValue<T>(GetFieldAddress(offset));
	}

	public void Set<T>(Int32 offset, T value) where T : unmanaged
	{
		context.SetValue<T>(GetFieldAddress(offset), value);
	}
}

//public struct PeachyMixin<AccessorT> : IPeachyMixing
//	where AccessorT : IPeachyAccessor
//{
//	public UInt64 Address { get; set; }

//	public AccessorT Accessor { get; set; }

//	ref T GetCore<T>(Int32 offset) where T : unmanaged => ref Accessor.Get<T>(Address + (UInt32)offset);

//	public T Get<T>(Int32 offset) where T : unmanaged => GetCore<T>(offset);

//	public void Set<T>(Int32 offset, T value) where T : unmanaged => GetCore<T>(offset) = value;
//}

public interface IPeachyPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Mixin : IPeachyMixin
{
	Value Get(ref Mixin mixin, Int32 offset);

	void Set(ref Mixin mixin, Int32 offset, Value value);
}

public struct PeachyPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPeachyPropertyImplementation<Value, ExamplePeachyMixin>
	where Value : unmanaged
{
	public Value Get(ref ExamplePeachyMixin mixin, Int32 offset) => mixin.Get<Value>(offset);

	public void Set(ref ExamplePeachyMixin mixin, Int32 offset, Value value) => mixin.Set(offset, value);
}

