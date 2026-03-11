using AlphaBee.Layouts;
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
	ObjectHeader Header { get; set; }

	PropNo PropNo { get; set; }

	Int32 Offset { get; set; }

	String? ClrName { get; set; }

	TypeNo TypeNo => Header.TypeNo;

	ObjectExtent Extent => Header.Extent;

	OffsetExtent OffsetExtent => new(Extent, Offset);
}

[StructLayout(LayoutKind.Sequential)]
public struct PropertyDescriptionLayout
{
	public ObjectHeader Header;

	public PropNo PropNo;

	public Int32 Offset;

	public Int64 ClrName;
}

[PeachLayout(typeof(TypeDescriptionLayout))]
public interface ITypeDescription
{
	ObjectHeader Header { get; set; }

	String? ClrName { get; set; }

	Object?[]? Properties { get; set; }

	Object? RootInstance { get; set; }

	TypeNo TypeNo => Header.TypeNo;
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
