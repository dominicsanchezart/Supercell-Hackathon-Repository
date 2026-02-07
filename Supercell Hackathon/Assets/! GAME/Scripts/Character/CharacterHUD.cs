using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterHUD : MonoBehaviour
{
    [SerializeField] private CharacterInfo characterInfo;
    [SerializeField] private Slider healthSlider;
	[SerializeField] private Slider guardSlider;
	[SerializeField] private TextMeshProUGUI healthText;
	[SerializeField] private TextMeshProUGUI guardText;
	[SerializeField] private TextMeshProUGUI energyText;

	[Header("Status Effect Icons")]
	[SerializeField] private StatusEffectData statusEffectData;
	[SerializeField] private GameObject statusIconPrefab;
	[SerializeField] private Transform statusIconContainer;

	private readonly List<StatusEffectIcon> _activeIcons = new();
	private readonly List<StatusEffectIcon> _iconPool = new();



	private void OnEnable()
	{
		if (characterInfo != null)
			characterInfo.OnStatsChanged += Refresh;
	}

	private void OnDisable()
	{
		if (characterInfo != null)
			characterInfo.OnStatsChanged -= Refresh;
	}

	/// <summary>
	/// Assign a character at runtime (e.g. from Arena) and start listening.
	/// </summary>
	public void Bind(CharacterInfo info)
	{
		// Unsubscribe from previous
		if (characterInfo != null)
			characterInfo.OnStatsChanged -= Refresh;

		characterInfo = info;

		if (characterInfo != null)
		{
			characterInfo.OnStatsChanged += Refresh;
			Refresh();
		}
	}

	/// <summary>
	/// Legacy method kept for compatibility — prefer Bind() + event-driven updates.
	/// </summary>
	public void UpdateHUD(CharacterInfo info)
	{
		if (characterInfo != info)
			Bind(info);
		else
			Refresh();
	}

	private void Refresh()
	{
		if (characterInfo == null) return;

		healthSlider.maxValue = characterInfo._data.baseHealth;
		healthSlider.value = characterInfo.GetHealth();
		healthText.text = $"{characterInfo.GetHealth()}";

		if (guardSlider != null)
		{
			guardSlider.maxValue = characterInfo._data.baseHealth;
			guardSlider.value = characterInfo.GetBlock();
			guardSlider.gameObject.SetActive(characterInfo.GetBlock() > 0);
		}

		guardText.text = characterInfo.GetBlock() > 0 ? $"{characterInfo.GetBlock()}" : "";
		energyText.text = $"{characterInfo.GetEnergy()}/3";

		RefreshStatusIcons();
	}

	private void RefreshStatusIcons()
	{
		if (statusEffectData == null || statusIconPrefab == null || statusIconContainer == null)
			return;

		// Return all active icons to pool
		foreach (var icon in _activeIcons)
		{
			icon.gameObject.SetActive(false);
			_iconPool.Add(icon);
		}
		_activeIcons.Clear();

		// Show an icon for each active status effect
		TryShowStatus(StatusEffects.Burn, characterInfo.GetBurn());
		TryShowStatus(StatusEffects.Poison, characterInfo.GetPoison());
		TryShowStatus(StatusEffects.Weakened, characterInfo.GetWeaken());
		TryShowStatus(StatusEffects.Fury, characterInfo.GetFury());
		TryShowStatus(StatusEffects.Energized, characterInfo.GetEnergized());
		TryShowStatus(StatusEffects.Dodge, characterInfo.GetDodge());

		// Empower is a separate buff, not in the StatusEffects enum
		if (characterInfo.GetEmpower() > 0 && statusEffectData.empowerIcon != null)
		{
			var icon = GetOrCreateIcon();
			icon.Setup(statusEffectData.empowerIcon, statusEffectData.empowerTint, characterInfo.GetEmpower());
		}
	}

	private void TryShowStatus(StatusEffects status, int stacks)
	{
		if (stacks <= 0) return;

		if (statusEffectData.TryGetVisual(status, out Sprite sprite, out Color tint))
		{
			var icon = GetOrCreateIcon();
			tint.a = 1f;
			icon.Setup(sprite, tint, stacks);
		}
	}

	private StatusEffectIcon GetOrCreateIcon()
	{
		StatusEffectIcon icon;

		if (_iconPool.Count > 0)
		{
			icon = _iconPool[_iconPool.Count - 1];
			_iconPool.RemoveAt(_iconPool.Count - 1);
		}
		else
		{
			GameObject go = Instantiate(statusIconPrefab, statusIconContainer);
			icon = go.GetComponent<StatusEffectIcon>();
		}

		icon.gameObject.SetActive(true);
		_activeIcons.Add(icon);
		return icon;
	}
}