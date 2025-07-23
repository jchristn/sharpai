namespace SharpAI.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Directory helper.
    /// </summary>
    public static class DirectoryHelper
    {
        /// <summary>
        /// Normalize directory path.
        /// </summary>
        /// <param name="directory">Directory.</param>
        /// <returns>Normalized directory.</returns>
        public static string NormalizeDirectory(string directory)
        {
            directory = directory.Replace("\\", "/");
            if (!directory.EndsWith("/")) directory += "/";
            return directory;
        }

        /// <summary>
        /// Recursively delete a directory.
        /// </summary>
        /// <param name="baseDir">Base directory.</param>
        /// <param name="deleteRootDirectory">True to delete the root directory.</param>
        public static void RecursiveDelete(string baseDir, bool deleteRootDirectory)
        {
            if (String.IsNullOrEmpty(baseDir)) throw new ArgumentNullException(nameof(baseDir));
            RecursiveDelete(new DirectoryInfo(baseDir), deleteRootDirectory);
        }

        /// <summary>
        /// Recursively delete a directory.
        /// </summary>
        /// <param name="baseDir">Base directory.</param>
        /// <param name="deleteRootDirectory">True to delete the root directory.</param>
        public static void RecursiveDelete(DirectoryInfo baseDir, bool deleteRootDirectory)
        {
            if (!baseDir.Exists) return;

            foreach (DirectoryInfo dir in baseDir.EnumerateDirectories()) RecursiveDelete(dir, false);

            foreach (FileInfo file in baseDir.GetFiles())
            {
                file.IsReadOnly = false;
                file.Delete();
            }

            if (!deleteRootDirectory)
            {
                baseDir.Delete();
            }
        }
    }
}
