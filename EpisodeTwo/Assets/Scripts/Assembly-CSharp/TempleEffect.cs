using System;
using UnityEngine;

public class TempleEffect : Task
{
	private const float maximumHeight = 150f;

	private const float heightOrigo = 391f;

	private const float zNear = 0.2f;

	private float time;

	private FlagConditionChanged activateEvent;

	private bool isDone;

	private float gravity;

	private float upForce;

	private float maximumUpForce;

	private float upForceDelta;

	private float velocity;

	private float z;

	private bool triggeredFadeOut;

	private RotationAngle rotation = new RotationAngle();

	private bool didTriggerEffectNotify;

	public TempleEffect()
	{
		time = -1f;
		activateEvent = new FlagConditionChanged(104);
		activateEvent.Callback += OnActivate;
		gravity = -5f;
		upForce = 0f - gravity + 1f;
		upForceDelta = 0.05f;
		maximumUpForce = (0f - gravity) * 1.72f;
	}

	public void Dispose()
	{
		if (activateEvent != null)
		{
			activateEvent.Callback -= OnActivate;
			activateEvent.Dispose();
			activateEvent = null;
		}
	}

	private void OnActivate()
	{
		time = 0f;
		StandardVessel vessel = World.vessel as StandardVessel;
		if (vessel != null) vessel.SetPortalHidden(true);
	}

	private void SetupCamera()
	{
		float far = 1000f;
		float fieldOfView = 79f;
		World.cameraFollow.GetComponent<Camera>().orthographic = false;
		Camera camera=World.cameraFollow.GetComponent<Camera>();
        float originalHorizontal=Camera.VerticalToHorizontalFieldOfView(fieldOfView,LegacyScreenFit.ReferenceAspect);
		camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(originalHorizontal,camera.aspect);
		World.cameraFollow.GetComponent<Camera>().nearClipPlane = 0.2f;
		World.cameraFollow.GetComponent<Camera>().farClipPlane = far;
		World.vessel.transform.Translate(0f, -0.1f, 0f);
	}

	private void UpdateCamera()
	{
		World.cameraFollow.transform.rotation = Quaternion.Euler(90f, 0f - rotation.Degrees, 0f);
		Vector3 position = World.cameraFollow.transform.position;
		position.y = 391f + z;
		World.cameraFollow.transform.position = position;
	}

	private void RestoreCamera()
	{
		Vector3 position = World.cameraFollow.transform.position;
		position.y = 391f;
		World.cameraFollow.transform.position = position;
		World.cameraFollow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		World.cameraFollow.GetComponent<Camera>().orthographic = true;
	}

	public override void Update()
	{
		if (time == -1f)
		{
			return;
		}
		if (time == 0f)
		{
			World.TriggerFlag(102);
			ObjectPool.instance.GetObjectForType<Shockwave>().Play("Big", World.vessel.transform.position, 0.3f, 40);
			SetupCamera();
		}
		else if (time >= 50f && !didTriggerEffectNotify)
		{
			didTriggerEffectNotify = true;
			World.TriggerFlag(106);
		}
		upForce += upForceDelta * Time.deltaTime * 30f;
		if (upForce > maximumUpForce)
		{
			upForce = maximumUpForce;
			upForceDelta = 0f;
			upForce = 0f;
		}
		else if (upForce < 0f)
		{
			upForce = 0f;
		}
		velocity += upForce * Time.deltaTime * 30f;
		velocity += gravity * Time.deltaTime * 30f;
		if (velocity < 0f)
		{
			RotationAngle rotationAngle = new RotationAngle((float)Math.PI / 16f);
			rotation.Radians = rotationAngle.Radians * (0f - velocity) * 0.005f;
		}
		z += velocity * Time.deltaTime;
		if (z < -391f)
		{
			World.TriggerFlag(105);
			time = -1f;
			RestoreCamera();
			isDone = true;
			return;
		}
		if (z < -70f && !triggeredFadeOut)
		{
			World.TriggerFlag(103);
			triggeredFadeOut = true;
		}
		time += Time.deltaTime * 30f;
		UpdateCamera();
	}

	public bool IsActive()
	{
		return time != -1f && !isDone;
	}
}
