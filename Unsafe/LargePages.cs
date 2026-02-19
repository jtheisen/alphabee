using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AlphaBee;

public static class LargePagePrivilege
{
	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
		ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

	[DllImport("advapi32.dll", SetLastError = true)]
	public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass,
	IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

	[StructLayout(LayoutKind.Sequential)]
	public struct LUID
	{
		public uint LowPart;
		public int HighPart;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TOKEN_PRIVILEGES
	{
		public uint PrivilegeCount;
		public LUID Luid;
		public uint Attributes;
	}

	public enum TOKEN_INFORMATION_CLASS : int
	{
		TokenPrivileges = 3
	}

	const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
	const uint TOKEN_QUERY = 0x0008;
	const uint SE_PRIVILEGE_ENABLED = 0x00000002;
	const string SE_LOCK_MEMORY_PRIVILEGE = "SeLockMemoryPrivilege";

	public static bool Enable()
	{
		IntPtr token;
		if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
			return false;

		LUID luid;
		if (!LookupPrivilegeValue(null, SE_LOCK_MEMORY_PRIVILEGE, out luid))
		{
			CloseHandle(token);
			return false;
		}

		TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
		bool success = AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
		CloseHandle(token);
		return success && Marshal.GetLastWin32Error() == 0;  // Check ERROR_SUCCESS == 0
	}

	public static bool IsLargePagePrivilegeEnabled()
	{
		if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_QUERY, out IntPtr token))
			return false;

		uint len;
		GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, 0, out len);
		IntPtr pPrivs = Marshal.AllocHGlobal((int)len);
		bool getSuccess = GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenPrivileges, pPrivs, len, out _);
		CloseHandle(token);

		if (!getSuccess)
		{
			Marshal.FreeHGlobal(pPrivs);
			return false;
		}

		TOKEN_PRIVILEGES tp = Marshal.PtrToStructure<TOKEN_PRIVILEGES>(pPrivs);
		Marshal.FreeHGlobal(pPrivs);

		// Simplified: checks first privilege; extend for full scan if multiple
		if (tp.PrivilegeCount == 0) return false;
		bool enabled = (tp.Attributes & SE_PRIVILEGE_ENABLED) != 0;
		Console.WriteLine($"Privilege enabled: {enabled}");
		return enabled;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseHandle(IntPtr hObject);


}

public static class LargePages
{
	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern uint GetLargePageMinimum();

	const uint MEM_LARGE_PAGES = 0x20000000;
	const uint MEM_RESERVE = 0x2000;
	const uint MEM_COMMIT = 0x1000;
	const uint PAGE_READWRITE = 0x04;

	public static bool Enable() => LargePagePrivilege.Enable();

	public static MemoryRange AllocLargePages(nuint sizeInBytes)
	{
		uint minLargePage = GetLargePageMinimum();
		if (sizeInBytes % minLargePage != 0)
			throw new ArgumentException("Size must be multiple of large page size.");

		var ptr = VirtualAlloc(0, sizeInBytes, MEM_RESERVE | MEM_COMMIT | MEM_LARGE_PAGES, PAGE_READWRITE);

		if (ptr == 0)
		{
			throw new InvalidOperationException($"Failed to allocate with Windows error {Marshal.GetLastWin32Error()}");
		}			

		return new MemoryRange(ptr, sizeInBytes);
	}
}
