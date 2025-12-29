namespace AlphaBee;

[InlineArray(8)]
public struct UInt512
{
	private ulong _element0;
}

public static class Extensions
{
	public static Span<Byte> GetCharPairAtNo(this Storage storage, UInt64 pageNo)
	{
		return storage.GetPageSpanAtOffset(storage.PageSize * pageNo);
	}

	public static String GetCharPairAtOffset(this Storage storage, UInt64 offset)
	{
		var span = storage.GetPageSpanAtOffset(offset);

		var header = new PageHeader(ref span.InterpretAs<UInt64>()[0]);

		return header.PageCharPair;
	}
}