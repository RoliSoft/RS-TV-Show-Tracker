namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static partial class Utils
    {
        // ReSharper disable InconsistentNaming
        /// <summary>
        /// Provides access to Win32 API functions.
        /// </summary>
        public static class Interop
        {
            #region Icons
            /// <summary>
            /// Creates an array of handles to large or small icons extracted from the specified executable file, DLL, or icon file.
            /// </summary>
            /// <param name="lpszFile">The name of an executable file, DLL, or icon file from which icons will be extracted.</param>
            /// <param name="nIconIndex">The zero-based index of the first icon to extract. For example, if this value is zero, the function extracts the first icon in the specified file.</param>
            /// <param name="phiconLarge">An array of icon handles that receives handles to the large icons extracted from the file. If this parameter is NULL, no large icons are extracted from the file.</param>
            /// <param name="phiconSmall">An array of icon handles that receives handles to the small icons extracted from the file. If this parameter is NULL, no small icons are extracted from the file.</param>
            /// <param name="nIcons">The number of icons to be extracted from the file.</param>
            /// <returns>If the nIconIndex parameter is -1, the phiconLarge parameter is NULL, and the phiconSmall parameter is NULL, then the return value is the number of icons contained in the specified file. Otherwise, the return value is the number of icons successfully extracted from the file.</returns>
            [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
            public static extern int ExtractIconExW(string lpszFile, int nIconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);

            /// <summary>
            /// Destroys an icon and frees any memory the icon occupied.
            /// </summary>
            /// <param name="hIcon">A handle to the icon to be destroyed. The icon must not be in use.</param>
            /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
            [DllImport("user32.dll")]
            public static extern int DestroyIcon(IntPtr hIcon);
            #endregion

            #region Symbolic links
            /// <summary>
            /// Creates a symbolic link.
            /// </summary>
            /// <param name="lpSymlinkFileName">The symbolic link to be created.</param>
            /// <param name="lpTargetFileName">The name of the target for the symbolic link to be created.</param>
            /// <param name="dwFlags">Indicates whether the link target, lpTargetFileName, is a directory.</param>
            /// <returns></returns>
            [DllImport("kernel32.dll")]
            public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLinkFlags dwFlags);

            public enum SymbolicLinkFlags : uint
            {
                SYMBLOC_LINK_FLAG_FILE      = 0x0,
                SYMBLOC_LINK_FLAG_DIRECTORY = 0x1
            }
            #endregion

            #region File copy/move with progress
            /// <summary>
            /// Copies an existing file to a new file, notifying the application of its progress through a callback function.
            /// </summary>
            /// <param name="lpExistingFileName">The name of an existing file.</param>
            /// <param name="lpNewFileName">The name of the new file.</param>
            /// <param name="lpProgressRoutine">The address of a callback function of type LPPROGRESS_ROUTINE that is called each time another portion of the file has been copied. This parameter can be NULL. For more information on the progress callback function, see the CopyProgressRoutine function.</param>
            /// <param name="lpData">The argument to be passed to the callback function. This parameter can be NULL.</param>
            /// <param name="pbCancel">If this flag is set to TRUE during the copy operation, the operation is canceled. Otherwise, the copy operation will continue to completion.</param>
            /// <param name="dwCopyFlags">Flags that specify how the file is to be copied. This parameter can be a combination of the following values.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. To get extended error information call GetLastError.
            /// </returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CopyFileEx(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, ref Int32 pbCancel, CopyFileFlags dwCopyFlags);

            /// <summary>
            /// Moves a file or directory, including its children. You can provide a callback function that receives progress notifications.
            /// </summary>
            /// <param name="lpExistingFileName">The name of the existing file or directory on the local computer.</param>
            /// <param name="lpNewFileName">The new name of the file or directory on the local computer.</param>
            /// <param name="lpProgressRoutine">A pointer to a CopyProgressRoutine callback function that is called each time another portion of the file has been moved. The callback function can be useful if you provide a user interface that displays the progress of the operation. This parameter can be NULL.</param>
            /// <param name="lpData">An argument to be passed to the CopyProgressRoutine callback function. This parameter can be NULL.</param>
            /// <param name="dwFlags">The move options. This parameter can be one or more of the following values.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool MoveFileWithProgress(string lpExistingFileName, string lpNewFileName, CopyProgressRoutine lpProgressRoutine, IntPtr lpData, MoveFileFlags dwFlags);

            /// <summary>
            /// An application-defined callback function used with the CopyFileEx, MoveFileTransacted, and MoveFileWithProgress functions. It is called when a portion of a copy or move operation is completed. The LPPROGRESS_ROUTINE type defines a pointer to this callback function. CopyProgressRoutine is a placeholder for the application-defined function name.
            /// </summary>
            public delegate CopyProgressResult CopyProgressRoutine(long TotalFileSize, long TotalBytesTransferred, long StreamSize, long StreamBytesTransferred, uint dwStreamNumber, CopyProgressCallbackReason dwCallbackReason, IntPtr hSourceFile, IntPtr hDestinationFile, IntPtr lpData);

            public enum CopyProgressResult : uint
            {
                PROGRESS_CONTINUE = 0,
                PROGRESS_CANCEL   = 1,
                PROGRESS_STOP     = 2,
                PROGRESS_QUIET    = 3
            }

            public enum CopyProgressCallbackReason : uint
            {
                CALLBACK_CHUNK_FINISHED = 0x00000000,
                CALLBACK_STREAM_SWITCH  = 0x00000001
            }

            [Flags]
            public enum CopyFileFlags : uint
            {
                COPY_FILE_FAIL_IF_EXISTS              = 0x00000001,
                COPY_FILE_RESTARTABLE                 = 0x00000002,
                COPY_FILE_OPEN_SOURCE_FOR_WRITE       = 0x00000004,
                COPY_FILE_ALLOW_DECRYPTED_DESTINATION = 0x00000008
            }

            [Flags]
            public enum MoveFileFlags : uint
            {
                MOVE_FILE_REPLACE_EXISTSING     = 0x00000001,
                MOVE_FILE_COPY_ALLOWED          = 0x00000002,
                MOVE_FILE_DELAY_UNTIL_REBOOT    = 0x00000004,
                MOVE_FILE_WRITE_THROUGH         = 0x00000008,
                MOVE_FILE_CREATE_HARDLINK       = 0x00000010,
                MOVE_FILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
            }
            #endregion

            #region Aero
            /// <summary>
            /// Extends the window frame into the client area.
            /// </summary>
            /// <param name="hWnd">The handle to the window in which the frame will be extended into the client area.</param>
            /// <param name="pMargins">A pointer to a <c>MARGINS</c> structure that describes the margins to use when extending the frame into the client area.</param>
            /// <returns>
            /// If this function succeeds, it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.
            /// </returns>
            [DllImport("dwmapi.dll")]
            public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);

            /// <summary>
            /// Obtains a value that indicates whether Desktop Window Manager (DWM) composition is enabled.
            /// Applications can listen for composition state changes by handling the <c>WM_DWMCOMPOSITIONCHANGED</c> notification.
            /// </summary>
            /// <returns>
            /// A pointer to a value that, when this function returns successfully, receives <c>TRUE</c> if DWM composition is enabled; otherwise, <c>FALSE</c>.
            /// </returns>
            [DllImport("dwmapi.dll", PreserveSig = false)]
            public static extern bool DwmIsCompositionEnabled();

            /// <summary>
            /// Returned by the <c>GetThemeMargins</c> function to define the margins of windows that have visual styles applied.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct MARGINS
            {
                /// <summary>
                /// Width of the left border that retains its size.
                /// </summary>
                public int cxLeftWidth;

                /// <summary>
                /// Width of the right border that retains its size.
                /// </summary>
                public int cxRightWidth;

                /// <summary>
                /// Width of the top border that retains its size.
                /// </summary>
                public int cyTopHeight;

                /// <summary>
                /// Width of the bottom border that retains its size.
                /// </summary>
                public int cyBottomHeight;
            }
            #endregion
        }
        // ReSharper restore InconsistentNaming
    }
}
