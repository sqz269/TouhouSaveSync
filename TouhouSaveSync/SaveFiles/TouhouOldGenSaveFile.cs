using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync.SaveFiles
{
    public sealed class TouhouOldGenSaveFile : TouhouSaveFile
    {
        private readonly string m_gameExeName;

        public TouhouOldGenSaveFile(string gameTitle, string zipSaveStoragePath, string gameSavePath) : 
            base(gameTitle, zipSaveStoragePath, gameSavePath, TouhouGameGeneration.Old)
        {
            this.m_gameExeName = FindTouhouSavePath.TouhouToExeName[gameTitle];
        }

        public override string ZipSaveFile()
        {
            using FileStream stream = new FileStream(this.ZipSaveStoragePath, FileMode.Create);
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create);

            string gameName = this.m_gameExeName.Split(".")[0];

            string cfgFilePath = Path.Combine(this.GameSavePath, $"{gameName}.cfg");
            if (File.Exists(cfgFilePath))
                archive.CreateEntryFromFile(cfgFilePath, $"{gameName}.cfg");

            // There could be 2 kind of score file names in the older generation of touhou games
            // The new engine game's dat file's name is score<GameName>.dat. Example: th10.exe -> scoreth10.dat
            //      Note that the file named <GameName>.dat is not the save file
            // while the older engine's (TH6-9) dat file's name is just score.dat
            string scoreFilePath = Path.Combine(this.GameSavePath, "score.dat");
            string scoreFilePathType2 = Path.Combine(this.GameSavePath, $"score{gameName}.dat");
            if (File.Exists(scoreFilePath))
                archive.CreateEntryFromFile(scoreFilePath, "score.dat");
            else if (File.Exists(scoreFilePathType2))
                archive.CreateEntryFromFile(scoreFilePathType2, $"score{gameName}.dat");
            else
                Console.WriteLine("No score file found for: {0}", this.GameTitle);

            string replayFolderPath = Path.Combine(this.GameSavePath, "replay");
            if (Directory.Exists(replayFolderPath))
                archive.CreateEntryFromDirectory(replayFolderPath, "replay");
            archive.Dispose();  // We need to stop the ZipArchive from accessing the file before Generating a checksum

            return this.GenerateCheckSumForZipFile();
        }

        public override string LoadZippedSaveFile()
        {
            throw new System.NotImplementedException();
        }

        public static TouhouOldGenSaveFile[] ToTouhouSaveFiles(Dictionary<string, string> data)
        {
            TouhouOldGenSaveFile[] saveFiles = new TouhouOldGenSaveFile[data.Count];
            int i = 0;
            foreach ((string gameTitle, string gamePath) in data)
            {
                string savePath = Directory.GetParent(gamePath).FullName;
                saveFiles[i] = new TouhouOldGenSaveFile(gameTitle, Path.GetTempFileName(), savePath);
                i++;
            }

            return saveFiles;
        }
    }
}