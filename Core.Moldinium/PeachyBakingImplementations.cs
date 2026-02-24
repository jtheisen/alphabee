using AlphaBee;
using Moldinium.Baking;

namespace Alphabee;

public abstract class AbstractPeachyContext
{
	public abstract T Get<T>(UInt64 offset) where T : unmanaged;

	public abstract void Set<T>(UInt64 offset, T value) where T : unmanaged;
}


public class PeachyTestContext : AbstractPeachyContext
{
	private readonly Byte[] data;

	public PeachyTestContext(Byte[] data)
	{
		this.data = data;
	}

	ref T At<T>(UInt64 offset) where T : unmanaged
		=> ref data.AsSpan()[(Int32)offset..].InterpretAs<T>()[0];

	public override T Get<T>(UInt64 offset) => At<T>(offset);

	public override void Set<T>(UInt64 offset, T value) => At<T>(offset) = value;
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
		return context.Get<T>(GetFieldAddress(offset));
	}

	public void Set<T>(Int32 offset, T value) where T : unmanaged
	{
		context.Set<T>(GetFieldAddress(offset), value);
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

