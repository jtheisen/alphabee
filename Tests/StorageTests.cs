global using BitFieldPage = AlphaBee.FieldPage<AlphaBee.FieldPageLayout<System.UInt64>>;

namespace AlphaBee;

[TestClass]
public class StorageTests
{
	[TestMethod]
	public void TestIndexPages()
	{
		using var storage = Storage.CreateTestFileStorage();

		var page = new BitFieldPage(storage.GetPageSpanAtOffset(storage.Header.IndexRootOffset));

		var firstWord = new BitsWord(ref page.Get(0));

		Assert.AreEqual(false, page.GetFullBit(0));
		Assert.AreEqual(true, page.GetUsedBit(0));

		Assert.AreEqual(false, page.GetFullBit(1));
		Assert.AreEqual(false, page.GetUsedBit(1));

		// One less because one page is already allocated and
		// one more less because we want to stop one early.
		for (var i = 0; i < 64 - 2; ++i)
		{
			storage.AllocatePageOffset();
		}

		Assert.AreEqual(false, page.GetFullBit(0));

		storage.AllocatePageOffset();

		Assert.AreEqual(true, page.GetFullBit(0));

		Assert.AreEqual(false, page.GetFullBit(1));
		Assert.AreEqual(false, page.GetUsedBit(1));

		storage.AllocatePageOffset();

		Assert.AreEqual(false, page.GetFullBit(1));
		Assert.AreEqual(true, page.GetUsedBit(1));

		for (var i = 0; i < 64 * 64 - 1; ++i)
		{
			storage.AllocatePageOffset();
		}

		Assert.IsTrue(page.IsFull);
	}

	[TestMethod]
	public void TestStorage()
	{
		using var storage = new Storage(pageSize => new MemoryMappedFileStorageImplementation("test.ab", pageSize), true);

		var expectedFirstIndexPageNo = 1ul * (4096 - 4 * 8) * 8;

		for (var i = 1ul; i < expectedFirstIndexPageNo; ++i)
		{
			Assert.AreEqual(storage.PageSize * i, storage.AllocatePageOffset());
		}

		Assert.AreEqual(storage.PageSize * expectedFirstIndexPageNo + 1, storage.AllocatePageOffset());
	}
}
