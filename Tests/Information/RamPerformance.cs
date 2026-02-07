using AlphaBee;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Tests.Information.RamPerformance;

namespace Tests.Information;

[TestClass]
public class RamPerformance
{
	[TestMethod]
	public void Test()
	{
		IFixture[] cases = [
			new Fixture<RandomPreparer>(12),
			new Fixture<RandomPreparer>(21),
			new Fixture<RandomPreparer>(30),
			//new Fixture<LinearPreparer>(),
			//new Fixture<LinearPreparer>(),
			//new Fixture<LinearPreparer>()
			];

		foreach (var test in cases)
		{
			test.Test();
		}
	}

	public interface IFixture
	{
		void Test();

		int Size { get; }

		int LineSize => 64;

		int LineCount => Size / LineSize;

		int WordSize => Unsafe.SizeOf<int>();

		int WordsPerLine => LineSize / WordSize;

		int WordCount => Size / WordSize;
	}

	public class Fixture<PreparerT> : IFixture
		where PreparerT : struct, IPreparer
	{
		static PreparerT preparer = default;

		Byte[] mem;

		public int Size => mem.Length;

		public Fixture(Int32 sizeLog2)
		{
			mem = new Byte[1 << sizeLog2];
		}

		public void Test()
		{
			var span = mem.AsSpan().InterpretAs<Int32>();

			span.Clear();

			preparer.Prepare(span, this);
			//DebugPrintSpace(span);

			GC.Collect();

			var sw = new Stopwatch();
			sw.Start();

			Run(span, 1 << 23);

			sw.Stop();

			Console.WriteLine($"{typeof(PreparerT).Name} {Size} {sw.ElapsedMilliseconds}ms");
		}

		void DebugPrintSpace(Span<Int32> span)
		{
			for (var i = 0; i < span.Length; ++i)
			{
				Console.Write($" {span[i]}");
				if (i % 16 == 15)
				{
					Console.WriteLine();

					if (i > 60)
					{
						Console.WriteLine("...");

						return;
					}
				}
			}
		}

		void Run(Span<Int32> span, Int32 count)
		{
			Int32 i = 0;
			do
			{
				i = span[i];
			}
			while (--count > 0);
		}
	}

	public interface ISpace
	{
		int Size { get; }

		int LineSize => 64;

		int Lines => Size / LineSize;

		int WordsPerLine => LineSize / Unsafe.SizeOf<int>();

		int Words => Size / Unsafe.SizeOf<int>();

		uint M => (uint)Lines - 1;

		uint A { get; }

		uint C { get; }

		uint GetNextLine(uint previous)
		{
			return (A * previous + C) % M;
		}
	}

	public struct Space4K : ISpace
	{
		public int Size => 1 << 12; // 6 in lines

		public uint M => 61;

		public uint A => 17;

		public uint C => 0;
	}

	public struct Space2M : ISpace
	{
		public int Size => 1 << 21; // 15 in lines

		public uint M => (1 << 13) - 1;

		public uint A => (1 << 11) - 3;

		public uint C => 0;
	}

	public struct Space1G : ISpace
	{
		public int Size => 1 << 30; // 24 in lines

		public uint M => (1 << 24) - 1;

		public uint A => 1103515245;

		public uint C => 1013;
	}

	public interface IPreparer
	{
		void Prepare(Span<int> span, IFixture fixture);
	}

	public struct RandomPreparer : IPreparer
	{
		public void Prepare(Span<Int32> span, IFixture fixture)
		{
			var lineCount = fixture.LineCount;

			var random = new Random();

			var remainingLines = Enumerable.Range(1, lineCount - 1).ToArray();

			var remainingLinesCount = lineCount - 1;

			var currentI = 0;

			do
			{
				var p = random.Next(remainingLinesCount);
				var i = remainingLines[p];
				remainingLines[p] = remainingLines[remainingLinesCount - 1];
				--remainingLinesCount;

				currentI = span[currentI] = i * fixture.WordsPerLine;
			}
			while (remainingLinesCount > 0);

			span[currentI] = 0;
		}
	}

	public struct LinearPreparer : IPreparer
	{
		public void Prepare(Span<Int32> span, IFixture fixture)
		{
			for (var i = 0; i < span.Length; i += fixture.WordsPerLine)
			{
				span[i] = (i + fixture.WordsPerLine) % fixture.WordCount;
			}
		}
	}
}
