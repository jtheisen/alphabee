using Alphabee;
using AlphaBee.Layouts.Structs;
using Microsoft.Testing.Platform.Requests;
using Moldinium.Baking;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace AlphaBee;

[TestClass]
public class BakingTests
{
	public interface IFoo
	{
		SByte Integer8 { get; set; }
		Int16 Integer16 { get; set; }
		Int32 Integer32 { get; set; }
		Int64 Integer64 { get; set; }

		String String { get; set; }
		Byte[] Bytes { get; set; }
	}

	public struct SFoo
	{
		public SByte Integer8;
		public Int16 Integer16;
		public Int32 Integer32;
		public Int64 Integer64;

		public Int64 String;
		public Int64 Bytes;
	}

	public class TypeConfiguration<T> : ITypeConfiguration
	{
		static FieldEntry[] fields = typeof(T).GetLayoutFields().ToArray();

		public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
		{
			return fields.Single(f => f.FieldInfo.Name == property.Name).Layout.Offset;
		}
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
		var storage = new TestStorage(Unsafe.SizeOf<SFoo>() * 2);

		var targets = storage.Data.AsSpan().InterpretAs<SFoo>();

		var context = new PeachyContext(storage);

		var configuration = BakeryConfiguration.Create(
			typeof(PeachyStructPropertyImplementation<>),
			typeof(PeachyClassPropertyImplementation<>)
			) with { MakeValue = true };

		var bakery = configuration.CreateBakery("test");

		IFoo Create()
		{
			var target = bakery.Create<IFoo>(new TypeConfiguration<SFoo>());

			var mixin = target as IPeachyMixin;

			Trace.Assert(mixin is not null);

			mixin.Init(context);
			mixin.Address = Unsafe.SizeOf<SFoo>() * index;

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

		tester.Test(property.Name, defaultValue is not null, defaultValue, value, peach, ref targets[index]);
	}

	static Object? GetDefaultValue(Type type)
	{
		if (type.IsValueType)
		{
			return Activator.CreateInstance(type)!;
		}
		else
		{
			return null;
		}
	}

	static Object GetTestValue(Type type)
	{
		if (type == typeof(SByte))
		{
			return (SByte)42;
		}
		else if (type == typeof(Int16))
		{
			return (Int16)42;
		}
		else if (type == typeof(Int32))
		{
			return 42;
		}
		else if (type == typeof(Int64))
		{
			return (Int64)42;
		}
		else if (type == typeof(String))
		{
			return "foo";
		}
		else if (type == typeof(Byte[]))
		{
			return new Byte[] { 1, 2, 3 };
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

			AssertEqual(defaultValue, property.GetValue(peach));

			if (isStruct)
			{
				Assert.AreEqual(defaultValue, field.GetValue(target));
			}

			property.SetValue(peach, value);

			AssertEqual(value, property.GetValue(peach));

			if (isStruct)
			{
				Assert.AreEqual(value, field.GetValue(target));
			}
		}
	}
}
