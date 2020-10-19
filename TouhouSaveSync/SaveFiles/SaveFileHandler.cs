using System;
using System.Collections.Generic;
using System.IO;

namespace TouhouSaveSync.SaveFiles
{
    public class SaveFileHandler
    {
        public readonly TouhouSaveFile SaveFile;
        public readonly string ExecutableName;
        private FileSystemWatcher m_fileWatcher;

        /// <summary>
        /// Delegate function that is called when the score.dat file is changed
        /// </summary>
        /// <param name="handler">The handler for the save file that was changed</param>
        public delegate void OnSaveFileChange(SaveFileHandler handler);

        public OnSaveFileChange OnSaveFileChangeCallback;

        public SaveFileHandler(TouhouSaveFile saveFile)
        {
            this.SaveFile = saveFile;
            this.ExecutableName = (this.SaveFile.Generation == TouhouGameGeneration.New
                ? FindTouhouSavePath.TouhouToExeNameNewGen[this.SaveFile.GameTitle]
                : FindTouhouSavePath.TouhouToExeNameOldGen[this.SaveFile.GameTitle]).Split(".")[0];
            this.RegisterFileSystemWatcher();
        }

        /// <summary>
        /// Watch the SaveFile.GameSavePath directory for any changes in .dat file
        /// and set the call back for changes to OnSaveFileChangeFromWatch
        /// </summary>
        private void RegisterFileSystemWatcher()
        {
            this.m_fileWatcher = new FileSystemWatcher(this.SaveFile.GameSavePath)
            {
                NotifyFilter = NotifyFilters.LastWrite, 
                Filter = "*.dat" // Too lazy to actually find the score.dat file and watch, so we're just watch the whole dir
            };
            this.m_fileWatcher.Changed += this.OnSaveFileChangeFromWatch;
            this.m_fileWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// A callback used by RegisterFileSystemWatcher,
        /// invokes OnSaveFileChangeCallback on call
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSaveFileChangeFromWatch(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File: {e.FullPath} changed. Type: {e.ChangeType}");
            this.OnSaveFileChangeCallback?.Invoke(this);
        }

        /// <summary>
        /// Register a function callback when the score file is changed
        /// </summary>
        /// <param name="callback">The function to be registered as a callback</param>
        public void RegisterOnSaveFileChangeCallbackExternal(OnSaveFileChange callback)
        {
            this.OnSaveFileChangeCallback += callback;
        }

        public static SaveFileHandler[] ToTouhouSaveFilesHandlers(Dictionary<string, string> newGen,
            Dictionary<string, string> oldGen)
        {
            SaveFileHandler[] handlers = new SaveFileHandler[newGen.Count + oldGen.Count];
            int i = 0;
            foreach (TouhouNewGenSaveFile newGenSaveFile in TouhouNewGenSaveFile.ToTouhouSaveFiles(newGen))
            {
                Console.WriteLine("Instantiating TouhouSaveFileHandler for: {0}", newGenSaveFile.GameTitle);
                handlers[i] = new SaveFileHandler(newGenSaveFile);
                i++;
            }

            foreach (TouhouOldGenSaveFile oldGenSaveFile in TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGen))
            {
                Console.WriteLine("Instantiating TouhouSaveFileHandler for: {0}", oldGenSaveFile.GameTitle);
                handlers[i] = new SaveFileHandler(oldGenSaveFile);
                i++;
            }

            return handlers;
        }
    }
}