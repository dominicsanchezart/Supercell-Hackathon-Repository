using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click editor tool that creates all three PatronDialogueData assets
/// with pre-written scripted lines matching each patron's personality.
///
/// Usage: Tools > Patron > Create Dialogue Data Assets
/// Creates assets in: Assets/! GAME/Data/Patron/
/// </summary>
public class CreatePatronDialogueAssets
{
	private const string BASE_PATH = "Assets/! GAME/Data/Patron";

	[MenuItem("Tools/Patron/Create Dialogue Data Assets")]
	public static void CreateAll()
	{
		// Ensure folder exists
		if (!AssetDatabase.IsValidFolder("Assets/! GAME/Data"))
			AssetDatabase.CreateFolder("Assets/! GAME", "Data");
		if (!AssetDatabase.IsValidFolder("Assets/! GAME/Data/Patron"))
			AssetDatabase.CreateFolder("Assets/! GAME/Data", "Patron");

		CreateWrathDialogue();
		CreatePrideDialogue();
		CreateRuinDialogue();

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log("[Patron] Created 3 Patron Dialogue Data assets in " + BASE_PATH);
	}

	private static void CreateWrathDialogue()
	{
		var data = ScriptableObject.CreateInstance<PatronDialogueData>();

		// Cinder King voice: cold, military, laconic, commanding
		data.combatStartLines = new[]
		{
			"Another one. Begin.",
			"Burn them down.",
			"Show me what my pact bought.",
			"No mercy. No hesitation.",
			"Let the flames judge."
		};

		data.bigDamageLines = new[]
		{
			"Acceptable.",
			"More. Always more.",
			"That is the fury I demand.",
			"Good. Again.",
			"The fire answers."
		};

		data.highStatusLines = new[]
		{
			"They crumble.",
			"Watch them wither.",
			"The affliction spreads. Good.",
			"Suffering is the purest fuel."
		};

		data.lowHPLines = new[]
		{
			"Bleed, but do not break.",
			"Pain sharpens. Use it.",
			"You are not permitted to fall.",
			"Wrath does not kneel."
		};

		data.victoryLines = new[]
		{
			"As expected.",
			"Adequate.",
			"The pyre claims another.",
			"Onward. We are not finished."
		};

		data.closeCallVictoryLines = new[]
		{
			"Sloppy. But alive.",
			"That was too close. Sharpen yourself.",
			"Victory through stubbornness. I allow it.",
			"A pyrrhic win. We will do better."
		};

		data.approvalLines = new[]
		{
			"A worthy addition to the arsenal.",
			"Fire recognizes fire.",
			"Yes. Feed the flames.",
			"That card burns with purpose."
		};

		data.disapprovalLines = new[]
		{
			"That is not our way.",
			"A distraction. Nothing more.",
			"You stray from the pact.",
			"Weakness dressed as strategy."
		};

		data.bossStartFallbackLines = new[]
		{
			"A true challenge. Finally.",
			"This one has teeth. Good.",
			"Prove yourself against real fire."
		};

		data.eventFallbackLines = new[]
		{
			"Choose wisely. Or don't.",
			"Even rest serves the war.",
			"Make your decision. I grow impatient."
		};

		AssetDatabase.CreateAsset(data, BASE_PATH + "/Wrath Dialogue Data.asset");
	}

