using System;
using System.Collections.Generic;
using System.IO;
using TouhouSaveSync.Utility;

namespace TouhouSaveSync.SaveFiles
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

                String game = DictionaryExtension.GetValueOrDefault(matchFor, fileName);
                if (game == null) continue;

                if (useFileName)
                    itemsFound.TryAdd(game, fileName);
                else
                    itemsFound.TryAdd(game, f);
            }

            foreach (string d in Directory.GetDirectories(dir))
            {
                SearchDirectoryRecursive(d, matchFor, itemsFound, useFileName);
            }
        }

        /// <summary>
        /// Recursively walks the directory and search for touhou exe
        /// <br></br>
        /// Add a element to itemsFound with The Game Title as the Key and the path of the save directory as value
        /// </summary>
        /// <param name="directory">The directory to search for</param>
        /// <param name="itemsFound">A dictionary contains TouhouGameName : Game Save Path</param>
        private static void SearchTouhouOldGenerationExe(string directory, Dictionary<String, String> itemsFound)
        {
            // This function sets the dictionary's value to .exe
            // so we need get the exe's parent path for save data path
            SearchDirectoryRecursive(directory, ExeNameToTouhouOldGen, itemsFound);
            List<string> keys = new List<string>(itemsFound.Keys);
            foreach (string key in keys)
            {
                string saveDirectory = Directory.GetParent(itemsFound[key]).FullName;
                itemsFound[key] = saveDirectory;
            }
        }

        /// <summary>
        /// Recursively walks the directory and search for older generation games
        /// </summary>
        /// <param name="dir">The directory to walk and search for</param>
        /// <returns>A dictionary with keys that represent the game's title (Touhou7 for example)<br></br>
        /// And value that is the game's save path</returns>
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
            string touhouNewGenSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ShanghaiAlice");
            List<string> keys = new List<string>(itemsFound.Keys);
            foreach (string key in keys)
            {
                string thName = itemsFound[key].Split(".")[0];
                string thSave = Path.Combine(touhouNewGenSavePath, thName);
                itemsFound[key] = thSave;
            }
        }

        /// <summary>
        /// Recursively walks the directory and search for newer generation games
        /// <br></br>
        /// Note: The save path is produced by guessing the path based on the exe file name
        /// <br></br>
        /// For example: if the exe name is th17.exe,
        /// the prediction will extract th17 combine that with %APPDATA%\ShanghaiAlice
        /// to produce the save directory, the save directory might be non-existent or just straight up invalid
        /// but it works for now
        /// </summary>
        /// <param name="dir">The directory to walk and search for</param>
        /// <returns>A dictionary with keys that represent the game's title (Touhou14 for example)<br></br>
        /// And value that is the game's save path (At %APPDATA%\ShanghaiAlice)</returns>
        public static Dictionary<String, String> GetTouhouNewGenPath(string dir)
        {
            Dictionary<String, String> newGenSavesFound = new Dictionary<string, string>();
            SearchTouhouNewGenerationLocal(dir, newGenSavesFound);
            return newGenSavesFound;
        }
    }
}