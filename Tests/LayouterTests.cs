using AlphaBee.Layouts;

namespace AlphaBee;

[TestClass]
public class LayouterTests
{
	public interface IBase<T>
	{
		T BaseP { get; set; }
	}

	public interface IFoo<T1, T2> : IBase<T2>
	{
		T1 P1 { get; set; }
		T2 P2 { get; set; }
	}

	[TestMethod]
	public void TestBitArray()
	{
		var bits = new System.Collections.BitArray(1);

		bits[0] = true;

		Assert.AreEqual(1, bits.Length);

		Assert.ThrowsException<ArgumentOutOfRangeException>(() => bits[1] = true);
	}

	[DataTestMethod]
	[DataRow(typeof(IFoo<Int16, Int32>), 0)]
	//[DataRow(typeof(IFoo<Int32, Int16>), 0)]
	//[DataRow(typeof(IFoo<Int32, Byte>), 0)]
	//[DataRow(typeof(IFoo<Byte, Int32>), 0)]
	//[DataRow(typeof(IFoo<Int16, Int32>), 8)]
	//[DataRow(typeof(IFoo<Int32, Int16>), 8)]
	//[DataRow(typeof(IFoo<Int32, Byte>), 8)]
	//[DataRow(typeof(IFoo<Byte, Int32>), 8)]
	public void TestLayoutValidates(Type type, Int32 offset)
	{
		var layouter = new NaiveLayouter();

		var metadata = new LayouterMetdataProvider();

		var layout = layouter.GetLayout(new(type, offset, metadata));

		Assert.IsFalse(layout.Entries.Any(e => e.Layout.offset < offset));
	}
}
