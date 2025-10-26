namespace VBBossChatter;

[HarmonyPatch]
public static class VB_BossDodgePatches
{
    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    public static class AddDodgeComponentPatch
    {
        private static void Postfix(Character __instance)
        {
            if (!__instance.IsBoss()) return;
            
            var humanoid = __instance as Humanoid;
            if (humanoid && !humanoid.GetComponent<VB_BossDodge>())
            {
                humanoid.gameObject.AddComponent<VB_BossDodge>();
                Debug.Log($"Добавлен компонент телепорт-уклонения для босса: {humanoid.m_name}");
            }
        }
    }

    // Патч для уменьшения урона при уклонении
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    public static class BossDodgeDamageReductionPatch
    {
        private static void Prefix(Character __instance, ref HitData hit)
        {
            if (!__instance.IsBoss()) return;
            
            var dodgeComponent = __instance.GetComponent<VB_BossDodge>();
            if (dodgeComponent != null && dodgeComponent.IsDodging())
            {
                // Уменьшаем урон если босс уклоняется (телепортируется)
                float originalDamage = hit.GetTotalDamage();
                hit.m_damage.m_damage *= 0.1f;
                hit.m_damage.m_blunt *= 0.1f;
                hit.m_damage.m_slash *= 0.1f;
                hit.m_damage.m_pierce *= 0.1f;
                hit.m_damage.m_fire *= 0.1f;
                hit.m_damage.m_frost *= 0.1f;
                hit.m_damage.m_lightning *= 0.1f;
                hit.m_damage.m_poison *= 0.1f;
                hit.m_damage.m_spirit *= 0.1f;
                
                Debug.Log($"Босс {__instance.m_name} телепортировался и уменьшил урон с {originalDamage} до {hit.GetTotalDamage()}!");
            }
        }
    }

    // Упрощенный патч для реакции на лук - через отслеживание анимации стрельбы
    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class BossDodgeOnBowAttack
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance.InAttack()) return;
            
            ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
            bool isRangedWeapon = weapon != null && weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
            
            if (!isRangedWeapon) return;
            
            // Проверяем всех боссов в радиусе
            var allBosses = BossUtill.GetAllAvailableBosses();
            foreach (var boss in allBosses)
            {
                if (boss == null) continue;
                
                float distance = Vector3.Distance(__instance.transform.position, boss.transform.position);
                if (distance <= 15f && IsPlayerFacingBoss(__instance, boss))
                {
                    var dodgeComponent = boss.GetComponent<VB_BossDodge>();
                    if (dodgeComponent != null && !dodgeComponent.IsDodging())
                    {
                        // Шанс уклонения от выстрела из лука
                        if (Random.value < 0.4f) // 40% шанс
                        {
                            dodgeComponent.TryDodge(__instance);
                        }
                    }
                }
            }
        }

        private static bool IsPlayerFacingBoss(Player player, Humanoid boss)
        {
            Vector3 toBoss = (boss.transform.position - player.transform.position).normalized;
            return Vector3.Dot(toBoss, player.GetLookDir()) > 0.3f;
        }
    }

    // Дополнительный патч для отслеживания выпущенных стрел
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Awake))]
    public static class BossDodgeOnProjectileAwake
    {
        private static void Postfix(Projectile __instance)
        {
            // Добавляем компонент для отслеживания стрел
            if (!__instance.GetComponent<VB_ProjectileTracker>())
            {
                __instance.gameObject.AddComponent<VB_ProjectileTracker>();
            }
        }
    }
}
public class VB_ProjectileTracker : MonoBehaviour
{
    private Projectile m_projectile;
    private ZNetView m_nview;
    private bool m_hasTriggeredDodge = false;
    
    void Awake()
    {
        m_projectile = GetComponent<Projectile>();
        m_nview = GetComponent<ZNetView>();
    }
    
    void Update()
    {
        if (!m_nview.IsValid()) return;
        if (m_hasTriggeredDodge) return;
        if (m_projectile == null) return;
        
        // Проверяем близость к боссам
        var allBosses = BossUtill.GetAllAvailableBosses();
        foreach (var boss in allBosses)
        {
            if (boss == null) continue;
            
            float distance = Vector3.Distance(transform.position, boss.transform.position);
            if (distance <= 8f) // Босс видит летящую стрелу
            {
                var dodgeComponent = boss.GetComponent<VB_BossDodge>();
                if (dodgeComponent != null && !dodgeComponent.IsDodging())
                {
                    // Шанс уклонения от стрелы
                    if (Random.value < 0.3f) // 30% шанс
                    {
                        Character owner = m_projectile.m_owner;
                        if (owner != null && owner.IsPlayer())
                        {
                            dodgeComponent.TryDodge(owner as Player);
                            m_hasTriggeredDodge = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}