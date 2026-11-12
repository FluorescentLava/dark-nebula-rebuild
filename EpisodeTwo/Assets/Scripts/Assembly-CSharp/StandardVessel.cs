using System.Collections.Generic;
using FLD;
using UnityEngine;

public class StandardVessel : Vessel
{
	private const float Z_Jump = -10f;

	private const float Z_Normal = 0f;

	private const float forcedVelocityAcceleration = 20f;

	public float AbyssPositionZ;

	public int Layer;

	private bool crashInWater;

	[HideInInspector]
	public float Acceleration = 75f;

	[HideInInspector]
	public int _isOnMovingPlatform;

	public int SpawnerType;

	public LightEntity lightEntity;

	private tk2dAnimatedSprite _anim;

	private tk2dAnimatedSprite _animShield;

	private tk2dAnimatedSprite _animEnergy;

	private Vector3 _lastCheckpoint;

	private bool _firstTimeSpawned = true;

	private float _invincibleTimer;

	public bool _isDead;

	private float _scale = 1f;

	protected bool _isPlayingJumpAnimation;

	private VesselBullet _bullet;

	private float _twirlFactor;

	private float _playerLightScale;

	private float _playerLightMinScale;

	private float _playerLightMaxScale;

	private VesselSkinType skinType;

	private List<Waypoint> activeWaypoints = new List<Waypoint>();

	private List<Entity> Waypoints = new List<Entity>();

	public int dieTimer;

	public bool canMoveInAir;

	public bool _canMove;

	private float forcedSpeed;

	private float forcedFactor;

	private Vector2 forcedDirection;

	private Vector2 forcedOriginalDirection;

	private Vector2 invertedForcedDirection = Vector2.one;

	private Vector2 targetForcedVelocity;

	private Vector2 forcedVelocity;

	private bool forcedVelocityUseDecay;

	private Rigidbody _rigidbody;

	private Transform _transform;

	private Transform _gfxTransform;

	private Renderer _gfxPuckRenderer;


	private Renderer _gfxShieldRenderer;

	private bool _portalHidden;

	private readonly List<KeyValuePair<Renderer, bool>> _portalRendererStates = new List<KeyValuePair<Renderer, bool>>();

	private Transform _cameraTransform;

	private CameraFollow _cameraFollow;

	private GameCamera gameCamera;

	private Vector3 _animPos;

	private Vector3 _animShieldPos;

	private Vector3 _animEnergyPos;

	private int flipFloorCount;

	private Vector3 _lastPlatformPosition;

	private List<Transform> _platforms;

	private Transform _platform;

	private Vector3 acc;

	private float limit;

	private float sideWaysLimit;

	private float backupLimit;

	private float factor;

	private float deltaTime;

	private float platformScale = 1.35508f;

	public Vector3 velocityThisFrame = Vector3.zero;

	private bool _isSolid = true;

	private float decayTimer;

	private bool treadmillActivated;

	private Vector3 treadmillPushVector;

	public float Z_Below
	{
		get
		{
			return 0f - (AbyssPositionZ - base.transform.position.y + 1f);
		}
	}

	public override bool IsInAir
	{
		get
		{
			return _isPlayingJumpAnimation || base.IsInAir;
		}
	}

	public VesselBullet Bullet
	{
		get
		{
			return _bullet;
		}
	}

	public bool canMove
	{
		get
		{
			return _canMove;
		}
		set
		{
			_canMove = value;
		}
	}

	public float ForcedSpeed
	{
		get
		{
			return forcedSpeed;
		}
	}

	public int IsOnMovingPlatform
	{
		get
		{
			return _isOnMovingPlatform;
		}
	}

	public int IsOnFlipFloor
	{
		get
		{
			return flipFloorCount;
		}
	}

	public void SetTwirlFactor(float factor)
	{
		_twirlFactor = factor;
		if (_twirlFactor <= 0f)
		{
			lightEntity.SetVisible(false);
		}
		else
		{
			lightEntity.SetVisible(true);
		}
	}

	public void SetLightScale(float playerLightScale, float playerLightMinScale, float playerLightMaxScale)
	{
		_playerLightScale = playerLightScale;
		_playerLightMinScale = playerLightMinScale;
		_playerLightMaxScale = playerLightMaxScale;
	}

