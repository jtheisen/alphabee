using AlphaBee.Layouts;
using AlphaBee.Layouts.Structs;
using Moldinium.Baking;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using PeachTypeLayoutDict = System.Collections.Generic.Dictionary<
	System.Reflection.PropertyInfo,
	AlphaBee.Layouts.LayoutEntry
>;

namespace AlphaBee;

public interface IPeachTypeConfiguration : ITypeConfiguration
{
	PeachTypeLayout Layout { get; }
	Int32 Size { get; }
	Type InterfaceType { get; }
}

// Exists only because normal dictionaries can't be compared
public struct PropertyNumbersDictionary : IEquatable<PropertyNumbersDictionary>
{
	public readonly PeachTypeLayoutDict dict;

	public static implicit operator PropertyNumbersDictionary(PeachTypeLayoutDict dict)
	{
		return new PropertyNumbersDictionary(dict);
	}

	public PropertyNumbersDictionary(PeachTypeLayoutDict dict)
	{
		this.dict = dict;
	}

	public Boolean Equals(PropertyNumbersDictionary other)
	{
		if (dict.Count != other.dict.Count)
		{
			return false;
		}

		foreach (var kvp in dict)
		{
			var value = other.dict[kvp.Key];

			if (!kvp.Value.Equals(value)) return false;
		}

		return true;
	}

	public override Int32 GetHashCode()
	{
		var hash = 0;

		foreach (var kvp in dict)
		{
			hash = kvp.Key.GetHashCode() ^ kvp.Value.GetHashCode();
		}

		return hash;
	}

	public override Boolean Equals([NotNullWhen(true)] Object? obj)
	{
		if (obj is PropertyNumbersDictionary other)
		{
			return Equals(other);
		}
		else
		{
			return false;
		}
	}
}

// Can compare, and represents only the configuration part that uniquily identifies the layout
public record PeachTypeLayout(PropertyNumbersDictionary Properties, Int32 Size, Type InterfaceType)
{
	public static PeachTypeLayout Create<PeachT, LayoutT>()
		where PeachT : class
		where LayoutT : struct
	{
		return Create(typeof(PeachT), typeof(LayoutT));
	}

	public PeachTypeConfiguration ToConfiguration(TypeRef typeRef)
	{
		return new PeachTypeConfiguration(this, typeRef, true);
	}

	public static PeachTypeLayout Create(Type peachType, Type layoutType)
	{
		var layoutFields = layoutType.GetLayoutFields();

		var dict = new PeachTypeLayoutDict();

		foreach (var field in layoutFields)
		{
			var property = peachType.GetProperty(field.FieldInfo.Name);

			Trace.Assert(property is not null);

			dict[property] = field.Layout;
		}

		return new PeachTypeLayout(dict, layoutType.SizeOf(), peachType);
	}

	public static PeachTypeLayout Create(IClrTypeResolver resolver, ITypeDescription description)
	{
		var properties = description.Properties;

		Trace.Assert(properties is not null);

		var n = properties.Length;

		var size = description.Size;

		Trace.Assert(size > 0);

		var clrTypeName = description.ClrName;

		Trace.Assert(clrTypeName is not null);

		var clrType = resolver.GetClrType(clrTypeName);

		var dict = new PeachTypeLayoutDict();

		for (var i = 0; i < n; i++)
		{
			ref var entry = ref properties[i];

			Trace.Assert(entry.ClrName is not null);

			var property = resolver.GetClrProperty(entry.ClrName);

			dict.Add(property, new LayoutEntry(entry.Offset, entry.Size));
		}

		return new PeachTypeLayout(dict, size, clrType);
	}
}

// Compares only the layout part and tags all the remaining configuration data along
public class PeachTypeConfiguration : IPeachTypeConfiguration
{
	static readonly PropertyInfo TypeRefProperty = typeof(IPeach).GetProperty(nameof(IPeach.ImplementationTypeRef))!;

	private readonly PeachTypeLayout layout;
	private readonly TypeRef typeRef;
	private readonly Boolean useSuffix;

	public PeachTypeLayout Layout => layout;

	public String? TypeSuffix => useSuffix ? $"#{typeRef.no}" : null;

	public Int32 Size => layout.Size;

	public Type InterfaceType => layout.InterfaceType;

	public PeachTypeConfiguration(PeachTypeLayout layout, TypeRef typeRef, Boolean useSuffix)
	{
		this.layout = layout;
		this.typeRef = typeRef;
		this.useSuffix = useSuffix;
	}

	public IEnumerable<Type> GetExtraInterfaces()
	{
		yield return typeof(IPeach);
	}

	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
	{
		if (property == TypeRefProperty)
		{
			return typeRef.no;
		}

		var entry = layout.Properties.dict[property];

		switch (argumentName)
		{
			case "offset":
				return entry.offset + ObjectHeader.Size;
			default:
				throw new Exception($"Unknown number argument '{argumentName}'");
		}
	}

	public override Int32 GetHashCode() => layout.GetHashCode();

	public override Boolean Equals(Object? obj) => layout?.Equals(obj) ?? false;
}
