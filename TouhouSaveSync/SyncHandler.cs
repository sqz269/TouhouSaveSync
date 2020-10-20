﻿using System;
using System.Collections.Generic;
using System.Threading;
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
        None
    }

    public class SyncHandler
    {
        public SaveFileHandler[] SaveFiles { get; private set; }

        private readonly GoogleDriveHandler m_googleDriveHandler;

        private string m_googleDriveSaveFolder;

        /// <summary>
        /// Don't perform pull/push action if the remote/local file is only changed this many seconds apart
        /// </summary>
        private const int SyncThresholdTimeDifference = 60;

        /// <summary>
        /// A hash set to store saves that needs to by synced
        /// <br></br>
        /// Use hash set because it only allows unique items
        /// </summary>
        private readonly HashSet<SaveFileHandler> m_syncQueue = new HashSet<SaveFileHandler>();

        public SyncHandler(GoogleDriveHandler googleDriveHandler)
        {
            this.m_googleDriveHandler = googleDriveHandler;
            Console.WriteLine("Getting Drive Folder ID for folder with name: TouhouSaveSync");
            this.InitGoogleDrive();
            Console.WriteLine("Scanning for save directories");
            this.InitSaveFiles();
            Console.WriteLine("Starting Initial Sync");
            this.InitialSync();
            Console.WriteLine("Initial Sync Completed");
        }

        private void InitSaveFiles()
        {
            Console.WriteLine("Scanning for games at: {0}", ConfigManager.GetSetting("TouhouGamesDirectory"));
            Dictionary<String, String> oldGenGamesFound = FindTouhouSavePath.GetTouhouOldGenPath(ConfigManager.GetSetting("TouhouGamesDirectory"));
            // TODO: Detect and sync newer generation games that does not exist on the PC, but have save files on the drive
            Dictionary<String, String> newGenGamesFound = FindTouhouSavePath.GetTouhouNewGenPath(ConfigManager.GetSetting("TouhouGamesDirectory"));
            Console.WriteLine("Scanning completed. Found a total of {0} games", oldGenGamesFound.Count + newGenGamesFound.Count);

            Console.WriteLine("Pre-Processing Game Save Folders");
            this.SaveFiles = SaveFileHandler.ToTouhouSaveFilesHandlers(newGenGamesFound, oldGenGamesFound,
                this.m_googleDriveHandler, this.m_googleDriveSaveFolder);

            this.RegisterSaveFileChangeHandlers();
        }

        private void InitGoogleDrive()
        {
            this.m_googleDriveSaveFolder =
                this.m_googleDriveHandler.GetFolderId("TouhouSaveSync");
            Console.WriteLine("TouhouSaveSync folder ID: {0}", this.m_googleDriveSaveFolder);
        }

        private void InitialSync()
        {
            foreach (SaveFileHandler handler in this.SaveFiles)
            {
                this.ExecuteSyncAction(handler, this.DetermineSyncAction(handler));
            }
        }

        private void RegisterSaveFileChangeHandlers()
        {
            foreach (SaveFileHandler handler in this.SaveFiles)
            {
                handler.RegisterOnSaveFileChangeCallbackExternal(this.OnSaveFileChanged);
            }
        }

        private void OnSaveFileChanged(SaveFileHandler handler)
        {
            this.m_syncQueue.Add(handler);
        }

        /// <summary>
        /// Determines what to do with the local save file
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public SyncAction DetermineSyncAction(SaveFileHandler handler)
        {
            var remoteFile =
                this.m_googleDriveHandler.FindFirstFileWithName(handler.SaveFile.GetRemoteFileName(), this.m_googleDriveSaveFolder);
            return this.DetermineSyncAction(remoteFile, handler);
        }

        private SyncAction DetermineSyncAction(Google.Apis.Drive.v3.Data.File remoteFile, SaveFileHandler saveHandler)
        {
            double saveModifyTime = saveHandler.SaveFile.GetScoreDatModifyTime();

            // The remote file's Description should be set with GetScoreDatModifyTime during upload/update
            // The description will be converted to a double and seen as an Unix Time stamp and compared
            // Which will be used to determine which side is out of date
            if (remoteFile.Description == null)
                return SyncAction.Push;

            double remoteModifyTime;
            try
            {
                remoteModifyTime = Double.Parse(remoteFile.Description);
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to parse the file's description for last mod. Updating file");
                return SyncAction.Push;
            }

            double timeDifference = saveModifyTime - remoteModifyTime;

            // If the time difference with the google drive are not that different
            // and we have to account for upload time and other stuff
            // Thus, they might be the same save file
            if (Math.Abs(timeDifference) <= SyncThresholdTimeDifference)
                return SyncAction.None;

            /*if ()*/

            // if the time difference is a positive number,
            // that means the current save file is x seconds ahead of google drive save
            // which then we need to push the current save to the cloud
            if (timeDifference > 0)
                return SyncAction.Push;
            else
                return SyncAction.Pull;
        }

        private void ExecuteSyncAction(SaveFileHandler saveHandler, SyncAction action)
        {
            Console.WriteLine("Executing Sync Action: {0}. On: {1}", action, saveHandler.SaveFile.GetRemoteFileName());
            switch (action)
            {
                case SyncAction.Push:
                {
                    saveHandler.PushSaves();
                    break;
                }
                case SyncAction.Pull:
                {
                    saveHandler.PullSaves();
                    break;
                }
                case SyncAction.None:
                {
                    break;
                }
            }
        }

        private void PollQueue()
        {
            foreach (SaveFileHandler handler in this.m_syncQueue)
            {
                if (ProcessUtility.IsProcessActive(handler.ExecutableName))
                {
                    Console.WriteLine("Holding off sync for {0} because game is active", handler.SaveFile.GameTitle);
                }
                else
                {
                    this.ExecuteSyncAction(handler, this.DetermineSyncAction(handler));
                    
                    // We are modifying collection here,
                    // and iteration can't continue after that
                    // so breakout early
                    this.m_syncQueue.Remove(handler);
                    return;
                }
            }
        }

        /// <summary>
        /// Enters a sync loop
        /// </summary>
        public void SyncLoop()
        {
            while (true)
            {
                this.PollQueue();
                Thread.Sleep(5000);
            }
        }
    }
}