using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ArmAssistBll
{
    /// <summary>
    /// Static class that provides Windows CE process information and manipulation.
    /// The biggest difference with the Compact Framework's Process class is that this
    /// class works with the full path to the .EXE file. And not the pathless .EXE file name.
    /// </summary>
    public static class ProcessCE
    {
        private const int MAX_PATH = 260;
        private const int TH32CS_SNAPPROCESS = 0x00000002;
        private const int TH32CS_SNAPNOHEAPS = 0x40000000;
        private const int INVALID_HANDLE_VALUE = -1;
        private const int PROCESS_TERMINATE = 1;

        /// <summary>
        /// Returns an array with information about running processes.
        /// </summary>
        ///<exception cref="Win32Exception">Thrown when enumerating the processes fails.</exception>
        public static ProcessInfo[] GetProcesses()
        {
            List<ProcessInfo> procList = new List<ProcessInfo>();

            IntPtr handle = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS | TH32CS_SNAPNOHEAPS, 0);

            if ((Int32)handle == INVALID_HANDLE_VALUE)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateToolhelp32Snapshot error.");

            try
            {
                PROCESSENTRY processentry = new PROCESSENTRY();
                processentry.dwSize = (uint)Marshal.SizeOf(processentry);

                //Get the first process
                int retval = Process32First(handle, ref processentry);

                while (retval == 1)
                {
                    procList.Add(new ProcessInfo(new IntPtr((int)processentry.th32ProcessID), (int)processentry.cntThreads, (int)processentry.th32MemoryBase, (int)processentry.th32ParentProcessID));
                    retval = Process32Next(handle, ref processentry);
                }
            }
            finally
            {
                CloseToolhelp32Snapshot(handle);
            }

            return procList.ToArray();
        }

        /// <summary>
        /// Checks if the specified .EXE is running.
        /// </summary>
        /// <param name="fullpath">The full path to an .EXE file.</param>
        /// <returns>Returns true is the process is running.</returns>
        /// <exception cref="Win32Exception">Thrown when taking a system snapshot fails.</exception>
        public static bool IsRunning(string fullpath)
        {
            return (FindProcessPID(fullpath) != IntPtr.Zero);
        }

        /// <summary>
        /// Finds and kills if the process for the specified .EXE file is running.
        /// </summary>
        /// <param name="fullpath">The full path to an .EXE file.</param>
        /// <returns>True if the process was terminated. False if the process was not found.</returns>
        /// <exception cref="Win32Exception">Thrown when opening or killing the process fails.</exception>
        public static bool FindAndKill(string fullpath)
        {
            IntPtr pid = FindProcessPID(fullpath);

            if (pid == IntPtr.Zero)
                return false;

            Kill(pid);

            return true;
        }

        /// <summary>
        /// Terminates the process with the specified Process Id.
        /// </summary>
        /// <param name="pid">The Process Id of the process to kill.</param>
        /// <exception cref="Win32Exception">Thrown when opening or killing the process fails.</exception>
        internal static void Kill(IntPtr pid)
        {

            IntPtr process_handle = OpenProcess(PROCESS_TERMINATE, false, (int)pid);

            if (process_handle == (IntPtr)INVALID_HANDLE_VALUE)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "OpenProcess failed.");

            try
            {
                bool result = TerminateProcess(process_handle, 0);

                if (result == false)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "TerminateProcess failed.");

            }
            finally
            {
                CloseHandle(process_handle);
            }
        }

        /// <summary>
        /// Finds the Process Id of the specified .EXE file.
        /// </summary>
        /// <param name="fullpath">The full path to an .EXE file.</param>
        /// <returns>The Process Id to the process found. Return IntPtr.Zero if the process is not running.</returns>
        ///<exception cref="Win32Exception">Thrown when taking a system snapshot fails.</exception>
        public static IntPtr FindProcessPID(string fullpath)
        {
            fullpath = fullpath.ToLower();

            IntPtr snapshot_handle = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS | TH32CS_SNAPNOHEAPS, 0);

            if ((Int32)snapshot_handle == INVALID_HANDLE_VALUE)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateToolhelp32Snapshot failed.");

            try
            {
                PROCESSENTRY processentry = new PROCESSENTRY();
                processentry.dwSize = (uint)Marshal.SizeOf(processentry);
                StringBuilder fullexepath = new StringBuilder(1024);

                int retval = Process32First(snapshot_handle, ref processentry);

                while (retval == 1)
                {
                    IntPtr pid = new IntPtr((int)processentry.th32ProcessID);

                    // Writes the full path to the process into a StringBuilder object.
                    // Note: If first parameter is IntPtr.Zero it returns the path to the current process.
                    GetModuleFileName(pid, fullexepath, fullexepath.Capacity);

                    if (fullexepath.ToString().ToLower() == fullpath)
                        return pid;

                    retval = Process32Next(snapshot_handle, ref processentry);
                }
            }
            finally
            {
                CloseToolhelp32Snapshot(snapshot_handle);
            }

            return IntPtr.Zero;
        }

        [DllImport("coredll.Dll", EntryPoint = "CreateProcess", SetLastError = true)]
        public extern static int CreateProcess(string strImageName, string strCmdLine, IntPtr pProcessAttributes,
                                    IntPtr pThreadAttributes, int bInheritsHandle, int dwCreationFlags,
                                    IntPtr pEnvironment, IntPtr pCurrentDir, IntPtr bArray, ProcessContext oProc);

        [DllImport("toolhelp.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processID);

        [DllImport("toolhelp.dll")]
        private static extern int CloseToolhelp32Snapshot(IntPtr snapshot);

        [DllImport("toolhelp.dll")]
        private static extern int Process32First(IntPtr snapshot, ref PROCESSENTRY processEntry);

        [DllImport("toolhelp.dll")]
        private static extern int Process32Next(IntPtr snapshot, ref PROCESSENTRY processEntry);

        [DllImport("coredll.dll", SetLastError = true)]
        private static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);

        [DllImport("coredll.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint ExitCode);

        [DllImport("coredll.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int flags, bool fInherit, int PID);

        [DllImport("coredll.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        private struct PROCESSENTRY
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public uint th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szExeFile;
            public uint th32MemoryBase;
            public uint th32AccessKey;
        }

    } // end of class
}

