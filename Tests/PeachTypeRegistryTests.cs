using System.Runtime.InteropServices;

namespace AlphaBee;

[TestClass]
public class PeachTypeRegistryTests
{
	PeachTypeRegistry registry = null!;

	public interface IFoo
	{
		public Int32 Number { get; set; }

		public IBar Bar { get; set; }
	}

	public interface IBar
	{
		public String String { get; set; }
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SFoo1
	{
		public Int32 Number;

		public Int64 Bar;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SFoo2
	{
		public Int64 Bar;

		public Int32 Number;
	}

	public struct SFoo1b
	{
		public Int32 Number;

		public Int64 Bar;
	}

	[TestInitialize]
	public void Setup()
	{
		registry = new PeachTypeRegistry(PeachTypeRegistry.Stage.Declaring);

		registry.EnsureTypeNosForTesting(typeof(IFoo));
		registry.EnsureTypeNosForTesting(typeof(IBar));

		registry.SetStage(PeachTypeRegistry.Stage.Importing);
	}

	[TestMethod]
	public void TestFundamentals()
	{
		registry.SetStage(PeachTypeRegistry.Stage.Ready);

		registry.LookupClrType(typeof(Byte[]), out var byteArrayRef, out var byteArrayType);

		Assert.AreEqual(TypeCode.Byte, byteArrayRef.typeByte.Code);
		Assert.AreEqual(typeof(Byte[]), byteArrayType);
	}

	[TestMethod]
	public void TestLayoutEqualities()
	{
		Assert.AreEqual(MakeLayout<IFoo, SFoo1>(), MakeLayout<IFoo, SFoo1b>());
		Assert.AreNotEqual(MakeLayout<IFoo, SFoo1>(), MakeLayout<IFoo, SFoo2>());
	}

	[TestMethod]
	public void TestAddingCanonicalsAfterAlternates()
	{
		var foo1Type = AddAlternativeLayout<IFoo, SFoo1>();
		Assert.ThrowsException<PeachTypeRegistry.MultipleImplementationsException>(AddAlternativeLayout<IFoo, SFoo2>);
		Assert.ThrowsException<PeachTypeRegistry.DuplicateImplementationException>(AddAlternativeLayout<IFoo, SFoo1b>);

		registry.SetStage(PeachTypeRegistry.Stage.Ready);

		registry.EnsureCanonicalImplementation(typeof(IFoo), out var fooRef, out var fooType);
		registry.EnsureCanonicalImplementation(typeof(IBar), out var barRef, out var barType);

		Assert.AreEqual(foo1Type, fooType);

		Assert.AreEqual(1, foo1Type.CreateInstance<IPeach>()?.ImplementationTypeNo.no);

		Assert.AreEqual(1, fooRef.no);
		Assert.AreEqual(2, barRef.no);

		registry.Validate();
	}

	PeachTypeLayout MakeLayout<InterfaceT, LayoutT>()
	{
		return PeachTypeLayout.Create(typeof(InterfaceT), typeof(LayoutT), registry);
	}

	Type AddAlternativeLayout<InterfaceT, LayoutT>()
	{
		var layoutType = MakeLayout<InterfaceT, LayoutT>();

		return registry.AssignImplementationForTesting(layoutType);
	}
}
