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
	public Object?[]? TypeDescriptions { get; set; }
}

public struct HiveRoot
{
	public Int64 TypeDescriptions;
}

[StructLayout(LayoutKind.Sequential)]
public struct TypeDescriptionEntry
{
	public Int32 PropertyNo { get; set; }

	public TypeNo? TypeNo { get; init; }

	public Int32 Offset { get; init; }

	public Int32 Size { get; init; }

	public String? ClrName { get; init; }
}

[PeachLayout(typeof(TypeDescriptionLayout))]
public interface ITypeDescription
{
	Int32 No { get; set; }

	Int32 Size { get; set; }

	String? ClrName { get; set; }

	TypeDescriptionEntry[]? Properties { get; set; }
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
}
