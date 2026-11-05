using System;
using FLD;
using UnityEngine;

public class StarfighterVesselEntity : Vessel
{
	public tk2dSprite sprite;

	public tk2dSprite thrustSprite;

	public tk2dSprite shieldSprite;

	public tk2dSprite matrixSprite;

	public AudioClip pickupSound;

	public AudioClip shieldSound;

	private Vector2 thrustSpriteXScaleInterval;

	private Vector2 thrustSpriteYScaleInterval;

	private Vector2[] bulletOrigins = new Vector2[8];

	private Vector2[] bulletDirs = new Vector2[8];

	private int[] bulletsPhase = new int[8];

	private float maxBlinkFrames;

	private bool movementDisabled;

	private int shootFrame;

	private bool shooting;

	private float currBlinkFrame;

	private float slideinFrames;

	private float slideinMaxYVelocity;

	private int bulletsActivated;

	private int deathPauseFrames;

	private int currDeathPauseFrames;

	private float currFrame;

	private bool endMorph;

	private bool slideinFinished;

	private bool weaponsActivated;

	private float movementFactor;

	private Vector2 localPos;

	private Vector2 invertedForcedDirection;

	private new StarfighterCamera camera;

	private FlagConditionChanged slottedInCondition;

	private Vector3 spawnPos;

	private static int[] spriteIds;

	private bool godMode;

	private EndMorph endMorphEntity;

	private int lastShootFrame = -1;

	private Vector3 thrustSpriteScale = Vector3.one;

	private float xVelocity;

	private float yVelocity;

	private float normalizedXVelocity;

	private float normalizedYVelocity;

	private Vector3 move;

	private Vector3 bossPos
	{
		get
		{
			return ShooterBoss.Instance.transform.position;
		}
	}

