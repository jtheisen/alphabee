using Alphabee;
using AlphaBee.Layouts.Structs;
using Moldinium.Baking;

namespace AlphaBee;

[TestClass]
public class BakingTests
{
	public interface IFoo
	{
		int Integer { get; set; }
	}

	[TestMethod]
	public void TestBaking()
	{
		var configuration = BakeryConfiguration.Create(typeof(PeachyPropertyImplementation<>)) with { MakeValue = true };

		var bakery = configuration.CreateBakery("test");

		var target = bakery.Create<IFoo>();

		var type = target.GetType();

		Assert.IsTrue(type.IsValueType);

		Console.WriteLine(type.GetLayoutFields().Stringify());

		//var fooUser = typeof(FooUser<>).MakeGenericType(type).CreateInstance<IFooUser>();

		// Still have some defaults that blow up the size
		//Assert.AreEqual(8, fooUser.Size);
	}
}
