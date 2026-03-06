namespace AlphaBee;

[TestClass]
public class ArrayListTests
{
	[TestMethod]
	public void TestExceptions()
	{
		var list = new ArrayList<Int32>(4);

		Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[0]);
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[-1]);
		Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[Int32.MaxValue]);
	}

	[TestMethod]
	public void TestCorrectnessOfCopy()
	{
		var list = new ArrayList<Int32>(4);

		Assert.AreEqual(4, list.Capacity);

		var n = 24;

		for (var i = 0; i < n; ++i)
		{
			list.Add(i * 3);

			for (var j = 0; j < i; ++j)
			{
				Assert.AreEqual(j * 3, list[j]);
			}
		}
	}

	[TestMethod]
	public void TestReferenceTypes()
	{
		var list = new ArrayList<String?>();

		Assert.AreEqual(0, list.Count);

		list.Add("foo");

		Assert.AreEqual(list[0], "foo");

		list.Add("bar");

		Assert.AreEqual(list[1], "bar");

		list[0] = "baz";

		Assert.AreEqual(list[0], "baz");

		Assert.AreEqual(2, list.Count);

		Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[2]);
	}

	[TestMethod]
	public void TestClear()
	{
		var list = new ArrayList<String?>
		{
			"foo", "bar", "baz"
		};

		Assert.AreEqual(3, list.Count);

		list.Clear();

		Assert.AreEqual(0, list.Count);

		Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[0]);

		list.Add("one");

		Assert.AreEqual(1, list.Count);

		Assert.AreEqual("one", list[0]);

		Assert.ThrowsException<ArgumentOutOfRangeException>(() => list[1]);
	}
}
