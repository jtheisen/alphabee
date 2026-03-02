namespace AlphaBee.Layouts;

public struct LayoutEntry
{
	public readonly Int32 offset;
	public readonly Int32 size;

	public LayoutEntry(Int32 offset, Int32 size)
	{
		this.offset = offset;
		this.size = size;
	}
}
