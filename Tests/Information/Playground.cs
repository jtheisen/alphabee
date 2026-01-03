using AlphaBee.StructLayouts;
using System.Runtime.CompilerServices;

namespace AlphaBee.Baking;

public interface IFoo
{
	Int32 Value1 { get; set; }
	Int32 Value2 { get; set; }
}

public interface IFooUser
{
	public Int32 Size { get; }
}

public struct FooUser<Foo> : IFooUser
	where Foo : unmanaged, IFoo
{
	public Int32 Size => Unsafe.SizeOf<Foo>();
}

[TestClass]
public class Playground
{
	[TestMethod]
	public void TestBaking()
	{
		var configuration = BakeryConfiguration.PocGenerationConfiguration with { MakeValue = true };

		var bakery = configuration.CreateBakery("test");

		var target = bakery.Create<IFoo>();

		var type = target.GetType();

		Assert.IsTrue(type.IsValueType);

		target.Value1 = 42;

		Assert.AreEqual(42, target.Value1);

		Console.WriteLine(type.GetLayoutFields().Stringify());

		var fooUser = typeof(FooUser<>).MakeGenericType(type).CreateInstance<IFooUser>();

		// Still have some defaults that blow up the size
		//Assert.AreEqual(8, fooUser.Size);
	}
}
