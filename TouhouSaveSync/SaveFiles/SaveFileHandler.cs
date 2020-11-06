using System;
using System.IO;

namespace TouhouSaveSync.SaveFiles
{
    public class SaveFileHandler
    {
        public readonly TouhouLocalSaveFile LocalSaveFile;
        public readonly TouhouRemoteSaveFile RemoteSaveFile;
        public readonly string ExecutableName;

        #region SaveSizeRegion

        /// <summary>
        /// Indicates if a save file is changed since last access of
        /// SaveSize property
        /// </summary>
        private bool m_saveZipChangedSinceLastAccess;

        // ReSharper disable once InconsistentNaming
        // Backing field of SaveSize
        private long _saveSize;
        public long SaveSize
        {
            get
            {
                if (!this.m_saveZipChangedSinceLastAccess)
                    return this._saveSize;
                this._saveSize = this.GetSaveZipFileSize();
                this.m_saveZipChangedSinceLastAccess = false;
                return this._saveSize;
            }
            private set => this._saveSize = value;
        }
        #endregion

        private FileSystemWatcher m_fileWatcher;

        /// <summary>
        /// Delegate function that is called when the score.dat file is changed
        /// </summary>
        /// <param name="handler">The handler for the save file that was changed</param>
        public delegate void OnSaveFileChange(SaveFileHandler handler);

        public OnSaveFileChange OnSaveFileChangeCallback;

        public SaveFileHandler(TouhouRemoteSaveFile remoteSaveFile)
        {
            this.LocalSaveFile = remoteSaveFile.LocalSaveFile;
            this.RemoteSaveFile = remoteSaveFile;
            this.m_saveZipChangedSinceLastAccess = true;
            this.ExecutableName = (this.LocalSaveFile.Generation == TouhouGameGeneration.New
                ? FindTouhouSavePath.TouhouToExeNameNewGen[this.LocalSaveFile.GameTitle]
                : FindTouhouSavePath.TouhouToExeNameOldGen[this.LocalSaveFile.GameTitle]).Split(".")[0];
            this.RegisterFileSystemWatcher();
        }

        /// <summary>
        /// Watch the LocalSaveFile.GameSavePath directory for any changes in .dat file
        /// and set the call back for changes to OnSaveFileChangeFromWatch
        /// </summary>
        private void RegisterFileSystemWatcher()
        {
            this.m_fileWatcher = new FileSystemWatcher(this.LocalSaveFile.GameSavePath)
            {
                NotifyFilter = NotifyFilters.LastWrite, 
                Filter = "*.dat" // Too lazy to actually find the score.dat file and watch, so we're just watch the whole dir
            };
            this.m_fileWatcher.Changed += this.OnSaveFileChangeFromWatch;
            this.m_fileWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// A callback used by RegisterFileSystemWatcher,
        /// invokes OnSaveFileChangeCallback on call
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSaveFileChangeFromWatch(object sender, FileSystemEventArgs e)
        {
            this.m_saveZipChangedSinceLastAccess = true;
            this.OnSaveFileChangeCallback?.Invoke(this);
        }

        /// <summary>
        /// Register a function callback when the score file is changed
        /// </summary>
        /// <param name="callback">The function to be registered as a callback</param>
        public void RegisterOnSaveFileChangeCallbackExternal(OnSaveFileChange callback)
        {
            this.OnSaveFileChangeCallback += callback;
        }

        /// <summary>
        /// Get the size of the zipped save files
        /// <br></br>
        /// Note: This function call might be expensive as it invokes LocalSaveFile.ZipSaveFile,
        /// If you want to regularly access the Zipped save size, use this.SaveSize instead
        /// </summary>
        /// <returns></returns>
        public long GetSaveZipFileSize()
        {
            this.LocalSaveFile.ZipSaveFile();
            return new FileInfo(this.LocalSaveFile.ZipSaveStoragePath).Length;
        }

        public static SaveFileHandler[] ToTouhouSaveFilesHandlers(TouhouRemoteSaveFile[] remoteSaveFiles)
        {
            SaveFileHandler[] handlers = new SaveFileHandler[remoteSaveFiles.Length];
            int i = 0;
            foreach (TouhouRemoteSaveFile remoteSaveFile in remoteSaveFiles)
            {
                handlers[i] = new SaveFileHandler(remoteSaveFile);
                i++;
            }

            return handlers;
        }
    }
}