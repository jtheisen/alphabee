using FsCheck.Xunit;
using System.Buffers.Binary;

namespace AlphaBee;

public class UnitTest1
{
	[Property]
	public bool ReverseOfReverseIsOriginal(UInt64[] words)
	{
		var i = words.AsSpan().IndexOfBitOne();
		
		if (i < 0) return true;

		var word = words[i / 64];

		var reversedWord = BinaryPrimitives.ReverseEndianness(word);

		var shift = i % 64;

		return (reversedWord >> shift) == 1;
	}
}
