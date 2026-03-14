using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace AlphaBee;

public class LayoutSpacerBakery
{
	public readonly static LayoutSpacerBakery Intance = new LayoutSpacerBakery();

	readonly AssemblyBuilder assemblyBuilder;
	readonly ModuleBuilder moduleBuilder;

	record struct Sizes(Int32 Size, Int32 SubSize);

	readonly Dictionary<Sizes, Type> spacers = new();

	[AttributeUsage(AttributeTargets.Struct)]
	public class SubSizeAttribute(Int32 subSize) : Attribute
	{
		public Int32 SubSize { get; } = subSize;
	}

	public interface ISpacer
	{
	}

	public LayoutSpacerBakery()
	{
		var name = nameof(LayoutSpacerBakery);

		assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
		moduleBuilder = assemblyBuilder.DefineDynamicModule(name);
	}

	public static Boolean IsSpacer(Type type, out Int32 size, out Int32 subSize)
	{
		if (type.IsAssignableTo(typeof(ISpacer)))
		{
			var subSizeAttribute = type.GetCustomAttribute<SubSizeAttribute>();

			Trace.Assert(subSizeAttribute is not null);
			
			size = type.SizeOf();
			subSize = subSizeAttribute.SubSize;

			return true;
		}
		else
		{
			size = 0;
			subSize = 0;

			return false;
		}
	}

	public Type EnsureSpacerType(Int32 size, Type? itemType = null)
	{
		return EnsureSpacerType(size, itemType?.SizeOf() ?? 1);
	}

	public Type EnsureSpacerType(Int32 size, Int32 subSize)
	{
		Trace.Assert(size > 0, "Layout spacers must have a positive size");

		var sizes = new Sizes(size, subSize);

		if (!spacers.TryGetValue(sizes, out var spacerType))
		{
			spacerType = spacers[sizes] = DefineSpacer(sizes);
		}

		return spacerType;
	}

	Type DefineSpacer(Sizes sizes)
	{
		var (size, subSize) = sizes;

		var typeBuilder = moduleBuilder.DefineType(
			$"SpacerWith{size}BytesInChunksOf{subSize}Bytes",
			TypeAttributes.Public | TypeAttributes.ExplicitLayout,
			typeof(ValueType),
			PackingSize.Size1,
			size
		);

		typeBuilder.AddInterfaceImplementation(typeof(ISpacer));

		var subsSizeAttributeCtor = typeof(SubSizeAttribute).GetConstructor([typeof(Int32)]);

		Trace.Assert(subsSizeAttributeCtor is not null);

		var subsSizeAttributeBuilder = new CustomAttributeBuilder(subsSizeAttributeCtor, [subSize]);

		typeBuilder.SetCustomAttribute(subsSizeAttributeBuilder);

		return typeBuilder.CreateType();
	}
}
