using System.Reflection;
using System.Text;

namespace AlphaBee.StructLayouts;

public struct FieldEntry
{
	public FieldInfo FieldInfo;
	public LayoutEntry Layout;
}

public struct LayoutEntry
{
	public Int32 Offset;
	public Int32 Size;
}

static class Helper
{
	public static IHelper Create(Type type)
	{
		return typeof(Helper<>).MakeGenericType(type).CreateInstance<IHelper>();
	}

	public static Object GetAllOne(Type type)
	{
		return Create(type).GetAllOneAsObject();
	}

	public static Int32 SizeOf(Type type)
	{
		return Create(type).SizeOf();
	}
}

interface IHelper
{
	Object GetAllOneAsObject();

	Int32 SizeOf();
}

struct Helper<T> : IHelper
	where T : unmanaged
{
	public void SetAllOne(ref T value)
	{
		value.AsBytes().Fill(Byte.MaxValue);
	}

	public T GetAllOne()
	{
		var value = default(T);
		SetAllOne(ref value);
		return value;
	}

	public Object GetAllOneAsObject() => GetAllOne();

	public Int32 SizeOf() => Unsafe.SizeOf<T>();
}

static class Layout
{
}

interface ILayout
{
	IEnumerable<FieldEntry> Fields { get; }
}

struct Layout<T> : ILayout
	where T : unmanaged
{
	public IEnumerable<FieldEntry> Fields => fields;

	static IEnumerable<FieldEntry> fields;

	static Layout()
	{
		fields = GetLayout();
	}

	static IEnumerable<FieldEntry> GetLayout()
	{
		var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

		var entries = fields.Select(f => new FieldEntry { FieldInfo = f }).ToArray();

		GetLayout(entries.AsSpan());

		return entries;
	}

	static void GetLayout(Span<FieldEntry> fields)
	{
		foreach (ref var field in fields)
		{
			Object boxed = default(T);

			field.FieldInfo.SetValue(boxed, Helper.GetAllOne(field.FieldInfo.FieldType));

			var unboxed = (T)boxed;

			var bytes = unboxed.AsBytes();

			var i = bytes.IndexOfAnyExcept((Byte)0);

			field.Layout = new LayoutEntry { Offset = i, Size = Helper.SizeOf(field.FieldInfo.FieldType) };
		}
	}

}

public static class LayoutExtensions
{
	public static IEnumerable<FieldEntry> GetLayoutFields(this Type type)
	{
		var layoutType = typeof(Layout<>).MakeGenericType(type);

		var layout = layoutType.CreateInstance<ILayout>();

		return layout.Fields;
	}

	public static T CreateInstance<T>(this Type type)
	{
		return (T)(Activator.CreateInstance(type) ?? throw new ArgumentException("Could not create instances from type"));
	}

	public static String Stringify(this IEnumerable<FieldEntry> entries)
	{
		var result = new StringBuilder();

		foreach (var entry in entries)
		{
			result.AppendLine($"{entry.Layout.Offset,4}: {entry.FieldInfo.Name} ({entry.Layout.Size} bytes)");
		}

		return result.ToString();
	}
}