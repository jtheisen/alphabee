namespace AlphaBee.Information;

[TestClass]
public class Playground
{
	interface IFoo<T>
	{
		static String Get(String name) => nameof(T);
	}

	[TestMethod]
	public void Test()
	{
		Assert.AreEqual("T", IFoo<Int32>.Get(""));
	}
}
