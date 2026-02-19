using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace AlphaBee;

public unsafe struct IndexPageHeader
{
	public fixed byte Utf8Chars[4];
}

public unsafe struct MemoryRange
{
	IntPtr ptr;
	UInt64 size;

	public MemoryRange(IntPtr ptr, UInt64 size)
	{
		this.ptr = ptr;
		this.size = size;
	}

	public Span<T> AsSpan<T>()
		where T : unmanaged
	{
		var itemSize = Unsafe.SizeOf<T>();

		var length = size / (UInt64)itemSize;
		var mod = size % (UInt64)itemSize;

		Trace.Assert(length < Int32.MaxValue, "Span is too long");
		Trace.Assert(mod == 0, "Size mismatch");

		return new Span<T>(ptr.ToPointer(), (Int32)length);
	}
}

public static class BitsAndBytes
{
	public unsafe static Int64 Offset<S, M>(ref S root, ref M member)
		where S : unmanaged
		where M : unmanaged
	{
		var offset = (byte*)Unsafe.AsPointer(ref member) - (byte*)Unsafe.AsPointer(ref root);

		return offset;
	}

	public static Int64 GetFieldOffset(FieldInfo field)
	{
		var method = new DynamicMethod("GetFieldOffset", typeof(Int64), [], typeof(BitsAndBytes), true);
		var generator = method.GetILGenerator();

		var type = field.DeclaringType!;

		Debug.Assert(type.IsValueType);

		var local_0 = generator.DeclareLocal(type);
		var local_1 = generator.DeclareLocal(typeof(Int64));
		var local_2 = generator.DeclareLocal(typeof(Int64));

		generator.Emit(OpCodes.Nop);
		generator.Emit(OpCodes.Ldloca_S, local_0);
		generator.Emit(OpCodes.Ldflda, field);
		generator.Emit(OpCodes.Conv_U);
		generator.Emit(OpCodes.Conv_U8);
		generator.Emit(OpCodes.Stloc_1);
		generator.Emit(OpCodes.Ldloca_S, local_0);
		generator.Emit(OpCodes.Conv_U);
		generator.Emit(OpCodes.Conv_U8);
		generator.Emit(OpCodes.Stloc_2);
		generator.Emit(OpCodes.Ldloc_1);
		generator.Emit(OpCodes.Ldloc_2);
		generator.Emit(OpCodes.Sub);
		generator.Emit(OpCodes.Ret);

 		var function = (Func<Int64>)method.CreateDelegate(typeof(Func<Int64>));

		return function();
	}
}