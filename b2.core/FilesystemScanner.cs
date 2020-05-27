using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace com.wibblr.b2
{
    /// <summary>
    /// Stores the file system info of a file or directory, and also the relative path of the file.directory from the
    /// root of the scan
    /// </summary>
    public class ScannedFileSystemInfo
    {
        public FileSystemInfo Info { get; private set; }
        public string RelativePath { get; private set; } = "";

        public long Length { get; private set; }

        public ScannedFileSystemInfo(FileSystemInfo info, ScannedFileSystemInfo parent = null)
        {
            Info = info;
            Length = new FileInfo(info.FullName).Length;
            RelativePath = (parent == null) 
                ? info.Name
                : Path.Combine(parent.RelativePath, info.Name);            
        }
    }

    /// <summary>
    /// Can scan either a file, a directory, or the contents of a directory ()
    /// </summary>
    public class FilesystemScanner
    {
        /// <summary>
        /// Return all files and directories within a path. The path can be a file (in which case it will be
        /// the only item returned), or a directory (in which case all the directory entries are returned).
        /// If the recursive flag is set, all subdirectories will also be scanned.
        /// 
        /// Files are returned before directories; directories are recursed as they are found, and all
        /// items are returned in alphabetical order.
        /// </summary>
        /// <param name="path">Root of the scan. Uses the same convention as rsync,
        /// where specifying a slash after the directory name means the *contents* of the directory</param>
        /// <param name="recursive">Whether to recurse into subdirectories</param>
        /// <returns>An IEnumerable of ScannedFile. The scanned file includes the file or directory info, and also
        /// the relative path to the file/directory from the scan root. If the scan root was a directory ending with 
        /// a slash, then that directory is not included in the relative path</returns>
        public static IEnumerable<ScannedFileSystemInfo> Scan(string path, bool recursive = true)
        {
            if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
            {
                ScannedFileSystemInfo parent = null;
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                if (path.Last() != (Path.DirectorySeparatorChar))
                {
                    var scannedFile = new ScannedFileSystemInfo(directoryInfo);
                    yield return scannedFile;
                    parent = scannedFile;
                }

                foreach (var scannedFile in ScanDirectoryContents(directoryInfo, recursive, parent))
                    yield return scannedFile;
            }
            else
                yield return new ScannedFileSystemInfo(new FileInfo(path));  
        }

        private static IEnumerable<ScannedFileSystemInfo> ScanDirectoryContents(DirectoryInfo directoryInfo, bool recursive, ScannedFileSystemInfo parent)
        {
            foreach (var fileInfo in directoryInfo.GetFiles().OrderBy(x => x.Name))
                yield return new ScannedFileSystemInfo(fileInfo, parent);

            foreach (var subdirectoryInfo in directoryInfo.GetDirectories().OrderBy(x => x.Name))
            {
                var scannedSubdirectory = new ScannedFileSystemInfo(subdirectoryInfo, parent);
                yield return scannedSubdirectory;

                if (recursive)
                {
                    foreach (var info in ScanDirectoryContents(subdirectoryInfo, recursive, scannedSubdirectory))
                        yield return info;
                }
            }
        }
    }
}
