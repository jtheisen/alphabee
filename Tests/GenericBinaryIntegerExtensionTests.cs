using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBee;

[TestClass]
public class GenericBinaryIntegerExtensionTests
{
	[DataTestMethod]
	[DataRow(0, 0)]
	[DataRow(0, 1)]
	[DataRow(1, 2)]
	[DataRow(1, 3)]
	[DataRow(2, 4)]
	[DataRow(30, Int32.MaxValue)]
	public void TestLog2(Int32 expected, Int32 value)
	{
		Assert.AreEqual(expected, value.Log2());
	}

	[DataTestMethod]
	[DataRow(1, 1)]
	[DataRow(1, 2)]
	[DataRow(2, 3)]
	[DataRow(2, 4)]
	[DataRow(31, Int32.MaxValue)]
	public void TestLog2Ceil(Int32 expected, Int32 value)
	{
		Assert.AreEqual(expected, value.Log2Ceil());
	}

	[DataTestMethod]
	[DataRow(0, 0)]
	[DataRow(1, 2)]
	[DataRow(2, 2)]
	[DataRow(3, 4)]
	[DataRow(4, 4)]
	[DataRow(5, 8)]
	[DataRow(7, 8)]
	[DataRow(100, 128)]
	[DataRow(500, 512)]
	public void TestCeilToPowerOfTwo(Int32 value, Int32 expected)
	{
		Assert.AreEqual(expected, value.CeilToPowerOfTwo());
	}
}
