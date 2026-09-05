using System.Collections.Generic;
using UnityEngine;

public enum CardQuality
{
    Basic,
    Refined,
    Rare
}

public enum CardSpecialEffect
{
    None,
    DrawCards,
    ReduceNextEnemyAttack,
    TrueDamage,
    AllEnemyDamage
}

/// <summary>Immutable runtime definition. Saves only store the stable Id.</summary>
public sealed class CardDefinition
{
    public readonly string Id;
    public readonly string Name;
    public readonly ElementType Element;
    public readonly CardQuality Quality;
    public readonly int Cost;
    public readonly int Damage;
    public readonly int Block;
    public readonly CardSpecialEffect Effect;
    public readonly int EffectValue;
    public readonly int RewardWeight;
    public readonly bool IsInitiallyOwned;
    public readonly bool IsRewardCard;
    public readonly Color DisplayColor;
    public readonly string Description;

    public CardDefinition(
        string id,
        string name,
        ElementType element,
        CardQuality quality,
        int damage,
        int cost,
        int block,
        CardSpecialEffect effect,
        int effectValue,
        int rewardWeight,
        bool isInitiallyOwned,
        Color displayColor)
    {
        Id = id;
        Name = name;
        Element = element;
        Quality = quality;
        Damage = Mathf.Max(0, damage);
        Cost = Mathf.Max(0, cost);
        Block = Mathf.Max(0, block);
        Effect = effect;
        EffectValue = Mathf.Max(0, effectValue);
        RewardWeight = Mathf.Max(0, rewardWeight);
        IsInitiallyOwned = isInitiallyOwned;
        IsRewardCard = element != ElementType.None && RewardWeight > 0;
        DisplayColor = displayColor;
        Description = BuildDescription();
    }

    private string BuildDescription()
    {
        List<string> parts = new List<string>();
        if (Damage > 0)
        {
            parts.Add("造成" + Damage + "点" + ElementSystem.GetDisplayName(Element) + "属性伤害");
        }
        if (Block > 0)
        {
            parts.Add("获得" + Block + "点格挡");
        }

        switch (Effect)
        {
            case CardSpecialEffect.DrawCards:
                parts.Add("抽" + EffectValue + "张牌");
                break;
            case CardSpecialEffect.ReduceNextEnemyAttack:
                parts.Add("敌人下回合攻击-" + EffectValue);
                break;
            case CardSpecialEffect.TrueDamage:
                parts.Add("造成" + EffectValue + "点真实伤害");
                break;
            case CardSpecialEffect.AllEnemyDamage:
                parts.Add("对全体敌人造成" + EffectValue + "点伤害");
                break;
        }

        return parts.Count > 0 ? string.Join("；", parts.ToArray()) : "无效果";
    }
}

/// <summary>Central card database for the 23 cards in the current design table.</summary>
public static class CardCatalog
{
    private static readonly Dictionary<string, CardDefinition> Cards = new Dictionary<string, CardDefinition>();
    private static readonly Dictionary<string, string> LegacyIdAliases = new Dictionary<string, string>();
    private static readonly Dictionary<ElementType, List<CardDefinition>> RewardPools =
        new Dictionary<ElementType, List<CardDefinition>>();

