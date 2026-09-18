using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HookChime.Core.ProcessTree;

/// <summary>Walks the process tree via Toolhelp32 snapshot — the same API the original C++ tool used.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsProcessTreeReader : IProcessTreeReader
{
    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [DllImport("kernel32.dll")]
    private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll")]
    private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    public IEnumerable<ProcessInfo> WalkAncestors(int startPid)
    {
        var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1)) yield break;

        try
        {
            var entries = new Dictionary<uint, (uint ParentPid, string Name)>();
            var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
            if (Process32First(snapshot, ref entry))
            {
                do
                {
                    entries[entry.th32ProcessID] = (entry.th32ParentProcessID, entry.szExeFile);
                } while (Process32Next(snapshot, ref entry));
            }

            var currentPid = (uint)startPid;
            for (var depth = 0; depth < 20; depth++)
            {
                if (!entries.TryGetValue(currentPid, out var current)) yield break;
                var parentPid = current.ParentPid;
                if (parentPid == 0 || parentPid == currentPid) yield break;
                if (!entries.TryGetValue(parentPid, out var parent)) yield break;

                var name = Path.GetFileNameWithoutExtension(parent.Name);
                yield return new ProcessInfo((int)parentPid, name);

                currentPid = parentPid;
            }
        }
        finally
        {
            CloseHandle(snapshot);
        }
    }
}
