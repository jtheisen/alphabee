using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace Tests.Information;

[TestClass]
public class Hardware
{
	[TestMethod]
	public void TestIntrinsicsAvailability()
	{
		// x86: Avx, ARM: AdvSimd

		// Comparisons:
		// Avx has: compare 1-8 byte long items on Vector128
		// Avx2 has: compare 1-8 byte long items on Vector265
		// Avx512F has: compare 1-8 byte long items on Vector512
		// AdvSimd has: compare 1-4 byte long items on Vector128
		// On ARM, more than Vector128 is rare

		// Bit counting:
		// x86: Avx512CD is the only one with leading zero count and
		//      it's super-rare (only some Intel Server-CPUs)
		// ARM: AdvSimd has leading zero count for Vector128

		Console.WriteLine($"Vector128: {Vector128.IsHardwareAccelerated}");
		Console.WriteLine($"Vector256: {Vector256.IsHardwareAccelerated}");
		Console.WriteLine($"Vector512: {Vector512.IsHardwareAccelerated}");
		Console.WriteLine($"Avx: {Avx.IsSupported}");
		Console.WriteLine($"Avx2: {Avx2.IsSupported}");
		Console.WriteLine($"Avx512F: {Avx512F.IsSupported}");
		Console.WriteLine($"AdvSimd: {AdvSimd.IsSupported}");
	}
}
