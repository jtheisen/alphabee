using Moldinium.Baking;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Diagnostics;
using System.Numerics;
using static AlphaBee.FundamentalTypeBaking;

namespace AlphaBee;

[TestClass]
public class FundamentalTypeBaking
{
	[TestMethod]
	public void TestFundamentalTypeStruct()
	{
		var kinds = new FundamentalTypesStruct();

		Console.WriteLine(kinds.ReportTypes());
	}

	public interface IFoo
	{
		SByte Integer8 { get; set; }
		Int16 Integer16 { get; set; }
		Int32 Integer32 { get; set; }
		Int64 Integer64 { get; set; }

		Byte UInteger8 { get; set; }
		UInt16 UInteger16 { get; set; }
		UInt32 UInteger32 { get; set; }
		UInt64 UInteger64 { get; set; }

		SByte? NullableInteger8 { get; set; }
		Int16? NullableInteger16 { get; set; }
		Int32? NullableInteger32 { get; set; }
		Int64? NullableInteger64 { get; set; }

		String String { get; set; }
		Byte[] Bytes { get; set; }

		Boolean?[] NullableBooleanArray { get; set; }
		Int32?[] NullableInteger32Array { get; set; }
		Byte?[] NullableByteArray { get; set; }
	}

	public struct SFoo
	{
		public SByte Integer8;
		public Int16 Integer16;
		public Int32 Integer32;
		public Int64 Integer64;

		public Byte UInteger8;
		public UInt16 UInteger16;
		public UInt32 UInteger32;
		public UInt64 UInteger64;

		public NullableStruct<SByte> NullableInteger8;
		public NullableStruct<Int16> NullableInteger16;
		public NullableStruct<Int32> NullableInteger32;
		public NullableStruct<Int64> NullableInteger64;

		public Int64 String;
		public Int64 Bytes;

		public Int64 NullableBooleanArray;
		public Int64 NullableInteger32Array;
		public Int64 NullableByteArray;
	}

	public struct SFooWithHeader
	{
		public ObjectHeader header;
		public SFoo foo;
	}

	public static IEnumerable<Object?[]> GetTestCases()
	{
		for (var i = 0; i < 2; ++i)
		{
			foreach (var property in typeof(IFoo).GetProperties())
			{
				yield return [ property.Name, i ];
			}
		}
	}

	[DataTestMethod]
	[DynamicData(nameof(GetTestCases), DynamicDataSourceType.Method)]
	public void TestStructPropertyBaking(String propertyName, Int32 index)
	{
		var storage = new TestStorage(reserved: Unsafe.SizeOf<SFooWithHeader>() * 2);

		var targets = storage.Data.AsSpan().InterpretAs<SFooWithHeader>();

		var context = new PeachContext(storage, new PeachTypeRegistry());

		var configuration = BakeryConfiguration.Create(
			new PeachPropertyImplementationProvider()
			) with { MakeValue = true };

		var bakery = configuration.CreateBakery("test");

		Int32 no = 0;

		IFoo Create()
		{
			var target = bakery.Create<IFoo>(PeachTypeLayout.CreateWithoutPropNos<IFoo, SFoo>().ToConfiguration(++no));

			var mixin = target as IPeachMixin;

			Trace.Assert(mixin is not null);

			mixin.Init(context, (Unsafe.SizeOf<SFoo>() + ObjectHeader.Size) * index);

			return target;
		}

		var peach = Create();

		var type = peach.GetType();

		Assert.IsTrue(type.IsValueType);

		var property = typeof(IFoo).GetProperty(propertyName);

		Assert.IsNotNull(property);

		var testerType = typeof(PropertyTester<>).MakeGenericType(property.PropertyType);

		var tester = testerType.CreateInstance<PropertyTester>();

		var defaultValue = GetDefaultValue(property.PropertyType);

		var value = GetTestValue(property.PropertyType);

		tester.Test(property.Name, defaultValue is not null, defaultValue, value, peach, ref targets[index].foo);
	}

	static Object? GetDefaultValue(Type type)
	{
		if (Nullable.GetUnderlyingType(type) is not null)
		{
			return null;
		}
		else if (type.IsValueType)
		{
			return Activator.CreateInstance(type)!;
		}
		else
		{
			return null;
		}
	}

	public static bool ImplementsIBinaryNumber(Type type)
	{
		var iBinaryNumber = typeof(IBinaryNumber<>);
		return type.GetInterfaces()
			.Any(i => i.IsGenericType
				&& i.GetGenericTypeDefinition() == iBinaryNumber);
	}

	struct GetTestValueFromType<N> : IValueFromType<Object>
		where N : IBinaryNumber<N>
	{
		Object IValueFromType<Object>.Value => N.CreateChecked(42);
	}

	static Object GetTestValue(Type type)
	{
		if (Nullable.GetUnderlyingType(type) is Type underlyingType)
		{
			return GetTestValue(underlyingType);
		}

		if (ImplementsIBinaryNumber(type))
		{
			return ValueFromType.Get<Object>(typeof(GetTestValueFromType<>), type);
		}

		if (type == typeof(Byte[]))
		{
			return new Byte[] { 1, 2, 3 };
		}
		else if (type == typeof(Byte?[]))
		{
			return new Byte?[] { 1, null, 3 };
		}
		else if (type == typeof(String))
		{
			return "foo";
		}
		else if (type == typeof(Int32?[]))
		{
			return new Int32?[] { 1, null, 3 };
		}
		else if (type == typeof(Boolean?[]))
		{
			return new Boolean?[] { true, null, false };
		}
		else
		{
			throw new NotImplementedException($"No test value for type {type.Name}");
		}
	}

	static void AssertEqual(Object? expected, Object? actual)
	{
		Assert.AreEqual(expected?.GetType(), actual?.GetType());

		if (expected is ICollection e && actual is ICollection a)
		{
			Assert.AreEqual(e.Count, a.Count);

			foreach (var p in e.Cast<Object>().Zip(a.Cast<Object>()))
			{
				AssertEqual(p.First, p.Second);
			}
		}
		else
		{
			Assert.AreEqual(expected, actual);
		}
	}

	public abstract class PropertyTester
	{
		public abstract void Test(String name, Boolean isStruct, Object? defaultValue, Object value, IFoo peach, ref SFoo target);
	}

	public class PropertyTester<T> : PropertyTester
	{
		public override void Test(String name, Boolean isStruct, Object? defaultValue, Object value, IFoo peach, ref SFoo target)
		{
			var property = peach.GetType().GetProperty(name)!;
			var field = target.GetType().GetField(name)!;

			Object? GetFieldValue(SFoo target)
			{
				var result = field.GetValue(target);

				NullableStruct.FixToClrNullableIfAppropriate(ref result);

				return result;
			}

			AssertEqual(defaultValue, property.GetValue(peach));

			if (isStruct)
			{
				Assert.AreEqual(defaultValue, GetFieldValue(target));
			}

			property.SetValue(peach, value);

			AssertEqual(value, property.GetValue(peach));

			if (isStruct)
			{
				Assert.AreEqual(value, GetFieldValue(target));
			}
		}
	}
}
