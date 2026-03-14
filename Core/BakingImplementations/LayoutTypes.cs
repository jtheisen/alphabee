namespace AlphaBee.Layouts;

[StructLayout(LayoutKind.Sequential)]
public record struct OffsetExtent(ObjectExtent Extent, Int32 Offset)
{
	public override String ToString()
	{
		return $"{Offset}: {Extent.Size} bytes";
	}
}
