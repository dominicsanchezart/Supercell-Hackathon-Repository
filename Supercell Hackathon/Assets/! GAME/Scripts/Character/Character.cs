using UnityEngine;

public class Character : MonoBehaviour
{
    [field: SerializeField] public int health { get; private set; }
	[field: SerializeField] public int maxHealth { get; private set; }
}