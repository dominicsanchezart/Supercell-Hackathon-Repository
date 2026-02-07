using UnityEngine;

[CreateAssetMenu(fileName = "Character Data", menuName = "Scriptable Objects/Character Data")]
public class CharacterData : ScriptableObject
{
	[field: SerializeField] public int baseHealth { get; private set; }
	[field: SerializeField] public int baseEnergy { get; private set; }
	[field: SerializeField] public int baseDrawSize { get; private set; }

	[Header("Battle Settings")]
	[Tooltip("Delay in seconds between each action this character takes. Used by enemy AI.")]
	[field: SerializeField] public float attackDelay { get; private set; } = 0.8f;

	[Header("Visuals")]
	[field: SerializeField] public Sprite characterSprite { get; private set; }
	[field: SerializeField] public bool flipSpriteForRight { get; private set; }

	[Header("Battle Background (Optional)")]
	[Tooltip("Background sprite shown when fighting this character. Leave null to keep the current background.")]
	[field: SerializeField] public Sprite battleBackground { get; private set; }

	[Header("State Sprites (Optional)")]
	[Tooltip("Idle pose. Falls back to characterSprite if null.")]
	[field: SerializeField] public Sprite idleSprite { get; private set; }
	[Tooltip("Shown when playing an Attack card.")]
	[field: SerializeField] public Sprite activeActionSprite { get; private set; }
	[Tooltip("Shown when playing any non-Attack card.")]
	[field: SerializeField] public Sprite passiveActionSprite { get; private set; }
	[Tooltip("Shown when taking damage.")]
	[field: SerializeField] public Sprite damageTakenSprite { get; private set; }
	[Tooltip("How long action/damage pose sprites are shown before returning to idle.")]
	[field: SerializeField] public float poseDuration { get; private set; } = 0.4f;
}