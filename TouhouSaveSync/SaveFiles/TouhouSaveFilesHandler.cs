using System;
using System.Collections.Generic;
using System.IO;

namespace TouhouSaveSync.SaveFiles
{
    public class TouhouSaveFilesHandler
    {
        public readonly TouhouSaveFile SaveFile;
        private FileSystemWatcher m_fileWatcher;

        public TouhouSaveFilesHandler(TouhouSaveFile saveFile)
        {
            this.SaveFile = saveFile;
            this.RegisterFileSystemWatcher();
        }

        private void RegisterFileSystemWatcher()
        {
            this.m_fileWatcher = new FileSystemWatcher(this.SaveFile.GameSavePath)
            {
                NotifyFilter = NotifyFilters.LastWrite, 
                Filter = "*.dat" // Too lazy to actually find the score.dat file and watch, so we're just watch the whole dir
            };
            this.m_fileWatcher.Changed += this.OnSaveFileChange;
            this.m_fileWatcher.EnableRaisingEvents = true;
        }

        private void OnSaveFileChange(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"File: {e.FullPath} changed. Type: {e.ChangeType}");
        }

        public static TouhouSaveFilesHandler[] ToTouhouSaveFilesHandlers(Dictionary<string, string> newGen,
            Dictionary<string, string> oldGen)
        {
            TouhouSaveFilesHandler[] handlers = new TouhouSaveFilesHandler[newGen.Count + oldGen.Count];
            int i = 0;
            foreach (TouhouNewGenSaveFile newGenSaveFile in TouhouNewGenSaveFile.ToTouhouSaveFiles(newGen))
            {
                Console.WriteLine("Instantiating TouhouSaveFileHandler for: {0}", newGenSaveFile.GameTitle);
                handlers[i] = new TouhouSaveFilesHandler(newGenSaveFile);
                i++;
            }

            foreach (TouhouOldGenSaveFile oldGenSaveFile in TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGen))
            {
                Console.WriteLine("Instantiating TouhouSaveFileHandler for: {0}", oldGenSaveFile.GameTitle);
                handlers[i] = new TouhouSaveFilesHandler(oldGenSaveFile);
                i++;
            }

            return handlers;
        }
    }
}