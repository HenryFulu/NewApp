using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace MilitaryRPG.Units
{
    [RequireComponent(typeof(NavMeshAgent))]
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
        
        // コンポーネント
        private NavMeshAgent navAgent;
        private Animator animator;
        
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
            navAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
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
                navAgent.speed = classData.baseSpeed;
                
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
                    if (navAgent.remainingDistance < 0.5f)
                    {
                        currentState = UnitState.Idle;
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
        
        private void FindTarget()
        {
            // 敵を探す（簡単な実装）
            Collider[] enemies = Physics.OverlapSphere(transform.position, classData.attackRange * 2);
            
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
                float distance = Vector3.Distance(transform.position, target.position);
                
                if (distance <= classData.attackRange)
                {
                    currentState = UnitState.Attacking;
                }
                else
                {
                    navAgent.SetDestination(target.position);
                    currentState = UnitState.Moving;
                }
            }
        }
        
        private void AttackTarget()
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                // 攻撃範囲チェック
                float distance = Vector3.Distance(transform.position, target.position);
                
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
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            isAlive = false;
            currentState = UnitState.Dead;
            navAgent.enabled = false;
            
            // 死亡エフェクト
            if (animator != null)
                animator.SetTrigger("Die");
                
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
            // 矢や投射物のエフェクト（簡単な実装）
            Debug.Log($"{unitName} が矢を放ちました！");
        }
        
        private void CreateMagicEffect(Unit targetUnit)
        {
            // 魔法エフェクト（簡単な実装）
            Debug.Log($"{unitName} が魔法を唱えました！");
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // アニメーションパラメータの更新
            animator.SetFloat("Speed", navAgent.velocity.magnitude);
            animator.SetBool("IsInCombat", isInCombat);
            animator.SetBool("IsAlive", isAlive);
        }
        
        // 回復スキル（僧侶用）
        public void HealAllies()
        {
            if (!classData.canHeal || currentMana < 30) return;
            
            Collider[] allies = Physics.OverlapSphere(transform.position, classData.attackRange);
            
            foreach (var ally in allies)
            {
                Unit allyUnit = ally.GetComponent<Unit>();
                if (allyUnit != null && allyUnit.squadID == squadID && allyUnit.isAlive)
                {
                    int healAmount = classData.baseAttack / 2;
                    allyUnit.currentHealth = Mathf.Min(allyUnit.classData.baseHealth, 
                                                     allyUnit.currentHealth + healAmount);
                    
                    Debug.Log($"{unitName} が {allyUnit.unitName} を {healAmount} 回復しました！");
                }
            }
            
            currentMana -= 30;
        }
        
        // 挑発スキル（戦士用）
        public void Taunt()
        {
            if (!classData.hasTaunt) return;
            
            Collider[] enemies = Physics.OverlapSphere(transform.position, classData.attackRange * 1.5f);
            
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
    }
}