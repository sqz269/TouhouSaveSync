using System;
using System.Collections.Generic;
using TouhouSaveSync.Config;
using TouhouSaveSync.GoogleDrive;
using TouhouSaveSync.SaveFiles;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync
{
    public class SyncHandler
    {
        public TouhouNewGenSaveFile[] NewGenSaveFiles;
        public TouhouOldGenSaveFile[] OldGenSaveFiles;

        private readonly GoogleDriveHandler m_googleDriveHandler;

        private string m_googleDriveSaveFolder;

        public SyncHandler(GoogleDriveHandler googleDriveHandler)
        {
            this.m_googleDriveHandler = googleDriveHandler;
            this.InitSaveFiles();
            this.InitGoogleDrive();
        }

        private void InitSaveFiles()
        {
            Dictionary<String, String> oldGenGamesFound = FindTouhouSavePath.GetTouhouOldGenPath(ConfigManager.GetSetting("EarlyTouhouGamesDirectory"));
            Dictionary<String, String> newGenSavesFound = FindTouhouSavePath.GetTouhouNewGenPath();

            TouhouNewGenSaveFile[] newGenSaveFiles = TouhouNewGenSaveFile.ToTouhouSaveFiles(newGenSavesFound);
            TouhouOldGenSaveFile[] oldGenSaveFiles = TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGenGamesFound);
        }

        private void InitGoogleDrive()
        {
            this.m_googleDriveSaveFolder =
                this.m_googleDriveHandler.GetFolderId("TouhouSaveSync");
        }

        /// <summary>
        /// Sync this PC's save with the cloud save,
        /// will overwrite local save files
        /// </summary>
        public void PullSaves()
        {

        }

        /// <summary>
        /// Syncs the cloud save with this PC's save
        /// Overwrite cloud files
        /// </summary>
        public void PushSaves()
        {

        }

        /// <summary>
        /// Enters a sync loop
        /// </summary>
        public void SyncLoop()
        {

        }
    }
}