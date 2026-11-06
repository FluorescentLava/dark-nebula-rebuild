using System;
using FLD;
using UnityEngine;

public class AnimatedEntity : Entity
{
	public float spriteAnimationSpeed;

	public bool spriteAnimationShouldLoop;

	public AudioClip TakeOffSound;

	public AudioClip ArriveSound;

	public bool canBeBorded;

	public bool isCrusher;

	public int activateEventFlag;

	public int activateEventCount;

	public int onboardTriggerFlag;

	public bool dontAffectPlayer;

	public bool animationOneShot;

	private FlagConditionChanged vesselRespawnedCondition;

	private FlagCondition activatePresentationEvent;

	private FlagCondition activateEvent;

	protected CurveAnimation curveAnimation;

	protected CurveAnimation presentationAnimation;

	protected bool onPresentationStarted;

	protected Vector2 localPos;

	protected bool hasReportedDone;

	protected bool wantsVerlet;

	protected tk2dSprite _sprite;

	protected SpriteAnimation spriteAnimation;

	protected Type shapeType = typeof(BoxCollider);

	protected CurveAnimationNode lastNode;

	protected virtual tk2dSprite Sprite
	{
		get
		{
			if (_sprite == null)
			{
				_sprite = GetComponentInChildren<tk2dSprite>();
			}
			return _sprite;
		}
	}

	private bool startedByPlayer
	{
		get
		{
			return onboardTriggerFlag == activateEventFlag && onboardTriggerFlag != 0;
		}
	}

	private void Start()
	{
		if (isCreatedFromLevel)
		{
			PostInit();
		}
	}

	public override bool PostInit()
	{
		base.keepalive = GetParameterBool("keepalive");
		base.alwaysAlive = GetParameterBool("alwaysAlive");
		if (base.PostInit())
		{
			return true;
		}
		if (nodes.Count > 0)
		{
			addCurveAnimationNodes();
			if (base.GetComponent<Rigidbody>() == null)
			{
				base.gameObject.AddComponent<Rigidbody>();
			}
			base.GetComponent<Rigidbody>().isKinematic = true;
			base.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
			if (Sprite != null)
			{
				Sprite.transform.Translate(0f, 0f, -0.01f);
			}
		}
		if (activateEventFlag != -1 || curveAnimation != null)
		{
			activateEvent = new FlagCondition(activateEventFlag, activateEventCount);
			activateEvent.Callback += OnActivateEvent;
		}
		if (spriteAnimationSpeed != 0f)
		{
			spriteAnimation = new SpriteAnimation(Sprite, GetParameterString("image"), spriteAnimationSpeed);
			if (spriteAnimationShouldLoop)
			{
				spriteAnimation.LoopSpriteAnimation();
			}
			else
			{
				spriteAnimation.PlaySpriteAnimation();
			}
		}
		if (GetParameterBool("givesDamage"))
		{
			base.GetComponent<Collider>().isTrigger = false;
		}
		wantsVerlet = !canBeBorded;
		activatePresentationEvent = new FlagCondition(GetParameterInt("activatePresentationEventFlag"), GetParameterInt("activatePresentationEventCount"));
		activatePresentationEvent.Callback += OnStartPresentation;
		if (GetParameterInt("activatePresentationEventCount") > 0)
		{
			base.GetComponent<Collider>().enabled = false;
			SetRenderersEnabled(false);
		}
		if (NeedsTallScanEntry())
		{
			GetTallScanEntry().HideUntilTriggered(this);
		}
		World.SignalVesselDied += OnVesselDied;
		vesselRespawnedCondition = new FlagConditionChanged(110);
		vesselRespawnedCondition.Callback += OnVesselRespawned;
		return false;
	}

	protected override void OnDestroy()
	{
		if (activatePresentationEvent != null)
		{
			activatePresentationEvent.Callback -= OnStartPresentation;
			activatePresentationEvent.Dispose();
			activatePresentationEvent = null;
		}
		if (activateEvent != null)
		{
			activateEvent.Callback -= OnActivateEvent;
			activateEvent.Dispose();
			activateEvent = null;
		}
		if (vesselRespawnedCondition != null)
		{
			vesselRespawnedCondition.Callback -= OnVesselRespawned;
			vesselRespawnedCondition.Dispose();
			vesselRespawnedCondition = null;
		}
		World.SignalVesselDied -= OnVesselDied;
		CancelInvoke("Reset");
		base.OnDestroy();
	}

