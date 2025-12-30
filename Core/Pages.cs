namespace AlphaBee;

public enum PageType
{
	Unkown = '.',
	Page = 'p',
	Field = 'f'
}

public interface IPageLayout
{
	Int32 Size { get; }
}

public interface IFieldPageLayout : IPageLayout
{
	Int32 LeadSize { get; }

	Int32 LeadWords { get; }

	Int32 FieldLength { get; }

	UInt64 AllPattern { get; }
}

public static class PageExtensions
{
	public static void ValidateWordPage(this IndexPage page, Boolean asBitFieldLeaf)
	{
		page.Validate();

		for (var i = 0; i < 64; ++i)
		{
			if (page.GetUsedBit(i))
			{
				var word = page.Use(i, out _);

				Debug.Assert(word != 0);

				if (asBitFieldLeaf)
				{
					Debug.Assert(word != UInt64.MaxValue || page.GetFullBit(i));
				}
			}
		}
	}
}
