using Jotunn.Extensions;

namespace VBBossChatter
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    
    class VBBossChatter : BaseUnityPlugin
    {
        private const string ModName = "VBBossChatter";
        private const string ModVersion = "0.0.1";
        private const string ModGUID = "VitByr.VBBossChatter";
        internal static VBBossChatter self;

        internal static ConfigEntry<float> radiusConfig;
        internal static ConfigEntry<float> despawnDelayConfig;
        
        private void Awake()
        {
            self = this;

            radiusConfig = Config.BindConfig("01 - BossDespawn", "Despawn radius", 100f, "Радиус обнаружения игроков", synced: true); 
            despawnDelayConfig = Config.BindConfig("01 - BossDespawn", "Despawn delay", 5f, "Через сколько минут босс деспавнится", synced: true);
            
            CreateConfigWatcher();
            
           Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);
        }
        
        private void OnDestroy()
        {
            Config.Save();
            Logger.LogInfo("DESTROY");
        }
        
        private void CreateConfigWatcher()
        {
            ConfigFileWatcher configFileWatcher = new(Config, reloadDelay: 1000);
            configFileWatcher.OnConfigFileReloaded += () =>
            {
            };
        }
        
        private bool CheckIfModIsLoaded(string modGUID)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos) if (keyValuePair.Value.Metadata.GUID.Equals(modGUID)) return true;
            return false;
        }
    }
}
