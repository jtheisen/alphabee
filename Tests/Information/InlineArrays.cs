using System.Reflection;
using System.Reflection.Emit;

namespace AlphaBee.Information;

#pragma warning disable CS0169

[TestClass]
public class InlineArrays
{
	[InlineArray(10)]
	public struct Buffer<T>
	{
		private T _firstElement;
	}

	[TestMethod]
	public void Test()
	{
		Assert.AreEqual(20, Unsafe.SizeOf<Buffer<Int16>>());
	}

	public struct Wrapper<T>
	{
		T item;
	}

	public class BufferTypeFactory
	{
		AssemblyBuilder assemblyBuilder;

		ModuleBuilder moduleBuilder;

		ConstructorInfo attrCtor;

		public BufferTypeFactory(String name = "Buffers")
		{
			assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new(name), AssemblyBuilderAccess.RunAndCollect);

			moduleBuilder = assemblyBuilder.DefineDynamicModule(name);

			attrCtor = typeof(InlineArrayAttribute).GetConstructor([typeof(Int32)])!;
		}

		public Type CreateBufferType(Type elementType, Int32 length)
		{
			var typeBuilder = moduleBuilder.DefineType("Buffer", default, typeof(ValueType));

			var attrBuilder = new CustomAttributeBuilder(attrCtor, [length]);

			typeBuilder.SetCustomAttribute(attrBuilder);

			var fieldBuilder = typeBuilder.DefineField("_firstElement", elementType, FieldAttributes.Private);

			var bufferType = typeBuilder.CreateType();

			return bufferType;
		}
	}

	[TestMethod]
	public void TestDynamic()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new("test"), AssemblyBuilderAccess.RunAndCollect);

		var moduleBuilder = assemblyBuilder.DefineDynamicModule("test");

		var typeBuilder = moduleBuilder.DefineType("Buffer", default, typeof(ValueType));

		var attrCtor = typeof(InlineArrayAttribute).GetConstructor([typeof(Int32)])!;

		var attrBuilder = new CustomAttributeBuilder(attrCtor, [3]);

		typeBuilder.SetCustomAttribute(attrBuilder);

		var fieldBuilder = typeBuilder.DefineField("_firstElement", typeof(Int16), FieldAttributes.Private);

		var type = typeBuilder.CreateType();

		Assert.IsTrue(type.IsValueType);

		Assert.AreEqual(6, type.SizeOf());

		// This is odd, and it works if you dynamically build a wrapper instead of using a generic one.
		// However, it turns out we don't need this anyway, we just need spacers.
		Assert.ThrowsException<InvalidProgramException>(() => typeof(Wrapper<>).MakeGenericType(type));
	}
}
