using AssemblyCSharp;
using FLD;
using UnityEngine;

public class Windmill : Entity, IFreezable
{
	public float[] rotationSpeedSetting = new float[2] { 1f, 1f };

	public GameObject Arms;

	public int numberOfRods = 3;

	public int numberOfBullets = 7;

	public float bulletSpacing = 20f;

	public float rotationOffset;

	public AudioClip Sfx;

	private float rotationSpeed = 1f;

	private float originalRotationSpeed = 1f;
    private float rotationTimeOffset;

	private bool _isActive = true;

	private Transform _armsTransform;

	private Transform _transform;

	private float _z;

	private void Awake()
	{
		World.SignalDifficultyChanged += SetDifficultyTo;
		rotationSpeed = (originalRotationSpeed = UserSettings.GetDifficultValue(rotationSpeedSetting));
		_transform = base.transform;
		_armsTransform = Arms.transform;
		_z = base.transform.position.z;
	}

	private void OnDestroy()
	{
		World.SignalDifficultyChanged -= SetDifficultyTo;
	}

	private void Update()
	{
		if (_isActive)
		{
            UpdateRotation();
			if (World.IsFrozen(_z))
			{
				World.AddToFrozenObjects(this);
				_isActive = false;
			}
		}
	}

	private void SetDifficultyTo(float delta)
	{
        float nextSpeed = originalRotationSpeed * delta;
        rotationTimeOffset += Time.time * (rotationSpeed - nextSpeed);
        rotationSpeed = nextSpeed;
	}

	private void OnTriggerStay(Collider other)
	{
		if (!other.CompareTag("Player") || World.vessel.IsFalling)
		{
			return;
		}
		float num = 48f;
		float distance = bulletSpacing * (float)(numberOfBullets - 1) - 8f;
		Ray ray = new Ray(Vector3.zero, Vector3.right);
		int layerMask = 512;
		Vector3 position = _transform.position;
		int childCount = _armsTransform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = _armsTransform.GetChild(i);
			ray.direction = child.right;
			ray.origin = position + ray.direction * num;
			if (Physics.SphereCast(ray, 16f, distance, layerMask))
			{
				if (World.vessel.TriggerBulletCollision())
				{
					Audio.Play("windmill", Sfx, position);
				}
				break;
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		float num = 48f;
		float num2 = 40f + bulletSpacing * (float)(numberOfBullets - 1) - 8f;
		for (int i = 0; i < numberOfRods; i++)
		{
			Transform child = Arms.transform.GetChild(i);
			Vector3 position = base.gameObject.transform.position;
			Gizmos.DrawWireSphere(position + child.right * num, 16f);
			Gizmos.DrawWireSphere(position + child.right * num2, 16f);
			Gizmos.DrawLine(position + child.right * num, position + child.right * num2);
		}
	}

	public void Defrost()
	{
		_isActive = true;
        UpdateRotation();
	}

    private void UpdateRotation()
    {
        _armsTransform.localRotation = Quaternion.AngleAxis((rotationSpeed * Time.time + rotationTimeOffset) * 30f * -3.275f, Vector3.forward);
    }
}
