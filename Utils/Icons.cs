namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Microsoft.Win32;

    /// <summary>
    /// Provides various little utility functions.
    /// </summary>
    public static partial class Utils
    {
        /// <summary>
        /// Utilities for getting icons.
        /// </summary>
        public static class Icons
        {
            #region Custom exceptions class

            public class IconNotFoundException : Exception
            {
                public IconNotFoundException(string fileName, int index)
                    : base(string.Format("Icon with Id = {0} wasn't found in file {1}", index, fileName))
                {
                }
            }

            public class UnableToExtractIconsException : Exception
            {
                public UnableToExtractIconsException(string fileName, int firstIconIndex, int iconCount)
                    : base(string.Format("Tryed to extract {2} icons starting from the one with id {1} from the \"{0}\" file but failed", fileName, firstIconIndex, iconCount))
                {
                }
            }

            #endregion

            #region DllImports

            [Flags]
            enum FileInfoFlags : int
            {
                /// <summary>
                /// Retrieve the handle to the icon that represents the file and the index 
                /// of the icon within the system image list. The handle is copied to the 
                /// hIcon member of the structure specified by psfi, and the index is copied 
                /// to the iIcon member.
                /// </summary>
                SHGFI_ICON = 0x000000100,
                /// <summary>
                /// Indicates that the function should not attempt to access the file 
                /// specified by pszPath. Rather, it should act as if the file specified by 
                /// pszPath exists with the file attributes passed in dwFileAttributes.
                /// </summary>
                SHGFI_USEFILEATTRIBUTES = 0x000000010
            }

            /// <summary>
            ///     Creates an array of handles to large or small icons extracted from
            ///     the specified executable file, dynamic-link library (DLL), or icon
            ///     file. 
            /// </summary>
            /// <param name="lpszFile">
            ///     Name of an executable file, DLL, or icon file from which icons will
            ///     be extracted.
            /// </param>
            /// <param name="nIconIndex">
            ///     <para>
            ///         Specifies the zero-based index of the first icon to extract. For
            ///         example, if this value is zero, the function extracts the first
            ///         icon in the specified file.
            ///     </para>
            ///     <para>
            ///         If this value is �1 and <paramref name="phiconLarge"/> and
            ///         <paramref name="phiconSmall"/> are both NULL, the function returns
            ///         the total number of icons in the specified file. If the file is an
            ///         executable file or DLL, the return value is the number of
            ///         RT_GROUP_ICON resources. If the file is an .ico file, the return
            ///         value is 1. 
            ///     </para>
            ///     <para>
            ///         Windows 95/98/Me, Windows NT 4.0 and later: If this value is a 
            ///         negative number and either <paramref name="phiconLarge"/> or 
            ///         <paramref name="phiconSmall"/> is not NULL, the function begins by
            ///         extracting the icon whose resource identifier is equal to the
            ///         absolute value of <paramref name="nIconIndex"/>. For example, use -3
            ///         to extract the icon whose resource identifier is 3. 
            ///     </para>
            /// </param>
            /// <param name="phIconLarge">
            ///     An array of icon handles that receives handles to the large icons
            ///     extracted from the file. If this parameter is NULL, no large icons
            ///     are extracted from the file.
            /// </param>
            /// <param name="phIconSmall">
            ///     An array of icon handles that receives handles to the small icons
            ///     extracted from the file. If this parameter is NULL, no small icons
            ///     are extracted from the file. 
            /// </param>
            /// <param name="nIcons">
            ///     Specifies the number of icons to extract from the file. 
            /// </param>
            /// <returns>
            ///     If the <paramref name="nIconIndex"/> parameter is -1, the
            ///     <paramref name="phIconLarge"/> parameter is NULL, and the
            ///     <paramref name="phiconSmall"/> parameter is NULL, then the return
            ///     value is the number of icons contained in the specified file.
            ///     Otherwise, the return value is the number of icons successfully
            ///     extracted from the file. 
            /// </returns>
            [DllImport("Shell32", CharSet = CharSet.Auto)]
            extern static int ExtractIconEx(
                [MarshalAs(UnmanagedType.LPTStr)] 
            string lpszFile,
                int nIconIndex,
                IntPtr[] phIconLarge,
                IntPtr[] phIconSmall,
                int nIcons);

            #endregion

            /// <summary>
            /// Two constants extracted from the FileInfoFlags, the only that are
            /// meaningfull for the user of this class.
            /// </summary>
            public enum SystemIconSize : uint
            {
                Large = 0x0,
                Small = 0x1
            }

            /// <summary>
            /// Get the number of icons in the specified file.
            /// </summary>
            /// <param name="fileName">Full path of the file to look for.</param>
            /// <returns></returns>
            static int GetIconsCountInFile(string fileName)
            {
                return ExtractIconEx(fileName, -1, null, null, 0);
            }

            #region ExtractIcon-like functions

            public static void ExtractEx(string fileName, List<Icon> largeIcons,
                List<Icon> smallIcons, int firstIconIndex, int iconCount)
            {
                /*
                 * Memory allocations
                 */

                IntPtr[] smallIconsPtrs = null;
                IntPtr[] largeIconsPtrs = null;

                if (smallIcons != null)
                {
                    smallIconsPtrs = new IntPtr[iconCount];
                }
                if (largeIcons != null)
                {
                    largeIconsPtrs = new IntPtr[iconCount];
                }

                /*
                 * Call to native Win32 API
                 */

                int apiResult = ExtractIconEx(fileName, firstIconIndex, largeIconsPtrs, smallIconsPtrs, iconCount);
                if (apiResult != iconCount)
                {
                    throw new UnableToExtractIconsException(fileName, firstIconIndex, iconCount);
                }

                /*
                 * Fill lists
                 */

                if (smallIcons != null)
                {
                    smallIcons.Clear();
                    foreach (IntPtr actualIconPtr in smallIconsPtrs)
                    {
                        smallIcons.Add(Icon.FromHandle(actualIconPtr));
                    }
                }
                if (largeIcons != null)
                {
                    largeIcons.Clear();
                    foreach (IntPtr actualIconPtr in largeIconsPtrs)
                    {
                        largeIcons.Add(Icon.FromHandle(actualIconPtr));
                    }
                }
            }

            public static List<Icon> ExtractEx(string fileName, SystemIconSize size,
                int firstIconIndex, int iconCount)
            {
                List<Icon> iconList = new List<Icon>();

                switch (size)
                {
                    case SystemIconSize.Large:
                        ExtractEx(fileName, iconList, null, firstIconIndex, iconCount);
                        break;

                    case SystemIconSize.Small:
                        ExtractEx(fileName, null, iconList, firstIconIndex, iconCount);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("size");
                }

                return iconList;
            }

            public static void Extract(string fileName, List<Icon> largeIcons, List<Icon> smallIcons)
            {
                int iconCount = GetIconsCountInFile(fileName);
                ExtractEx(fileName, largeIcons, smallIcons, 0, iconCount);
            }

            public static List<Icon> Extract(string fileName, SystemIconSize size)
            {
                int iconCount = GetIconsCountInFile(fileName);
                return ExtractEx(fileName, size, 0, iconCount);
            }

            public static Icon ExtractOne(string fileName, int index, SystemIconSize size)
            {
                try
                {
                    List<Icon> iconList = ExtractEx(fileName, size, index, 1);
                    return iconList[0];
                }
                catch (UnableToExtractIconsException)
                {
                    throw new IconNotFoundException(fileName, index);
                }
            }

            public static void ExtractOne(string fileName, int index,
                out Icon largeIcon, out Icon smallIcon)
            {
                List<Icon> smallIconList = new List<Icon>();
                List<Icon> largeIconList = new List<Icon>();
                try
                {
                    ExtractEx(fileName, largeIconList, smallIconList, index, 1);
                    largeIcon = largeIconList[0];
                    smallIcon = smallIconList[0];
                }
                catch (UnableToExtractIconsException)
                {
                    throw new IconNotFoundException(fileName, index);
                }
            }

            #endregion

            //this will look throw the registry 
            //to find if the Extension have an icon.
            public static Icon IconFromExtension(string extension,
                                                    SystemIconSize size)
            {
                // Add the '.' to the extension if needed
                if (extension[0] != '.') extension = '.' + extension;

                //opens the registry for the wanted key.
                RegistryKey Root = Registry.ClassesRoot;
                RegistryKey ExtensionKey = Root.OpenSubKey(extension);
                ExtensionKey.GetValueNames();
                RegistryKey ApplicationKey =
                    Root.OpenSubKey(ExtensionKey.GetValue("").ToString());

                //gets the name of the file that have the icon.
                string IconLocation =
                    ApplicationKey.OpenSubKey("DefaultIcon").GetValue("").ToString();
                string[] IconPath = IconLocation.Split(',');

                if (IconPath[1] == null) IconPath[1] = "0";
                IntPtr[] Large = new IntPtr[1], Small = new IntPtr[1];

                //extracts the icon from the file.
                ExtractIconEx(IconPath[0],
                    Convert.ToInt16(IconPath[1]), Large, Small, 1);
                return size == SystemIconSize.Large ?
                    Icon.FromHandle(Large[0]) : Icon.FromHandle(Small[0]);
            }

            public static Icon IconFromExtensionShell(string extension, SystemIconSize size)
            {
                //add '.' if nessesry
                //if (extension[0] != '.') extension = '.' + extension;

                //temp struct for getting file shell info
                SHFILEINFO fileInfo = new SHFILEINFO();

                SHGetFileInfo(
                    extension,
                    0,
                    ref fileInfo,
                    (uint)Marshal.SizeOf(fileInfo),
                    (uint)FileInfoFlags.SHGFI_ICON | (uint)FileInfoFlags.SHGFI_USEFILEATTRIBUTES | (uint)size);

                if (fileInfo.hIcon != IntPtr.Zero)
                {
                    return Icon.FromHandle(fileInfo.hIcon);
                }

                return null;
            }

            public static Icon IconFromResource(string resourceName)
            {
                Assembly assembly = Assembly.GetCallingAssembly();

                return new Icon(assembly.GetManifestResourceStream(resourceName));
            }

            /// <summary>
            /// Parse strings in registry who contains the name of the icon and
            /// the index of the icon an return both parts.
            /// </summary>
            /// <param name="regString">The full string in the form "path,index" as found in registry.</param>
            /// <param name="fileName">The "path" part of the string.</param>
            /// <param name="index">The "index" part of the string.</param>
            public static void ExtractInformationsFromRegistryString(
                string regString, out string fileName, out int index)
            {
                if (regString == null)
                {
                    throw new ArgumentNullException("regString");
                }
                if (regString.Length == 0)
                {
                    throw new ArgumentException("The string should not be empty.", "regString");
                }

                index = 0;
                string[] strArr = regString.Replace("\"", "").Split(',');
                fileName = strArr[0].Trim();
                if (strArr.Length > 1)
                {
                    int.TryParse(strArr[1].Trim(), out index);
                }
            }

            public static Icon ExtractFromRegistryString(string regString, SystemIconSize size)
            {
                string fileName;
                int index;
                ExtractInformationsFromRegistryString(regString, out fileName, out index);
                return ExtractOne(fileName, index, size);
            }





            #region Win32api
            [System.Runtime.InteropServices.DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);

            [StructLayout(LayoutKind.Sequential)]
            internal struct SHFILEINFO
            {
                public IntPtr hIcon;
                public IntPtr iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
                public string szTypeName;
            };

            internal const uint SHGFI_ICON = 0x100;
            internal const uint SHGFI_TYPENAME = 0x400;
            internal const uint SHGFI_LARGEICON = 0x0; // 'Large icon
            internal const uint SHGFI_SMALLICON = 0x1; // 'Small icon
            internal const uint SHGFI_SYSICONINDEX = 16384;
            internal const uint SHGFI_USEFILEATTRIBUTES = 16;

            // <summary>
            /// Get Icons that are associated with files.
            /// To use it, use (System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon));
            /// hImgSmall = SHGetFileInfo(fName, 0, ref shinfo,(uint)Marshal.SizeOf(shinfo),Win32.SHGFI_ICON |Win32.SHGFI_SMALLICON);
            /// </summary>
            [DllImport("shell32.dll")]
            internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
                                                      ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

            // <summary>
            /// Return large file icon of the specified file.
            /// </summary>
            internal static Icon GetFileIcon(string fileName, uint size)
            {
                SHFILEINFO shinfo = new SHFILEINFO();

                uint flags = SHGFI_SYSICONINDEX;
                if (fileName.IndexOf(":") == -1)
                    flags = flags | SHGFI_USEFILEATTRIBUTES;
                if (size == SHGFI_SMALLICON)
                    flags = flags | SHGFI_ICON | SHGFI_SMALLICON;
                else flags = flags | SHGFI_ICON;

                SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
                return Icon.FromHandle(shinfo.hIcon);
            }
            #endregion

        }
    }
}
