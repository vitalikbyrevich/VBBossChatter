namespace VBBossChatter;

public class BossUtill
{
    public static readonly Dictionary<Vector3, BossData> bossDataDict = new();
    public static readonly HashSet<Vector3> bossesToRemove = new();

    public struct BossData
    {
        public float LastPlayerSeenTime;
        public bool IsTimerRunning;
        public Humanoid BossRef;
    }

    public static void SendMessageInChatShout(Humanoid boss, string[] messages, string m_color, string playerName = null)
    {
        if (!boss || !Chat.instance || messages == null || messages.Length == 0) return;

        string bossName = Localization.instance.Localize(boss.m_name);
        string randomMessage = messages[Random.Range(0, messages.Length)];

        string finalText = string.IsNullOrEmpty(playerName) ? $"<color={m_color}>{randomMessage}</color>" : $"<color={m_color}>{playerName}, {randomMessage}</color>";

        Chat.instance.m_hideTimer = 0f;
        Chat.instance.AddString(bossName, finalText, Talker.Type.Shout);
    }

    public static void SendMessageAsRaven(Humanoid boss, string[] messages, string color = "#FFA500", string playerName = null)
    {
        if (!boss || !Chat.instance || messages == null || messages.Length == 0) return;

        string randomMessage = messages[Random.Range(0, messages.Length)];
        if (string.IsNullOrEmpty(randomMessage)) return;

        // Форматируем текст
        string finalText;
        if (string.IsNullOrEmpty(playerName))
        {
            finalText = $"<color={color}>{randomMessage}</color>";
        }
        else
        {
            finalText = $"<color={color}>{playerName}, {randomMessage}</color>";
        }

        Vector3 textOffset = Vector3.up * 2f;
        float textCullDistance = 25f;
        float visibleTime = 5f;

        Chat.instance.SetNpcText(boss.gameObject, textOffset, textCullDistance, visibleTime, "", finalText, large: false);
    }

