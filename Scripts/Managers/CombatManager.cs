using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MilitaryRPG.Units;
using MilitaryRPG.Squads;

namespace MilitaryRPG.Core
{
    public class CombatManager : MonoBehaviour
    {
        [Header("2D戦闘設定")]
        public float battleRadius = 30f;
        public float battleUpdateInterval = 2f;
        public bool autoBattleMode = true;
        
        [Header("戦闘バランス")]
        public float combatIntensity = 1f;
        public float skillCooldown = 5f;
        
        private SquadManager squadManager;
        private UnitManager unitManager;
        private bool battleInProgress = false;
        private Coroutine battleCoroutine;
        
        private void Awake()
        {
            squadManager = GetComponent<SquadManager>();
            unitManager = GetComponent<UnitManager>();
            
            if (squadManager == null)
                squadManager = FindObjectOfType<SquadManager>();
            if (unitManager == null)
                unitManager = FindObjectOfType<UnitManager>();
        }
        
        public void StartBattle()
        {
            if (battleInProgress)
            {
                Debug.LogWarning("既に戦闘が進行中です！");
                return;
            }
            
            Debug.Log("🔥 2D戦闘開始！全軍、攻撃準備！");
            
            battleInProgress = true;
            
            // 敵軍を生成（簡単な実装として、既存のユニットの半分を敵にする）
            CreateEnemyForces();
            
            if (autoBattleMode)
            {
                battleCoroutine = StartCoroutine(AutoBattleLoop());
            }
            
            // 全ユニットの色を更新
            unitManager.SetUnitColors();
        }
        
        public void EndBattle()
        {
            if (!battleInProgress)
                return;
                
            battleInProgress = false;
            
            if (battleCoroutine != null)
            {
                StopCoroutine(battleCoroutine);
                battleCoroutine = null;
            }
            
            Debug.Log("⚔️ 2D戦闘終了！");
            
            // 戦闘結果を表示
            DisplayBattleResults();
            
            // 小隊を再編成
            squadManager.ReorganizeSquads();
        }
        
        private void CreateEnemyForces()
        {
            List<Unit> allUnits = unitManager.GetAliveUnits();
            int enemyCount = allUnits.Count / 2;
            
            // 後半のユニットを敵軍に変更
            for (int i = allUnits.Count - enemyCount; i < allUnits.Count; i++)
            {
                Unit unit = allUnits[i];
                
                // 敵軍の小隊IDを1000番台に変更
                unit.squadID += 1000;
                
                // 敵軍の色を設定（少し暗い色）
                SpriteRenderer spriteRenderer = unit.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    Color enemyColor = GetEnemyClassColor(unit.classData.unitType);
                    spriteRenderer.color = enemyColor;
                }
                
                // 敵軍を反対側に移動（2D版）
                Vector2 enemyPosition = new Vector2(
                    Random.Range(20f, 60f),
                    Random.Range(-30f, 30f)
                );
                
                unit.SetDestination(enemyPosition);
            }
            
            Debug.Log($"敵軍{enemyCount}体を生成しました！");
        }
        
