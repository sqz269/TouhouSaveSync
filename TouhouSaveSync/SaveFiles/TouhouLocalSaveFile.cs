using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace TouhouSaveSync.SaveFiles
{
    public enum TouhouGameGeneration
    {
        New,
        Old
    }

    public struct SaveFileMetadata
    {
        public string Checksum;
        public double ZipLastMod;
        public double ZipSize;
        public double DatLastMod;
        public double DatSize;

        public override string ToString()
        {
            return $"CheckSum: {Checksum} | Zip Last Mod: {ZipLastMod} | Dat Last Mod: {DatLastMod}";
        }
    }

    public abstract class TouhouLocalSaveFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly string GameTitle;
        public readonly string GameSavePath;
        public readonly string ZipSaveStoragePath;
        public readonly TouhouGameGeneration Generation;

        protected TouhouLocalSaveFile(string gameTitle, string zipSaveStoragePath, string gameSavePath, TouhouGameGeneration generation)
        {
            this.GameTitle = gameTitle;
            this.ZipSaveStoragePath = zipSaveStoragePath;
            this.Generation = generation;
            this.GameSavePath = gameSavePath;
            this.ValidateSavePath();
        }

        /// <summary>
        /// Check if GameSaveFilePath for new generations exist, if not, create them
        /// <br></br>
        /// Because we predicted the new generation's save path
        /// by combining the exe filename and %APPDATA%\ShanghaiAlice
        /// There is a possibility that the save directory does not exist at all
        /// and it will cause problems when we trying do stuff when the content
        /// </summary>
        private void ValidateSavePath()
        {
            if (this.Generation == TouhouGameGeneration.New)
            {
                Directory.CreateDirectory(this.GameSavePath);
            }
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

        public string GetScorePath()  // TODO: Cache Value
        {
            foreach (string f in Directory.GetFiles(this.GameSavePath, "*.dat"))
            {
                string filename = f.Split(Path.DirectorySeparatorChar)[^1];
                if (filename.StartsWith("score"))
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>
        /// Get the last mod date of the score.dat file in unix time
        /// <br></br>
        /// This is an important metric to determine if we want to upload the save file or download the save file
        /// </summary>
        /// <returns>A double representing seconds after 1970/1/1</returns>
        public double GetScoreDatModifyTime()
        {
            string f = GetScorePath();
            if (f != null)
            {
                DateTime dateTime = File.GetLastWriteTime(f);
                return dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
            }
            return -1;
        }

        public double GetScoreDatSize()
        {
            string f = GetScorePath();
            if (f != null)
            {
                return new FileInfo(f).Length;
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
        public abstract SaveFileMetadata ZipSaveFile();

        /// <summary>
        /// Extract the zip file at ZipSaveStoragePath to GameSavePath
        /// </summary>
        public void LoadZippedSaveFile()
        {
            Logger.Debug($"Loading Save From Zip. Zip Path: {ZipSaveStoragePath} -> {GameSavePath}");
            using FileStream stream = new FileStream(this.ZipSaveStoragePath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(stream);
            archive.ExtractToDirectory(this.GameSavePath, true);
        }
    }
}
