using System.Reflection;
using System.Reflection.Emit;

namespace AlphaBee;

public class LayoutSpacerBakery
{
	public readonly static LayoutSpacerBakery Intance = new LayoutSpacerBakery();

	readonly AssemblyBuilder assemblyBuilder;
	readonly ModuleBuilder moduleBuilder;

	readonly Dictionary<Int32, Type> spacers = new();

	public interface ISpacer
	{
	}

	public LayoutSpacerBakery()
	{
		var name = nameof(LayoutSpacerBakery);

		assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
		moduleBuilder = assemblyBuilder.DefineDynamicModule(name);
	}

	public static Boolean IsSpacer(Type type, out Int32 size)
	{
		if (type.IsAssignableTo(typeof(ISpacer)))
		{
			size = type.SizeOf();
			return true;
		}
		else
		{
			size = 0;
			return false;
		}
	}

	public Type EnsureSpacerType(Int32 size)
	{
		Trace.Assert(size > 0, "Layout spacers must have a positive size");

		if (spacers.TryGetValue(size, out var type)) return type;

		return spacers[size] = DefineSpacer(size);
	}

	Type DefineSpacer(Int32 size)
	{
		var typeBuilder = moduleBuilder.DefineType(
			$"Spacer{size}Bytes",
			TypeAttributes.Public | TypeAttributes.ExplicitLayout,
			typeof(ValueType),
			PackingSize.Size1,
			size
		);

		typeBuilder.AddInterfaceImplementation(typeof(ISpacer));

		return typeBuilder.CreateType();
	}
}
