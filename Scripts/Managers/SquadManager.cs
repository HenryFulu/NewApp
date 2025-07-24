using System.Collections.Generic;
using UnityEngine;
using MilitaryRPG.Units;
using MilitaryRPG.Squads;

namespace MilitaryRPG.Core
{
    public class SquadManager : MonoBehaviour
    {
        [Header("小隊編成設定")]
        public int maxSquadSize = 10;
        public bool balancedSquads = true; // 職業をバランスよく配置
        
        [Header("2Dフォーメーション設定")]
        public Squad.SquadFormation defaultFormation = Squad.SquadFormation.Line;
        public float squadSpacing = 20f;
        
        private List<Squad> allSquads = new List<Squad>();
        private UnitManager unitManager;
        private int nextSquadID = 1;
        
        private void Awake()
        {
            unitManager = GetComponent<UnitManager>();
            if (unitManager == null)
            {
                unitManager = FindObjectOfType<UnitManager>();
            }
        }
        
        public void OrganizeSquads()
        {
            List<Unit> availableUnits = unitManager.GetAliveUnits();
            
            if (availableUnits.Count == 0)
            {
                Debug.LogWarning("編成可能なユニットがありません！");
                return;
            }
            
            allSquads.Clear();
            
            if (balancedSquads)
            {
                OrganizeBalancedSquads(availableUnits);
            }
            else
            {
                OrganizeSimpleSquads(availableUnits);
            }
            
            PositionSquads();
            
            Debug.Log($"2D小隊編成完了！{allSquads.Count}個の小隊を編成しました。");
            
            // 各小隊の状況を報告
            foreach (var squad in allSquads)
            {
                squad.ReportStatus();
            }
        }
        
        private void OrganizeBalancedSquads(List<Unit> units)
        {
            // 職業別にユニットを分類
            Dictionary<UnitType, List<Unit>> unitsByType = new Dictionary<UnitType, List<Unit>>();
            
            foreach (UnitType type in System.Enum.GetValues(typeof(UnitType)))
            {
                unitsByType[type] = new List<Unit>();
            }
            
            foreach (var unit in units)
            {
                unitsByType[unit.classData.unitType].Add(unit);
            }
            
            // 必要な小隊数を計算
            int squadCount = Mathf.CeilToInt(units.Count / (float)maxSquadSize);
            
            // 小隊を作成
            for (int i = 0; i < squadCount; i++)
            {
                Squad newSquad = new Squad(nextSquadID++, $"第{i + 1}小隊");
                newSquad.formation = defaultFormation;
                allSquads.Add(newSquad);
            }
            
            // 各職業のユニットを均等に分配
            foreach (var kvp in unitsByType)
            {
                UnitType unitType = kvp.Key;
                List<Unit> typeUnits = kvp.Value;
                
                int squadIndex = 0;
                foreach (var unit in typeUnits)
                {
                    allSquads[squadIndex].AddMember(unit);
                    squadIndex = (squadIndex + 1) % allSquads.Count;
                }
            }
            
            Debug.Log($"2Dバランス編成で{squadCount}個の小隊を編成しました。");
        }
        
        private void OrganizeSimpleSquads(List<Unit> units)
        {
            int squadCount = Mathf.CeilToInt(units.Count / (float)maxSquadSize);
            
            for (int i = 0; i < squadCount; i++)
            {
                Squad newSquad = new Squad(nextSquadID++, $"第{i + 1}小隊");
                newSquad.formation = defaultFormation;
                allSquads.Add(newSquad);
                
                // 順番にユニットを配属
                int startIndex = i * maxSquadSize;
                int endIndex = Mathf.Min(startIndex + maxSquadSize, units.Count);
                
                for (int j = startIndex; j < endIndex; j++)
                {
                    newSquad.AddMember(units[j]);
                }
            }
            
            Debug.Log($"2D単純編成で{squadCount}個の小隊を編成しました。");
        }
        
