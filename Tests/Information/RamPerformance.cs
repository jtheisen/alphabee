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
			new Fixture<LinearPreparer>(12),
			new Fixture<LinearPreparer>(21),
			new Fixture<LinearPreparer>(30)
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

		int MemSize => 1 << 30;

		int MemWordCount => MemSize / WordSize;

		int MemLineCount => MemSize / LineSize;
	}

	public class Fixture<PreparerT> : IFixture
		where PreparerT : struct, IPreparer
	{
		static PreparerT preparer = default;

		Byte[] mem;

		IFixture Self => this;

		public int Size { get; }

		public Fixture(Int32 sizeLog2)
		{
			Size = 1 << sizeLog2;

			mem = new Byte[Self.MemSize];
		}

		public void Test()
		{
			var span = mem.AsSpan().InterpretAs<Int32>();

			span.Clear();

			Prepare(span);
			//DebugPrintSpace(span);

			GC.Collect();

			var sw = new Stopwatch();
			sw.Start();

			Run(span, 1 << 23);

			sw.Stop();

			Console.WriteLine($"{typeof(PreparerT).Name} {Size} {sw.ElapsedMilliseconds}ms");
		}

		void Prepare(Span<int> span)
		{
			var i = 0;

			do
			{
				var nextI = i + Self.WordCount;

				if (nextI > Self.MemWordCount - Self.WordCount)
				{
					nextI = 0;
				}

				preparer.Prepare(span[i..(i + Self.WordCount)], i, nextI, this);

				//DebugPrintSpace(span);

				i = nextI;
			}
			while (i != 0);

			//DebugPrintSpace(span);

			var checkCount = Count(span);
			//Console.WriteLine(Self.MemLineCount);

			Trace.Assert(checkCount == Self.MemLineCount);
		}

		void DebugPrintSpace(Span<Int32> span)
		{
			Console.WriteLine("-- span");
			for (var i = 0; i < span.Length; ++i)
			{
				if (i % 16 == 0)
				{
					Console.WriteLine();
					Console.Write($"{i / 16}:");

					if (i > 66 * 16)
					{
						Console.WriteLine("...");

						return;
					}
				}
				Console.Write($" {span[i] / 16}");
			}
			Console.WriteLine();
		}

		Int32 Count(Span<Int32> span)
		{
			Int32 i = 0, count = 0;
			do
			{
				i = span[i];
				count++;
			}
			while (i > 0);
			return count;
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

	public interface IPreparer
	{
		void Prepare(Span<int> span, Int32 offsetI, Int32 nextI, IFixture fixture);
	}

	public struct RandomPreparer : IPreparer
	{
		public void Prepare(Span<Int32> span, Int32 offset, Int32 next, IFixture fixture)
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

				var nextI = i * fixture.WordsPerLine;

				span[currentI] = offset + nextI;

				currentI = nextI;
			}
			while (remainingLinesCount > 0);

			span[currentI] = next;
		}
	}

	public struct LinearPreparer : IPreparer
	{
		public void Prepare(Span<Int32> span, Int32 offset, Int32 next, IFixture fixture)
		{
			for (var i = 0; i < span.Length; i += fixture.WordsPerLine)
			{
				span[i] = offset + (i + fixture.WordsPerLine) % fixture.WordCount;
			}

			span[^fixture.WordsPerLine] = next;
		}
	}
}
