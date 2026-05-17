using UnityEngine;

namespace ForgottenDomain
{
    /// <summary>Monster, Magic, or Trap.</summary>
    public enum CardType
    {
        Monster,
        Magic,
        Trap
    }

    /// <summary>Scientific domain — determines visual theme and synergies.</summary>
    public enum ScienceDomain
    {
        Quantum,
        Nanite,
        Entropy,
        Relativity,
        Thermodynamics
    }

    /// <summary>What a Magic card does when cast.</summary>
    public enum SpellEffectType
    {
        None,            // Monster card — no spell effect
        Heal,            // Restore HP to target monster
        Damage,          // Deal damage to enemy monster
        DirectDamage,    // Deal damage to enemy tower (life points)
        Equip,           // Equip to friendly monster: permanent stat boost
        Buff,            // Temporary ATK/DEF boost (expires after N turns)
        Debuff,          // Temporary ATK/DEF reduction on enemy (expires after N turns)
        Swap,            // Swap positions of two friendly monsters
        Destroy,         // Destroy target monster (may have tile condition)
        Root,            // Reduce enemy move range to 0 for N turns
        Rebirth,         // Bring back a monster from the graveyard
        AOE_Destroy,     // Destroy monsters in a radius
        Teleport,        // Move a monster to any valid tile
        DrawCards        // Draw extra cards
    }

    /// <summary>
    /// ScriptableObject representing a single card in EternalForge Tactics.
    /// Created automatically by BattlefieldSetupEditor — no manual setup needed.
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "Forgotten Domain/CardData", order = 1)]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardName = "Unnamed";
        public CardType cardType = CardType.Monster;
        public ScienceDomain domain = ScienceDomain.Quantum;
        [TextArea(2, 3)]
        public string description = "";

        [Header("Cost")]
        [Range(0, 10)]
        public int manaCost = 1;

        [Header("Stats (Monster only)")]
        public int health = 800;
        public int attack = 400;
        public int defense = 200;
        [Range(1, 3)]
        public int moveRange = 1;

        [Header("Level")]
        [Range(1, 8)]
        public int level = 1;

        /// <summary>Movement range derived from level.</summary>
        public int MovementRange => level <= 4 ? 1 : (level <= 6 ? 2 : 3);

        [Header("Visual")]
        public Color summonColor = new Color(0.2f, 0.5f, 1f);

        [Header("Model")]
        [Tooltip("Path under Resources/ to a GLB model. Leave empty to use default prefab.")]
        public string modelPath = "";

        // ──────────────────────────────────────────────────────
        //  SPELL FIELDS (Magic / Trap cards only)
        // ──────────────────────────────────────────────────────

        [Header("Spell Effect (Magic / Trap only)")]
        public SpellEffectType spellEffectType = SpellEffectType.None;

        [Tooltip("Damage or heal amount.")]
        public int spellValue = 0;

        [Tooltip("ATK modifier. Positive = buff, negative = debuff.")]
        public int spellAtkBuff = 0;

        [Tooltip("DEF modifier. Positive = buff, negative = debuff.")]
        public int spellDefBuff = 0;

        [Tooltip("How many turns the buff/debuff/root lasts. 0 = permanent (Equip).")]
        public int spellDuration = 1;

        [Tooltip("True = targets enemy. False = targets friendly.")]
        public bool spellTargetsEnemy = true;

        [Tooltip("Can this spell target a tower directly?")]
        public bool spellTargetsTower = false;

        [Header("Trap (Trap cards only)")]
        [Tooltip("Range of the trap effect in tiles.")]
        public int trapRadius = 3;

        [Tooltip("If true, the trap also benefits the owner's creatures that land on it.")]
        public bool trapBenefitsOwner = false;

        [Tooltip("How many turns the trap stays on the field after being set. 0 = one-time trigger.")]
        public int trapLifespan = 0;

        // ── Convenience helpers ──

        public bool IsMonsterCard => cardType == CardType.Monster;
        public bool IsMagicCard   => cardType == CardType.Magic;
        public bool IsTrapCard    => cardType == CardType.Trap;
        public bool IsSpellCard   => cardType == CardType.Magic || cardType == CardType.Trap;
        public bool IsEquipOrBuff => spellEffectType == SpellEffectType.Equip
                                  || spellEffectType == SpellEffectType.Buff;
        public bool IsHostile     => spellEffectType == SpellEffectType.Damage
                                  || spellEffectType == SpellEffectType.DirectDamage
                                  || spellEffectType == SpellEffectType.Debuff
                                  || spellEffectType == SpellEffectType.Destroy
                                  || spellEffectType == SpellEffectType.Root;
    }
}
