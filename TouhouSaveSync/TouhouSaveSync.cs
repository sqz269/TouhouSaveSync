using System;
using TouhouSaveSync.Config;
using TouhouSaveSync.GoogleDrive;
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

            GoogleDriveHandler googleDriveHandler = 
                new GoogleDriveHandler(ConfigManager.GetSetting("CredentialsPath"), ConfigManager.GetSetting("TokenPath"));

            SyncHandler syncHandler = new SyncHandler(googleDriveHandler);
            syncHandler.SyncLoop();
        }
    }
}
