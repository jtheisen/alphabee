using AlphaBee.Layouts.Structs;
using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

public class StructLayoutTypeConfiguration<T> : ITypeConfiguration
	where T : unmanaged
{
	static FieldEntry[] fields = typeof(T).GetLayoutFields().ToArray();

	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
	{
		return fields.Single(f => f.FieldInfo.Name == property.Name).Layout.Offset;
	}
}
