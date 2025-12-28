namespace AlphaBee;

public class Constants
{
	public const Int32 BitsPerByteLog2 = 3;
	public const Int32 BitsPerByte = 1 << BitsPerByteLog2;
	public const Int32 WordSizeLog2 = 3;
	public const Int32 WordSize = 1 << WordSizeLog2;

	public const Int32 PageSizeLog2 = 12;
	public const UInt64 PageSize = 1 << PageSizeLog2;
	public const Int32 PageSize32 = (Int32)PageSize;
}



