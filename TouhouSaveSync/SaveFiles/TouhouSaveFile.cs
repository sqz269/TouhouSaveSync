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
        /// Get the save file's creation time in Unix timestamp
        /// </summary>
        /// <returns>A double representing seconds after 1970/1/1</returns>
        public double GetZipSaveFileModifyTime()
        {
            DateTime dateTime = File.GetLastWriteTime(this.ZipSaveStoragePath);
            return dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Get the name of the save file that should be stored as in google drive
        /// </summary>
        /// <returns>The name to store the save file as (Same as this.GameTitle)</returns>
        public string GetRemoteFileName()
        {
            return this.GameTitle;
        }

        /// <summary>
        /// Get the last mod date of the score.dat file in unix time
        /// <br></br>
        /// This is an important metric to determine if we want to upload the save file or download the save file
        /// </summary>
        /// <returns>A double representing seconds after 1970/1/1</returns>
        public double GetScoreDatModifyTime()
        {
            foreach (string f in Directory.GetFiles(this.GameSavePath, "*.dat"))
            {
                if (f.StartsWith("score"))
                {
                    DateTime dateTime = File.GetLastWriteTime(f);
                    return dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
                }
            }

            return -1;
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
