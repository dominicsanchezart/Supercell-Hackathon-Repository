using UnityEngine;

/// <summary>
/// Computes modified action values for a card based on the caster's buffs/debuffs.
/// Used both for display (colored description text) and for resolving final action values.
/// </summary>
public static class CardModifiers
{
	// TMP rich text color tags
	private const string BuffColor = "#22CC44";   // green
	private const string DebuffColor = "#DD3333";  // red

	/// <summary>
	/// Returns the final modified value for a single action slot,
	/// based on the action type and the caster's current buffs.
	/// </summary>
	public static int GetModifiedValue(CardActionType type, int baseValue, CharacterInfo caster)
	{
		if (caster == null)
			return baseValue;

		switch (type)
		{
			case CardActionType.Damage:
			case CardActionType.DamageAll:
				return caster.GetModifiedDamage(baseValue);

			case CardActionType.Guard:
				return caster.GetModifiedBlock(baseValue);

			default:
				return baseValue;
		}
	}

	/// <summary>
	/// Builds the full set of three modified values for a card.
	/// </summary>
	public static void GetModifiedValues(CardData data, CharacterInfo caster,
		out int mod1, out int mod2, out int mod3)
	{
		mod1 = GetModifiedValue(data.actionType1, data.action1Value, caster);
		mod2 = GetModifiedValue(data.actionType2, data.action2Value, caster);
		mod3 = GetModifiedValue(data.actionType3, data.action3Value, caster);
	}

	/// <summary>
	/// Returns the card description with values colored:
	///   green if above base, red if below, white/normal if unchanged.
	/// Uses TMP rich-text color tags.
	/// </summary>
	public static string GetColoredDescription(CardData data, CharacterInfo caster)
	{
		if (string.IsNullOrEmpty(data.actionDescription))
			return "";

		GetModifiedValues(data, caster, out int mod1, out int mod2, out int mod3);

		string desc = data.actionDescription;
		desc = desc.Replace("{action1Value}", ColoredValue(mod1, data.action1Value));
		desc = desc.Replace("{action2Value}", ColoredValue(mod2, data.action2Value));
		desc = desc.Replace("{action3Value}", ColoredValue(mod3, data.action3Value));

		return desc;
	}

	/// <summary>
	/// Returns the value string wrapped in a TMP color tag if it differs from base.
	/// </summary>
	private static string ColoredValue(int modified, int baseValue)
	{
		if (modified > baseValue)
			return $"<color={BuffColor}>{modified}</color>";
		if (modified < baseValue)
			return $"<color={DebuffColor}>{modified}</color>";
		return modified.ToString();
	}
}