	public void Hide()
	{
		_anim.transform.localPosition = _animPos + new Vector3(1000f, 0f, 0f);
		_animShield.transform.localPosition = _animShieldPos + new Vector3(1000f, 0f, 0f);
		_animEnergy.transform.localPosition = _animEnergyPos + new Vector3(1000f, 0f, 0f);
	}

	public void Show()
	{
		_anim.transform.localPosition = _animPos;
		_animShield.transform.localPosition = _animShieldPos;
		_animEnergy.transform.localPosition = _animEnergyPos;
		SetPortalHidden(false);
	}

	public bool IsPortalHidden => _portalHidden;

	public void SetPortalHidden(bool hidden)
	{
		if (_portalHidden == hidden) return;
		_portalHidden = hidden;
		if (hidden)
		{
			// Keep the original stone-ring disappearance independent of transparent sorting.
			// Update still owns the normal damage flash and shield visibility.
			if (Gfx == null) return;
			foreach (Renderer renderer in Gfx.GetComponentsInChildren<Renderer>(true))
			{
				_portalRendererStates.Add(new KeyValuePair<Renderer, bool>(renderer, renderer.forceRenderingOff));
				renderer.forceRenderingOff = true;
			}
			return;
		}
		foreach (var state in _portalRendererStates)
			if (state.Key != null) state.Key.forceRenderingOff = state.Value;
		_portalRendererStates.Clear();
	}

	public override void EndMorph()
	{
		World.TriggerEndOfLevel();
	}

	private void OnDisable()
	{
		SetPortalHidden(false);
		_anim = null;
		_animShield = null;
		_animEnergy = null;
		Gfx = null;
		GfxPuck = null;
		GfxShield = null;
		GfxEnergy = null;
		SfxExtraLife = null;
	}

	protected override void Awake()
	{
		base.Awake();
		_rigidbody = base.GetComponent<Rigidbody>();
		_transform = base.transform;
		_gfxTransform = Gfx.transform;
		_gfxPuckRenderer = GfxPuck.GetComponent<Renderer>();
		_gfxShieldRenderer = GfxShield.GetComponent<Renderer>();
	}

	public override bool PostInit()
	{
		if (base.PostInit())
		{
			return true;
		}
		_cameraFollow = World.cameraFollow;
		_cameraTransform = _cameraFollow.GetComponent<Camera>().transform;
		_anim = GfxPuck.GetComponentInChildren<tk2dAnimatedSprite>();
		_animShield = GfxShield.GetComponentInChildren<tk2dAnimatedSprite>();
		_animEnergy = GfxEnergy.GetComponentInChildren<tk2dAnimatedSprite>();
        VesselSkins.Apply(_anim, false);
        VesselSkins.Apply(_animEnergy, true);
		_animPos = _anim.transform.localPosition;
		_animShieldPos = _animShield.transform.localPosition;
		_animEnergyPos = _animEnergy.transform.localPosition;
		_anim.animationCompleteDelegate = OnAnimationComplete;
		_lastCheckpoint = base.transform.position;
		Reset();
		World.hud.UpdateLives();
		World.hud.UpdateOrbs();
		if (gameCamera == null)
		{
			gameCamera = new StandardVesselCamera(new Vector2(base.transform.position.x, base.transform.position.z));
		}
		return false;
	}

	public void FlipFloor(bool enter)
	{
		flipFloorCount += (enter ? 1 : (-1));
	}

	public void SetCanMove(bool pCanMove)
	{
		canMove = pCanMove;
		if (canMove)
		{
			_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		}
		else
		{
			_rigidbody.interpolation = RigidbodyInterpolation.None;
		}
	}

	public void SetPlatform(Transform pPlatform)
	{
		if (pPlatform != null && (_platforms == null || !_platforms.Contains(pPlatform)))
		{
			_platform = pPlatform;
			_lastPlatformPosition = _platform.position;
			if (_platforms == null)
			{
				_platforms = new List<Transform>();
			}
			_platforms.Add(_platform);
			_isOnMovingPlatform = _platforms.Count;
		}
	}

