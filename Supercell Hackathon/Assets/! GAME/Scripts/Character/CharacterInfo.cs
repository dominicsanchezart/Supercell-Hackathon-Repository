using System;
using UnityEngine;

public class CharacterInfo : MonoBehaviour
{
	public CharacterData _data;
	[SerializeField] private Inventory _inventory;
	[Tooltip("If true, this character's stats will be overridden by the active PatronData at battle start.")]
	[SerializeField] private bool _isPlayer;
    [SerializeField] private int _health;
	[SerializeField] private int _block;
	[SerializeField] private int _energy;
	[SerializeField] private int _burn;
	[SerializeField] private int _poison;
	[SerializeField] private int _weaken;
	[SerializeField] private int _empower;
	[SerializeField] private int _fury;
	[SerializeField] private int _energized;
	[SerializeField] private int _dodge;
	[SerializeField] private int _gold;

	/// <summary>
	/// Fired whenever any stat changes. HUD subscribes to this.
	/// </summary>
	public event Action OnStatsChanged;

	/// <summary>
	/// Fired when this character takes damage (after block absorption).
	/// </summary>
	public event Action OnDamageTaken;

	/// <summary>
	/// Fired when this character plays a card. Passes the full CardData so listeners
	/// can read type, faction, etc.
	/// </summary>
	public event Action<CardData> OnCardPlayed;

	/// <summary>
	/// Fired when this character dies. Arena subscribes to trigger win/lose flow.
	/// </summary>
	public event Action OnDeath;



	private void Awake()
	{
		// If this is the player character, override _data with the patron's CharacterData
		if (_isPlayer && RunManager.Instance != null && RunManager.Instance.State != null)
		{
			PatronData patron = RunManager.Instance.State.patronData;
			if (patron != null && patron.characterData != null)
				_data = patron.characterData;
		}

		SetupCharacter();
	}

	public void SetupCharacter()
	{
		_health = _data.baseHealth;
		_energy = _data.baseEnergy;

		// Assign the deck to the inventory using the appropriate source
		if (_inventory != null)
		{
			// Player: prefer the run state deck, then CharacterData deck
			if (_isPlayer && RunManager.Instance != null && RunManager.Instance.State != null
				&& RunManager.Instance.State.deck.Count > 0)
			{
				_inventory.AssignDeck(RunManager.Instance.State.deck);
			}
			// Both player and enemy: fall back to CharacterData deck
			else if (_data.deck != null && _data.deck.cards != null && _data.deck.cards.Count > 0)
			{
				_inventory.AssignDeck(_data.deck.cards);
			}
		}

		NotifyStatsChanged();
	}

	private void NotifyStatsChanged()
	{
		OnStatsChanged?.Invoke();
	}

	public void NotifyCardPlayed(CardData data)
	{
		OnCardPlayed?.Invoke(data);
	}

	public void TakeDamage(int amount)
	{
		if (_block > 0)
		{
			int absorbed = Mathf.Min(_block, amount);
			_block -= absorbed;
			amount -= absorbed;
		}

		_health = Mathf.Max(_health - amount, 0);
		Debug.Log($"{_data.name} takes {amount} damage. HP: {_health}/{_data.baseHealth} Block: {_block}");
		NotifyStatsChanged();
		OnDamageTaken?.Invoke();
		if (_health == 0)
			HandleDeath();
	}

	public void Heal(int amount)
	{
		_health = Mathf.Min(_health + amount, _data.baseHealth);
		NotifyStatsChanged();
	}

	public int GetHealth()
	{
		return _health;
	}

	/// <summary>
	/// Sets health directly (e.g. syncing run HP into battle).
	/// Clamped between 0 and baseHealth.
	/// </summary>
	public void SetHealth(int value)
	{
		_health = Mathf.Clamp(value, 0, _data.baseHealth);
		NotifyStatsChanged();
	}

	/// <summary>
	/// Returns how much HP has been lost (maxHP - currentHP).
	/// </summary>
	public int GetLostHP()
	{
		return _data.baseHealth - _health;
	}

	public void SpendEnergy(int amount)
	{
		_energy = Mathf.Max(_energy - amount, 0);
		NotifyStatsChanged();
	}

	public void GainEnergy(int amount)
	{
		_energy += amount;
		NotifyStatsChanged();
	}

	public void ResetEnergy()
	{
		_energy = _data.baseEnergy;
		NotifyStatsChanged();
	}

	public int GetEnergy()
	{
		return _energy;
	}

	public Inventory GetInventory()
	{
		return _inventory;
	}

	public int GetBlock()
	{
		return _block;
	}

	public void GainBlock(int amount)
	{
		_block = Mathf.Min(_block + amount, _data.baseHealth);
		Debug.Log($"{_data.name} gains {amount} block. Total block: {_block}");
		NotifyStatsChanged();
	}

	public void RemoveBlock(int amount)
	{
		_block = Mathf.Max(_block - amount, 0);
		NotifyStatsChanged();
	}

	public void Empower(int amount)
	{
		_empower += amount;
		Debug.Log($"{_data.name} gains {amount} empower. Total empower: {_empower}");
		NotifyStatsChanged();
	}

	public int GetEmpower()
	{
		return _empower;
	}

	public void ApplyBurn(int amount)
	{
		_burn += amount;
		NotifyStatsChanged();
	}

	public void RemoveBurn(int amount)
	{
		_burn = Mathf.Max(_burn - amount, 0);
		NotifyStatsChanged();
	}

	public void ApplyPoison(int amount)
	{
		_poison += amount;
		NotifyStatsChanged();
	}

