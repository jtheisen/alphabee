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
	Int32 Size { get; }
	Type InterfaceType { get; }
}

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

public record PeachTypeConfiguration(PropertyNumbersDictionary Properties, Int32 Size, Type InterfaceType) : IPeachTypeConfiguration
{
	static readonly PropertyInfo TypeRefProperty = typeof(IPeach).GetProperty(nameof(IPeach.ImplementationTypeRef))!;

	public IEnumerable<Type> GetExtraInterfaces()
	{
		yield return typeof(IPeach);
	}

	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
	{
		if (property == TypeRefProperty)
		{
			return 0;
		}

		var layout = Properties.dict[property];

		switch (argumentName)
		{
			case "offset":
				return layout.offset + ObjectHeader.Size;
			default:
				throw new Exception($"Unknown number argument '{argumentName}'");
		}
	}

	public static IPeachTypeConfiguration Create<PeachT, LayoutT>()
		where PeachT : class
		where LayoutT : struct
	{
		return Create(typeof(PeachT), typeof(LayoutT));
	}

	public static PeachTypeConfiguration Create(Type peachType, Type layoutType)
	{
		var layoutFields = layoutType.GetLayoutFields();

		var dict = new PeachTypeLayoutDict();

		foreach (var field in layoutFields)
		{
			var property = peachType.GetProperty(field.FieldInfo.Name);

			Trace.Assert(property is not null);

			dict[property] = field.Layout;
		}

		return new PeachTypeConfiguration(dict, layoutType.SizeOf(), peachType);
	}

	public static PeachTypeConfiguration Create(IClrTypeResolver resolver, ITypeDescription description)
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

		return new PeachTypeConfiguration(dict, size, clrType);
	}
}