	private void OnVesselDied()
	{
		if (!(base.gameObject == null))
		{
			Invoke("Reset", 3f);
		}
	}

	private void OnStartPresentation()
	{
		onPresentationStarted = true;
		base.GetComponent<Collider>().enabled = true;
		SetRenderersEnabled(true);
		presentationAnimation = new CurveAnimation();
		presentationAnimation.shouldLoop = false;
		AnimatedEntityNode animatedEntityNode = nodes[0];
		Vector2 position = animatedEntityNode.position;
		Vector2 p = position + new Vector2(0f, -100f);
		CurveAnimationTimeFunction timeFunction = new CurveAnimationTimeFunction(0.1f, 0.2f, 3f);
		presentationAnimation.addNode(p, 3f, timeFunction);
		presentationAnimation.addNode(position, 0f, null);
		presentationAnimation.prepare();
		presentationAnimation.play();
	}

	private void vesselAttached()
	{
		World.TriggerFlag(onboardTriggerFlag);
	}

	public virtual void Update()
	{
		if (isCrusher || dontAffectPlayer)
		{
			updateCrusher();
		}
		if (spriteAnimation != null)
		{
			spriteAnimation.Update();
		}
		if (curveAnimation == null)
		{
			return;
		}
		if (presentationAnimation != null)
		{
			presentationAnimation.update();
			SetPosition(presentationAnimation.getPosition());
			if (presentationAnimation.isDone)
			{
				presentationAnimation = null;
			}
		}
		else
		{
			curveAnimation.update();
			if (!hasReportedDone && curveAnimation.isDone)
			{
				OnAnimationDone();
				hasReportedDone = true;
			}
			else
			{
				SetPosition(curveAnimation.getPosition());
			}
		}
	}

	public override void Defrost()
	{
		base.Defrost();
		// Catch up while still outside the visible area, before the next render.
		if(presentationAnimation!=null)
		{
			presentationAnimation.update();SetPosition(presentationAnimation.getPosition());
		}
		else if(curveAnimation!=null)
		{
			curveAnimation.update();SetPosition(curveAnimation.getPosition());
		}
	}

	private void SetPosition(Vector2 pPos)
	{
		localPos = pPos;
		Vector3 localPosition = base.transform.localPosition;
		localPosition.x = localPos.x;
		localPosition.z = 0f - localPos.y;
		base.transform.localPosition = localPosition;
	}

	private void updateCrusher()
	{
		CurveAnimationNode currentNode = curveAnimation.currentNode;
		CurveAnimationNode curveAnimationNode = curveAnimation.nodes[0];
		if (currentNode == lastNode)
		{
			return;
		}
		if (!World.IsFrozen(base.transform.position.z))
		{
			if (curveAnimationNode == currentNode)
			{
				Audio.Play("TakeOffSound" + UnityEngine.Random.value, TakeOffSound, transform);
			}
			else
			{
				Audio.Play("ArriveSound" + UnityEngine.Random.value, ArriveSound, transform);
			}
		}
		lastNode = currentNode;
	}

	public virtual void Reset()
	{
		hasReportedDone = false;
		var scanEntry = GetComponent<TallScanEntry>();
		if (scanEntry != null) scanEntry.HideUntilTriggered(this);
		if (!isCrusher)
		{
			if (curveAnimation != null)
			{
				curveAnimation.rewind();
				curveAnimation.pause();
			}
			World.ResetFlag(onboardTriggerFlag);
			if (activateEvent != null)
			{
				activateEvent.Reset();
			}
			CancelInvoke("Reset");
		}
	}

	public virtual void OnAnimationDone()
	{
		Audio.Play("ArriveSound", ArriveSound, transform);
	}

	private void OnVesselRespawned()
	{
		if (activateEventFlag != -1)
		{
			Reset();
		}
	}

	Vector2[] authoredScanPositions;
	int scanExitIndex;

    bool NeedsTallScanEntry()
    {
        return activateEventFlag != -1
            && GetParameterString("image") == "entity_shockwave" && curveAnimation != null
            && curveAnimation.nodes.Count > 0 && World.cameraFollow != null
            && World.cameraFollow.GetComponent<Camera>().aspect < LegacyScreenFit.ReferenceAspect - .001f;
    }

