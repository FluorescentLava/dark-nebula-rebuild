using UnityEngine;

public class Checkpoint : Entity
{
	private Shield shield;

	private float _z;

	private bool isShieldVisible = true;

	private void Start()
	{
		shield = GetComponentInChildren<Shield>();
		_z = base.transform.position.z;
	}

	private void Update()
	{
		if (shield == null || !World.IsAboutToShowOnScreen(_z, 30f) || !(World.vessel != null))
		{
			return;
		}
		if (World.vessel.shield < World.vessel.startShield && !UserSettings.I.expert)
		{
			if (!isShieldVisible)
			{
				AddShield();
			}
		}
		else if (isShieldVisible)
		{
			RemoveShield();
		}
	}

	private void AddShield()
	{
		isShieldVisible = true;
		shield.Show();
	}

	private void RemoveShield()
	{
		isShieldVisible = false;
		shield.Hide();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			World.vessel.TriggerCheckpoint(base.transform.position);
		}
	}
}
