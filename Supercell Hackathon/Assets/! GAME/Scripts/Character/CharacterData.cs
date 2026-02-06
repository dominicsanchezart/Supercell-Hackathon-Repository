using UnityEngine;

[CreateAssetMenu(fileName = "Character Data", menuName = "Scriptable Objects/Character Data")]
public class CharacterData : ScriptableObject
{
	[field: SerializeField] public int maxHealth { get; private set; }
	[field: SerializeField] public int maxEnergy { get; private set; }
	[field: SerializeField] public int baseDrawSize { get; private set; }
}