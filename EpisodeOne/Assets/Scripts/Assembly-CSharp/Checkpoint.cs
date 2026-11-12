using UnityEngine;

public class Checkpoint : Entity
{
	private Shield shield;

	private float _z;

	private void Start()
	{
		shield = GetComponentInChildren<Shield>();
		_z = base.transform.position.z;
	}

	private void Update()
	{
		if (!(shield == null) && World.vessel.ShieldBooster)
		{
			if (World.IsAboutToShowOnScreen(_z, 30f) && !World.vessel.HaveShield)
			{
				shield.Show();
			}
			else
			{
				shield.Hide();
			}
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			World.vessel.TriggerCheckpoint(base.transform.position);
		}
	}
}
