namespace AlphaBee;

#pragma warning disable CS0169

[TestClass]
public class PeachTypeConfigurationTests
{
	public interface IFoo
	{
		Int32 Prop1 { get; set; }
		Int32 Prop2 { get; set; }
	}

	public struct FooLayout1
	{
		Int32 Prop1;
		Int32 Prop2;
	}

	public struct FooLayout2
	{
		Int32 Prop2;
		Int32 Prop1;
	}

	public struct FooLayout3
	{
		Int32 Prop1;
		Int32 Prop2;
	}

	[DataTestMethod]
	[DataRow(true, typeof(ITypeDescription), typeof(TypeDescriptionLayout), typeof(ITypeDescription), typeof(TypeDescriptionLayout))]
	[DataRow(false, typeof(ITypeDescription), typeof(TypeDescriptionLayout), typeof(IFoo), typeof(FooLayout1))]
	[DataRow(true, typeof(IFoo), typeof(FooLayout1), typeof(IFoo), typeof(FooLayout1))]
	[DataRow(false, typeof(IFoo), typeof(FooLayout1), typeof(IFoo), typeof(FooLayout2))]
	[DataRow(true, typeof(IFoo), typeof(FooLayout1), typeof(IFoo), typeof(FooLayout3))]
	public void TestEqualitiesAndHashes(Boolean shouldBeEqual, Type description1, Type layout1, Type description2, Type layout2)
	{
		Object configuration1 = PeachTypeLayout.CreateWithoutPropNos(description1, layout1);
		Object configuration2 = PeachTypeLayout.CreateWithoutPropNos(description2, layout2);

		var areEqual = configuration1.Equals(configuration2);

		var areHashesEqual = configuration1.GetHashCode() == configuration2.GetHashCode();

		Assert.AreEqual(shouldBeEqual, areEqual);
		Assert.AreEqual(shouldBeEqual, areHashesEqual);
	}
}
