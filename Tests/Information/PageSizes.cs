using Humanizer;

namespace AlphaBee.Information;

public record PageLayout(Int32 PageSize, Int32 IndexBitSize, Int32 ItemSize, Int32 HeaderSize = 0)
{
	public Int32 PaddedItemSize => 1 << ItemSize.ToUInt64().Log2Ceil();

	public Int32 LeadBitSize => 2 * IndexBitSize + HeaderSize * 8;

	public Int32 LeadSize => (Int32)Math.Ceiling(LeadBitSize / 8.0);

	public Int32 ContentSize => PageSize - LeadSize;

	public Int32 ContentBitSize => ContentSize * Constants.BitsPerByte;

	public Int32 FieldLength => Math.Min(IndexBitSize, ContentSize / PaddedItemSize);

	public Int32 UnusedIndexBitSize => IndexBitSize - FieldLength;

	public Int32 UnusedLeadBitSize => UnusedIndexBitSize * 2;

	public Int32 UnusedLeadSize => UnusedLeadBitSize / 8;

	public Double Efficiency => 1.0 * FieldLength * ItemSize / PageSize;

	public String LeadSizeInfo => $"{HeaderSize * 8}+2x{IndexBitSize} = {LeadBitSize}";

	public UInt64 GetBitSpaceSizeForDepth(Int32 depth)
	{
		var result = (UInt64)ContentBitSize;

		for (var i = 0; i < depth; i++)
		{
			result *= (UInt64)FieldLength;
		}

		return result;
	}

	public String? IsDumb()
	{
		if (UnusedLeadBitSize < 0)
		{
			return "lead doesn't fit";
		}



		return null;
	}

	static String Format(Object pageSize, Object partition, Object leadInfo, Object unusedLeadBitSize, Object contentSize, Object efficiency)
	{
		return $"{pageSize,10}{partition,15}{leadInfo,17}{unusedLeadBitSize,9}{contentSize,15}{efficiency,13:p}";
	}

	public static String GetCaption()
	{
		return Format("page size", "partitioning", "lead bits", "unused", "content size", "efficiency");
	}

	public override String ToString()
	{
		var partition = $"{FieldLength}x{ItemSize.Bytes()}";

		return Format(PageSize.Bytes(), partition, LeadSizeInfo, UnusedLeadBitSize, ContentSize.Bytes(), Efficiency);
	}
}

[TestClass]
public class PageSizes
{
	public void HandleLayouts(Int32 pageSizeLog2, Int32 headerSize = 0)
	{
		Console.WriteLine($"Pages of size {(1 << pageSizeLog2).Bytes()} with {headerSize.Bytes()} header");
		Console.WriteLine(PageLayout.GetCaption());

		for (var itemSizeLog2 = 0; itemSizeLog2 < pageSizeLog2; ++itemSizeLog2)
		{
			var maxIndexBits = 1 << (pageSizeLog2 - itemSizeLog2);

			PageLayout? bestPageLayout = null;

			for (var indexBits = maxIndexBits; indexBits > 0; --indexBits)
			{
				var layout = new PageLayout(1 << pageSizeLog2, indexBits, 1 << itemSizeLog2, headerSize);

				if (layout.UnusedLeadBitSize < 0)
				{
					continue;
				}

				if (bestPageLayout is null || layout.FieldLength > bestPageLayout.FieldLength)
				{
					bestPageLayout = layout;
				}
			}

			if (bestPageLayout is not null)
			{
				Console.WriteLine($"{bestPageLayout}");
			}
			else
			{
				Console.WriteLine($"No page layout for item size {(1 << itemSizeLog2).Bytes()}");
			}
		}
	}

	[TestMethod]
	public void Calculate()
	{
		HandleLayouts(8, 2);
	}

	[TestMethod]
	public void TreeDepths()
	{
		/**
		 * Tree depth can be about 23 for max(UInt64) with 64 byte index pages,
		 * and it's half that for max(UInt32). Such pages have only 7 children
		 * and an index size of 7 bits.
		 * 
		 * When used as heaps, we don't really need random-access, so
		 * page sizes can be small and tree depth high (assuming we
		 * optimize the allocation so that we don't walk the tree each time).
		 * 
		 * When used as fields, we benefit a lot from larger page sizes if
		 * and only if we access truly randomly. This may be something users
		 * want to do, but it's not something we need for the infrastructure.
		 * 
		 * Bottom line: cache-line-sized index page sizes are a
		 * very good baseline.
		 */

		Console.WriteLine($"32;63: {Math.Log(UInt32.MaxValue, 7)}");
		Console.WriteLine($"64;63: {Math.Log(UInt64.MaxValue, 7)}");
	}

}
