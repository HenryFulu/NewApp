using UnityEngine;

namespace MilitaryRPG.Units
{
    public enum UnitType
    {
        Warrior,    // 戦士
        Archer,     // 弓兵
        Mage,       // 魔法使い
        Priest      // 僧侶
    }
    
    [System.Serializable]
    public class UnitClassData
    {
        public UnitType unitType;
        public string className;
        public string classNameJP;
        
        [Header("基本ステータス")]
        public int baseHealth = 100;
        public int baseAttack = 20;
        public int baseDefense = 10;
        public int baseMana = 50;
        public float baseSpeed = 5f;
        public float attackRange = 2f;
        
        [Header("特殊能力")]
        public bool canHeal = false;
        public bool canCastMagic = false;
        public bool canRangedAttack = false;
        public bool hasTaunt = false;
        
        public UnitClassData(UnitType type)
        {
            unitType = type;
            SetClassDefaults();
        }
        
        private void SetClassDefaults()
        {
            switch (unitType)
            {
                case UnitType.Warrior:
                    className = "Warrior";
                    classNameJP = "戦士";
                    baseHealth = 150;
                    baseAttack = 25;
                    baseDefense = 20;
                    baseMana = 20;
                    baseSpeed = 4f;
                    attackRange = 1.5f;
                    hasTaunt = true;
                    break;
                    
                case UnitType.Archer:
                    className = "Archer";
                    classNameJP = "弓兵";
                    baseHealth = 80;
                    baseAttack = 30;
                    baseDefense = 8;
                    baseMana = 30;
                    baseSpeed = 6f;
                    attackRange = 8f;
                    canRangedAttack = true;
                    break;
                    
                case UnitType.Mage:
                    className = "Mage";
                    classNameJP = "魔法使い";
                    baseHealth = 60;
                    baseAttack = 35;
                    baseDefense = 5;
                    baseMana = 100;
                    baseSpeed = 5f;
                    attackRange = 6f;
                    canCastMagic = true;
                    break;
                    
                case UnitType.Priest:
                    className = "Priest";
                    classNameJP = "僧侶";
                    baseHealth = 100;
                    baseAttack = 15;
                    baseDefense = 12;
                    baseMana = 80;
                    baseSpeed = 4.5f;
                    attackRange = 5f;
                    canHeal = true;
                    canCastMagic = true;
                    break;
            }
        }
    }
}