using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AlphaBee;

public interface ITypeDescription
{
	String?[]? Names { get; set; }

	ITypeDescription?[]? Descriptions { get; set; }

	TypeByte[] TypeBytes { get; set; }

	Int32[] Offsets { get; set; }
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct TypeRef
{
	[FieldOffset(0)]
	public readonly Int64 address;

	[FieldOffset(7)]
	public readonly TypeByte typeByte;

	public TypeRef(TypeByte typeByte)
	{
		this.typeByte = typeByte;
	}

	public TypeRef(Int64 address)
	{
		this.address = address;

		Debug.Assert(typeByte.IsZero);
	}

	public static implicit operator TypeRef(Int64 address) => new TypeRef(address);

	public static implicit operator Int64(TypeRef address) => address.address;

	public override String ToString()
	{
		if (typeByte.IsZero)
		{
			return $"{address:x}";
		}
		else
		{
			return $"{typeByte}";
		}
	}
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 1)]
public readonly struct TypeByte
{
	[FieldOffset(0)]
	public readonly Byte value;

	static Int32 IsNullablePattern = 1 << 6;

	static Int32 IsSpanPattern = 1 << 5;

	static Int32 CodePattern = (1 << 5) - 1;

	public TypeCode Code => (TypeCode)(value & CodePattern);

	public Boolean IsSpan => (value & IsSpanPattern) != 0;

	public Boolean IsNullable => (value & IsNullablePattern) != 0;

	public Boolean IsZero => value == 0;

	public static implicit operator TypeByte(TypeCode code) => new TypeByte(code);

	public TypeByte(Byte value)
	{
		Trace.Assert(value <= 127);

		this.value = value;
	}

	public TypeByte(TypeCode code, Boolean isSpan = false, Boolean isNullable = false)
	{
		value = (Byte)((Byte)code | (isSpan ? IsSpanPattern : 0) | (isNullable ? IsNullablePattern : 0));
	}

	public override String ToString()
	{
		var b = new StringBuilder();
		b.Append(Code.ToString());
		if (IsNullable)
		{
			b.Append("?");
		}
		if (IsSpan)
		{
			b.Append("[]");
		}
		return b.ToString();
	}
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct ObjectHeader
{
	public static Int32 Size => Unsafe.SizeOf<ObjectHeader>();

	[FieldOffset(0)]
	public readonly TypeRef type;

	[FieldOffset(8)]
	public readonly Int32 id;

	[FieldOffset(12)]
	public readonly Int32 size;

	public ObjectHeader(TypeRef type, Int32 size)
	{
		this.type = type;
		this.id = 0;
		this.size = size;
	}

	public override String ToString()
	{
		return $"#{id}:{type}({size} bytes)";
	}
}

[StructLayout(LayoutKind.Explicit)]
public struct Ref
{
	[FieldOffset(0)]
	Int64 address;

	public Ref(Int64 address)
	{
		this.address = address;
	}

	public static implicit operator Ref(Int64 address) => new Ref(address);

	public static implicit operator Int64(Ref address) => address.address;

	//public Object Get(AbstractTestStorage storage)
	//{
	//	storage.GetValue<>
	//}
}
