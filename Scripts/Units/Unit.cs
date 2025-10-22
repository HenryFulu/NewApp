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
            if (rb2d != null)
            {
                rb2d.gravityScale = 0f; // 2Dトップダウンなので重力無効
                rb2d.freezeRotation = true; // 回転を制御
            }
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
                        if (rb2d != null) rb2d.velocity = Vector2.zero;
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
            if (currentState == UnitState.Moving && hasDestination && rb2d != null)
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
        
        // 目的地を設定するパブリックメソッド
        public void SetDestination(Vector2 destination)
        {
            targetPosition = destination;
            hasDestination = true;
            currentState = UnitState.Moving;
        }
        
        // Vector3版も追加（互換性のため）
        public void SetDestination(Vector3 destination)
        {
            SetDestination(new Vector2(destination.x, destination.y));
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
                    if (rb2d != null) rb2d.velocity = Vector2.zero;
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
                    // 遠距離攻撃エフェクト
                    StartCoroutine(RangedAttackEffect(targetUnit, damage));
                }
                else if (classData.canCastMagic && currentMana >= 20)
                {
                    // 魔法攻撃エフェクト
                    damage = (int)(damage * 1.5f);
                    currentMana -= 20;
                    StartCoroutine(MagicAttackEffect(targetUnit, damage));
                }
                else
                {
                    // 近接攻撃エフェクト
                    StartCoroutine(MeleeAttackEffect(targetUnit, damage));
                }
                
                Debug.Log($"{unitName} が {targetUnit.unitName} に {damage} ダメージを与えました！");
            }
        }
        
        // 近接攻撃エフェクト
        private IEnumerator MeleeAttackEffect(Unit targetUnit, int damage)
        {
            // 攻撃者が少し前に移動するアニメーション
            Vector3 originalPos = transform.position;
            Vector3 targetPos = Vector3.MoveTowards(originalPos, targetUnit.transform.position, 0.5f);
            
            // 前進
            float attackTime = 0.2f;
            float timer = 0f;
            while (timer < attackTime)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(originalPos, targetPos, timer / attackTime);
                yield return null;
            }
            
            // 攻撃エフェクト（赤い閃光）
            StartCoroutine(CreateAttackFlash(targetUnit.transform.position, Color.red));
            
            // 衝撃波エフェクト
            StartCoroutine(CreateShockwave(targetUnit.transform.position, Color.white));
            
            // ダメージを与える
            targetUnit.TakeDamage(damage);
            
            // ダメージ数値表示
            StartCoroutine(ShowDamageNumber(targetUnit.transform.position, damage, Color.red));
            
            // 後退
            timer = 0f;
            while (timer < attackTime)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(targetPos, originalPos, timer / attackTime);
                yield return null;
            }
            
            transform.position = originalPos;
        }
        
        // 遠距離攻撃エフェクト
        private IEnumerator RangedAttackEffect(Unit targetUnit, int damage)
        {
            Debug.Log($"{unitName} が矢を放ちました！");
            
            // 弓を引くポーズ（少し後ろに移動）
            Vector3 originalPos = transform.position;
            Vector3 drawPos = originalPos + (originalPos - targetUnit.transform.position).normalized * 0.3f;
            
            float drawTime = 0.3f;
            float timer = 0f;
            while (timer < drawTime)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(originalPos, drawPos, timer / drawTime);
                yield return null;
            }
            
            // 矢のエフェクト
            yield return StartCoroutine(CreateProjectileEffect(targetUnit, damage));
            
            // 元の位置に戻る
            timer = 0f;
            while (timer < drawTime)
            {
                timer += Time.deltaTime;
                transform.position = Vector3.Lerp(drawPos, originalPos, timer / drawTime);
                yield return null;
            }
            
            transform.position = originalPos;
        }
        
        // 魔法攻撃エフェクト
        private IEnumerator MagicAttackEffect(Unit targetUnit, int damage)
        {
            Debug.Log($"{unitName} が魔法を唱えました！");
            
            // 詠唱エフェクト（自分の周りに魔法陣）
            yield return StartCoroutine(CreateMagicCircle(transform.position, classData.unitType));
            
            // 魔法弾を発射
            yield return StartCoroutine(CreateMagicProjectile(targetUnit, damage));
        }
        
        // 攻撃フラッシュエフェクト
        private IEnumerator CreateAttackFlash(Vector3 position, Color color)
        {
            GameObject flash = new GameObject("AttackFlash");
            flash.transform.position = position;
            
            SpriteRenderer flashRenderer = flash.AddComponent<SpriteRenderer>();
            flashRenderer.sprite = CreateCircleSprite(32);
            flashRenderer.color = new Color(color.r, color.g, color.b, 0.8f);
            flashRenderer.sortingOrder = 10;
            
            float duration = 0.15f;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 拡大して消える
                float scale = Mathf.Lerp(0.5f, 2f, progress);
                flash.transform.localScale = Vector3.one * scale;
                
                Color currentColor = flashRenderer.color;
                currentColor.a = Mathf.Lerp(0.8f, 0f, progress);
                flashRenderer.color = currentColor;
                
                yield return null;
            }
            
            Destroy(flash);
        }
        
        // 衝撃波エフェクト
        private IEnumerator CreateShockwave(Vector3 position, Color color)
        {
            GameObject shockwave = new GameObject("Shockwave");
            shockwave.transform.position = position;
            
            SpriteRenderer shockRenderer = shockwave.AddComponent<SpriteRenderer>();
            shockRenderer.sprite = CreateRingSprite(64);
            shockRenderer.color = new Color(color.r, color.g, color.b, 0.6f);
            shockRenderer.sortingOrder = 9;
            
            float duration = 0.3f;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // リングが拡大
                float scale = Mathf.Lerp(0.2f, 3f, progress);
                shockwave.transform.localScale = Vector3.one * scale;
                
                Color currentColor = shockRenderer.color;
                currentColor.a = Mathf.Lerp(0.6f, 0f, progress);
                shockRenderer.color = currentColor;
                
                yield return null;
            }
            
            Destroy(shockwave);
        }
        
        // 矢のエフェクト
        private IEnumerator CreateProjectileEffect(Unit targetUnit, int damage)
        {
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.position = transform.position;
            
            SpriteRenderer arrowRenderer = arrow.AddComponent<SpriteRenderer>();
            arrowRenderer.sprite = CreateArrowSprite();
            arrowRenderer.color = Color.yellow;
            arrowRenderer.sortingOrder = 8;
            
            Vector3 startPos = transform.position;
            Vector3 endPos = targetUnit.transform.position;
            Vector3 direction = (endPos - startPos).normalized;
            
            // 矢の向きを設定
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            float flightTime = 0.3f;
            float timer = 0f;
            
            while (timer < flightTime)
            {
                timer += Time.deltaTime;
                float progress = timer / flightTime;
                
                // 放物線軌道
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                currentPos.y += Mathf.Sin(progress * Mathf.PI) * 1f; // 弧を描く
                arrow.transform.position = currentPos;
                
                yield return null;
            }
            
            // 着弾エフェクト
            StartCoroutine(CreateAttackFlash(endPos, Color.orange));
            StartCoroutine(CreateShockwave(endPos, Color.yellow));
            
            // ダメージを与える
            targetUnit.TakeDamage(damage);
            StartCoroutine(ShowDamageNumber(endPos, damage, Color.orange));
            
            Destroy(arrow);
        }
        
        // 魔法陣エフェクト
        private IEnumerator CreateMagicCircle(Vector3 position, UnitType unitType)
        {
            GameObject magicCircle = new GameObject("MagicCircle");
            magicCircle.transform.position = position;
            
            SpriteRenderer circleRenderer = magicCircle.AddComponent<SpriteRenderer>();
            circleRenderer.sprite = CreateMagicCircleSprite();
            circleRenderer.color = GetMagicColor(unitType);
            circleRenderer.sortingOrder = 7;
            
            float duration = 0.5f;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 回転しながら拡大
                magicCircle.transform.Rotate(0, 0, 180f * Time.deltaTime);
                float scale = Mathf.Lerp(0.1f, 1.5f, progress);
                magicCircle.transform.localScale = Vector3.one * scale;
                
                // 点滅効果
                Color color = circleRenderer.color;
                color.a = 0.7f + 0.3f * Mathf.Sin(progress * Mathf.PI * 6);
                circleRenderer.color = color;
                
                yield return null;
            }
            
            Destroy(magicCircle);
        }
        
        // 魔法弾エフェクト
        private IEnumerator CreateMagicProjectile(Unit targetUnit, int damage)
        {
            GameObject magicBall = new GameObject("MagicProjectile");
            magicBall.transform.position = transform.position;
            
            SpriteRenderer ballRenderer = magicBall.AddComponent<SpriteRenderer>();
            ballRenderer.sprite = CreateCircleSprite(16);
            ballRenderer.color = GetMagicColor(classData.unitType);
            ballRenderer.sortingOrder = 8;
            
            Vector3 startPos = transform.position;
            Vector3 endPos = targetUnit.transform.position;
            
            float flightTime = 0.4f;
            float timer = 0f;
            
            while (timer < flightTime)
            {
                timer += Time.deltaTime;
                float progress = timer / flightTime;
                
                magicBall.transform.position = Vector3.Lerp(startPos, endPos, progress);
                
                // 魔法弾の脈動
                float scale = 1f + 0.3f * Mathf.Sin(progress * Mathf.PI * 8);
                magicBall.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            // 爆発エフェクト
            StartCoroutine(CreateMagicExplosion(endPos, classData.unitType));
            
            // ダメージを与える
            targetUnit.TakeDamage(damage);
            StartCoroutine(ShowDamageNumber(endPos, damage, GetMagicColor(classData.unitType)));
            
            Destroy(magicBall);
        }
        
        // 魔法爆発エフェクト
        private IEnumerator CreateMagicExplosion(Vector3 position, UnitType unitType)
        {
            // 複数の爆発リングを作成
            for (int i = 0; i < 3; i++)
            {
                GameObject explosion = new GameObject($"MagicExplosion_{i}");
                explosion.transform.position = position;
                
                SpriteRenderer explosionRenderer = explosion.AddComponent<SpriteRenderer>();
                explosionRenderer.sprite = CreateCircleSprite(32);
                explosionRenderer.color = GetMagicColor(unitType);
                explosionRenderer.sortingOrder = 10 - i;
                
                StartCoroutine(AnimateExplosionRing(explosion, 0.4f + i * 0.1f, 2f + i * 0.5f));
            }
            
            yield return new WaitForSeconds(0.6f);
        }
        
        private IEnumerator AnimateExplosionRing(GameObject ring, float duration, float maxScale)
        {
            SpriteRenderer renderer = ring.GetComponent<SpriteRenderer>();
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                float scale = Mathf.Lerp(0.1f, maxScale, progress);
                ring.transform.localScale = Vector3.one * scale;
                
                Color color = renderer.color;
                color.a = Mathf.Lerp(0.8f, 0f, progress);
                renderer.color = color;
                
                yield return null;
            }
            
            Destroy(ring);
        }
        
        // ダメージ数値表示
        private IEnumerator ShowDamageNumber(Vector3 position, int damage, Color color)
        {
            GameObject damageText = new GameObject("DamageNumber");
            damageText.transform.position = position + Vector3.up * 0.5f;
            
            // テキストメッシュの代わりにスプライトで数字を表現
            SpriteRenderer numberRenderer = damageText.AddComponent<SpriteRenderer>();
            numberRenderer.sprite = CreateNumberSprite(damage);
            numberRenderer.color = color;
            numberRenderer.sortingOrder = 15;
            
            float duration = 1f;
            float timer = 0f;
            Vector3 startPos = position + Vector3.up * 0.5f;
            Vector3 endPos = startPos + Vector3.up * 2f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 上に移動しながらフェードアウト
                damageText.transform.position = Vector3.Lerp(startPos, endPos, progress);
                
                Color currentColor = numberRenderer.color;
                currentColor.a = Mathf.Lerp(1f, 0f, progress);
                numberRenderer.color = currentColor;
                
                // 少し拡大
                float scale = Mathf.Lerp(1f, 1.2f, progress);
                damageText.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            Destroy(damageText);
        }
        
        // 死亡エフェクト
        private void Die()
        {
            isAlive = false;
            currentState = UnitState.Dead;
            if (rb2d != null) rb2d.velocity = Vector2.zero;
            
            // 死亡エフェクトを開始
            StartCoroutine(DeathEffect());
                
            Debug.Log($"{unitName} が戦死しました...");
        }
        
        private IEnumerator DeathEffect()
        {
            // 1. スプライトを赤くして点滅
            if (spriteRenderer != null)
            {
                Color originalColor = spriteRenderer.color;
                
                for (int i = 0; i < 6; i++)
                {
                    spriteRenderer.color = Color.red;
                    yield return new WaitForSeconds(0.1f);
                    spriteRenderer.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // 2. 死亡爆発エフェクト
            StartCoroutine(CreateDeathExplosion());
            
            // 3. 魂のエフェクト
            StartCoroutine(CreateSoulEffect());
            
            // 4. 徐々に透明にしながら縮小
            yield return StartCoroutine(FadeAndShrink());
            
            // 5. 墓石エフェクト
            CreateTombstone();
            
            // 6. 一定時間後にオブジェクトを削除
            yield return new WaitForSeconds(5f);
            gameObject.SetActive(false);
        }
        
        private IEnumerator CreateDeathExplosion()
        {
            Vector3 position = transform.position;
            
            // 複数のパーティクルを放射状に飛ばす
            for (int i = 0; i < 8; i++)
            {
                GameObject particle = new GameObject($"DeathParticle_{i}");
                particle.transform.position = position;
                
                SpriteRenderer particleRenderer = particle.AddComponent<SpriteRenderer>();
                particleRenderer.sprite = CreateCircleSprite(8);
                particleRenderer.color = new Color(1f, 0.5f, 0f, 0.8f); // オレンジ
                particleRenderer.sortingOrder = 12;
                
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                
                StartCoroutine(AnimateDeathParticle(particle, direction));
            }
            
            // 中心の爆発
            GameObject explosion = new GameObject("DeathExplosion");
            explosion.transform.position = position;
            
            SpriteRenderer explosionRenderer = explosion.AddComponent<SpriteRenderer>();
            explosionRenderer.sprite = CreateCircleSprite(64);
            explosionRenderer.color = new Color(1f, 0.2f, 0f, 0.7f);
            explosionRenderer.sortingOrder = 11;
            
            float timer = 0f;
            float duration = 0.5f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                float scale = Mathf.Lerp(0.1f, 3f, progress);
                explosion.transform.localScale = Vector3.one * scale;
                
                Color color = explosionRenderer.color;
                color.a = Mathf.Lerp(0.7f, 0f, progress);
                explosionRenderer.color = color;
                
                yield return null;
            }
            
            Destroy(explosion);
        }
        
        private IEnumerator AnimateDeathParticle(GameObject particle, Vector3 direction)
        {
            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
            Vector3 startPos = particle.transform.position;
            
            float timer = 0f;
            float duration = 0.8f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 放射状に飛んで落ちる
                Vector3 pos = startPos + direction * 3f * progress;
                pos.y += Mathf.Sin(progress * Mathf.PI) * 1.5f; // 放物線
                particle.transform.position = pos;
                
                // フェードアウト
                Color color = renderer.color;
                color.a = Mathf.Lerp(0.8f, 0f, progress);
                renderer.color = color;
                
                yield return null;
            }
            
            Destroy(particle);
        }
        
        private IEnumerator CreateSoulEffect()
        {
            GameObject soul = new GameObject("Soul");
            soul.transform.position = transform.position;
            
            SpriteRenderer soulRenderer = soul.AddComponent<SpriteRenderer>();
            soulRenderer.sprite = CreateCircleSprite(16);
            soulRenderer.color = new Color(0.8f, 0.8f, 1f, 0.6f); // 薄い青
            soulRenderer.sortingOrder = 13;
            
            float timer = 0f;
            float duration = 2f;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * 5f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // ゆっくりと上昇
                soul.transform.position = Vector3.Lerp(startPos, endPos, progress);
                
                // ふわふわと左右に揺れる
                Vector3 pos = soul.transform.position;
                pos.x += Mathf.Sin(timer * 3f) * 0.3f;
                soul.transform.position = pos;
                
                // フェードアウト
                Color color = soulRenderer.color;
                color.a = Mathf.Lerp(0.6f, 0f, progress);
                soulRenderer.color = color;
                
                yield return null;
            }
            
            Destroy(soul);
        }
        
        private IEnumerator FadeAndShrink()
        {
            if (spriteRenderer == null) yield break;
            
            Color originalColor = spriteRenderer.color;
            Vector3 originalScale = transform.localScale;
            
            float timer = 0f;
            float duration = 1f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 透明度を下げる
                Color color = originalColor;
                color.a = Mathf.Lerp(originalColor.a, 0.1f, progress);
                spriteRenderer.color = color;
                
                // 縮小
                float scale = Mathf.Lerp(1f, 0.3f, progress);
                transform.localScale = originalScale * scale;
                
                yield return null;
            }
        }
        
        private void CreateTombstone()
        {
            GameObject tombstone = new GameObject("Tombstone");
            tombstone.transform.position = transform.position;
            
            SpriteRenderer tombRenderer = tombstone.AddComponent<SpriteRenderer>();
            tombRenderer.sprite = CreateTombstoneSprite();
            tombRenderer.color = Color.gray;
            tombRenderer.sortingOrder = 1;
            tombstone.transform.localScale = Vector3.one * 0.5f;
            
            // 墓石は永続的に残る
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
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // アニメーションパラメータの更新
            if (rb2d != null)
            {
                animator.SetFloat("Speed", rb2d.velocity.magnitude);
            }
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
                    StartCoroutine(EnhancedHealEffect(allyUnit, healAmount));
                    
                    Debug.Log($"{unitName} が {allyUnit.unitName} を {healAmount} 回復しました！");
                }
            }
            
            currentMana -= 30;
        }
        
        private IEnumerator EnhancedHealEffect(Unit healedUnit, int healAmount)
        {
            Vector3 position = healedUnit.transform.position;
            
            // 1. 回復の光柱エフェクト
            GameObject healPillar = new GameObject("HealPillar");
            healPillar.transform.position = position;
            
            SpriteRenderer pillarRenderer = healPillar.AddComponent<SpriteRenderer>();
            pillarRenderer.sprite = CreateRectangleSprite(16, 64);
            pillarRenderer.color = new Color(0f, 1f, 0.5f, 0.8f); // 緑っぽい光
            pillarRenderer.sortingOrder = 12;
            
            // 2. 回復パーティクル
            for (int i = 0; i < 6; i++)
            {
                GameObject particle = new GameObject($"HealParticle_{i}");
                particle.transform.position = position + Vector3.down * 2f;
                
                SpriteRenderer particleRenderer = particle.AddComponent<SpriteRenderer>();
                particleRenderer.sprite = CreateCircleSprite(8);
                particleRenderer.color = new Color(0.5f, 1f, 0.5f, 0.9f);
                particleRenderer.sortingOrder = 13;
                
                StartCoroutine(AnimateHealParticle(particle, position, i * 0.1f));
            }
            
            // 3. 回復リング
            GameObject healRing = new GameObject("HealRing");
            healRing.transform.position = position;
            
            SpriteRenderer ringRenderer = healRing.AddComponent<SpriteRenderer>();
            ringRenderer.sprite = CreateRingSprite(48);
            ringRenderer.color = new Color(0f, 1f, 0f, 0.7f);
            ringRenderer.sortingOrder = 11;
            
            // 光柱のアニメーション
            float duration = 1f;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 光柱が上に伸びて消える
                Vector3 scale = pillarRenderer.transform.localScale;
                scale.y = Mathf.Lerp(0.1f, 2f, progress);
                pillarRenderer.transform.localScale = scale;
                
                Color pillarColor = pillarRenderer.color;
                pillarColor.a = Mathf.Lerp(0.8f, 0f, progress);
                pillarRenderer.color = pillarColor;
                
                // リングが拡大
                float ringScale = Mathf.Lerp(0.5f, 2f, progress);
                healRing.transform.localScale = Vector3.one * ringScale;
                
                Color ringColor = ringRenderer.color;
                ringColor.a = Mathf.Lerp(0.7f, 0f, progress);
                ringRenderer.color = ringColor;
                
                yield return null;
            }
            
            // 4. 回復数値表示
            StartCoroutine(ShowDamageNumber(position, healAmount, Color.green));
            
            // 5. 対象ユニットの一時的な輝き
            if (healedUnit.spriteRenderer != null)
            {
                StartCoroutine(HealGlow(healedUnit));
            }
            
            Destroy(healPillar);
            Destroy(healRing);
        }
        
        private IEnumerator AnimateHealParticle(GameObject particle, Vector3 targetPos, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            SpriteRenderer renderer = particle.GetComponent<SpriteRenderer>();
            Vector3 startPos = particle.transform.position;
            
            float timer = 0f;
            float duration = 0.8f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 上昇しながら対象に向かう
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos + Vector3.up * 0.5f, progress);
                currentPos.x += Mathf.Sin(timer * 5f) * 0.3f; // 揺れる動き
                particle.transform.position = currentPos;
                
                // 輝き
                Color color = renderer.color;
                color.a = Mathf.Lerp(0.9f, 0f, progress);
                renderer.color = color;
                
                // 回転
                particle.transform.Rotate(0, 0, 360f * Time.deltaTime);
                
                yield return null;
            }
            
            Destroy(particle);
        }
        
        private IEnumerator HealGlow(Unit healedUnit)
        {
            SpriteRenderer renderer = healedUnit.spriteRenderer;
            Color originalColor = renderer.color;
            
            float timer = 0f;
            float duration = 0.6f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                
                // 緑色に光る
                Color glowColor = Color.Lerp(originalColor, Color.green, Mathf.Sin(progress * Mathf.PI * 3) * 0.5f);
                renderer.color = glowColor;
                
                yield return null;
            }
            
            renderer.color = originalColor;
        }
        
        // スプライト作成メソッド群
        private Sprite CreateCircleSprite(int size)
        {
            try
            {
                Texture2D texture = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float radius = size / 2f - 1;
                
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        Vector2 pixel = new Vector2(x, y);
                        float distance = Vector2.Distance(pixel, center);
                        
                        if (distance <= radius)
                        {
                            float alpha = 1f - (distance / radius) * 0.3f;
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                        else
                        {
                            pixels[y * size + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateCircleSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Sprite CreateRingSprite(int size)
        {
            try
            {
                Texture2D texture = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float outerRadius = size / 2f - 1;
                float innerRadius = outerRadius * 0.7f;
                
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        Vector2 pixel = new Vector2(x, y);
                        float distance = Vector2.Distance(pixel, center);
                        
                        if (distance <= outerRadius && distance >= innerRadius)
                        {
                            float alpha = 1f - Mathf.Abs(distance - (outerRadius + innerRadius) / 2f) / ((outerRadius - innerRadius) / 2f) * 0.5f;
                            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                        else
                        {
                            pixels[y * size + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateRingSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Sprite CreateArrowSprite()
        {
            try
            {
                int width = 24;
                int height = 8;
                Texture2D texture = new Texture2D(width, height);
                Color[] pixels = new Color[width * height];
                
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // 矢の形を作成
                        if ((x < width - 4 && y >= 3 && y <= 4) || // 矢の軸
                            (x >= width - 8 && y >= 2 && y <= 5 && x + y >= width + 1) || // 矢尻上
                            (x >= width - 8 && y >= 2 && y <= 5 && x - y <= width - 7)) // 矢尻下
                        {
                            pixels[y * width + x] = Color.white;
                        }
                        else
                        {
                            pixels[y * width + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateArrowSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Sprite CreateMagicCircleSprite()
        {
            try
            {
                int size = 48;
                Texture2D texture = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                
                Vector2 center = new Vector2(size / 2f, size / 2f);
                
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        Vector2 pixel = new Vector2(x, y);
                        float distance = Vector2.Distance(pixel, center);
                        float angle = Mathf.Atan2(y - center.y, x - center.x);
                        
                        // 魔法陣のパターン
                        bool isVisible = false;
                        
                        // 外側のリング
                        if (distance > size/2f - 3 && distance < size/2f - 1)
                            isVisible = true;
                        
                        // 内側のリング
                        if (distance > size/2f - 8 && distance < size/2f - 6)
                            isVisible = true;
                        
                        // 星型のパターン
                        if (distance < size/2f - 10)
                        {
                            float starAngle = Mathf.Repeat(angle + Mathf.PI, Mathf.PI * 2f / 6f);
                            if (starAngle < Mathf.PI / 6f && distance > size/4f)
                                isVisible = true;
                        }
                        
                        if (isVisible)
                        {
                            pixels[y * size + x] = Color.white;
                        }
                        else
                        {
                            pixels[y * size + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateMagicCircleSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Sprite CreateRectangleSprite(int width, int height)
        {
            try
            {
                Texture2D texture = new Texture2D(width, height);
                Color[] pixels = new Color[width * height];
                
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateRectangleSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Sprite CreateTombstoneSprite()
        {
            try
            {
                int width = 16;
                int height = 20;
                Texture2D texture = new Texture2D(width, height);
                Color[] pixels = new Color[width * height];
                
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // 墓石の形
                        if ((y < height - 4 && x >= 2 && x < width - 2) || // 本体
                            (y >= height - 4 && x >= 1 && x < width - 1) || // 土台
                            (y >= height - 8 && y < height - 4 && x >= 4 && x < width - 4)) // 上部
                        {
                            pixels[y * width + x] = Color.white;
                        }
                        else
                        {
                            pixels[y * width + x] = Color.clear;
                        }
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.1f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateTombstoneSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Sprite CreateNumberSprite(int number)
        {
            try
            {
                // 簡単な数字スプライト（実際にはより複雑にできます）
                int size = 24;
                Texture2D texture = new Texture2D(size, size);
                Color[] pixels = new Color[size * size];
                
                // 数字を簡単な矩形で表現
                for (int x = 6; x < 18; x++)
                {
                    for (int y = 6; y < 18; y++)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateNumberSpriteエラー: {e.Message}");
                return null;
            }
        }
        
        private Color GetMagicColor(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Mage:
                    return new Color(0.5f, 0.5f, 1f, 0.8f); // 青い魔法
                case UnitType.Priest:
                    return new Color(1f, 1f, 0.5f, 0.8f); // 聖なる光
                default:
                    return new Color(1f, 0.5f, 1f, 0.8f); // 紫の魔法
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