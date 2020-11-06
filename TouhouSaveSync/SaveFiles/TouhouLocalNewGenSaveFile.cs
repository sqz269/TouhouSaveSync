using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TouhouSaveSync.SaveFiles
{
    public sealed class TouhouLocalNewGenSaveFile : TouhouLocalSaveFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public TouhouLocalNewGenSaveFile(string gameTitle, string zipSaveStoragePath, string gameSavePath) : 
            base(gameTitle, zipSaveStoragePath, gameSavePath, TouhouGameGeneration.New)
        {
        }

        public override SaveFileMetadata ZipSaveFile()
        {
            Logger.Debug($"Creating Zip file for {GameTitle} to {ZipSaveStoragePath}");
            // Because ZipFile.CreateFromDirectory does not attempt to overwrite the destination
            // and will throw an error if the destination already exist
            // so we just remove the tmp file before continuing to prevent error
            if (File.Exists(this.ZipSaveStoragePath))
                File.Delete(this.ZipSaveStoragePath);
            ZipFile.CreateFromDirectory(this.GameSavePath, this.ZipSaveStoragePath);

            Logger.Trace("Generating Metadata for created zip");
            string checksum = GenerateCheckSumForZipFile();
            double datSize = GetScoreDatSize();
            double zipSize = new FileInfo(ZipSaveStoragePath).Length;

            return new SaveFileMetadata()
            {
                Checksum = checksum,
                DatLastMod = GetScoreDatModifyTime(),
                ZipLastMod = GetZipSaveFileModifyTime(),
                DatSize = datSize,
                ZipSize = zipSize
            };
        }

        public static TouhouLocalSaveFile[] ToTouhouSaveFiles(Dictionary<string, string> data)
        {
            TouhouLocalSaveFile[] saveFiles = new TouhouLocalSaveFile[data.Count];
            int i = 0;
            foreach ((string gameTitle, string savePath) in data)
            {
                saveFiles[i] = new TouhouLocalNewGenSaveFile(gameTitle, Path.GetTempFileName(), savePath);
                i++;
            }

            return saveFiles;
        }
    }
}