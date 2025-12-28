using System.Runtime.InteropServices;

namespace AlphaBee;

[TestClass]
public sealed class SpanExtensionsTests
{
	[DataTestMethod]
	[DataRow(-1, new UInt64[] { 0 })]
	[DataRow(0, new UInt64[] { UInt64.MaxValue })]
	[DataRow(1, new UInt64[] { 2 })]
	[DataRow(0, new UInt64[] { 1 })]
	[DataRow(64, new UInt64[] { 0, UInt64.MaxValue })]
	[DataRow(65, new UInt64[] { 0, UInt64.MaxValue << 1 })]
	[DataRow(64, new UInt64[] { 0, 1 })]
	[DataRow(-1, new UInt64[] { 0, 0 })]
	public void TestIndexOfBitOne(Int32 expected, UInt64[] words)
	{
		Assert.AreEqual(expected, words.AsSpan().TryIndexOfBitOne());
	}

	[DataTestMethod]
	[DataRow(0, new UInt64[] { 0 })]
	[DataRow(-1, new UInt64[] { UInt64.MaxValue })]
	[DataRow(0, new UInt64[] { ~(UInt64.MaxValue >> 1) })]
	public void TestIndexOfBitZero(Int32 expected, UInt64[] words)
	{
		Assert.AreEqual(expected, words.AsSpan().IndexOfBitZero());
	}

	[TestMethod]
	public void TestByteAccess()
	{
		var value = 42ul;

		Assert.AreEqual(42, value.AtByte(0));

		ref var b = ref value.AtByte(0);

		b = 43;

		Assert.AreEqual(43, value.AtByte(0));
	}

	[TestMethod]
	public void TestMemoryMarshal()
	{
		var value = 42ul;

		var span = MemoryMarshal.CreateSpan(ref value, 1);

		span[0] = 43;

		Assert.AreEqual(43ul, value);

		var bytes = MemoryMarshal.AsBytes(span);

		bytes[0] = 44;

		Assert.AreEqual(44ul, value);
	}
}
