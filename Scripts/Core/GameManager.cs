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
        
        [Header("2D Camera Settings")]
        public Camera mainCamera;
        public float cameraSize = 20f;
        public Vector3 cameraOffset = new Vector3(0, 0, -10);
        
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
            Debug.Log("2D軍隊戦略RPGゲーム初期化中...");
            
            // 2Dカメラ設定
            SetupCamera();
            
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
        
        private void SetupCamera()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = cameraSize;
                mainCamera.transform.position = cameraOffset;
            }
        }
        
        private void StartGame()
        {
            // 100体のユニットを生成
            unitManager.CreateInitialUnits(totalUnits);
            
            // 小隊を編成
            squadManager.OrganizeSquads();
            
            Debug.Log($"2Dゲーム開始！{totalUnits}体のユニットを{squadManager.GetSquadCount()}個の小隊に編成しました。");
        }
        
        private void Update()
        {
            // ゲームループの管理
            if (Input.GetKeyDown(KeyCode.Space))
            {
                combatManager.StartBattle();
            }
            
            // カメラ操作（WASD移動、マウスホイールズーム）
            HandleCameraControls();
        }
        
        private void HandleCameraControls()
        {
            if (mainCamera == null) return;
            
            // カメラ移動
            float moveSpeed = 10f;
            Vector3 movement = Vector3.zero;
            
            if (Input.GetKey(KeyCode.W)) movement.y += moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) movement.y -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.A)) movement.x -= moveSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.D)) movement.x += moveSpeed * Time.deltaTime;
            
            mainCamera.transform.Translate(movement);
            
            // ズーム
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                mainCamera.orthographicSize = Mathf.Clamp(
                    mainCamera.orthographicSize - scroll * 5f, 
                    5f, 50f
                );
            }
        }
    }
}