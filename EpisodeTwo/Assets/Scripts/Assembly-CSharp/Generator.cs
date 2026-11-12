using System;
using UnityEngine;

public class Generator : Enemy
{
	public tk2dSprite PulseSprite;

	public tk2dSprite Gfx;

	public LightEntity lightEntity;

	public string _spriteName;

	private float pulseTimer;

	private float pulseAlpha;

	public override void InitParametersInEditor()
	{
		base.InitParametersInEditor();
		lightEntity.SetVisible(GetParameterBool("hasLightEntity"));
		if (GetParameterInt("type") == 0)
		{
			_spriteName = "entity_generator_" + GetParameterString("color");
		}
		else
		{
			_spriteName = "entity_generator_desert_" + GetParameterString("color");
		}
		Gfx.spriteId = Gfx.GetSpriteIdByName(_spriteName + "/0");
	}

	public override void InitParametersInRuntime()
	{
		InitParametersInEditor();
	}

	protected override void Start()
	{
		base.Start();
		maxHealth = 3;
		base.Health = maxHealth;
		removeFromWorldOnDeath = false;
		lightEntity.SetVisible(GetParameterBool("hasLightEntity"));
	}

	protected override void OnHealthChanged()
	{
		Gfx.spriteId = Gfx.GetSpriteIdByName(_spriteName + "/" + (3 - base.Health));
		if (base.Health == 0)
		{
			if (lightEntity != null)
			{
				lightEntity.SetVisible(false);
			}
			if (PulseSprite != null)
			{
				PulseSprite.enabled = false;
			}
		}
	}

	public override void ReceiveDamage(int damage, RotationAngle impactDirectionRadian = null)
	{
		int previousHealth = base.Health;
		base.ReceiveDamage(damage, impactDirectionRadian);
		if (base.Health < previousHealth)
		{
			GameHaptics.Play(base.Health == 0 ? HapticEvent.MechanismDestroyed : HapticEvent.MechanismHit);
		}
	}

	public override void Update()
	{
		base.Update();
		if (base.Health > 0)
		{
			pulseTimer += Time.deltaTime;
			pulseAlpha = Mathf.Sin(pulseTimer * (float)Math.PI * 2f * 0.8f) * 0.25f + 0.25f;
			PulseSprite.color = new Color(1f, 1f, 1f, pulseAlpha);
		}
	}

	public void SetChildedLight(bool b)
	{
		lightEntity.SetVisible(b);
	}
}
