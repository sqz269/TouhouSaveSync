using System.Configuration;

namespace TouhouSaveSync.Config
{
    public static class ConfigManager
    {
        private static readonly System.Configuration.Configuration Config = 
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        public static string GetSetting(string key)
        {
            return Config.AppSettings.Settings[key].Value;
        }

        public static void UpdateSetting(string key, string value, bool save = true)
        {
            Config.AppSettings.Settings[key].Value = value;
            if (save)
                Config.Save(ConfigurationSaveMode.Modified);
        }

        public static void AddOrUpdateSetting(string key, string value, bool save = true)
        {
            if (Config.AppSettings.Settings[key] == null)
            {
                Config.AppSettings.Settings.Add(key, value);
                Config.Save(ConfigurationSaveMode.Modified);
            }
            else
            {
                UpdateSetting(key, value);
            }
        }

        public static void Save(ConfigurationSaveMode mode)
        {
            Config.Save(mode);
        }
    }
}
