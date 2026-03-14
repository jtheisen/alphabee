using Moldinium.Baking;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace AlphaBee;

[AttributeUsage(AttributeTargets.Interface)]
public class PeachLayoutAttribute : Attribute
{
	public Type LayoutType { get; }

	public PeachLayoutAttribute(Type layoutType) => LayoutType = layoutType;
}

[AttributeUsage(AttributeTargets.Property)]
public class InlineSpanAttribute : Attribute
{
	public Int32 Length { get; }

	public InlineSpanAttribute(Int32 length) => Length = length;

	public static Boolean IsInlineSpan(PropertyInfo property, out Int32 length)
	{
		if (property.GetCustomAttribute<InlineSpanAttribute>() is InlineSpanAttribute inlineSpanAttribute)
		{
			length = inlineSpanAttribute.Length;
			return true;
		}
		else
		{
			length = 0;
			return false;
		}
	}
}

[AttributeUsage(AttributeTargets.Field)]
public class ForPropertyAttribute : Attribute
{
	PropertyInfo? propertyInfo;

	public ForPropertyAttribute(Type declaringType, String propertyName)
	{
		DeclaringType = declaringType;
		PropertyName = propertyName;
	}

	public Type DeclaringType { get; }
	public String PropertyName { get; }

	public PropertyInfo Property
	{
		get
		{
			return propertyInfo ?? (propertyInfo = DeclaringType.GetProperty(PropertyName) ?? Throw());
		}
	}

	PropertyInfo Throw()
	{
		throw new Exception($"Can't find property {PropertyName} on type {DeclaringType}");
	}
}

public class LayoutBakingCustomMemberModifier : ICustomMemberModifier
{
	public static LayoutBakingCustomMemberModifier Instance = new();

	void ICustomMemberModifier.Handle(FieldBuilder builder, PropertyInfo property)
	{
		var ctor = typeof(ForPropertyAttribute).GetConstructor([typeof(Type), typeof(String)]);

		Trace.Assert(ctor is not null);

		var args = new Object?[] { property.DeclaringType, property.Name };

		var attributeBuilder = new CustomAttributeBuilder(ctor, args);

		builder.SetCustomAttribute(attributeBuilder);
	}
}
