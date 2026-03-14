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
		var type = bakery.EnsureSpacerType(size);

		var actualSize = type.SizeOf();

		Assert.AreEqual(size, actualSize);

		Activator.CreateInstance(type);

		Assert.IsTrue(LayoutSpacerBakery.IsSpacer(type, out var size2, out var subSize2));

		Assert.AreEqual(size, size2);
		Assert.AreEqual(1, subSize2);

		var fooType = typeof(Foo<>).MakeGenericType(type);

		var fieldEntries = fooType.GetLayoutFields().ToArray();

		var offset = fieldEntries[1].Offset;

		Assert.AreEqual(size, offset);
	}

	[DataTestMethod]
	[DataRow(8, 1)]
	[DataRow(8, 2)]
	[DataRow(8, 3)]
	[DataRow(8, 4)]
	public void TestSpacerTypes(Int32 size, Int32 subSize)
	{
		var type = bakery.EnsureSpacerType(size, subSize);

		Activator.CreateInstance(type);

		Assert.IsTrue(LayoutSpacerBakery.IsSpacer(type, out var size2, out var subSize2));

		Assert.AreEqual(size, size2);
		Assert.AreEqual(subSize, subSize2);
	}

	[TestMethod]
	public void TestLargestSpacer()
	{
		var size = Int32.MaxValue;

		var type = bakery.EnsureSpacerType(size);

		var actualSize = type.SizeOf();

		Assert.AreEqual(size, actualSize);

		Assert.ThrowsException<TypeLoadException>(() => typeof(Foo<>).MakeGenericType(type));
	}
}
