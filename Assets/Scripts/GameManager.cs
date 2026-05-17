using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ForgottenDomain
{
    public enum TurnState { PlayerTurn, EnemyTurn }
    public enum InteractionState { None, Selected, Moving, Attacking, CastingSpell, SelectingSwapTarget, PlacingTrap }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public static readonly int GridWidth = 20, GridHeight = 20;
        public const float TileSize = 2f;

        private BattleTile[,] _tiles;
private HandManager _handManager;
        private AIManager _aiManager;
        private int _summonsUsedThisTurn;
        private HashSet<BattleTile> _highlightedTiles = new HashSet<BattleTile>();
        private List<BattleTile> _currentPathTiles = new List<BattleTile>();
        
        private List<CardData> _playerGraveyard = new List<CardData>();
        private List<CardData> _opponentGraveyard = new List<CardData>();

        private Monster _swapTarget1;
        private CardData _swapCard;
        private Team _swapTeam;

        private List<BattleTile> _activeTrapRadiusTiles = new List<BattleTile>();
        
        public int TurnNumber { get; private set; } = 1;
        public float TurnTimer { get; private set; } = 30f;
        public const float MaxTurnTime = 30f;

        public int PlayerGraveCount => _playerGraveyard.Count;
        public int OpponentGraveCount => _opponentGraveyard.Count;
        public List<CardData> GetGraveyard(Team team) => team == Team.Player ? _playerGraveyard : _opponentGraveyard;
        
        public InteractionState CurrentState { get; private set; } = InteractionState.None;
public Monster SelectedMonster { get; private set; }

        public Tower PlayerTower { get; set; }
        public Tower OpponentTower { get; set; }
public TurnState CurrentTurn { get; private set; } = TurnState.PlayerTurn;
        public UnityEvent<BattleTile> onTileClicked;
        public bool IsAutoPlay { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _tiles = new BattleTile[GridWidth, GridHeight];
            onTileClicked ??= new UnityEvent<BattleTile>();
            WireExistingTiles();
            PlaceTowers();
            SetupCamera();
        }

        private void Start() 
        { 
            _handManager = FindAnyObjectByType<HandManager>();
            _aiManager = FindAnyObjectByType<AIManager>();
        }

        public void AddToGraveyard(CardData card, Team owner)
        {
            if (card == null) return;
            if (owner == Team.Player) _playerGraveyard.Add(card);
            else _opponentGraveyard.Add(card);
            Debug.Log($"[Graveyard] {card.cardName} added to {owner}'s grave.");
        }

        private void Update()
        {
            if (TurnTimer > 0)
            {
                TurnTimer -= Time.deltaTime;
                if (TurnTimer <= 0)
                {
                    TurnTimer = 0;
                    EndTurnAuto();
                }
            }
        }

        private void EndTurnAuto()
        {
            if (CurrentTurn == TurnState.PlayerTurn) EndTurn();
            else EndEnemyTurn();
        }

        private void WireExistingTiles()
        {
            int found = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
            {
                if (!go.name.StartsWith("Tile_")) continue;
                var parts = go.name.Split('_');
                if (parts.Length < 3 || !int.TryParse(parts[1], out int x) || !int.TryParse(parts[2], out int z)) continue;
                if (!IsInBounds(x, z)) continue;
                var tile = go.GetComponent<BattleTile>() ?? go.AddComponent<BattleTile>();
                tile.coordinates = new Vector2Int(x, z);
                tile.OnTileClicked = new UnityEvent<BattleTile>();
                tile.OnTileClicked.AddListener(OnTileClicked);
                _tiles[x, z] = tile;
                found++;
            }
        }

        private void PlaceTowers()
        {
            foreach (var t in FindObjectsByType<Tower>())
            {
                if (t.OwnerTeam == Team.Player) 
                {
                    PlayerTower = t;
                    t.GridX = GridWidth / 2; t.GridZ = 0;
                }
                else 
                {
                    OpponentTower = t;
                    t.GridX = GridWidth / 2; t.GridZ = GridHeight - 1;
                }
            }
            if (PlayerTower == null) PlayerTower = CreateTower(Team.Player, GridWidth / 2, 0);
            if (OpponentTower == null) OpponentTower = CreateTower(Team.Opponent, GridWidth / 2, GridHeight - 1);
        }

        private Tower CreateTower(Team team, int x, int z)
        {
            var go = new GameObject($"Tower_{team}");
            go.transform.position = GridToWorld(x, z) + Vector3.up * 0.1f;
            go.transform.SetParent(transform);
            var t = go.AddComponent<Tower>();
            t.Initialize(team, go.transform.position, x, z);
            return t;
        }

        private void SetupCamera()
        {
            var cam = Camera.main; if (cam == null) return;
            cam.tag = "MainCamera"; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f); cam.fieldOfView = 55f;
            if (cam.GetComponent<CameraController>() == null) cam.gameObject.AddComponent<CameraController>();
        }

        public void OnTileClicked(BattleTile tile)
        {
            if (tile == null) return;
            onTileClicked?.Invoke(tile);
            
            if (CurrentState == InteractionState.PlacingTrap)
            {
                if (_highlightedTiles.Contains(tile))
                {
                    SetTrap(_handManager.SelectedCard, tile, Team.Player);
                    _handManager.PlayCard(_handManager.SelectedIndex);
                }
                DeselectMonster();
                ClearHighlights();
                CurrentState = InteractionState.None;
                return;
            }

            if (_handManager != null && _handManager.SelectedIndex != -1)
            {
                var card = _handManager.SelectedCard;
                if (card.IsMonsterCard) TrySummon(tile);
                else if (card.IsSpellCard) { if (TryCastSpell(card, tile, Team.Player)) _handManager.PlayCard(_handManager.SelectedIndex); }
                else if (card.IsTrapCard) { /* State-based placement */ }
                return;
            }

            switch (CurrentState)
            {
                case InteractionState.None:
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == Team.Player) SelectMonster(tile.OccupyingMonster);
                    break;
                case InteractionState.Selected:
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == Team.Player) SelectMonster(tile.OccupyingMonster);
                    else DeselectMonster();
                    break;
                case InteractionState.SelectingSwapTarget:
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == _swapTeam && tile.OccupyingMonster != _swapTarget1)
                    {
                        PerformSwap(_swapTarget1, tile.OccupyingMonster);
                        if (_swapTeam == Team.Player) _handManager.PlayCard(_handManager.SelectedIndex);
                        AddToGraveyard(_swapCard, _swapTeam);
                    }
                    CurrentState = InteractionState.None;
                    break;
                case InteractionState.Moving:
                    if (_currentPathTiles.Contains(tile)) SelectedMonster.MoveTo(tile);
                    DeselectMonster();
                    break;
                case InteractionState.Attacking:
                    if (tile.IsOccupied && SelectedMonster.CanAttackMonster(tile.OccupyingMonster)) SelectedMonster.AttackMonster(tile.OccupyingMonster);
                    else if (IsTowerAt(tile, out Tower tower) && SelectedMonster.CanAttackTower(tower)) SelectedMonster.AttackTower(tower);
                    DeselectMonster();
                    break;
            }
        }

        public void SetTrap(CardData card, BattleTile tile, Team owner)
        {
            if (tile.SetTrap != null || tile.IsOccupied) return;
            tile.SetTrap = card;
            tile.TrapOwner = owner;
            
            if (owner == Team.Player)
            {
                tile.ShowTrapMarker(card.cardName);
                ShowTrapRadiusForPlayer(tile, card.trapRadius);
                GameLogManager.Instance?.Log($"Player set a Trap: {card.cardName} at {tile.coordinates}");
            }
            else
            {
                GameLogManager.Instance?.Log("Opponent set a hidden Trap.");
            }
        }

        private void ShowTrapRadiusForPlayer(BattleTile center, int radius)
        {
            Color radiusColor = new Color(1f, 0.4f, 0f, 0.25f);
            for (int x = 0; x < GridWidth; x++)
            {
                for (int z = 0; z < GridHeight; z++)
                {
                    var t = _tiles[x, z];
                    if (t != null && center.DistanceTo(t) <= radius)
                    {
                        t.ShowTrapRadius(radiusColor);
                        _activeTrapRadiusTiles.Add(t);
                    }
                }
            }
        }

        private void PerformSwap(Monster m1, Monster m2)
        {
            var t1 = m1.CurrentTile;
            var t2 = m2.CurrentTile;
            
            t1.OccupyingMonster = m2;
            t2.OccupyingMonster = m1;
            
            m1.CurrentTile = t2;
            m2.CurrentTile = t1;
            
            m1.transform.position = t2.transform.position + Vector3.up * 0.1f;
            m2.transform.position = t1.transform.position + Vector3.up * 0.1f;
            
            GameLogManager.Instance?.Log($"Swapped {m1.DisplayName} and {m2.DisplayName}!");
        }

        private void TrySetTrap(CardData card, BattleTile tile)
        {
            SetTrap(card, tile, Team.Player);
        }

        public bool TryCastSpell(CardData card, BattleTile tile, Team team)
        {
            bool success = false;
            string targetName = tile.IsOccupied ? tile.OccupyingMonster.DisplayName : "Target";

            switch (card.spellEffectType)
            {
                case SpellEffectType.Heal:
                {
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == team)
                    {
                        tile.OccupyingMonster.Heal(card.spellValue);
                        success = true;
                        GameLogManager.Instance?.Log($"{team} healed {targetName} for {card.spellValue} HP.");
                    }
                    break;
                }
                case SpellEffectType.Damage:
                {
                    Team dmgTeam = (team == Team.Player) ? Team.Opponent : Team.Player;
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == dmgTeam)
                    {
                        tile.OccupyingMonster.TakeDamage(card.spellValue);
                        success = true;
                        GameLogManager.Instance?.Log($"{team} used {card.cardName} to deal {card.spellValue} damage to {targetName}.");
                    }
                    break;
                }
                case SpellEffectType.Buff:
                {
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == team)
                    {
                        tile.OccupyingMonster.ApplyBuff(card.spellAtkBuff, card.spellDefBuff, card.spellDuration);
                        success = true;
                        GameLogManager.Instance?.Log($"{team} buffed {targetName} with {card.cardName}.");
                    }
                    break;
                }
                case SpellEffectType.Debuff:
                {
                    Team debuffTeam = (team == Team.Player) ? Team.Opponent : Team.Player;
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == debuffTeam)
                    {
                        tile.OccupyingMonster.ApplyBuff(card.spellAtkBuff, card.spellDefBuff, card.spellDuration); // ApplyBuff handles negative values fine
                        success = true;
                        GameLogManager.Instance?.Log($"{team} debuffed {targetName} with {card.cardName}.");
                    }
                    break;
                }
                case SpellEffectType.Destroy:
                {
                    Team killTeam = (team == Team.Player) ? Team.Opponent : Team.Player;
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == killTeam)
                    {
                        tile.OccupyingMonster.TakeDamage(9999);
                        success = true;
                        GameLogManager.Instance?.Log($"{team} destroyed {targetName} with {card.cardName}!");
                    }
                    break;
                }
                case SpellEffectType.Swap:
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == team)
                    {
                        if (team == Team.Player)
                        {
                            _swapTarget1 = tile.OccupyingMonster;
                            _swapCard = card;
                            _swapTeam = team;
                            CurrentState = InteractionState.SelectingSwapTarget;
                            GameLogManager.Instance?.Log("Select second monster to swap.");
                            return false; 
                        }
                        else
                        {
                            // AI simple swap with a random other friendly monster
                            var others = GetAllMonsters().FindAll(m => m.OwnerTeam == team && m != tile.OccupyingMonster);
                            if (others.Count > 0)
                            {
                                PerformSwap(tile.OccupyingMonster, others[Random.Range(0, others.Count)]);
                                success = true;
                            }
                        }
                    }
                    break;
                case SpellEffectType.Equip:
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == team)
                    {
                        tile.OccupyingMonster.ApplyEquip(card.spellAtkBuff, card.spellDefBuff);
                        success = true;
                        GameLogManager.Instance?.Log($"{team} equipped {card.cardName} to {targetName}.");
                    }
                    break;
                case SpellEffectType.AOE_Destroy:
                {
                    int radius = card.spellValue;
                    Team targetT = card.spellTargetsEnemy ? (team == Team.Player ? Team.Opponent : Team.Player) : team;
                    List<Monster> targets = new List<Monster>();
                    foreach (var m in GetAllMonsters()) 
                    {
                        if (tile.DistanceTo(m.CurrentTile) <= radius && m.OwnerTeam == targetT) 
                            targets.Add(m);
                    }
                    foreach (var m in targets) m.TakeDamage(9999);
                    success = true;
                    GameLogManager.Instance?.Log($"{team} activated {card.cardName}! Destroyed targets in radius {radius}.");
                    break;
                }
                case SpellEffectType.Rebirth:
                    var grave = (team == Team.Player) ? _playerGraveyard : _opponentGraveyard;
                    if (!tile.IsOccupied && grave.Count > 0)
                    {
                        var resurrected = grave[Random.Range(0, grave.Count)];
                        grave.Remove(resurrected);
                        SpawnMonster(resurrected, tile, team);
                        success = true;
                        GameLogManager.Instance?.Log($"{team} used {card.cardName} to Rebirth {resurrected.cardName}!");
                    }
                    break;
        case SpellEffectType.Root:
                    Team enemyTeam = (team == Team.Player) ? Team.Opponent : Team.Player;
                    if (tile.IsOccupied && tile.OccupyingMonster.OwnerTeam == enemyTeam)
                    {
                        tile.OccupyingMonster.RootTurns = card.spellDuration;
                        success = true;
                        GameLogManager.Instance?.Log($"{team} used {card.cardName} to Root {targetName} for {card.spellDuration} turns.");
                    }
                    break;
                case SpellEffectType.DirectDamage:
                    var targetTower = (team == Team.Player) ? OpponentTower : PlayerTower;
                    if (targetTower != null) { 
                        targetTower.TakeDamage(card.spellValue); 
                        success = true; 
                        GameLogManager.Instance?.Log($"{team} used {card.cardName} to deal {card.spellValue} damage to Enemy Tower!");
                    }
                    break;
                case SpellEffectType.DrawCards:
                    if (team == Team.Player) { for (int i = 0; i < card.spellValue; i++) _handManager.DrawCard(); }
                    success = true;
                    GameLogManager.Instance?.Log($"{team} used {card.cardName} to draw {card.spellValue} cards.");
                    break;
            }
            if (success) 
            {
                SpellVisualManager.Instance?.PlaySpellSequence(card, tile.transform.position);
                AddToGraveyard(card, team);
            }
            return success;
        }

        public void CheckTrapsTrigger(Monster monster, BattleTile tile)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int z = 0; z < GridHeight; z++)
                {
                    var trapTile = _tiles[x, z];
                    if (trapTile != null && trapTile.SetTrap != null && trapTile.TrapOwner != monster.OwnerTeam)
                    {
                        if (trapTile.DistanceTo(tile) <= trapTile.SetTrap.trapRadius)
                        {
                            TriggerTrap(trapTile.SetTrap, trapTile, monster);
                        }
                    }
                }
            }
        }

        private void TriggerTrap(CardData trap, BattleTile trapTile, Monster triggeredBy)
        {
            GameLogManager.Instance?.Log($"!!! TRAP TRIGGERED: {trap.cardName} !!!");
            switch (trap.spellEffectType)
            {
                case SpellEffectType.Debuff:
                    foreach (var m in GetAllMonsters()) 
                        if (trapTile.DistanceTo(m.CurrentTile) <= trap.trapRadius && m.OwnerTeam != trapTile.TrapOwner) 
                            m.ApplyEquip(-trap.spellAtkBuff, -trap.spellDefBuff);
                    GameLogManager.Instance?.Log($"{trap.cardName} debuffed enemies in range {trap.trapRadius}.");
                    break;
                case SpellEffectType.Destroy:
                    foreach (var m in GetAllMonsters()) 
                        if (trapTile.DistanceTo(m.CurrentTile) <= trap.trapRadius && m.OwnerTeam != trapTile.TrapOwner) 
                            m.TakeDamage(9999);
                    GameLogManager.Instance?.Log($"{trap.cardName} destroyed all enemies in range {trap.trapRadius}!");
                    break;
                case SpellEffectType.Root:
                    foreach (var m in GetAllMonsters()) 
                        if (trapTile.DistanceTo(m.CurrentTile) <= trap.trapRadius && m.OwnerTeam != trapTile.TrapOwner) 
                            m.RootTurns = trap.spellDuration;
                    GameLogManager.Instance?.Log($"{trap.cardName} rooted all enemies in range {trap.trapRadius}.");
                    break;
            }
            trapTile.SetTrap = null; 
            trapTile.HideTrapMarker(); 
            AddToGraveyard(trap, trapTile.TrapOwner);
            if (trapTile.TrapOwner == Team.Player) RefreshAllTrapRadiuses();
        }

        private void RefreshAllTrapRadiuses()
        {
            foreach (var t in _activeTrapRadiusTiles) if (t != null) t.HideTrapRadius();
            _activeTrapRadiusTiles.Clear();

            for (int x = 0; x < GridWidth; x++)
            {
                for (int z = 0; z < GridHeight; z++)
                {
                    var t = _tiles[x, z];
                    if (t != null && t.SetTrap != null && t.TrapOwner == Team.Player)
                    {
                        ShowTrapRadiusForPlayer(t, t.SetTrap.trapRadius);
                    }
                }
            }
        }

        private void TrySummon(BattleTile tile)
        {
            // Removed _summonsUsedThisTurn >= 1 to allow multiple summons per turn
            if (tile.IsOccupied || tile.coordinates.y >= 2) return;
            var card = _handManager.SelectedCard; if (card == null) return;
            _handManager.PlayCard(_handManager.SelectedIndex);
            SpawnMonster(card, tile, Team.Player);
            _handManager.MarkSummonUsed();
            _summonsUsedThisTurn++;
        }

        public Monster SpawnMonster(CardData card, BattleTile tile, Team team)
        {
            if (card == null || tile == null || tile.IsOccupied) return null;
            var go = new GameObject(card.cardName);
            go.transform.position = tile.transform.position + Vector3.up * 0.1f;
            go.transform.SetParent(transform);
            var m = go.AddComponent<Monster>();
            m.Initialize(team, card, tile);
            tile.OccupyingMonster = m;
            
            // Log with specific mode since monsters default to ATK mode in Initialize
            GameLogManager.Instance?.Log($"{team} summoned {card.cardName} in Attack Mode!");
            return m;
        }

        public void SelectMonster(Monster monster)
        {
            DeselectMonster(); SelectedMonster = monster; CurrentState = InteractionState.Selected;
            GameLogManager.Instance?.Log($"Selected {monster.DisplayName} at {monster.CurrentTile.coordinates}.");
            FindAnyObjectByType<ActionMenu>()?.Show(monster);
        }

        public void DeselectMonster()
        {
            SelectedMonster = null; CurrentState = InteractionState.None; ClearHighlights();
            FindAnyObjectByType<ActionMenu>()?.Hide();
        }

        public void StartMove()
        {
            if (SelectedMonster == null || SelectedMonster.HasMovedThisTurn) return;
            CurrentState = InteractionState.Moving; _currentPathTiles = GetMovementRange(SelectedMonster);
            foreach (var t in _currentPathTiles) { t.ShowMoveRange(); _highlightedTiles.Add(t); }
        }

        public void StartAttack()
        {
            if (SelectedMonster == null || SelectedMonster.HasAttackedThisTurn) return;
            CurrentState = InteractionState.Attacking;
            (int, int)[] dirs = { (0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) };
            foreach (var (dx, dz) in dirs)
            {
                var t = GetTile(SelectedMonster.CurrentTile.coordinates.x + dx, SelectedMonster.CurrentTile.coordinates.y + dz);
                if (t != null) { t.ShowPlacement(); _highlightedTiles.Add(t); }
            }
        }

        public void ToggleAutoPlay()
        {
            IsAutoPlay = !IsAutoPlay;
            if (IsAutoPlay && CurrentTurn == TurnState.PlayerTurn) _aiManager?.StartTurn(Team.Player);
        }

        public void EndTurn()
        {
            if (CurrentTurn != TurnState.PlayerTurn) return;
            DeselectMonster(); ClearHighlights(); _summonsUsedThisTurn = 0;
            foreach (var m in GetAllMonsters()) if (m.OwnerTeam == Team.Player) m.ResetTurnState();
            CurrentTurn = TurnState.EnemyTurn;
            TurnTimer = MaxTurnTime;
            GameLogManager.Instance?.Log("--- Opponent's Turn ---");
            if (_aiManager != null) _aiManager.StartTurn(Team.Opponent); else EndEnemyTurn();
        }

        public void EndEnemyTurn()
        {
            if (CurrentTurn != TurnState.EnemyTurn) return;
            if (_aiManager != null) _aiManager.StopAllCoroutines();
            foreach (var m in GetAllMonsters()) if (m.OwnerTeam == Team.Opponent) m.ResetTurnState();
            CurrentTurn = TurnState.PlayerTurn; 
            _handManager?.ResetTurnState();
            TurnNumber++;
            TurnTimer = MaxTurnTime;
            GameLogManager.Instance?.Log($"--- Player's Turn (Turn {TurnNumber}) ---");
            if (IsAutoPlay) _aiManager?.StartTurn(Team.Player);
        }

        public void OnCardSelected(CardData card) 
        { 
            DeselectMonster(); 
            if (card == null) return;
            if (card.IsMonsterCard) HighlightValidSummonTiles(); 
            else if (card.IsTrapCard) 
            { 
                CurrentState = InteractionState.PlacingTrap; 
                HighlightValidTrapTiles(); 
            }
        }

        public void OnCardDeselected() 
        { 
            ClearHighlights(); 
            if (CurrentState == InteractionState.PlacingTrap) CurrentState = InteractionState.None;
        }

        private void HighlightValidTrapTiles()
        {
            ClearHighlights();
            // Traps can be placed on any empty tile that doesn't already have a trap
            for (int x = 0; x < GridWidth; x++)
            {
                for (int z = 0; z < GridHeight; z++)
                {
                    var t = _tiles[x, z];
                    if (t != null && !t.IsOccupied && t.SetTrap == null)
                    {
                        t.ShowPlacement();
                        _highlightedTiles.Add(t);
                    }
                }
            }
        }

        private void HighlightValidSummonTiles() { ClearHighlights(); for (int x = 0; x < GridWidth; x++) for (int z = 0; z < 2; z++) { var t = _tiles[x, z]; if (t != null && !t.IsOccupied) { t.ShowPlacement(); _highlightedTiles.Add(t); } } }