	public void RemovePlatform(Transform pPlatform)
	{
		if (pPlatform != null && _platforms != null && _platforms.Contains(pPlatform))
		{
			int num = _platforms.IndexOf(pPlatform);
			if (num >= 0)
			{
				_platforms.RemoveAt(num);
			}
			int count = _platforms.Count;
			_isOnMovingPlatform = count;
			if (count == 0 || _isOnMovingPlatform <= 0)
			{
				_platform = null;
				return;
			}
			_platform = _platforms[count - 1];
			_lastPlatformPosition = _platform.position;
		}
	}

    public bool HasPlatform(Transform platform)
    {
        return _platforms != null && _platforms.Contains(platform);
    }

    // Layer changes during a jump can reset trigger pairs. At the instant of
    // landing, use actual overlap rather than depend on callback ordering.
    public bool FindPlatformSupport()
    {
        Collider player = GetComponent<Collider>();
        if (player == null || !player.enabled) return false;
        Physics.SyncTransforms();
        Bounds bounds = player.bounds;
        foreach (Collider candidate in Physics.OverlapBox(bounds.center, bounds.extents,
            Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
        {
            AnimatedEntity platform = candidate.GetComponentInParent<AnimatedEntity>();
            if (platform == null || !platform.canBeBorded || platform.IsPooled) continue;
            Vector3 direction; float distance;
            if (Physics.ComputePenetration(player, player.transform.position, player.transform.rotation,
                candidate, candidate.transform.position, candidate.transform.rotation, out direction, out distance))
            {
                platform.Board(this);
                return true;
            }
        }
        return false;
    }

	public override void ReceiveDamage(int pDamage)
	{
		if (!IsInAir)
		{
			ReduceShieldWith(30);
		}
	}

	public override void Reset()
	{
		_scale = 1f;
		if (_animEnergy != null)
		{
			if (World.isInMatrixMode)
			{
				_animEnergy.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
			}
			else
			{
				_animEnergy.transform.localScale = Vector3.one;
			}
		}
		TriggerEnergy(0);
		shield = startShield;
		_invincibleTimer = 0f;
		_isFalling = false;
		_z = (_dz = 0f);
		_isOnMovingPlatform = 0;
		_platforms = null;
		OnAnimationComplete(null, 0);
		dieTimer = -1;
		_isDead = false;
		Gfx.SetActive(true);
		_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, 0f);
		SetSolid(true, true);
		SetGfxActive(true);
		flipFloorCount = 0;
		_platforms = null;
		World.hud.Reset();
	}

	protected virtual void Update()
	{
		_gfxTransform.localScale = new Vector3(_scale, _scale, 1f);
		if (_invincibleTimer <= 0f || Mathf.FloorToInt(Time.time * 30f / 2f) % 2 == 0)
		{
			_gfxPuckRenderer.enabled = true;
			_gfxShieldRenderer.enabled = shield > 15;
		}
		else
		{
			_gfxPuckRenderer.enabled = false;
			_gfxShieldRenderer.enabled = false;
		}
		_invincibleTimer -= Time.deltaTime;
		treadmillActivated = false;
		RenderWaypoints();
		if (_twirlFactor > 0f)
		{
			UpdateLight();
		}
	}

	private void UpdateLight()
	{
		float num = 0f;
		num = (base.IsFalling ? Mathf.Clamp(_scale * _playerLightScale, _playerLightMinScale, _playerLightMaxScale) : ((!(_scale <= 0f)) ? Mathf.Clamp(1f / _scale * _playerLightScale, _playerLightMinScale, _playerLightMaxScale) : _playerLightMinScale));
		num *= 1f / (0.2f + _twirlFactor);
		lightEntity.Scale = num;
	}

	protected virtual void FixedUpdate()
	{
		velocityThisFrame = Vector3.zero;
		UpdateForcedVelocity();
		UpdateMovement();
		deltaTime = Time.deltaTime * 28f;
		if (_isFalling)
		{
			if (_z > 10f)
			{
				_scale -= 0.22f * deltaTime;
			}
			else
			{
				_scale -= 0.08f * deltaTime;
			}
			if (_scale < 0.05f)
			{
				Die();
			}
		}
		else
		{
			_z += _dz * deltaTime;
			if (_z < 0f)
			{
				_dz *= -0.4f;
				_z = 0f;
				SetSolid(true);
			}
			if (_z > 0f)
			{
				_dz -= 1.5f * deltaTime;
			}
			if (_z < 45f && _z < 0f)
			{
				SetSolid(true);
			}
			_scale = 1f + _z * 0.0135f;
		}
		if (_isOnMovingPlatform > 0 && _platform != null)
		{
			Vector3 vector = _platform.position - _lastPlatformPosition;
			_lastPlatformPosition = _platform.position;
			MovePosition(vector * platformScale);
		}
		velocityThisFrame *= 100f * Time.deltaTime * Acceleration;
		_rigidbody.AddForce(velocityThisFrame - _rigidbody.linearVelocity, ForceMode.VelocityChange);
	}

	public void UpdateMovement()
	{
		if (!(_cameraTransform == null) && dieTimer < 40 && canMove && (!IsInAir || canMoveInAir))
		{
			limit = 0.35f;
			sideWaysLimit = limit;
			backupLimit = 0f - limit;
			Vector2 input = DarkNebulaInput.GetInput();
			float num = input.x;
			float y = input.y;
			if (num < 0f - sideWaysLimit)
			{
				num = 0f - sideWaysLimit;
			}
			if (num > sideWaysLimit)
			{
				num = sideWaysLimit;
			}
			if (y > limit)
			{
				y = limit;
			}
			if (y < backupLimit)
			{
				y = backupLimit;
			}
			Vector2 b = new Vector2(num * 10f, y * 10f);
			if (b.sqrMagnitude > 0.25f)
			{
				Vector2 delta = Vector2.Scale(invertedForcedDirection, b);
				MovePosition(delta);
				lastFilteredAccelerometerPosition = b;
			}
			else
			{
				lastFilteredAccelerometerPosition = Vector2.zero;
			}
			if (treadmillActivated)
			{
				MovePosition(treadmillPushVector);
			}
		}
	}

	private void MovePosition(Vector2 delta)
	{
		velocityThisFrame.x += delta.x;
		velocityThisFrame.z += delta.y;
	}

	private void MovePosition(Vector3 delta)
	{
		velocityThisFrame += delta;
	}

	private void SetSolid(bool on, bool pForceSet = false)
	{
		if (_isSolid != on || pForceSet)
		{
			_isSolid = on;
			Physics.IgnoreLayerCollision(9, 0, !on);
			Physics.IgnoreLayerCollision(9, 10, !on);
		}
	}

	public override void TriggerExtraLife()
	{
        GameHaptics.Play(HapticEvent.CriticalPickup);
		Vessel._lives++;
		TextAttachedEntity objectForType = ObjectPool.instance.GetObjectForType<TextAttachedEntity>();
		objectForType.SetText("1UP");
		objectForType.gameObject.transform.position = base.gameObject.transform.position + new Vector3(0f, 80f, 0f);
		objectForType.StartText();
		World.hud.UpdateLives();
		World.hud.ShowMessage(Language.Get("EXTRA LIFE!"));
		Audio.Play("extralife" + Random.value, SfxExtraLife, _transform);
	}

	public override bool TriggerLifeOrb(Vector3 position)
	{
		Vessel._lifeOrbsCollected++;
		_lifeOrbsCollectedOnThisLevel++;
		if (Vessel._lifeOrbsCollected >= 100)
		{
			Vessel._lifeOrbsCollected = 0;
			World.hud.AddOrb();
			TriggerExtraLife();
			return false;
		}
		TextEntity objectForType = ObjectPool.instance.GetObjectForType<TextEntity>();
		objectForType.gameObject.transform.position = position + new Vector3(0f, 80f, 0f);
		objectForType.SetText(Vessel._lifeOrbsCollected.ToString());
		World.hud.AddOrb();
		return true;
	}

	public override void TriggerJump()
	{
		_dz = 25f;
		SetSolid(false);
		_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, -10f);
		if (skinType != VesselSkinType.Matrix)
		{
			_isPlayingJumpAnimation = true;
			_anim.Play("VesselJump");
			_anim.gameObject.transform.localScale = Vector3.one * 0.65f;
		}
		if (_anim.CurrentClip != null)
		{
			_anim.CurrentClip.fps = 22.5f;
		}
	}

