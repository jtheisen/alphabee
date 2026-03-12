using AlphaBee.Layouts;
using AlphaBee.Layouts.Structs;
using Moldinium.Baking;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using PeachTypeLayoutDict = System.Collections.Generic.IReadOnlyDictionary<
	System.Reflection.PropertyInfo,
	AlphaBee.PropertyEntry
>;

namespace AlphaBee;

public interface IPropNumbersResolver
{
	PropAndTypeNo Resolve(PropertyInfo propertyInfo);
}

public class TrivialPropNumbersResolver : IPropNumbersResolver
{
	public static readonly TrivialPropNumbersResolver Instance = new();

	PropAndTypeNo IPropNumbersResolver.Resolve(PropertyInfo propertyInfo) => default;
}

[StructLayout(LayoutKind.Sequential, Size = 16)]
[DebuggerDisplay("{ToString()}")]
public readonly record struct PropertyEntry(ObjectHeader TypeAndExtent, PropNo PropNo, Int32 Offset)
{
	public OffsetExtent OffsetExtent => new(TypeAndExtent.Extent, Offset);
	public ObjectExtent Extent => TypeAndExtent.Extent;
	public Int32 Size => Extent.Size;

	public override String ToString()
	{
		return $"{PropNo}:{OffsetExtent}";
	}
}

public interface IPeachTypeConfiguration : ITypeConfiguration
{
	PeachTypeLayout Layout { get; }
	Int32 Size { get; }
	Type InterfaceType { get; }
}

// Exists only because normal dictionaries can't be compared
public struct PropertyBakingInfosDictionary : IEquatable<PropertyBakingInfosDictionary>, PeachTypeLayoutDict
{
	readonly PeachTypeLayoutDict dict;

	public static Dictionary<PropertyInfo, PropertyEntry> CreateEmptyArgumentDict() => new();

	public static implicit operator PropertyBakingInfosDictionary(Dictionary<PropertyInfo, PropertyEntry> dict)
	{
		return new PropertyBakingInfosDictionary(dict);
	}

	public PropertyBakingInfosDictionary(PeachTypeLayoutDict dict)
	{
		this.dict = dict;
	}

	public PropertyEntry this[PropertyInfo property] => dict[property];

	public Boolean TryGetValue(PropertyInfo property, [MaybeNullWhen(false)] out PropertyEntry entry)
	{
		return dict.TryGetValue(property, out entry);
	}

	public PropertyEntry? GetPropertyEntryOrNull(PropertyInfo property)
	{
		return TryGetValue(property, out var entry) ? entry : null;
	}

	public Boolean Equals(PropertyBakingInfosDictionary other)
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
		if (obj is PropertyBakingInfosDictionary other)
		{
			return Equals(other);
		}
		else
		{
			return false;
		}
	}

	public Int32 Count => dict.Count;

	public Boolean ContainsKey(PropertyInfo key) => dict.ContainsKey(key);

	public IEnumerator<KeyValuePair<PropertyInfo, PropertyEntry>> GetEnumerator()
	{
		return dict.GetEnumerator();
	}

	public IEnumerable<PropertyInfo> Keys => dict.Keys;

	public IEnumerable<PropertyEntry> Values => dict.Values;

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Can compare, and represents only the configuration part that uniquily identifies the layout
public record PeachTypeLayout(PropertyBakingInfosDictionary Properties, Int32 Size, Type InterfaceType)
{
	public static PeachTypeLayout CreateWithoutPropNos<PeachT, LayoutT>()
		where PeachT : class
		where LayoutT : struct
	{
		return CreateWithoutPropNos(typeof(PeachT), typeof(LayoutT));
	}

	public static PeachTypeLayout Create<PeachT, LayoutT>(IPropNumbersResolver resolver)
		where PeachT : class
		where LayoutT : struct
	{
		return Create(typeof(PeachT), typeof(LayoutT), resolver);
	}

	public static PeachTypeLayout CreateWithoutPropNos(Type peachType, Type layoutType)
	{
		return Create(peachType, layoutType, TrivialPropNumbersResolver.Instance);
	}

	static PropertyInfo? GetPropertyForField(FieldInfo fieldInfo)
	{
		return fieldInfo.GetCustomAttribute<ForPropertyAttribute>()?.Property;
	}

