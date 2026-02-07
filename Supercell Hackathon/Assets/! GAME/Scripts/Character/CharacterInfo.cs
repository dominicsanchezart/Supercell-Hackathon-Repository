using System;
using UnityEngine;

public class CharacterInfo : MonoBehaviour
{
	public CharacterData _data;
	[SerializeField] private Inventory _inventory;
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
	/// Fired when this character dies. Arena subscribes to trigger win/lose flow.
	/// </summary>
	public event Action OnDeath;



	private void Awake()
	{
		SetupCharacter();
	}

	public void SetupCharacter()
	{
		_health = _data.baseHealth;
		_energy = _data.baseEnergy;
		NotifyStatsChanged();
	}

	private void NotifyStatsChanged()
	{
		OnStatsChanged?.Invoke();
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

	public void SpendEnergy(int amount)
	{
		_energy = Mathf.Max(_energy - amount, 0);
		NotifyStatsChanged();
	}

	public void GainEnergy(int amount)
	{
		_energy = Mathf.Min(_energy + amount, _data.baseEnergy);
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
		_block += amount;
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
		// Poison: deal damage equal to stacks, then decay by 1
		// NOTE: Poison intentionally bypasses block (direct HP damage, like Slay the Spire)
		if (_poison > 0)
		{
			int poisonDamage = _poison;
			_health = Mathf.Max(_health - poisonDamage, 0);
			_poison = Mathf.Max(_poison - 1, 0);
			Debug.Log($"{_data.name} takes {poisonDamage} poison damage. Poison remaining: {_poison}");

			if (_health == 0) HandleDeath();
		}

		if (_weaken > 0)
			_weaken = Mathf.Max(_weaken - 1, 0);

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