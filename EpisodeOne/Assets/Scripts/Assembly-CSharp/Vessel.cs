using System;
using FLD;
using Palringo;
using UnityEngine;

public class Vessel : Entity
{
	private const string jumpClipAnimation = "VesselJump";

	private const float Z_Jump = -128f;

	private const float Z_Normal = -90f;

	private const float Z_Below = 80f;

	public const float ORIGINAL_MAX_SPEED = 0.35f;

	public Action VesselDied;

	private SecureInt _lives;

	[SerializeField]
	private tk2dSpriteAnimator _shieldAnimator;

	[SerializeField]
	private tk2dSpriteAnimator _vesselAnimator;

	[SerializeField]
	private tk2dSprite _vesselEnergy;

	public Avatar VesselAvatar;

	public GameObject Gfx;

	public AudioClip SfxExtraLife;

	public AudioClip SfxExplode;

	public float Acceleration = 75f;

	public int currentEnergy;

	private bool _allowedToMove = true;

	public Vector2 lastFilteredAccelerometerPosition = Vector2.zero;

	private float _currentMaxSpeed = 0.35f;

	[SerializeField]
	private ParticleSystem _speedParticles;

	[SerializeField]
	private ParticleSystem _magnetParticles;

	private float _speedBonus;

	private bool _speedBoosterEnabled;

	private bool _shieldBoosterEnabled;

	private bool _magnetBoosterEnabled;

	private bool _haveShield;

	private SecureInt _lifeOrbsCollectedOnThisLevel = 0;

	private Vector3 _lastCheckpoint;

	private float _invincibleTimer;

	private bool _isDead;

	private float _z;

	private float _dz;

	private float _scale = 1f;

	private bool _isPlayingJumpAnimation;

	private bool _isFalling;

	private Rigidbody _rigidbody;

	private Transform _transform;

	private Vector3 tmp = Vector3.zero;

	private Vector2 tmp2 = Vector2.zero;

	private Transform _gfxTransform;

	private Renderer _gfxPuckRenderer;

	private Transform _cameraTransform;

	private CameraFollow _cameraFollow;

	private bool _invincible;

	public bool Spin;

	private float rotationScale = 17f;

	private float rotation;

	private float y;

	private float x;

	private Vector3 acc;

	private float sideWaysLimit;

	private float backupLimit;

	private float factor;

	private float deltaTime;

	private bool _isSolid = true;

	public bool AllowedToMove
	{
		get
		{
			return _allowedToMove;
		}
		set
		{
			if (!value)
			{
				DisableParticles();
			}
			_allowedToMove = value;
		}
	}

	public float CurrentMaxSpeed
	{
		get
		{
			return _currentMaxSpeed;
		}
	}

	public bool HaveShield
	{
		get
		{
			return _haveShield;
		}
	}

	public bool IsDead
	{
		get
		{
			return _isDead;
		}
	}

	public SecureInt Lives
	{
		get
		{
			return _lives;
		}
	}

	public SecureInt OrbsCollectedOnThisLevel
	{
		get
		{
			return _lifeOrbsCollectedOnThisLevel;
		}
	}

	public bool IsFalling
	{
		get
		{
			return _isFalling;
		}
	}

	public bool IsInAir
	{
		get
		{
			return _z > 45f || _isFalling;
		}
	}

	public bool ShieldBooster
	{
		get
		{
			return _shieldBoosterEnabled;
		}
		set
		{
			_shieldBoosterEnabled = value;
			_haveShield = _shieldBoosterEnabled;
		}
	}

	public bool SpeedBooster
	{
		get
		{
			return _speedBoosterEnabled;
		}
		set
		{
			_speedBoosterEnabled = value;
			if (_speedBoosterEnabled && !_speedParticles.isPlaying)
			{
				_speedParticles.Play();
			}
			else
			{
				_speedParticles.Stop();
			}
		}
	}

	public bool MagnetBooster
	{
		get
		{
			return _magnetBoosterEnabled;
		}
		set
		{
			_magnetBoosterEnabled = value;
			if (!(_magnetParticles == null))
			{
				if (_magnetBoosterEnabled && !_magnetParticles.isPlaying)
				{
					_magnetParticles.Play();
				}
				else
				{
					_magnetParticles.Stop();
				}
			}
		}
	}

