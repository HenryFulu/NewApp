using System.Collections.Generic;
using UnityEngine;

namespace MilitaryRPG.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int maxUnitsPerSquad = 10;
        public int totalUnits = 100;
        
        [Header("Managers")]
        public UnitManager unitManager;
        public SquadManager squadManager;
        public CombatManager combatManager;
        
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<GameManager>();
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeGame()
        {
            Debug.Log("軍隊戦略RPGゲーム初期化中...");
            
            // マネージャーの初期化
            if (unitManager == null)
                unitManager = GetComponent<UnitManager>();
            if (squadManager == null)
                squadManager = GetComponent<SquadManager>();
            if (combatManager == null)
                combatManager = GetComponent<CombatManager>();
                
            // ゲーム開始
            StartGame();
        }
        
        private void StartGame()
        {
            // 100体のユニットを生成
            unitManager.CreateInitialUnits(totalUnits);
            
            // 小隊を編成
            squadManager.OrganizeSquads();
            
            Debug.Log($"ゲーム開始！{totalUnits}体のユニットを{squadManager.GetSquadCount()}個の小隊に編成しました。");
        }
        
        private void Update()
        {
            // ゲームループの管理
            if (Input.GetKeyDown(KeyCode.Space))
            {
                combatManager.StartBattle();
            }
        }
    }
}