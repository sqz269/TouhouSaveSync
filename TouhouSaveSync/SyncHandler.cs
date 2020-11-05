using System;
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

            TouhouLocalSaveFile[] newGenSaveFiles = TouhouLocalNewGenSaveFile.ToTouhouSaveFiles(newGenGamesFound);
            TouhouLocalSaveFile[] oldGenSaveFiles = TouhouLocalOldGenSaveFile.ToTouhouSaveFiles(oldGenGamesFound);

            TouhouRemoteSaveFile[] remoteSaveFiles = new TouhouRemoteSaveFile[newGenSaveFiles.Length + oldGenSaveFiles.Length];
            int i = 0;
            foreach (TouhouRemoteSaveFile remoteSaveFile in TouhouRemoteSaveFile.ToTouhouRemoteSaveFiles(newGenSaveFiles, m_googleDriveHandler, m_googleDriveSaveFolder))
            {
                remoteSaveFiles[i] = remoteSaveFile;
                i++;
            }

            foreach (TouhouRemoteSaveFile remoteSaveFile in TouhouRemoteSaveFile.ToTouhouRemoteSaveFiles(oldGenSaveFiles, m_googleDriveHandler, m_googleDriveSaveFolder))
            {
                remoteSaveFiles[i] = remoteSaveFile;
                i++;
            }

            Console.WriteLine("Pre-Processing Game Save Folders");
            this.SaveFiles = SaveFileHandler.ToTouhouSaveFilesHandlers(remoteSaveFiles);

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

        private SyncAction HandleConflict(bool localSaveBigger, bool localSaveRecent)
        {
            if (localSaveBigger && !localSaveRecent)
            {
                Console.WriteLine("CONFLICT: The LOCAL's score.dat is bigger, but the REMOTE score.dat is more recently updated.");
            }
            else if (!localSaveBigger && localSaveRecent)
            {
                Console.WriteLine("CONFLICT: The REMOTE's score.dat is bigger, but the LOCAL score.dat is more recently updated.");
            }
            while (true)
            {
                string keepTarget = InputManager.GetStringInput("Keep Local or Keep Remote? (1: Local, 0: Remote)");
                if (keepTarget == "1")
                    return SyncAction.Push;
                else if (keepTarget == "2")
                    return SyncAction.Pull;
            }
        }

        /// <summary>
        /// Determines what to do with the local save file
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        private SyncAction DetermineSyncAction(SaveFileHandler handler)
        {
            SaveFileMetadata metadata = handler.RemoteSaveFile.GetRemoteSaveFileMetaData();
            double localModifyTime = handler.LocalSaveFile.GetScoreDatModifyTime();
            // The remote file's Description should be set with GetScoreDatModifyTime during upload/update
            // The description will be converted to a double and seen as an Unix Time stamp and compared
            // Which will be used to determine which side is out of date
            double remoteModifyTime;
            try
            {
                remoteModifyTime = metadata.ZipLastMod;
            }
            catch
            {
                Console.WriteLine("Unable to parse the file's description for last mod. Updating file");
                return SyncAction.Push;
            }

            double timeDifference = localModifyTime - remoteModifyTime;

            double localDatSize = handler.LocalSaveFile.GetScoreDatSize();
            bool isSaveSizeSame = Math.Abs(localDatSize - metadata.DatSize) < 0.001;
            bool localSaveBigger = localDatSize > metadata.DatSize;

            // If the time difference with the google drive are not that different
            // and we have to account for upload time and other stuff
            // Thus, they might be the same save file
            if (Math.Abs(timeDifference) <= SyncThresholdTimeDifference)
                return SyncAction.None;

            // if the time difference is a positive number,
            // that means the current save file is x seconds ahead of google drive save
            // which then we need to push the current save to the cloud
            if (timeDifference > 0)
            {
                // The remote save is bigger but the local dat is more recently updated
                if (!isSaveSizeSame && !localSaveBigger)
                {
                    HandleConflict(false, true);
                }
                return SyncAction.Push;
            }
            else
            {
                if (!isSaveSizeSame && localSaveBigger)
                {
                    HandleConflict(true, false);
                }
                return SyncAction.Pull;
            }
        }

        private void ExecuteSyncAction(SaveFileHandler saveHandler, SyncAction action)
        {
            Console.WriteLine("Executing Sync Action: {0}. On: {1}", action, saveHandler.RemoteSaveFile.RemoteFileId);
            switch (action)
            {
                case SyncAction.Push:
                {
                    saveHandler.RemoteSaveFile.PushSaves();
                    break;
                }
                case SyncAction.Pull:
                {
                    saveHandler.RemoteSaveFile.PullSaves();
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
                    Console.WriteLine("Holding off sync for {0} because game is active", handler.LocalSaveFile.GameTitle);
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