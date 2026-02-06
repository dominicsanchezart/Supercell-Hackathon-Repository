using UnityEngine;

public class CharacterInfo : MonoBehaviour
{
	private Character _character;
	private Inventory _inventory;
    private int _health;
	private int _maxHealth;
	private int _energy;
	private int _maxEnergy;



	private void Awake()
	{
		_character = GetComponent<Character>();
		_inventory = GetComponent<Inventory>();
		_health = _character.health;
		_maxHealth = _character.maxHealth;
	}

	public int GetHealth()
	{
		return _health;
	}

	public int GetMaxHealth()
	{
		return _maxHealth;
	}

	public int GetEnergy()
	{
		return _energy;
	}

	public int GetMaxEnergy()
	{
		return _maxEnergy;
	}

	public Inventory GetInventory()
	{
		return _inventory;
	}
}