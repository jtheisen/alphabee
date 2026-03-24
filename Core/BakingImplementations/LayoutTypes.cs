namespace AlphaBee.Layouts;

[StructLayout(LayoutKind.Sequential)]
public record struct OffsetExtent(Int32 Offset, ObjectExtent Extent)
{
	public Int32 Size => Extent.Size;
	public Int32 Length => Extent.Length;

	public override String ToString()
	{
		return $"{Offset}: {Extent.Size} bytes";
	}
}
