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
	Type ClrType { get; }
}

//public class StructLayoutTypeConfiguration<PeachT, LayoutT> : IPeachTypeConfiguration
//	where PeachT : class
//	where LayoutT : unmanaged
//{
//	public Type ClrType => typeof(PeachT);	
//	public Int32 Size => Unsafe.SizeOf<LayoutT>();

//	static FieldEntry[] fields = typeof(LayoutT).GetLayoutFields().ToArray();

//	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
//	{
//		return fields.Single(f => f.FieldInfo.Name == property.Name).Layout.offset + ObjectHeader.Size;
//	}

//	public override Int32 GetHashCode() => GetType().GetHashCode();
//	public override Boolean Equals(Object? obj) => GetType() == obj?.GetType();
//}

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

public record PeachTypeConfiguration(PropertyNumbersDictionary Fields, Int32 Size, Type ClrType) : IPeachTypeConfiguration
{
	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
	{
		var layout = Fields.dict[property];

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

	public static IPeachTypeConfiguration Create(Type peachType, Type layoutType)
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

	public static IPeachTypeConfiguration Create(IClrTypeResolver resolver, ITypeDescription description)
	{
		if (description.Offsets?.Length is not Int32 n)
		{
			throw new Exception($"No offsets in type descriptor");
		}

		Trace.Assert(description.Sizes?.Length == n);
		Trace.Assert(description.ClrNames?.Length == n);
		Trace.Assert(description.TypeRefs?.Length == n);

		var size = description.Size;

		Trace.Assert(size > 0);

		var clrTypeName = description.ClrName;

		Trace.Assert(clrTypeName is not null);

		var clrType = resolver.GetClrType(clrTypeName);

		var dict = new PeachTypeLayoutDict();

		for (var i = 0; i < n; i++)
		{
			var name = description.ClrNames[i];
			var typeRef = description.TypeRefs[i];
			var offset = description.Offsets[i];
			var fieldSize = description.Sizes[i];

			Trace.Assert(name is not null);

			var property = resolver.GetClrProperty(name);

			dict.Add(property, new LayoutEntry(offset, fieldSize));
		}

		return new PeachTypeConfiguration(dict, size, clrType);
	}
}
