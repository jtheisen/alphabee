using Newtonsoft.Json;

namespace AlphaBee.Information;

[TestClass]
public class Serialization
{
	public interface IFoo
	{
		Int32 X { get; }
	}

	public class ExplicitFoo : IFoo
	{
		Int32 IFoo.X => 3;
	}

	public class ImplicitFoo : IFoo
	{
		public Int32 X => 3;
	}

	public class ImplicitFooWithY : ImplicitFoo
	{
		public Int32 Y => 4;
	}

	[DataTestMethod]
	[DataRow("{}", typeof(ExplicitFoo))]
	[DataRow(@"{""X"":3}", typeof(ImplicitFoo))]
	[DataRow(@"{""Y"":4,""X"":3}", typeof(ImplicitFooWithY))]
	public void Test(String expected, Type type)
	{
		Assert.AreEqual(expected, JsonConvert.SerializeObject(type.CreateInstance<IFoo>()));
	}
}
