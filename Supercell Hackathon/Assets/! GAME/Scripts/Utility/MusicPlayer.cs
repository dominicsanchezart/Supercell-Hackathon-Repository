using UnityEngine;

/// <summary>
/// Persistent music player singleton. Survives scene loads and crossfades between tracks.
/// 
/// Usage:
///   MusicPlayer.Instance.Play(myClip);
///   MusicPlayer.Instance.Stop();
/// 
/// Setup: Create a GameObject in your first scene, add this component,
/// and assign clips in the inspector or call Play() from code.
/// </summary>
public class MusicPlayer : Singleton<MusicPlayer>
{
	[Header("Settings")]
	[SerializeField] private AudioClip startingTrack;
	[SerializeField][Range(0f, 1f)] private float volume = 0.5f;
	[SerializeField] private float crossfadeDuration = 1f;

	private AudioSource sourceA;
	private AudioSource sourceB;
	private AudioSource activeSource;
	private Coroutine fadeCoroutine;

	protected override void Awake()
	{
		base.Awake();
		if (Instance != this) return;

		sourceA = gameObject.AddComponent<AudioSource>();
		sourceB = gameObject.AddComponent<AudioSource>();

		foreach (var src in new[] { sourceA, sourceB })
		{
			src.loop = true;
			src.playOnAwake = false;
			src.volume = 0f;
		}

		activeSource = sourceA;

		if (startingTrack != null)
			Play(startingTrack);
	}

	/// <summary>Play a track, crossfading from the current one.</summary>
	public void Play(AudioClip clip)
	{
		if (clip == null) return;
		if (activeSource.clip == clip && activeSource.isPlaying) return;

		AudioSource next = (activeSource == sourceA) ? sourceB : sourceA;
		next.clip = clip;
		next.volume = 0f;
		next.Play();

		if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
		fadeCoroutine = StartCoroutine(Crossfade(activeSource, next));

		activeSource = next;
	}

	/// <summary>Stop music with a fade out.</summary>
	public void Stop(float fadeDuration = -1f)
	{
		if (fadeDuration < 0f) fadeDuration = crossfadeDuration;

		if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
		fadeCoroutine = StartCoroutine(FadeOut(activeSource, fadeDuration));
	}

	/// <summary>Change volume at runtime.</summary>
	public void SetVolume(float newVolume)
	{
		volume = Mathf.Clamp01(newVolume);
		if (activeSource != null && activeSource.isPlaying)
			activeSource.volume = volume;
	}

	private System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to)
	{
		float t = 0f;
		float fromStart = from.volume;

		while (t < crossfadeDuration)
		{
			t += Time.unscaledDeltaTime;
			float progress = t / crossfadeDuration;

			from.volume = Mathf.Lerp(fromStart, 0f, progress);
			to.volume = Mathf.Lerp(0f, volume, progress);

			yield return null;
		}

		from.volume = 0f;
		from.Stop();
		to.volume = volume;
		fadeCoroutine = null;
	}

	private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
	{
		float startVol = source.volume;
		float t = 0f;

		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			source.volume = Mathf.Lerp(startVol, 0f, t / duration);
			yield return null;
		}

		source.volume = 0f;
		source.Stop();
		fadeCoroutine = null;
	}
}
