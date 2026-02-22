using UnityEngine;

[CreateAssetMenu(menuName = "Upgrades/UpgradeDefinition", fileName = "UpgradeDefinition")]
public class UpgradeDefinition : ScriptableObject
{
	[System.Serializable]
	public struct LevelDefinition
	{
		[Min(0)]
		public int cost;
		[Tooltip("Bonus numeric value applied at this level (designer-defined meaning, e.g. +20% => 0.2)")]
		public float bonusValue;
	}

	[Header("Metadata")]
	public string upgradeId;
	public string displayName;
	[TextArea(2,4)] public string description;

	[Header("Levels")]
	[Tooltip("Per-level cost and bonus. Index 0 => level 1. Use length = number of levels (recommended 3).")]
	public LevelDefinition[] levels = new LevelDefinition[3];

	public int MaxLevels => levels != null ? levels.Length : 0;

	// Level is 1-based here to match designer mental model (1..MaxLevels)
	public int GetCostForLevel(int level)
	{
		int idx = level - 1;
		if (levels == null || idx < 0 || idx >= levels.Length) return int.MaxValue;
		return Mathf.Max(0, levels[idx].cost);
	}

	public float GetBonusForLevel(int level)
	{
		int idx = level - 1;
		if (levels == null || idx < 0 || idx >= levels.Length) return 0f;
		return levels[idx].bonusValue;
	}
}

