using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

public interface IPeach
{
	AbstractPeachyContext Context { get; }

	Int64 Address { get; }

	void Init(AbstractPeachyContext context, Int64 address);
}

[IgnoreForBaking]
public interface IInternalPeachMixin : IPeach
{
	T GetValue<T>(Int32 offset) where T : unmanaged;

	void SetValue<T>(Int32 offset, T value) where T : unmanaged;

	T? GetObject<T>(Int32 offset) where T : class;

	void SetObject<T>(Int32 offset, T? value) where T : class;
}

public struct InternalPeachMixin : IInternalPeachMixin
{
	Int64 address;
	AbstractPeachyContext context;

	public Int64 Address => address;
	public AbstractPeachyContext Context => context;

	public void Init(AbstractPeachyContext context, Int64 address)
	{
		this.context = context;
		this.address = address;
	}

	Int64 GetFieldAddress(Int32 offset) => address + offset;

	public T GetValue<T>(Int32 offset) where T : unmanaged
	{
		var fieldAddress = GetFieldAddress(offset);

		return context.GetValue<T>(fieldAddress);
	}

	public void SetValue<T>(Int32 offset, T value) where T : unmanaged
	{
		context.SetValue(GetFieldAddress(offset), value);
	}

	public T? GetObject<T>(Int32 offset) where T : class
	{
		var address = GetFieldAddress(offset);

		return (T?)context.GetObject(address);
	}

	public void SetObject<T>(Int32 offset, T? value) where T : class
	{
		context.SetObject(GetFieldAddress(offset), value);
	}
}

public interface IPeachyStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Value : unmanaged
	where Mixin : IPeach
{
	Value Get(ref Mixin mixin, Int32 offset);

	void Set(ref Mixin mixin, Int32 offset, Value value);
}

public struct PeachyStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPeachyStructPropertyImplementation<Value, InternalPeachMixin>
	where Value : unmanaged
{
	public Value Get(ref InternalPeachMixin mixin, Int32 offset) => mixin.GetValue<Value>(offset);

	public void Set(ref InternalPeachMixin mixin, Int32 offset, Value value) => mixin.SetValue(offset, value);
}

public interface IPeachyClassPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Value : class
	where Mixin : IPeach
{
	Value? Get(ref Mixin mixin, Int32 offset);

	void Set(ref Mixin mixin, Int32 offset, Value? value);
}

public struct PeachyClassPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPeachyClassPropertyImplementation<Value, InternalPeachMixin>
	where Value : class
{
	public Value? Get(ref InternalPeachMixin mixin, Int32 offset) => mixin.GetObject<Value>(offset);

	public void Set(ref InternalPeachMixin mixin, Int32 offset, Value? value) => mixin.SetObject(offset, value);
}

public class PeachyPropertyImplementationProvider : PropertyImplementationProvider
{
	public override Type Get(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return typeof(PeachyClassPropertyImplementation<>);
		}
		else
		{
			return typeof(PeachyStructPropertyImplementation<>);
		}
	}

	public override IEnumerable<Type> GetAll()
	{
		yield return typeof(PeachyClassPropertyImplementation<>);
		yield return typeof(PeachyStructPropertyImplementation<>);
	}
}
