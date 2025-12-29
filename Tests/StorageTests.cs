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

		var uhp = Constants.UnaccountedHeaderPages.ToUInt64();

		var leafPageBits = 1ul * (4096 - 256) * 8;

		for (var i = 1ul; i < leafPageBits - uhp; ++i)
		{
			Assert.AreEqual(uhp + i, storage.AllocatePageNo());
		}

		var allocatedAfterFirstIndexPageNo = storage.AllocatePageNo();

		Assert.AreEqual(uhp + 1 + leafPageBits, allocatedAfterFirstIndexPageNo);

		Assert.AreEqual("p1", storage.GetCharPairAtNo(uhp + leafPageBits));

		Assert.AreEqual(uhp + leafPageBits + 1, allocatedAfterFirstIndexPageNo);

		// We've already allocated two - one being the last index page.
		for (var i = 1ul; i < leafPageBits - 1; ++i)
		{
			var pageNo = storage.AllocatePageNo();

			Assert.AreEqual(allocatedAfterFirstIndexPageNo + i, pageNo);
		}

		var allocatedAfterSecondIndexPageNo = storage.AllocatePageNo();

		Assert.AreEqual(leafPageBits * 2 + 1, allocatedAfterSecondIndexPageNo);

		Assert.AreEqual("p0", storage.GetCharPairAtNo(leafPageBits * 2));
	}
}
