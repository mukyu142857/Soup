using System;

public enum ElementType
{
    None = 0,
    Wood = 1,
    Earth = 2,
    Water = 3,
    Fire = 4,
    Gold = 5
}

public enum ElementRelationship
{
    Disadvantage = -1,
    Neutral = 0,
    Advantage = 1
}

/// <summary>
/// Shared five-element combat rules:
/// Wood > Earth > Water > Fire > Gold > Wood.
/// </summary>
public static class ElementSystem
{
    public const float AdvantageMultiplier = 1.5f;
    public const float DisadvantageMultiplier = 0.75f;

    public static ElementRelationship GetRelationship(ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None || attacker == defender)
        {
            return ElementRelationship.Neutral;
        }

        if (Beats(attacker, defender)) return ElementRelationship.Advantage;
        if (Beats(defender, attacker)) return ElementRelationship.Disadvantage;
        return ElementRelationship.Neutral;
    }

    public static bool Beats(ElementType attacker, ElementType defender)
    {
        return (attacker == ElementType.Wood && defender == ElementType.Earth) ||
               (attacker == ElementType.Earth && defender == ElementType.Water) ||
               (attacker == ElementType.Water && defender == ElementType.Fire) ||
               (attacker == ElementType.Fire && defender == ElementType.Gold) ||
               (attacker == ElementType.Gold && defender == ElementType.Wood);
    }

    public static float GetDamageMultiplier(ElementType attacker, ElementType defender)
    {
        ElementRelationship relationship = GetRelationship(attacker, defender);
        if (relationship == ElementRelationship.Advantage) return AdvantageMultiplier;
        if (relationship == ElementRelationship.Disadvantage) return DisadvantageMultiplier;
        return 1f;
    }

    public static int CalculateDamage(int baseDamage, ElementType attacker, ElementType defender)
    {
        if (baseDamage <= 0) return 0;
        return (int)Math.Ceiling(baseDamage * GetDamageMultiplier(attacker, defender));
    }

    public static string GetDisplayName(ElementType element)
    {
        switch (element)
        {
            case ElementType.Wood: return "木";
            case ElementType.Earth: return "土";
            case ElementType.Water: return "水";
            case ElementType.Fire: return "火";
            case ElementType.Gold: return "金";
            default: return "无";
        }
    }

    public static string GetRelationshipLabel(ElementRelationship relationship)
    {
        if (relationship == ElementRelationship.Advantage) return "克制";
        if (relationship == ElementRelationship.Disadvantage) return "被克制";
        return "普通";
    }
}
