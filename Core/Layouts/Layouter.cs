using System.Reflection;

namespace AlphaBee.Layouts;

public abstract class Layouter
{
	public record struct LayoutInput(Type InterfaceType, Int32 Offset, ILayouterMetadataProvider MetaDataProvider);

	public record struct Layout(Entry[] Entries, Int32 Size);

	public record struct Entry(PropertyInfo Property, LayoutEntry Layout);

	public Layout GetLayout(LayoutInput input)
	{
		var layout = FindLayout(input);

		Validate(layout);

		return layout;
	}

	[Conditional("DEBUG")]
	void Validate(Layout layout)
	{
		var (entries, totalSize) = layout;

		var bits = new System.Collections.BitArray(totalSize);

		foreach (var (_, entry) in entries)
		{
			var (offset, size) = entry;

			for (var i = 0; i < size; ++i)
			{
				Trace.Assert(!bits[offset + i]);

				bits[i] = true;
			}
		}
	}

	protected abstract Layout FindLayout(LayoutInput input);

	public static Layout GetDefaultLayout(LayoutInput input)
	{
		return NaiveLayouter.Instance.GetLayout(input);
	}
}

public class NaiveLayouter : Layouter
{
	public static readonly NaiveLayouter Instance = new();

	protected override Layout FindLayout(LayoutInput input)
	{
		var (type, p, metadata) = input;

		var properties = metadata.GetProperties(type).ToArray();

		var n = properties.Length;

		var entries = new Entry[n];

		for (var i = 0; i < n; i++)
		{
			var property = properties[i];

			var size = metadata.GetSize(property);

			entries[i] = new Entry(property, new LayoutEntry(p, size));

			p += size;
		}

		return new(entries, p);
	}
}

public interface ILayouterMetadataProvider
{
	IEnumerable<PropertyInfo> GetProperties(Type interfaceType);

	Int32 GetSize(PropertyInfo property);
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

	public Int32 GetSize(PropertyInfo property)
	{
		var type = property.PropertyType;

		if (!type.IsValueType)
		{
			return Unsafe.SizeOf<Ref>();
		}
		else if (Nullable.GetUnderlyingType(type) is not null)
		{
			return NullableStruct.MakeNullableType(type).SizeOf();
		}
		else
		{
			return type.SizeOf();
		}
	}
}
