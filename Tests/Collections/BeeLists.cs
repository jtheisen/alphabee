namespace AlphaBee.Collections;

[TestClass]
public class BeeLists : HiveTestBase
{
	[TestMethod]
	public void Test()
	{
		var list = context.New<BeeList<Int32>>();

		Assert.AreEqual(0, list.count);

		//list.Add(42);

		Assert.AreEqual(1, list.count);
	}
}