        private Color GetEnemyClassColor(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Warrior:
                    return new Color(0.8f, 0f, 0f);    // 暗い赤
                case UnitType.Archer:
                    return new Color(0f, 0.6f, 0f);    // 暗い緑
                case UnitType.Mage:
                    return new Color(0f, 0f, 0.8f);    // 暗い青
                case UnitType.Priest:
                    return new Color(0.8f, 0.6f, 0f);  // 暗い黄
                default:
                    return Color.gray;
            }
        }
        
        private IEnumerator AutoBattleLoop()
        {
            while (battleInProgress)
            {
                yield return new WaitForSeconds(battleUpdateInterval);
                
                // 戦闘状況を更新
                UpdateBattleState();
                
                // 戦闘終了条件をチェック
                if (CheckBattleEndConditions())
                {
                    EndBattle();
                    yield break;
                }
                
                // スキル使用
                if (Random.value < 0.3f) // 30%の確率でスキル使用
                {
                    squadManager.OrderAllSquadsUseSkills();
                }
            }
        }
        
        private void UpdateBattleState()
        {
            List<Unit> friendlyUnits = GetFriendlyUnits();
            List<Unit> enemyUnits = GetEnemyUnits();
            
            // 各ユニットの戦闘AIを更新
            foreach (var unit in friendlyUnits)
            {
                if (unit.isAlive && !unit.isInCombat)
                {
                    Unit nearestEnemy = FindNearestEnemy(unit, enemyUnits);
                    if (nearestEnemy != null)
                    {
                        float distance = Vector2.Distance(unit.transform.position, nearestEnemy.transform.position);
                        if (distance <= battleRadius)
                        {
                            unit.target = nearestEnemy.transform;
                            unit.isInCombat = true;
                        }
                    }
                }
            }
            
            // 敵ユニットも同様に処理
            foreach (var unit in enemyUnits)
            {
                if (unit.isAlive && !unit.isInCombat)
                {
                    Unit nearestEnemy = FindNearestEnemy(unit, friendlyUnits);
                    if (nearestEnemy != null)
                    {
                        float distance = Vector2.Distance(unit.transform.position, nearestEnemy.transform.position);
                        if (distance <= battleRadius)
                        {
                            unit.target = nearestEnemy.transform;
                            unit.isInCombat = true;
                        }
                    }
                }
            }
        }
        
        private Unit FindNearestEnemy(Unit unit, List<Unit> enemies)
        {
            Unit nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var enemy in enemies)
            {
                if (!enemy.isAlive) continue;
                
                float distance = Vector2.Distance(unit.transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }
            
            return nearest;
        }
        
        private List<Unit> GetFriendlyUnits()
        {
            List<Unit> friendlyUnits = new List<Unit>();
            List<Unit> allUnits = unitManager.GetAliveUnits();
            
            foreach (var unit in allUnits)
            {
                if (unit.squadID < 1000) // 友軍は1000未満のID
                {
                    friendlyUnits.Add(unit);
                }
            }
            
            return friendlyUnits;
        }
        
        private List<Unit> GetEnemyUnits()
        {
            List<Unit> enemyUnits = new List<Unit>();
            List<Unit> allUnits = unitManager.GetAliveUnits();
            
            foreach (var unit in allUnits)
            {
                if (unit.squadID >= 1000) // 敵軍は1000以上のID
                {
                    enemyUnits.Add(unit);
                }
            }
            
            return enemyUnits;
        }
        
        private bool CheckBattleEndConditions()
        {
            List<Unit> friendlyUnits = GetFriendlyUnits();
            List<Unit> enemyUnits = GetEnemyUnits();
            
            int friendlyAlive = 0;
            int enemyAlive = 0;
            
            foreach (var unit in friendlyUnits)
            {
                if (unit.isAlive) friendlyAlive++;
            }
            
            foreach (var unit in enemyUnits)
            {
                if (unit.isAlive) enemyAlive++;
            }
            
            // どちらかの軍勢が全滅したら戦闘終了
            if (friendlyAlive == 0 || enemyAlive == 0)
            {
                return true;
            }
            
            // 戦闘が膠着状態になった場合の判定も可能
            return false;
        }
        
        private void DisplayBattleResults()
        {
            List<Unit> friendlyUnits = GetFriendlyUnits();
            List<Unit> enemyUnits = GetEnemyUnits();
            
            int friendlyAlive = 0;
            int friendlyDead = 0;
            int enemyAlive = 0;
            int enemyDead = 0;
            
            foreach (var unit in friendlyUnits)
            {
                if (unit.isAlive)
                    friendlyAlive++;
                else
                    friendlyDead++;
            }
            
            foreach (var unit in enemyUnits)
            {
                if (unit.isAlive)
                    enemyAlive++;
                else
                    enemyDead++;
            }
            
            Debug.Log("=== 2D戦闘結果 ===");
            Debug.Log($"味方軍: 生存 {friendlyAlive}, 戦死 {friendlyDead}");
            Debug.Log($"敵軍: 生存 {enemyAlive}, 戦死 {enemyDead}");
            
            if (friendlyAlive > enemyAlive)
            {
                Debug.Log("🎉 味方軍の勝利！");
            }
            else if (enemyAlive > friendlyAlive)
            {
                Debug.Log("💀 敵軍の勝利...");
            }
            else
            {
                Debug.Log("⚖️ 引き分け");
            }
            
            Debug.Log("===============");
        }
        
        // 戦術指令システム（2D版）
        public void OrderCharge()
        {
            if (!battleInProgress) return;
            
            Debug.Log("📯 全軍突撃命令！");
            
            List<Unit> friendlyUnits = GetFriendlyUnits();
            List<Unit> enemyUnits = GetEnemyUnits();
            
            if (enemyUnits.Count > 0)
            {
                // 敵の中心位置を計算（2D版）
                Vector2 enemyCenter = Vector2.zero;
                foreach (var enemy in enemyUnits)
                {
                    if (enemy.isAlive)
                        enemyCenter += (Vector2)enemy.transform.position;
                }
                enemyCenter /= enemyUnits.Count;
                
                // 友軍を敵の中心に向かわせる
                foreach (var unit in friendlyUnits)
                {
                    if (unit.isAlive)
                    {
                        Vector2 chargePosition = enemyCenter + Random.insideUnitCircle * 10f;
                        unit.SetDestination(chargePosition);
                    }
                }
            }
        }
        
        public void OrderRetreat()
        {
            if (!battleInProgress) return;
            
            Debug.Log("🚩 全軍後退命令！");
            
            List<Unit> friendlyUnits = GetFriendlyUnits();
            
            foreach (var unit in friendlyUnits)
            {
                if (unit.isAlive)
                {
                    // 後方に移動（2D版）
                    Vector2 retreatPosition = new Vector2(
                        Random.Range(-30f, -10f),
                        Random.Range(-20f, 20f)
                    );
                    unit.SetDestination(retreatPosition);
                    
                    unit.isInCombat = false;
                    unit.target = null;
                }
            }
        }
        
        // 画面内で戦闘を開始
        public void StartBattleOnScreen()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            // まず全ユニットを画面内に配置
            unitManager.RepositionUnitsToScreen();
            
            // 少し待ってから戦闘開始
            StartCoroutine(DelayedBattleStart());
        }
        
        private IEnumerator DelayedBattleStart()
        {
            yield return new WaitForSeconds(1f);
            StartBattle();
        }
        
        private void Update()
        {
            // デバッグ用のキー入力
            if (Input.GetKeyDown(KeyCode.B))
            {
                if (!battleInProgress)
                    StartBattleOnScreen();
                else
                    EndBattle();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                OrderCharge();
            }
            
            if (Input.GetKeyDown(KeyCode.Y))
            {
                OrderRetreat();
            }
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                DisplayBattleResults();
            }
        }
    }
}