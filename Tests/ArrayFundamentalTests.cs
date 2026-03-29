namespace AlphaBee;

[TestClass]
public class ArrayFundamentalTests
{
	[TestMethod]
	public void TestArrayLevel()
	{
		Assert.AreEqual(1, IBeeArray<Int32>.ArrayLevel);
		Assert.AreEqual(2, IBeeArray<IBeeArray<Int32>>.ArrayLevel);
		Assert.AreEqual(3, IBeeArray<IBeeArray<IBeeArray<Int32>>>.ArrayLevel);
	}
}
