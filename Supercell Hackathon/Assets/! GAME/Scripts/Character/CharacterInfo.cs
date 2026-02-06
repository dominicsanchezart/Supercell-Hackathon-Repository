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
	[SerializeField] private int _gold;



	private void Awake()
	{
		SetupCharacter();
	}

	public void SetupCharacter()
	{
		_health = _data.maxHealth;
		_energy = _data.maxEnergy;
	}

	public void TakeDamage(int amount)
	{
		_health = Mathf.Max(_health - amount, 0);
		if (_health == 0)
			HandleDeath();
	}

	public void Heal(int amount)
	{
		_health = Mathf.Min(_health + amount, _data.maxHealth);
	}

	public int GetHealth()
	{
		return _health;
	}

	public int GetMaxHealth()
	{
		return _data.maxHealth;
	}

	public void SpendEnergy(int amount)
	{
		_energy = Mathf.Max(_energy - amount, 0);
	}

	public void GainEnergy(int amount)
	{
		_energy = Mathf.Min(_energy + amount, _data.maxEnergy);
	}

	public void ResetEnergy()
	{
		_energy = _data.maxEnergy;
	}

	public int GetEnergy()
	{
		return _energy;
	}

	public int GetMaxEnergy()
	{
		return _data.maxEnergy;
	}

	public Inventory GetInventory()
	{
		return _inventory;
	}

	public int GetBlock()
	{
		return _block;
	}

	public void GainBlock(CharacterInfo target, int amount)
	{
		_block += amount;
	}

	public void RemoveBlock(int amount)
	{
		_block = Mathf.Max(_block - amount, 0);
	}

	public void Empower(int amount)
	{
		// Implement empower logic
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
		
	}
}