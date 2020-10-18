using System;
using System.Collections.Generic;

namespace TouhouSaveSync.SaveFiles
{
    public class TouhouSaveFilesHandler
    {
        public readonly TouhouSaveFile SaveFile;

        public TouhouSaveFilesHandler(TouhouSaveFile saveFile)
        {
            this.SaveFile = saveFile;
        }

        public static TouhouSaveFilesHandler[] ToTouhouSaveFilesHandlers(Dictionary<string, string> newGen,
            Dictionary<string, string> oldGen)
        {
            TouhouSaveFilesHandler[] handlers = new TouhouSaveFilesHandler[newGen.Count + oldGen.Count];
            int i = 0;
            foreach (TouhouNewGenSaveFile newGenSaveFile in TouhouNewGenSaveFile.ToTouhouSaveFiles(newGen))
            {
                handlers[i] = new TouhouSaveFilesHandler(newGenSaveFile);
                i++;
            }

            foreach (TouhouOldGenSaveFile oldGenSaveFile in TouhouOldGenSaveFile.ToTouhouSaveFiles(oldGen))
            {
                handlers[i] = new TouhouSaveFilesHandler(oldGenSaveFile);
            }

            return handlers;
        }
    }
}