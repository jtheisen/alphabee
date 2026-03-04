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
	public override Type Get(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return typeof(LayoutClassPropertyImplementation<>);
		}
		else
		{
			return typeof(LayoutStructPropertyImplementation<>);
		}
	}

	public override IEnumerable<Type> GetAll()
	{
		yield return typeof(LayoutClassPropertyImplementation<>);
		yield return typeof(LayoutStructPropertyImplementation<>);
	}
}
