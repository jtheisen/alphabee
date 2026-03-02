using AlphaBee.Layouts.Structs;
using AlphaBee.Utilities;
using Moldinium.Baking;
using System.Reflection;

namespace AlphaBee;

public static class StructLayoutTypeConfiguration
{
	public static ITypeConfiguration Create(Type unmanagedType)
	{
		return typeof(StructLayoutTypeConfiguration<>)
			.MakeGenericType(unmanagedType)
			.CreateInstance<ITypeConfiguration>();
	}
}

public class StructLayoutTypeConfiguration<T> : ITypeConfiguration
	where T : unmanaged
{
	static FieldEntry[] fields = typeof(T).GetLayoutFields().ToArray();

	public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
	{
		return fields.Single(f => f.FieldInfo.Name == property.Name).Layout.offset + ObjectHeader.Size;
	}

}
