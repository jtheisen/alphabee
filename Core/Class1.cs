using System.Numerics;

namespace AlphaBee;

public static class Extensions
{
	public static Int32 IndexOfBitCore(this Span<UInt64> words, UInt64 pattern)
	{
		var passed = 0;
		foreach (var word in words)
		{
			var count = BitOperations.LeadingZeroCount(word ^ pattern);

			if (count < 64)
			{
				return passed + count;
			}

			passed += 64;
		}

		return -1;
	}

	public static Int32 IndexOfBitZero(this Span<UInt64> words)
		=> words.IndexOfBitCore(UInt64.MaxValue);

	public static Int32 IndexOfBitOne(this Span<UInt64> words)
		=> words.IndexOfBitCore(0);
}

//ref struct PageHelper
//{
//	Int32 wordsPerObject;

//	Span<Byte> bytes;
//	Span<Int64> words;

//	public void Find()
//	{
//		BitOperations.LeadingZeroCount()
//	}
//}