    TallScanEntry GetTallScanEntry()
    {
        var entry = GetComponent<TallScanEntry>();
        return entry != null ? entry : gameObject.AddComponent<TallScanEntry>();
    }

    public virtual void OnActivateEvent()
	{
        // Native triggers run in the physics step. Resolve the scan entry after
        // the camera and its scrolling parent have settled for this frame.
        if (NeedsTallScanEntry())
        {
            curveAnimation.pause();
            GetTallScanEntry().Schedule(this);
            return;
        }
		var scanEntry = GetComponent<TallScanEntry>();
		if (scanEntry != null) scanEntry.Reveal();
		playAnimation();
		if (IsFrozen)
		{
			Defrost();
		}
	}

    internal void StartTallScan()
    {
        if (curveAnimation == null || World.cameraFollow == null || Sprite == null) return;
        if (authoredScanPositions == null)
        {
            authoredScanPositions = new Vector2[curveAnimation.nodes.Count];
            for (int i = 0; i < authoredScanPositions.Length; i++)
                authoredScanPositions[i] = curveAnimation.nodes[i].position;
        }
        for (int i = 0; i < authoredScanPositions.Length; i++)
            curveAnimation.nodes[i].position = authoredScanPositions[i];

        Vector2 first = authoredScanPositions[0];
        SetPosition(first);
        Camera camera = World.cameraFollow.GetComponent<Camera>();
        Bounds bounds = Sprite.GetComponent<Renderer>().bounds;
        float depth = camera.WorldToViewportPoint(bounds.center).z;
        float screenTop = camera.ViewportToWorldPoint(new Vector3(.5f, 1f, depth)).z;
        float extra = Mathf.Max(0f, screenTop - bounds.min.z + 1f);
        Vector3 offset = Vector3.forward * extra;
        if (transform.parent != null) offset = transform.parent.InverseTransformVector(offset);
        Vector2 entry = first + new Vector2(offset.x, -offset.z);

        // The curve contains copies for pauses and its terminal pose. Keep all
        // copies of each authored endpoint together when adapting tall screens.
        scanExitIndex = 0;
        for (int i = 1; i < authoredScanPositions.Length; i++)
            if (authoredScanPositions[i].y > authoredScanPositions[scanExitIndex].y) scanExitIndex = i;
        for (int i = 0; i < authoredScanPositions.Length; i++)
            if (authoredScanPositions[i] == first) curveAnimation.nodes[i].position = entry;
        SetPosition(entry);
        var body = GetComponent<Rigidbody>();
        if (body != null && body.isKinematic) body.position = transform.position;
        playAnimation();
        if (IsFrozen) Defrost();
    }

    internal bool UpdateTallScanExit()
    {
        // The short background sweeps in these two level variants can finish
        // inside a tall view when the player backs up during their two seconds.
        if ((World.levelNum != 1 && World.levelNum != 21)
            || (activateEventFlag != 10 && activateEventFlag != 11)
            || !NeedsTallScanEntry() || authoredScanPositions == null) return false;

        Vector2 exit = curveAnimation.nodes[scanExitIndex].position;
        Vector3 endpoint = transform.localPosition;
        endpoint.x = exit.x;
        endpoint.z = -exit.y;
        if (transform.parent != null) endpoint = transform.parent.TransformPoint(endpoint);
        Bounds bounds = Sprite.GetComponent<Renderer>().bounds;
        bounds.center += endpoint - transform.position;
        Camera camera = World.cameraFollow.GetComponent<Camera>();
        float depth = camera.WorldToViewportPoint(bounds.center).z;
        float screenBottom = camera.ViewportToWorldPoint(new Vector3(.5f, 0f, depth)).z;
        float extra = Mathf.Max(0f, bounds.max.z - screenBottom + 1f);
        if (extra > 0f)
        {
            Vector3 offset = Vector3.back * extra;
            if (transform.parent != null) offset = transform.parent.InverseTransformVector(offset);
            exit += new Vector2(offset.x, -offset.z);
            for (int i = 0; i < authoredScanPositions.Length; i++)
                if (authoredScanPositions[i] == authoredScanPositions[scanExitIndex])
                    curveAnimation.nodes[i].position = exit;
        }
        bool moving = !curveAnimation.isDone;
        if (extra > 0f || !moving)
        {
            // Re-evaluate this same native curve time after the camera's
            // LateUpdate, including the final frame before its long pause.
            curveAnimation.update();
            SetPosition(curveAnimation.getPosition());
            var body = GetComponent<Rigidbody>();
            if (body != null && body.isKinematic) body.position = transform.position;
        }
        return moving;
    }

