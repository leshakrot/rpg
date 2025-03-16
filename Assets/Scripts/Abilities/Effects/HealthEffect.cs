using UnityEngine;
using System;
using RPG.Attributes;

namespace RPG.Abilities.Effects
{	
	[CreateAssetMenu(fileName = "Health Effect", menuName = "Abilities/Effects/Health", order = 0)]
	public class HealthEffect : EffectStrategy
	{
		[SerializeField] float healthChange;
		
		public override void StartEffect(AbilityData data, Action finished)
		{
			foreach (var target in data.GetTargets())
			{
				var health = target.GetComponent<Health>();
				if(health)
				{
					if(healthChange < 0)
					{
						health.TakeDamage(data.GetUser(), -healthChange);
					}					
					else
					{
						health.Heal(healthChange);
					}
				}
			}
			finished();
		}
	}
}
