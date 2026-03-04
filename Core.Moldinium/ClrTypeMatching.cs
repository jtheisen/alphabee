using System.Diagnostics;
using System.Reflection;

namespace AlphaBee;

public interface IClrTypeResolver
{
	Type GetClrType(String name);
}

public class ClrTypeResolver : IClrTypeResolver
{
	private readonly Assembly[] assemblies;

	private readonly Dictionary<String, Type> resolvedTypes = new();

	public ClrTypeResolver(params Assembly[] assemblies)
	{
		this.assemblies = assemblies;
	}

	public Type GetClrType(String fqTypeName)
	{
		if (resolvedTypes.TryGetValue(fqTypeName, out var type)) return type;

		return resolvedTypes[fqTypeName] = FindClrType(fqTypeName);
	}

	Type FindClrType(String fqTypeName)
	{
		var type = Type.GetType(fqTypeName);

		if (type is not null) return type;

		foreach (var assembly in assemblies)
		{
			type = assembly.GetType(fqTypeName);

			if (type is not null) return type;
		}

		var assemblyNames = String.Join(", ", assemblies.Select(a => a.FullName));

		var currentAssemblyName = Assembly.GetExecutingAssembly().FullName;

		throw new Exception($"Could not find type {fqTypeName} in the base libs, the currently executing assembly ({currentAssemblyName}), or any of the following assemblies: {assemblyNames}");
	}
}

public static class ClrTypeMatching
{
	public static String GetFqTypeName(this IClrTypeResolver resolver, Type type)
	{
		return type.FullName ?? throw new Exception($"Strangly no FullName on type {type}");
	}

	public static PropertyInfo GetClrProperty(this IClrTypeResolver resolver, String name)
	{
		ParseFqPropertyName(name, out var fqTypeName, out var propertyName);

		var type = resolver.GetClrType(fqTypeName);
		var property = type.GetProperty(propertyName);

		Trace.Assert(property is not null, $"Property {propertyName} does not exist on type {type.FullName}");

		return property;
	}

	public static void ParseFqPropertyName(String fqPropertyName, out String fqTypeName, out String propertyName)
	{
		var i = fqPropertyName.LastIndexOf('.');

		Trace.Assert(i > 0 && i < fqPropertyName.Length - 1, $"Invalid fully qualified property name {fqPropertyName}");

		fqTypeName = fqPropertyName[..i];
		propertyName = fqPropertyName[(i + 1)..];
	}

	public static String GetFqPropertyName(this PropertyInfo property)
	{
		var type = property.DeclaringType;

		Trace.Assert(type is not null);

		return $"{type.FullName}.{property.Name}";
	}
}