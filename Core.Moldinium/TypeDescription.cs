using System.Runtime.InteropServices;

namespace AlphaBee;

[AttributeUsage(AttributeTargets.Interface)]
public class PeachLayoutAttribute : Attribute
{
	public Type LayoutType { get; }

	public PeachLayoutAttribute(Type layoutType) => LayoutType = layoutType;
}

[PeachLayout(typeof(TypeDescriptionLayout))]
public interface ITypeDescription
{
	String?[]? Names { get; set; }

	ITypeDescription?[]? Descriptions { get; set; }

	TypeRef[] TypeRefs { get; set; }

	Int32[] Offsets { get; set; }
}

[StructLayout(LayoutKind.Explicit)]
public struct TypeDescriptionLayout
{
	[FieldOffset(0)]
	public Int64 Names;

	[FieldOffset(8)]
	public Int64 Descriptions;

	[FieldOffset(8)]
	public Int64 TypeRefs;

	[FieldOffset(16)]
	public Int64 Offsets;
}