	private void playAnimation()
	{
		if (!isCrusher && !World.IsFrozen(base.transform.position.z))
		{
			Audio.Play("TakeOffSound" + UnityEngine.Random.value, TakeOffSound, transform);
		}
		if (curveAnimation != null)
		{
			curveAnimation.play();
		}
	}

	public void addCurveAnimationNodes()
	{
		curveAnimation = new CurveAnimation();
		int num = 1;
		int index = 0;
		curveAnimation.shouldLoop = !animationOneShot;
		curveAnimation.shouldMoveInSyncWithOthers = !startedByPlayer && !animationOneShot && (activateEventFlag == 0 || activateEventFlag == -1);
		foreach (AnimatedEntityNode node in nodes)
		{
			if (num == nodes.Count)
			{
				if (animationOneShot)
				{
					index = num - 1;
					break;
				}
				num = 0;
			}
			AnimatedEntityNode nextNode = nodes[num];
			addToAnimation(curveAnimation, node, nextNode);
			num++;
		}
		AnimatedEntityNode animatedEntityNode = nodes[index];
		Vector2 p = default(Vector2);
		p.x = animatedEntityNode.x;
		p.y = animatedEntityNode.y;
		curveAnimation.addNode(p, 0f, null);
		curveAnimation.prepare();
		curveAnimation.update();
	}

	private void addToAnimation(CurveAnimation pAnimation, AnimatedEntityNode node, AnimatedEntityNode nextNode)
	{
		Vector2 position = default(Vector2);
		position.x = node.x;
		position.y = node.y;
		float easeout = node.easeout;
		float easein = nextNode.easein;
		addNodeToAnimation(pAnimation, position, node.time, easeout, easein);
		float num = 0f;
		if (node.pause < 0f)
		{
			num = 100000000f;
		}
		else if (node.pause > 0.001f)
		{
			num = node.pause;
		}
		if (num > 0.001f)
		{
			Vector2 p = default(Vector2);
			p.x = nextNode.x;
			p.y = nextNode.y;
			pAnimation.addNode(p, num, null);
		}
	}

	private void addNodeToAnimation(CurveAnimation pAnimation, Vector2 position, float duration, float _startEase, float _endEase)
	{
		CurveAnimationTimeFunction timeFunction = new CurveAnimationTimeFunction(_startEase, _endEase, duration);
		pAnimation.addNode(position, duration, timeFunction);
	}

