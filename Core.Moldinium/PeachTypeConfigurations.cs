using AlphaBee.Layouts;
using AlphaBee.Layouts.Structs;
using Moldinium.Baking;
using Moldinium.Tracking;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using PeachTypeLayoutDict = System.Collections.Generic.IReadOnlyDictionary<
	System.Reflection.PropertyInfo,
	AlphaBee.PropertyEntry
>;

namespace AlphaBee;

public interface IPropNoResolver
{
	PropNo GetPropNo(PropertyInfo propertyInfo);
}

public class TrivialPropNoResolver : IPropNoResolver
{
	public static readonly TrivialPropNoResolver Instance = new();

	PropNo IPropNoResolver.GetPropNo(PropertyInfo propertyInfo) => default;
}

[DebuggerDisplay("{ToString()}")]
public readonly struct PropertyEntry : IEquatable<PropertyEntry>
{
	readonly PropNo propNo;
	readonly LayoutEntry layout;

	public LayoutEntry Layout => layout;
	public PropNo PropNo => propNo; 
	public Int32 Offset => layout.offset;
	public Int32 Size => layout.size;

	public PropertyEntry(PropNo propNo, LayoutEntry layout)
	{
		this.propNo = propNo;
		this.layout = layout;
	}

	public Boolean Equals(PropertyEntry other)
	{
		return propNo.Equals(other.propNo) && layout.Equals(other.layout);
	}

	public override Int32 GetHashCode()
	{
		return propNo.GetHashCode() ^ layout.GetHashCode();
	}

	public override Boolean Equals([NotNullWhen(true)] Object? obj)
	{
		return obj is PropertyEntry other ? Equals(other) : false;
	}

	public override String ToString()
	{
		return $"{propNo}:{layout}";
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

	public LayoutEntry? GetLayoutEntryOrNull(PropertyInfo property)
	{
		return GetPropertyEntryOrNull(property)?.Layout;
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

	public static PeachTypeLayout Create<PeachT, LayoutT>(IPropNoResolver resolver)
		where PeachT : class
		where LayoutT : struct
	{
		return Create(typeof(PeachT), typeof(LayoutT), resolver);
	}

	public static PeachTypeLayout CreateWithoutPropNos(Type peachType, Type layoutType)
	{
		return Create(peachType, layoutType, TrivialPropNoResolver.Instance);
	}

	static PropertyInfo? GetPropertyForField(FieldInfo fieldInfo)
	{
		return fieldInfo.GetCustomAttribute<ForPropertyAttribute>()?.Property;
	}

	public static PeachTypeLayout Create(Type peachType, Type layoutType, IPropNoResolver resolver)
	{
		var layoutFields = layoutType.GetLayoutFields();

		var dict = PropertyBakingInfosDictionary.CreateEmptyArgumentDict();

		foreach (var field in layoutFields)
		{
			var fieldInfo = field.FieldInfo;

			var property = GetPropertyForField(fieldInfo) ?? peachType.GetProperty(fieldInfo.Name);

			Trace.Assert(property is not null, $"Could not get the property for the field {fieldInfo.Name} in layout struct");

			var propNo = resolver.GetPropNo(property);

			dict[property] = new PropertyEntry(propNo, field.Layout);
		}

		return new PeachTypeLayout(dict, layoutType.SizeOf(), peachType);
	}

	public static PeachTypeLayout Create(IClrTypeResolver typeResolver, ITypeDescription description)
	{
		var properties = description.Properties;

		Trace.Assert(properties is not null);

		var n = properties.Length;

		var size = description.Size;

		Trace.Assert(size > 0);

		var clrTypeName = description.ClrName;

		Trace.Assert(clrTypeName is not null);

		var clrType = typeResolver.GetClrType(clrTypeName);

		var dict = PropertyBakingInfosDictionary.CreateEmptyArgumentDict();

		for (var i = 0; i < n; i++)
		{
			ref var entry = ref properties[i];

			Trace.Assert(entry.ClrName is not null);

			var property = typeResolver.GetClrProperty(entry.ClrName);

			var propNo = entry.PropertyNo;

			var layoutEntry = new LayoutEntry(entry.Offset, entry.Size);

			dict.Add(property, new PropertyEntry(propNo, layoutEntry));
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
	private readonly TypeNo typeNo;
	private readonly Boolean useSuffix;

	public PeachTypeLayout Layout => layout;

	public String? TypeSuffix => useSuffix ? $"#{typeNo.no}" : null;

	public Int32 Size => layout.Size;

	public Type InterfaceType => layout.InterfaceType;

	public PeachTypeConfiguration(PeachTypeLayout layout, TypeNo typeNo, Boolean useSuffix)
	{
		this.layout = layout;
		this.typeNo = typeNo;
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
			return typeNo.no;
		}

		var entry = layout.Properties[property];

		switch (argumentName)
		{
			case "offset":
				return entry.Offset + ObjectHeader.Size;
			default:
				throw new Exception($"Unknown number argument '{argumentName}'");
		}
	}

	public override Int32 GetHashCode() => layout.GetHashCode();

	public override Boolean Equals(Object? obj) => layout?.Equals(obj) ?? false;
}
