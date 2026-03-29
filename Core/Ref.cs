using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AlphaBee;

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 4)]
public readonly record struct TypeNo
{
	[FieldOffset(0)]
	readonly Int32 no;

	[FieldOffset(3)]
	readonly TypeByte typeByte;

	public Int32 No => no;
	public TypeByte TypeByte => typeByte;

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

	public static implicit operator TypeNo(TypeByte typeByte) => new TypeNo(typeByte);

	public static implicit operator TypeNo(Int32 no) => new TypeNo(no);

	public override String ToString()
	{
		if (typeByte.IsZero)
		{
			return $"#{No}";
		}
		else
		{
			return $"{typeByte}";
		}
	}
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly record struct PropAndTypeNo(TypeNo DeclaringTypeNo, PropNo PropNo);

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly record struct PropNo(Int32 No)
{
	public static implicit operator PropNo(Int32 no) => new PropNo(no);

	public override String ToString() => $"#{No}";
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Sequential, Size = Size)]
public readonly record struct ArrayHeader(Int32 Length, Int32 ArrayLevel)
{
	public const Int32 Size = 8;

	public Boolean IsEmpty => this == default;

	static String GetBrackets(Int32 level)
	{
		if (level == 0) return "";

		var n = level * 2;
		var chars = new Char[level * 2];
		for (var i = 0; i < n; i += 2)
		{
			chars[i] = '[';
			chars[i + 1] = ']';
		}
		return new String(chars);
	}

	public override String ToString() => $"{GetBrackets(ArrayLevel)}[{Length}]";
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
		b.Append(Code.ToStringExtended());
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
public readonly struct ObjectExtent : IEquatable<ObjectExtent>
{
	[FieldOffset(0)]
	readonly Int16 unitSize;

	[FieldOffset(2)]
	readonly Int16 length;

	public Int32 UnitSize => unitSize;

	public Int32 Length => length;

	public Int32 Size => UnitSize * length;

	public void Deconstruct(out Int32 length, out Int32 unitSize)
	{
		length = this.length;
		unitSize = this.unitSize;
	}

	public ObjectExtent(Int32 length, Int32 unitSize = 1)
	{
		checked
		{
			this.length = (Int16)length;
			this.unitSize = (Int16)unitSize;
		}
	}

	public ObjectExtent(Int16 length, Int16 unitSize)
	{
		this.length = length;
		this.unitSize = unitSize;
	}
	
	public static ObjectExtent CreateForStruct<T>(Int32 length = 1) where T : unmanaged
	{
		var unitSize = Unsafe.SizeOf<T>();

		return new ObjectExtent(length, unitSize);
	}

	public static ObjectExtent CreateForStruct(Type type, Int32 length = 1)
	{
		var unitSize = type.SizeOf();

		return new ObjectExtent(length, unitSize);
	}

	public Boolean Equals(ObjectExtent other)
	{
		return unitSize == other.unitSize && length == other.length;
	}
}

[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct ObjectHeader : IEquatable<ObjectHeader>
{
	[FieldOffset(0)]
	readonly Int64 value;

	[FieldOffset(0)]
	readonly ObjectExtent extent;

	[FieldOffset(4)]
	readonly TypeNo typeNo;

	ObjectHeader(Int64 value)
	{
		this.value = value;
	}

	public ObjectHeader(TypeNo typeNo, ObjectExtent extent)
	{
		this.typeNo = typeNo;
		this.extent = extent;
	}

	public static explicit operator Int64(ObjectHeader tae) => tae.value;
	public static explicit operator ObjectHeader(Int64 value) => new ObjectHeader(value);

	public void Deconstruct(out TypeNo typeNo, out ObjectExtent extent)
	{
		typeNo = this.typeNo;
		extent = this.extent;
	}

	public Int64 Int64 => value;
	public TypeNo TypeNo => typeNo;
	public ObjectExtent Extent => extent;

	public static Int32 Size => Unsafe.SizeOf<ObjectHeader>();

	public Boolean IsArray => TypeNo.IsArray;

	public Int32 EntireSize => Size + Extent.Size;

	//public Int32 PaddedEntireSize => EntireSize.Log2Ceil;

	public Int32 UnitSize => Extent.UnitSize;

	public Int32 ContentLength => Extent.Length;

	public Int32 ContentSize => Extent.Size;

	public static ObjectHeader CreateWithSize(TypeNo typeNo, Int32 size, Int32 length = 1)
	{
		return new ObjectHeader(typeNo, checked(new ObjectExtent((Int16)length, checked((Int16)size))));
	}

	public static ObjectHeader CreateForRef(TypeNo typeNo, Int32 length = 1)
	{
		return new ObjectHeader(typeNo, checked(new ObjectExtent((Int16)length, (Int16)Ref.Size)));
	}

	public static ObjectHeader CreateForStruct<T>(TypeNo typeNo, Int32 length = 1) where T : unmanaged
	{
		return new ObjectHeader(typeNo, ObjectExtent.CreateForStruct<T>(length));
	}

	public Boolean Equals(ObjectHeader other) => value == other.value;

	public override Boolean Equals([NotNullWhen(true)] Object? obj)
	{
		return obj is ObjectHeader other ? Equals(other) : false;
	}

	public static Boolean operator ==(ObjectHeader lhs, ObjectHeader rhs) => lhs.Equals(rhs);
	public static Boolean operator !=(ObjectHeader lhs, ObjectHeader rhs) => !lhs.Equals(rhs);

	public override int GetHashCode() => value.GetHashCode();

	public override String ToString()
	{
		var arrayTag = IsArray ? $"[{ContentLength}]" : null;

		return $"obj:#{TypeNo}{arrayTag}({Size} bytes total)";
	}
}

[StructLayout(LayoutKind.Explicit)]
public struct Ref
{
	public static Int32 Size => Unsafe.SizeOf<Ref>();

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
