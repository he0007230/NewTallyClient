using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ArmAssistBll
{
    /// <summary>
    /// Contains information about a process.
    /// This information is collected by ProcessCE.GetProcesses().
    /// </summary>
    public class ProcessInfo
    {
        [DllImport("coredll.dll", SetLastError = true)]
        private static extern int GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);

        private const int INVALID_HANDLE_VALUE = -1;

        private IntPtr _pid;
        private int _threadCount;
        private int _baseAddress;
        private int _parentProcessID;
        private string _fullPath;

        internal ProcessInfo(IntPtr pid, int threadcount, int baseaddress, int parentid)
        {
            _pid = pid;
            _threadCount = threadcount;
            _baseAddress = baseaddress;
            _parentProcessID = parentid;

            StringBuilder sb = new StringBuilder(1024);
            GetModuleFileName(_pid, sb, sb.Capacity);
            _fullPath = sb.ToString();
        }

        /// <summary>
        /// Returns the full path to the process .EXE file.
        /// </summary>
        /// <example>"\Program Files\Acme\main.exe"</example>
        public override string ToString()
        {
            return _fullPath;
        }

        public int BaseAddress
        {
            get { return _baseAddress; }
        }

        public int ThreadCount
        {
            get { return _threadCount; }
        }

        /// <summary>
        /// Returns the Process Id.
        /// </summary>
        public IntPtr Pid
        {
            get { return _pid; }
        }

        /// <summary>
        /// Returns the full path to the process .EXE file.
        /// </summary>
        /// <example>"\Program Files\Acme\main.exe"</example>
        public string FullPath
        {
            get { return _fullPath; }
        }

        public int ParentProcessID
        {
            get { return _parentProcessID; }
        }

        /// <summary>
        /// Kills the process.
        /// </summary>
        /// <exception cref="Win32Exception">Thrown when killing the process fails.</exception>
        public void Kill()
        {
            ProcessCE.Kill(_pid);
        }

    }

}
