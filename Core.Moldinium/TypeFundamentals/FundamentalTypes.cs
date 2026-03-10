namespace AlphaBee;

public static class FundamentalTypesInstance
{
	public static FundamentalTypesStruct ObjectTypeKinds = new FundamentalTypesStruct();
}

public struct FundamentalTypesStruct
{
	readonly IObjectTypeHandler?[] handlersByByte;

	readonly Dictionary<Type, IObjectTypeHandler> handlersByType = new();

	readonly IObjectTypeHandler objectArrayTypeHandler = new ObjectArrayTypeHandler();

	public FundamentalTypesStruct()
	{
		var typeCodes = Enum.GetValues<TypeCode>();

		handlersByByte = new IObjectTypeHandler[128];

		for (Byte i = 0; i < 128; ++i)
		{
			var typeByte = new TypeByte(i);

			var handler = handlersByByte[i] = FindHandler(typeByte);

			if (handler is not null)
			{
				var reportedTypeByte = handler.TypeNo.typeByte;

				Trace.Assert(reportedTypeByte == typeByte,
					$"Type handler '{handler}' reports TypeByte '{reportedTypeByte}', but '{typeByte}' was expected");
			}

			if (handler?.Type is Type type)
			{
				Trace.Assert(!handlersByType.ContainsKey(type), $"There's already a handler registered for type '{type}'");

				handlersByType[type] = handler;
			}
		}
	}

	IObjectTypeHandler? FindHandler(TypeByte typeByte)
	{
		var code = typeByte.Code;

		var isSpan = typeByte.IsSpan;

		var isNullable = typeByte.IsNullable;

		IObjectTypeHandler Get<HandlerT>()
			where HandlerT : IObjectTypeHandler
		{
			return HandlerT.GetHandler(typeByte);
		}

		if (code == TypeCode.String)
		{
			// A TypeCode.String means a Char span that is treated as a String

			if (!isNullable) return null;

			if (isSpan)
			{
				return Get<Ucs2StringTypeHandler>();
			}
			else
			{
				return null;
			}
		}
		else if (code == TypeCode.Object)
		{
			// A TypeCode.Object means a reference to something else

			if (!isNullable) return null;

			if (isSpan)
			{
				return objectArrayTypeHandler;
			}
			else
			{
				return new UnimplementedMiscTypeHandler(typeByte, typeof(Object));
			}
		}
		else if (code.IsSupportedStruct())
		{
			var type = code.FindType();

			if (isSpan)
			{
				if (isNullable)
				{
					return IHandlerGetter.Get(typeof(NullableStructArrayTypeHandler<>).MakeGenericType(type), typeByte);
				}
				else
				{
					return IHandlerGetter.Get(typeof(StructArrayTypeHandler<>).MakeGenericType(type), typeByte);
				}
			}
			else
			{
				// FIXME: we can implement this

				return Get<UnimplementedSupportedStructTypeHandler>();
				// unsafe, because the layout of Nullable<> could change: typeof(Nullable<>).MakeGenericType(type);
			}
		}
		else
		{
			return null;
		}
	}

	public String ReportTypes()
	{
		var writer = new StringWriter();

		for (Byte i = 0; i < 128; ++i)
		{
			writer.WriteLine($"{new TypeByte(i)} - {handlersByByte[i]?.Type?.Name ?? "n/a"}");
		}

		return writer.ToString();
	}

	public IObjectTypeHandler GetHandler(TypeByte typeByte)
	{
		Trace.Assert(!typeByte.IsZero);

		return handlersByByte[typeByte.value] ?? throw new Exception($"No handler exists for TypeByte '{typeByte}'"); ;
	}

	public IObjectTypeHandler? GetHandlerOrNull(Type type)
	{
		if (type.IsArray && type.GetElementType() is Type elementType && !elementType.IsValueType)
		{
			return objectArrayTypeHandler;
		}
		else
		{
			return handlersByType.GetValueOrDefault(type);
		}
	}

	public IObjectTypeHandler GetHandler(Type type)
	{
		return GetHandlerOrNull(type) ?? throw new Exception($"No handler exists for type '{type.Name}'");
	}
}
