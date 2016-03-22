using System;
using System.IO;

namespace com.wibblr.utils
{
    public class TempDirectory : IDisposable
    {
        public string Name { get; private set; }
        public string FullPath { get; private set; }

        /// <summary>
        /// Creates a temporary directory under the user's temp directory (i.e. the TEMP
        /// environment variable).
        /// 
        /// Optionally the temporary directory may be created in an intermediate subdirectory
        /// of the user's TEMP directory.
        /// 
        /// The temporary directory (but not the intermediate directory) will be automatically
        /// deleted when the TempDirectory object is disposed.
        /// </summary>
        public TempDirectory(string subdirectory = "")
        {
            var userTempDir = Environment.GetEnvironmentVariable("TEMP");

            if (userTempDir == null)
                throw new Exception("TEMP environment variable is not set");

            Name = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-") + RandomString.Next();
            FullPath = Path.Combine(userTempDir, subdirectory, Name);

            Directory.CreateDirectory(FullPath);
        }


        public void Dispose()
        {
            Directory.Delete(FullPath, true);
        }

        /// <summary>
        /// Create a file int the temporary directory. Name can use forward or backward slashes.
        /// Parent must pre-exist.
        /// </summary>
        public void CreateFile(string name) => File.Create(Path.Combine(FullPath, name.ToNativePath())).Close();

        /// <summary>
        /// Create a directory int the temporary directory. Name can use forward or backward slashes.
        /// Parent must pre-exist.
        /// </summary>
        public void CreateDir(string name) => Directory.CreateDirectory(Path.Combine(FullPath, name.ToNativePath()));
    }
}
