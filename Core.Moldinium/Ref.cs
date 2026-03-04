using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AlphaBee;

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct TypeRef : IEquatable<TypeRef>
{
	[FieldOffset(0)]
	public readonly Int32 no;

	[FieldOffset(3)]
	public readonly TypeByte typeByte;

	public Boolean IsFundamental => !typeByte.IsZero;

	public TypeRef(TypeByte typeByte)
	{
		this.typeByte = typeByte;
	}

	public TypeRef(Int32 no)
	{
		this.no = no;

		Debug.Assert(typeByte.IsZero);
	}

	public override String ToString()
	{
		if (typeByte.IsZero)
		{
			return $"#{no}";
		}
		else
		{
			return $"{typeByte}";
		}
	}

	public Boolean Equals(TypeRef other) => no == other.no;
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 1)]
public readonly struct TypeByte : IEquatable<TypeByte>
{
	[FieldOffset(0)]
	public readonly SByte value;

	static Int32 IsNullablePattern = 1 << 6;

	static Int32 IsSpanPattern = 1 << 5;

	static Int32 CodePattern = (1 << 5) - 1;

	public TypeCode Code => (TypeCode)(value & CodePattern);

	public Boolean IsSpan => (value & IsSpanPattern) != 0;

	public Boolean IsNullable => (value & IsNullablePattern) != 0;

	public Boolean IsZero => value == 0;

	public static implicit operator TypeByte(TypeCode code) => new TypeByte(code);

	public TypeByte(Byte value, Boolean isSpan = false, Boolean isNullable = false)
	{
		this.value = (SByte)(value | (isSpan ? IsSpanPattern : 0) | (isNullable ? IsNullablePattern : 0));

		Trace.Assert(value >= 0);
	}

	public TypeByte(TypeCode code, Boolean isSpan = false, Boolean isNullable = false)
		: this((Byte)code, isSpan, isNullable)
	{
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

	public Boolean Equals(TypeByte other) => value == other.value;

	public static Boolean operator ==(TypeByte lhs, TypeByte rhs) => lhs.Equals(rhs);
	public static Boolean operator !=(TypeByte lhs, TypeByte rhs) => !lhs.Equals(rhs);

	public override Boolean Equals(Object? obj) => obj is TypeByte && Equals((TypeByte)obj);
	public override int GetHashCode() => value.GetHashCode();
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 16)]
public readonly struct ObjectHeader
{
	public static Int32 Size => Unsafe.SizeOf<ObjectHeader>();

	[FieldOffset(0)]
	public readonly TypeRef type;

	[FieldOffset(4)]
	public readonly Int32 unused;

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