	private static void CreatePrideDialogue()
	{
		var data = ScriptableObject.CreateInstance<PatronDialogueData>();

		// Gilded Serpent voice: silky, theatrical, dripping with charm and vanity
		data.combatStartLines = new[]
		{
			"Ah, a performance awaits.",
			"Do try to make this entertaining, darling.",
			"The stage is set. Dazzle me.",
			"Another opponent? How quaint.",
			"Let us see if they deserve our attention."
		};

		data.bigDamageLines = new[]
		{
			"...lovely.",
			"Now THAT had flair.",
			"Exquisite. Do it again.",
			"A masterpiece in violence.",
			"Beauty and brutality, intertwined."
		};

		data.highStatusLines = new[]
		{
			"They are unraveling. Delightful.",
			"Like watching a flower wilt. Gorgeous.",
			"The venom takes hold. Patience rewarded.",
			"How poetic. They poison themselves."
		};

		data.lowHPLines = new[]
		{
			"Careful, darling. Scars are unbecoming.",
			"This is beneath you. Rally.",
			"I did not invest in a corpse.",
			"Composure. Always composure."
		};

		data.victoryLines = new[]
		{
			"Naturally.",
			"Was there ever any doubt?",
			"A flawless performance.",
			"Take your bow, champion."
		};

		data.closeCallVictoryLines = new[]
		{
			"Graceless, but... you survived.",
			"That lacked elegance. We must practice.",
			"A win is a win, I suppose. Barely.",
			"Do not make a habit of such close calls."
		};

		data.approvalLines = new[]
		{
			"A card of distinction. Well chosen.",
			"Refined taste. I approve.",
			"Yes, that suits us perfectly.",
			"Now you are learning."
		};

		data.disapprovalLines = new[]
		{
			"How... pedestrian.",
			"That card lacks... je ne sais quoi.",
			"Are you certain? It seems rather common.",
			"I expected better judgment."
		};

		data.bossStartFallbackLines = new[]
		{
			"The main act begins. Do not disappoint.",
			"A worthy adversary at last. How exciting.",
			"All eyes on us now, darling."
		};

		data.eventFallbackLines = new[]
		{
			"An opportunity to shine. Or not.",
			"Every choice is a reflection. Choose well.",
			"How delightfully unexpected."
		};

		AssetDatabase.CreateAsset(data, BASE_PATH + "/Pride Dialogue Data.asset");
	}

	private static void CreateRuinDialogue()
	{
		var data = ScriptableObject.CreateInstance<PatronDialogueData>();

		// Stitch Prophet voice: manic, clinical, excited by destruction and reconstruction
		data.combatStartLines = new[]
		{
			"Oh! A new specimen. Let's begin.",
			"The plating HOLDS! ...Probably.",
			"Systems online. Deploying countermeasures.",
			"Another stress test! How wonderful.",
			"Let's see what breaks first."
		};

		data.bigDamageLines = new[]
		{
			"MAGNIFICENT impact force!",
			"The readings are off the charts!",
			"Yes yes yes! More of THAT!",
			"Structural damage: critical. Theirs, not ours!",
			"Beautiful destruction. Just beautiful."
		};

		data.highStatusLines = new[]
		{
			"Cascading system failure! In THEM!",
			"The ailments compound. Fascinating data.",
			"Their defenses are compromised. Perfect.",
			"Stack overflow! Haha! No, the OTHER kind."
		};

		data.lowHPLines = new[]
		{
			"Hull integrity failing! Reroute! REROUTE!",
			"Fascinating. We're dying. Stop dying.",
			"Emergency protocols standing by...",
			"The stitches strain. Hold together!"
		};

		data.victoryLines = new[]
		{
			"Test complete! Subject: dismantled.",
			"All readings nominal. We survive. AGAIN!",
			"Another successful field test!",
			"The patchwork holds! I told you!"
		};

		data.closeCallVictoryLines = new[]
		{
			"HA! By the THINNEST margin!",
			"Warning levels critical but we LIVE!",
			"That was... educational. Let's not repeat it.",
			"Barely functional! My favorite state of being!"
		};

		data.approvalLines = new[]
		{
			"Ooh, that one has POTENTIAL!",
			"Adding to the arsenal. Smart!",
			"This fits the design. The grand design!",
			"Perfect component. Slot it in!"
		};

		data.disapprovalLines = new[]
		{
			"That's... not in the blueprint.",
			"Incompatible components. Tsk.",
			"Why would you... never mind.",
			"Square peg, round hole, dear."
		};

		data.bossStartFallbackLines = new[]
		{
			"BOSS-CLASS specimen! Maximum output!",
			"This one is built different. Like us!",
			"All systems to combat mode! GO GO GO!"
		};

		data.eventFallbackLines = new[]
		{
			"Uncharted data! How thrilling!",
			"A variable! I love variables!",
			"Recalculating optimal path..."
		};

		AssetDatabase.CreateAsset(data, BASE_PATH + "/Ruin Dialogue Data.asset");
	}
}
