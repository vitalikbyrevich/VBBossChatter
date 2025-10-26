namespace VBBossChatter;

[HarmonyPatch]
public class VB_BossTaunts
{
    private static readonly Dictionary<(string bossName, string playerName), int> bossKillStats = new();
    private static readonly Dictionary<Player, (Humanoid boss, float time)> lastBossDamage = new();
    private static readonly Dictionary<(string bossName, string playerName), float> lastTauntTime = new();
    internal static int tauntDeathThreshold = 3;

    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateAI))]
    public static class BossTaunt_UpdateAIPatch
    {
        private static readonly Dictionary<Humanoid, float> bossLastHealth = new();
        private static readonly Dictionary<Humanoid, float> lastDamageTauntTime = new();
        private static readonly Dictionary<(Humanoid, Player), bool> lastTargetState = new();

        private static void Postfix(MonsterAI __instance, float dt)
        {
            if (!__instance.m_character.IsBoss()) return;

            var boss = __instance.m_character as Humanoid;
            if (!boss) return;

            float currentTime = Time.time;

            // НОВОЕ: Насмешка при взятии в цель
            var target = __instance.m_targetCreature as Player;
            if (target && target.IsPlayer())
            {
                var key = (boss, target);
                bool isNowTarget = __instance.m_targetCreature == target;

                if (!lastTargetState.ContainsKey(key))
                {
                    // Первое обнаружение - считаем что это новая цель
                    lastTargetState[key] = isNowTarget;
                    if (isNowTarget)
                    {
                        TriggerAggroTaunt(boss, target);
                    }
                }
                else
                {
                    bool wasTargeting = lastTargetState[key];
                    if (isNowTarget && !wasTargeting)
                    {
                        // Стал новой целью после перерыва
                        TriggerAggroTaunt(boss, target);
                    }

                    lastTargetState[key] = isNowTarget;
                }

                // Существующая логика отслеживания урона боссу
                // Существующая логика отслеживания урона боссу
                float currentHealth = boss.GetHealth();
                if (bossLastHealth.TryGetValue(boss, out float lastHealth))
                {
                    float healthDiff = lastHealth - currentHealth;
                    if (healthDiff > 10f)
                    {
                        float lastTauntTime = lastDamageTauntTime.ContainsKey(boss) ? lastDamageTauntTime[boss] : 0f;
                        if (currentTime - lastTauntTime > 25f && Random.value < 0.5f)
                        {
                            // Получаем индивидуальные сообщения для босса
                            string bossPrefabName = BossUtill.GetBossPrefabName(boss);
                            string[] damageMessages = BossMessage.GetDamageTauntMessages(bossPrefabName);
                            string msg = damageMessages[Random.Range(0, damageMessages.Length)];

                            BossUtill.SendMessageAsRaven(boss, new[] { msg }, "#FF4500");
                            lastDamageTauntTime[boss] = currentTime;
                        }
                    }
                }

                bossLastHealth[boss] = currentHealth;
            }

            // Очистка старых записей о целях
            if (currentTime % 60f < dt) // Раз в минуту
            {
                var keysToRemove = lastTargetState.Where(x => currentTime - (x.Value ? 0 : 300f) > 300f).Select(x => x.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    lastTargetState.Remove(key);
                }
            }
        }

        private static void TriggerAggroTaunt(Humanoid boss, Player target)
        {
            string bossName = Localization.instance.Localize(boss.m_name);
            string playerName = target.GetPlayerName();

            // Получаем префаб имя босса для индивидуальных сообщений
            string bossPrefabName = BossUtill.GetBossPrefabName(boss);

            // Всегда срабатывает, но с разными сообщениями в зависимости от статистики
            string msg;

            if (bossKillStats.TryGetValue((bossName, playerName), out int deaths) && deaths >= tauntDeathThreshold)
            {
                // Особые насмешки для "любимых жертв" (3+ смертей)
                if (Random.value < 0.15f)
                {
                    string[] rareMessages = BossMessage.GetRareTauntMessages(bossPrefabName);
                    msg = rareMessages[Random.Range(0, rareMessages.Length)];
                }
                else
                {
                    string[] tauntMessages = BossMessage.GetTauntMessages(bossPrefabName);
                    msg = tauntMessages[Random.Range(0, tauntMessages.Length)];
                }
            }
            else
            {
                // Обычные насмешки для всех игроков
                string[] aggroMessages = BossMessage.GetAggroTauntMessages(bossPrefabName);
                msg = aggroMessages[Random.Range(0, aggroMessages.Length)];
            }

            msg = msg.Replace("{player}", playerName);
            BossUtill.SendMessageAsRaven(boss, new[] { msg }, "#00FF00");
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDamaged))]
    public static class BossDamageTracker
    {
        private static void Postfix(Player __instance, HitData hit)
        {
            if (!__instance || hit == null) return;

            var attacker = hit.GetAttacker();
            if (attacker && attacker.IsBoss())
            {
                // Записываем урон от босса (это уже работает)
                lastBossDamage[__instance] = (attacker as Humanoid, Time.time);

                // Насмешка при блокировании - проверяем через IsBlocking()
                // Насмешка при блокировании - проверяем через IsBlocking()
                if (__instance.IsBlocking() && Random.value < 0.8f)
                {
                    string bossName = Localization.instance.Localize((attacker as Humanoid).m_name);
                    string playerName = __instance.GetPlayerName();
                    var key = (bossName, playerName);

                    // Получаем префаб имя босса
                    string bossPrefabName = BossUtill.GetBossPrefabName(attacker as Humanoid);
                    string[] blockMessages = BossMessage.GetBlockTauntMessages(bossPrefabName);

                    // Проверяем счетчик смертей для особых насмешек
                    if (bossKillStats.TryGetValue(key, out int deaths) && deaths >= tauntDeathThreshold)
                    {
                        string msg = blockMessages[Random.Range(0, blockMessages.Length)];
                        msg = msg.Replace("{player}", playerName);
                        BossUtill.SendMessageAsRaven(attacker as Humanoid, new[] { msg }, "#FFA500");
                    }
                    else
                    {
                        // Обычная насмешка при блоке
                        string msg = blockMessages[Random.Range(0, blockMessages.Length)];
                        BossUtill.SendMessageAsRaven(attacker as Humanoid, new[] { msg }, "#FFA500");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    public static class BossHealUseItemPatch
    {
        private static readonly Dictionary<Player, (string itemName, int initialCount)> pendingHealChecks = new();

       private static void Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
{
    if (!__instance.IsPlayer()) return;
    var player = __instance as Player;
    if (!player || item == null) return;

    // Используем "default" для проверки предметов
    bool isHealingItem = BossUtill.IsHealingItem(item, "default");
    if (!isHealingItem) return;

    var playerInventory = player.GetInventory();
    if (playerInventory == null) return;

    string itemPrefabName = BossUtill.GetRealPrefabName(item);
    int initialCount = BossUtill.GetItemCountInInventory(playerInventory, itemPrefabName);
    pendingHealChecks[player] = (itemPrefabName, initialCount);
}

private static void Postfix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
{
    if (!__instance.IsPlayer()) return;
    var player = __instance as Player;
    if (!player || item == null) return;

    string itemPrefabName = BossUtill.GetRealPrefabName(item);
    if (!pendingHealChecks.TryGetValue(player, out var pending) || pending.itemName != itemPrefabName) 
        return;

    var playerInventory = player.GetInventory();
    if (playerInventory == null)
    {
        pendingHealChecks.Remove(player);
        return;
    }

    int currentCount = BossUtill.GetItemCountInInventory(playerInventory, itemPrefabName);
    bool itemWasUsed = currentCount < pending.initialCount;

    if (itemWasUsed)
    {
        var selectedBoss = SelectBossForHealTaunt(player);
        
        if (selectedBoss != null)
        {
            // Получаем префаб имя выбранного босса
            string bossPrefabName = BossUtill.GetBossPrefabName(selectedBoss);
            
            // Используем индивидуальные сообщения для этого босса
            string[] messages = BossUtill.GetHealTauntMessages(item, bossPrefabName);
            string msg = messages.Length > 0 ? messages[Random.Range(0, messages.Length)] : "Лечи свои раны!";
            string category = BossUtill.GetItemCategory(item, bossPrefabName);
            
            BossUtill.SendMessageAsRaven(selectedBoss, new[] { msg }, "#FF69B4");
            Debug.Log($"Насмешка при использовании {category}: {itemPrefabName}");
        }
    }

    pendingHealChecks.Remove(player);
}

        private static Humanoid SelectBossForHealTaunt(Player player)
        {
            var allBosses = BossUtill.GetAllAvailableBosses();
            if (allBosses.Count == 0) return null;

            // Приоритет 1: Босс, который атакует игрока
            /*   var attackingBoss = BossUtill.FindBossWithPlayerTarget(player);
               if (attackingBoss != null) return attackingBoss;*/

            // Приоритет 2: Ближайший босс
            var nearestBoss = BossUtill.FindNearestBossToPlayer(player);
            if (nearestBoss != null) return nearestBoss;

            // Приоритет 3: Случайный босс
            return BossUtill.FindRandomAvailableBoss();
        }
    }

// Также очищаем при смерти игрока
    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class BossKill_MessagePatch
    {
        private static void Prefix(Player __instance)
        {
            if (!Chat.instance || !__instance) return;

            // Проверяем, был ли урон от босса в последние 10 секунд
            if (lastBossDamage.TryGetValue(__instance, out var damageInfo) && Time.time - damageInfo.time < 10f)
            {
                var boss = damageInfo.boss;
                if (boss && boss.IsBoss())
                {
                    string bossName = Localization.instance.Localize(boss.m_name);
                    string playerName = __instance.GetPlayerName();
                    var key = (bossName, playerName);

                    // увеличиваем счётчик убийств
                    if (bossKillStats.ContainsKey(key)) bossKillStats[key]++;
                    else bossKillStats[key] = 1;

                    // Только сообщение об убийстве
                    string bossPrefabName = BossUtill.GetBossPrefabName(__instance);
                    string[] messages = BossMessage.GetKillMessages(bossPrefabName);
                    BossUtill.SendMessageAsRaven(__instance, messages, "#FFFF00");

                    // СБРАСЫВАЕМ таймер насмешки для этой пары босс-игрок
                    lastTauntTime.Remove(key);

                    // Очищаем запись об уроне
                    lastBossDamage.Remove(__instance);
                }
            }
        }
    }

    // Очищаем таймеры когда босс теряет цель
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.SetTarget))]
    public static class BossTaunt_ClearPatch
    {
        private static void Postfix(MonsterAI __instance, Character attacker)
        {
            if (!__instance.m_character.IsBoss()) return;

            // Если босс теряет цель или меняет на другую
            if (!attacker || !attacker.IsPlayer())
            {
                var boss = __instance.m_character as Humanoid;
                if (boss)
                {
                    string bossName = Localization.instance.Localize(boss.m_name);

                    // Используем LINQ для поиска ключей
                    var keysToRemove = lastTauntTime.Keys.Where(key => key.bossName == bossName).ToList();
                    foreach (var key in keysToRemove)
                    {
                        lastTauntTime.Remove(key);
                    }
                }
            }
        }
    }
}