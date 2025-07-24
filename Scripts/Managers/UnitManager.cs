using System.Collections.Generic;
using UnityEngine;
using MilitaryRPG.Units;

namespace MilitaryRPG.Core
{
    public class UnitManager : MonoBehaviour
    {
        [Header("ユニット生成設定")]
        public GameObject unitPrefab;
        public Transform unitParent;
        
        [Header("生成位置設定")]
        public Vector3 spawnAreaMin = new Vector3(-50, 0, -50);
        public Vector3 spawnAreaMax = new Vector3(50, 0, 50);
        
        [Header("職業分布")]
        [Range(0f, 1f)] public float warriorRatio = 0.3f;
        [Range(0f, 1f)] public float archerRatio = 0.3f;
        [Range(0f, 1f)] public float mageRatio = 0.2f;
        [Range(0f, 1f)] public float priestRatio = 0.2f;
        
        private List<Unit> allUnits = new List<Unit>();
        private Dictionary<UnitType, List<Unit>> unitsByType = new Dictionary<UnitType, List<Unit>>();
        private int nextUnitID = 1;
        
        private void Awake()
        {
            InitializeUnitTypeDict();
        }
        
        private void InitializeUnitTypeDict()
        {
            unitsByType[UnitType.Warrior] = new List<Unit>();
            unitsByType[UnitType.Archer] = new List<Unit>();
            unitsByType[UnitType.Mage] = new List<Unit>();
            unitsByType[UnitType.Priest] = new List<Unit>();
        }
        
        public void CreateInitialUnits(int totalCount)
        {
            Debug.Log($"ユニット生成開始: {totalCount}体");
            
            // 職業ごとの生成数を計算
            int warriorCount = Mathf.RoundToInt(totalCount * warriorRatio);
            int archerCount = Mathf.RoundToInt(totalCount * archerRatio);
            int mageCount = Mathf.RoundToInt(totalCount * mageRatio);
            int priestCount = totalCount - warriorCount - archerCount - mageCount; // 残りは僧侶
            
            Debug.Log($"職業分布 - 戦士: {warriorCount}, 弓兵: {archerCount}, 魔法使い: {mageCount}, 僧侶: {priestCount}");
            
            // 各職業のユニットを生成
            CreateUnitsOfType(UnitType.Warrior, warriorCount);
            CreateUnitsOfType(UnitType.Archer, archerCount);
            CreateUnitsOfType(UnitType.Mage, mageCount);
            CreateUnitsOfType(UnitType.Priest, priestCount);
            
            Debug.Log($"ユニット生成完了！総数: {allUnits.Count}体");
        }
        
        private void CreateUnitsOfType(UnitType unitType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = GetRandomSpawnPosition();
                Unit newUnit = CreateUnit(unitType, spawnPosition);
                
                if (newUnit != null)
                {
                    allUnits.Add(newUnit);
                    unitsByType[unitType].Add(newUnit);
                }
            }
        }
        
        private Unit CreateUnit(UnitType unitType, Vector3 position)
        {
            GameObject unitObj;
            
            if (unitPrefab != null)
            {
                unitObj = Instantiate(unitPrefab, position, Quaternion.identity, unitParent);
            }
            else
            {
                // プレハブがない場合は基本的なオブジェクトを生成
                unitObj = CreateBasicUnitObject(position);
            }
            
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null)
            {
                unit = unitObj.AddComponent<Unit>();
            }
            
            // ユニットの初期化
            unit.unitID = nextUnitID++;
            unit.classData = new UnitClassData(unitType);
            
            // NavMeshAgentを追加
            if (unitObj.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
            {
                unitObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }
            
            // Colliderを追加
            if (unitObj.GetComponent<Collider>() == null)
            {
                unitObj.AddComponent<CapsuleCollider>();
            }
            
            // ユニット初期化
            unit.InitializeUnit();
            
            return unit;
        }
        
