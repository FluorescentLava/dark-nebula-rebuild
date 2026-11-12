public class VesselRespawn : Entity
{
	private FlagCondition activeEvent;

	protected override void Awake()
	{
		base.Awake();
		base.alwaysAlive = true;
	}

	private void Start()
	{
		activeEvent = new FlagCondition(GetParameterInt("activateEventFlag"), GetParameterInt("activateEventCount"));
		activeEvent.Callback += OnActivateEvent;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (activeEvent != null)
		{
			activeEvent.Callback -= OnActivateEvent;
			activeEvent.Dispose();
			activeEvent = null;
		}
	}

	public void OnActivateEvent()
	{
		StandardVessel standardVessel = World.vessel as StandardVessel;
		if (standardVessel == null)
		{
			standardVessel = ObjectPool.instance.GetObjectForType<StandardVessel>();
			standardVessel.transform.parent = base.transform.parent;
			standardVessel.transform.position = base.transform.position;
			standardVessel.PostInit();
			World.vessel = standardVessel;
		}
		standardVessel.canMove = false;
		standardVessel.Hide();
		standardVessel.Layer = GetParameterInt("layer");
		standardVessel.SetSpawnPosition(base.transform.position);
		standardVessel.SpawnerType = GetParameterInt("spawnertype");
		standardVessel.InitiateSpawner((VesselSkinType)GetParameterInt("skinType"), false, GetParameterInt("doneFlag"), GetParameterInt("scrollInLength"), GetParameterInt("scrollInTime"));
	}

	public override void DisableAlwaysAliveAndKeepaliveEntities()
	{
	}
}
