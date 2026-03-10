using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Interface)]
public class ImplementExplicitlyAttribute : Attribute;

[ImplementExplicitly]
public interface IPeach : IPeachMixin
{
	TypeNo ImplementationTypeNo { get; }
}

[ImplementExplicitly]
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
	static PropertyImplementationWithFlags ImplTypeNo => new(typeof(PeachyTypeNoPropertyImplementation), PropertyImplementationFlags.ImplementationExplicit);

	static PropertyImplementationWithFlags Get(Boolean asStruct, Boolean implementExplicity)
	{
		var flags = implementExplicity ? PropertyImplementationFlags.ImplementationExplicit : PropertyImplementationFlags.None;

		var implementationType = asStruct ? typeof(PeachyStructPropertyImplementation<>) : typeof(PeachyClassPropertyImplementation<>);

		return new(implementationType, flags);
	}

	static Boolean ShouldImplementExplicitly(PropertyInfo property)
	{
		var attribute =
			property.GetCustomAttribute<ImplementExplicitlyAttribute>() ??
			property.DeclaringType?.GetCustomAttribute<ImplementExplicitlyAttribute>();

		return attribute is not null;
	}

	public override PropertyImplementationWithFlags Get(PropertyInfo property)
	{
		var propertyType = property.PropertyType;

		var explicitly = ShouldImplementExplicitly(property);

		if (!propertyType.IsValueType)
		{
			return Get(false, explicitly);
		}
		else if (propertyType == typeof(TypeNo) && property.Name == nameof(IPeach.ImplementationTypeNo))
		{
			return ImplTypeNo;
		}
		else
		{
			return Get(true, explicitly);
		}
	}

	public override IEnumerable<PropertyImplementationWithFlags> GetAll()
	{
		yield return Get(false, false);
		yield return Get(false, true);
		yield return Get(true, false);
		yield return Get(true, true);
		yield return ImplTypeNo;
	}
}
