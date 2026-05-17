using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenDomain
{
    public class AIManager : MonoBehaviour
    {
        [SerializeField] private List<CardData> deckCards = new List<CardData>();
        [SerializeField] private float actionDelay = 1.0f;

        private Deck _deck;
        private List<CardData> _hand = new List<CardData>();
        private HandManager _handManager;

        public int DeckRemaining => _deck != null ? _deck.Remaining : 0;

        private GameManager GM => GameManager.Instance;

        private void Start()
        {
            _deck = new Deck();
            _deck.Initialize(deckCards);
            _handManager = FindAnyObjectByType<HandManager>();
            // Draw initial hand
            for (int i = 0; i < 4; i++) DrawCard();
        }

        public void StartTurn(Team team)
        {
            StopAllCoroutines();
            StartCoroutine(AITurnRoutine(team));
        }

        private IEnumerator AITurnRoutine(Team team)
        {
            yield return new WaitForSeconds(actionDelay);

            // 1. Draw card
            if (team == Team.Player && _handManager != null) _handManager.DrawCard();
            else DrawCard();
            
            yield return new WaitForSeconds(actionDelay);

            // 2. Assess field state
            List<Monster> myMonsters = GM.GetAllMonsters().FindAll(m => m.OwnerTeam == team);
            List<Monster> enemyMonsters = GM.GetAllMonsters().FindAll(m => m.OwnerTeam != team);
            Tower myTower = (team == Team.Player) ? GM.PlayerTower : GM.OpponentTower;
            Tower enemyTower = (team == Team.Player) ? GM.OpponentTower : GM.PlayerTower;

            // 3. Play Magic Cards strategically
            TryPlayMagicSmart(team, myMonsters, enemyMonsters);
            yield return new WaitForSeconds(actionDelay);

            // 4. Set Traps based on enemy positions
            TrySetTrapsSmart(team, enemyMonsters);
            yield return new WaitForSeconds(actionDelay);

            // 5. Strategic Summoning
            TrySummonMonsterSmart(team, myMonsters, enemyMonsters, myTower);
            yield return new WaitForSeconds(actionDelay);

            // 6. Tactical Movement and Combat
            yield return StartCoroutine(MoveAndAttackTactical(team, myTower, enemyTower));

            // 7. End turn
            if (team == Team.Opponent) GM.EndEnemyTurn();
            else GM.EndTurn();
        }

        private void TryPlayMagicSmart(Team team, List<Monster> myMonsters, List<Monster> enemyMonsters)
        {
            if (GM == null) return;
            
            List<CardData> hand = (team == Team.Player) ? GetPlayerHand() : new List<CardData>(_hand);
            var magicCards = hand.FindAll(c => c.IsMagicCard);

            foreach (var card in magicCards)
            {
                BattleTile targetTile = null;
                switch (card.spellEffectType)
                {
                    case SpellEffectType.Heal:
                        Monster bestHeal = null; float minHealthPct = 1.1f;
                        foreach (var m in myMonsters) {
                            float pct = (float)m.CurrentHealth / m.MaxHealth;
                            if (pct < 0.8f && pct < minHealthPct) { minHealthPct = pct; bestHeal = m; }
                        }
                        if (bestHeal != null) targetTile = bestHeal.CurrentTile;
                        break;

                    case SpellEffectType.Buff:
                    case SpellEffectType.Equip:
                        Monster strongest = null; int maxAtk = -1;
                        foreach (var m in myMonsters) if (m.Attack > maxAtk) { maxAtk = m.Attack; strongest = m; }
                        if (strongest != null) targetTile = strongest.CurrentTile;
                        break;

                    case SpellEffectType.Damage:
                    case SpellEffectType.Destroy:
                    case SpellEffectType.Root:
                    case SpellEffectType.Debuff:
                        Monster threat = GetHighestThreat(enemyMonsters, (team == Team.Player ? GM.PlayerTower : GM.OpponentTower));
                        if (threat != null) targetTile = threat.CurrentTile;
                        break;

                    case SpellEffectType.DirectDamage:
                    case SpellEffectType.DrawCards:
                        targetTile = GM.GetTile(GameManager.GridWidth / 2, GameManager.GridHeight / 2);
                        break;
                }

                if (targetTile != null)
                {
                    ExecutePlayCard(team, card, targetTile);
                    // Continue to next card in hand
                }
            }
        }

        private void TrySetTrapsSmart(Team team, List<Monster> enemies)
        {
            List<CardData> hand = (team == Team.Player) ? GetPlayerHand() : new List<CardData>(_hand);
            var traps = hand.FindAll(c => c.IsTrapCard);
            
            foreach (var trap in traps)
            {
                Tower myTower = (team == Team.Player) ? GM.PlayerTower : GM.OpponentTower;
                int zRow = (team == Team.Player) ? 2 : 17;
                
                int bestX = Random.Range(0, GameManager.GridWidth);
                if (enemies.Count > 0)
                {
                    Monster closest = GetHighestThreat(enemies, myTower);
                    if (closest != null) bestX = closest.CurrentTile.coordinates.x;
                }

                BattleTile tile = GM.GetTile(bestX, zRow);
                if (tile != null && tile.SetTrap == null && !tile.IsOccupied)
                {
                    ExecutePlayCard(team, trap, tile);
                }
            }
        }

        private void TrySummonMonsterSmart(Team team, List<Monster> myMonsters, List<Monster> enemyMonsters, Tower myTower)
        {
            // AutoPlay for player: respect the "one summon per turn" if needed, 
            // but the current GameManager allows multiple. 
            // However, HandManager.CanSummon is currently blocking AI because SelectedIndex is -1.
            
            List<CardData> hand = (team == Team.Player) ? GetPlayerHand() : new List<CardData>(_hand);
            var monsterCards = hand.FindAll(c => c.IsMonsterCard);

            foreach (var monsterCard in monsterCards)
            {
                // Decision: Defensive vs Offensive
                Monster threat = GetHighestThreat(enemyMonsters, myTower);
                BattleTile myTowerTile = (myTower != null) ? GM.GetTile(myTower.GridX, myTower.GridZ) : null;
                bool needDefense = threat != null && myTowerTile != null && myTowerTile.DistanceTo(threat.CurrentTile) < 8;

                int startZ = (team == Team.Player) ? 0 : 18;
                int endZ = (team == Team.Player) ? 2 : 20;

                List<BattleTile> validTiles = new List<BattleTile>();
                for (int x = 0; x < GameManager.GridWidth; x++)
                    for (int z = startZ; z < endZ; z++)
                    {
                        var t = GM.GetTile(x, z);
                        if (t != null && !t.IsOccupied) validTiles.Add(t);
                    }

                if (validTiles.Count == 0) break;

                BattleTile targetTile = null;
                if (needDefense && threat != null)
                {
                    targetTile = validTiles.Find(t => t.coordinates.x == threat.CurrentTile.coordinates.x);
                }
                
                if (targetTile == null) targetTile = validTiles[Random.Range(0, validTiles.Count)];

                ExecutePlayCard(team, monsterCard, targetTile, true);
            }
        }

        private IEnumerator MoveAndAttackTactical(Team team, Tower myTower, Tower enemyTower)
        {
            List<Monster> myMonsters = GM.GetAllMonsters().FindAll(m => m.OwnerTeam == team);
            List<Monster> enemies = GM.GetAllMonsters().FindAll(m => m.OwnerTeam != team);

            foreach (var m in myMonsters)
            {
                if (!m.IsAlive) continue;

                // 1. If can attack enemy tower, DO IT
                if (!m.HasAttackedThisTurn && m.CanAttackTower(enemyTower))
                {
                    m.AttackTower(enemyTower);
                    yield return new WaitForSeconds(actionDelay);
                    continue;
                }

                // 2. If can attack high threat enemy nearby, DO IT (only if we win or it's a strategic sacrifice)
                if (!m.HasAttackedThisTurn)
                {
                    Monster adjacentEnemy = GetAdjacentEnemy(m, enemies);
                    if (adjacentEnemy != null)
                    {
                        // AI logic: Only attack if we can destroy the enemy (ATK > DEF)
                        // Or if the enemy is so weak it doesn't matter.
                        if (m.Attack > adjacentEnemy.Defense)
                        {
                            m.AttackMonster(adjacentEnemy);
                            yield return new WaitForSeconds(actionDelay);
                            if (!m.IsAlive) continue;
                        }
                        else
                        {
                            // If we can't win, don't suicide attack unless we are at full health and target is critical (basic AI check)
                            // For now, let's just make it "Cowardly AI" - don't attack if you die.
                            Debug.Log($"[AI] {m.DisplayName} decides not to attack {adjacentEnemy.DisplayName} (Suicide check: {m.Attack} ATK vs {adjacentEnemy.Defense} DEF)");
                        }
                    }
                }

                // 3. Movement
                if (!m.HasMovedThisTurn)
                {
                    Monster threat = GetHighestThreat(enemies, myTower);
                    BattleTile moveTarget = null;

                    // If a threat is near our tower, intercept it
                    BattleTile myTowerTile = (myTower != null) ? GM.GetTile(myTower.GridX, myTower.GridZ) : null;
                    if (threat != null && myTowerTile != null && myTowerTile.DistanceTo(threat.CurrentTile) < 10)
                    {
                        moveTarget = GetTileToIntercept(m, threat.CurrentTile.coordinates);
                    }
                    else
                    {
                        // Otherwise, move towards enemy tower
                        moveTarget = GetTileTowards(m, new Vector2Int(enemyTower.GridX, enemyTower.GridZ));
                    }

                    if (moveTarget != null)
                    {
                        m.MoveTo(moveTarget);
                        yield return new WaitForSeconds(actionDelay);
                    }
                }

                // 4. Attack again if moved into range
                if (!m.HasAttackedThisTurn)
                {
                    if (m.CanAttackTower(enemyTower)) m.AttackTower(enemyTower);
                    else
                    {
                        Monster adjacentEnemy = GetAdjacentEnemy(m, enemies);
                        if (adjacentEnemy != null && m.Attack > adjacentEnemy.Defense) 
                        {
                            m.AttackMonster(adjacentEnemy);
                        }
                    }
                    yield return new WaitForSeconds(actionDelay);
                }
}
        }

        private List<CardData> GetPlayerHand()
        {
            List<CardData> h = new List<CardData>();
            for (int i = 0; i < _handManager.HandCount; i++) h.Add(_handManager.GetCard(i));
            return h;
        }

        private void ExecutePlayCard(Team team, CardData card, BattleTile tile, bool isSummon = false)
        {
            if (team == Team.Player)
            {
                int idx = -1;
                for (int i = 0; i < _handManager.HandCount; i++) if (_handManager.GetCard(i) == card) { idx = i; break; }
                if (idx != -1)
                {
                    _handManager.AISetSelection(idx);
                    // The GM.OnTileClicked will handle the logic for Summons/Spells/Traps.
                    GM.OnTileClicked(tile);
                }
            }
            else
            {
                if (isSummon) 
                { 
                    GM.SpawnMonster(card, tile, team); 
                    _hand.Remove(card); 
                }
                else if (card.IsTrapCard) 
                { 
                    GM.SetTrap(card, tile, team);
                    _hand.Remove(card); 
                }
                else if (GM.TryCastSpell(card, tile, team)) 
                {
                    _hand.Remove(card); 
                }
            }
        }

        private Monster GetHighestThreat(List<Monster> enemies, Tower myTower)
        {
            Monster highest = null; float maxScore = -1;
            Vector2Int towerPos = new Vector2Int(myTower.GridX, myTower.GridZ);
            foreach (var e in enemies)
            {
                float dist = Vector2Int.Distance(e.CurrentTile.coordinates, towerPos);
                float score = (e.Attack * 0.5f) + (100f / (dist + 1));
                if (score > maxScore) { maxScore = score; highest = e; }
            }
            return highest;
        }

        private Monster GetAdjacentEnemy(Monster me, List<Monster> enemies)
        {
            foreach (var e in enemies) if (me.CurrentTile.DistanceTo(e.CurrentTile) == 1) return e;
            return null;
        }

        private void DrawCard()
        {
            if (_hand.Count >= 6) return;
            var card = _deck.Draw();
            if (card != null) _hand.Add(card);
        }

        private BattleTile GetTileTowards(Monster m, Vector2Int target)
{
            var range = GM.GetMovementRange(m);
            BattleTile best = null; float minD = float.MaxValue;
            foreach (var t in range)
            {
                float d = Vector2Int.Distance(t.coordinates, target);
                if (d < minD) { minD = d; best = t; }
            }
            return best;
        }

        private BattleTile GetTileToIntercept(Monster m, Vector2Int target)
        {
            var range = GM.GetMovementRange(m);
            BattleTile best = null; float minD = float.MaxValue;
            foreach (var t in range)
            {
                // Try to get adjacent to the target
                float d = Vector2Int.Distance(t.coordinates, target);
                if (d < minD) { minD = d; best = t; }
            }
            return best;
        }
}

}