	private Collider GetOrCreateCollider()
	{
		if (shapeType == typeof(BoxCollider))
		{
			BoxCollider boxCollider = GetComponent<BoxCollider>();
			if (boxCollider == null)
			{
				boxCollider = base.gameObject.AddComponent<BoxCollider>();
			}
			boxCollider.size = new Vector3(GetParameterFloat("width"), GetParameterFloat("height"), 256f);
			return boxCollider;
		}
		if (shapeType == typeof(CapsuleCollider))
		{
			CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
			if (capsuleCollider == null)
			{
				capsuleCollider = base.gameObject.AddComponent<CapsuleCollider>();
			}
			capsuleCollider.radius = GetParameterFloat("width") * 0.5f;
			capsuleCollider.height = 256f + capsuleCollider.radius * 2f;
			capsuleCollider.direction = 2;
			return capsuleCollider;
		}
		return null;
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

	protected void InitParameters(bool IsRuntime)
	{
		string text = GetParameterString("image");
		spriteAnimationSpeed = GetParameterFloat("spriteAnimationSpeed");
		spriteAnimationShouldLoop = GetParameterBool("spriteAnimationShouldLoop");
		activateEventFlag = GetParameterInt("activateEventFlag");
		activateEventCount = GetParameterInt("activateEventCount");
		onboardTriggerFlag = GetParameterInt("onboardTriggerFlag");
		canBeBorded = GetParameterBool("canBeBorded");
		isCrusher = GetParameterBool("isCrusher");
		dontAffectPlayer = GetParameterBool("dontAffectPlayer");
		animationOneShot = GetParameterBool("animationOneShot");
		if (spriteAnimationSpeed != 0f)
		{
			text += "/0";
		}
		Sprite.spriteId = Sprite.GetSpriteIdByName(text);
		if (Sprite.spriteId == 0 && text != "entity_generator_blue/0" && !string.IsNullOrEmpty(text))
		{
			Sprite.spriteId = Sprite.GetSpriteIdByName(text);
			Debug.LogWarning("Image not found: " + text);
		}
		canBeBorded = GetParameterBool("canBeBorded");
		if (canBeBorded || GetParameterBool("givesDamage") || GetParameterBool("isCrusher"))
		{
			if (canBeBorded)
			{
				base.tag = "CanBeBorded";
			}
			Collider orCreateCollider = GetOrCreateCollider();
			orCreateCollider.isTrigger = true;
			if (!dontAffectPlayer && isCrusher)
			{
				orCreateCollider.isTrigger = false;
				TakeOffSound = GetClip(IsRuntime, GetParameterString("takeOffSoundName"));
				ArriveSound = GetClip(IsRuntime, GetParameterString("arriveSoundName"));
			}
			else if (dontAffectPlayer)
			{
				TakeOffSound = (ArriveSound = GetClip(IsRuntime, "LaserWall7"));
			}
			if (startedByPlayer && !isCrusher)
			{
				TakeOffSound = GetClip(IsRuntime, GetParameterString("takeOffSoundName"));
				ArriveSound = GetClip(IsRuntime, GetParameterString("arriveSoundName"));
			}
			if (dontAffectPlayer)
			{
				orCreateCollider.isTrigger = true;
			}
		}
		if (GetParameterBool("hasJumpPad"))
		{
			float parameterFloat = GetParameterFloat("jumpPadOffsetY");
			float parameterFloat2 = GetParameterFloat("jumpPadOffsetX");
			Bump componentInChildren = GetComponentInChildren<Bump>();
			GameObject gameObject = null;
			gameObject = ((componentInChildren != null) ? componentInChildren.gameObject : ((!IsRuntime) ? ObjectPool.instance.GetObjectForType<Bump>().gameObject : LevelEP2Runtime.Instatiate("Assets/Prefabs/Entities/BumpEntity.prefab")));
			if (gameObject == null)
			{
				Debug.LogError("Could not create bump on animatedEntity");
				return;
			}
			gameObject.transform.parent = base.transform;
			gameObject.transform.localPosition = new Vector3(parameterFloat2, 0f - parameterFloat, 0f);
			gameObject.GetComponent<Bump>().keepalive = true;
		}
	}

	private AudioClip GetClip(bool IsRuntime, string name)
	{
		AudioClip audioClip = null;
		audioClip = ((!IsRuntime) ? LevelAssets.GetAudio(name) : LevelAssets.GetAudio(name));
		if (audioClip == null)
		{
			Debug.LogWarning("Missing sound " + name);
		}
		return audioClip;
	}

	private GameObject InstatiatePrefab(string path)
	{
		return null;
	}

	private void VesselAttached()
	{
		World.TriggerFlag(onboardTriggerFlag);
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			World.vessel.TriggerRecieveDamage();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (canBeBorded && World.IsRunning && !IsPooled && other.CompareTag("Player"))
		{
			Board(World.vessel as StandardVessel);
		}
	}

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) Board(World.vessel as StandardVessel);
    }

    public void Board(StandardVessel vessel)
    {
        if (!canBeBorded || !World.IsRunning || IsPooled || vessel == null || vessel.HasPlatform(transform)) return;
        vessel.SetPlatform(transform);
        CancelInvoke("VesselAttached");
        Invoke("VesselAttached", 0.8f);
    }

	private void OnTriggerExit(Collider other)
	{
		if (canBeBorded && other.CompareTag("Player"))
		{
			CancelInvoke("VesselAttached");
			var vessel = World.vessel as StandardVessel;
			if (vessel != null) vessel.RemovePlatform(base.transform);
		}
	}

	public override void DisableAlwaysAliveAndKeepaliveEntities()
	{
	}
}
