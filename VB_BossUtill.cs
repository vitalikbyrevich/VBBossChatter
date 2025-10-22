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
        if (!boss || !Chat.instance) return;

        string bossName = Localization.instance.Localize(boss.m_name);
        string randomMessage = messages[Random.Range(0, messages.Length)];

        string finalText = string.IsNullOrEmpty(playerName) ? $"<color={m_color}>{randomMessage}</color>" : $"<color={m_color}>{playerName}, {randomMessage}</color>";

        Chat.instance.m_hideTimer = 0f;
        Chat.instance.AddString(bossName, finalText, Talker.Type.Shout);
    }

    public static void SendMessageInChatNormal(Humanoid boss, string[] messages, string m_color, string playerName = null)
    {
        if (!boss || !Chat.instance) return;

        string bossName = Localization.instance.Localize(boss.m_name);
        string randomMessage = messages[Random.Range(0, messages.Length)];

        string finalText = string.IsNullOrEmpty(playerName) ? $"<color={m_color}>{randomMessage}</color>" : $"<color={m_color}>{playerName}, {randomMessage}</color>";

        Chat.instance.m_hideTimer = 0f;
        Chat.instance.AddString(bossName, finalText, Talker.Type.Normal);
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

        BossUtill.SendMessageInChatShout(__instance, BossMessage.despawnMessages, "#FF0000"); //красный
        //  SendMessageAboveBossShout(__instance, despawnMessages);

        // Прямое удаление через ZNetScene
        if (ZNetScene.instance && __instance.gameObject)
        {
            if (__instance.m_nview && __instance.m_nview.IsValid()) __instance.m_nview.ClaimOwnership();
            ZNetScene.instance.Destroy(__instance.gameObject);
        }

        // Fallback через ZDO если объект все еще существует
        if (__instance && __instance.gameObject)
        {
            //   Debug.LogWarning($"Принудительное удаление босса {__instance.m_name} через ZDO");
            if (__instance.m_nview && __instance.m_nview.IsValid())
            {
                ZDO zdo = __instance.m_nview.GetZDO();
                if (zdo != null) ZDOMan.instance.DestroyZDO(zdo);
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


 /*   public static bool IsHealingConsumable(ItemDrop.ItemData item)
    {
        return item != null && item.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
    }

    public static GameObject GetItemPrefab(string prefabName)
    {
        if (ObjectDB.instance == null || string.IsNullOrEmpty(prefabName)) return null;
        return ObjectDB.instance.GetItemPrefab(prefabName);
    }

    public static string GetItemPrefabName(ItemDrop.ItemData item)
    {
        if (item?.m_shared?.m_name != null)
            return item.m_shared.m_name;

        return string.Empty;
    }*/

    public static bool IsHealingItem(ItemDrop.ItemData item)
    {
        string prefabName = GetRealPrefabName(item);
        if (string.IsNullOrEmpty(prefabName)) return false;

        return BossMessage.PotionPrefabs.Contains(prefabName) ||
               BossMessage.FoodPrefabs.Contains(prefabName) ||
               BossMessage.BerryPrefabs.Contains(prefabName) ||
               BossMessage.MushroomPrefabs.Contains(prefabName);
    }


    public static string[] GetHealTauntMessages(ItemDrop.ItemData item)
    {
        string prefabName = GetRealPrefabName(item);
        if (string.IsNullOrEmpty(prefabName)) return BossMessage.healTauntMessages;

        // Определяем категорию для подбора сообщений
        if (BossMessage.PotionPrefabs.Contains(prefabName))
        {
            return BossMessage.potionTauntMessages;
        }
        else if (BossMessage.FoodPrefabs.Contains(prefabName))
        {
            return BossMessage.foodTauntMessages;
        }
        else if (BossMessage.BerryPrefabs.Contains(prefabName))
        {
            return BossMessage.berryTauntMessages;
        }
        else if (BossMessage.MushroomPrefabs.Contains(prefabName))
        {
            return BossMessage.mushroomTauntMessages;
        }

        return BossMessage.healTauntMessages;
    }

    public static string GetItemCategory(ItemDrop.ItemData item)
    {
        string prefabName = GetRealPrefabName(item);
        if (string.IsNullOrEmpty(prefabName)) return "unknown";

        if (BossMessage.PotionPrefabs.Contains(prefabName)) return "potion";
        if (BossMessage.FoodPrefabs.Contains(prefabName)) return "food";
        if (BossMessage.BerryPrefabs.Contains(prefabName)) return "berry";
        if (BossMessage.MushroomPrefabs.Contains(prefabName)) return "mushroom";

        return "unknown";
    }
    public static string GetRealPrefabName(ItemDrop.ItemData item)
    {
        if (item?.m_dropPrefab != null)
            return item.m_dropPrefab.name;

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