using System.Collections;
using System.Linq;

namespace VBBossChatter;

public class VB_BossDodge : MonoBehaviour
{
    private MonsterAI m_ai;
    private Humanoid m_humanoid;
    private ZNetView m_nview;
    private Animator m_animator;
    
    [Header("Dodge Settings")]
    public float m_dodgeChance = 0.7f;
    public float m_dodgeCooldown = 3f;
    public float m_teleportDistance = 5f;
    
    [Header("Effects")]
    public string m_teleportEffectPrefab = "fx_eikthyr_stomp"; // Имя префаба эффекта
    
    private float m_lastDodgeTime;
    private bool m_isDodging;
    
    void Awake()
    {
        m_ai = GetComponent<MonsterAI>();
        m_humanoid = GetComponent<Humanoid>();
        m_nview = GetComponent<ZNetView>();
        m_animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        if (m_humanoid.IsDead()) return;
        
        // Проверяем атаки игрока (и для ближнего оружия и для лука)
        CheckPlayerAttack();
    }
    
    void CheckPlayerAttack()
    {
        if (m_ai.m_targetCreature == null) return;
        
        Player player = m_ai.m_targetCreature as Player;
        if (!player) return;
        
        // Проверяем атакует ли игрок (любым оружием)
        if (player.InAttack() && IsPlayerInRange(player))
        {
            TryDodge(player);
        }
    }
    
    public void TryDodge(Player player)
    {
        if (!CanDodge()) return;
        
        if (Random.value <= m_dodgeChance)
        {
            ExecuteTeleportDodge(player);
        }
    }
    
    void ExecuteTeleportDodge(Player player)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        
        m_lastDodgeTime = Time.time;
        m_isDodging = true;
        
        // Эффект перед телепортацией
        CreateTeleportEffect(transform.position);
        
        // Находим безопасную позицию для телепортации
        Vector3 teleportPosition = FindTeleportPosition(player);
        
        // Телепортируем босса
        transform.position = teleportPosition;
        
        // Эффект после телепортации
        CreateTeleportEffect(transform.position);
        
        Debug.Log($"{m_humanoid.m_name} телепортируется для уклонения!");
        
        // Завершаем уклонение
        StartCoroutine(EndDodgeAfterTime(0.5f));
    }
    
    Vector3 FindTeleportPosition(Player player)
    {
        Vector3 playerPosition = player.transform.position;
        Vector3 currentPosition = transform.position;
        
        // Определяем возможные направления для телепортации
        Vector3[] directions = {
            Vector3.right,
            Vector3.left, 
            Vector3.forward,
            Vector3.back,
            Vector3.right + Vector3.forward,
            Vector3.right + Vector3.back,
            Vector3.left + Vector3.forward,
            Vector3.left + Vector3.back
        };
        
        // Пробуем разные направления пока не найдем безопасное
        foreach (Vector3 direction in directions.OrderBy(x => Random.value))
        {
            Vector3 potentialPosition = currentPosition + direction.normalized * m_teleportDistance;
            
            // Проверяем что позиция безопасна
            if (IsPositionSafe(potentialPosition, playerPosition))
            {
                return potentialPosition;
            }
        }
        
        // Если не нашли безопасную позицию, телепортируемся назад от игрока
        Vector3 awayFromPlayer = (currentPosition - playerPosition).normalized;
        return currentPosition + awayFromPlayer * m_teleportDistance;
    }
    
    bool IsPositionSafe(Vector3 position, Vector3 playerPosition)
    {
        // Проверяем дистанцию до игрока
        float distanceToPlayer = Vector3.Distance(position, playerPosition);
        if (distanceToPlayer < 2f) return false; // Слишком близко к игроку
        
        // Проверяем что позиция на земле и нет препятствий
        if (!IsGrounded(position)) return false;
        
        // Проверяем что нет других существ на этой позиции
        if (Physics.CheckSphere(position, 1f, LayerMask.GetMask("character", "item"))) 
            return false;
            
        return true;
    }
    
    bool IsGrounded(Vector3 position)
    {
        // Проверяем что позиция на земле
        return Physics.Raycast(position + Vector3.up * 2f, Vector3.down, 3f, LayerMask.GetMask("terrain", "default"));
    }
    
    void CreateTeleportEffect(Vector3 position)
    {
        if (ZNetScene.instance == null) return;
        
        // Получаем префаб эффекта по имени
        GameObject effectPrefab = ZNetScene.instance.GetPrefab(m_teleportEffectPrefab);
        if (effectPrefab != null)
        {
            // Создаем эффект
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, 4f); // Уничтожаем через 4 секунды
        }
        else
        {
            Debug.LogWarning($"Не найден префаб эффекта: {m_teleportEffectPrefab}");
        }
    }
    
    IEnumerator EndDodgeAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        m_isDodging = false;
    }
    
    bool CanDodge()
    {
        if (Time.time - m_lastDodgeTime < m_dodgeCooldown) return false;
        if (m_isDodging) return false;
        if (m_humanoid.InAttack() || m_humanoid.IsStaggering()) return false;
        if (m_humanoid.IsKnockedBack()) return false;
        if (m_humanoid.IsDead()) return false;
        
        return true;
    }
    
    bool IsPlayerInRange(Player player)
    {
        float distance = Vector3.Distance(transform.position, player.transform.position);
        
        // Для лука проверяем большую дистанцию
        ItemDrop.ItemData weapon = player.GetCurrentWeapon();
        bool isRangedWeapon = weapon != null && weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
        
        if (isRangedWeapon)
        {
            return distance <= 15f; // Дистанция для лука
        }
        else
        {
            return distance <= 6f; // Дистанция для ближнего оружия
        }
    }
    
    // Public method to check if boss is currently dodging
    public bool IsDodging()
    {
        return m_isDodging;
    }
}