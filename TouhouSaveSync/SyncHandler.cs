using System;
using System.Collections.Generic;
using System.Threading;
using Google.Apis.Download;
using Google.Apis.Upload;
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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


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
            this.InitGoogleDrive();

            this.InitSaveFiles();

            this.InitialSync();

            this.RegisterSaveFileChangeHandlers();

            this.RegisterGoogleDriveProgressChangeHandlers();
        }

        private void InitSaveFiles()
        {
            Logger.Info($"Scanning for Touhou Games at: {ConfigManager.GetSetting("TouhouGamesDirectory")}");
            Dictionary<string, string> oldGenGamesFound = FindTouhouSavePath.GetTouhouOldGenPath(ConfigManager.GetSetting("TouhouGamesDirectory"));
            // TODO: Detect and sync newer generation games that does not exist on the PC, but have save files on the drive
            Dictionary<string, string> newGenGamesFound = FindTouhouSavePath.GetTouhouNewGenPath(ConfigManager.GetSetting("TouhouGamesDirectory"));
            Logger.Info($"Scanning completed. Found a total of {oldGenGamesFound.Count + newGenGamesFound.Count} games");

            Logger.Info("Processing Found Games");

            Logger.Trace("Instantiating Object Representation for Local Touhou Save files");
            TouhouLocalSaveFile[] newGenSaveFiles = TouhouLocalNewGenSaveFile.ToTouhouSaveFiles(newGenGamesFound);
            TouhouLocalSaveFile[] oldGenSaveFiles = TouhouLocalOldGenSaveFile.ToTouhouSaveFiles(oldGenGamesFound);
            Logger.Trace("Instantiation Completed for Local Touhou Save Object Representation");

            Logger.Trace("Instantiating Object Representation for Remote Touhou Save files");
            TouhouRemoteSaveFile[] remoteSaveFiles = TouhouRemoteSaveFile.ToTouhouRemoteSaveFiles(newGenSaveFiles,
                oldGenSaveFiles, m_googleDriveHandler, m_googleDriveSaveFolder);
            Logger.Trace("Instantiation Completed for Remote Touhou Save Object Representation");

            Logger.Trace("Instantiating Touhou Save File Wrapper");
            this.SaveFiles = SaveFileHandler.ToTouhouSaveFilesHandlers(remoteSaveFiles);
            Logger.Trace("Instantiation Completed for Touhou Save File Wrapper");
        }

        private void InitGoogleDrive()
        {
            Logger.Debug("Getting Drive Folder ID for folder with name: TouhouSaveSync");
            this.m_googleDriveSaveFolder =
                this.m_googleDriveHandler.GetFolderId("TouhouSaveSync");
            Logger.Debug($"Found Folder \"TouhouSaveSync\" with id: {this.m_googleDriveSaveFolder}");

        }

        private void InitialSync()
        {
            Logger.Info("Starting Initial Sync");
            foreach (SaveFileHandler handler in this.SaveFiles)
            {
                this.ExecuteSyncAction(handler, this.DetermineSyncAction(handler));
            }
            Logger.Info("Initial Sync Completed");
        }

        private void RegisterSaveFileChangeHandlers()
        {
            Logger.Debug("Registering On Change callback for save files");
            foreach (SaveFileHandler handler in this.SaveFiles)
            {
                handler.RegisterOnSaveFileChangeCallbackExternal(this.OnSaveFileChanged);
            }
        }

        private void OnSaveFileChanged(SaveFileHandler handler)
        {
            this.m_syncQueue.Add(handler);
        }

        private void RegisterGoogleDriveProgressChangeHandlers()
        {
            this.m_googleDriveHandler.RegisterDownloadProgressCallback(OnDownloadProgressChanged);
            this.m_googleDriveHandler.RegisterUploadProgressCallback(OnUploadProgressChanged);
        }

        private void OnDownloadProgressChanged(IDownloadProgress progress)
        {
            switch (progress.Status)
            {
                case DownloadStatus.Downloading:
                    Logger.Debug($"Downloading ... {progress.BytesDownloaded} bytes downloaded");
                    break;
                case DownloadStatus.Completed:
                    Logger.Debug($"Download completed. Downloaded ad total of {progress.BytesDownloaded} bytes");
                    break;
            }
        }

        private void OnUploadProgressChanged(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Starting:
                    Logger.Debug("Starting Upload");
                    break;
                case UploadStatus.Uploading:
                    Logger.Debug($"Uploading ... {progress.BytesSent} bytes uploaded");
                    break;
                case UploadStatus.Completed:
                    Logger.Debug($"Upload Completed. Uploaded a total of {progress.BytesSent}");
                    break;
            }
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
            SaveFileMetadata metadata;
            double localModifyTime = handler.LocalSaveFile.GetScoreDatModifyTime();

            try
            {
                metadata = handler.RemoteSaveFile.GetRemoteSaveFileMetaData();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to retrieve/load Metadata for file. Overwriting Remote File");
                return SyncAction.Push;
            }
            double remoteModifyTime = metadata.DatLastMod;

            double timeDifference = localModifyTime - remoteModifyTime;

            double localDatSize = handler.LocalSaveFile.GetScoreDatSize();
            bool isSaveSizeSame = Math.Abs(localDatSize - metadata.DatSize) < 0.001;
            bool localSaveBigger = localDatSize > metadata.DatSize;

            // If the time difference with the google drive are not that different
            // and we have to account for upload time and other stuff
            // Thus, they might be the same save file
            if (Math.Abs(timeDifference) <= SyncThresholdTimeDifference)
            {
                Logger.Debug($"Not performing any Sync action for: {handler.LocalSaveFile.GameTitle}. Same modified time or modified time difference in acceptable range ({SyncThresholdTimeDifference} > {timeDifference})");
                return SyncAction.None;
            }

            // if the time difference is a positive number,
            // that means the current save file is x seconds ahead of google drive save
            // which then we need to push the current save to the cloud
            if (timeDifference > 0)
            {
                Logger.Trace($"Potential Sync action: Push. Remote Save is {timeDifference} seconds behind local save");
                // The remote save is bigger but the local dat is more recently updated
                if (!isSaveSizeSame && !localSaveBigger)
                {
                    Logger.Trace($"Potential Sync Action Discarded. The Remote File is {timeDifference} seconds behind, but save size is {metadata.DatSize - localDatSize} bytes larger. Requesting User Action");
                    return HandleConflict(false, true);
                }
                Logger.Debug($"Performing Sync Action: Push, on {handler.LocalSaveFile.GameTitle}");
                return SyncAction.Push;
            }
            else
            {
                Logger.Trace($"Potential Sync action: Push. Local Save is {Math.Abs(timeDifference)} seconds behind remote save");
                if (!isSaveSizeSame && localSaveBigger)
                {
                    Logger.Trace($"Potential Sync Action Discarded. The Local File is {Math.Abs(timeDifference)} seconds behind, but save size is {localDatSize - metadata.DatSize} bytes larger. Requesting User Action");
                    return HandleConflict(true, false);
                }
                Logger.Debug($"Performing Sync Action: Pull, on {handler.LocalSaveFile.GameTitle}");
                return SyncAction.Pull;
            }
        }

        private void ExecuteSyncAction(SaveFileHandler saveHandler, SyncAction action)
        {
            Logger.Info($"Performing Sync Action: {action}, on {saveHandler.LocalSaveFile.GameTitle}");
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
            Logger.Debug("Polling Queue");
            foreach (SaveFileHandler handler in this.m_syncQueue)
            {
                Logger.Trace($"Processing: {handler.LocalSaveFile.GameTitle}");
                if (ProcessUtility.IsProcessActive(handler.ExecutableName))
                {
                    Logger.Info($"Holding off sync for {handler.LocalSaveFile.GameTitle} because game is active");
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
            Logger.Info("Entered Sync Loop");
            while (true)
            {
                this.PollQueue();
                Thread.Sleep(5000);
            }
        }
    }
}