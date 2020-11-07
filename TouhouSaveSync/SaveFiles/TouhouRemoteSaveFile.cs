using Google.Apis.Drive.v3.Data;
using Newtonsoft.Json;
using TouhouSaveSync.GoogleDrive;

namespace TouhouSaveSync.SaveFiles
{
    public class TouhouRemoteSaveFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly GoogleDriveHandler m_googleDriveHandler;
        private readonly string m_remoteSaveFolder;
        public readonly TouhouLocalSaveFile LocalSaveFile;

        private string _remoteFileId;
        public string RemoteFileId
        {
            get
            {
                if (_remoteFileId == null)
                {
                    Logger.Debug($"No Cached Remote ID Stored for: {LocalSaveFile.GameTitle}. Caching new ID");
                    RemoteFileId = this.GetRemoteFileId();
                    return _remoteFileId;
                }

                return _remoteFileId;
            }
            private set => _remoteFileId = value;
        }

        public string RemoteFileName { get; private set; }

        public TouhouRemoteSaveFile(TouhouLocalSaveFile localSaveFile, GoogleDriveHandler googleDriveHandler, string remoteSaveFolder)
        {
            this.RemoteFileName = localSaveFile.GameTitle;
            this.m_googleDriveHandler = googleDriveHandler;
            this.LocalSaveFile = localSaveFile;
            this.m_remoteSaveFolder = remoteSaveFolder;
        }

        private string GetRemoteFileId()
        {
            var file = m_googleDriveHandler.FindFirstFileWithName(this.RemoteFileName, this.m_remoteSaveFolder);
            if (file == null)
            {
                Logger.Debug($"Did not find remote file with name: {RemoteFileName}. Uploading a new file");
                return this.CreateSaves();
            }
            else
            {
                Logger.Debug($"Found remote file with name: {RemoteFileName}. Id: {file.Id}");
                return file.Id;
            }
        }

        public SaveFileMetadata GetRemoteSaveFileMetaData()
        {
            Logger.Debug($"Retriving File Metadata (From Description) for Id: {RemoteFileId}");
            File remoteFile = m_googleDriveHandler.GetFile(this.RemoteFileId);
            string description = remoteFile.Description;
            SaveFileMetadata metadata = JsonConvert.DeserializeObject<SaveFileMetadata>(description);
            return metadata;
        }

        /// <summary>
        /// Upload this PC's save to the google drive.
        /// <br></br>
        /// Should Only be used if there is no previous saves
        /// </summary>
        public string CreateSaves()
        {
            Logger.Debug("Uploading a new save to remote");
            SaveFileMetadata metadata = this.LocalSaveFile.ZipSaveFile();
            // We set the description of the file to the ScoreDatModifyTime to later use it as a metric
            // for determining which side is outdated. See DetermineSyncAction for how is the description used
            string id = this.m_googleDriveHandler.Upload(this.RemoteFileName,
                this.LocalSaveFile.ZipSaveStoragePath, "application/zip", this.m_remoteSaveFolder,
                JsonConvert.SerializeObject(metadata));
            Logger.Debug($"Upload Complete. Uploaded File Id: {id}");
            return id;
        }

        /// <summary>
        /// Sync this PC's save with the cloud save,
        /// will overwrite local save files
        /// </summary>
        public void PullSaves()
        {
            Logger.Debug("Downloading remote saves");
            this.m_googleDriveHandler.Download(this.RemoteFileId, this.LocalSaveFile.ZipSaveStoragePath);
            Logger.Debug("Downloading complete. Loading Remote Save Files to Local Save");
            this.LocalSaveFile.LoadZippedSaveFile();
        }

        /// <summary>
        /// Syncs the cloud save with this PC's save
        /// Overwrite cloud files
        /// </summary>
        public void PushSaves()
        {
            Logger.Debug("Updating remote save with local save");
            SaveFileMetadata metadata = this.LocalSaveFile.ZipSaveFile();
            this.m_googleDriveHandler.Update(this.RemoteFileName, this.LocalSaveFile.ZipSaveStoragePath,
                this.RemoteFileId, "application/zip", JsonConvert.SerializeObject(metadata));
            Logger.Debug("Remote Save Updated");
        }

        public static TouhouRemoteSaveFile[] ToTouhouRemoteSaveFiles(TouhouLocalSaveFile[] localSaveFile,
            GoogleDriveHandler googleDriveHandler, string remoteSaveFolderId)
        {
            TouhouRemoteSaveFile[] remoteSaveFiles = new TouhouRemoteSaveFile[localSaveFile.Length];
            int i = 0;
            foreach (TouhouLocalSaveFile saveFile in localSaveFile)
            {
                remoteSaveFiles[i] = new TouhouRemoteSaveFile(saveFile, googleDriveHandler, remoteSaveFolderId);
                i++;
            }

            return remoteSaveFiles;
        }

        public static TouhouRemoteSaveFile[] ToTouhouRemoteSaveFiles(TouhouLocalSaveFile[] newGenLocal,
            TouhouLocalSaveFile[] oldGenLocal, GoogleDriveHandler googleDriveHandler, string remoteSaveFolderId)
        {
            TouhouRemoteSaveFile[] remoteSaveFiles = new TouhouRemoteSaveFile[newGenLocal.Length + oldGenLocal.Length];
            int i = 0;
            foreach (TouhouLocalSaveFile saveFile in newGenLocal)
            {
                remoteSaveFiles[i] = new TouhouRemoteSaveFile(saveFile, googleDriveHandler, remoteSaveFolderId);
                i++;
            }

            foreach (TouhouLocalSaveFile saveFile in oldGenLocal)
            {
                remoteSaveFiles[i] = new TouhouRemoteSaveFile(saveFile, googleDriveHandler, remoteSaveFolderId);
                i++;
            }

            return remoteSaveFiles;
        }
    }
}