//
//  Code originates from http://stackoverflow.com/a/3504251/156626
//  The earlier implementation was not compatible with x64, and after
//  rewriting half of the pointer-related codes, it still wouldn't work.
//  This code was modified slightly to use yield and recognize networked paths.
//  Also, to prevent lock-ups in GetFilePath(), I added thread abortion logic
//  similar to the one used in the earlier DetectOpenFiles implementation.
//  Added Handle and OpenedFilesView output parser implementations as backup.
//

using CookComputing.XmlRpc;

namespace RoliSoft.TVShowTracker.Dependencies.DetectOpenFiles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;

    public static class DetectOpenFiles
    {
        public static IEnumerable<string> SysinternalsGetFilesLockedBy(List<int> pids)
        {
            var res = Utils.RunAndRead("handle", "-accepteula", true).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (res.Length < 3)
            {
                Log.Error("Empty or erroneous result from third-party process monitoring application handle.exe. Make sure it exists in your %PATH% and you can elevate to admin rights.");
                yield break;
            }

            var pid = -1;
            var skip = true;
            var need = false;

            for (int i = 0; i < res.Length; i++)
            {
                if (skip && res[i] == "Initialization error:")
                {
                    Log.Error("Third-party process monitoring application handle.exe failed to initialize due to an error: " + res[i + 1]);
                    yield break;
                }

                if (skip && res[i] == "------------------------------------------------------------------------------")
                {
                    skip = false;
                    continue;
                }

                if (skip)
                {
                    continue;
                }

                if (res[i] == "------------------------------------------------------------------------------")
                {
                    pid = -1;
                    need = false;
                    continue;
                }

                if (pid == -1)
                {
                    var m = Regex.Match(res[i], @" pid: (\d+) ");

                    if (!m.Success || !m.Groups[1].Success)
                    {
                        continue;
                    }

                    pid = int.Parse(m.Groups[1].Value);
                    need = pids.Contains(pid);
                    continue;
                }

                if (need)
                {
                    var m = Regex.Match(res[i], @"^ *[0-9A-F]+: File  \(...\)   (.+)$");

                    if (!m.Success || !m.Groups[1].Success)
                    {
                        continue;
                    }

                    yield return m.Groups[1].Value;
                }
            }
        }

        public static IEnumerable<string> NirSoftGetFilesLockedBy(List<int> pids)
        {
            var lst = Utils.GetRandomFileName(".txt");

            if (Utils.IsAdmin)
            {
                Utils.Run("OpenedFilesView", "/nosort /stab \"" + lst + "\"");
            }
            else
            {
                Utils.RunElevated("OpenedFilesView", "/nosort /stab \"" + lst + "\"");
            }

            if (!File.Exists(lst) || new FileInfo(lst).Length < 3)
            {
                try { File.Delete(lst); } catch { }
                Log.Error("Empty or erroneous result from third-party process monitoring application OpenedFilesView.exe. Make sure it exists in your %PATH%, you can elevate to admin rights and test driver signing is enabled if you're on a 64-bit operating system.");
                yield break;
            }

            string[] res;

            try
            {
                res = File.ReadAllLines(lst);
            }
            catch (Exception ex)
            {
                Log.Error("Error while reading third-party process monitoring application OpenedFilesView.exe output from " + lst, ex);
                yield break;
            }
            finally
            {
                try { File.Delete(lst); } catch { }
            }

            if (res.Length < 3)
            {
                Log.Error("Empty or erroneous result from third-party process monitoring application OpenedFilesView.exe. Make sure it exists in your %PATH%, you can elevate to admin rights and test driver signing is enabled if you're on a 64-bit operating system.");
                yield break;
            }

            foreach (var ln in res)
            {
                var lns = ln.Split("\t".ToCharArray(), StringSplitOptions.None);

                int pid;
                if (lns.Length > 15 && int.TryParse(lns[15], out pid) && pids.Contains(pid))
                {
                    yield return lns[1];
                }
            }
        }

        public static IEnumerable<string> UnsafeGetFilesLockedBy(List<int> pids)
        {
            if (!Utils.IsAdmin)
            {
                Log.Error("You are not running with administrator rights, therefore you cannot use the internal implementation.");
                yield break;
            }

            foreach (var handle in GetHandles(pids))
            {
                var path = string.Empty;
                var hwnd = IntPtr.Zero;
                var hinf = handle;

                using (var mre = new ManualResetEvent(false))
                {
                    var thd = new Thread(() =>
                        {
                            try
                            {
                                path = GetFilePath(hinf, ref hwnd);
                                mre.Set();
                            }
                            catch { }
                        })
                        {
                            IsBackground = true
                        };

                    thd.Start();

                    if (!mre.WaitOne(200))
                    {
                        new Thread(() =>
                            {
                                try
                                {
                                    Win32API.CancelIo(hwnd);
                                    Win32API.CloseHandle(hwnd);
                                    thd.Interrupt();
                                    thd.Abort();
                                }
                                catch { }
                            }).Start();
                    }
                }

                if (!string.IsNullOrEmpty(path))
                {
                    yield return path;
                }
            }
        }

        const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
        private static string GetFilePath(Win32API.SYSTEM_HANDLE_INFORMATION systemHandleInformation, ref IntPtr ipHandle)
        {
            var ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, systemHandleInformation.ProcessID);
            var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
            var objObjectType = new Win32API.OBJECT_TYPE_INFORMATION();
            var objObjectName = new Win32API.OBJECT_NAME_INFORMATION();
            var strObjectName = "";
            var nLength = 0;

            if (!Win32API.DuplicateHandle(ipProcessHwnd, systemHandleInformation.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_SAME_ACCESS))
            {
                //Log.Trace("GetFilePath(" + systemHandleInformation.ProcessID + ", 0x" + systemHandleInformation.Handle.ToString("X") + ") : DuplicateHandle(0x" + ipProcessHwnd.ToString("X") + ", 0x" + systemHandleInformation.Handle.ToString("X") + ") returned false.");
                return null;
            }

            IntPtr ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
            Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation, ipBasic, Marshal.SizeOf(objBasic), ref nLength);
            objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
            Marshal.FreeHGlobal(ipBasic);

            IntPtr ipObjectType = Marshal.AllocHGlobal(objBasic.TypeInformationLength);
            nLength = objBasic.TypeInformationLength;
            // this one never locks...
            while ((uint)(Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectTypeInformation, ipObjectType, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                if (nLength == 0)
                {
                    Log.Trace("GetFilePath(" + systemHandleInformation.ProcessID + ", 0x" + systemHandleInformation.Handle.ToString("X") + ") : NtQueryObject(0x" + ipHandle.ToString("X") + ", ObjectTypeInformation) returned !STATUS_INFO_LENGTH_MISMATCH when nLength == 0.");
                    return null;
                }
                Marshal.FreeHGlobal(ipObjectType);
                ipObjectType = Marshal.AllocHGlobal(nLength);
            }

            // TODO: check if this code still works on Windows 7, now that UNICODE_STRING doesn't have Pack = 1 defined

            objObjectType = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(ipObjectType, objObjectType.GetType());
            var strObjectTypeName = Marshal.PtrToStringUni(objObjectType.Name.Buffer, objObjectType.Name.Length >> 1);
            Marshal.FreeHGlobal(ipObjectType);
            if (strObjectTypeName != "File")
                return null;

            nLength = objBasic.NameInformationLength;

            var ipObjectName = Marshal.AllocHGlobal(nLength);

            // ...this call sometimes hangs due to a Windows error.
            while ((uint)(Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectNameInformation, ipObjectName, nLength, ref nLength)) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectName);
                if (nLength == 0)
                {
                    Log.Trace("GetFilePath(" + systemHandleInformation.ProcessID + ", 0x" + systemHandleInformation.Handle.ToString("X") + ") : NtQueryObject(0x" + ipHandle.ToString("X") + ", ObjectNameInformation) returned !STATUS_INFO_LENGTH_MISMATCH when nLength == 0.");
                    return null;
                }
                ipObjectName = Marshal.AllocHGlobal(nLength);
            }
            objObjectName = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(ipObjectName, objObjectName.GetType());

            if (objObjectName.Name.Buffer.ToInt64() > 0 && nLength > 0)
            {

                var baTemp = new byte[nLength];
                try
                {
                    Marshal.Copy(objObjectName.Name.Buffer, baTemp, 0, nLength);

                    strObjectName = Marshal.PtrToStringUni(objObjectName.Name.Buffer);
                }
                catch (AccessViolationException ex)
                {
                    Log.Trace("GetFilePath(" + systemHandleInformation.ProcessID + ", 0x" + systemHandleInformation.Handle.ToString("X") + ") Error while marshaling file name.", ex);
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(ipObjectName);
                    Win32API.CloseHandle(ipHandle);
                }
            }

            if (strObjectName == null)
            {
                return null;
            }

            if (strObjectName.StartsWith(@"\Device\Mup\"))
            {
                return @"\\" + strObjectName.Substring(12);
            }

            try
            {
                return GetRegularFileNameFromDevice(strObjectName);
            }
            catch
            {
                return null;
            }
        }

        private static string GetRegularFileNameFromDevice(string strRawName)
        {
            string strFileName = strRawName;
            foreach (string strDrivePath in Environment.GetLogicalDrives())
            {
                var sbTargetPath = new StringBuilder(Win32API.MAX_PATH);
                if (Win32API.QueryDosDevice(strDrivePath.Substring(0, 2), sbTargetPath, Win32API.MAX_PATH) == 0)
                {
                    return strRawName;
                }
                string strTargetPath = sbTargetPath.ToString();
                if (strFileName.StartsWith(strTargetPath))
                {
                    strFileName = strFileName.Replace(strTargetPath, strDrivePath.Substring(0, 2));
                    break;
                }
            }
            return strFileName;
        }

        private static IEnumerable<Win32API.SYSTEM_HANDLE_INFORMATION> GetHandles(List<int> pids)
        {
            var nHandleInfoSize = 0x10000;
            var ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
            var nLength = 0;
            IntPtr ipHandle;

            while (Win32API.NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer, nHandleInfoSize, ref nLength) == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                nHandleInfoSize = nLength;
                Marshal.FreeHGlobal(ipHandlePointer);
                ipHandlePointer = Marshal.AllocHGlobal(nLength);
            }

            var baTemp = new byte[nLength];
            Marshal.Copy(ipHandlePointer, baTemp, 0, nLength);

            long lHandleCount;
            if (Utils.Is64Bit)
            {
                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
            }
            else
            {
                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
            }

            for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
            {
                var shHandle = new Win32API.SYSTEM_HANDLE_INFORMATION();
                if (Utils.Is64Bit)
                {
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                }
                else
                {
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                }

                if (pids.Contains(shHandle.ProcessID))
                {
                    yield return shHandle;
                }
            }
        }

        internal class Win32API
        {
            [DllImport("ntdll.dll")]
            public static extern int NtQueryObject(IntPtr ObjectHandle, int
                ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength,
                ref int returnLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

            [DllImport("ntdll.dll")]
            public static extern uint NtQuerySystemInformation(int
                SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength,
                ref int returnLength);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
            [DllImport("kernel32.dll")]
            public static extern bool CloseHandle(IntPtr hObject);
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
               ushort hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
               uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentProcess();
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] StringBuilder path, int bufLen, int flags);
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CancelIo(IntPtr hFile);

            public enum ObjectInformationClass
            {
                ObjectBasicInformation = 0,
                ObjectNameInformation = 1,
                ObjectTypeInformation = 2,
                ObjectAllTypesInformation = 3,
                ObjectHandleInformation = 4
            }

            [Flags]
            public enum ProcessAccessFlags : uint
            {
                All = 0x001F0FFF,
                Terminate = 0x00000001,
                CreateThread = 0x00000002,
                VMOperation = 0x00000008,
                VMRead = 0x00000010,
                VMWrite = 0x00000020,
                DupHandle = 0x00000040,
                SetInformation = 0x00000200,
                QueryInformation = 0x00000400,
                Synchronize = 0x00100000
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_BASIC_INFORMATION
            { // Information Class 0
                public int Attributes;
                public int GrantedAccess;
                public int HandleCount;
                public int PointerCount;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int NameInformationLength;
                public int TypeInformationLength;
                public int SecurityDescriptorLength;
                public System.Runtime.InteropServices.ComTypes.FILETIME CreateTime;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_TYPE_INFORMATION
            { // Information Class 2
                public UNICODE_STRING Name;
                public int ObjectCount;
                public int HandleCount;
                public int Reserved1;
                public int Reserved2;
                public int Reserved3;
                public int Reserved4;
                public int PeakObjectCount;
                public int PeakHandleCount;
                public int Reserved5;
                public int Reserved6;
                public int Reserved7;
                public int Reserved8;
                public int InvalidAttributes;
                public GENERIC_MAPPING GenericMapping;
                public int ValidAccess;
                public byte Unknown;
                public byte MaintainHandleDatabase;
                public int PoolType;
                public int PagedPoolUsage;
                public int NonPagedPoolUsage;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct OBJECT_NAME_INFORMATION
            { // Information Class 1
                public UNICODE_STRING Name;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct UNICODE_STRING
            {
                public ushort Length;
                public ushort MaximumLength;
                public IntPtr Buffer;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct GENERIC_MAPPING
            {
                public int GenericRead;
                public int GenericWrite;
                public int GenericExecute;
                public int GenericAll;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct SYSTEM_HANDLE_INFORMATION
            { // Information Class 16
                public int ProcessID;
                public byte ObjectTypeNumber;
                public byte Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
                public ushort Handle;
                public int Object_Pointer;
                public UInt32 GrantedAccess;
            }

            public const int MAX_PATH = 260;
            public const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
            public const int DUPLICATE_SAME_ACCESS = 0x2;
            public const uint FILE_SEQUENTIAL_ONLY = 0x00000004;
        }
    }
}
