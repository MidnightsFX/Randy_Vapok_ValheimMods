using BepInEx.Configuration;

namespace Common {
    /// <summary>
    /// Binding helpers over <see cref="ModContext.Cfg"/>.
    ///
    /// BindServerConfig marks entries IsAdminOnly, which is what makes Jotunn's SynchronizationManager
    /// treat them as server-authoritative; BindClientConfig leaves them local to each player.
    /// </summary>
    public static class ConfigBinder {
        private static ConfigFile Cfg => ModContext.Cfg;

        // -- Server synced (admin only) ------------------------------------------------------------

        public static ConfigEntry<bool> BindServerConfig(string category, string key, bool value, string description,
                                                        AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        public static ConfigEntry<int> BindServerConfig(string category, string key, int value, string description,
                                                       bool advanced = false, int valMin = 0, int valMax = 150) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, new AcceptableValueRange<int>(valMin, valMax),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        public static ConfigEntry<float> BindServerConfig(string category, string key, float value, string description,
                                                         bool advanced = false, float valMin = 0f, float valMax = 150f) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, new AcceptableValueRange<float>(valMin, valMax),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        public static ConfigEntry<string> BindServerConfig(string category, string key, string value, string description,
                                                          AcceptableValueList<string> acceptableValues = null, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>Generic escape hatch for types without a dedicated overload above.</summary>
        public static ConfigEntry<T> BindServerConfig<T>(string category, string key, T value, string description,
                                                        AcceptableValueBase acceptableValues, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        // -- Client local --------------------------------------------------------------------------

        public static ConfigEntry<T> BindClientConfig<T>(string category, string key, T value, string description,
                                                        AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = false, IsAdvanced = advanced }));
        }
    }
}