	public void RemovePoison(int amount)
	{
		_poison = Mathf.Max(_poison - amount, 0);
		NotifyStatsChanged();
	}

	public int GetPoison()
	{
		return _poison;
	}

	public void ApplyWeaken(int amount)
	{
		_weaken += amount;
		NotifyStatsChanged();
	}

	public void RemoveWeaken(int amount)
	{
		_weaken = Mathf.Max(_weaken - amount, 0);
		NotifyStatsChanged();
	}

	public int GetWeaken()
	{
		return _weaken;
	}

	public void ApplyFury(int amount)
	{
		_fury += amount;
		Debug.Log($"{_data.name} gains {amount} fury. Total fury: {_fury}");
		NotifyStatsChanged();
	}

	public void RemoveFury(int amount)
	{
		_fury = Mathf.Max(_fury - amount, 0);
		NotifyStatsChanged();
	}

	public int GetFury()
	{
		return _fury;
	}

	public void ApplyEnergized(int amount)
	{
		_energized += amount;
		_energy += amount;
		Debug.Log($"{_data.name} gains {amount} energized. Energy: {_energy}, Energized stacks: {_energized}");
		NotifyStatsChanged();
	}

	public int GetEnergized()
	{
		return _energized;
	}

	public void ApplyDodge(int amount)
	{
		_dodge += amount;
		Debug.Log($"{_data.name} gains {amount} dodge. Total dodge: {_dodge}");
		NotifyStatsChanged();
	}

	public void RemoveDodge(int amount)
	{
		_dodge = Mathf.Max(_dodge - amount, 0);
		NotifyStatsChanged();
	}

	public int GetDodge()
	{
		return _dodge;
	}

	/// <summary>
	/// Returns the current stack count for the given status effect.
	/// </summary>
	public int GetStatusEffectStacks(StatusEffectType type)
	{
		switch (type)
		{
			case StatusEffectType.Burn:      return _burn;
			case StatusEffectType.Poison:    return _poison;
			case StatusEffectType.Weaken:    return _weaken;
			case StatusEffectType.Empower:   return _empower;
			case StatusEffectType.Fury:      return _fury;
			case StatusEffectType.Energized: return _energized;
			case StatusEffectType.Dodge:     return _dodge;
			default:                         return 0;
		}
	}

	/// <summary>
	/// Resets the given status effect stacks to zero.
	/// </summary>
	public void ResetStatusEffect(StatusEffectType type)
	{
		switch (type)
		{
			case StatusEffectType.Burn:      _burn = 0; break;
			case StatusEffectType.Poison:    _poison = 0; break;
			case StatusEffectType.Weaken:    _weaken = 0; break;
			case StatusEffectType.Empower:   _empower = 0; break;
			case StatusEffectType.Fury:      _fury = 0; break;
			case StatusEffectType.Energized: _energized = 0; break;
			case StatusEffectType.Dodge:     _dodge = 0; break;
		}
		NotifyStatsChanged();
	}

	public int GetModifiedBlock(int baseBlock)
	{
		return baseBlock + _dodge;
	}

	public void GainGold(int amount)
	{
		_gold += amount;
		NotifyStatsChanged();
	}

	public bool TrySpendGold(int amount)
	{
		if (_gold - amount < 0)
			return false;
		
		_gold = Mathf.Max(_gold - amount, 0);
		NotifyStatsChanged();
		return true;
	}

	public void HandleDeath()
	{
		Debug.Log($"{_data.name} has been defeated!");
		OnDeath?.Invoke();
	}

	public bool IsAlive()
	{
		return _health > 0;
	}

	public int GetModifiedDamage(int baseDamage)
	{
		int damage = baseDamage + _empower + _fury;

		if (_weaken > 0)
			damage -= _weaken;

		return Mathf.Max(damage, 1);
	}

	public void ResetBlock()
	{
		_block = 0;
		NotifyStatsChanged();
	}

	public void ProcessStartOfTurnEffects()
	{
		// Burn is handled by Hand (sets cards on fire)
		// Poison is handled at end of turn
	}

	public void ProcessEndOfTurnEffects()
	{
		// Poison: deal damage equal to stacks, then clear
		// NOTE: Poison intentionally bypasses block (direct HP damage, like Slay the Spire)
		if (_poison > 0)
		{
			int poisonDamage = _poison;
			_health = Mathf.Max(_health - poisonDamage, 0);
			_poison = 0;
			Debug.Log($"{_data.name} takes {poisonDamage} poison damage. Poison cleared.");

			if (_health == 0) HandleDeath();
		}

		// Weaken resets at end of turn
		_weaken = 0;

		_empower = 0;

		// Fury resets at end of turn
		_fury = 0;

		// Energized: remove bonus energy at end of turn
		if (_energized > 0)
		{
			_energy = Mathf.Max(_energy - _energized, 0);
			Debug.Log($"{_data.name} loses {_energized} energized energy. Energy: {_energy}");
			_energized = 0;
		}

		// Dodge resets at end of turn
		_dodge = 0;

		NotifyStatsChanged();
	}

	public int GetBurn()
	{
		return _burn;
	}

	/// <summary>
	/// Consumes burn stacks (used by Hand to set cards on fire).
	/// Returns the number of stacks actually consumed.
	/// </summary>
	public int ConsumeBurn(int maxCards)
	{
		int consumed = Mathf.Min(_burn, maxCards);
		_burn -= consumed;
		Debug.Log($"{_data.name} burn ignites {consumed} cards. Burn remaining: {_burn}");
		NotifyStatsChanged();
		return consumed;
	}
}