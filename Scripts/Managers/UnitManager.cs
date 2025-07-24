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
        
        [Header("2D生成位置設定")]
        public Vector2 spawnAreaMin = new Vector2(-50, -50);
        public Vector2 spawnAreaMax = new Vector2(50, 50);
        
        [Header("職業分布")]
        [Range(0f, 1f)] public float warriorRatio = 0.3f;
        [Range(0f, 1f)] public float archerRatio = 0.3f;
        [Range(0f, 1f)] public float mageRatio = 0.2f;
        [Range(0f, 1f)] public float priestRatio = 0.2f;
        
        [Header("2Dスプライト設定")]
        public Sprite warriorSprite;
        public Sprite archerSprite;
        public Sprite mageSprite;
        public Sprite priestSprite;
        
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
            Debug.Log($"2Dユニット生成開始: {totalCount}体");
            
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
            
            Debug.Log($"2Dユニット生成完了！総数: {allUnits.Count}体");
        }
        
        private void CreateUnitsOfType(UnitType unitType, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPosition = GetRandomSpawnPosition();
                Unit newUnit = CreateUnit(unitType, spawnPosition);
                
                if (newUnit != null)
                {
                    allUnits.Add(newUnit);
                    unitsByType[unitType].Add(newUnit);
                }
            }
        }
        
        private Unit CreateUnit(UnitType unitType, Vector2 position)
        {
            GameObject unitObj;
            
            if (unitPrefab != null)
            {
                unitObj = Instantiate(unitPrefab, position, Quaternion.identity, unitParent);
            }
            else
            {
                // プレハブがない場合は基本的な2Dオブジェクトを生成
                unitObj = CreateBasic2DUnitObject(position, unitType);
            }
            
            Unit unit = unitObj.GetComponent<Unit>();
            if (unit == null)
            {
                unit = unitObj.AddComponent<Unit>();
            }
            
            // ユニットの初期化
            unit.unitID = nextUnitID++;
            unit.classData = new UnitClassData(unitType);
            
            // 2D物理コンポーネントを追加
            SetupUnit2DComponents(unitObj, unitType);
            
            // ユニット初期化
            unit.InitializeUnit();
            
            return unit;
        }
        
        private GameObject CreateBasic2DUnitObject(Vector2 position, UnitType unitType)
        {
            GameObject unitObj = new GameObject($"Unit_{nextUnitID:000}_{unitType}");
            unitObj.transform.position = new Vector3(position.x, position.y, 0);
            
            // SpriteRendererを追加
            SpriteRenderer spriteRenderer = unitObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetSpriteForUnitType(unitType);
            spriteRenderer.sortingOrder = 1;
            
            // スプライトがない場合はデフォルトの色付き正方形を作成
            if (spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = CreateDefaultSprite();
                spriteRenderer.color = GetClassColor(unitType);
            }
            
            return unitObj;
        }
        
        private Sprite GetSpriteForUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Warrior:
                    return warriorSprite;
                case UnitType.Archer:
                    return archerSprite;
                case UnitType.Mage:
                    return mageSprite;
                case UnitType.Priest:
                    return priestSprite;
                default:
                    return null;
            }
        }
        
        private Sprite CreateDefaultSprite()
        {
            // 16x16の白いテクスチャを作成
            Texture2D texture = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }
        
        private void SetupUnit2DComponents(GameObject unitObj, UnitType unitType)
        {
            // Rigidbody2Dを追加
            if (unitObj.GetComponent<Rigidbody2D>() == null)
            {
                Rigidbody2D rb2d = unitObj.AddComponent<Rigidbody2D>();
                rb2d.gravityScale = 0f;
                rb2d.freezeRotation = true;
            }
            
            // CircleCollider2Dを追加
            if (unitObj.GetComponent<Collider2D>() == null)
            {
                CircleCollider2D collider = unitObj.AddComponent<CircleCollider2D>();
                collider.radius = 0.3f;
                collider.isTrigger = false; // 物理的な衝突を有効にする
            }
            
            // スケールを調整
            unitObj.transform.localScale = GetScaleForUnitType(unitType);
        }
        
        private Vector3 GetScaleForUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Warrior:
                    return new Vector3(1.2f, 1.2f, 1f); // 戦士は少し大きく
                case UnitType.Archer:
                    return new Vector3(0.9f, 0.9f, 1f); // 弓兵は少し小さく
                case UnitType.Mage:
                    return new Vector3(0.8f, 0.8f, 1f); // 魔法使いはさらに小さく
                case UnitType.Priest:
                    return new Vector3(1f, 1f, 1f);     // 僧侶は標準サイズ
                default:
                    return Vector3.one;
            }
        }
        
        private Vector2 GetRandomSpawnPosition()
        {
            float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float y = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            
            return new Vector2(x, y);
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
                if (unit != null && unit.isAlive)
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
                if (unit != null && unit.isAlive)
                    aliveUnits.Add(unit);
            }
            return aliveUnits;
        }
        
        // 戦闘統計を取得
        public void GetCombatStatistics()
        {
            int totalAlive = 0;
            int totalDead = 0;
            
            Debug.Log("=== 2D戦闘統計 ===");
            
            foreach (UnitType unitType in System.Enum.GetValues(typeof(UnitType)))
            {
                int alive = 0;
                int dead = 0;
                
                foreach (var unit in unitsByType[unitType])
                {
                    if (unit != null)
                    {
                        if (unit.isAlive)
                            alive++;
                        else
                            dead++;
                    }
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
        
        // 全ユニットを指定位置に集合（2D版）
        public void GatherAllUnits(Vector2 position)
        {
            foreach (var unit in GetAliveUnits())
            {
                if (unit != null)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * 5f;
                    unit.SetDestination(position + randomOffset);
                }
            }
            
            Debug.Log($"全ユニットを {position} 付近に集合させました！");
        }
        
        // ユニットの色を職業別に設定
        public void SetUnitColors()
        {
            foreach (var unit in allUnits)
            {
                if (unit != null)
                {
                    SpriteRenderer spriteRenderer = unit.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        Color color = GetClassColor(unit.classData.unitType);
                        spriteRenderer.color = color;
                    }
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
        
        // 画面に収まるようにユニットを配置
        public void RepositionUnitsToScreen()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            // カメラの見える範囲を計算
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;
            
            Vector2 cameraPos = mainCamera.transform.position;
            Vector2 newSpawnMin = new Vector2(cameraPos.x - width/2 + 2f, cameraPos.y - height/2 + 2f);
            Vector2 newSpawnMax = new Vector2(cameraPos.x + width/2 - 2f, cameraPos.y + height/2 - 2f);
            
            foreach (var unit in GetAliveUnits())
            {
                if (unit != null)
                {
                    Vector2 newPos = new Vector2(
                        Random.Range(newSpawnMin.x, newSpawnMax.x),
                        Random.Range(newSpawnMin.y, newSpawnMax.y)
                    );
                    unit.transform.position = newPos;
                }
            }
            
            Debug.Log("ユニットを画面内に再配置しました。");
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
                GatherAllUnits(Vector2.zero);
            }
            
            if (Input.GetKeyDown(KeyCode.V))
            {
                SetUnitColors();
            }
            
            if (Input.GetKeyDown(KeyCode.P))
            {
                RepositionUnitsToScreen();
            }
        }
    }
}