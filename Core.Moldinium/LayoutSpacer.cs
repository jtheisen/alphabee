using System.Reflection;
using System.Reflection.Emit;

namespace AlphaBee;

public class LayoutSpacerBakery
{
	readonly AssemblyBuilder assemblyBuilder;
	readonly ModuleBuilder moduleBuilder;

	readonly Dictionary<Int32, Type> spacers = new();

	public LayoutSpacerBakery()
	{
		var name = nameof(LayoutSpacerBakery);

		assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
		moduleBuilder = assemblyBuilder.DefineDynamicModule(name);
	}

	public Type EnsureSpacer(Int32 size)
	{
		Trace.Assert(size > 0, "Layout spacers must have a positive size");

		if (spacers.TryGetValue(size, out var type)) return type;

		return spacers[size] = DefineSpacer(size);
	}

	Type DefineSpacer(Int32 size)
	{
		var builder = moduleBuilder.DefineType(
			$"Spacer{size}Bytes",
			TypeAttributes.Public | TypeAttributes.ExplicitLayout,
			typeof(ValueType),
			size
		);

		return builder.CreateType();
	}
}
