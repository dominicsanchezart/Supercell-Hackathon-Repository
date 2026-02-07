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
	[SerializeField] private int _gold;



	private void Awake()
	{
		SetupCharacter();
	}

	public void SetupCharacter()
	{
		_health = _data.baseHealth;
		_energy = _data.baseEnergy;
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
		if (_health == 0)
			HandleDeath();
	}

	public void Heal(int amount)
	{
		_health = Mathf.Min(_health + amount, _data.baseHealth);
	}

	public int GetHealth()
	{
		return _health;
	}

	public void SpendEnergy(int amount)
	{
		_energy = Mathf.Max(_energy - amount, 0);
	}

	public void GainEnergy(int amount)
	{
		_energy = Mathf.Min(_energy + amount, _data.baseEnergy);
	}

	public void ResetEnergy()
	{
		_energy = _data.baseEnergy;
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
	}

	public void RemoveBlock(int amount)
	{
		_block = Mathf.Max(_block - amount, 0);
	}

	public void Empower(int amount)
	{
		_empower += amount;
		Debug.Log($"{_data.name} gains {amount} empower. Total empower: {_empower}");
	}

	public void ApplyBurn(int amount)
	{
		_burn += amount;
	}

	public void RemoveBurn(int amount)
	{
		_burn = Mathf.Max(_burn - amount, 0);
	}

	public void ApplyPoison(int amount)
	{
		_poison += amount;
	}

	public void RemovePoison(int amount)
	{
		_poison = Mathf.Max(_poison - amount, 0);
	}

	public void ApplyWeaken(int amount)
	{
		_weaken += amount;
	}

	public void RemoveWeaken(int amount)
	{
		_weaken = Mathf.Max(_weaken - amount, 0);
	}

	public void GainGold(int amount)
	{
		_gold += amount;
	}

	public bool TrySpendGold(int amount)
	{
		if (_gold - amount < 0)
			return false;
		
		_gold = Mathf.Max(_gold - amount, 0);
		return true;
	}

	public void HandleDeath()
	{
		Debug.Log($"{_data.name} has been defeated!");
	}

	public bool IsAlive()
	{
		return _health > 0;
	}

	public int GetModifiedDamage(int baseDamage)
	{
		int damage = baseDamage + _empower;

		if (_weaken > 0)
			damage -= _weaken;

		return Mathf.Max(damage, 1);
	}

	public void ResetBlock()
	{
		_block = 0;
	}

	public void ProcessStartOfTurnEffects()
	{
		// Burn is handled by Hand (sets cards on fire)
		// Poison is handled at end of turn
	}

	public void ProcessEndOfTurnEffects()
	{
		// Poison: deal damage equal to stacks, then decay by 1
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
		return consumed;
	}
}