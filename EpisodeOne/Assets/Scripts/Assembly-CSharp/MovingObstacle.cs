using System;
using System.Collections.Generic;
using AssemblyCSharp;
using UnityEngine;

public class MovingObstacle : Entity, IFreezable
{
	public float amplitude = 2f;

	public List<float> speedSetting = new List<float>();

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

	private void Awake()
	{
		World.SignalDifficultyChanged += SetDifficultyTo;
		speed = (originalSpeed = UserSettings.GetDifficultValue(speedSetting.ToArray()));
		_transform = base.transform;
		_rigidbody = base.GetComponent<Rigidbody>();
	}

	private void SetDifficultyTo(float delta)
	{
        float nextSpeed = delta * originalSpeed;
        phaseTimeOffset += Time.time * (speed - nextSpeed);
        speed = nextSpeed;
	}

	private void OnDestroy()
	{
		World.SignalDifficultyChanged -= SetDifficultyTo;
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
			World.AddToFrozenObjects(this);
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

	public void Defrost()
	{
		_isFrozen = false;
        UpdatePosition();
        _rigidbody.position = tmp;
	}

    private void UpdatePosition()
    {
        float angle = (Time.time * speed + phaseTimeOffset) * ((float)Math.PI / 5f) + phase * (float)Math.PI * 2f;
        float position = startpos + Mathf.Sin(angle) * amplitude;
        if (isVertical) tmp.z = position;
        else tmp.x = position;
    }
}
