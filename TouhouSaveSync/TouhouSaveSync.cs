using System;
using System.IO;
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

            string credentials = ConfigManager.GetSetting("CredentialsPath");
            if (!File.Exists(credentials))
            {
                Logger.Fatal($"No credentials exists at: \"{credentials}\". Cannot proceed");
                InputManager.GetStringInput("Press Enter to exit");
                Environment.Exit(1);
            }

            Logger.Info("Initializing Google Drive API");
            GoogleDriveHandler googleDriveHandler = 
                new GoogleDriveHandler(credentials, ConfigManager.GetSetting("TokenPath"));
            Logger.Info("Google Drive Initialized");

            SyncHandler syncHandler = new SyncHandler(googleDriveHandler);
            syncHandler.SyncLoop();
        }
    }
}
