using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using TouhouSaveSync.GoogleDrive;

namespace TouhouSaveSync.SaveFiles
{
    public class SaveFileHandler
    {
        public readonly TouhouSaveFile SaveFile;
        public readonly string ExecutableName;

        private FileSystemWatcher m_fileWatcher;
        private readonly GoogleDriveHandler m_googleDriveHandler;
        private readonly string m_googleDriveSaveFolder;

        /// <summary>
        /// Delegate function that is called when the score.dat file is changed
        /// </summary>
        /// <param name="handler">The handler for the save file that was changed</param>
        public delegate void OnSaveFileChange(SaveFileHandler handler);

        public OnSaveFileChange OnSaveFileChangeCallback;

        public SaveFileHandler(TouhouSaveFile saveFile, GoogleDriveHandler driveHandler, string parentFolder)
        {
            this.SaveFile = saveFile;
            this.m_googleDriveHandler = driveHandler;
            this.m_googleDriveSaveFolder = parentFolder;
            this.ExecutableName = (this.SaveFile.Generation == TouhouGameGeneration.New
                ? FindTouhouSavePath.TouhouToExeNameNewGen[this.SaveFile.GameTitle]
                : FindTouhouSavePath.TouhouToExeNameOldGen[this.SaveFile.GameTitle]).Split(".")[0];
            this.RegisterFileSystemWatcher();
        }

        /// <summary>
        /// Watch the SaveFile.GameSavePath directory for any changes in .dat file
        /// and set the call back for changes to OnSaveFileChangeFromWatch
        /// </summary>
        private void RegisterFileSystemWatcher()
        {
            this.m_fileWatcher = new FileSystemWatcher(this.SaveFile.GameSavePath)
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
            Console.WriteLine($"File: {e.FullPath} changed. Type: {e.ChangeType}");
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

        // TODO: move all the SyncAction method into saveHandler?
        /// <summary>
        /// Upload this PC's save to the google drive.
        /// <br></br>
        /// Should Only be used if there is no previous saves
        /// </summary>
        /// <param name="saveHandler"></param>
        public void CreateSaves()
        {
            this.SaveFile.ZipSaveFile();
            // We set the description of the file to the ScoreDatModifyTime to later use it as a metric
            // for determining which side is outdated. See DetermineSyncAction for how is the description used
            string id = this.m_googleDriveHandler.Upload(this.SaveFile.GetRemoteFileName(),
                this.SaveFile.ZipSaveStoragePath, "application/zip", this.m_googleDriveSaveFolder,
                this.SaveFile.GetScoreDatModifyTime().ToString());
            this.SaveFile.GoogleDriveFileId = id;
        }

        /// <summary>
        /// Sync this PC's save with the cloud save,
        /// will overwrite local save files
        /// </summary>
        public void PullSaves()
        {
            this.m_googleDriveHandler.Download(this.SaveFile.GoogleDriveFileId, this.SaveFile.ZipSaveStoragePath);
            this.SaveFile.LoadZippedSaveFile();
        }

        /// <summary>
        /// Syncs the cloud save with this PC's save
        /// Overwrite cloud files
        /// </summary>
        public void PushSaves()
        {
            this.SaveFile.ZipSaveFile();
            this.m_googleDriveHandler.Update(this.SaveFile.GetRemoteFileName(), this.SaveFile.ZipSaveStoragePath,
                this.SaveFile.GoogleDriveFileId, "application/zip", this.SaveFile.GetScoreDatModifyTime().ToString());
        }


        public static SaveFileHandler[] ToTouhouSaveFilesHandlers(Dictionary<string, string> newGen,
            Dictionary<string, string> oldGen, GoogleDriveHandler driveHandler, string parentFolder)
        {
            SaveFileHandler[] handlers = new SaveFileHandler[newGen.Count + oldGen.Count];
            int i = 0;
            foreach (TouhouNewGenSaveFile newGenSaveFile in TouhouNewGenSaveFile.ToTouhouSaveFiles(newGen))
            {
                Console.WriteLine("Instantiating TouhouSaveFileHandler for: {0}", newGenSaveFile.GameTitle);
                handlers[i] = new SaveFileHandler(newGenSaveFile, driveHandler, parentFolder);
                i++;
            }

            foreach (TouhouOldGenSaveFile oldGenSaveFile in TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGen))
            {
                Console.WriteLine("Instantiating TouhouSaveFileHandler for: {0}", oldGenSaveFile.GameTitle);
                handlers[i] = new SaveFileHandler(oldGenSaveFile, driveHandler, parentFolder);
                i++;
            }

            return handlers;
        }
    }
}