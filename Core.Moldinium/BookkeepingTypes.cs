using System.Runtime.InteropServices;

namespace AlphaBee;

[AttributeUsage(AttributeTargets.Interface)]
public class PeachLayoutAttribute : Attribute
{
	public Type LayoutType { get; }

	public PeachLayoutAttribute(Type layoutType) => LayoutType = layoutType;
}

[PeachLayout(typeof(HiveRoot))]
public interface IHiveRoot
{
	public Object?[]? TypeDescriptions { get; }
}

public struct HiveRoot
{
	public Int64 TypeDescriptions;
}

[PeachLayout(typeof(TypeDescriptionLayout))]
public interface ITypeDescription
{
	Int32 No { get; set; }

	Int32 Size { get; set; }
	
	String? ClrName { get; set; }

	String?[]? ClrNames { get; set; }

	ITypeDescription?[]? Descriptions { get; set; }

	TypeRef[] TypeRefs { get; set; }

	Int32[] Offsets { get; set; }

	Int32[] Sizes { get; set; }
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
	public Int64 ClrNames;

	[FieldOffset(24)]
	public Int64 Descriptions;

	[FieldOffset(24)]
	public Int64 TypeRefs;

	[FieldOffset(32)]
	public Int64 Offsets;

	[FieldOffset(32)]
	public Int64 Sizes;
}