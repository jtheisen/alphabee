using AlphaBee.Layouts.Structs;
using System.Runtime.InteropServices;

namespace AlphaBee;

[TestClass]
public class LayoutSpacerTests
{
	LayoutSpacerBakery bakery = new();

	[StructLayout(LayoutKind.Sequential)]
	public struct Foo<T>
		where T : unmanaged
	{
		public T First;
		public T Second;
	}

	[DataTestMethod]
	[DataRow(1)]
	[DataRow(2)]
	[DataRow(3)]
	[DataRow(4)]
	[DataRow(5)]
	[DataRow(6)]
	[DataRow(7)]
	[DataRow(8)]
	[DataRow(9)]
	[DataRow(16)]
	[DataRow(17)]
	[DataRow(256)]
	[DataRow(4096)]
	public void TestSpacers(Int32 size)
	{
		var type = bakery.EnsureSpacer(size);

		var actualSize = type.SizeOf();

		Assert.AreEqual(size, actualSize);

		var fooType = typeof(Foo<>).MakeGenericType(type);

		var fieldEntries = fooType.GetLayoutFields().ToArray();

		var offset1 = fieldEntries[1].Layout.offset;

		Assert.AreEqual(size, offset1);
	}

	[TestMethod]
	public void TestLargestSpacer()
	{
		var size = Int32.MaxValue;

		var type = bakery.EnsureSpacer(size);

		var actualSize = type.SizeOf();

		Assert.AreEqual(size, actualSize);

		Assert.ThrowsException<TypeLoadException>(() => typeof(Foo<>).MakeGenericType(type));
	}
}
