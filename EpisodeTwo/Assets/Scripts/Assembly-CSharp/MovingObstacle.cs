using System;
using UnityEngine;

public class MovingObstacle : Entity, IFreezable
{
	public float amplitude = 2f;

	public float[] speedSetting = new float[2] { 3f, 3f };

	public float phase;

	public bool isVertical;

	private float speed;

	private float originalSpeed;

	private float phaseTimeOffset;

	private Transform _transform;

	private Rigidbody _rigidbody;

	private bool _isFrozen;

	private float startpos;

	private Vector3 tmp = Vector3.zero;

	protected override void Awake()
	{
		base.Awake();
		World.SignalDifficultyChanged += SetDifficultyTo;
		speed = (originalSpeed = UserSettings.GetDifficultValue(speedSetting));
		_transform = base.transform;
		_rigidbody = base.GetComponent<Rigidbody>();
	}

	private void SetDifficultyTo(float delta)
	{
		float nextSpeed = delta * originalSpeed;
		phaseTimeOffset += Time.time * (speed - nextSpeed);
		speed = nextSpeed;
	}

	protected override void OnDestroy()
	{
		// Sleeping obstacles still need phase corrections when difficulty changes.
		World.SignalDifficultyChanged -= SetDifficultyTo;
		base.OnDestroy();
	}

	private void Start()
	{
		if (isVertical)
		{
			startpos = _transform.position.z;
		}
		else
		{
			startpos = _transform.position.x;
		}
		tmp = _transform.position;
	}

	private void Update()
	{
		if (!_isFrozen && World.IsFrozen(_transform.position.z))
		{
			World.FreezeManager.AddToFrozenObjects(this);
			_isFrozen = true;
		}
	}

	private void FixedUpdate()
	{
		if (!_isFrozen)
		{
			UpdatePosition();
			_rigidbody.MovePosition(tmp);
		}
	}

	private void UpdatePosition()
	{
		float angle = (Time.time * speed + phaseTimeOffset) * ((float)Math.PI / 5f) + phase * (float)Math.PI * 2f;
		float position = startpos + Mathf.Sin(angle) * amplitude;
		if (isVertical) tmp.z = position;
		else tmp.x = position;
	}

	public override void Defrost()
	{
		base.Defrost();
		_isFrozen = false;
		UpdatePosition();
		_rigidbody.position = tmp;
	}
}
