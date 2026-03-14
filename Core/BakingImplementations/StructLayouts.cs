using System.Reflection;
using System.Text;

namespace AlphaBee.Layouts.Structs;

public record struct FieldEntry(FieldInfo FieldInfo, Int32 Offset, Int32 Size);

public static class LayoutExtensions
{
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

	interface ILayout
	{
		FieldEntry[] Fields { get; }
	}

	struct Layout<T> : ILayout
		where T : unmanaged
	{
		public FieldEntry[] Fields => fields;

		static FieldEntry[] fields;

		static Layout()
		{
			fields = GetLayout();
		}

		static FieldEntry[] GetLayout()
		{
			var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

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

				field.Offset = i;
				field.Size = Helper.SizeOf(field.FieldInfo.FieldType);
			}
		}
	}

	public static FieldEntry[] GetLayoutFields(this Type type)
	{
		var layoutType = typeof(Layout<>).MakeGenericType(type);

		var layout = layoutType.CreateInstance<ILayout>();

		return layout.Fields;
	}

	public static String Stringify(this IEnumerable<FieldEntry> entries)
	{
		var result = new StringBuilder();

		foreach (var entry in entries)
		{
			result.AppendLine($"{entry.Offset,4}: {entry.FieldInfo.Name} ({entry.Size} bytes)");
		}

		return result.ToString();
	}
}
