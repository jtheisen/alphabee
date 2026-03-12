using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

#pragma warning disable CS0169

public interface ILayoutStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPropertyImplementation
	where Value : unmanaged
{
	Value Get();

	void Set(Value value);
}

public struct LayoutStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : ILayoutStructPropertyImplementation<Value>
	where Value : unmanaged
{
	Value value;

	public Value Get() => throw new NotImplementedException();

	public void Set(Value value) => throw new NotImplementedException();
}


public interface ILayoutNullableStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPropertyImplementation
	where Value : unmanaged
{
	Value? Get();

	void Set(Value value);
}

public struct LayoutNullableStructPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : ILayoutNullableStructPropertyImplementation<Value>
	where Value : unmanaged
{
	NullableStruct<Value> value;

	public Value? Get() => throw new NotImplementedException();

	public void Set(Value value) => throw new NotImplementedException();
}


public interface IInlineSpanPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value,
	[TypeKind(ImplementationTypeArgumentKind.Extra)] Spacer
> : IPropertyImplementation
	where Value : unmanaged, allows ref struct
	where Spacer : unmanaged
{
	Value Get();

	void Set(Value value);
}

public struct InlineSpanPropertyImplementation<Value, Spacer> : IInlineSpanPropertyImplementation<Value, Spacer>
	where Value : unmanaged, allows ref struct
	where Spacer : unmanaged
{
	Spacer spacer;

	public Value Get() => throw new NotImplementedException();

	public void Set(Value value) => throw new NotImplementedException();
}


public interface ILayoutClassPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : IPropertyImplementation
	where Value : class
{
	Value Get();

	void Set(Value value);
}

public struct LayoutClassPropertyImplementation<
	[TypeKind(ImplementationTypeArgumentKind.Value)] Value
> : ILayoutClassPropertyImplementation<Value>
	where Value : class
{
	Int64 address;

	public Value Get() => throw new NotImplementedException();

	public void Set(Value value) => throw new NotImplementedException();
}


public class LayoutPropertyImplementationProvider : PropertyImplementationProvider
{
	static PropertyImplementationWithFlags Get(Type type) => new(type, PropertyImplementationFlags.BackingFieldPublicAndUnprefixed);

	public override PropertyImplementationWithFlags Get(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return Get(typeof(LayoutClassPropertyImplementation<>));
		}
		else if (Nullable.GetUnderlyingType(type) is not null)
		{
			return Get(typeof(LayoutNullableStructPropertyImplementation<>));
		}
		else if (InlineSpanAttribute.IsInlineSpan(property, out var length))
		{
			var valueType = Spanlikes.GetTypeFromSpanlikeOrNull(type);

			Trace.Assert(valueType is not null, $"Unsupported span-like property type {type}");

			var spacerType = LayoutSpacerBakery.Intance.EnsureSpacerType(valueType.SizeOf() * length);

			return Get(typeof(InlineSpanPropertyImplementation<,>));
		}
		else
		{
			return Get(typeof(LayoutStructPropertyImplementation<>));
		}
	}

	public override IEnumerable<PropertyImplementationWithFlags> GetAll()
	{
		yield return Get(typeof(LayoutClassPropertyImplementation<>));
		yield return Get(typeof(LayoutStructPropertyImplementation<>));
		yield return Get(typeof(LayoutNullableStructPropertyImplementation<>));
		yield return Get(typeof(InlineSpanPropertyImplementation<,>));
	}
}
