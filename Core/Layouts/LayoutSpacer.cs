using System.Reflection;
using System.Reflection.Emit;

namespace AlphaBee.Layouts;

//public struct DummyInlArr<T> : IInlArr<T>
//	where T : unmanaged
//{
	
//}

//public interface IInlArr<T>
//{

//}

//public interface IStructLayout
//{
//	Int32? GetArrayPropertyLength(String propertyName) => null;
//}

//public interface IStructLayout<ConcreteSampleT> : IStructLayout
//	where ConcreteSampleT : IStructLayout
//{
//}

//public interface IStructMetaLayout
//{
//	Int32? GetArrayPropertyLength(String propertyName);
//}

//public struct Foo<ArrayT> : IStructLayout<Foo<DummyInlArr<Int16>>>
//	where ArrayT : struct, IInlArr<Int16>
//{
//	public ArrayT Words;

//	Int32? IStructLayout.GetArrayPropertyLength(String propertyName)
//	{
//		switch (propertyName)
//		{
//			case nameof(Words):
//				return 42;
//			default:
//				return null;
//		}
//	}
//}

//public interface IFoo
//{
//	IInlArr<Int16> Words { get; }
//}

//public class LayoutSpacerBakery
//{
//	readonly AssemblyBuilder assemblyBuilder;
//	readonly ModuleBuilder moduleBuilder;

//	readonly Dictionary<Int32, Type> spacers = new();

//	public LayoutSpacerBakery()
//	{
//		var name = nameof(LayoutSpacerBakery);

//		assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
//		moduleBuilder = assemblyBuilder.DefineDynamicModule(name);
//	}

//	public Type EnsureSpacer(Int32 size)
//	{
//		Trace.Assert(size > 0, "Layout spacers must have a positive size");

//		if (spacers.TryGetValue(size, out var type)) return type;

//		return spacers[size] = DefineSpacer(size);
//	}

//	Type DefineSpacer(Int32 size)
//	{
//		var builder = moduleBuilder.DefineType(
//			$"Spacer{size}Bytes",
//			TypeAttributes.Public | TypeAttributes.ExplicitLayout,
//			typeof(ValueType),
//			size
//		);

//		return builder.CreateType();
//	}
//}
