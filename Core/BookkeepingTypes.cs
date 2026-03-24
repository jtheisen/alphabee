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
	TypeNo PropTypeNo { get; set; }

	PropNo PropNo { get; set; }

	OffsetExtent OffsetExtent { get; set; }

	String? ClrName { get; set; }

	Int32 ArrayLevel { get; set; }

	public PropertyEntry GetPropertyEntry() => new(PropTypeNo, PropNo, OffsetExtent);

	public void Deconstruct(out TypeNo propertyTypeNo, out PropNo propNo, out Int32 offset, out ObjectExtent extent)
	{
		propertyTypeNo = PropTypeNo;
		propNo = PropNo;
		offset = OffsetExtent.Offset;
		extent = OffsetExtent.Extent;
	}
}

[StructLayout(LayoutKind.Sequential)]
public struct PropertyDescriptionLayout
{
	public TypeNo PropTypeNo;	

	public PropNo PropNo;

	public OffsetExtent OffsetExtent;

	public Int64 ClrName;

	public Int32 ArrayLevel;
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
