using UnityEngine;

namespace ForgottenDomain
{
    public enum Team { Player, Opponent }

    public enum SummonMode { ATK, DEF }

    public class Monster : MonoBehaviour
    {
        [SerializeField] private string displayName = "Unknown";
        [SerializeField] private int maxHealth = 1000, currentHealth;
        [SerializeField] private int attack = 400, defense = 200;
        [SerializeField] private int moveRange = 1, level = 1;
        [SerializeField] private int fortifyBonus;
        private int _tempAtkBonus, _tempDefBonus, _buffTurns;

        public Team OwnerTeam { get; private set; }
        public BattleTile CurrentTile { get; set; }
        public string DisplayName => displayName;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public int Attack => attack + _tempAtkBonus;
        public int Defense => defense + fortifyBonus + _tempDefBonus;
        public int Level => level;
        public int MoveRange => level <= 4 ? 1 : (level <= 6 ? 2 : 3);
        public bool IsAlive => currentHealth > 0;
        public bool HasMovedThisTurn { get; set; }
        public bool HasAttackedThisTurn { get; set; }
        public bool IsFortified => fortifyBonus > 0;
        public SummonMode CurrentMode { get; private set; } = SummonMode.ATK;

        public CardData SourceCard { get; private set; }
        public int RootTurns { get; set; }

        private Animator _animator;
        private Vector3 _targetPos;
        private bool _isMoving;

        public void Initialize(Team team, CardData card, BattleTile tile)
        {
            OwnerTeam = team; SourceCard = card; displayName = card.cardName; CurrentTile = tile;
            level = card.level; attack = card.attack; defense = card.defense;
            maxHealth = card.health; currentHealth = card.health;
            moveRange = MoveRange;
            name = $"{card.cardName} ({team})";
            
            // Set initial position and face enemy
            transform.position = tile.transform.position + Vector3.up * 0.1f;
            _targetPos = transform.position;
            transform.rotation = Quaternion.Euler(0, team == Team.Player ? 0 : 180, 0);

            BuildVisual();
            AddIndicator();

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.Play("Summon", 0, 0f);
                Debug.Log($"[Monster] Triggered Summon animation for {displayName}");
            }
        }

        public void ApplyEquip(int atk, int def)
        {
            attack += atk;
            fortifyBonus += def;
            Debug.Log($"[Monster] {DisplayName} equipped! ATK +{atk}, DEF +{def}");
        }

        public void ApplyBuff(int atk, int def, int turns)
        {
            _tempAtkBonus += atk;
            _tempDefBonus += def;
            _buffTurns = Mathf.Max(_buffTurns, turns);
            Debug.Log($"[Monster] {DisplayName} buffed! ATK +{atk}, DEF +{def} for {turns} turns.");
            
            string text = "";
            if (atk != 0) text += $"ATK {(atk > 0 ? "+" : "")}{atk} ";
            if (def != 0) text += $"DEF {(def > 0 ? "+" : "")}{def}";
            if (!string.IsNullOrEmpty(text)) DamageText.Create(transform.position, text.Trim(), atk + def > 0 ? Color.cyan : Color.magenta);
        }

        public void Heal(int amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            Debug.Log($"[Monster] {DisplayName} healed for {amount}. Health: {currentHealth}/{MaxHealth}");
            DamageText.Create(transform.position, $"+{amount}", Color.green);
        }

