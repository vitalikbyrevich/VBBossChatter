namespace VBBossChatter;

[HarmonyPatch]
public static class VB_BossMain
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.CustomFixedUpdate))]
    [HarmonyPostfix]
    public static void CheckAndUpdateBossTimers(Humanoid __instance)
    {
        if (!BossUtill.ShouldProcess(__instance)) return;

        var spawnPoint = __instance.GetBaseAI()?.m_spawnPoint ?? Vector3.zero;
        var currentTime = Time.time;
        var hasPlayerInRange = Player.IsPlayerInRange(__instance.transform.position, VBBossChatter.radiusConfig.Value);

        // Всегда регистрируем босса при обнаружении
        if (!BossUtill.bossDataDict.ContainsKey(spawnPoint))
        {
            BossUtill.bossDataDict[spawnPoint] = new BossUtill.BossData
            {
                LastPlayerSeenTime = currentTime,
                IsTimerRunning = !hasPlayerInRange, // запускаем таймер только если игрок не в радиусе
                BossRef = __instance
            };
        }

        if (BossUtill.bossDataDict.TryGetValue(spawnPoint, out var data))
        {
            if (hasPlayerInRange)
            {
                data.LastPlayerSeenTime = currentTime;
                data.IsTimerRunning = false;
                BossUtill.bossDataDict[spawnPoint] = data;
            }
            else
            {
                if (!data.IsTimerRunning)
                {
                    data.LastPlayerSeenTime = currentTime;
                    data.IsTimerRunning = true;
                    BossUtill.bossDataDict[spawnPoint] = data;
                    BossUtill.SendMessageInChatNormal(__instance, BossMessage.lostMessages, "#FFFF00"); //желтый
                    //  SendMessageAboveBoss(__instance, lostMessages);
                }
                else if (currentTime - data.LastPlayerSeenTime >= VBBossChatter.despawnDelayConfig.Value * 60f)
                {
                    BossUtill.DespawnBoss(__instance, spawnPoint);
                }
            }
        }

        BossUtill.CleanupDestroyedBosses();
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Destroy))]
    [HarmonyPrefix]
    public static void OnObjectDestroyed(GameObject go)
    {
        if (go.TryGetComponent<Humanoid>(out var humanoid) && humanoid.IsBoss())
        {
            var spawnPoint = humanoid.GetBaseAI()?.m_spawnPoint ?? Vector3.zero;
            if (spawnPoint != Vector3.zero) BossUtill.bossesToRemove.Add(spawnPoint);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Awake))]
    public static class Boss_LOD_DisablePatch
    {
        private static void Postfix(Humanoid __instance)
        {
            if (!__instance.IsBoss()) return;

            foreach (var lod in __instance.GetComponentsInChildren<LODGroup>(true))
            {
                lod.enabled = false;
            }

            var znv = __instance.GetComponent<ZNetView>();
            if (znv)
            {
                znv.m_distant = false;
                znv.m_type = ZDO.ObjectType.Prioritized;
            }
        }
    }
}