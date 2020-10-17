using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TouhouSaveSync.SaveFiles
{
    public sealed class TouhouNewGenSaveFile : TouhouSaveFile
    {
        public TouhouNewGenSaveFile(string gameTitle, string zipSaveStoragePath, string gameSavePath) : 
            base(gameTitle, zipSaveStoragePath, gameSavePath, TouhouGameGeneration.New)
        {
        }

        public override string ZipSaveFile()
        {
            if (File.Exists(this.ZipSaveStoragePath))
                File.Delete(this.ZipSaveStoragePath);
            ZipFile.CreateFromDirectory(this.GameSavePath, this.ZipSaveStoragePath);
            return this.GenerateCheckSumForZipFile();
        }

        public override string LoadZippedSaveFile()
        {
            throw new System.NotImplementedException();
        }

        public static TouhouNewGenSaveFile[] ToTouhouSaveFiles(Dictionary<string, string> data)
        {
            TouhouNewGenSaveFile[] saveFiles = new TouhouNewGenSaveFile[data.Count];
            int i = 0;
            foreach ((string gameTitle, string savePath) in data)
            {
                saveFiles[i] = new TouhouNewGenSaveFile(gameTitle, Path.GetTempFileName(), savePath);
                i++;
            }

            return saveFiles;
        }
    }
}