        private void PositionSquads()
        {
            for (int i = 0; i < allSquads.Count; i++)
            {
                // 小隊の配置位置を計算（2D版）
                Vector2 squadPosition = CalculateSquadPosition(i);
                allSquads[i].MoveTo(squadPosition);
                
                Debug.Log($"{allSquads[i].squadName} を位置 {squadPosition} に配置");
            }
        }
        
        private Vector2 CalculateSquadPosition(int squadIndex)
        {
            // 小隊を格子状に配置（2D版）
            int squadsPerRow = Mathf.CeilToInt(Mathf.Sqrt(allSquads.Count));
            
            int row = squadIndex / squadsPerRow;
            int col = squadIndex % squadsPerRow;
            
            float x = (col - squadsPerRow / 2f) * squadSpacing;
            float y = row * squadSpacing;
            
            return new Vector2(x, y);
        }
        
        // 小隊数を取得
        public int GetSquadCount()
        {
            return allSquads.Count;
        }
        
        // 指定されたIDの小隊を取得
        public Squad GetSquadByID(int squadID)
        {
            foreach (var squad in allSquads)
            {
                if (squad.squadID == squadID)
                    return squad;
            }
            return null;
        }
        
        // 全小隊を指定位置に移動（2D版）
        public void MoveAllSquadsTo(Vector2 targetArea)
        {
            for (int i = 0; i < allSquads.Count; i++)
            {
                Vector2 squadTarget = targetArea + CalculateSquadOffset(i);
                allSquads[i].MoveTo(squadTarget);
            }
            
            Debug.Log($"全{allSquads.Count}小隊を{targetArea}方面へ移動開始！");
        }
        
        private Vector2 CalculateSquadOffset(int squadIndex)
        {
            int squadsPerRow = Mathf.CeilToInt(Mathf.Sqrt(allSquads.Count));
            
            int row = squadIndex / squadsPerRow;
            int col = squadIndex % squadsPerRow;
            
            float x = (col - squadsPerRow / 2f) * squadSpacing * 0.5f;
            float y = row * squadSpacing * 0.5f;
            
            return new Vector2(x, y);
        }
        
        // 全小隊のフォーメーションを変更
        public void ChangeAllSquadFormation(Squad.SquadFormation newFormation)
        {
            foreach (var squad in allSquads)
            {
                squad.formation = newFormation;
                squad.ArrangeFormation();
            }
            
            Debug.Log($"全小隊のフォーメーションを{newFormation}に変更しました！");
        }
        
        // 戦闘状況を確認
        public void CheckCombatStatus()
        {
            int combatSquads = 0;
            int idleSquads = 0;
            
            foreach (var squad in allSquads)
            {
                if (squad.isInCombat)
                    combatSquads++;
                else
                    idleSquads++;
            }
            
            Debug.Log($"2D戦闘状況 - 交戦中: {combatSquads}小隊, 待機中: {idleSquads}小隊");
        }
        
        // 最も近い敵小隊を見つける（2D版）
        public Squad FindNearestEnemySquad(Squad friendlySquad, List<Squad> enemySquads)
        {
            if (enemySquads.Count == 0) return null;
            
            Squad nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var enemy in enemySquads)
            {
                if (enemy.GetAliveCount() == 0) continue;
                
                float distance = Vector2.Distance(friendlySquad.formationCenter, enemy.formationCenter);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            return nearestEnemy;
        }
        
        // 小隊を再編成（戦死者を除外）
        public void ReorganizeSquads()
        {
            Debug.Log("2D小隊の再編成を開始...");
            
            // 生存ユニットのみを収集
            List<Unit> survivors = new List<Unit>();
            
            foreach (var squad in allSquads)
            {
                foreach (var member in squad.members)
                {
                    if (member.isAlive)
                    {
                        survivors.Add(member);
                        member.squadID = -1; // 一時的に小隊から除外
                    }
                }
            }
            
            // 既存の小隊をクリア
            allSquads.Clear();
            nextSquadID = 1;
            
            // 再編成
            if (survivors.Count > 0)
            {
                OrganizeSquads();
            }
            
            Debug.Log($"2D再編成完了！生存者{survivors.Count}名を{allSquads.Count}個の小隊に再配置しました。");
        }
        
