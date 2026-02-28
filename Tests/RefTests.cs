namespace AlphaBee;

[TestClass]
public class RefTests
{
	[TestMethod]
	[DataRow(TypeCode.Decimal, true, true)]
	[DataRow(TypeCode.Decimal, false, false)]
	[DataRow(TypeCode.Decimal, true, false)]
	[DataRow(TypeCode.Decimal, false, true)]
	[DataRow(TypeCode.Object, true, true)]
	[DataRow(TypeCode.Object, false, false)]
	public void TestTypeByteEdgeCondition(TypeCode code, Boolean isSpan, Boolean isNullable)
	{
		var typeByte = new TypeByte(code, isSpan, isNullable);

		Assert.IsTrue(typeByte.value >= 0, "Value is negative");

		Assert.AreEqual(code, typeByte.Code);

		Assert.AreEqual(isSpan, typeByte.IsSpan);

		Assert.AreEqual(isNullable, typeByte.IsNullable);
	}
}
