using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync.SaveFiles
{
    public sealed class TouhouLocalOldGenSaveFile : TouhouLocalSaveFile
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string m_gameExeName;

        public TouhouLocalOldGenSaveFile(string gameTitle, string zipSaveStoragePath, string gameSavePath) : 
            base(gameTitle, zipSaveStoragePath, gameSavePath, TouhouGameGeneration.Old)
        {
            this.m_gameExeName = FindTouhouSavePath.TouhouToExeNameOldGen[gameTitle];
        }

        public override SaveFileMetadata ZipSaveFile()
        {
            Logger.Debug($"Creating Zip file for {GameTitle} to \"{ZipSaveStoragePath}\"");

            using FileStream stream = new FileStream(this.ZipSaveStoragePath, FileMode.Create);
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create);

            string gameName = this.m_gameExeName.Split(".")[0];

            string cfgFilePath = Path.Combine(this.GameSavePath, $"{gameName}.cfg");
            if (File.Exists(cfgFilePath))
            {
                Logger.Trace($"Found config file (key bind and game settings) at \"{cfgFilePath}\"");
                archive.CreateEntryFromFile(cfgFilePath, $"{gameName}.cfg");
            }
            else
                Logger.Trace($"Did not find config file. Either it does not exist or wrong path has been guessed. Guessed path: \"{cfgFilePath}\"");

            // There could be 2 kind of score file names in the older generation of touhou games
            // The new engine game's dat file's name is score<GameName>.dat. Example: th10.exe -> scoreth10.dat
            //      Note that the file named <GameName>.dat is not the save file
            // while the older engine's (TH6-9) dat file's name is just score.dat
            string scoreFilePath = Path.Combine(this.GameSavePath, "score.dat");
            string scoreFilePathType2 = Path.Combine(this.GameSavePath, $"score{gameName}.dat");
            Logger.Trace("Guessing score.dat paths");
            if (File.Exists(scoreFilePath))
            {
                Logger.Trace($"Found score.dat at: \"{scoreFilePath}\". Adding to archive");
                archive.CreateEntryFromFile(scoreFilePath, "score.dat");
            }
            else if (File.Exists(scoreFilePathType2))
            {
                Logger.Trace($"Found score.dat at: \"{scoreFilePathType2}\" Adding to archive");
                archive.CreateEntryFromFile(scoreFilePathType2, $"score{gameName}.dat");
            }
            else
                Logger.Trace($"Did not find score.dat. Either it does not exist or wrong path has been guessed. Guess 1: \"{scoreFilePath}\" | Guess 2: \"{scoreFilePathType2}\"");

            Logger.Trace("Guessing replay paths");
            string replayFolderPath = Path.Combine(this.GameSavePath, "replay");
            if (Directory.Exists(replayFolderPath))
            {
                Logger.Trace($"Found replay directory at: \"{replayFolderPath}\". Adding to archive");
                archive.CreateEntryFromDirectory(replayFolderPath, "replay");
            }
            else
                Logger.Trace($"Did not file replay folder. Either it does not exist or wrong path has been guessed. Guess 1: \"{replayFolderPath}\"");
            archive.Dispose();  // We need to stop the ZipArchive from accessing the file before Generating a checksum

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
            foreach ((string gameTitle, string gamePath) in data)
            {
                saveFiles[i] = new TouhouLocalOldGenSaveFile(gameTitle, Path.GetTempFileName(), gamePath);
                i++;
            }

            return saveFiles;
        }
    }
}