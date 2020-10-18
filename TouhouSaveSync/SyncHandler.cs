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
        Create,
        None
    }

    public class SyncHandler
    {
        public TouhouSaveFilesHandler[] SaveFiles { get; private set; }

        private readonly GoogleDriveHandler m_googleDriveHandler;

        private string m_googleDriveSaveFolder;

        /// <summary>
        /// Don't perform pull/push action if the remote/local file is only changed this many seconds apart
        /// </summary>
        private const int SyncThresholdTimeDifference = 60;

        private readonly HashSet<TouhouSaveFilesHandler> m_syncQueue = new HashSet<TouhouSaveFilesHandler>();

        public SyncHandler(GoogleDriveHandler googleDriveHandler)
        {
            this.m_googleDriveHandler = googleDriveHandler;
            Console.WriteLine("Scanning for save directories");
            this.InitSaveFiles();
            Console.WriteLine("Getting Drive Folder ID for folder with name: TouhouSaveSync");
            this.InitGoogleDrive();
            Console.WriteLine("Starting Initial Sync");
            this.InitialSync();
        }

        private void InitSaveFiles()
        {
            Console.WriteLine("Scanning for games at: {0}", ConfigManager.GetSetting("TouhouGamesDirectory"));
            Dictionary<String, String> oldGenGamesFound = FindTouhouSavePath.GetTouhouOldGenPath(ConfigManager.GetSetting("TouhouGamesDirectory"));
            // TODO: Detect and sync newer generation games that does not exist on the PC, but have save files on the drive
            Dictionary<String, String> newGenGamesFound = FindTouhouSavePath.GetTouhouNewGenPath(ConfigManager.GetSetting("TouhouGamesDirectory"));
            Console.WriteLine("Scanning completed. Found a total of {0} games", oldGenGamesFound.Count + newGenGamesFound.Count);

            Console.WriteLine("Pre-Processing Game Save Folders");
            this.SaveFiles = TouhouSaveFilesHandler.ToTouhouSaveFilesHandlers(newGenGamesFound, oldGenGamesFound);

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
            foreach (TouhouSaveFilesHandler handler in this.SaveFiles)
            {
                var remoteFile =
                    this.m_googleDriveHandler.FindFirstFileWithName(handler.SaveFile.GetRemoteFileName(), this.m_googleDriveSaveFolder);

                if (remoteFile != null)
                {
                    handler.SaveFile.GoogleDriveFileId = remoteFile.Id;
                    Console.WriteLine("Found remote save file with id: {0}", remoteFile.Id);
                }
                else
                {
                    Console.WriteLine("Did not find a remote file under folder: {0} with name: {1}",
                        this.m_googleDriveSaveFolder, handler.SaveFile.GetRemoteFileName());
                }


                this.ExecuteSyncAction(handler, this.DetermineSyncAction(remoteFile, handler));
            }
        }

        private void RegisterSaveFileChangeHandlers()
        {
            foreach (TouhouSaveFilesHandler handler in this.SaveFiles)
            {
                handler.RegisterOnSaveFileChangeCallbackExternal(this.OnSaveFileChanged);
            }
        }

        private void OnSaveFileChanged(TouhouSaveFilesHandler handler)
        {
            this.m_syncQueue.Add(handler);
        }

        private SyncAction DetermineSyncAction(Google.Apis.Drive.v3.Data.File remoteFile, TouhouSaveFilesHandler saveHandler)
        {
            if (remoteFile == null)
                return SyncAction.Create;

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

            // if the time difference is a positive number,
            // that means the current save file is x seconds ahead of google drive save
            // which then we need to push the current save to the cloud
            return timeDifference > 0 ? SyncAction.Push : SyncAction.Pull;
        }

        private void ExecuteSyncAction(TouhouSaveFilesHandler saveHandler, SyncAction action)
        {
            Console.WriteLine("Executing Sync Action: {0}. On: {1}", action, saveHandler.SaveFile.GetRemoteFileName());
            switch (action)
            {
                case SyncAction.Create:
                {
                    this.CreateSaves(saveHandler);
                    break;
                }
                case SyncAction.Push:
                {
                    this.PushSaves(saveHandler);
                    break;
                }
                case SyncAction.Pull:
                {
                    this.PullSaves(saveHandler);
                    break;
                }
                case SyncAction.None:
                {
                    break;
                }
            }
        }

        // TODO: move all the SyncAction method into saveHandler?
        /// <summary>
        /// Upload this PC's save to the google drive.
        /// <br></br>
        /// Should Only be used if there is no previous saves
        /// </summary>
        /// <param name="saveHandler"></param>
        public void CreateSaves(TouhouSaveFilesHandler saveHandler)
        {
            saveHandler.SaveFile.ZipSaveFile();
            // We set the description of the file to the ScoreDatModifyTime to later use it as a metric
            // for determining which side is outdated. See DetermineSyncAction for how is the description used
            string id = this.m_googleDriveHandler.Upload(saveHandler.SaveFile.GetRemoteFileName(),
                saveHandler.SaveFile.ZipSaveStoragePath, "application/zip", this.m_googleDriveSaveFolder,
                saveHandler.SaveFile.GetScoreDatModifyTime().ToString());
            saveHandler.SaveFile.GoogleDriveFileId = id;
        }

        /// <summary>
        /// Sync this PC's save with the cloud save,
        /// will overwrite local save files
        /// </summary>
        public void PullSaves(TouhouSaveFilesHandler saveHandler)
        {
            this.m_googleDriveHandler.Download(saveHandler.SaveFile.GoogleDriveFileId,
                saveHandler.SaveFile.ZipSaveStoragePath);
            saveHandler.SaveFile.LoadZippedSaveFile();
        }

        /// <summary>
        /// Syncs the cloud save with this PC's save
        /// Overwrite cloud files
        /// </summary>
        public void PushSaves(TouhouSaveFilesHandler saveHandler)
        {
            saveHandler.SaveFile.ZipSaveFile();
            this.m_googleDriveHandler.Update(saveHandler.SaveFile.GetRemoteFileName(),
                saveHandler.SaveFile.ZipSaveStoragePath,
                saveHandler.SaveFile.GoogleDriveFileId, "application/zip",
                saveHandler.SaveFile.GetScoreDatModifyTime().ToString());
        }

        /// <summary>
        /// Enters a sync loop
        /// </summary>
        public void SyncLoop()
        {
            while (true)
            {
                Thread.Sleep(2);
            }
        }
    }
}