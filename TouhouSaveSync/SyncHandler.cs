using System;
using System.Collections.Generic;
using TouhouSaveSync.Config;
using TouhouSaveSync.GoogleDrive;
using TouhouSaveSync.SaveFiles;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync
{
    public enum SyncAction
    {
        Pull,
        Push,
        Create
    }

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
            this.InitialSync();
        }

        private void InitSaveFiles()
        {
            Dictionary<String, String> oldGenGamesFound = FindTouhouSavePath.GetTouhouOldGenPath(ConfigManager.GetSetting("EarlyTouhouGamesDirectory"));
            Dictionary<String, String> newGenSavesFound = FindTouhouSavePath.GetTouhouNewGenPath();

            this.NewGenSaveFiles = TouhouNewGenSaveFile.ToTouhouSaveFiles(newGenSavesFound);
            this.OldGenSaveFiles = TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGenGamesFound);
        }

        private void InitGoogleDrive()
        {
            this.m_googleDriveSaveFolder =
                this.m_googleDriveHandler.GetFolderId("TouhouSaveSync");
        }

        private void InitialSync()
        {
            foreach (TouhouNewGenSaveFile newGenSaveFile in this.NewGenSaveFiles)
            {
                var remoteFile = 
                    this.m_googleDriveHandler.FindFirstFileWithName(newGenSaveFile.GetRemoteFileName(), this.m_googleDriveSaveFolder);
                this.ExecuteSyncAction(newGenSaveFile, this.DetermineSyncAction(remoteFile, newGenSaveFile));
            }

            foreach (TouhouOldGenSaveFile oldGenSaveFile in this.OldGenSaveFiles)
            {
                var remoteFile =
                    this.m_googleDriveHandler.FindFirstFileWithName(oldGenSaveFile.GetRemoteFileName(), this.m_googleDriveSaveFolder);
                this.ExecuteSyncAction(oldGenSaveFile, this.DetermineSyncAction(remoteFile, oldGenSaveFile));
            }
        }

        private SyncAction DetermineSyncAction(Google.Apis.Drive.v3.Data.File remoteFile, TouhouSaveFile saveFile)
        {
            if (remoteFile == null)
                return SyncAction.Create;

            double saveModifyTime = saveFile.GetScoreDatModifyTime();
            double remoteModifyTime = remoteFile.ModifiedTime.Value.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds;
            if (saveModifyTime > remoteModifyTime)
                return SyncAction.Push;

            return SyncAction.Pull;
        }

        private void ExecuteSyncAction(TouhouSaveFile saveFile, SyncAction action)
        {
            /*switch (action)
            {
                case 
            }*/
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