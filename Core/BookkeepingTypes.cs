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

	public PropertyEntry PropertyEntry => new(Header, PropNo, Offset);

	public void Deconstruct(out ObjectHeader tae, out PropNo propNo, out Int32 offset)
	{
		tae = Header;
		propNo = PropNo;
		offset = Offset;
	}
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
}

[StructLayout(LayoutKind.Sequential)]
public struct TypeDescriptionLayout
{
	public ObjectHeader Header;

	public Int64 ClrName;

	public Int64 Properties;

	public Int64 RootInstance;
}