    public static string GetBossPrefabName(Humanoid boss)
    {
        if (boss == null) return "unknown";
    
        try
        {
            // Получаем Character компонент
            Character character = boss as Character ?? boss.GetComponent<Character>();
            if (character != null)
            {
                // Пытаемся найти префаб в ZNetScene
                foreach (var prefab in ZNetScene.instance.m_prefabs)
                {
                    var prefabCharacter = prefab.GetComponent<Character>();
                    if (prefabCharacter != null && 
                        prefabCharacter.m_name == character.m_name &&
                        prefabCharacter.IsBoss())
                    {
                        return prefab.name;
                    }
                }
            }
        
            // Запасной способ через ZDO
            if (boss.m_nview?.IsValid() == true)
            {
                ZDO zdo = boss.m_nview.GetZDO();
                if (zdo != null)
                {
                    GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                    if (prefab != null)
                    {
                        return prefab.name;
                    }
                }
            }
        
            return boss.gameObject.name.Replace("(Clone)", "").Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"VBBossChatter: Ошибка получения имени префаба босса: {e.Message}");
            return "unknown";
        }
    }

    public static string NormalizeBossName(string bossName)
    {
        if (string.IsNullOrEmpty(bossName)) return "unknown";
    
        // Просто убираем числовую часть из имени файла
        if (bossName.Contains("_messages"))
        {
            return bossName.Replace("_messages", "");
        }
    
        return bossName;
    }

    public static void CleanupDestroyedBosses()
    {
        foreach (var spawnPoint in bossesToRemove) bossDataDict.Remove(spawnPoint);
        bossesToRemove.Clear();

        var invalidKeys = new List<Vector3>();
        foreach (var kvp in bossDataDict)
            if (!kvp.Value.BossRef)
                invalidKeys.Add(kvp.Key);
        foreach (var key in invalidKeys) bossDataDict.Remove(key);
    }

    public static void DespawnBoss(Humanoid __instance, Vector3 spawnPoint)
    {
        if (!__instance) return;

        // Для деспавна используем чат, а не сообщение над боссом
        string bossPrefabName = GetBossPrefabName(__instance);
        string[] messages = BossMessage.GetDespawnMessages(bossPrefabName);
        SendMessageInChatShout(__instance, messages, "#FF0000"); //красный

        // Прямое удаление через ZNetScene
        if (ZNetScene.instance && __instance.gameObject)
        {
            if (__instance.m_nview && __instance.m_nview.IsValid()) 
                __instance.m_nview.ClaimOwnership();
            ZNetScene.instance.Destroy(__instance.gameObject);
        }

        // Fallback через ZDO если объект все еще существует
        if (__instance && __instance.gameObject)
        {
            if (__instance.m_nview && __instance.m_nview.IsValid())
            {
                ZDO zdo = __instance.m_nview.GetZDO();
                if (zdo != null) 
                    ZDOMan.instance.DestroyZDO(zdo);
            }
        }

        BossUtill.bossesToRemove.Add(spawnPoint);
    }

    public static bool ShouldProcess(Humanoid humanoid)
    {
        if (SceneManager.GetActiveScene().name != "main") return false;
        if (!ZNetScene.instance || !Player.m_localPlayer) return false;
        if (!humanoid || humanoid.IsDead()) return false;
        if (!humanoid.GetBaseAI()) return false;
        return humanoid.IsBoss();
    }

    public static List<Humanoid> GetAllAvailableBosses()
    {
        var bosses = new List<Humanoid>();
        foreach (var bossData in bossDataDict.Values)
        {
            if (bossData.BossRef && !bossData.BossRef.IsDead())
                bosses.Add(bossData.BossRef);
        }

        return bosses;
    }

    public static Humanoid FindRandomAvailableBoss()
    {
        var availableBosses = GetAllAvailableBosses();
        return availableBosses.Count > 0 ? availableBosses[Random.Range(0, availableBosses.Count)] : null;
    }

    public static Humanoid FindNearestBossToPlayer(Player player)
    {
        if (!player) return null;

        var availableBosses = GetAllAvailableBosses();
        if (availableBosses.Count == 0) return null;

        Humanoid nearestBoss = null;
        float nearestDistance = float.MaxValue;

        foreach (var boss in availableBosses)
        {
            float distance = Vector3.Distance(player.transform.position, boss.transform.position);
            if (distance < nearestDistance)
            {
                nearestBoss = boss;
                nearestDistance = distance;
            }
        }

        return nearestBoss;
    }

    public static Humanoid FindBossWithPlayerTarget(Player player)
    {
        if (!player) return null;

        var availableBosses = GetAllAvailableBosses();
        foreach (var boss in availableBosses)
        {
            var monsterAI = boss.GetBaseAI();
            if (monsterAI && monsterAI.HaveTarget() == player)
            {
                return boss;
            }
        }

        return null;
    }

    // МЕТОДЫ ДЛЯ РАБОТЫ С ИНВЕНТАРЕМ
    public static int GetItemCountInInventory(Inventory inventory, string prefabName)
    {
        if (inventory == null) return 0;

        int count = 0;
        foreach (var invItem in inventory.GetAllItems())
        {
            string invPrefab = GetRealPrefabName(invItem);
            if (invPrefab == prefabName) count += invItem.m_stack;
        }

        return count;
    }

    public static bool IsHealingItem(ItemDrop.ItemData item, string bossPrefabName = "default")
    {
        if (item == null) return false;
    
        string prefabName = GetRealPrefabName(item);
        if (string.IsNullOrEmpty(prefabName)) return false;

        // Получаем префабы для конкретного босса (или по умолчанию)
        HashSet<string> potionPrefabs = BossMessage.GetPotionPrefabs(bossPrefabName);
        HashSet<string> foodPrefabs = BossMessage.GetFoodPrefabs(bossPrefabName);
        HashSet<string> berryPrefabs = BossMessage.GetBerryPrefabs(bossPrefabName);
        HashSet<string> mushroomPrefabs = BossMessage.GetMushroomPrefabs(bossPrefabName);

        return potionPrefabs.Contains(prefabName) || 
               foodPrefabs.Contains(prefabName) || 
               berryPrefabs.Contains(prefabName) || 
               mushroomPrefabs.Contains(prefabName);
    }


    public static string[] GetHealTauntMessages(ItemDrop.ItemData item, string bossPrefabName = "default")
    {
        if (item == null) return new string[0];
    
        string prefabName = GetRealPrefabName(item);
        if (string.IsNullOrEmpty(prefabName)) 
            return BossMessage.GetHealTauntMessages(bossPrefabName);

        // Получаем префабы для конкретного босса
        HashSet<string> potionPrefabs = BossMessage.GetPotionPrefabs(bossPrefabName);
        HashSet<string> foodPrefabs = BossMessage.GetFoodPrefabs(bossPrefabName);
        HashSet<string> berryPrefabs = BossMessage.GetBerryPrefabs(bossPrefabName);
        HashSet<string> mushroomPrefabs = BossMessage.GetMushroomPrefabs(bossPrefabName);

        if (potionPrefabs.Contains(prefabName))
        {
            return BossMessage.GetPotionTauntMessages(bossPrefabName);
        }
        else if (foodPrefabs.Contains(prefabName))
        {
            return BossMessage.GetFoodTauntMessages(bossPrefabName);
        }
        else if (berryPrefabs.Contains(prefabName))
        {
            return BossMessage.GetBerryTauntMessages(bossPrefabName);
        }
        else if (mushroomPrefabs.Contains(prefabName))
        {
            return BossMessage.GetMushroomTauntMessages(bossPrefabName);
        }

        return BossMessage.GetHealTauntMessages(bossPrefabName);
    }

    public static string GetItemCategory(ItemDrop.ItemData item, string bossPrefabName = "default")
    {
        if (item == null) return "unknown";
    
        string prefabName = GetRealPrefabName(item);
        if (string.IsNullOrEmpty(prefabName)) return "unknown";

        // Получаем префабы для конкретного босса
        HashSet<string> potionPrefabs = BossMessage.GetPotionPrefabs(bossPrefabName);
        HashSet<string> foodPrefabs = BossMessage.GetFoodPrefabs(bossPrefabName);
        HashSet<string> berryPrefabs = BossMessage.GetBerryPrefabs(bossPrefabName);
        HashSet<string> mushroomPrefabs = BossMessage.GetMushroomPrefabs(bossPrefabName);

        if (potionPrefabs.Contains(prefabName)) return "potion";
        if (foodPrefabs.Contains(prefabName)) return "food";
        if (berryPrefabs.Contains(prefabName)) return "berry";
        if (mushroomPrefabs.Contains(prefabName)) return "mushroom";

        return "unknown";
    }

    public static string GetRealPrefabName(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab != null) return item.m_dropPrefab.name;

        // Попробуем восстановить через ObjectDB
        foreach (var prefab in ObjectDB.instance.m_items)
        {
            var drop = prefab.GetComponent<ItemDrop>();
            if (drop != null && drop.m_itemData.m_shared.m_name == item.m_shared.m_name)
            {
                return prefab.name;
            }
        }

        return string.Empty;
    }
}