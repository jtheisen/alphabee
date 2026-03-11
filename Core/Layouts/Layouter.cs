using System.Reflection;

namespace AlphaBee.Layouts;

public abstract class Layouter
{
	public record struct Input(Type InterfaceType, Int32 Offset, ILayouterMetadataProvider MetaDataProvider);

	public record struct TypeLayout(PropertyLayout[] Properties, Int32 Size);

	public record struct PropertyLayout(PropertyInfo Property, OffsetExtent OffsetExtent);

	public record struct OffsetExtent(ObjectExtent Extent, Int32 Offset);

	public TypeLayout GetLayout(Input input)
	{
		var layout = FindLayout(input);

#if DEBUG
		Validate(layout);
#endif

		return layout;
	}

	public void Validate(TypeLayout layout)
	{
		var (entries, totalSize) = layout;

		var bits = new System.Collections.BitArray(totalSize);

		foreach (var (_, entry) in entries)
		{
			var (extent, offset) = entry;

			for (var i = 0; i < extent.Size; ++i)
			{
				Trace.Assert(!bits[offset + i]);

				bits[i] = true;
			}
		}
	}

	protected abstract TypeLayout FindLayout(Input input);

	public static TypeLayout GetDefaultLayout(Input input)
	{
		return NaiveLayouter.Instance.GetLayout(input);
	}
}

public class NaiveLayouter : Layouter
{
	public static readonly NaiveLayouter Instance = new();

	protected override TypeLayout FindLayout(Input input)
	{
		var (type, p, metadata) = input;

		var properties = metadata.GetProperties(type).ToArray();

		var n = properties.Length;

		var entries = new PropertyLayout[n];

		for (var i = 0; i < n; i++)
		{
			var property = properties[i];

			var extent = metadata.GetExtent(property);

			entries[i] = new PropertyLayout(property, new OffsetExtent(extent, p));

			p += extent.Size;
		}

		return new(entries, p);
	}
}

public interface ILayouterMetadataProvider
{
	IEnumerable<PropertyInfo> GetProperties(Type interfaceType);

	ObjectExtent GetExtent(PropertyInfo property);
}

public class LayouterMetdataProvider : ILayouterMetadataProvider
{
	public IEnumerable<PropertyInfo> GetProperties(Type interfaceType)
	{
		foreach (var property in interfaceType.GetProperties())
		{
			yield return property;
		}

		foreach (var otherInterfaceType in interfaceType.GetInterfaces())
		{
			foreach (var property in otherInterfaceType.GetProperties())
			{
				yield return property;
			}
		}
	}

	public ObjectExtent GetExtent(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return ObjectExtent.CreateForStruct<Ref>();
		}
		else if (Nullable.GetUnderlyingType(type) is not null)
		{
			return ObjectExtent.CreateForStruct(NullableStruct.MakeNullableType(type));
		}
		else
		{
			return ObjectExtent.CreateForStruct(type);
		}
	}
}
