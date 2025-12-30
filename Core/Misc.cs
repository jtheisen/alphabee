using System.Reflection;

namespace AlphaBee;

public class InternalErrorException : Exception
{
	public InternalErrorException()
	{
	}

	public InternalErrorException(String message)
		: base(message)
	{
	}
}

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

	public static String FormatAsDecimalLog(this Double value)
		=> $"1E{Math.Log10(value):#.00}";

	public static String FormatAsBinaryLog(this Double value)
		=> $"1B{Math.Log2(value):#.00}";

	public static Boolean IsOptimized(this Assembly assembly)
	{
		var attr = assembly.GetCustomAttribute<DebuggableAttribute>();

		return attr == null || !attr.IsJITOptimizerDisabled;
	}
}


[DebuggerDisplay("{ToString()}")]
public ref struct BitsWord
{
	ref UInt64 word;

	public BitsWord(ref UInt64 word)
	{
		this.word = ref word;
	}

	public override String ToString()
	{
		return word.ToBrailleString();
	}
}
