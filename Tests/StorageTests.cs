namespace AlphaBee;

[TestClass]
public class StorageTests
{
	[TestMethod]
	public void TestStorage()
	{
		using var storage = new Storage(pageSize => new MemoryMappedFileStorageImplementation("test.ab", pageSize), true);

		var expectedFirstIndexPageNo = UInt64Page.ContentLength * 64;

		for (var i = 1ul; i < expectedFirstIndexPageNo; ++i)
		{
			Assert.AreEqual(storage.PageSize * i, storage.AllocatePageOffset());
		}

		Assert.AreEqual(storage.PageSize * expectedFirstIndexPageNo + 1, storage.AllocatePageOffset());
	}
}
