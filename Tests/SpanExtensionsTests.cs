using System.Runtime.InteropServices;

namespace AlphaBee;

[TestClass]
public sealed class SpanExtensionsTests
{
	[DataTestMethod]
	[DataRow(0)]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(64)]
	[DataRow(511)]
	public void TestUInt512SpecificBits(Int32 i)
	{
		var bits = default(UInt512);

		Assert.AreEqual(512, bits.IndexOfBitOne());

		Assert.IsTrue(bits.IsAllZero());

		bits.SetBit(i, true);

		Assert.IsFalse(bits.IsAllZero());

		Assert.AreEqual(true, bits.GetBit(i));

		Assert.AreEqual(i, bits.IndexOfBitOne());

		bits.SetBit(i, false);

		Assert.IsTrue(bits.IsAllZero());
	}

	[TestMethod]
	public void TestUInt512General()
	{
		var bits = default(UInt512);

		for (var i = 0; i < 512; i++)
		{
			Assert.IsFalse(bits.GetBit(i));

			bits.Allocate();

			Assert.AreEqual(i + 1, bits.IndexOfBitZero());

			Assert.IsTrue(bits.GetBit(i));
		}

		Assert.AreEqual("⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿", bits.ToBrailleString());
	}

	[DataTestMethod]
	[DataRow(true, 0ul, 0ul)]
	[DataRow(true, 1ul, 1ul)]
	[DataRow(false, 1ul, 0ul)]
	[DataRow(true, 0ul, 1ul)]
	[DataRow(true, 0ul << 32, 0ul << 32)]
	[DataRow(true, 1ul << 32, 1ul << 32)]
	[DataRow(false, 1ul << 32, 0ul << 32)]
	[DataRow(true, 0ul << 32, 1ul << 32)]
	public void TestBitImplies(Boolean expected, UInt64 condition, UInt64 conclusion)
	{
		Assert.AreEqual(expected, condition.BitImplies(ref conclusion));
	}

	[DataTestMethod]
	[DataRow(64, new UInt64[] { 0 })]
	[DataRow(0, new UInt64[] { UInt64.MaxValue })]
	[DataRow(1, new UInt64[] { 2 })]
	[DataRow(0, new UInt64[] { 1 })]
	[DataRow(64, new UInt64[] { 0, UInt64.MaxValue })]
	[DataRow(65, new UInt64[] { 0, UInt64.MaxValue << 1 })]
	[DataRow(64, new UInt64[] { 0, 1 })]
	[DataRow(128, new UInt64[] { 0, 0 })]
	public void TestIndexOfBitOne(Int32 expected, UInt64[] words)
	{
		Assert.AreEqual(expected, words.AsSpan().IndexOfBitOne());
	}

	[DataTestMethod]
	[DataRow(0, new UInt64[] { 0 })]
	[DataRow(64, new UInt64[] { UInt64.MaxValue })]
	[DataRow(0, new UInt64[] { ~(UInt64.MaxValue >> 1) })]
	public void TestIndexOfBitZero(Int32 expected, UInt64[] words)
	{
		Assert.AreEqual(expected, words.AsSpan().IndexOfBitZero());
	}

	[DataTestMethod]
	[DataRow(-1, -1, 0ul)]
	[DataRow(0, 0, 1ul)]
	[DataRow(1, 1, 2ul)]
	[DataRow(1, 2, 3ul)]
	[DataRow(2, 2, 4ul)]
	[DataRow(2, 3, 6ul)]
	[DataRow(3, 3, 8ul)]
	[DataRow(63, 64, UInt64.MaxValue)]
	public void TestLog2(Int32 expectedFloor, Int32 expectedCeil, UInt64 value)
	{
		Assert.AreEqual(expectedFloor, value.Log2());
		Assert.AreEqual(expectedCeil, value.Log2Ceil());
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