private void ClearHighlights() { foreach (var t in _highlightedTiles) if (t != null) { t.HidePlacement(); t.HideMoveRange(); } _highlightedTiles.Clear(); }
        private bool IsTowerAt(BattleTile tile, out Tower tower) { tower = null; if (tile == null) return false; if (PlayerTower?.GridX == tile.coordinates.x && PlayerTower?.GridZ == tile.coordinates.y) { tower = PlayerTower; return true; } if (OpponentTower?.GridX == tile.coordinates.x && OpponentTower?.GridZ == tile.coordinates.y) { tower = OpponentTower; return true; } return false; }
        public List<BattleTile> GetMovementRange(Monster monster) { var r = new List<BattleTile>(); if (monster?.CurrentTile == null) return r; int max = monster.MoveRange; var visited = new Dictionary<BattleTile, int>(); var q = new Queue<BattleTile>(); var s = monster.CurrentTile; visited[s] = 0; q.Enqueue(s); (int, int)[] dirs = { (0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) }; while (q.Count > 0) { var c = q.Dequeue(); int cost = visited[c]; foreach (var (dx, dz) in dirs) { int nx = c.coordinates.x + dx, nz = c.coordinates.y + dz; var n = GetTile(nx, nz); if (n == null || !n.IsWalkable) continue; int nc = cost + 1; if (nc > max) continue; if (!visited.ContainsKey(n) || nc < visited[n]) { visited[n] = nc; q.Enqueue(n); if (n != s) r.Add(n); } } } return r; }
        public List<Monster> GetAllMonsters() { var r = new List<Monster>(); for (int x = 0; x < GridWidth; x++) for (int z = 0; z < GridHeight; z++) if (_tiles[x, z]?.OccupyingMonster != null && _tiles[x, z].OccupyingMonster.IsAlive) r.Add(_tiles[x, z].OccupyingMonster); return r; }
        public Vector3 GridToWorld(int x, int z) => new((x - 9.5f) * TileSize, 0f, (z - 9.5f) * TileSize);
public bool IsInBounds(int x, int z) => x >= 0 && x < GridWidth && z >= 0 && z < GridHeight;
        public BattleTile GetTile(int x, int z) => IsInBounds(x, z) ? _tiles[x, z] : null;
    }
}