using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using TouhouSaveSync.Config;
using TouhouSaveSync.GoogleDrive;
using TouhouSaveSync.SaveFiles;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync
{
    static class TouhouSaveSync
    {
        static void FirstTimeInit()
        {
            Console.WriteLine("It looks like it's the first time the program has ran on this computer. We need a few things to get started");
            string earlyTouhouGameDirectory =
                InputManager.GetStringInput("Please Enter the folder path contains early generation touhou games (6-12.8): ");

            ConfigManager.UpdateSetting("EarlyTouhouGamesDirectory", earlyTouhouGameDirectory, false);
        }

        static void Main(string[] args)
        {
            /*if (ConfigManager.GetSetting("FirstRun") == "true")
            {
                FirstTimeInit();
                ConfigManager.UpdateSetting("FirstRun", "false");
            }*/

            FirstTimeInit();
            Dictionary<String, String> oldGenGamesFound = FindTouhouSavePath.GetTouhouOldGenPath(ConfigManager.GetSetting("EarlyTouhouGamesDirectory"));
            Dictionary<String, String> newGenSavesFound = FindTouhouSavePath.GetTouhouNewGenPath();

            TouhouNewGenSaveFile[] newGenSaveFiles = TouhouNewGenSaveFile.ToTouhouSaveFiles(newGenSavesFound);
            TouhouOldGenSaveFile[] oldGenSaveFiles = TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGenGamesFound);

            GoogleDriveHandler googleDriveHandler = 
                new GoogleDriveHandler(ConfigManager.GetSetting("CredentialsPath"), ConfigManager.GetSetting("TokenPath"));
        }
    }
}
