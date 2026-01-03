namespace AlphaBee.Baking;

public interface IFoo
{
	Int32 Value1 { get; set; }
	Int32 Value2 { get; set; }
}

[TestClass]
public class Playground
{
	[TestMethod]
	public void TestBaking()
	{
		var bakery = BakeryConfiguration.PocGenerationConfiguration.CreateBakery("test");

		var target = bakery.Create<IFoo>();

		target.Value1 = 42;

		Console.WriteLine($"Got: {target.Value1}");
	}
}
