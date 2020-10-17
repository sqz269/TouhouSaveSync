using System;
using System.Collections.Generic;
using System.Resources;
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
        Create,
        None
    }

    public class SyncHandler
    {
        public TouhouNewGenSaveFile[] NewGenSaveFiles { get; private set; }
        public TouhouOldGenSaveFile[] OldGenSaveFiles { get; private set; }

        private readonly GoogleDriveHandler m_googleDriveHandler;

        private string m_googleDriveSaveFolder;

        /// <summary>
        /// Don't perform pull/push action if the remote/local file is only changed this many seconds apart
        /// </summary>
        private const int SyncThresholdTimeDifference = 60;

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

            double timeDifference = saveModifyTime - remoteModifyTime;

            // If the time difference with the google drive are not that different
            // and we have to account for upload time and other stuff
            // Thus, they might be the same save file
            if (Math.Abs(timeDifference) <= SyncThresholdTimeDifference)
                return SyncAction.None;

            // if the time difference is a positive number,
            // that means the current save file is x seconds ahead of google drive save
            // which then we need to push the current save to the cloud
            return timeDifference > 0 ? SyncAction.Push : SyncAction.Pull;
        }

        private void ExecuteSyncAction(TouhouSaveFile saveFile, SyncAction action)
        {
            Console.WriteLine("Executing Sync Action: {0}. On: {1}", action, saveFile.GetRemoteFileName());
            switch (action)
            {
                case SyncAction.Create:
                {
                    saveFile.ZipSaveFile();
                    this.m_googleDriveHandler.Upload(saveFile.GetRemoteFileName(), saveFile.ZipSaveStoragePath,
                        "application/zip", this.m_googleDriveSaveFolder);
                    break;
                }
                case SyncAction.Push:
                {
                    break;
                }
                case SyncAction.Pull:
                {
                    break;
                }
                case SyncAction.None:
                {
                    break;
                }
            }
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