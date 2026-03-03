using System.Diagnostics.CodeAnalysis;

namespace AlphaBee.Layouts;

[DebuggerDisplay("{ToString()}")]
public struct LayoutEntry : IEquatable<LayoutEntry>
{
	public readonly Int32 offset;
	public readonly Int32 size;

	public LayoutEntry(Int32 offset, Int32 size)
	{
		this.offset = offset;
		this.size = size;
	}

	public Boolean Equals(LayoutEntry other)
	{
		return other.offset == offset && other.size == size;
	}

	public override Int32 GetHashCode()
	{
		return offset.GetHashCode() ^ size.GetHashCode();
	}

	public override Boolean Equals([NotNullWhen(true)] Object? obj)
	{
		return obj is LayoutEntry other ? Equals(other) : false;
	}

	public override String ToString()
	{
		return $"{offset} ({size} bytes)";
	}
}
