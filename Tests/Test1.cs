namespace AlphaBee;

[TestClass]
public sealed class MiscTests
{
	[DataTestMethod]
	[DataRow(-1, new UInt64[] { 0 })]
	[DataRow(0, new UInt64[] { UInt64.MaxValue })]
	[DataRow(62, new UInt64[] { 2 })]
	[DataRow(63, new UInt64[] { 1 })]
	[DataRow(64, new UInt64[] { 0, UInt64.MaxValue })]
	[DataRow(65, new UInt64[] { 0, UInt64.MaxValue >> 1 })]
	[DataRow(127, new UInt64[] { 0, 1 })]
	[DataRow(-1, new UInt64[] { 0, 0 })]
	public void TestIndexOfBitOne(Int32 expected, UInt64[] words)
	{
		Assert.AreEqual(expected, words.AsSpan().IndexOfBitOne());
	}

	[DataTestMethod]
	[DataRow(0, new UInt64[] { 0 })]
	[DataRow(-1, new UInt64[] { UInt64.MaxValue })]
	[DataRow(1, new UInt64[] { ~(UInt64.MaxValue >> 1) })]
	public void TestIndexOfBitZero(Int32 expected, UInt64[] words)
	{
		Assert.AreEqual(expected, words.AsSpan().IndexOfBitZero());
	}
}
