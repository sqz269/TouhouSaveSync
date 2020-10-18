using System.Diagnostics;

namespace TouhouSaveSync.Utility
{
    public static class ProcessUtility
    {
        public static bool IsProcessActive(string procName)
        {
            return Process.GetProcessesByName(procName).Length != 0;
        }
    }
}
