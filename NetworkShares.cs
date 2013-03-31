namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Provides the ability to fetch a list of transferring shared files.
    /// </summary>
    public static class NetworkShares
    {
        /// <summary>
        /// Enumerates the currently transfered files.
        /// </summary>
        /// <returns>List of active transfer names.</returns>
        public static IEnumerable<FileInfo> GetActiveTransfers()
        {
            int dwReadEntries;
            int dwTotalEntries;
            var pBuffer = IntPtr.Zero;
            var pCurrent = new NativeMethods.FILE_INFO_3();

            if (NativeMethods.NetFileEnum(null, null, null, 3, ref pBuffer, -1, out dwReadEntries, out dwTotalEntries, IntPtr.Zero) != NativeMethods.NET_API_STATUS.NERR_Success)
            {
                yield break;
            }

            for (var i = 0; i < dwReadEntries; i++)
            {
                var iPtr = new IntPtr(pBuffer.ToInt32() + (i * Marshal.SizeOf(pCurrent)));
                pCurrent = (NativeMethods.FILE_INFO_3)Marshal.PtrToStructure(iPtr, typeof(NativeMethods.FILE_INFO_3));

                if (File.Exists(pCurrent.fi3_pathname))
                {
                    yield return new FileInfo(pCurrent.fi3_pathname);
                }
            }

            NativeMethods.NetApiBufferFree(pBuffer);
        }

        private static class NativeMethods
        {
            [DllImport("netapi32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
            public static extern NET_API_STATUS NetFileEnum(
                 string servername,
                 string basepath,
                 string username,
                 int level,
                 ref IntPtr bufptr,
                 int prefmaxlen,
                 out int entriesread,
                 out int totalentries,
                 IntPtr resume_handle
            );

            [DllImport("netapi32.dll")]
            public static extern uint NetApiBufferFree(IntPtr Buffer);

            public enum NET_API_STATUS : uint
            {
                NERR_Success = 0,
                NERR_InvalidComputer = 2351,
                NERR_NotPrimary = 2226,
                NERR_SpeGroupOp = 2234,
                NERR_LastAdmin = 2452,
                NERR_BadPassword = 2203,
                NERR_PasswordTooShort = 2245,
                NERR_UserNotFound = 2221,
                ERROR_ACCESS_DENIED = 5,
                ERROR_NOT_ENOUGH_MEMORY = 8,
                ERROR_INVALID_PARAMETER = 87,
                ERROR_INVALID_NAME = 123,
                ERROR_INVALID_LEVEL = 124,
                ERROR_MORE_DATA = 234,
                ERROR_SESSION_CREDENTIAL_CONFLICT = 1219
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public struct FILE_INFO_3
            {
                public int fi3_id;
                public int fi3_permission;
                public int fi3_num_locks;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string fi3_pathname;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string fi3_username;
            }
        }
    }
}
