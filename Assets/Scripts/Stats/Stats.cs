namespace RPG.Stats
{
	/// <summary>
	/// ВАЖНО: числа зафиксированы, потому что Progression.asset хранит характеристики как int.
	/// Новую характеристику добавляй В КОНЕЦ со следующим номером. Не переставляй и не вставляй в середину.
	/// Она сама появится в меню "+ Стат" инспектора Progression.
	/// </summary>
	public enum Stat
	{
		Health = 0,
		Mana = 1,
		ManaRegenRate = 2,
		ExperienceReward = 3,
		ExperienceToLevelUp = 4,
		Damage = 5,
		TotalTraitPoints = 6,
		BuyingDiscountPercentage = 7,
		Defence = 8,
		MovementSpeed = 9,
		/// <summary>Скорость компаньона при движении к GuideTarget (см. AnimalTraining.GetGuideMoveSpeed).</summary>
		GuideMoveSpeed = 10
	}
}
