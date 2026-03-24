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
	PeachConnection Connection { get; set; }

	Int64 Address { get; }
	PeachContext Context { get; }

	void Init(PeachContext context, Int64 address);
}

[IgnoreForBaking]
public interface IInternalPeachMixin : IPeachMixin
{
	T GetValue<T>(Int32 offset) where T : unmanaged;

	void SetValue<T>(Int32 offset, T value) where T : unmanaged;

	T? GetNullableValue<T>(Int32 offset) where T : unmanaged;

	void SetNullableValue<T>(Int32 offset, T? value) where T : unmanaged;

	T? GetObject<T>(Int32 offset) where T : class;

	void SetObject<T>(Int32 offset, T? value) where T : class;
}

public readonly record struct PeachConnection(PeachContext Context, Int64 Address);

public struct InternalPeachMixin : IInternalPeachMixin
{
	public PeachConnection Connection { get; set; }

	public Int64 Address => Connection.Address;
	public PeachContext Context => Connection.Context;

	public void Init(PeachContext context, Int64 address) => Connection = new(context, address);

	Int64 GetFieldAddress(Int32 offset) => Address + offset;

	public Span<T> GetSpan<T>(Int32 offset, Int32 length) where T : unmanaged
	{
		var fieldAddress = GetFieldAddress(offset);

		return Context.GetSpan<T>(fieldAddress, length);
	}

	public T GetValue<T>(Int32 offset) where T : unmanaged
	{
		var fieldAddress = GetFieldAddress(offset);

		return Context.GetValue<T>(fieldAddress);
	}

	public void SetValue<T>(Int32 offset, T value) where T : unmanaged
	{
		Context.SetValue(GetFieldAddress(offset), value);
	}

	public T? GetNullableValue<T>(Int32 offset) where T : unmanaged
	{
		var fieldAddress = GetFieldAddress(offset);

		return Context.GetValue<NullableStruct<T>>(fieldAddress);
	}

	public void SetNullableValue<T>(Int32 offset, T? value) where T : unmanaged
	{
		var fieldAddress = GetFieldAddress(offset);

		Context.SetValue<NullableStruct<T>>(fieldAddress, value);
	}

	public T? GetObject<T>(Int32 offset) where T : class
	{
		var address = GetFieldAddress(offset);

		return (T?)Context.GetObjectFromReferenceAddress(address);
	}

	public void SetObject<T>(Int32 offset, T? value) where T : class
	{
		Context.SetObjectToReferenceAddress(GetFieldAddress(offset), value);
	}
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


public interface IPeachyInlineSpanPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Arg)] ValueArg,
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where ValueArg : unmanaged
	where Value : allows ref struct
	where Mixin : IPeachMixin
{
	Value Get(ref Mixin mixin, Int32 offset, Int32 length);

	Value Set();
}

public struct PeachyInlineSpanPropertyImplementation<ValueArg> : IPeachyInlineSpanPropertyImplementation<ValueArg, Span<ValueArg>, InternalPeachMixin>
	where ValueArg : unmanaged
{
	public Span<ValueArg> Get(ref InternalPeachMixin mixin, Int32 offset, Int32 lengthAsArray) => mixin.GetSpan<ValueArg>(offset, lengthAsArray);

	public Span<ValueArg> Set() => throw new NotImplementedException();
}


public interface IPeachyNullableStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Arg)] ImplementationArgument,
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Mixin)] Mixin
> : IPropertyImplementation
	where Mixin : IPeachMixin
{
	Value Get(ref Mixin mixin, Int32 offset);

	void Set(ref Mixin mixin, Int32 offset, Value value);
}

public struct PeachyNullableStructPropertyImplementation<ValueArg> : IPeachyNullableStructPropertyImplementation<ValueArg, ValueArg?, InternalPeachMixin>
	where ValueArg : unmanaged
{
	public ValueArg? Get(ref InternalPeachMixin mixin, Int32 offset) => mixin.GetNullableValue<ValueArg>(offset);

	public void Set(ref InternalPeachMixin mixin, Int32 offset, ValueArg? value) => mixin.SetNullableValue(offset, value);
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
	static readonly PropertyImplementationWithFlags ImplTypeNo = new(typeof(PeachyTypeNoPropertyImplementation), PropertyImplementationFlags.ImplementationExplicit);

	static readonly PropertyImplementationWithFlags ImplSpan = new(typeof(PeachyInlineSpanPropertyImplementation<>), PropertyImplementationFlags.None);

	static PropertyImplementationWithFlags Get(Boolean asStruct, Boolean isNullable, Boolean implementExplicity)
	{
		var flags = implementExplicity ? PropertyImplementationFlags.ImplementationExplicit : PropertyImplementationFlags.None;

		if (!asStruct)
		{
			return new(typeof(PeachyClassPropertyImplementation<>), flags);
		}
		else if (isNullable)
		{
			return new(typeof(PeachyNullableStructPropertyImplementation<>), flags | PropertyImplementationFlags.MakeGenericWithBaseType);
		}
		else
		{
			return new(typeof(PeachyStructPropertyImplementation<>), flags);
		}
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

		var isValueType = propertyType.IsValueType;
		var isNullable = !isValueType || Nullable.GetUnderlyingType(propertyType) is not null;

		if (propertyType == typeof(TypeNo) && property.Name == nameof(IPeach.ImplementationTypeNo))
		{
			return ImplTypeNo;
		}
		else if (Spanlikes.IsInlineSpanlike(property, out _))
		{
			return ImplSpan;
		}
		else
		{
			return Get(isValueType, isNullable, explicitly);
		}
	}

	public override IEnumerable<PropertyImplementationWithFlags> GetAll()
	{
		yield return Get(false, true, false);
		yield return Get(false, true, true);
		yield return Get(true, false, false);
		yield return Get(true, false, true);
		yield return Get(true, true, false);
		yield return Get(true, true, true);
		yield return ImplSpan;
		yield return ImplTypeNo;
	}
}
