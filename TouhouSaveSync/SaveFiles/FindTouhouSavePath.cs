using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Policy;
using TouhouSaveSync.Config;

namespace TouhouSaveSync.Utility
{
    public static class FindTouhouSavePath
    {
        /// <summary>
        /// For older generation games only (6-12)
        /// </summary>
        public static readonly Dictionary<String, String> TouhouToExeNameOldGen = new Dictionary<string, string>
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

        /// <summary>
        /// For older generation games only (6-12)
        /// </summary>
        public static readonly Dictionary<String, String> ExeNameToTouhouOldGen = new Dictionary<string, string>
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
        /// For newer generation games (12.5-???)
        /// </summary>
        public static readonly Dictionary<String, String> TouhouToExeNameNewGen = new Dictionary<string, string>
        {
            {"Touhou125", "th125.exe"},
            {"Touhou128", "th128.exe"},
            {"Touhou13", "th13.exe"},
            {"Touhou14", "th14.exe"},
            {"Touhou143", "th143.exe"},
            {"Touhou15", "th15.exe"},
            {"Touhou16", "th16.exe"},
            {"Touhou165", "th165.exe"},
            {"Touhou17", "th17.exe"},
            {"Touhou175", "th175.exe"}
        };

        /// <summary>
        /// For newer generation games (12.5-???)
        /// </summary>
        public static readonly Dictionary<String, String> ExeNameToTouhouNewGen = new Dictionary<string, string>
        {
            {"th125.exe", "Touhou125"},
            {"th128.exe", "Touhou128"},
            {"th13.exe", "Touhou13"},
            {"th14.exe", "Touhou14"},
            {"th143.exe", "Touhou143"},
            {"th15.exe", "Touhou15"},
            {"th16.exe", "Touhou16"},
            {"th165.exe", "Touhou165"},
            {"th17.exe", "Touhou17"},
            {"th175.exe", "Touhou175"}
        };

        private static void SearchDirectoryRecursive(string dir, Dictionary<String, String> matchFor,
            Dictionary<String, String> itemsFound, bool useFileName=false)
        {
            foreach (string f in Directory.GetFiles(dir, "*.exe"))
            {
                string fileName = f.Split(Path.DirectorySeparatorChar)[^1];

                String game = matchFor.GetValueOrDefault(fileName);
                if (game != null)
                {
                    itemsFound.TryAdd(game, useFileName ? fileName : f);
                }
            }

            foreach (string d in Directory.GetDirectories(dir))
            {
                SearchDirectoryRecursive(d, matchFor, itemsFound);
            }
        }

        /// <summary>
        /// Recursively walks the directory and search for touhou exe
        /// </summary>
        /// <param name="directory">The directory to search for</param>
        /// <param name="itemsFound">A dictionary contains TouhouGame : GameExePath</param>
        private static void SearchTouhouOldGenerationExe(string directory, Dictionary<String, String> itemsFound)
        {
            // This function sets the dictionary's value to .exe
            // so we need get the exe's parent path for save data path
            SearchDirectoryRecursive(directory, ExeNameToTouhouOldGen, itemsFound);
            foreach ((string key, string value) in itemsFound)
            {
                string saveDirectory = Directory.GetParent(value).FullName;
                itemsFound[key] = saveDirectory;
            }
        }

        public static Dictionary<String, String> GetTouhouOldGenPath(string dir)
        {
            Dictionary<String, String> itemsFound = new Dictionary<string, string>();
            SearchTouhouOldGenerationExe(dir, itemsFound);
            return itemsFound;
        }

        /// <summary>
        /// Walks the surface of the directory at %APPDATA%\ShanghaiAlice to find newer touhou game's save folders
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="itemsFound">The dictionary contains TouhouGame : TouhouSaveFolder</param>
        private static void SearchTouhouNewGenerationLocal(string directory, Dictionary<String, String> itemsFound)
        {
            // This function sets the dictionary's value to .exe
            // the exe name without the .exe is the save folder path at %APPDATA%
            SearchDirectoryRecursive(directory, ExeNameToTouhouNewGen, itemsFound, true);
            string touhouNewGenSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            foreach ((string key, string value) in itemsFound)
            {
                // Value is just the exe file name rather than full qualified path because SearchDirectoryRecursive's arg
                // so split the exe name to get the non extension part. For example: th13.exe -> th13
                string thName = value.Split(".")[0];
                string thSave = Path.Combine(touhouNewGenSavePath, thName);
                itemsFound[key] = thSave;
            }
        }

        public static Dictionary<String, String> GetTouhouNewGenPath(string directory)
        {
            Dictionary<String, String> newGenSavesFound = new Dictionary<string, string>();
            SearchTouhouNewGenerationLocal(directory, newGenSavesFound);
            return newGenSavesFound;
        }
    }
}