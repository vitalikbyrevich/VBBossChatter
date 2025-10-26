namespace VBBossChatter
{
    public static class BossMessage
    {
        private static BossMessageConfig _mainConfig;
        private static readonly Dictionary<string, BossMessages> _bossMessagesCache = new Dictionary<string, BossMessages>();
        private static readonly Dictionary<string, BossConfigEntry> _bossConfigCache = new Dictionary<string, BossConfigEntry>();

        public static void LoadConfig()
        {
            string configDir = Path.Combine(Paths.ConfigPath, "VBBossChatter");
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            // Загружаем или создаем основной конфиг
            string mainConfigPath = Path.Combine(configDir, "boss_config.json");
            _mainConfig = LoadOrCreateMainConfig(mainConfigPath);

            // Создаем кэш настроек
            foreach (var bossConfig in _mainConfig.BossPrefabs)
            {
                _bossConfigCache[bossConfig.PrefabName] = bossConfig;
                Debug.Log($"VBBossChatter: Зарегистрирован босс {bossConfig.PrefabName} -> {bossConfig.ConfigFile} (Enabled: {bossConfig.Enabled})");
            }

            // Создаем конфиги для всех боссов
            CreateAllBossConfigs();

            Debug.Log($"VBBossChatter: Загружено {_mainConfig.BossPrefabs.Count} боссов");
        }

        private static BossMessageConfig LoadOrCreateMainConfig(string configPath)
        {
            string json;
            if (File.Exists(configPath))
            {
                try
                {
                    json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<BossMessageConfig>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"VBBossChatter: Ошибка загрузки основного конфига: {e.Message}");
                }
            }

            // Создаем новый конфиг с боссами по умолчанию
            var config = new BossMessageConfig();

            var defaultBosses = new[]
            {
                "Eikthyr", "gd_king", "Bonemass", "Dragon", "GoblinKing", "SeekerQueen", "Fader"
            };

            foreach (var bossName in defaultBosses)
            {
                config.BossPrefabs.Add(new BossConfigEntry
                {
                    PrefabName = bossName,
                    Enabled = true,
                    ConfigFile = $"{bossName.ToLower()}_messages.json",
                    DespawnMessagesChance = config.DefaultSettings.DespawnMessagesChance,
                    LostMessagesChance = config.DefaultSettings.LostMessagesChance,
                    KillMessagesChance = config.DefaultSettings.KillMessagesChance,
                    TauntMessagesChance = config.DefaultSettings.TauntMessagesChance,
                    RareTauntMessagesChance = config.DefaultSettings.RareTauntMessagesChance,
                    DamageTauntMessagesChance = config.DefaultSettings.DamageTauntMessagesChance,
                    BlockTauntMessagesChance = config.DefaultSettings.BlockTauntMessagesChance,
                    HealTauntMessagesChance = config.DefaultSettings.HealTauntMessagesChance,
                    AggroTauntMessagesChance = config.DefaultSettings.AggroTauntMessagesChance,
                    FoodTauntMessagesChance = config.DefaultSettings.FoodTauntMessagesChance,
                    PotionTauntMessagesChance = config.DefaultSettings.PotionTauntMessagesChance,
                    BerryTauntMessagesChance = config.DefaultSettings.BerryTauntMessagesChance,
                    MushroomTauntMessagesChance = config.DefaultSettings.MushroomTauntMessagesChance
                });
            }

            json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configPath, json);
            Debug.Log("VBBossChatter: Создан новый основной конфиг с боссами по умолчанию");

            return config;
        }

        public static BossConfigEntry GetBossConfig(string bossPrefabName)
        {
            if (_bossConfigCache.TryGetValue(bossPrefabName, out var config))
                return config;

            return _mainConfig?.DefaultSettings ?? new BossConfigEntry();
        }

        public static bool IsBossEnabled(string bossPrefabName)
        {
            var config = GetBossConfig(bossPrefabName);
            return config.Enabled;
        }

        public static bool ShouldShowMessage(string bossPrefabName, string messageType)
        {
            if (!IsBossEnabled(bossPrefabName))
                return false;

            var config = GetBossConfig(bossPrefabName);
            int chance = config.GetChanceForMessageType(messageType);
            return UnityEngine.Random.Range(0, 100) < chance;
        }

        public static BossMessages GetMessagesForBoss(string bossPrefabName)
        {
            if (string.IsNullOrEmpty(bossPrefabName))
            {
                Debug.Log($"VBBossChatter: Получено пустое имя босса");
                return new BossMessages();
            }

            Debug.Log($"VBBossChatter: Поиск сообщений для босса: {bossPrefabName}");

            // Если босс уже в кэше - возвращаем его сообщения
            if (_bossMessagesCache.ContainsKey(bossPrefabName))
            {
                return _bossMessagesCache[bossPrefabName];
            }

            // Получаем конфиг босса
            var bossConfig = GetBossConfig(bossPrefabName);

            // Если босс отключен - возвращаем пустые сообщения
            if (!bossConfig.Enabled)
            {
                Debug.Log($"VBBossChatter: Босс {bossPrefabName} отключен в конфиге");
                _bossMessagesCache[bossPrefabName] = new BossMessages();
                return new BossMessages();
            }

            // Загружаем из файла если есть индивидуальный конфиг
            if (!string.IsNullOrEmpty(bossConfig.ConfigFile))
            {
                string configPath = Path.Combine(Paths.ConfigPath, "VBBossChatter", bossConfig.ConfigFile);

                if (File.Exists(configPath))
                {
                    try
                    {
                        string json = File.ReadAllText(configPath);
                        var messages = JsonConvert.DeserializeObject<BossMessages>(json);
                        _bossMessagesCache[bossPrefabName] = messages;
                        Debug.Log($"VBBossChatter: Загружены сообщения для {bossPrefabName}");
                        return messages;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"VBBossChatter: Ошибка загрузки конфига для {bossPrefabName}: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"VBBossChatter: Файл конфига не найден: {configPath}");
                    CreateDefaultBossConfig(bossPrefabName, configPath);
                }
            }
            else
            {
                Debug.LogWarning($"VBBossChatter: Не указан ConfigFile для босса {bossPrefabName}");
            }

            // Возвращаем пустые сообщения по умолчанию
            _bossMessagesCache[bossPrefabName] = new BossMessages();
            return new BossMessages();
        }

        private static void CreateAllBossConfigs()
        {
            var defaultMessages = new BossMessages();

            foreach (var bossConfig in _mainConfig.BossPrefabs)
            {
                if (bossConfig.Enabled)
                {
                    string configPath = Path.Combine(Paths.ConfigPath, "VBBossChatter", bossConfig.ConfigFile);
                    if (!File.Exists(configPath))
                    {
                        var bossMessages = new BossMessages();
                        string json = JsonConvert.SerializeObject(bossMessages, Formatting.Indented);
                        File.WriteAllText(configPath, json);
                        _bossMessagesCache[bossConfig.PrefabName] = bossMessages;
                        Debug.Log($"VBBossChatter: Создан конфиг для {bossConfig.PrefabName}");
                    }
                }
                else
                {
                    Debug.Log($"VBBossChatter: Босс {bossConfig.PrefabName} отключен в конфиге");
                }
            }
        }

        private static void CreateDefaultBossConfig(string bossName, string configPath)
        {
            try
            {
                var bossMessages = new BossMessages();
                string json = JsonConvert.SerializeObject(bossMessages, Formatting.Indented);
                File.WriteAllText(configPath, json);
                _bossMessagesCache[bossName] = bossMessages;
                Debug.Log($"VBBossChatter: Создан конфиг по умолчанию для {bossName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"VBBossChatter: Ошибка создания конфига для {bossName}: {e.Message}");
            }
        }

        // Методы для удобного доступа к сообщениям с проверкой шанса
        public static string[] GetDespawnMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "despawn")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.DespawnMessages ?? new string[0];
        }

        public static string[] GetLostMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "lost")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.LostMessages ?? new string[0];
        }

        public static string[] GetKillMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "kill")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.KillMessages ?? new string[0];
        }

        public static string[] GetTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "taunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.TauntMessages ?? new string[0];
        }

        public static string[] GetRareTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "raretaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.RareTauntMessages ?? new string[0];
        }

        public static string[] GetDamageTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "damagetaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.DamageTauntMessages ?? new string[0];
        }

        public static string[] GetBlockTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "blocktaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.BlockTauntMessages ?? new string[0];
        }

        public static string[] GetHealTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "healtaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.HealTauntMessages ?? new string[0];
        }

        public static string[] GetAggroTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "aggrotaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.AggroTauntMessages ?? new string[0];
        }

        public static string[] GetFoodTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "foodtaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.FoodTauntMessages ?? new string[0];
        }

        public static string[] GetPotionTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "potiontaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.PotionTauntMessages ?? new string[0];
        }

        public static string[] GetBerryTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "berrytaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.BerryTauntMessages ?? new string[0];
        }

        public static string[] GetMushroomTauntMessages(string bossPrefabName)
        {
            if (!ShouldShowMessage(bossPrefabName, "mushroomtaunt")) return new string[0];
            return GetMessagesForBoss(bossPrefabName)?.MushroomTauntMessages ?? new string[0];
        }

        public static HashSet<string> GetPotionPrefabs(string bossPrefabName)
        {
            return GetMessagesForBoss(bossPrefabName)?.PotionPrefabs ?? new HashSet<string>();
        }

        public static HashSet<string> GetFoodPrefabs(string bossPrefabName)
        {
            return GetMessagesForBoss(bossPrefabName)?.FoodPrefabs ?? new HashSet<string>();
        }

        public static HashSet<string> GetBerryPrefabs(string bossPrefabName)
        {
            return GetMessagesForBoss(bossPrefabName)?.BerryPrefabs ?? new HashSet<string>();
        }

        public static HashSet<string> GetMushroomPrefabs(string bossPrefabName)
        {
            return GetMessagesForBoss(bossPrefabName)?.MushroomPrefabs ?? new HashSet<string>();
        }
    }
}