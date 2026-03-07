using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AlphaBee;

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct TypeNo : IEquatable<TypeNo>
{
	[FieldOffset(0)]
	public readonly Int32 no;

	[FieldOffset(3)]
	public readonly TypeByte typeByte;

	public Boolean IsArray => typeByte.IsSpan;

	public Boolean IsFundamental => !typeByte.IsZero;

	public TypeNo(TypeByte typeByte)
	{
		this.typeByte = typeByte;
	}

	public TypeNo(Int32 no)
	{
		this.no = no;

		Debug.Assert(typeByte.IsZero);
	}

	public static implicit operator TypeNo(Int32 no) => new TypeNo(no);

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

	public Boolean Equals(TypeNo other) => no == other.no;

	public static Boolean operator ==(TypeNo lhs, TypeNo rhs) => lhs.Equals(rhs);
	public static Boolean operator !=(TypeNo lhs, TypeNo rhs) => !lhs.Equals(rhs);

	public override bool Equals(Object? obj) => obj is TypeNo other ? other.Equals(this) : false;
	public override int GetHashCode() => no.GetHashCode();
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct PropNo : IEquatable<PropNo>
{
	[FieldOffset(0)]
	public readonly Int32 no;

	public PropNo(Int32 no)
	{
		this.no = no;
	}

	public static implicit operator PropNo(Int32 no)
	{
		return new PropNo(no);
	}

	public override String ToString()
	{
		return $"#{no}";
	}

	public Boolean Equals(PropNo other) => no == other.no;

	public static Boolean operator ==(PropNo lhs, PropNo rhs) => lhs.Equals(rhs);
	public static Boolean operator !=(PropNo lhs, PropNo rhs) => !lhs.Equals(rhs);

	public override bool Equals(Object? obj) => obj is PropNo other ? other.Equals(this) : false;
	public override int GetHashCode() => no.GetHashCode();
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

	public override Boolean Equals(Object? obj) => obj is TypeByte other ? other.Equals(this) : false;
	public override int GetHashCode() => value.GetHashCode();
}

public struct ObjectSize
{
	readonly UInt32 value;

	public void Get(out Int32 length, out Int32 sizeLog2)
	{
		sizeLog2 = BitOperations.LeadingZeroCount(value);
		length = (Int32)(value & (UInt32.MaxValue << (32 - sizeLog2)));
	}

	public ObjectSize(UInt32 value) => this.value = value;

	public static ObjectSize Create(Int32 length, Int32 sizeLog2)
	{
		Trace.Assert(sizeLog2 >= 0 && sizeLog2 < 32);

		var max = 1u << sizeLog2;

		Trace.Assert(length < max);

		return new ObjectSize(max | (UInt32)length);
	}
}

[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly struct ObjectExtent
{
	[FieldOffset(0)]
	readonly Int16 unitSize;

	[FieldOffset(2)]
	readonly Int16 length;

	public Int32 UnitSize => unitSize;

	public Int32 Length => length;

	public Int32 Size => UnitSize * length;

	public ObjectExtent(Int16 length, Int16 unitSize)
	{
		this.length = length;
		this.unitSize = unitSize;
	}
	
	public static ObjectExtent CreateForStruct<T>(Int32 length = 1) where T : unmanaged
	{
		Trace.Assert(length < Int16.MaxValue, "Larger arrays are not yet supported");

		var unitSize = Unsafe.SizeOf<T>();

		return checked(new ObjectExtent((Int16)length, (SByte)unitSize));
	}
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct ObjectHeader
{
	public static Int32 Size => Unsafe.SizeOf<ObjectHeader>();

	[FieldOffset(0)]
	public readonly TypeNo typeNo;

	[FieldOffset(4)]
	public readonly ObjectExtent extent;

	// 3 unused bytes

	//[FieldOffset(8)]
	//public readonly Int32 id;

	//[FieldOffset(12)]
	//public readonly Int32 length;

	public Boolean IsArray => typeNo.IsArray;

	public Int32 EntireSize => Size + extent.Size;

	public Int32 ContentLength => extent.Length;

	public Int32 ContentSize => extent.Size;

	public ObjectHeader(TypeNo typeNo, ObjectExtent extent)
	{
		this.typeNo = typeNo;
		this.extent = extent;
	}

	public static ObjectHeader CreateForStruct<T>(TypeNo typeNo, Int32 length) where T : unmanaged
	{
		return new ObjectHeader(typeNo, ObjectExtent.CreateForStruct<T>(length));
	}

	public override String ToString()
	{
		var arrayTag = IsArray ? $"[{ContentLength}]" : null;

		return $"obj:#{typeNo}{arrayTag}({Size} bytes)";
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
