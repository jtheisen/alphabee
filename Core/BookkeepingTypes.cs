using System.Runtime.InteropServices;

namespace AlphaBee;

[PeachLayout(typeof(HiveRoot))]
public interface IHiveRoot
{
	Object?[]? TypeDescriptions { get; set; }
}

public struct HiveRoot
{
	public Int64 TypeDescriptions;
}

[PeachLayout(typeof(PropertyDescriptionLayout))]
public interface IPropertyDescription
{
	public Int32 PropertyNo { get; set; }

	public TypeNo TypeNo { get; set; }

	public Int32 Offset { get; set; }

	public Int32 Size { get; set; }

	public String? ClrName { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct PropertyDescriptionLayout
{
	public Int32 PropertyNo;

	public TypeNo TypeNo;

	public Int32 Offset;

	public Int32 Size;

	public Int64 ClrName;
}

[PeachLayout(typeof(TypeDescriptionLayout))]
public interface ITypeDescription
{
	Int32 No { get; set; }

	Int32 Size { get; set; }

	String? ClrName { get; set; }

	Object?[]? Properties { get; set; }

	Object? RootInstance { get; set; }
}

[StructLayout(LayoutKind.Explicit)]
public struct TypeDescriptionLayout
{
	[FieldOffset(0)]
	public Int32 No;

	[FieldOffset(4)]
	public Int32 Size;

	[FieldOffset(8)]
	public Int64 ClrName;

	[FieldOffset(16)]
	public Int64 Properties;

	[FieldOffset(24)]
	public Int64 RootInstance;
}