	private void OnDisable()
	{
		Gfx = null;
		SfxExtraLife = null;
	}

	private void Awake()
	{
		SetAvatar();
		_rigidbody = base.GetComponent<Rigidbody>();
		_transform = base.transform;
		_gfxTransform = Gfx.transform;
		_gfxPuckRenderer = _vesselAnimator.gameObject.GetComponent<Renderer>();
	}

	public void SetAvatar()
	{
		VesselAvatar = AvatarManager.Instance.ActiveAvatar;
		_vesselAnimator.SetSprite(VesselAvatar.SpriteCollection, 24);
		tk2dSpriteAnimationClip clipByName = _vesselAnimator.GetClipByName("VesselJump");
		tk2dSpriteAnimationFrame[] frames = clipByName.frames;
		foreach (tk2dSpriteAnimationFrame tk2dSpriteAnimationFrame2 in frames)
		{
			tk2dSpriteAnimationFrame2.spriteCollection = VesselAvatar.SpriteCollection;
		}
	}

	public void SetRelaxedAccelleration()
	{
		lastFilteredAccelerometerPosition = new Vector2(0f, 2.4f - World.cameraFollow.GetComponent<Camera>().aspect);
	}

	public void RestoreAllLives()
	{
		_lives = CustomBuildVariables<CustomBuildVars>.Instance.Lives;
		World.hud.UpdateLives();
	}

	private void Start()
	{
		_speedBonus = BoosterManager.SpeedBonus;
		_cameraFollow = World.cameraFollow;
		_cameraTransform = _cameraFollow.GetComponent<Camera>().transform;
		tk2dSpriteAnimator vesselAnimator = _vesselAnimator;
		vesselAnimator.AnimationCompleted = (Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>)Delegate.Combine(vesselAnimator.AnimationCompleted, new Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip>(OnAnimationComplete));
		_lastCheckpoint = _transform.position;
		_lifeOrbsCollectedOnThisLevel = 0;
		_lives = CustomBuildVariables<CustomBuildVars>.Instance.Lives;
		Reset();
		World.hud.UpdateLives();
		World.hud.UpdateOrbs();
	}

