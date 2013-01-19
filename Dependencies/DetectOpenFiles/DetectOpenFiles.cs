//
//  Based on http://vmccontroller.codeplex.com/SourceControl/changeset/view/47386#195318
//  Modifications to OpenFiles:
//   - Filters against a list of process IDs, not just one.
//   - Returns full path as string, and not as FileSystemInfo.
//   - Added support for shared files.
//   - Removed helper function and changed constructor to public.
//  
//  Big thanks to the developers of Windows Media Center TCP/IP Controller for writing and sharing this awesome class!
//

namespace RoliSoft.TVShowTracker.Dependencies.DetectOpenFiles
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using Microsoft.Win32.SafeHandles;

    #region ENUMs
    internal enum NT_STATUS
    {
        STATUS_SUCCESS = 0x00000000,
        STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005L),
        STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004L)
    }

    internal enum SYSTEM_INFORMATION_CLASS
    {
        SystemBasicInformation = 0,
        SystemPerformanceInformation = 2,
        SystemTimeOfDayInformation = 3,
        SystemProcessInformation = 5,
        SystemProcessorPerformanceInformation = 8,
        SystemHandleInformation = 16,
        SystemInterruptInformation = 23,
        SystemExceptionInformation = 33,
        SystemRegistryQuotaInformation = 37,
        SystemLookasideInformation = 45
    }

    internal enum OBJECT_INFORMATION_CLASS
    {
        ObjectBasicInformation = 0,
        ObjectNameInformation = 1,
        ObjectTypeInformation = 2,
        ObjectAllTypesInformation = 3,
        ObjectHandleInformation = 4
    }

    [Flags]
    internal enum ProcessAccessRights
    {
        PROCESS_DUP_HANDLE = 0x00000040
    }

    [Flags]
    internal enum DuplicateHandleOptions
    {
        DUPLICATE_CLOSE_SOURCE = 0x1,
        DUPLICATE_SAME_ACCESS = 0x2
    }
    #endregion

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    internal sealed class SafeObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeObjectHandle()
            : base(true)
        { }

        internal SafeObjectHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(base.handle);
        }
    }

    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
    internal sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProcessHandle()
            : base(true)
        { }

        internal SafeProcessHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            base.SetHandle(preexistingHandle);
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(base.handle);
        }
    }

    #region Native Methods
    internal static class NativeMethods
    {
        [DllImport("ntdll.dll")]
        internal static extern NT_STATUS NtQuerySystemInformation(
            [In] SYSTEM_INFORMATION_CLASS SystemInformationClass,
            [In] IntPtr SystemInformation,
            [In] int SystemInformationLength,
            [Out] out int ReturnLength);

        [DllImport("ntdll.dll")]
        internal static extern NT_STATUS NtQueryObject(
            [In] IntPtr Handle,
            [In] OBJECT_INFORMATION_CLASS ObjectInformationClass,
            [In] IntPtr ObjectInformation,
            [In] int ObjectInformationLength,
            [Out] out int ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern SafeProcessHandle OpenProcess(
            [In] ProcessAccessRights dwDesiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            [In] int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(
            [In] IntPtr hSourceProcessHandle,
            [In] IntPtr hSourceHandle,
            [In] IntPtr hTargetProcessHandle,
            [Out] out SafeObjectHandle lpTargetHandle,
            [In] int dwDesiredAccess,
            [In, MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
            [In] DuplicateHandleOptions dwOptions);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetProcessId(
            [In] IntPtr Process);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(
            [In] IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int QueryDosDevice(
            [In] string lpDeviceName,
            [Out] StringBuilder lpTargetPath,
            [In] int ucchMax);
    }
    #endregion

    [ComVisible(true)]
    public static class DetectOpenFiles
    {
        private static Dictionary<string, string> deviceMap;
        private const string networkDevicePrefix = "\\Device\\LanmanRedirector\\";

        private const int MAX_PATH = 260;

        private enum SystemHandleType
        {
            OB_TYPE_UNKNOWN = 0,
            OB_TYPE_TYPE = 1,
            OB_TYPE_DIRECTORY,
            OB_TYPE_SYMBOLIC_LINK,
            OB_TYPE_TOKEN,
            OB_TYPE_PROCESS,
            OB_TYPE_THREAD,
            OB_TYPE_UNKNOWN_7,
            OB_TYPE_EVENT,
            OB_TYPE_EVENT_PAIR,
            OB_TYPE_MUTANT,
            OB_TYPE_UNKNOWN_11,
            OB_TYPE_SEMAPHORE,
            OB_TYPE_TIMER,
            OB_TYPE_PROFILE,
            OB_TYPE_WINDOW_STATION,
            OB_TYPE_DESKTOP,
            OB_TYPE_SECTION,
            OB_TYPE_KEY,
            OB_TYPE_PORT,
            OB_TYPE_WAITABLE_PORT,
            OB_TYPE_UNKNOWN_21,
            OB_TYPE_UNKNOWN_22,
            OB_TYPE_UNKNOWN_23,
            OB_TYPE_UNKNOWN_24,
            //OB_TYPE_CONTROLLER,
            //OB_TYPE_DEVICE,
            //OB_TYPE_DRIVER,
            OB_TYPE_IO_COMPLETION,
            OB_TYPE_FILE
        };

        private const int handleTypeTokenCount = 27;
        private static readonly string[] handleTypeTokens = new string[] { 
                "", "", "Directory", "SymbolicLink", "Token",
                "Process", "Thread", "Unknown7", "Event", "EventPair", "Mutant",
                "Unknown11", "Semaphore", "Timer", "Profile", "WindowStation",
                "Desktop", "Section", "Key", "Port", "WaitablePort",
                "Unknown21", "Unknown22", "Unknown23", "Unknown24", 
                "IoCompletion", "File"
            };

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_HANDLE_ENTRY
        {
            public int OwnerPid;
            public byte ObjectType;
            public byte HandleFlags;
            public short HandleValue;
            public int ObjectPointer;
            public int AccessMask;
        }

        public sealed class OpenFiles : IEnumerable<string>
        {
            private readonly List<int> processIds;

            public OpenFiles(List<int> processIds)
            {
                this.processIds = processIds;
            }

            #region IEnumerable<FileSystemInfo> Members

            public IEnumerator<string> GetEnumerator()
            {
                NT_STATUS ret;
                int length = 0x10000;
                // Loop, probing for required memory.


                do
                {
                    IntPtr ptr = IntPtr.Zero;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        RuntimeHelpers.PrepareConstrainedRegions();
                        try { }
                        finally
                        {
                            // CER guarantees that the address of the allocated 
                            // memory is actually assigned to ptr if an 
                            // asynchronous exception occurs.
                            ptr = Marshal.AllocHGlobal(length);
                        }
                        int returnLength;
                        ret = NativeMethods.NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemHandleInformation, ptr, length, out returnLength);
                        if (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH)
                        {
                            // Round required memory up to the nearest 64KB boundary.
                            length = ((returnLength + 0xffff) & ~0xffff);
                        }
                        else if (ret == NT_STATUS.STATUS_SUCCESS)
                        {
                            int handleCount = Marshal.ReadInt32(ptr);
                            int offset = sizeof(int);
                            int size = Marshal.SizeOf(typeof(SYSTEM_HANDLE_ENTRY));
                            for (int i = 0; i < handleCount; i++)
                            {
                                SYSTEM_HANDLE_ENTRY handleEntry = (SYSTEM_HANDLE_ENTRY)Marshal.PtrToStructure((IntPtr)((int)ptr + offset), typeof(SYSTEM_HANDLE_ENTRY));
                                if (processIds.Contains(handleEntry.OwnerPid))
                                {
                                    IntPtr handle = (IntPtr)handleEntry.HandleValue;
                                    SystemHandleType handleType;

                                    if (GetHandleType(handle, handleEntry.OwnerPid, out handleType) && handleType == SystemHandleType.OB_TYPE_FILE)
                                    {
                                        string devicePath;
                                        if (GetFileNameFromHandle(handle, handleEntry.OwnerPid, out devicePath))
                                        {string dosPath;
                                            if (devicePath.StartsWith(@"\Device\Mup\"))
                                            {
                                                yield return @"\\" + devicePath.Substring(12);
                                            }
                                            else if (ConvertDevicePathToDosPath(devicePath, out dosPath))
                                            {
                                                yield return dosPath;
                                            }
                                        }
                                    }
                                }
                                offset += size;
                            }
                        }
                    }
                    finally
                    {
                        // CER guarantees that the allocated memory is freed, 
                        // if an asynchronous exception occurs. 
                        Marshal.FreeHGlobal(ptr);
                        //sw.Flush();
                        //sw.Close();
                    }
                }
                while (ret == NT_STATUS.STATUS_INFO_LENGTH_MISMATCH);
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #region Private Members

        private static bool GetFileNameFromHandle(IntPtr handle, int processId, out string fileName)
        {
            IntPtr currentProcess = NativeMethods.GetCurrentProcess();
            bool remote = (processId != NativeMethods.GetProcessId(currentProcess));
            SafeProcessHandle processHandle = null;
            SafeObjectHandle objectHandle = null;
            try
            {
                if (remote)
                {
                    processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId);
                    if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess, out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS))
                    {
                        handle = objectHandle.DangerousGetHandle();
                    }
                }
                return GetFileNameFromHandle(handle, out fileName, 200);
            }
            finally
            {
                if (remote)
                {
                    if (processHandle != null)
                    {
                        processHandle.Close();
                    }
                    if (objectHandle != null)
                    {
                        objectHandle.Close();
                    }
                }
            }
        }
        private static bool GetFileNameFromHandle(IntPtr handle, out string fileName, int wait)
        {
            using (FileNameFromHandleState f = new FileNameFromHandleState(handle))
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(GetFileNameFromHandle), f);
                if (f.WaitOne(wait))
                {
                    fileName = f.FileName;
                    return f.RetValue;
                }
                else
                {
                    fileName = string.Empty;
                    return false;
                }
            }
        }

        private class FileNameFromHandleState : IDisposable
        {
            private ManualResetEvent _mr;
            private IntPtr _handle;
            private string _fileName;
            private bool _retValue;

            public IntPtr Handle
            {
                get
                {
                    return _handle;
                }
            }

            public string FileName
            {
                get
                {
                    return _fileName;
                }
                set
                {
                    _fileName = value;
                }

            }

            public bool RetValue
            {
                get
                {
                    return _retValue;
                }
                set
                {
                    _retValue = value;
                }
            }

            public FileNameFromHandleState(IntPtr handle)
            {
                _mr = new ManualResetEvent(false);
                this._handle = handle;
            }

            public bool WaitOne(int wait)
            {
               return _mr.WaitOne(wait, false);
            }

            public void Set()
            {
                _mr.Set();
            }
            #region IDisposable Members

            public void Dispose()
            {
                if (_mr != null)
                    _mr.Close();
            }

            #endregion
        }

        private static void GetFileNameFromHandle(object state)
        {
            FileNameFromHandleState s = (FileNameFromHandleState)state;
            string fileName;
            s.RetValue = GetFileNameFromHandle(s.Handle, out fileName);
            s.FileName = fileName;

            try
            {
                s.Set();
            }
            catch (ObjectDisposedException)
            {
                // Fun fact of the day:
                // For some reason on some occasions s.Set() may decide to fire
                // ObjectDisposedException every single fucking call each ten minutes.
                // If the user decided to enable exceptions, he/she will be flooded
                // will thousands of TaskDialogs. Not fun for him/her.
                // If the user didn't choose to view exceptions, the software would
                // try to create thousands of HTTP connections to my server and send the exception report.
                // Imagine this happening randomly when you have ~1k users online at any given time.
                // I wondered why my server would spawn a bunch of php-cgi instances
                // then die because the VPS ran out of memory...
                // Lesson learned about rate-limiting things.
            }
        }

        private static bool GetFileNameFromHandle(IntPtr handle, out string fileName)
        {
            IntPtr ptr = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                int length = 0x200;  // 512 bytes
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    // CER guarantees the assignment of the allocated 
                    // memory address to ptr, if an ansynchronous exception 
                    // occurs.
                    ptr = Marshal.AllocHGlobal(length);
                }
                NT_STATUS ret = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr, length, out length);
                if (ret == NT_STATUS.STATUS_BUFFER_OVERFLOW)
                {
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try { }
                    finally
                    {
                        // CER guarantees that the previous allocation is freed,
                        // and that the newly allocated memory address is 
                        // assigned to ptr if an asynchronous exception occurs.
                        Marshal.FreeHGlobal(ptr);
                        ptr = Marshal.AllocHGlobal(length);
                    }
                    ret = NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectNameInformation, ptr, length, out length);
                }
                if (ret == NT_STATUS.STATUS_SUCCESS)
                {
                    fileName = Marshal.PtrToStringUni((IntPtr)((int)ptr + 8), (length - 9) / 2);
                    return fileName.Length != 0;
                }
            }
            finally
            {
                // CER guarantees that the allocated memory is freed, 
                // if an asynchronous exception occurs.
                Marshal.FreeHGlobal(ptr);
            }

            fileName = string.Empty;
            return false;
        }

        private static bool GetHandleType(IntPtr handle, int processId, out SystemHandleType handleType)
        {
            string token = GetHandleTypeToken(handle, processId);
            return GetHandleTypeFromToken(token, out handleType);
        }

        private static bool GetHandleType(IntPtr handle, out SystemHandleType handleType)
        {
            string token = GetHandleTypeToken(handle);
            return GetHandleTypeFromToken(token, out handleType);
        }

        private static bool GetHandleTypeFromToken(string token, out SystemHandleType handleType)
        {
            for (int i = 1; i < handleTypeTokenCount; i++)
            {
                if (handleTypeTokens[i] == token)
                {
                    handleType = (SystemHandleType)i;
                    return true;
                }
            }
            handleType = SystemHandleType.OB_TYPE_UNKNOWN;
            return false;
        }

        private static string GetHandleTypeToken(IntPtr handle, int processId)
        {
            IntPtr currentProcess = NativeMethods.GetCurrentProcess();
            bool remote = (processId != NativeMethods.GetProcessId(currentProcess));
            SafeProcessHandle processHandle = null;
            SafeObjectHandle objectHandle = null;
            try
            {
                if (remote)
                {
                    processHandle = NativeMethods.OpenProcess(ProcessAccessRights.PROCESS_DUP_HANDLE, true, processId);
                    if (NativeMethods.DuplicateHandle(processHandle.DangerousGetHandle(), handle, currentProcess, out objectHandle, 0, false, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS))
                    {
                        handle = objectHandle.DangerousGetHandle();
                    }
                }
                return GetHandleTypeToken(handle);
            }
            finally
            {
                if (remote)
                {
                    if (processHandle != null)
                    {
                        processHandle.Close();
                    }
                    if (objectHandle != null)
                    {
                        objectHandle.Close();
                    }
                }
            }
        }

        private static string GetHandleTypeToken(IntPtr handle)
        {
            int length;
            NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, IntPtr.Zero, 0, out length);
            if (length < 0)
            {
                return string.Empty;
            }
            IntPtr ptr = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try { }
                finally
                {
                    ptr = Marshal.AllocHGlobal(length);
                }
                if (NativeMethods.NtQueryObject(handle, OBJECT_INFORMATION_CLASS.ObjectTypeInformation, ptr, length, out length) == NT_STATUS.STATUS_SUCCESS)
                {
                    return Marshal.PtrToStringUni((IntPtr)((int)ptr + 0x60));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return string.Empty;
        }

        private static bool ConvertDevicePathToDosPath(string devicePath, out string dosPath)
        {
            EnsureDeviceMap();
            int i = devicePath.Length;
            while (i > 0 && (i = devicePath.LastIndexOf('\\', i - 1)) != -1)
            {
                string drive;
                if (deviceMap.TryGetValue(devicePath.Substring(0, i), out drive))
                {
                    dosPath = string.Concat(drive, devicePath.Substring(i));
                    return dosPath.Length != 0;
                }
            }
            dosPath = string.Empty;
            return false;
        }

        private static void EnsureDeviceMap()
        {
            if (deviceMap == null)
            {
                Dictionary<string, string> localDeviceMap = BuildDeviceMap();
                Interlocked.CompareExchange<Dictionary<string, string>>(ref deviceMap, localDeviceMap, null);
            }
        }

        private static Dictionary<string, string> BuildDeviceMap()
        {
            string[] logicalDrives = Environment.GetLogicalDrives();
            Dictionary<string, string> localDeviceMap = new Dictionary<string, string>(logicalDrives.Length);
            StringBuilder lpTargetPath = new StringBuilder(MAX_PATH);
            foreach (string drive in logicalDrives)
            {
                string lpDeviceName = drive.Substring(0, 2);
                NativeMethods.QueryDosDevice(lpDeviceName, lpTargetPath, MAX_PATH);
                localDeviceMap.Add(NormalizeDeviceName(lpTargetPath.ToString()), lpDeviceName);
            }
            localDeviceMap.Add(networkDevicePrefix.Substring(0, networkDevicePrefix.Length - 1), "\\");
            return localDeviceMap;
        }

        private static string NormalizeDeviceName(string deviceName)
        {
            if (string.Compare(deviceName, 0, networkDevicePrefix, 0, networkDevicePrefix.Length, StringComparison.InvariantCulture) == 0)
            {
                string shareName = deviceName.Substring(deviceName.IndexOf('\\', networkDevicePrefix.Length) + 1);
                return string.Concat(networkDevicePrefix, shareName);
            }
            return deviceName;
        }

        #endregion
    }
}
