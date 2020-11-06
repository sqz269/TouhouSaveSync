#define TRACE

using System;
using TouhouSaveSync.Config;
using TouhouSaveSync.GoogleDrive;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync
{
    static class TouhouSaveSync
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        static void FirstTimeInit()
        {
            Console.WriteLine("It looks like it's the first time the program has ran on this computer. We need a few things to get started");
            string earlyTouhouGameDirectory =
                InputManager.GetStringInput("Please Enter the folder path contains touhou games: ");

            ConfigManager.UpdateSetting("TouhouGamesDirectory", earlyTouhouGameDirectory, false);
        }

        static void Main(string[] args)
        {
            LoggingHelper.ConfigureLogger();
            if (ConfigManager.GetSetting("FirstRun") == "true")
            {
                FirstTimeInit();
                ConfigManager.UpdateSetting("FirstRun", "false");
            }

            Logger.Info("Initializing Google Drive API");
            GoogleDriveHandler googleDriveHandler = 
                new GoogleDriveHandler(ConfigManager.GetSetting("CredentialsPath"), ConfigManager.GetSetting("TokenPath"));
            Logger.Info("Google Drive Initialized");

            SyncHandler syncHandler = new SyncHandler(googleDriveHandler);
            syncHandler.SyncLoop();
        }
    }
}
