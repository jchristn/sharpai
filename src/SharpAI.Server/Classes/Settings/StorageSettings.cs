namespace SharpAI.Server.Classes.Settings
{
    using System;

    /// <summary>
    /// Storage settings.
    /// </summary>
    public class StorageSettings
    {
        /// <summary>
        /// Temporary storage directory.
        /// </summary>
        public string TempDirectory
        {
            get
            {
                return _TempDirectory;
            }
            set
            {
                if (!String.IsNullOrEmpty(value)) value = NormalizeDirectory(value);
                _TempDirectory = value;
            }
        }

        /// <summary>
        /// Models directory.
        /// </summary>
        public string ModelsDirectory
        {
            get
            {
                return _ModelsDirectory;
            }
            set
            {
                if (!String.IsNullOrEmpty(value)) value = NormalizeDirectory(value);
                _ModelsDirectory = value;
            }
        }

        private string _TempDirectory = "./temp/";
        private string _ModelsDirectory = "./models/";

        /// <summary>
        /// Instantiate.
        /// </summary>
        public StorageSettings()
        {

        }

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
    }
}