        private GameObject CreateBasicUnitObject(Vector3 position)
        {
            GameObject unitObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObj.transform.position = position;
            unitObj.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            unitObj.name = $"Unit_{nextUnitID:000}";
            
            return unitObj;
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float z = Random.Range(spawnAreaMin.z, spawnAreaMax.z);
            float y = spawnAreaMin.y;
            
            return new Vector3(x, y, z);
        }
        
        // 指定した職業のユニットを取得
        public List<Unit> GetUnitsByType(UnitType unitType)
        {
            return new List<Unit>(unitsByType[unitType]);
        }
        
        // 生存しているユニットのみを取得
        public List<Unit> GetAliveUnits()
        {
            List<Unit> aliveUnits = new List<Unit>();
            foreach (var unit in allUnits)
            {
                if (unit.isAlive)
                    aliveUnits.Add(unit);
            }
            return aliveUnits;
        }
        
        // 生存しているユニットを職業別に取得
        public List<Unit> GetAliveUnitsByType(UnitType unitType)
        {
            List<Unit> aliveUnits = new List<Unit>();
            foreach (var unit in unitsByType[unitType])
            {
                if (unit.isAlive)
                    aliveUnits.Add(unit);
            }
            return aliveUnits;
        }
        
        // 戦闘統計を取得
        public void GetCombatStatistics()
        {
            int totalAlive = 0;
            int totalDead = 0;
            
            Debug.Log("=== 戦闘統計 ===");
            
            foreach (UnitType unitType in System.Enum.GetValues(typeof(UnitType)))
            {
                int alive = 0;
                int dead = 0;
                
                foreach (var unit in unitsByType[unitType])
                {
                    if (unit.isAlive)
                        alive++;
                    else
                        dead++;
                }
                
                totalAlive += alive;
                totalDead += dead;
                
                UnitClassData classData = new UnitClassData(unitType);
                Debug.Log($"{classData.classNameJP}: 生存 {alive}, 戦死 {dead}");
            }
            
            Debug.Log($"総計: 生存 {totalAlive}, 戦死 {totalDead}");
            Debug.Log("================");
        }
        
        // ユニットを削除
        public void RemoveUnit(Unit unit)
        {
            if (allUnits.Contains(unit))
            {
                allUnits.Remove(unit);
                
                foreach (var typeList in unitsByType.Values)
                {
                    if (typeList.Contains(unit))
                    {
                        typeList.Remove(unit);
                        break;
                    }
                }
                
                Debug.Log($"{unit.unitName} がリストから削除されました。");
            }
        }
        
        // 全ユニットを指定位置に集合
        public void GatherAllUnits(Vector3 position)
        {
            foreach (var unit in GetAliveUnits())
            {
                UnityEngine.AI.NavMeshAgent agent = unit.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-5f, 5f),
                        0,
                        Random.Range(-5f, 5f)
                    );
                    agent.SetDestination(position + randomOffset);
                }
            }
            
            Debug.Log($"全ユニットを {position} 付近に集合させました！");
        }
        
        // ユニットの色を職業別に設定
        public void SetUnitColors()
        {
            foreach (var unit in allUnits)
            {
                Renderer renderer = unit.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = GetClassColor(unit.classData.unitType);
                    renderer.material.color = color;
                }
            }
        }
        
        private Color GetClassColor(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Warrior:
                    return Color.red;      // 戦士 = 赤
                case UnitType.Archer:
                    return Color.green;    // 弓兵 = 緑
                case UnitType.Mage:
                    return Color.blue;     // 魔法使い = 青
                case UnitType.Priest:
                    return Color.yellow;   // 僧侶 = 黄
                default:
                    return Color.white;
            }
        }
        
        private void Update()
        {
            // デバッグ用のキー入力
            if (Input.GetKeyDown(KeyCode.C))
            {
                GetCombatStatistics();
            }
            
            if (Input.GetKeyDown(KeyCode.G))
            {
                GatherAllUnits(Vector3.zero);
            }
            
            if (Input.GetKeyDown(KeyCode.V))
            {
                SetUnitColors();
            }
        }
    }
}