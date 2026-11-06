using System;
using FLD;
using UnityEngine;

public class Windmill : Entity, IFreezable
{
	public GameObject Arms;

	public int numberOfRods = 3;

	public int numberOfBullets = 7;

	public float bulletSpacing = 20f;

	public float rotationOffset;

	public AudioClip Sfx;

	protected float rotationSpeed = 1f;

	protected float originalRotationSpeed = 1f;

	private float rotationTimeOffset;

	protected Transform _armsTransform;

	protected Transform _transform;

	protected float _z;

	public Transform armsTransform
	{
		get
		{
			if (_armsTransform == null)
			{
				_armsTransform = Arms.transform;
			}
			return _armsTransform;
		}
	}

	public virtual void Start()
	{
		World.SignalDifficultyChanged += SetDifficultyTo;
		rotationSpeed = (originalRotationSpeed = GetParameterFloat("rotationspeed"));
		_transform = base.transform;
		_z = base.transform.position.z;
	}

	protected override void OnDestroy()
	{
		// Keep the difficulty subscription while this entity is asleep.
		World.SignalDifficultyChanged -= SetDifficultyTo;
		base.OnDestroy();
	}

	public virtual void Update()
	{
		if (!IsPooled)
		{
			armsTransform.localRotation = Quaternion.AngleAxis((rotationSpeed * Time.time + rotationTimeOffset) * 30f * -3.275f, Vector3.forward);
		}
	}

	protected virtual void SetDifficultyTo(float delta)
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
		for (int i = 0; i < numberOfRods; i++)
		{
			Transform child = armsTransform.GetChild(i);
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

	public override void InitParametersInEditor()
	{
		base.InitParametersInEditor();
		InitParameters(false);
	}

	public override void InitParametersInRuntime()
	{
		base.InitParametersInRuntime();
		InitParameters(true);
	}

	private void InitParameters(bool IsRuntime)
	{
		numberOfRods = GetParameterInt("numberofrods");
		numberOfBullets = GetParameterInt("numberofbullets");
		bulletSpacing = GetParameterInt("bulletspacing");
		rotationSpeed = GetParameterFloat("rotationspeed");
		rotationOffset = GetParameterFloat("rotationoffset") * (float)System.Math.PI * 2f;
		CreateArms(IsRuntime);
	}

	public void CreateArms(bool IsRuntime)
	{
		CapsuleCollider capsuleCollider = base.gameObject.GetComponent<CapsuleCollider>();
		if (capsuleCollider == null)
		{
			capsuleCollider = base.gameObject.AddComponent<CapsuleCollider>();
		}
		capsuleCollider.radius = 40f + bulletSpacing * (float)(numberOfBullets - 1);
		capsuleCollider.height = 256f + capsuleCollider.radius * 2f;
		capsuleCollider.isTrigger = true;
		WindmillBullet[] componentsInChildren = GetComponentsInChildren<WindmillBullet>();
		WindmillBullet[] array = componentsInChildren;
		foreach (WindmillBullet windmillBullet in array)
		{
			if (windmillBullet != null && windmillBullet.gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(windmillBullet.gameObject);
			}
		}
		WindmillRod[] componentsInChildren2 = GetComponentsInChildren<WindmillRod>();
		WindmillRod[] array2 = componentsInChildren2;
		foreach (WindmillRod windmillRod in array2)
		{
			if (windmillRod != null && windmillRod.gameObject != null)
			{
				UnityEngine.Object.DestroyImmediate(windmillRod.gameObject);
			}
		}
		for (int k = 0; k < numberOfRods; k++)
		{
			float num = 40f;
			float angle = rotationOffset + (float)k * ((float)System.Math.PI * 2f / (float)numberOfRods);
			GameObject gameObject = null;
			gameObject = ((!IsRuntime) ? InstatiatePrefab("Assets/Prefabs/Entities/WindmillRod.prefab") : LevelEP2Runtime.Instatiate("Assets/Prefabs/Entities/WindmillRod.prefab"));
			gameObject.transform.parent = Arms.transform;
			gameObject.transform.localRotation = Quaternion.AngleAxis(0f, Vector3.up);
			gameObject.transform.localPosition = new Vector3(0f, 0f, -10f);
			gameObject.GetComponent<WindmillRod>().SetNumberOfBullets(numberOfBullets, bulletSpacing);
			for (int l = 0; l < numberOfBullets; l++)
			{
				GameObject gameObject2 = null;
				gameObject2 = ((!IsRuntime) ? InstatiatePrefab("Assets/Prefabs/Entities/WindmillBullet.prefab") : LevelEP2Runtime.Instatiate("Assets/Prefabs/Entities/WindmillBullet.prefab"));
				gameObject2.transform.parent = gameObject.transform;
				gameObject2.transform.localRotation = Quaternion.AngleAxis(0f, Vector3.up);
				gameObject2.transform.localPosition = new Vector3(num, 2f, 0f);
				num += bulletSpacing;
			}
			gameObject.transform.RotateAround(Vector3.up, angle);
		}
	}

	private static GameObject InstatiatePrefab(string path)
	{
		return null;
	}
}