        public void Initialize(Team team, string name, BattleTile tile, int lvl = 1, int atk = 400, int def = 200, int hp = 1000)
        {
            // Legacy init
            OwnerTeam = team; displayName = name; CurrentTile = tile;
            level = lvl; attack = atk; defense = def;
            maxHealth = hp; currentHealth = hp;
            moveRange = MoveRange;
            this.name = $"{name} ({team})";
            BuildVisual();
            AddIndicator();

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && _animator.isActiveAndEnabled)
            {
                _animator.Play("Summon", 0, 0f);
            }
        }

        private void AddIndicator()
        {
            GameObject indicatorGo = new GameObject("Indicator");
            indicatorGo.transform.SetParent(transform);
            indicatorGo.transform.localPosition = new Vector3(0, 3.5f, 0); // Above head
            var indicator = indicatorGo.AddComponent<MonsterIndicator>();
            indicator.Initialize(this);
        }

        public void ApplySummonMode(SummonMode mode)
        {
            CurrentMode = mode;
            if (mode == SummonMode.ATK) attack += Mathf.RoundToInt(attack * 0.2f);
            else defense += Mathf.RoundToInt(defense * 0.2f);

            if (_animator != null) _animator.SetBool("isDefending", mode == SummonMode.DEF);
        }

        private void BuildVisual()
        {
            if (SourceCard != null && !string.IsNullOrEmpty(SourceCard.modelPath))
            {
                var prefab = Resources.Load<GameObject>($"Models/{SourceCard.modelPath}");
                if (prefab != null)
                {
                    var model = Instantiate(prefab, transform);
                    model.name = "Body";
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    
                    var renderers = model.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        // 1. Calculate total height
                        Bounds worldBounds = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);
                        float currentHeight = worldBounds.size.y;

                        // 2. Scale model to target height (roughly 1.5 units)
                        if (currentHeight > 0.001f)
                        {
                            float scale = 1.5f / currentHeight;
                            model.transform.localScale *= scale;
                        }

                        // 3. Re-calculate bounds to find the new bottom point after scaling
                        worldBounds = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);
                        
                        // 4. Offset the model so its bottom point matches the monster's origin (on the board)
                        float bottomWorldY = worldBounds.min.y;
                        float parentWorldY = transform.position.y;
                        float localOffsetY = parentWorldY - bottomWorldY;
                        
                        model.transform.localPosition = new Vector3(0, localOffsetY, 0);
                        
                        Debug.Log($"[Monster] Positioned {SourceCard.modelPath}: Height={worldBounds.size.y}, Offset={localOffsetY}");
                    }
                    return;
                }
                else
                {
                    Debug.LogError($"[Monster] Failed to load model for {SourceCard.cardName} at path: Models/{SourceCard.modelPath}");
                }
            }

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(transform);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            body.transform.localScale = new Vector3(0.8f, 1.6f, 0.8f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", OwnerTeam == Team.Player ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.3f, 0.2f));
            mat.SetFloat("_Smoothness", 0.1f);
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(body.GetComponent<Collider>());
        }

        private void Update()
        {
            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, _targetPos, 5f * Time.deltaTime);
                if (Vector3.Distance(transform.position, _targetPos) < 0.01f)
                {
                    transform.position = _targetPos;
                    _isMoving = false;
                    if (_animator != null) _animator.SetBool("isMoving", false);
                }
            }
        }

        public void TakeDamage(int amount) 
        { 
            currentHealth = Mathf.Max(0, currentHealth - amount); 
            Debug.Log($"[Monster] {DisplayName} took {amount} damage. Health: {currentHealth}/{MaxHealth}");
            DamageText.Create(transform.position, $"-{amount}", Color.red);
            if (currentHealth <= 0) OnDeath(); 
        }

        public bool MoveTo(BattleTile dest)
        {
            if (!IsAlive || HasMovedThisTurn || dest == null || dest.IsOccupied || RootTurns > 0) return false;
            
            Vector2Int startPos = CurrentTile != null ? CurrentTile.coordinates : Vector2Int.zero;
            if (CurrentTile != null) CurrentTile.OccupyingMonster = null;
            
            CurrentTile = dest;
            dest.OccupyingMonster = this;
            
            _targetPos = dest.transform.position + Vector3.up * 0.1f;
            _isMoving = true;
            if (_animator != null) _animator.SetBool("isMoving", true);

            HasMovedThisTurn = true;
            
            GameLogManager.Instance?.Log($"{OwnerTeam}'s {DisplayName} moved from {startPos} to {dest.coordinates}.");
            
            GameManager.Instance.CheckTrapsTrigger(this, dest);
            return true;
        }

        public bool CanAttackMonster(Monster target) => target != null && target.IsAlive && target.OwnerTeam != OwnerTeam && CurrentTile.DistanceTo(target.CurrentTile) == 1;

        public void AttackMonster(Monster target)
        {
            if (!CanAttackMonster(target)) 
            {
                Debug.LogWarning($"[Monster] {DisplayName} cannot attack {target?.DisplayName}");
                return;
            }

            if (_animator != null) _animator.SetTrigger("attack");

            int attackerAtk = Attack;
            int defenderDef = target.Defense;

            GameLogManager.Instance?.Log($"{OwnerTeam}'s {DisplayName} (ATK: {attackerAtk}) attacks {target.OwnerTeam}'s {target.DisplayName} (DEF: {defenderDef})!");

            if (attackerAtk > defenderDef)
            {
                // Attacker wins
                GameLogManager.Instance?.Log($"{DisplayName} overpowers {target.DisplayName}!");
                target.TakeDamage(target.CurrentHealth); // Destroy defender
            }
            else if (attackerAtk < defenderDef)
            {
                // Defender wins
                int battleDamage = defenderDef - attackerAtk;
                GameLogManager.Instance?.Log($"{target.DisplayName} reflects the attack! {DisplayName} is destroyed.");
                GameLogManager.Instance?.Log($"{OwnerTeam} takes {battleDamage} battle damage to their Life Points!");
                
                // Deal damage to attacker's owner tower
                var ownerTower = (OwnerTeam == Team.Player) ? GameManager.Instance.PlayerTower : GameManager.Instance.OpponentTower;
                if (ownerTower != null) ownerTower.TakeDamage(battleDamage);
                
                TakeDamage(CurrentHealth); // Destroy attacker
            }
else
            {
                // Clash (Equal)
                GameLogManager.Instance?.Log("The attack was evenly matched! Both monsters survive the clash.");
            }
            
            HasAttackedThisTurn = true;
        }

        public bool CanAttackTower(Tower tower) => tower != null && !tower.IsDestroyed && tower.OwnerTeam != OwnerTeam && CurrentTile.DistanceTo(GameManager.Instance.GetTile(tower.GridX, tower.GridZ)) == 1;

        public void AttackTower(Tower tower) { 
            if (CanAttackTower(tower)) { 
                if (_animator != null) _animator.SetTrigger("attack");
                tower.TakeDamage(Attack); 
                HasAttackedThisTurn = true; 
                GameLogManager.Instance?.Log($"{OwnerTeam}'s {DisplayName} attacks the enemy Tower for {Attack} damage!");
            } 
        }

        public void ResetTurnState() 
        { 
            HasMovedThisTurn = false; 
            HasAttackedThisTurn = false; 
            if (RootTurns > 0) RootTurns--;
            
            if (_buffTurns > 0)
            {
                _buffTurns--;
                if (_buffTurns <= 0)
                {
                    _tempAtkBonus = 0;
                    _tempDefBonus = 0;
                    Debug.Log($"[Monster] {DisplayName}'s temporary buffs have expired.");
                }
            }
        }

        private void OnDeath()
        {
            GameLogManager.Instance?.Log($"{OwnerTeam}'s {DisplayName} has been destroyed.");
            
            if (_animator != null) _animator.SetTrigger("die");

            if (CurrentTile != null) CurrentTile.OccupyingMonster = null;
            if (SourceCard != null) GameManager.Instance.AddToGraveyard(SourceCard, OwnerTeam);
            
            Destroy(gameObject, 2f);
        }
    }
}
