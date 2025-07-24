using UnityEngine;
using System.Collections;

namespace MilitaryRPG.Units
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Unit : MonoBehaviour
    {
        [Header("ユニット情報")]
        public int unitID;
        public string unitName;
        public UnitClassData classData;
        
        [Header("現在のステータス")]
        public int currentHealth;
        public int currentMana;
        public bool isAlive = true;
        public bool isInCombat = false;
        
        [Header("戦闘関連")]
        public Transform target;
        public float lastAttackTime;
        public float attackCooldown = 1.5f;
        
        [Header("小隊情報")]
        public int squadID = -1;
        public bool isSquadLeader = false;
        
        [Header("2D移動設定")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 180f;
        public float arrivalDistance = 0.5f;
        
        // コンポーネント
        private Rigidbody2D rb2d;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        
        // 移動関連
        private Vector2 targetPosition;
        private bool hasDestination = false;
        
        // AI状態
        public enum UnitState
        {
            Idle,           // 待機
            Moving,         // 移動中
            Attacking,      // 攻撃中
            Casting,        // 魔法詠唱中
            Healing,        // 回復中
            Dead           // 死亡
        }
        
        public UnitState currentState = UnitState.Idle;
        
        private void Awake()
        {
            rb2d = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            
            // 2D物理設定
            rb2d.gravityScale = 0f; // 2Dトップダウンなので重力無効
            rb2d.freezeRotation = true; // 回転を制御
        }
        
        private void Start()
        {
            InitializeUnit();
        }
        
        public void InitializeUnit()
        {
            if (classData != null)
            {
                // ステータスを初期化
                currentHealth = classData.baseHealth;
                currentMana = classData.baseMana;
                moveSpeed = classData.baseSpeed;
                
                // ユニット名を設定
                if (string.IsNullOrEmpty(unitName))
                    unitName = $"{classData.classNameJP}_{unitID:000}";
                    
                Debug.Log($"{unitName} が配備されました！");
            }
        }
        
        private void Update()
        {
            if (!isAlive) return;
            
            UpdateAI();
            UpdateMovement();
            UpdateAnimations();
        }
        
        private void UpdateAI()
        {
            switch (currentState)
            {
                case UnitState.Idle:
                    FindTarget();
                    break;
                    
                case UnitState.Moving:
                    if (!hasDestination || Vector2.Distance(transform.position, targetPosition) < arrivalDistance)
                    {
                        currentState = UnitState.Idle;
                        hasDestination = false;
                        rb2d.velocity = Vector2.zero;
                    }
                    break;
                    
                case UnitState.Attacking:
                    if (target != null)
                    {
                        AttackTarget();
                    }
                    else
                    {
                        currentState = UnitState.Idle;
                    }
                    break;
            }
        }
        
        private void UpdateMovement()
        {
            if (currentState == UnitState.Moving && hasDestination)
            {
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                rb2d.velocity = direction * moveSpeed;
                
                // 向きを調整
                if (direction != Vector2.zero)
                {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Lerp(transform.rotation, 
                        Quaternion.AngleAxis(angle - 90f, Vector3.forward), 
                        rotationSpeed * Time.deltaTime);
                }
            }
        }
        
        public void SetDestination(Vector2 destination)
        {
            targetPosition = destination;
            hasDestination = true;
            currentState = UnitState.Moving;
        }
        
        private void FindTarget()
        {
            // 敵を探す（2D版）
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, classData.attackRange * 2);
            
            foreach (var enemy in enemies)
            {
                Unit enemyUnit = enemy.GetComponent<Unit>();
                if (enemyUnit != null && enemyUnit.squadID != squadID && enemyUnit.isAlive)
                {
                    target = enemyUnit.transform;
                    MoveToTarget();
                    break;
                }
            }
        }
        
        private void MoveToTarget()
        {
            if (target != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                
                if (distance <= classData.attackRange)
                {
                    currentState = UnitState.Attacking;
                    rb2d.velocity = Vector2.zero;
                }
                else
                {
                    SetDestination(target.position);
                }
            }
        }
        
        private void AttackTarget()
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                // 攻撃範囲チェック
                float distance = Vector2.Distance(transform.position, target.position);
                
                if (distance <= classData.attackRange)
                {
                    PerformAttack();
                    lastAttackTime = Time.time;
                }
                else
                {
                    MoveToTarget();
                }
            }
        }
        
        private void PerformAttack()
        {
            if (target == null) return;
            
            Unit targetUnit = target.GetComponent<Unit>();
            if (targetUnit != null && targetUnit.isAlive)
            {
                // ダメージ計算
                int damage = Mathf.Max(1, classData.baseAttack - targetUnit.classData.baseDefense);
                
                // 特殊攻撃の処理
                if (classData.canRangedAttack)
                {
                    // 遠距離攻撃
                    CreateProjectile(targetUnit);
                }
                else if (classData.canCastMagic && currentMana >= 20)
                {
                    // 魔法攻撃
                    damage = (int)(damage * 1.5f);
                    currentMana -= 20;
                    CreateMagicEffect(targetUnit);
                }
                
                // ダメージを与える
                targetUnit.TakeDamage(damage);
                
                Debug.Log($"{unitName} が {targetUnit.unitName} に {damage} ダメージを与えました！");
            }
        }
        
        public void TakeDamage(int damage)
        {
            if (!isAlive) return;
            
            currentHealth -= damage;
            
            // ダメージエフェクト（色の点滅）
            StartCoroutine(DamageFlash());
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private IEnumerator DamageFlash()
        {
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = originalColor;
            }
        }
        
        private void Die()
        {
            isAlive = false;
            currentState = UnitState.Dead;
            rb2d.velocity = Vector2.zero;
            
            // 死亡エフェクト
            if (animator != null)
                animator.SetTrigger("Die");
            
            // 透明度を下げる
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 0.3f;
                spriteRenderer.color = color;
            }
                
            Debug.Log($"{unitName} が戦死しました...");
            
            // 一定時間後にオブジェクトを削除
            StartCoroutine(RemoveAfterDelay(3f));
        }
        
        private IEnumerator RemoveAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
        
        private void CreateProjectile(Unit targetUnit)
        {
            // 矢や投射物のエフェクト（2D版）
            Debug.Log($"{unitName} が矢を放ちました！");
            
            // 簡単な弾道エフェクト
            StartCoroutine(ProjectileEffect(targetUnit.transform.position));
        }
        
        private IEnumerator ProjectileEffect(Vector2 targetPos)
        {
            // 簡単な線描画でプロジェクタイルを表現
            LineRenderer line = GetComponent<LineRenderer>();
            if (line == null)
            {
                line = gameObject.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.color = Color.yellow;
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.sortingOrder = 1;
            }
            
            line.positionCount = 2;
            line.SetPosition(0, transform.position);
            line.SetPosition(1, targetPos);
            
            yield return new WaitForSeconds(0.1f);
            
            line.positionCount = 0;
        }
        
        private void CreateMagicEffect(Unit targetUnit)
        {
            // 魔法エフェクト（2D版）
            Debug.Log($"{unitName} が魔法を唱えました！");
            
            // 簡単なパーティクルエフェクト
            StartCoroutine(MagicEffect());
        }
        
        private IEnumerator MagicEffect()
        {
            // 魔法エフェクト用の一時的なオブジェクト作成
            GameObject effect = new GameObject("MagicEffect");
            effect.transform.position = transform.position;
            
            SpriteRenderer effectRenderer = effect.AddComponent<SpriteRenderer>();
            effectRenderer.color = new Color(0, 0, 1, 0.5f);
            effectRenderer.sortingOrder = 2;
            
            // 拡大エフェクト
            float timer = 0f;
            float duration = 0.5f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float scale = Mathf.Lerp(0, 2f, timer / duration);
                effect.transform.localScale = Vector3.one * scale;
                
                Color color = effectRenderer.color;
                color.a = Mathf.Lerp(0.5f, 0f, timer / duration);
                effectRenderer.color = color;
                
                yield return null;
            }
            
            Destroy(effect);
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // アニメーションパラメータの更新
            animator.SetFloat("Speed", rb2d.velocity.magnitude);
            animator.SetBool("IsInCombat", isInCombat);
            animator.SetBool("IsAlive", isAlive);
        }
        
        // 回復スキル（僧侶用）
        public void HealAllies()
        {
            if (!classData.canHeal || currentMana < 30) return;
            
            Collider2D[] allies = Physics2D.OverlapCircleAll(transform.position, classData.attackRange);
            
            foreach (var ally in allies)
            {
                Unit allyUnit = ally.GetComponent<Unit>();
                if (allyUnit != null && allyUnit.squadID == squadID && allyUnit.isAlive)
                {
                    int healAmount = classData.baseAttack / 2;
                    allyUnit.currentHealth = Mathf.Min(allyUnit.classData.baseHealth, 
                                                     allyUnit.currentHealth + healAmount);
                    
                    // 回復エフェクト
                    StartCoroutine(HealEffect(allyUnit));
                    
                    Debug.Log($"{unitName} が {allyUnit.unitName} を {healAmount} 回復しました！");
                }
            }
            
            currentMana -= 30;
        }
        
        private IEnumerator HealEffect(Unit healedUnit)
        {
            if (healedUnit.spriteRenderer != null)
            {
                Color originalColor = healedUnit.spriteRenderer.color;
                healedUnit.spriteRenderer.color = Color.green;
                yield return new WaitForSeconds(0.3f);
                healedUnit.spriteRenderer.color = originalColor;
            }
        }
        
        // 挑発スキル（戦士用）
        public void Taunt()
        {
            if (!classData.hasTaunt) return;
            
            Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, classData.attackRange * 1.5f);
            
            foreach (var enemy in enemies)
            {
                Unit enemyUnit = enemy.GetComponent<Unit>();
                if (enemyUnit != null && enemyUnit.squadID != squadID && enemyUnit.isAlive)
                {
                    enemyUnit.target = this.transform;
                    Debug.Log($"{unitName} が {enemyUnit.unitName} の注意を引きました！");
                }
            }
        }
        
        // デバッグ用：攻撃範囲を表示
        private void OnDrawGizmosSelected()
        {
            if (classData != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCircle(transform.position, classData.attackRange);
            }
        }
    }
}