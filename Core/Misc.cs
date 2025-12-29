namespace AlphaBee;

[InlineArray(8)]
public struct UInt512
{
	private ulong _element0;
}

public static class Extensions
{
	public static UInt64 AllocatePageNo(this Storage storage)
	{
		return storage.AllocatePageOffset() / storage.PageSize;
	}

	public static Span<Byte> GetPageSpanAtNo(this Storage storage, UInt64 pageNo)
	{
		return storage.GetPageSpanAtOffset(pageNo * storage.PageSize);
	}

	public static String GetCharPairAtNo(this Storage storage, UInt64 pageNo)
	{
		return storage.GetCharPairAtOffset(pageNo * storage.PageSize);
	}

	public static String GetCharPairAtOffset(this Storage storage, UInt64 offset)
	{
		var span = storage.GetPageSpanAtOffset(offset);

		var header = new PageHeader(ref span.InterpretAs<UInt64>()[0]);

		return header.PageCharPair;
	}
}