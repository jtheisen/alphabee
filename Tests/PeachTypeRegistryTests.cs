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
		registry = new PeachTypeRegistry();
	}

	[TestMethod]
	public void TestFundamentals()
	{
		registry.LookupClrType(typeof(Byte[]), out var byteArrayRef, out var byteArrayType);

		Assert.AreEqual(TypeCode.Byte, byteArrayRef.typeByte.Code);
		Assert.AreEqual(typeof(Byte[]), byteArrayType);
	}

	[TestMethod]
	public void TestAddingCanonicalsAfterAlternates()
	{
		var foo1Type = AddAlternativeLayout<IFoo, SFoo1>();
		var foo2Type = AddAlternativeLayout<IFoo, SFoo2>();
		var foo1bType = AddAlternativeLayout<IFoo, SFoo1b>();

		Assert.AreEqual(foo1Type, foo1bType);
		Assert.AreNotEqual(foo1Type, foo2Type);

		registry.EnsureCanonicalImplementation(typeof(IFoo), out var fooRef, out var fooType);
		registry.EnsureCanonicalImplementation(typeof(IBar), out var barRef, out var barType);

		Assert.AreEqual(foo1Type, fooType);
		Assert.AreNotEqual(foo2Type, fooType);

		Assert.AreEqual(1, foo1Type.CreateInstance<IPeach>()?.ImplementationTypeNo.no);
		Assert.AreEqual(2, foo2Type.CreateInstance<IPeach>()?.ImplementationTypeNo.no);
		Assert.AreEqual(1, foo1bType.CreateInstance<IPeach>()?.ImplementationTypeNo.no);

		Assert.AreEqual(1, fooRef.no);
		Assert.AreEqual(3, barRef.no);

		registry.Validate();
	}

	Type AddAlternativeLayout<InterfaceT, LayoutT>()
	{
		registry.EnsureEntry(typeof(InterfaceT));

		var layoutType = PeachTypeLayout.Create(typeof(InterfaceT), typeof(LayoutT), registry);

		return registry.AddImplementation(layoutType, null, allowNewImplementation: true);
	}
}
