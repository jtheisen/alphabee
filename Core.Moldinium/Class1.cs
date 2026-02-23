using Moldinium.Baking;

namespace Alphabee;

public interface IPeachyAccessor
{
	ref T Get<T>(UInt64 address) where T : unmanaged;
}

public interface IPeachyMixing
{
	UInt64 Address { get; set; }

	T Get<T>(Int32 offset) where T : unmanaged;

	void Set<T>(Int32 offset, T value) where T : unmanaged;
}

public struct ExamplePeachyMixin : IPeachyMixing
{
	public UInt64 Address { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public T Get<T>(Int32 offset) where T : unmanaged
	{
		throw new NotImplementedException();
	}

	public void Set<T>(Int32 offset, T value) where T : unmanaged
	{
		throw new NotImplementedException();
	}
}

public struct PeachyMixin<AccessorT> : IPeachyMixing
	where AccessorT : IPeachyAccessor
{
	public UInt64 Address { get; set; }

	public AccessorT Accessor { get; set; }

	ref T GetCore<T>(Int32 offset) where T : unmanaged => ref Accessor.Get<T>(Address + (UInt32)offset);

	public T Get<T>(Int32 offset) where T : unmanaged => GetCore<T>(offset);

	public void Set<T>(Int32 offset, T value) where T : unmanaged => GetCore<T>(offset) = value;
}

public interface IPeachyPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Mixin : IPeachyMixing
{
	Value Get(Mixin mixin, Int32 offset);

	void Set(Mixin mixin, Int32 offset, Value value);
}

public struct PeachyPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPeachyPropertyImplementation<Value, ExamplePeachyMixin>
	where Value : unmanaged
{
	public Value Get(ExamplePeachyMixin mixin, Int32 offset) => mixin.Get<Value>(offset);

	public void Set(ExamplePeachyMixin mixin, Int32 offset, Value value) => mixin.Set(offset, value);
}

