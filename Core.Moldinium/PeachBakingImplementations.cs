using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

public interface IPeach : IPeachMixin
{
	TypeNo ImplementationTypeNo { get; }
}

public interface IPeachMixin
{
	AbstractPeachContext Context { get; }

	Int64 Address { get; }

	void Init(AbstractPeachContext context, Int64 address);
}

[IgnoreForBaking]
public interface IInternalPeachMixin : IPeachMixin
{
	T GetValue<T>(Int32 offset) where T : unmanaged;

	void SetValue<T>(Int32 offset, T value) where T : unmanaged;

	T? GetObject<T>(Int32 offset) where T : class;

	void SetObject<T>(Int32 offset, T? value) where T : class;
}

public struct InternalPeachMixin : IInternalPeachMixin
{
	Int64 address;
	AbstractPeachContext context;

	public Int64 Address => address;
	public AbstractPeachContext Context => context;

	public void Init(AbstractPeachContext context, Int64 address)
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

		return (T?)context.GetObjectFromReferenceAddress(address);
	}

	public void SetObject<T>(Int32 offset, T? value) where T : class
	{
		context.SetObjectToReferenceAddress(GetFieldAddress(offset), value);
	}
}

public interface IPeachyStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Value : unmanaged
	where Mixin : IPeachMixin
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
	where Mixin : IPeachMixin
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

public interface IPeachyTypeNoPropertyImplementation : IPropertyImplementation
{
	TypeNo Get(Int32 typeNo);

	void Set(Int32 typeNo);
}

public struct PeachyTypeNoPropertyImplementation : IPeachyTypeNoPropertyImplementation
{
	public TypeNo Get(Int32 typeNo) => new TypeNo(typeNo);

	public void Set(Int32 typeNo) => throw new NotImplementedException();
}

public class PeachPropertyImplementationProvider : PropertyImplementationProvider
{
	public override Type Get(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return typeof(PeachyClassPropertyImplementation<>);
		}
		else if (type == typeof(TypeNo) && property.Name == nameof(IPeach.ImplementationTypeNo))
		{
			return typeof(PeachyTypeNoPropertyImplementation);
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
		yield return typeof(PeachyTypeNoPropertyImplementation);
	}
}
