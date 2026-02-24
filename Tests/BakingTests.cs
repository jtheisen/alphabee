using Alphabee;
using AlphaBee.Layouts.Structs;
using Moldinium.Baking;
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
	}

	public struct SFoo
	{
		public SByte Integer8;
		public Int16 Integer16;
		public Int32 Integer32;
		public Int64 Integer64;
	}

	public class TypeConfiguration<T> : ITypeConfiguration
	{
		static FieldEntry[] fields = typeof(T).GetLayoutFields().ToArray();

		public Int64? GetPropertyIntegerForArgumentName(PropertyInfo property, String argumentName)
		{
			return fields.Single(f => f.FieldInfo.Name == property.Name).Layout.Offset;
		}
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(1)]
	public void TestBaking(Int32 index)
	{
		var data = new Byte[Unsafe.SizeOf<SFoo>() * 2];

		var targets = data.AsSpan().InterpretAs<SFoo>();

		var context = new PeachyTestContext(data);

		var configuration = BakeryConfiguration.Create(typeof(PeachyPropertyImplementation<>)) with { MakeValue = true };

		var bakery = configuration.CreateBakery("test");

		IFoo Create()
		{
			var target = bakery.Create<IFoo>(new TypeConfiguration<SFoo>());

			var mixin = target as IPeachyMixin;

			Trace.Assert(mixin is not null);

			mixin.Init(context);
			mixin.Address = (UInt64)(Unsafe.SizeOf<SFoo>() * index);

			return target;
		}

		var peach = Create();

		var type = peach.GetType();

		Assert.IsTrue(type.IsValueType);

		foreach (var field in typeof(SFoo).GetFields())
		{
			var testerType = typeof(PropertyTester<>).MakeGenericType(field.FieldType);

			var tester = testerType.CreateInstance<PropertyTester>();

			var value = GetTestValue(field.FieldType);

			tester.Test(field.Name, value, peach, ref targets[index]);
		}
	}

	public static Object GetTestValue(Type type)
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
		else
		{
			throw new NotImplementedException($"No test value for type {type.Name}");
		}
	}

	public abstract class PropertyTester
	{
		public abstract void Test(String name, Object value, IFoo peach, ref SFoo target);
	}

	public class PropertyTester<T> : PropertyTester
	{
		public override void Test(String name, Object value, IFoo peach, ref SFoo target)
		{
			var defaultValue = Activator.CreateInstance(value.GetType())!;

			var property = peach.GetType().GetProperty(name)!;
			var field = target.GetType().GetField(name)!;

			Assert.AreEqual(defaultValue, property.GetValue(peach));
			Assert.AreEqual(defaultValue, field.GetValue(target));

			property.SetValue(peach, value);

			Assert.AreEqual(value, property.GetValue(peach));
			Assert.AreEqual(value, field.GetValue(target));
		}
	}
}
