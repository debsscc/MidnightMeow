using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Progression/PlayerProgressionData", fileName = "PlayerProgressionData")]
public class PlayerProgressionData : ScriptableObject
{
	[Header("Currency")]
	[Tooltip("Ciência acumulada (moeda)")]
	public int science = 0;

	[Header("Upgrade Levels (0..3)")]
	[Range(0, 3)] public int healthLevel = 0;
	[Range(0, 3)] public int fireRateLevel = 0;
	[Range(0, 3)] public int damageLevel = 0;

	// Not serialized; runtime subscribers can listen to changes
	public event Action OnChanged;

	public enum UpgradeType
	{
		Health = 0,
		FireRate = 1,
		Damage = 2
	}

	public int GetLevel(UpgradeType type)
	{
		switch (type)
		{
			case UpgradeType.Health: return healthLevel;
			case UpgradeType.FireRate: return fireRateLevel;
			case UpgradeType.Damage: return damageLevel;
			default: return 0;
		}
	}

	public void SetLevel(UpgradeType type, int level)
	{
		int clamped = Mathf.Clamp(level, 0, 3);
		switch (type)
		{
			case UpgradeType.Health:
				if (healthLevel == clamped) return;
				healthLevel = clamped;
				break;
			case UpgradeType.FireRate:
				if (fireRateLevel == clamped) return;
				fireRateLevel = clamped;
				break;
			case UpgradeType.Damage:
				if (damageLevel == clamped) return;
				damageLevel = clamped;
				break;
		}
		OnChanged?.Invoke();
	}

	public void IncreaseLevel(UpgradeType type)
	{
		SetLevel(type, GetLevel(type) + 1);
	}

	public bool CanAfford(int cost) => cost <= science;

	public bool SpendScience(int cost)
	{
		if (cost <= 0) return true;
		if (science < cost) return false;
		science -= cost;
		OnChanged?.Invoke();
		return true;
	}

	public void AddScience(int amount)
	{
		if (amount <= 0) return;
		science += amount;
		OnChanged?.Invoke();
	}

	public void ResetProgression()
	{
		science = 0;
		healthLevel = 0;
		fireRateLevel = 0;
		damageLevel = 0;
		OnChanged?.Invoke();
	}
}

