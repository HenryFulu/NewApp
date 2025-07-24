using System.Collections.Generic;
using UnityEngine;
using MilitaryRPG.Units;

namespace MilitaryRPG.Squads
{
    [System.Serializable]
    public class Squad
    {
        [Header("小隊情報")]
        public int squadID;
        public string squadName;
        public List<Unit> members = new List<Unit>();
        public Unit leader;
        
        [Header("小隊構成")]
        public int maxMembers = 10;
        public Vector3 formationCenter;
        public SquadFormation formation = SquadFormation.Line;
        
        [Header("戦闘状態")]
        public bool isInCombat = false;
        public Vector3 targetPosition;
        public Squad enemySquad;
        
        public enum SquadFormation
        {
            Line,       // 横一列
            Column,     // 縦一列
            Box,        // 箱型
            Wedge,      // くさび型
            Circle      // 円形
        }
        
        public Squad(int id, string name)
        {
            squadID = id;
            squadName = name;
            members = new List<Unit>();
        }
        
        // メンバーを追加
        public bool AddMember(Unit unit)
        {
            if (members.Count >= maxMembers)
            {
                Debug.LogWarning($"小隊 {squadName} は満員です！");
                return false;
            }
            
            if (members.Contains(unit))
            {
                Debug.LogWarning($"{unit.unitName} は既に小隊 {squadName} に所属しています！");
                return false;
            }
            
            members.Add(unit);
            unit.squadID = squadID;
            
            // 最初のメンバーをリーダーに設定
            if (leader == null && unit.isAlive)
            {
                SetLeader(unit);
            }
            
            Debug.Log($"{unit.unitName} が小隊 {squadName} に配属されました。");
            return true;
        }
        
        // メンバーを削除
        public void RemoveMember(Unit unit)
        {
            if (members.Contains(unit))
            {
                members.Remove(unit);
                unit.squadID = -1;
                
                // リーダーが削除された場合、新しいリーダーを選出
                if (leader == unit)
                {
                    SelectNewLeader();
                }
                
                Debug.Log($"{unit.unitName} が小隊 {squadName} から除隊しました。");
            }
        }
        
        // 新しいリーダーを選出
        private void SelectNewLeader()
        {
            leader = null;
            
            foreach (var member in members)
            {
                if (member.isAlive)
                {
                    SetLeader(member);
                    break;
                }
            }
            
            if (leader == null)
            {
                Debug.Log($"小隊 {squadName} にリーダーがいません！");
            }
        }
        
        // リーダーを設定
        public void SetLeader(Unit unit)
        {
            if (members.Contains(unit))
            {
                // 前のリーダーのフラグを解除
                if (leader != null)
                    leader.isSquadLeader = false;
                
                leader = unit;
                unit.isSquadLeader = true;
                
                Debug.Log($"{unit.unitName} が小隊 {squadName} のリーダーになりました！");
            }
        }
        
        // 小隊を指定位置に移動
        public void MoveTo(Vector3 position)
        {
            targetPosition = position;
            formationCenter = position;
            
            ArrangeFormation();
        }
        
        // フォーメーションを配置
        public void ArrangeFormation()
        {
            if (members.Count == 0) return;
            
            List<Vector3> positions = CalculateFormationPositions();
            
            for (int i = 0; i < members.Count && i < positions.Count; i++)
            {
                if (members[i].isAlive)
                {
                    NavMeshAgent agent = members[i].GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.SetDestination(positions[i]);
                    }
                }
            }
        }
        
        // フォーメーションの位置を計算
        private List<Vector3> CalculateFormationPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            int aliveCount = GetAliveCount();
            
            switch (formation)
            {
                case SquadFormation.Line:
                    for (int i = 0; i < aliveCount; i++)
                    {
                        float x = formationCenter.x + (i - aliveCount / 2f) * 2f;
                        positions.Add(new Vector3(x, formationCenter.y, formationCenter.z));
                    }
                    break;
                    
                case SquadFormation.Column:
                    for (int i = 0; i < aliveCount; i++)
                    {
                        float z = formationCenter.z + i * 2f;
                        positions.Add(new Vector3(formationCenter.x, formationCenter.y, z));
                    }
                    break;
                    
                case SquadFormation.Box:
                    int rows = Mathf.CeilToInt(Mathf.Sqrt(aliveCount));
                    for (int i = 0; i < aliveCount; i++)
                    {
                        int row = i / rows;
                        int col = i % rows;
                        float x = formationCenter.x + (col - rows / 2f) * 2f;
                        float z = formationCenter.z + row * 2f;
                        positions.Add(new Vector3(x, formationCenter.y, z));
                    }
                    break;
                    
                case SquadFormation.Wedge:
                    for (int i = 0; i < aliveCount; i++)
                    {
                        float row = Mathf.Floor(i / 2f);
                        float side = (i % 2 == 0) ? -1f : 1f;
                        float offset = (i / 2) * side;
                        
                        float x = formationCenter.x + offset * 1.5f;
                        float z = formationCenter.z - row * 2f;
                        positions.Add(new Vector3(x, formationCenter.y, z));
                    }
                    break;
                    
                case SquadFormation.Circle:
                    float radius = Mathf.Max(3f, aliveCount * 0.5f);
                    for (int i = 0; i < aliveCount; i++)
                    {
                        float angle = (i / (float)aliveCount) * 360f * Mathf.Deg2Rad;
                        float x = formationCenter.x + Mathf.Cos(angle) * radius;
                        float z = formationCenter.z + Mathf.Sin(angle) * radius;
                        positions.Add(new Vector3(x, formationCenter.y, z));
                    }
                    break;
            }
            
            return positions;
        }
        
        // 生存メンバー数を取得
        public int GetAliveCount()
        {
            int count = 0;
            foreach (var member in members)
            {
                if (member.isAlive)
                    count++;
            }
            return count;
        }
        
        // 小隊の戦闘力を取得
        public int GetCombatPower()
        {
            int power = 0;
            foreach (var member in members)
            {
                if (member.isAlive)
                    power += member.classData.baseAttack + member.classData.baseDefense;
            }
            return power;
        }
        
        // 敵小隊と交戦開始
        public void EngageEnemy(Squad enemy)
        {
            enemySquad = enemy;
            isInCombat = true;
            
            // 全メンバーを戦闘状態に
            foreach (var member in members)
            {
                if (member.isAlive)
                {
                    member.isInCombat = true;
                }
            }
            
            Debug.Log($"小隊 {squadName} が小隊 {enemy.squadName} と交戦開始！");
        }
        
        // 戦闘終了
        public void EndCombat()
        {
            isInCombat = false;
            enemySquad = null;
            
            foreach (var member in members)
            {
                if (member.isAlive)
                {
                    member.isInCombat = false;
                }
            }
            
            Debug.Log($"小隊 {squadName} の戦闘が終了しました。");
        }
        
        // 小隊全体にスキル使用指示
        public void UseSquadSkills()
        {
            foreach (var member in members)
            {
                if (!member.isAlive) continue;
                
                // 僧侶は回復
                if (member.classData.canHeal)
                {
                    member.HealAllies();
                }
                
                // 戦士は挑発
                if (member.classData.hasTaunt)
                {
                    member.Taunt();
                }
            }
        }
        
        // 小隊の状況報告
        public void ReportStatus()
        {
            int alive = GetAliveCount();
            int total = members.Count;
            int combatPower = GetCombatPower();
            
            Debug.Log($"【小隊状況報告】{squadName}: 生存 {alive}/{total}, 戦闘力 {combatPower}");
        }
    }
}