    static CardCatalog()
    {
        Color wood = new Color(0.25f, 0.50f, 0.28f, 1f);
        Color earth = new Color(0.48f, 0.34f, 0.22f, 1f);
        Color water = new Color(0.20f, 0.43f, 0.66f, 1f);
        Color fire = new Color(0.68f, 0.24f, 0.12f, 1f);
        Color gold = new Color(0.86f, 0.72f, 0.38f, 1f);
        Color defend = new Color(0.18f, 0.34f, 0.49f, 1f);

        RegisterElementCard("card_01", "金牌", ElementType.Gold, CardQuality.Basic, 5, 1, 0, CardSpecialEffect.None, 0, 2, true, gold);
        RegisterElementCard("card_02", "精金斩", ElementType.Gold, CardQuality.Refined, 7, 1, 0, CardSpecialEffect.None, 0, 2, false, gold);
        RegisterElementCard("card_03", "金箭斩", ElementType.Gold, CardQuality.Rare, 5, 1, 0, CardSpecialEffect.DrawCards, 1, 1, false, gold);
        RegisterElementCard("card_04", "金毁击", ElementType.Gold, CardQuality.Basic, 10, 2, 0, CardSpecialEffect.None, 0, 1, false, gold);

        RegisterElementCard("card_05", "木牌", ElementType.Wood, CardQuality.Basic, 5, 1, 0, CardSpecialEffect.None, 0, 2, true, wood);
        RegisterElementCard("card_06", "精木击", ElementType.Wood, CardQuality.Refined, 7, 1, 0, CardSpecialEffect.None, 0, 2, false, wood);
        RegisterElementCard("card_07", "木藤缠", ElementType.Wood, CardQuality.Rare, 5, 1, 0, CardSpecialEffect.ReduceNextEnemyAttack, 3, 1, false, wood);
        RegisterElementCard("card_08", "木裂劈", ElementType.Wood, CardQuality.Basic, 10, 2, 0, CardSpecialEffect.None, 0, 1, false, wood);

        RegisterElementCard("card_09", "水牌", ElementType.Water, CardQuality.Basic, 5, 1, 0, CardSpecialEffect.None, 0, 2, true, water);
        RegisterElementCard("card_10", "精水刺", ElementType.Water, CardQuality.Refined, 7, 1, 0, CardSpecialEffect.None, 0, 2, false, water);
        RegisterElementCard("card_11", "水鳞盾", ElementType.Water, CardQuality.Rare, 5, 1, 4, CardSpecialEffect.None, 0, 1, false, water);
        RegisterElementCard("card_12", "水流斩", ElementType.Water, CardQuality.Basic, 10, 2, 0, CardSpecialEffect.None, 0, 1, false, water);

        RegisterElementCard("card_13", "火牌", ElementType.Fire, CardQuality.Basic, 5, 1, 0, CardSpecialEffect.None, 0, 2, true, fire);
        RegisterElementCard("card_14", "精火攻", ElementType.Fire, CardQuality.Refined, 7, 1, 0, CardSpecialEffect.None, 0, 2, false, fire);
        RegisterElementCard("card_15", "火焰块", ElementType.Fire, CardQuality.Rare, 5, 1, 0, CardSpecialEffect.TrueDamage, 2, 1, false, fire);
        RegisterElementCard("card_16", "火焰爆", ElementType.Fire, CardQuality.Basic, 10, 2, 0, CardSpecialEffect.None, 0, 1, false, fire);
        RegisterElementCard("card_17", "火凤凰", ElementType.Fire, CardQuality.Rare, 8, 2, 0, CardSpecialEffect.AllEnemyDamage, 4, 1, false, fire);

        RegisterElementCard("card_18", "土牌", ElementType.Earth, CardQuality.Basic, 5, 1, 0, CardSpecialEffect.None, 0, 2, true, earth);
        RegisterElementCard("card_19", "精土锤", ElementType.Earth, CardQuality.Refined, 7, 1, 0, CardSpecialEffect.None, 0, 2, false, earth);
        RegisterElementCard("card_20", "土岩甲", ElementType.Earth, CardQuality.Rare, 5, 1, 6, CardSpecialEffect.None, 0, 1, false, earth);
        RegisterElementCard("card_21", "土崩裂", ElementType.Earth, CardQuality.Basic, 10, 2, 0, CardSpecialEffect.None, 0, 1, false, earth);
        RegisterElementCard("card_22", "土灵盾", ElementType.Earth, CardQuality.Rare, 8, 2, 12, CardSpecialEffect.None, 0, 1, false, earth);

        Register(new CardDefinition("card_23", "基础防御", ElementType.None, CardQuality.Basic, 0, 1, 4, CardSpecialEffect.None, 0, 0, true, defend));

        AddLegacyAlias("strike_gold", "card_01");
        AddLegacyAlias("strike_wood", "card_05");
        AddLegacyAlias("strike_water", "card_09");
        AddLegacyAlias("strike_fire", "card_13");
        AddLegacyAlias("strike_earth", "card_18");
        AddLegacyAlias("defend_basic", "card_23");
        AddLegacyAlias("reward_gold_placeholder", "card_02");
        AddLegacyAlias("reward_wood_placeholder", "card_06");
        AddLegacyAlias("reward_water_placeholder", "card_10");
        AddLegacyAlias("reward_fire_placeholder", "card_14");
        AddLegacyAlias("reward_earth_placeholder", "card_19");
    }

