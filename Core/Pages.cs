namespace AlphaBee;

public enum PageType
{
	Unkown = '.',
	Page = 'p',
	Field = 'f'
}

public interface IPage
{
	void Init(PageType pageType, Int32 pageDepth);
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