        // 全小隊にスキル使用を指示
        public void OrderAllSquadsUseSkills()
        {
            foreach (var squad in allSquads)
            {
                squad.UseSquadSkills();
            }
            
            Debug.Log("全小隊にスキル使用を指示しました！");
        }
        
        // 画面内に小隊を配置
        public void RepositionSquadsToScreen()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            // カメラの見える範囲を計算
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            
            Vector2 cameraPos = mainCamera.transform.position;
            
            for (int i = 0; i < allSquads.Count; i++)
            {
                // 画面内でランダムな位置を計算
                Vector2 squadPos = new Vector2(
                    Random.Range(cameraPos.x - width/3, cameraPos.x + width/3),
                    Random.Range(cameraPos.y - height/3, cameraPos.y + height/3)
                );
                
                allSquads[i].MoveTo(squadPos);
            }
            
            Debug.Log("小隊を画面内に再配置しました。");
        }
        
        // 小隊の散開命令
        public void OrderSquadsSpread()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            Vector2 cameraPos = mainCamera.transform.position;
            
            for (int i = 0; i < allSquads.Count; i++)
            {
                Vector2 spreadPosition = new Vector2(
                    Random.Range(cameraPos.x - width/2 + 3f, cameraPos.x + width/2 - 3f),
                    Random.Range(cameraPos.y - height/2 + 3f, cameraPos.y + height/2 - 3f)
                );
                
                allSquads[i].MoveTo(spreadPosition);
            }
            
            Debug.Log("全小隊に散開命令を発令！");
        }
        
        private void Update()
        {
            // デバッグ用のキー入力
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ChangeAllSquadFormation(Squad.SquadFormation.Line);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ChangeAllSquadFormation(Squad.SquadFormation.Box);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ChangeAllSquadFormation(Squad.SquadFormation.Wedge);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ChangeAllSquadFormation(Squad.SquadFormation.Circle);
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                ChangeAllSquadFormation(Squad.SquadFormation.Column);
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                ReorganizeSquads();
            }
            
            if (Input.GetKeyDown(KeyCode.S))
            {
                CheckCombatStatus();
            }
            
            if (Input.GetKeyDown(KeyCode.Q))
            {
                OrderAllSquadsUseSkills();
            }
            
            if (Input.GetKeyDown(KeyCode.O))
            {
                RepositionSquadsToScreen();
            }
            
            if (Input.GetKeyDown(KeyCode.I))
            {
                OrderSquadsSpread();
            }
        }
        
        // 全小隊の詳細レポート
        public void GenerateDetailedReport()
        {
            Debug.Log("=== 2D詳細小隊レポート ===");
            
            foreach (var squad in allSquads)
            {
                squad.ReportStatus();
                
                // 職業構成を表示
                Dictionary<UnitType, int> composition = new Dictionary<UnitType, int>();
                
                foreach (UnitType type in System.Enum.GetValues(typeof(UnitType)))
                {
                    composition[type] = 0;
                }
                
                foreach (var member in squad.members)
                {
                    if (member.isAlive)
                        composition[member.classData.unitType]++;
                }
                
                string compositionStr = "";
                foreach (var kvp in composition)
                {
                    if (kvp.Value > 0)
                    {
                        UnitClassData classData = new UnitClassData(kvp.Key);
                        compositionStr += $"{classData.classNameJP}×{kvp.Value} ";
                    }
                }
                
                Debug.Log($"  構成: {compositionStr}");
                Debug.Log($"  中心位置: {squad.formationCenter}");
            }
            
            Debug.Log("========================");
        }
    }
}