    public static CardDefinition Get(string cardId)
    {
        string canonicalId = GetCanonicalCardId(cardId);
        if (string.IsNullOrEmpty(canonicalId)) return null;
        Cards.TryGetValue(canonicalId, out CardDefinition card);
        return card;
    }

    public static string GetCanonicalCardId(string cardId)
    {
        if (string.IsNullOrEmpty(cardId)) return cardId;
        return LegacyIdAliases.TryGetValue(cardId, out string canonicalId) ? canonicalId : cardId;
    }

    public static string GetQualityDisplayName(CardQuality quality)
    {
        switch (quality)
        {
            case CardQuality.Refined: return "精良";
            case CardQuality.Rare: return "稀有";
            default: return "基础";
        }
    }

    public static List<string> CreateInitialDeckIds()
    {
        return new List<string>
        {
            "card_01",
            "card_05",
            "card_09",
            "card_13",
            "card_18",
            "card_23",
            "card_23",
            "card_23",
            "card_23",
            "card_23"
        };
    }

    public static List<CardDefinition> GetRewardPool(ElementType element)
    {
        return RewardPools.TryGetValue(element, out List<CardDefinition> pool)
            ? pool
            : new List<CardDefinition>();
    }

    public static List<string> GenerateRewardChoices()
    {
        List<ElementType> elements = new List<ElementType>
        {
            ElementType.Gold,
            ElementType.Wood,
            ElementType.Water,
            ElementType.Fire,
            ElementType.Earth
        };

        for (int i = elements.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            ElementType temporary = elements[i];
            elements[i] = elements[randomIndex];
            elements[randomIndex] = temporary;
        }

        List<string> choices = new List<string>(3);
        for (int i = 0; i < 3; i++)
        {
            CardDefinition chosen = DrawWeighted(GetRewardPool(elements[i]));
            if (chosen != null) choices.Add(chosen.Id);
        }
        return choices;
    }

    private static CardDefinition DrawWeighted(List<CardDefinition> pool)
    {
        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++) totalWeight += pool[i].RewardWeight;
        if (totalWeight <= 0) return null;

        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= pool[i].RewardWeight;
            if (roll < 0) return pool[i];
        }
        return pool[pool.Count - 1];
    }

    private static void RegisterElementCard(
        string id,
        string name,
        ElementType element,
        CardQuality quality,
        int damage,
        int cost,
        int block,
        CardSpecialEffect effect,
        int effectValue,
        int rewardWeight,
        bool isInitiallyOwned,
        Color color)
    {
        CardDefinition card = new CardDefinition(
            id, name, element, quality, damage, cost, block, effect, effectValue,
            rewardWeight, isInitiallyOwned, color);
        Register(card);

        if (!RewardPools.TryGetValue(element, out List<CardDefinition> pool))
        {
            pool = new List<CardDefinition>();
            RewardPools.Add(element, pool);
        }
        pool.Add(card);
    }

    private static void Register(CardDefinition card)
    {
        Cards[card.Id] = card;
    }

    private static void AddLegacyAlias(string legacyId, string canonicalId)
    {
        LegacyIdAliases[legacyId] = canonicalId;
    }
}