	private void OnAnimationComplete(tk2dAnimatedSprite sprite, int clipId)
	{
		if (_isFalling)
		{
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, Z_Below);
		}
		else
		{
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, 0f);
		}
		if (_isPlayingJumpAnimation)
		{
			_isPlayingJumpAnimation = false;
		}
		_anim.gameObject.transform.localScale = Vector3.one;
		UpdateVesselGfx();
	}

    public void RefreshSkin()
    {
        VesselSkins.Apply(_anim, false);
        VesselSkins.Apply(_animEnergy, true);
        UpdateVesselGfx();
    }

	public override void UpdateVesselGfx()
	{
		if (skinType == VesselSkinType.Matrix || (World.isInMatrixMode && !World.isEndOfMatrix))
		{
			_anim.Play("VesselMatrix");

		}
		else if (UserSettings.I.expert)
		{
			_anim.Play("VesselExpert");
		}
		else
		{
			_anim.Play("VesselNormal");
		}
	}

	public override void TriggerCheckpoint(Vector3 pCheckpoint)
	{
		if (!_isFalling && !_isDead && _lastCheckpoint.z < pCheckpoint.z)
		{
			_lastCheckpoint = new Vector3(pCheckpoint.x, _lastCheckpoint.y, pCheckpoint.z);
			World.skillMeter.NotifyOfCheckpoint();
		}
	}

	public override bool TriggerGiveShield()
	{
		if (shield >= startShield)
		{
			return false;
		}
		shield = startShield;
		World.hud.ShowShieldRestored();
		return true;
	}

	public override bool TriggerEnergy(int energy)
	{
		bool result = false;
		if (currentEnergy != energy && energy != 0)
		{
			result = true;
		}
		currentEnergy = energy;
		_animEnergy.GetComponent<Renderer>().enabled = true;
		switch (currentEnergy)
		{
		case 1:
			_animEnergy.Play("EnergyRed");
			break;
		case 2:
			_animEnergy.Play("EnergyBlue");
			break;
		case 3:
			_animEnergy.Play("EnergyGreen");
			break;
		default:
			_animEnergy.GetComponent<Renderer>().enabled = false;
			break;
		}
		return result;
	}

	public override void TriggerFall(bool pCrashInWater)
	{
		crashInWater = pCrashInWater;
		if (skinType == VesselSkinType.Matrix)
		{
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, Z_Below - 20f);
		}
		else
		{
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, Z_Below);
		}
		_isFalling = true;
		SetSolid(false);
	}

	public override void TriggerDeathCollision()
	{
		if (_rigidbody.linearVelocity.sqrMagnitude > 9f)
		{
			ReduceShieldWith(30);
		}
	}

	public override void TriggerSpike()
	{
		if (!(_invincibleTimer > 0f))
		{
			Die();
		}
	}

	public override void TriggerRecieveDamage()
	{
		if (!(_invincibleTimer > 0f))
		{
			ReduceShieldWith(30);
		}
	}

	private bool ReduceShieldWith(int hitPower)
	{
		if (_invincibleTimer > 0f)
		{
			return false;
		}
		shield -= hitPower;
		if (shield < 0)
		{
			Die();
		}
		else
		{
			_shieldsLostOnThisLevel++;
			if (!World.isInMatrixMode && !UserSettings.I.expert)
			{
				World.hud.ShowShieldDownMessage();
			}
		}
		_invincibleTimer = 3.3333333f;
		ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", Gfx.transform.position, 0.5f, 20);
		Audio.Play("explode", SfxExplode, _transform);
		Vibrate();
		return true;
	}

	private void Die()
	{
		if (dieTimer >= 0 || _isDead)
		{
			return;
		}
		_isDead = true;
		if (!World.isInMatrixMode || GamePreferences.Vibration == VibrationMode.Enhanced)
		{
			GameHaptics.Play(HapticEvent.Death);
		}
		SetSolid(false);
		DarkNebulaMetrics.Died(World.levelNum, _transform.position, UserSettings.I.expert);
		World.skillMeter.NotifyOfDeath();
		Vector3 pos = _gfxPuckRenderer.transform.position + new Vector3(0f, -30f, 0f);
		if (World.isInMatrixMode)
		{
			ObjectPool.instance.GetObjectForType<Shockwave>().Play("Blue", pos, 0.5f, 20);
			ObjectPool.instance.GetObjectForType<Particle>().Play("-blue", pos, 1f);
			World.distortion = 0f;
		}
		else
		{
			if (crashInWater)
			{
				ObjectPool.instance.GetObjectForType<WaterSplashEntity>().Play(_gfxPuckRenderer.transform.position);
			}
			else
			{
				ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", pos, 0.5f, 20);
				ObjectPool.instance.GetObjectForType<Particle>().Play("-deathexplosion", pos, 1f);
			}
		}
		SetGfxActive(false);
		dieTimer = 0;
		Audio.Play("explode", SfxExplode, _transform);
		World.TriggerVesselDied();
		Invoke("DieComplete", 0.75f);
	}

	private void Vibrate()
	{
		if (!_isDead || GamePreferences.Vibration == VibrationMode.Original)
		{
			GameHaptics.Play(HapticEvent.Damage);
		}
	}

	private void DieComplete()
	{
		RestoreCheckpoint();
		if (World.isInMatrixMode)
		{
			Reset();
			_invincibleTimer = 3.3333333f;
		}
		else
		{
			Vessel._lives--;
			if (Vessel._lives > 0)
			{
				World.hud.UpdateLives();
				World.hud.ShowMessage(Language.Get("%d LIVES LEFT", Vessel._lives - 1));
				Reset();
			}
			else
			{
				Hud.ShowGameOverScreen();
			}
		}
		dieTimer = 40;
		InitiateSpawner(skinType, true, 0, 0, 0);
	}

	public void SetSpawnPosition(Vector3 position)
	{
		// Scripted spawns (including portal transitions) start a new checkpoint section.
		_lastCheckpoint = position;
		RestoreCheckpoint();
	}

	private void RestoreCheckpoint()
	{
		RigidbodyInterpolation interpolation = _rigidbody.interpolation;
		_rigidbody.interpolation = RigidbodyInterpolation.None;
		_rigidbody.linearVelocity = Vector3.zero;
		_rigidbody.angularVelocity = Vector3.zero;
		_rigidbody.position = _lastCheckpoint;
		_transform.position = _lastCheckpoint;
		Physics.SyncTransforms();
		_rigidbody.interpolation = interpolation;
		ResetForcedVelocity();
		forcedVelocity = Vector2.zero;
		velocityThisFrame = Vector3.zero;
		treadmillActivated = false;
		lastFilteredAccelerometerPosition = Vector2.zero;
	}

	private void SetGfxActive(bool on)
	{
		_animEnergy.gameObject.SetActive(on);
		_animShield.gameObject.SetActive(on);
		_anim.gameObject.SetActive(on);
	}

	public override bool TriggerBulletCollision()
	{
		return ReduceShieldWith(30);
	}

	public override void ResetDrag()
	{
		_rigidbody.linearDamping = 1f;
	}

	public override void ResetShield()
	{
		shield = startShield;
	}

	public void ResetVelocity()
	{
		StopMovement();
	}

	public void InitiateSpawner(VesselSkinType pSkinType, bool respawn, int flag, int scrollInLength, int scrollTime)
	{
		skinType = pSkinType;
		dieTimer = 41;
		VesselSpawnEntity vesselSpawnEntity = null;
		if (SpawnerType == 0)
		{
			vesselSpawnEntity = ObjectPool.instance.GetObjectForType<VesselNormalSpawnEntity>();
		}
		else if (SpawnerType == 1)
		{
			vesselSpawnEntity = ObjectPool.instance.GetObjectForType<VesselMatrixSpawnEntity>();
		}
		if (vesselSpawnEntity != null && base.transform.parent != null)
		{
			vesselSpawnEntity.transform.parent = base.transform.parent;
			vesselSpawnEntity.Init(base.transform.position, respawn, flag, scrollInLength, scrollTime);
			Vector3 position = vesselSpawnEntity.transform.position;
			position.y = (float)World._world.TopmostLayer * 5f + 100f;
			vesselSpawnEntity.transform.position = position;
			vesselSpawnEntity.SetSkinType(skinType);
			vesselSpawnEntity.PostInit();
			return;
		}
		if (vesselSpawnEntity == null)
		{
			throw new MissingComponentException("Missing type: " + SpawnerType + " VesselSpawnEntity in objectPool");
		}
		throw new MissingComponentException("Missing transform.parent");
	}

	public override void TriggerVesselBullet(int color, bool hasLightEntity)
	{
		if (_bullet != null)
		{
			if (_bullet.color != color)
			{
				if (color == 0)
				{
					_bullet.Deactivate();
					_bullet = null;
				}
				else
				{
					_bullet.Init(color, hasLightEntity);
				}
			}
		}
		else if (color != 0)
		{
			_bullet = ObjectPool.instance.GetObjectForType<VesselBullet>();
			_bullet.transform.parent = _transform;
			_bullet.Init(color, hasLightEntity);
		}
	}

	public void InitiateDespawner(int doneFlag)
	{
		Entity entity = null;
		if (SpawnerType == 1)
		{
			entity = ObjectPool.instance.GetObjectForType<VesselMatrixDespawnEntity>();
			entity.transform.parent = base.transform.parent;
			entity.transform.position = base.transform.position + new Vector3(0f, 100f, 0f);
			entity.SetParameter("doneFlag", doneFlag.ToString());
			(entity as VesselMatrixDespawnEntity).Init(base.transform.position + new Vector3(0f, 100f, 0f), doneFlag);
		}
		else if (SpawnerType == 2)
		{
			entity = ObjectPool.instance.GetObjectForType<VesselFadeoutDespawnEntity>();
			entity.transform.parent = base.transform.parent;
			entity.transform.position = base.transform.position + new Vector3(0f, 100f, 0f);
			entity.SetParameter("doneFlag", doneFlag.ToString());
			(entity as VesselFadeoutDespawnEntity).PostInit();
		}
		if (entity == null)
		{
			Despawn();
		}
		else
		{
			entity.PostInit();
		}
	}

	public void Despawn()
	{
		dieTimer = 99;
		Invoke("Hide", 0.25f);
	}

	private void RenderWaypoints()
	{
		int num = 0;
		for (int i = 0; i < Waypoints.Count; i++)
		{
			Entity entity = Waypoints[i];
			if (!(entity == null) && !(entity.gameObject == null))
			{
				Vector2 vector = new Vector2(entity.transform.position.x - base.transform.position.x, entity.transform.position.z - base.transform.position.z);
				Vector2 normalized = vector.normalized;
				float directionToEnemy = Math.AngleFromPositions(entity.transform.position, base.transform.position);
				float num2 = 1f;
				if (normalized.y != 0f)
				{
					num2 = Mathf.Clamp(Mathf.Abs(normalized.x) / Mathf.Abs(normalized.y), 0f, 1f);
				}
				float num3 = num2 * 320f + (1f - num2) * 480f;
				normalized.x *= num3 / 2f * 0.9f;
				normalized.y *= num3 / 2f * 0.9f;
				float magnitude = (base.transform.position - entity.transform.position).magnitude;
				if (magnitude < 800f)
				{
					float num4 = 1f - magnitude / 800f;
					float alpha = num4;
					num4 *= 0.8f;
					num4 += 0.2f;
					ShowWaypoint(num, alpha, normalized.x, normalized.y, directionToEnemy, num4 * 2f);
					num++;
				}
			}
		}
		HideWaypointsAfter(num);
		Waypoints.Clear();
	}

	private void ShowWaypoint(int waypointIndex, float alpha, float x, float y, float directionToEnemy, float scale)
	{
		Waypoint waypoint = null;
		if (waypointIndex == activeWaypoints.Count)
		{
			waypoint = ObjectPool.instance.GetObjectForType<Waypoint>();
			activeWaypoints.Add(waypoint);
		}
		else
		{
			waypoint = activeWaypoints[waypointIndex];
		}
		waypoint.transform.parent = base.transform;
		waypoint.SetAlpha(alpha);
		waypoint.transform.localScale = Vector3.one * scale;
		waypoint.transform.localRotation = Quaternion.AngleAxis(directionToEnemy * 57.29578f, Vector3.forward);
		waypoint.transform.localPosition = new Vector3(x, y, -60f);
	}

	private void HideWaypointsAfter(int waypointIndex)
	{
		for (int i = waypointIndex; i < activeWaypoints.Count; i++)
		{
			ObjectPool.instance.PoolObject(activeWaypoints[i]);
		}
		activeWaypoints.RemoveRange(waypointIndex, activeWaypoints.Count - waypointIndex);
	}

	public override void WaypointEnemy(Entity pEnemy)
	{
		Waypoints.Add(pEnemy);
	}

	public void Spawn(VesselSkinType skin)
	{
        GameHaptics.Play(HapticEvent.Spawn);
		Reset();
		skinType = skin;
		if (skinType == VesselSkinType.Matrix)
		{
			startShield = 0;
			shield = 0;
		}
		Vector3 position = _transform.position;
		canMoveInAir = true;
		StopMovement();
		Vector2 p = default(Vector2);
		p.x = position.x;
		p.y = position.z;
		if (gameCamera == null)
		{
			gameCamera = new StandardVesselCamera(p);
		}
		if (!_firstTimeSpawned)
		{
			World.TriggerFlag(110);
		}
		UpdateVesselGfx();
		Show();
		_firstTimeSpawned = false;
		World.skillMeter.NotifyOfSpawn();
	}

	public override void DisableMovement()
	{
		canMove = false;
		_rigidbody.interpolation = RigidbodyInterpolation.None;
		ResetForcedVelocity();
	}

	public void DisableMovementInAir()
	{
		canMoveInAir = false;
		ResetForcedVelocity();
	}

	public void DropFromSky()
	{
		_dz = 0f;
		_z = 100f;
		Show();
	}

	public void EnableMovement()
	{
		_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		canMoveInAir = true;
		canMove = true;
		StopMovement();
	}

	private void StopMovement()
	{
		_rigidbody.AddForce(-_rigidbody.linearVelocity, ForceMode.VelocityChange);
	}

	public void ResetForcedVelocity()
	{
		SetForcedVelocity(Vector2.zero, false);
	}

	public void SetForcedFactor(float factor)
	{
		forcedFactor = factor;
		forcedDirection = forcedOriginalDirection * forcedFactor;
		targetForcedVelocity = forcedDirection * forcedSpeed;
		invertedForcedDirection.x = 1f - Mathf.Abs(forcedDirection.x);
		invertedForcedDirection.y = 1f - Mathf.Abs(forcedDirection.y);
		if (forcedVelocityUseDecay)
		{
			invertedForcedDirection.x = 1f - factor;
			invertedForcedDirection.y = 1f - factor;
		}
	}

	public void SetForcedVelocity(Vector2 vector, bool useFriction)
	{
		targetForcedVelocity = vector;
		forcedSpeed = targetForcedVelocity.magnitude;
		if (forcedSpeed > 0.01f)
		{
			forcedOriginalDirection = targetForcedVelocity.normalized;
		}
		else
		{
			forcedOriginalDirection = Vector2.zero;
		}
		forcedVelocityUseDecay = useFriction;
		forcedFactor = 1f;
		SetForcedFactor(forcedFactor);
	}

	private void UpdateForcedVelocity()
	{
		if (dieTimer >= 0)
		{
			return;
		}
		forcedVelocity += (targetForcedVelocity - forcedVelocity) * 20f * Time.deltaTime;
		Vector2 delta = forcedVelocity * Time.deltaTime * 2f;
		MovePosition(delta);
		if (!forcedVelocityUseDecay)
		{
			return;
		}
		decayTimer += Time.deltaTime;
		if (decayTimer > 0.0333f)
		{
			decayTimer = 0f;
			float num = forcedFactor * 0.95f;
			if (num <= 0.01f)
			{
				ResetForcedVelocity();
			}
			else
			{
				SetForcedFactor(num);
			}
		}
	}

	public void ActivateTreadmill(Vector2 pushVector)
	{
		treadmillActivated = true;
		treadmillPushVector = new Vector3(pushVector.x, 0f, pushVector.y);
	}
}
