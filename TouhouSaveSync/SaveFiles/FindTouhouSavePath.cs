using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Google.Apis.Http;
using TouhouSaveSync.Config;

namespace TouhouSaveSync.Utility
{
    public static class FindTouhouSavePath
    {
        public static readonly Dictionary<String, String> TouhouToExeName = new Dictionary<string, string>
        {
            {"Touhou06", "東方紅魔郷.exe"},
            {"Touhou07", "th07.exe"},
            {"Touhou75", "th075.exe"},
            {"Touhou08", "th08.exe"},
            {"Touhou09", "th09.exe"},
            {"Touhou95", "th095.exe"},
            {"Touhou10", "th10.exe"},
            {"Touhou105", "th105.exe"},
            {"Touhou11", "th11.exe"},
            {"Touhou12", "th12.exe"}
        };

        public static readonly Dictionary<String, String> ExeNameToTouhou = new Dictionary<string, string>
        {
            {"東方紅魔郷.exe", "Touhou06"},
            {"th07.exe", "Touhou07"},
            {"th075.exe", "Touhou75"},
            {"th08.exe", "Touhou08"},
            {"th09.exe", "Touhou09"},
            {"th095.exe", "Touhou95"},
            {"th10.exe", "Touhou10"},
            {"th105.exe", "Touhou105"},
            {"th11.exe", "Touhou11"},
            {"th12.exe", "Touhou12"}
        };

        /// <summary>
        /// Recursively walks the directory and search for touhou exe
        /// </summary>
        /// <param name="directory">The directory to search for</param>
        /// <param name="itemsFound">A dictionary contains TouhouGame : GameExePath</param>
        public static void SearchTouhouOldGenerationExe(string directory, Dictionary<String, String> itemsFound)
        {
            foreach (string f in Directory.GetFiles(directory, "*.exe"))
            {
                // Get the filename from the path
                string fileName = f.Split(Path.DirectorySeparatorChar)[^1];

                String game = ExeNameToTouhou.GetValueOrDefault(fileName);
                if (game != null)
                {
                    Console.WriteLine("Found {0}", game);
                    itemsFound.TryAdd(game, f);
                }
            }

            foreach (string d in Directory.GetDirectories(directory))
            {
                SearchTouhouOldGenerationExe(d, itemsFound);
            }
        }

        public static Dictionary<String, String> GetTouhouOldGenPath(string dir)
        {
            Dictionary<String, String> itemsFound = new Dictionary<string, string>();
            SearchTouhouOldGenerationExe(dir, itemsFound);
            return itemsFound;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="touhouExe"></param>
        public static void UpdateTouhouOldGenerationConfig(Dictionary<String, String> touhouExe)
        {
            // TODO: Remove entries if necessary
            foreach (KeyValuePair<string, string> kv in touhouExe)
            {
                ConfigManager.AddOrUpdateSetting(kv.Key, kv.Value, false);
            }
            ConfigManager.Save(ConfigurationSaveMode.Modified);
        }

        /// <summary>
        /// Walks the surface of the directory at %APPDATA%\ShanghaiAlice to find newer touhou game's save folders
        /// </summary>
        /// <param name="itemsFound">The dictionary contains TouhouGame : TouhouSaveFolder</param>
        public static void SearchTouhouNewGeneration(Dictionary<String, String> itemsFound)
        {
            string appdataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string touhouSaveFolder = Path.Join(appdataRoaming, "ShanghaiAlice");
            foreach (string d in Directory.GetDirectories(touhouSaveFolder))
            {
                string dirName = d.Split(Path.DirectorySeparatorChar)[^1];
                string thVer = dirName.Substring(2);  // This is getting the number part from the folder names. For example: th13 -> 13
                string thName = $"Touhou{thVer}";
                Console.WriteLine("Found {0}", thName);
                itemsFound.Add(thName, d);
            }
        }

        public static Dictionary<String, String> GetTouhouNewGenPath()
        {
            Console.WriteLine("Detecting Newer Generation Games...");
            Dictionary<String, String> newGenSavesFound = new Dictionary<string, string>();
            string appdataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string modernTouhouSavePath = Path.Join(appdataPath, "ShanghaiAlice");
            if (!Directory.Exists(modernTouhouSavePath))
                Console.WriteLine("Did not attempt to detect any modern touhou save file as {0} does not exist", modernTouhouSavePath);
            else
            {
                SearchTouhouNewGeneration(newGenSavesFound);
            }

            return newGenSavesFound;
        }

        public static void UpdateTouhouNewGenerationConfig(Dictionary<String, String> touhouDictionary)
        {

        }
    }
}