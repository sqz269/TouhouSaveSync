using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TouhouSaveSync.SaveFiles
{
    public enum TouhouGameGeneration
    {
        New,
        Old
    }

    public abstract class TouhouSaveFile
    {
        public readonly string GameTitle;
        public readonly string GameSavePath;
        public readonly string ZipSaveStoragePath;
        public readonly TouhouGameGeneration Generation;

        protected TouhouSaveFile(string gameTitle, string zipSaveStoragePath, string gameSavePath, TouhouGameGeneration generation)
        {
            this.GameTitle = gameTitle;
            this.ZipSaveStoragePath = zipSaveStoragePath;
            this.Generation = generation;
            this.GameSavePath = gameSavePath;
        }

        /// <summary>
        /// Generates a MD5 Checksum for file at ZipSaveStoragePath, if the file does not exist
        /// Then return an empty string
        /// </summary>
        /// <returns>A string representing the MD5 checksum for the Save file</returns>
        public string GenerateCheckSumForZipFile()
        {
            if (!File.Exists(this.ZipSaveStoragePath))
                return "";

            using var md5 = MD5.Create();
            using var stream = File.OpenRead(this.ZipSaveStoragePath);
            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Zip the folder at GameSaveFile and store it at ZipSaveStoragePath
        /// Then, return the checksum of the zip file
        /// <br></br>
        /// To get the zip file's path, simply access ZipSaveStoragePath of the instance
        /// </summary>
        /// <returns>The MD5 Checksum of the newly created zip file</returns>
        public abstract string ZipSaveFile();

        public abstract string LoadZippedSaveFile();
    }
}