	public static PeachTypeLayout Create(Type interfaceType, Layouter.TypeLayout typeLayout, IPropNumbersResolver resolver)
	{
		var (properties, size) = typeLayout;

		var dict = PropertyBakingInfosDictionary.CreateEmptyArgumentDict();

		foreach (var (property, (extent, offset)) in properties)
		{
			var (typeNo, propNo) = resolver.Resolve(property);

			Trace.Assert(property is not null, $"Could not get the number for the property {property.Name} in layout");

			dict[property] = new PropertyEntry(new(typeNo, extent), propNo, offset);
		}

		return new(dict, size, interfaceType);
	}

	public static PeachTypeLayout Create(Type interfaceType, Type layoutType, IPropNumbersResolver propResolver)
	{
		var layoutFields = layoutType.GetLayoutFields();

		var dict = PropertyBakingInfosDictionary.CreateEmptyArgumentDict();

		foreach (var field in layoutFields)
		{
			var fieldInfo = field.FieldInfo;

			var property = GetPropertyForField(fieldInfo) ?? interfaceType.GetProperty(fieldInfo.Name);

			Trace.Assert(property is not null, $"Could not get the property for the field {fieldInfo.Name} in layout struct");

			var (typeNo, propNo) = propResolver.Resolve(property);

			dict[property] = new PropertyEntry(new(typeNo, new(field.Size)), propNo, field.Offset);
		}

		return new PeachTypeLayout(dict, layoutType.SizeOf(), interfaceType);
	}

	public static PeachTypeLayout Create(IClrTypeResolver typeResolver, ITypeDescription description)
	{
		var properties = description.Properties;

		Trace.Assert(properties is not null);

		var n = properties.Length;

		var size = description.Header.ContentSize;

		Trace.Assert(size > 0);

		var clrTypeName = description.ClrName;

		Trace.Assert(clrTypeName is not null);

		var clrType = typeResolver.GetClrType(clrTypeName);

		var dict = PropertyBakingInfosDictionary.CreateEmptyArgumentDict();

		for (var i = 0; i < n; i++)
		{
			if (properties[i] is not IPropertyDescription propertyDescription)
			{
				throw new Exception($"Property #{i} of type description for {clrTypeName} is null or has the wrong type");
			}

			Trace.Assert(propertyDescription.ClrName is not null);

			var property = typeResolver.GetClrProperty(propertyDescription.ClrName);

			dict.Add(property, propertyDescription.PropertyEntry);
		}

		return new PeachTypeLayout(dict, size, clrType);
	}

	public PeachTypeConfiguration ToConfiguration(TypeNo typeNo)
	{
		return new PeachTypeConfiguration(this, typeNo, true);
	}
}

// Compares only the layout part and tags all the remaining configuration data along
public class PeachTypeConfiguration : IPeachTypeConfiguration
{
	static readonly PropertyInfo TypeNoProperty = typeof(IPeach).GetProperty(nameof(IPeach.ImplementationTypeNo))!;

	private readonly PeachTypeLayout layout;
	private readonly TypeNo typeTypeNo;
	private readonly Boolean useSuffix;

	public PeachTypeLayout Layout => layout;

	public String? TypeSuffix => useSuffix ? $"#{typeTypeNo.no}" : null;

	public Int32 Size => layout.Size;

	public Type InterfaceType => layout.InterfaceType;

	public PeachTypeConfiguration(PeachTypeLayout layout, TypeNo typeTypeNo, Boolean useSuffix)
	{
		this.layout = layout;
		this.typeTypeNo = typeTypeNo;
		this.useSuffix = useSuffix;
	}

	public IEnumerable<Type> GetExtraInterfaces()
	{
		yield return typeof(IPeach);
	}

	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
	{
		if (property == TypeNoProperty)
		{
			return typeTypeNo.no;
		}

		var (tae, propNo, offset) = layout.Properties[property];

		var (typeNo, extent) = tae;

		switch (argumentName)
		{
			case "typeNo":
				return typeNo.no;
			case "propNo":
				return propNo.no;
			case "size":
				return extent.Size;
			case "typeAndExtent":
				return tae.Int64;
			case "offset":
				return offset + ObjectHeader.Size;
			default:
				throw new Exception($"Unknown number argument '{argumentName}'");
		}
	}

	public override Int32 GetHashCode() => layout.GetHashCode();

	public override Boolean Equals(Object? obj) => layout?.Equals(obj) ?? false;
}
