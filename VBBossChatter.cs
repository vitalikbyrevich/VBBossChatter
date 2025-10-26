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
        internal static ConfigEntry<float> bossDodgeChanceConfig;
        internal static ConfigEntry<float> bossDodgeCooldownConfig;

        
        private void Awake()
        {
            self = this;
            
            radiusConfig = Config.BindConfig("01 - BossDespawn", "Despawn radius", 100f, "Радиус обнаружения игроков", synced: true); 
            despawnDelayConfig = Config.BindConfig("01 - BossDespawn", "Despawn delay", 5f, "Через сколько минут босс деспавнится", synced: true);
            
            // Новые настройки уклонения босса
            bossDodgeChanceConfig = Config.BindConfig("02 - BossDodge", "Dodge chance", 0.4f, new ConfigDescription("Шанс уклонения босса от атаки игрока (0-1)", new AcceptableValueRange<float>(0f, 1f)).ToString(), synced: true);
            
            bossDodgeCooldownConfig = Config.BindConfig("02 - BossDodge", "Dodge cooldown", 3f, new ConfigDescription("Кулдаун уклонения в секундах", new AcceptableValueRange<float>(1f, 10f)).ToString(), synced: true);
            
            BossMessage.LoadConfig();
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
                // Перезагружаем настройки уклонения
                UpdateBossDodgeSettings();
            };
        }
        
        private void UpdateBossDodgeSettings()
        {
            // Обновляем настройки уклонения для всех боссов
            foreach (var boss in BossUtill.GetAllAvailableBosses())
            {
                var dodgeComponent = boss.GetComponent<VB_BossDodge>();
                if (dodgeComponent != null)
                {
                    dodgeComponent.m_dodgeChance = bossDodgeChanceConfig.Value;
                    dodgeComponent.m_dodgeCooldown = bossDodgeCooldownConfig.Value;
                }
            }
        }
        
        private bool CheckIfModIsLoaded(string modGUID)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos) if (keyValuePair.Value.Metadata.GUID.Equals(modGUID)) return true;
            return false;
        }
    }
}