global using BitFieldPage = AlphaBee.FieldPage<System.UInt64, AlphaBee.UInt512>;

namespace AlphaBee;

[TestClass]
public class StorageTests
{
	[TestMethod]
	public void TestIndexPages()
	{
		using var storage = Storage.CreateTestStorage();

		var page = new BitFieldPage(storage.GetPageSpanAtOffset(storage.Header.IndexRootOffset));

		var firstWord = new BitsWord(ref page.At(0));

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
	}

	[TestMethod]
	public void TestStorage()
	{
		using var storage = Storage.CreateTestStorage();

		var expectedFirstIndexPageNo = 1ul * (4096 - 256) * 8;
		var expectedFirstIndexPageOffset = storage.PageSize * expectedFirstIndexPageNo;

		for (var i = 1ul; i < expectedFirstIndexPageNo; ++i)
		{
			Assert.AreEqual(storage.PageSize * i, storage.AllocatePageOffset());
		}

		var allocatedAfterFirstIndexPageOffset = storage.AllocatePageOffset();

		Assert.AreNotEqual(expectedFirstIndexPageOffset, allocatedAfterFirstIndexPageOffset);

		Assert.AreEqual("p1", storage.GetCharPairAtOffset(expectedFirstIndexPageOffset));

		Assert.AreEqual(expectedFirstIndexPageOffset + storage.PageSize, allocatedAfterFirstIndexPageOffset);
	}
}
