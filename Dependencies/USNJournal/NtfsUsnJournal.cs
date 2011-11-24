//
//  Based on http://mftscanner.codeplex.com/SourceControl/changeset/view/2132#67339
//  Modifications:
//   - Removed some of the functions which weren't used in this software
//   - Commented, refactored and removed some small parts of the code
//   - Modified GetPathFromFileReference() to build up the full path
//     by navigating through the reference IDs, instead of using a Win32
//     API call, like the original does. This is much fucking faster.
//   - Merged GetNtfsVolumeFolders() and GetFilesMatchingFilter() so
//     you won't have to iterate through the MFT twice to get the
//     entries for the folders and files separately.
//   - Added optional Regex-based filter.
//  
//  Big thanks to StCroixSkipper!
//

namespace RoliSoft.TVShowTracker.Dependencies.USNJournal
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Accesses and parses the NTFS Master File Table entries.
    /// </summary>
    public class NtfsUsnJournal : IDisposable
    {
        private readonly DriveInfo _driveInfo;
        private readonly IntPtr _usnJournalRootHandle;

        /// <summary>
        /// Constructor for NtfsUsnJournal class.  If no exception is thrown, _usnJournalRootHandle and
        /// _volumeSerialNumber can be assumed to be good. If an exception is thrown, the NtfsUsnJournal
        /// object is not usable.
        /// </summary>
        /// <param name="driveInfo">DriveInfo object that provides access to information about a volume</param>
        /// <remarks> 
        /// An exception thrown if the volume is not an 'NTFS' volume or
        /// if GetRootHandle() or GetVolumeSerialNumber() functions fail. 
        /// Each public method checks to see if the volume is NTFS and if the _usnJournalRootHandle is
        /// valid.  If these two conditions aren't met, then the public function will return a UsnJournalReturnCode
        /// error.
        /// </remarks>
        public NtfsUsnJournal(DriveInfo driveInfo)
        {
            _driveInfo = driveInfo;

            if (_driveInfo.DriveFormat != "NTFS")
            {
                throw new Exception(_driveInfo.Name + " is not a NTFS partition.");
            }

            if(!GetRootHandle(out _usnJournalRootHandle))
            {
                throw new Win32Exception("Couldn't get root handle.");
            }
        }


        /// <summary>
        /// Gets the root handle.
        /// </summary>
        /// <param name="rootHandle">The root handle.</param>
        /// <returns>
        /// The success state of the <c>WinApi32.CreateFile</c> call.
        /// </returns>
        private bool GetRootHandle(out IntPtr rootHandle)
        {
            rootHandle = Win32Api.CreateFile(
                string.Concat(@"\\.\", _driveInfo.Name.TrimEnd('\\')),
                Win32Api.GENERIC_READ | Win32Api.GENERIC_WRITE,
                Win32Api.FILE_SHARE_READ | Win32Api.FILE_SHARE_WRITE,
                IntPtr.Zero,
                Win32Api.OPEN_EXISTING,
                0,
                IntPtr.Zero
            );

            return rootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE;
        }

        /// <summary>
        /// This function queries the usn journal on the volume.
        /// </summary>
        /// <param name="usnJournalState">the USN_JOURNAL_DATA object that is associated with this volume</param>
        /// <returns>
        /// The success state of the <c>WinApi32.DeviceIoControl</c> call.
        /// </returns>
        private bool QueryUsnJournal(ref Win32Api.USN_JOURNAL_DATA usnJournalState)
        {
            UInt32 cb;

            return Win32Api.DeviceIoControl(
                _usnJournalRootHandle,
                Win32Api.FSCTL_QUERY_USN_JOURNAL,
                IntPtr.Zero,
                0,
                out usnJournalState,
                Marshal.SizeOf(usnJournalState),
                out cb,
                IntPtr.Zero
            );
        }

        /// <summary>
        /// GetFileAndDirEntries() reads the Master File Table to find all of the files and
        /// folders on a volume and returns them individually.
        /// </summary>
        /// <param name="dirs">The directories.</param>
        /// <param name="files">The files.</param>
        /// <param name="filter">The filter.</param>
        private void GetFileAndDirEntries(out ConcurrentDictionary<ulong, Win32Api.UsnEntry> dirs, out ConcurrentBag<Win32Api.UsnEntry> files, Regex filter = null)
        {
            dirs  = new ConcurrentDictionary<ulong, Win32Api.UsnEntry>();
            files = new ConcurrentBag<Win32Api.UsnEntry>();

            var usnState = new Win32Api.USN_JOURNAL_DATA();

            if (!QueryUsnJournal(ref usnState))
            {
                throw new Win32Exception("Failed to query the USN journal on the volume.");
            }

            //
            // set up MFT_ENUM_DATA structure
            //
            Win32Api.MFT_ENUM_DATA med;
            med.StartFileReferenceNumber = 0;
            med.LowUsn = 0;
            med.HighUsn = usnState.NextUsn;
            Int32 sizeMftEnumData = Marshal.SizeOf(med);
            IntPtr medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
            Win32Api.ZeroMemory(medBuffer, sizeMftEnumData);
            Marshal.StructureToPtr(med, medBuffer, true);

            //
            // set up the data buffer which receives the USN_RECORD data
            //
            int pDataSize = sizeof (UInt64) + 10000;
            IntPtr pData = Marshal.AllocHGlobal(pDataSize);
            Win32Api.ZeroMemory(pData, pDataSize);
            uint outBytesReturned = 0;
            Win32Api.UsnEntry usnEntry = null;

            //
            // Gather up volume's directories
            //
            while (Win32Api.DeviceIoControl(
                _usnJournalRootHandle,
                Win32Api.FSCTL_ENUM_USN_DATA,
                medBuffer,
                sizeMftEnumData,
                pData,
                pDataSize,
                out outBytesReturned,
                IntPtr.Zero))
            {
                IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof (Int64));
                while (outBytesReturned > 60)
                {
                    usnEntry = new Win32Api.UsnEntry(pUsnRecord);

                    if (usnEntry.IsFile && (filter == null || filter.IsMatch(usnEntry.Name)))
                    {
                        files.Add(usnEntry);
                    }
                    if (usnEntry.IsFolder)
                    {
                        dirs.TryAdd(usnEntry.FileReferenceNumber, usnEntry);
                    }

                    pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usnEntry.RecordLength);
                    outBytesReturned -= usnEntry.RecordLength;
                }
                Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
            }

            Marshal.FreeHGlobal(pData);
        }

        /// <summary>
        /// Calls GetFileAndDirEntries() internally, and matches all of the file entries to their
        /// parent directory entries recursively, so it can generate a full path way much faster
        /// than the original function did in this library which placed Win32 API calls.
        /// </summary>
        /// <returns>
        /// List of all the files on the volume including their full path.
        /// </returns>
        public IEnumerable<string> GetParsedPaths(Regex filter = null, IEnumerable<string> pathFilters = null)
        {
            ConcurrentDictionary<ulong, Win32Api.UsnEntry> dirs;
            ConcurrentBag<Win32Api.UsnEntry> files;

            GetFileAndDirEntries(out dirs, out files, filter);

            var final = new ConcurrentBag<string>();

            Parallel.ForEach(files, file =>
                {
                    var names   = new Stack<string>();
                    var current = file;

                    while (true)
                    {
                        names.Push(current.Name);

                        if (!dirs.TryGetValue(current.ParentFileReferenceNumber, out current))
                        {
                            break;
                        }
                    }

                    var name = _driveInfo.Name + string.Join(@"\", names);

                    if (pathFilters == null || pathFilters.Any(pf => name.StartsWith(pf, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        final.Add(name);
                    }
                });

            return final;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Win32Api.CloseHandle(_usnJournalRootHandle);
        }
    }
}