	private void Reset()
	{
		_haveShield = _shieldBoosterEnabled;
		_scale = 1f;
		if (World.isInMatrixMode)
		{
			_vesselEnergy.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
		}
		else
		{
			_vesselEnergy.transform.localScale = Vector3.one;
		}
		TriggerEnergy(0);
		_invincibleTimer = 0f;
		_isFalling = false;
		_z = (_dz = 0f);
		OnAnimationComplete(null, null);
		_isDead = false;
		Gfx.SetActive(true);
		_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, -90f);
		SetSolid(true, true);
		SetGfxActive(true);
		World.hud.Reset();
	}

	private void Update()
	{
		if (Spin)
		{
			rotation -= 7f * Time.deltaTime;
			_vesselAnimator.transform.localRotation = Quaternion.AngleAxis(rotation * rotationScale, Vector3.back);
		}
		_gfxTransform.localScale = new Vector3(_scale, _scale, 1f);
		if (_invincibleTimer <= 0f || Mathf.FloorToInt(Time.time * 30f / 2f) % 2 == 0)
		{
			_gfxPuckRenderer.enabled = true;
			_shieldAnimator.gameObject.SetActive(_haveShield && !_isDead);
		}
		else
		{
			_gfxPuckRenderer.enabled = false;
			_shieldAnimator.gameObject.SetActive(false);
		}
		_invincibleTimer -= Time.deltaTime;
	}

	public void GotoEndCheat()
	{
		Vector3 position = _transform.position;
		float num = ((!Input.GetKey(KeyCode.LeftShift)) ? (-1f) : 1f);
		GameObject gameObject = GameObject.Find("OutroShuttleEntity");
		float num2 = 0f;
		if (gameObject == null)
		{
			gameObject = GameObject.Find("MainframeEntity");
		}
		if (gameObject == null)
		{
			Debug.Log("it happened!");
			gameObject = GameObject.Find("BrokenMainframeEntity");
			num2 = 800f;
		}
		position.z = gameObject.transform.position.z + 256f * num + num2;
		_transform.position = position;
		_cameraFollow.windowPosZ = position.z;
	}

	private void FixedUpdate()
	{
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
		if (!AllowedToMove)
		{
			_rigidbody.AddForce(-_rigidbody.linearVelocity, ForceMode.VelocityChange);
			SetRelaxedAccelleration();
			return;
		}
		if (_transform.position.z < _cameraTransform.position.z - (float)(Constants.OriginalScreenHeight / 2))
		{
			_rigidbody.AddForce(-_rigidbody.linearVelocity, ForceMode.VelocityChange);
			SetRelaxedAccelleration();
			return;
		}
		if (_transform.position.z > _cameraTransform.position.z + (float)(Constants.OriginalScreenHeight / 2))
		{
			_rigidbody.AddForce(-_rigidbody.linearVelocity, ForceMode.VelocityChange);
			SetRelaxedAccelleration();
			return;
		}
		y = 0f;
		x = 0f;
		Vector2 input = DarkNebulaInput.GetInput();
		x = input.x;
		y = input.y;
		_currentMaxSpeed = 0.35f;
		if (_speedBoosterEnabled && !_isPlayingJumpAnimation)
		{
			_currentMaxSpeed *= _speedBonus;
		}
		sideWaysLimit = _currentMaxSpeed;
		backupLimit = 0f - _currentMaxSpeed;
		if (_cameraFollow.scrollSpeed > 5f)
		{
			factor = _cameraFollow.scrollSpeed / 5f;
			_currentMaxSpeed = 0.35f * factor;
			backupLimit = _currentMaxSpeed;
			sideWaysLimit *= 1f + factor * 0.3f;
			if (_speedBoosterEnabled)
			{
				sideWaysLimit *= _speedBonus;
			}
		}
		if (x < 0f - sideWaysLimit)
		{
			x = 0f - sideWaysLimit;
		}
		else if (x > sideWaysLimit)
		{
			x = sideWaysLimit;
		}
		if (y > _currentMaxSpeed)
		{
			y = _currentMaxSpeed;
		}
		else if (y < backupLimit)
		{
			y = backupLimit;
		}
		tmp2.x = x * 10f;
		tmp2.y = y * 10f;
		if (tmp2.sqrMagnitude > 0.25f)
		{
			lastFilteredAccelerometerPosition = tmp2;
		}
		else
		{
			lastFilteredAccelerometerPosition = Vector2.zero;
		}
		tmp.x = tmp2.x * Acceleration * 100f * Time.deltaTime;
		tmp.y = 0f;
		tmp.z = tmp2.y * Acceleration * 100f * Time.deltaTime;
		_rigidbody.AddForce(tmp - _rigidbody.linearVelocity, ForceMode.VelocityChange);
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

	public bool WantsSuperLifeOrbWithMaxLives(int maxlives)
	{
		return _lives <= maxlives + 1;
	}

	public void TriggerGainHC()
	{
        GameHaptics.Play(HapticEvent.CriticalPickup);
		HardCurrencyManager.Instance.Earn(CustomBuildVariables<CustomBuildVars>.Instance.InGameHCValue);
		if (World.hud != null) World.hud.ShowMessage(GameLocalization.Format("SHARD COLLECTED", CustomBuildVariables<CustomBuildVars>.Instance.InGameHCValue));
		Audio.Play("extralife", SfxExtraLife, _transform);
	}

	public bool TriggerLifeOrb(Vector3 position)
	{
		++_lifeOrbsCollectedOnThisLevel;
		if (!_lifeOrbsCollectedOnThisLevel.IsConsistent)
		{
			Debug.Log("SecureInt warning: life orbs has been tampered with, setting 0. Tough luck");
			DarkNebulaMetrics.LogHack("_lifeOrbsCollectedOnThisLevel");
			_lifeOrbsCollectedOnThisLevel = 0;
		}
		TextEntity objectForType = ObjectPool.instance.GetObjectForType<TextEntity>();
		objectForType.gameObject.transform.position = position + new Vector3(0f, 80f, 0f);
		objectForType.SetText(_lifeOrbsCollectedOnThisLevel.ToString());
		objectForType.gameObject.SetActive(true);
		World.hud.AddOrb();
		return true;
	}

	public void TriggerJump()
	{
		_dz = 25f;
		SetSolid(false);
		_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, -128f);
		_isPlayingJumpAnimation = true;
		_vesselAnimator.Play("VesselJump");
		_vesselAnimator.CurrentClip.fps = 22.5f;
		_vesselAnimator.gameObject.transform.localScale = Vector3.one * 0.65f;
	}

	private void OnAnimationComplete(tk2dSpriteAnimator arg1, tk2dSpriteAnimationClip arg2)
	{
		if (_isFalling)
		{
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, 80f);
		}
		else
		{
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, -90f);
		}
		if (_isPlayingJumpAnimation)
		{
			_isPlayingJumpAnimation = false;
			_vesselAnimator.gameObject.transform.localScale = Vector3.one;
		}
		UpdateVesselGfx();
	}

	public void UpdateVesselGfx()
	{
		if (World.isInMatrixMode && !World.isEndOfMatrix)
		{
			_vesselAnimator.SetSprite(VesselAvatar.SpriteCollection, 29);
		}
		else if (UserSettings.Instance.IsExpertMode)
		{
			_vesselAnimator.SetSprite(VesselAvatar.SpriteCollection, 30);
		}
		else
		{
			_vesselAnimator.SetSprite(VesselAvatar.SpriteCollection, 24);
		}
	}

	public void TriggerCheckpoint(Vector3 position)
	{
		if (!_isFalling && !_isDead && _lastCheckpoint.z < position.z)
		{
			_lastCheckpoint = new Vector3(position.x, _lastCheckpoint.y, position.z);
			World.skillMeter.NotifyOfCheckpoint();
		}
	}

	public bool TriggerGiveShield()
	{
		if (_haveShield)
		{
			return false;
		}
		_haveShield = true;
		if (!World.isInMatrixMode && !World.isEndOfMatrix)
		{
			if (World.IsBeginningOfLevel1)
			{
				World.hud.ShowShieldFound();
			}
			else
			{
				World.hud.ShowShieldRestored();
			}
		}
		return true;
	}

	public bool TriggerEnergy(int energy)
	{
		bool result = false;
		if (currentEnergy != energy && energy != 0)
		{
			result = true;
		}
		currentEnergy = energy;
		_vesselEnergy.gameObject.SetActive(true);
		switch (currentEnergy)
		{
		case 1:
			_vesselEnergy.SetSprite(VesselAvatar.SpriteCollection, 26);
			break;
		case 2:
			_vesselEnergy.SetSprite(VesselAvatar.SpriteCollection, 27);
			break;
		case 3:
			_vesselEnergy.SetSprite(VesselAvatar.SpriteCollection, 28);
			break;
		default:
			_vesselEnergy.gameObject.SetActive(false);
			break;
		}
		return result;
	}

	public void TriggerFall()
	{
		if (!_invincible)
		{
			DisableParticles();
			_gfxTransform.localPosition = new Vector3(_gfxTransform.localPosition.x, _gfxTransform.localPosition.y, 80f);
			_isFalling = true;
			SetSolid(false);
		}
	}

	private void DisableParticles()
	{
		if (_speedBoosterEnabled)
		{
			_speedParticles.enableEmission = false;
		}
		if (_magnetBoosterEnabled)
		{
			_magnetParticles.gameObject.SetActive(false);
		}
	}

	private void EnableParticles()
	{
		Timer.Delay(0.01f, () =>
		{
			if (_speedBoosterEnabled)
			{
				_speedParticles.enableEmission = true;
			}
			if (_magnetBoosterEnabled)
			{
				_magnetParticles.gameObject.SetActive(true);
				_magnetParticles.Play();
			}
		});
	}

	public void TriggerDeathCollision()
	{
		if (_rigidbody.linearVelocity.sqrMagnitude > 9f)
		{
			TakeDamage();
		}
	}

	public void TriggerSpike()
	{
		if (!(_invincibleTimer > 0f))
		{
			Die();
		}
	}

	private bool TakeDamage()
	{
		if (_invincibleTimer > 0f)
		{
			return false;
		}
		if (!_haveShield)
		{
			Die();
		}
		else
		{
			_haveShield = false;
			if (!World.IsBeginningOfLevel1 && !World.isInMatrixMode && !UserSettings.Instance.IsExpertMode)
			{
				World.hud.ShowShieldDownMessage();
			}
		}
		_invincibleTimer = 3.3333333f;
		ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", _transform.position, 0.5f, 20);
		Audio.Play("explode", SfxExplode, _transform);
		Vibrate();
		return true;
	}

	private void Vibrate()
	{
		if (!_isDead || GamePreferences.Vibration == VibrationMode.Original)
		{
			GameHaptics.Play(HapticEvent.Damage);
		}
	}

	private void Die()
	{
		if (!_isDead && !_invincible)
		{
			if (VesselDied != null)
			{
				VesselDied();
			}
			DisableParticles();
			_isDead = true;
			if (!World.isInMatrixMode || GamePreferences.Vibration == VibrationMode.Enhanced)
			{
				GameHaptics.Play(HapticEvent.Death);
			}
			SetSolid(false);
			World.skillMeter.NotifyOfDeath();
			if (World.isInMatrixMode)
			{
				ObjectPool.instance.GetObjectForType<Shockwave>().Play("Blue", _gfxTransform.position, 0.5f, 20);
				ObjectPool.instance.GetObjectForType<Particle>().Play("-blue", _gfxTransform.position, 1f);
				World.distortion = 0f;
			}
			else
			{
				ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", _gfxTransform.position, 0.5f, 20);
				ObjectPool.instance.GetObjectForType<Particle>().Play("-deathexplosion", _gfxTransform.position, 1f);
			}
			SetGfxActive(false);
			_cameraFollow.targetScrollSpeed = 0f;
			_cameraFollow.scrollSpeed = 0f;
			Audio.Play("explode", SfxExplode, _transform);
			Invoke("DieComplete", 0.75f);
		}
	}

	private void DieComplete()
	{
		RestoreCheckpoint();
		if (World.isInMatrixMode)
		{
			Reset();
			_invincibleTimer = 3.3333333f;
			return;
		}
		--_lives;
		if (!_lives.IsConsistent)
		{
			Debug.Log("SecureInt warning: _lives is inconsistent, setting 0 lives.");
			DarkNebulaMetrics.LogHack("_lives");
			_lives = 0;
		}
		if (_lives > 0)
		{
			World.hud.UpdateLives();
			if (_lives - 1 == 1)
			{
				World.hud.ShowMessage("LIFE LEFT".Localize(_lives - 1));
			}
			else
			{
				World.hud.ShowMessage("LIVES LEFT".Localize(_lives - 1));
			}
			EnableParticles();
			Reset();
		}
		else
		{
			Hud.ShowGameOverScreen();
		}
	}

	private void RestoreCheckpoint()
	{
		// Teleport the physics body too, so interpolation cannot restore the death position.
		RigidbodyInterpolation interpolation = _rigidbody.interpolation;
		_rigidbody.interpolation = RigidbodyInterpolation.None;
		_rigidbody.linearVelocity = Vector3.zero;
		_rigidbody.angularVelocity = Vector3.zero;
		_rigidbody.position = _lastCheckpoint;
		_transform.position = _lastCheckpoint;
		Physics.SyncTransforms();
		_rigidbody.interpolation = interpolation;
		lastFilteredAccelerometerPosition = Vector2.zero;
	}

	private void SetGfxActive(bool on)
	{
		if (currentEnergy != 0)
		{
			_vesselEnergy.gameObject.SetActive(on);
		}
		if (_haveShield)
		{
			_shieldAnimator.gameObject.SetActive(on);
		}
		_vesselAnimator.gameObject.SetActive(on);
	}

	public bool TriggerBulletCollision()
	{
		return TakeDamage();
	}

	public void ResetDrag()
	{
		_rigidbody.linearDamping = 1f;
	}
}
