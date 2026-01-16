using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace TextToCad.SolidWorksAddin.Utils
{
    internal static class AddinConfig
    {
        private static readonly Lazy<KeyValueConfigurationCollection> Settings = new Lazy<KeyValueConfigurationCollection>(LoadSettings);

        public static string Get(string key, string defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return defaultValue;
            }

            var settings = Settings.Value;
            if (settings == null)
            {
                return defaultValue;
            }

            var entry = settings[key];
            return entry != null ? entry.Value : defaultValue;
        }

        private static KeyValueConfigurationCollection LoadSettings()
        {
            try
            {
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrWhiteSpace(assemblyPath))
                {
                    string configPath = assemblyPath + ".config";
                    if (File.Exists(configPath))
                    {
                        var map = new ExeConfigurationFileMap
                        {
                            ExeConfigFilename = configPath
                        };
                        var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                        return config.AppSettings.Settings;
                    }
                }
            }
            catch
            {
                // Fall back to default config below.
            }

            try
            {
                return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).AppSettings.Settings;
            }
            catch
            {
                return null;
            }
        }
    }
}
