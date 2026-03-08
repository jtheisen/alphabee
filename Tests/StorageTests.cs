namespace AlphaBee;

[TestClass]
public class StorageTests
{
	const Int32 reserved = 12;

	AbstractTestStorage storage = null!;

	[TestInitialize]
	public void Setup()
	{
		storage = new TestStorage(reserved: reserved);
	}

	[TestMethod]
	public void TestArray()
	{
		var typeByte = new TypeByte(TypeCode.Int16, true, false);

		var header = ObjectHeader.CreateForStruct<Int16>(typeByte, 4);

		storage.AllocateArrayObject<Int16>(header, out var address, out var content);

		Assert.AreEqual(reserved, address);

		Assert.AreEqual(header, storage.GetHeader(reserved));

		Assert.AreEqual(reserved + ObjectHeader.Size + 4 * Unsafe.SizeOf<Int16>(), storage.Position);
	}
}
