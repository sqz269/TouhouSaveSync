using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Upload;
using File = Google.Apis.Drive.v3.Data.File;

namespace TouhouSaveSync.GoogleDrive
{
    public class FileExistsException : Exception
    {
        public FileExistsException()
        {
        }

        public FileExistsException(string message) : base(message)
        {
        }

        public FileExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class GoogleDriveHandler
    {
        public static readonly string[] Scopes = {DriveService.Scope.Drive};
        public static readonly string ApplicationName = "Touhou Save File Sync";

        private readonly string m_credentialsPath;
        private readonly string m_tokenPath;

        public UserCredential Credential { get; private set; }
        public DriveService Service { get; private set; }

        public GoogleDriveHandler(string credentialsPath="credentials.json", string tokenPath="token.json")
        {
            m_credentialsPath = credentialsPath;
            m_tokenPath = tokenPath;
            this.Auth();
        }

        /// <summary>
        /// Performs authentication with google api using provided credentialPath
        /// and then store the fetched token at provided tokenPath
        /// </summary>
        public void Auth()
        {
            using (var stream =
                new FileStream(this.m_credentialsPath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                this.Credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.m_tokenPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + this.m_tokenPath);
            }

            // Create Drive API service.
            this.Service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = this.Credential,
                ApplicationName = ApplicationName,
            });
        }

        /// <summary>
        /// Find list of folders with name
        /// <br />
        /// NOTE It may return a list of files
        /// </summary>
        /// <param name="name">The name of the file to find</param>
        /// <param name="pageToken">The page token of the list</param>
        /// <param name="parentFolder">The folder to search the file in</param>
        /// <returns>A list of files with the provided name</returns>
        public IList<File> FindFilesWithName(string name, string pageToken="", string parentFolder="")
        {
            FilesResource.ListRequest request = this.Service.Files.List();
            request.PageSize = 10;
            request.PageToken = pageToken;
            request.Fields = "nextPageToken, files(id, name, modifiedTime, description)";
            string query;
            if (parentFolder.Length == 0)
                query = $"(name = '{name}') and (not mimeType = 'application/vnd.google-apps.folder') and (trashed = false)";
            else
                query = $"(name = '{name}') and (not mimeType = 'application/vnd.google-apps.folder') and (parents in '{parentFolder}') and (trashed = false)";
            request.Q = query;

            return request.Execute().Files;
        }

        /// <summary>
        /// Find the first file with name, if there is no file exist, then return null
        /// </summary>
        /// <param name="name">The name of the file to find</param>
        /// <param name="parentFolder">The folder to search the file in</param>
        /// <returns>The file with provided name if exists, else null</returns>
        public File FindFirstFileWithName(string name, string parentFolder)
        {
            IList<File> files = this.FindFilesWithName(name, "", parentFolder);
            return files.Count == 0 ? null : files[0];
        }

        /// <summary>
        /// Find list of folders with name
        /// <br />
        /// NOTE It may return a list of folders
        /// </summary>
        /// <param name="name">The name of the folder to find</param>
        /// <param name="pageToken">The page token of the list</param>
        /// <returns>A list of folders with the provided name</returns>
        public IList<File> FindFoldersWithName(string name, string pageToken="")
        {
            FilesResource.ListRequest request = this.Service.Files.List();
            request.PageSize = 10;
            request.PageToken = pageToken;
            request.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{name}' and trashed = false";
            request.Fields = "nextPageToken, files(id, name)";
            return request.Execute().Files;
        }

        /// <summary>
        /// Creates a folder in google drive
        /// </summary>
        /// <param name="name">The name of the folder</param>
        /// <param name="description">The description of the folder</param>
        /// <param name="existOk">False to throw an error if a folder with the same name exists already,True to create another folder with same name</param>
        /// <returns></returns>
        public string Mkdir(string name, string description="", bool existOk=false)
        {
            if (!existOk)
            {
                if (this.FindFoldersWithName(name).Count > 0)
                {
                    throw new FileExistsException($"Can not create a new folder. A folder with name: {name} already exists.");
                }
            }
            File folder = new File {Name = name, Description = description, MimeType = "application/vnd.google-apps.folder"};
            return this.Service.Files.Create(folder).Execute().Id;
        }

        /// <summary>
        /// Create a folder if not already exists, and returns the newly created folder's id
        /// <br/>
        /// If folder(s) already exist under the same name then return the ID of the FIRST folder (retrieved by FindFoldersWithName) with the same name
        /// </summary>
        /// <param name="name">The name of the folder</param>
        /// <param name="description">The description of the folder if it's going to be created. This will not serve as a search criteria</param>
        /// <returns>Returns the id of the folder with provided name</returns>
        public string GetFolderId(string name, string description = "")
        {
            IList<File> files = this.FindFoldersWithName(name);
            if (files.Count > 0)
            {
                return files[0].Id;
            }

            return this.Mkdir(name, description, true);
        }

        /// <summary>
        /// Performs an NON resumable upload
        /// </summary>
        /// <param name="name">The name to appear on the uploaded file</param>
        /// <param name="filePath">The absolute path to the file to upload</param>
        /// <param name="contentType">The mime type of the file</param>
        /// <param name="uploadFolderPath">The folder to upload the file to (Must be a Folder id</param>
        /// <param name="description">The file's description</param>
        /// <returns>Return the ID of uploaded file</returns>
        public string Upload(string name, string filePath, string contentType, string uploadFolderPath="", string description="")
        {
            File fileMetadata = new File {Name = name, Description = description};

            if (uploadFolderPath.Length != 0)
                fileMetadata.Parents = new List<string>{uploadFolderPath};

            FilesResource.CreateMediaUpload request;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                request = this.Service.Files.Create(fileMetadata, stream, contentType);
                request.Fields = "id";
                request.ProgressChanged += this.GoogleDriveUploadProgress;
                request.Upload();
            }
            return request.ResponseBody.Id;
        }

        public string Update(string name, string filePath, string fileId, string contentType)
        {
            File fileMetadata = new File { Name = name };
            
            FilesResource.UpdateMediaUpload request;
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                // TODO: Figure out if by leaving fileMetadata.Name empty, it will keep the original name
                request = this.Service.Files.Update(fileMetadata, fileId, stream, contentType);
                request.Fields = "id";
                request.ProgressChanged += this.GoogleDriveUploadProgress;
                request.Upload();
            }

            return request.ResponseBody.Id;
        }

        private void GoogleDriveUploadProgress(Google.Apis.Upload.IUploadProgress progress)
        {
            if (progress.Status == UploadStatus.Starting)
                Console.WriteLine("Starting upload");
            else if (progress.Status == UploadStatus.Uploading)
                Console.WriteLine("Uploading... {0} bytes uploaded", progress.BytesSent);
            else if (progress.Status == UploadStatus.Completed)
                Console.WriteLine("Upload completed. Uploaded a total of {0} bytes", progress.BytesSent);
        }

        /// <summary>
        /// Uploads a file if not already exists, and returns the uploaded file id
        /// <br/>
        /// If file(s) already exist under the same name then REPLACES the FIRST file with the file provided and returns the id
        /// </summary>
        /// <returns>Returns the ID of either uploaded or updated file</returns>
        public string UploadFileIfNotExistElseUpdate(string fileName, string file)
        {
            return "";
        }
    }
}