	private bool shieldActivated
	{
		get
		{
			return shieldSprite.GetComponent<Renderer>().enabled;
		}
		set
		{
			shieldSprite.GetComponent<Renderer>().enabled = value;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (spriteIds == null)
		{
			spriteIds = sprite.GetFrameIds("starfighter/", 13);
		}
	}

	public void Spawn(Vector3 position)
	{
		spawnPos = position;
		base.alwaysAlive = true;
		Reset();
		base.transform.position = position;
		if (camera != null)
		{
			throw new InvalidOperationException("Camera can not exist before spawn");
		}
		camera = new StarfighterCamera(new Vector2(position.x, position.z));
		matrixSprite.GetComponent<Renderer>().enabled = false;
	}

	public void DeActivateWeapons()
	{
		weaponsActivated = false;
	}

	public void SetAlwaysInvincible(bool on)
	{
		godMode = on;
	}

	public override void EndMorph()
	{
		if (!endMorph)
		{
			endMorph = true;
			matrixSprite.GetComponent<Renderer>().enabled = true;
			shieldSprite.GetComponent<Renderer>().enabled = false;
			thrustSprite.GetComponent<Renderer>().enabled = false;
			sprite.GetComponent<Renderer>().enabled = false;
		}
	}

	public override bool PostInit()
	{
		if (base.PostInit())
		{
			return true;
		}
		shootFrame = 15;
		shooting = true;
		bulletOrigins[3].x = 15f;
		bulletOrigins[3].y = 0f;
		bulletOrigins[4].x = 30f;
		bulletOrigins[4].y = 0f;
		bulletOrigins[5].x = -30f;
		bulletOrigins[5].y = 0f;
		bulletOrigins[6].x = 20f;
		bulletOrigins[6].y = 0f;
		bulletOrigins[7].x = -20f;
		bulletOrigins[7].y = 0f;
		bulletDirs[0].x = 0f;
		bulletDirs[0].y = -1f;
		bulletDirs[1].x = 0f;
		bulletDirs[1].y = -1f;
		bulletDirs[2].x = 0f;
		bulletDirs[2].y = -1f;
		bulletDirs[3].x = 0f;
		bulletDirs[3].y = -1f;
		bulletDirs[4].x = 0.2f;
		bulletDirs[4].y = -1f;
		bulletDirs[4].Normalize();
		bulletDirs[5].x = -0.2f;
		bulletDirs[5].y = -1f;
		bulletDirs[5].Normalize();
		bulletDirs[6].x = 0.5f;
		bulletDirs[6].y = -1f;
		bulletDirs[6].Normalize();
		bulletDirs[7].x = -0.5f;
		bulletDirs[7].y = -1f;
		bulletDirs[7].Normalize();
		bulletsPhase[0] = 0;
		bulletsPhase[1] = 0;
		bulletsPhase[2] = 0;
		bulletsPhase[3] = 0;
		bulletsPhase[4] = 7;
		bulletsPhase[5] = 7;
		bulletsPhase[6] = 12;
		bulletsPhase[7] = 12;
		thrustSpriteXScaleInterval.x = 0.25f;
		thrustSpriteXScaleInterval.y = 0.65f;
		thrustSpriteYScaleInterval.x = 0.5f;
		thrustSpriteYScaleInterval.y = 1.5f;
		slideinFrames = 45f;
		slideinMaxYVelocity = 10f;
		maxBlinkFrames = 150f;
		currBlinkFrame = maxBlinkFrames;
		slottedInCondition = new FlagConditionChanged(97);
		slottedInCondition.Callback += OnSlottedIn;
		bulletOrigins[0].x = -10f;
		bulletOrigins[0].y = 0f;
		bulletOrigins[1].x = 10f;
		bulletOrigins[1].y = 0f;
		bulletOrigins[2].x = 0f;
		bulletOrigins[2].y = 0f;
		bulletsActivated = 2;
		deathPauseFrames = 30;
		currDeathPauseFrames = 0;
		Reset();
		return false;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (slottedInCondition != null)
		{
			slottedInCondition.Callback += OnSlottedIn;
			slottedInCondition.Dispose();
			slottedInCondition = null;
		}
	}

	public override void Reset()
	{
		currFrame = 0f;
		slideinFinished = false;
		weaponsActivated = false;
		localPos.x = spawnPos.x;
		localPos.y = spawnPos.z - 300f;
		if (!UserSettings.I.expert)
		{
			shieldActivated = true;
		}
		movementFactor = 0f;
		invertedForcedDirection.x = 1f;
		invertedForcedDirection.y = 1f;
	}

	private void TriggerShieldUpReporting()
	{
	}

	private void TriggerShieldDownReporting()
	{
	}

	private void TriggerShieldUp()
	{
		TriggerShieldUpReporting();
		shieldActivated = true;
	}

	private bool Invincible()
	{
		return currBlinkFrame < maxBlinkFrames || godMode;
	}

	private void DamageReceived()
	{
		SpawnShockwave(base.transform.position);
		World.TriggerFlag(107);
		World.ResetFlag(107);
	}

	private bool ReceiveDamage()
	{
		if (Invincible())
		{
			return false;
		}
		DamageReceived();
		if (shieldActivated)
		{
			shieldActivated = false;
			TriggerShieldDownReporting();
		}
		else
		{
			Die();
		}
		currBlinkFrame = 0f;
		return true;
	}

	private void Die()
	{
		if (GamePreferences.Vibration == VibrationMode.Enhanced)
		{
			GameHaptics.Play(HapticEvent.Death);
		}
		ObjectPool.instance.GetObjectForType<Particle>().Play("_explosion", base.transform.position);
		Reset();
		currDeathPauseFrames = deathPauseFrames;
		Vessel._lives--;
		Hud.Instance.UpdateLives();
		if (Vessel._lives == 0)
		{
			CameraFade orAddComponent = World._world.GetOrAddComponent<CameraFade>();
			orAddComponent.FadeOut();
			Invoke("DieComplete", 0.5f);
		}
	}

	private void DieComplete()
	{
		CameraFade orAddComponent = World._world.GetOrAddComponent<CameraFade>();
		orAddComponent.FadeIn(0.1f);
		Hud.ShowGameOverScreen();
	}

	private void OnTriggerEnter(Collider e)
	{
		if (e.gameObject.GetComponent<StarfighterShieldPickupEntity>() != null)
		{
			if (!UserSettings.I.expert && !shieldActivated)
			{
                GameHaptics.Play(HapticEvent.Pickup);
				Audio.Play("shieldSound", shieldSound, transform);
				TriggerShieldUp();
				SpawnShockwaveInverted(base.transform.position);
				UnityEngine.Object.Destroy(e.gameObject);
			}
			return;
		}
		if (e.gameObject.GetComponent<StarfighterWeaponsPickupEntity>() != null)
		{
			StarfighterWeaponsPickupEntity component = e.gameObject.GetComponent<StarfighterWeaponsPickupEntity>();
            GameHaptics.Play(HapticEvent.Pickup);
			Audio.Play("pickupSound", pickupSound, transform);
			UpgradeWeapons();
			SpawnShockwaveInverted(base.transform.position);
			component.RemoveFromWorld();
			return;
		}
		Enemy component2 = e.gameObject.GetComponent<Enemy>();
		if (component2 != null && ReceiveDamage())
		{
			if (!(component2 is ShooterBoss) && !(component2 is ShooterBossTurret))
			{
				component2.ReceiveDamage(1, new RotationAngle());
			}
			if (component2 is ShooterEnemyBulletEntity)
			{
				UnityEngine.Object.Destroy(component2.gameObject);
			}
		}
	}

	private void UpgradeWeapons()
	{
		switch (bulletsActivated)
		{
		case 2:
			bulletsActivated++;
			break;
		case 3:
			bulletOrigins[0].x = -15f;
			bulletOrigins[0].y = 0f;
			bulletOrigins[1].x = -5f;
			bulletOrigins[1].y = 0f;
			bulletOrigins[2].x = 5f;
			bulletOrigins[2].y = 0f;
			bulletsActivated++;
			break;
		case 4:
			bulletsActivated += 2;
			break;
		}
	}

	private void OnSlottedIn()
	{
		if (!endMorphEntity.endMorphSlottedIn)
		{
			endMorphEntity.endMorphSlottedIn = true;
			SpawnShockwave(bossPos);
			base.transform.position = bossPos;
		}
	}

	private void SpawnShockwave(Vector3 pPos)
	{
		ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", base.transform.position, 0.5f, 12);
	}

	private void SpawnShockwaveInverted(Vector3 pPos)
	{
		ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", base.transform.position, 1.2f, 8).Inverse();
	}

	public override void DisableMovement()
	{
		movementDisabled = true;
	}

	protected virtual void Update()
	{
		if (IsPooled || Time.timeScale == 0f)
		{
			return;
		}
		if (currDeathPauseFrames != 0)
		{
			currDeathPauseFrames--;
			return;
		}
		currFrame += Time.deltaTime * 30f;
		if (endMorph)
		{
			UpdateEndMorphBoss();
			UpdateMovement();
			return;
		}
		if (slideinFinished)
		{
			UpdateMovement();
		}
		else
		{
			UpdateMovement();
			UpdateMovementWhenSlideIn();
		}
		UpdateSlideIn();
		if (weaponsActivated)
		{
			UpdateBullets();
		}
		UpdateThrust();
		UpdateSprite();
		UpdateBlink();
		UpdateFlags();
	}

	private void UpdateEndMorphBoss()
	{
		if (endMorphEntity == null)
		{
			endMorphEntity = ShooterBoss.Instance.CreateEndMorph(bossPos);
		}
		endMorphEntity.endMorphSpriteAlpha += 0.02f;
		if (endMorphEntity.endMorphSpriteAlpha > 1f)
		{
			endMorphEntity.endMorphSpriteAlpha = 1f;
		}
		endMorphEntity.currFrame = currFrame;
		endMorphEntity.UpdateEndMorphBoss();
	}

	private void UpdateBullets()
	{
		int num = (int)currFrame;
		if (!shooting || num == lastShootFrame)
		{
			return;
		}
		lastShootFrame = num;
		for (int i = 0; i < bulletsActivated; i++)
		{
			if ((num + bulletsPhase[i]) % shootFrame == 0)
			{
				Vector3 position = base.transform.position;
				position.x += bulletOrigins[i].x;
				position.z += bulletOrigins[i].y;
				StarfighterBulletEntity objectForType = ObjectPool.instance.GetObjectForType<StarfighterBulletEntity>();
				objectForType.transform.parent = base.transform.parent;
				objectForType.transform.position = position;
				objectForType.SetDirection(bulletDirs[i]);
				objectForType.PostInit();
			}
		}
	}

	private void UpdateSprite()
	{
		int num = (int)((double)(6f * normalizedXVelocity) + 7.0 - 1.0);
		if (num < 0)
		{
			num = 0;
		}
		else if (num > 12)
		{
			num = 12;
		}
		sprite.spriteId = spriteIds[num];
	}

	private void UpdateFlags()
	{
	}

	private bool DontBlink()
	{
		return currBlinkFrame >= maxBlinkFrames || (int)currBlinkFrame / 2 % 2 == 0;
	}

	private void UpdateBlink()
	{
		currBlinkFrame += Time.deltaTime * 30f;
		Gfx.GetComponent<Renderer>().enabled = DontBlink();
		GfxPuck.GetComponent<Renderer>().enabled = DontBlink();
	}

	private void UpdateMovementWhenSlideIn()
	{
		yVelocity = (1f - currFrame / slideinFrames) * slideinMaxYVelocity;
		localPos.y += yVelocity;
		normalizedXVelocity = xVelocity / 4.8f;
		normalizedYVelocity = yVelocity / 4.8f;
	}

	private void UpdateThrust()
	{
		thrustSpriteScale.z = thrustSpriteYScaleInterval.x + (thrustSpriteYScaleInterval.y - thrustSpriteYScaleInterval.x) * ((1f + (0f - normalizedYVelocity)) / 2f);
		thrustSprite.transform.localScale = thrustSpriteScale;
	}

	private void UpdateSlideIn()
	{
		if (!slideinFinished && currFrame > slideinFrames)
		{
			weaponsActivated = true;
			slideinFinished = true;
		}
	}

	public void UpdateMovement()
	{
		if (!movementDisabled)
		{
			float num = 0.35f;
			float num2 = num;
			float num3 = 0f - num;
			Vector2 input = DarkNebulaInput.GetInput();
			float num4 = input.x;
			float num5 = input.y;
			if (num4 < 0f - num2)
			{
				num4 = 0f - num2;
			}
			if (num4 > num2)
			{
				num4 = num2;
			}
			if (num5 > num)
			{
				num5 = num;
			}
			if (num5 < num3)
			{
				num5 = num3;
			}
			num5 += (World.CameraManager.FindHighestPriorityCamera() as StarfighterCamera).Speed * Time.deltaTime;
			Vector2 vector = new Vector2(num4 * 20f * movementFactor, num5 * 20f * movementFactor);
			if (vector.sqrMagnitude > 0.25f)
			{
				localPos += vector;
				ConstrainLocalPos();
				normalizedXVelocity = vector.x / 7f;
				normalizedYVelocity = vector.y / 7f;
				lastFilteredAccelerometerPosition = vector;
			}
			else
			{
				vector.x = (vector.y = 0f);
				lastFilteredAccelerometerPosition = Vector2.zero;
			}
			Vector3 position = base.transform.position;
			position.x = localPos.x;
			position.z = localPos.y;
			base.transform.position = position;
			movementFactor += Time.deltaTime * 0.7f;
			movementFactor = Mathf.Min(1f, movementFactor);
		}
	}

	private void ConstrainLocalPos()
	{
		float num = 16f;
		float aspect = World.cameraFollow.GetComponent<Camera>().aspect;
		float num2 = 1f / aspect;
		float num3 = 160f * num2 - num;
		localPos.x -= World.cameraFollow.windowPosX;
		localPos.y -= World.cameraFollow.windowPosZ;
		if (localPos.x < -144f)
		{
			localPos.x = -144f;
		}
		else if (localPos.x > 144f)
		{
			localPos.x = 144f;
		}
		if (localPos.y < 0f - num3)
		{
			localPos.y = 0f - num3;
		}
		else if (localPos.y > num3)
		{
			localPos.y = num3;
		}
		localPos.x += World.cameraFollow.windowPosX;
		localPos.y += World.cameraFollow.windowPosZ;
	}

	public override void TriggerFall(bool crashInWater)
	{
	}

	public override void ResetShield()
	{
		shield = startShield;
	}

	public override void TriggerRecieveDamage()
	{
		ReceiveDamage();
	}

	public override void TriggerDeathCollision()
	{
		throw new NotImplementedException();
	}

	public override bool TriggerEnergy(int pEnergy)
	{
		throw new NotImplementedException();
	}

	public override bool TriggerBulletCollision()
	{
		throw new NotImplementedException();
	}

	public override void TriggerJump()
	{
		throw new NotImplementedException();
	}

	public override void TriggerCheckpoint(Vector3 pCheckpoint)
	{
		throw new NotImplementedException();
	}

	public override void ResetDrag()
	{
		throw new NotImplementedException();
	}

	public override void UpdateVesselGfx()
	{
		throw new NotImplementedException();
	}

	public override bool TriggerLifeOrb(Vector3 position)
	{
		throw new NotImplementedException();
	}

	public override void TriggerExtraLife()
	{
		throw new NotImplementedException();
	}

	public override void TriggerSpike()
	{
		throw new NotImplementedException();
	}

	public override bool TriggerGiveShield()
	{
		throw new NotImplementedException();
	}

	public override void TriggerVesselBullet(int color, bool hasLightEntity)
	{
		throw new NotImplementedException();
	}
}
