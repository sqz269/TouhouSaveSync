using Google.Apis.Drive.v3.Data;
using Newtonsoft.Json;
using TouhouSaveSync.GoogleDrive;

namespace TouhouSaveSync.SaveFiles
{
    public class TouhouRemoteSaveFile
    {
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
                    RemoteFileId = this.GetRemoteFileId();
                    return _remoteFileId;
                }

                return _remoteFileId;
            }
            set => _remoteFileId = value;
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
            return file == null ? this.CreateSaves() : file.Id;
        }

        public SaveFileMetadata GetRemoteSaveFileMetaData()
        {
            File remoteFile = m_googleDriveHandler.GetFile(this.RemoteFileId);
            string description = remoteFile.Description;
            // TODO: Handle when description doesn't have the correct value
            SaveFileMetadata metadata = JsonConvert.DeserializeObject<SaveFileMetadata>(description);
            return new SaveFileMetadata()
            {
                Checksum = metadata.Checksum,
                ZipLastMod = metadata.ZipLastMod,
                DatLastMod = metadata.DatLastMod,
                DatSize = metadata.DatSize,
                ZipSize = metadata.ZipSize
            };
        }

        /// <summary>
        /// Upload this PC's save to the google drive.
        /// <br></br>
        /// Should Only be used if there is no previous saves
        /// </summary>
        public string CreateSaves()
        {
            SaveFileMetadata metadata = this.LocalSaveFile.ZipSaveFile();
            // We set the description of the file to the ScoreDatModifyTime to later use it as a metric
            // for determining which side is outdated. See DetermineSyncAction for how is the description used
            string id = this.m_googleDriveHandler.Upload(this.RemoteFileName,
                this.LocalSaveFile.ZipSaveStoragePath, "application/zip", this.m_remoteSaveFolder,
                JsonConvert.SerializeObject(metadata));
            return id;
        }

        /// <summary>
        /// Sync this PC's save with the cloud save,
        /// will overwrite local save files
        /// </summary>
        public void PullSaves()
        {
            this.m_googleDriveHandler.Download(this.RemoteFileId, this.LocalSaveFile.ZipSaveStoragePath);
            this.LocalSaveFile.LoadZippedSaveFile();
        }

        /// <summary>
        /// Syncs the cloud save with this PC's save
        /// Overwrite cloud files
        /// </summary>
        public void PushSaves()
        {
            SaveFileMetadata metadata = this.LocalSaveFile.ZipSaveFile();
            this.m_googleDriveHandler.Update(this.RemoteFileName, this.LocalSaveFile.ZipSaveStoragePath,
                this.RemoteFileId, "application/zip", JsonConvert.SerializeObject(metadata));
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
    }
}