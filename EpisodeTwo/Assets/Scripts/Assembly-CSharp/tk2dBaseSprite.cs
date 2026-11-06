using UnityEngine;
using tk2dRuntime;

[AddComponentMenu("2D Toolkit/Backend/tk2dBaseSprite")]
public abstract class tk2dBaseSprite : MonoBehaviour, ISpriteCollectionForceBuild
{
	public tk2dSpriteCollectionData collection;

	[SerializeField]
	protected Color _color = Color.white;

	[SerializeField]
	protected Vector3 _scale = new Vector3(1f, 1f, 1f);

	[SerializeField]
	protected int _spriteId;

	public bool pixelPerfect;

	public BoxCollider boxCollider;

	public MeshCollider meshCollider;

	public Vector3[] meshColliderPositions;

	public Mesh meshColliderMesh;

	public Color color
	{
		get
		{
			return _color;
		}
		set
		{
			if (value != _color)
			{
				_color = value;
				UpdateColors();
			}
		}
	}

	public Vector3 scale
	{
		get
		{
			return _scale;
		}
		set
		{
			if (value != _scale)
			{
				_scale = value;
				UpdateVertices();
				UpdateCollider();
			}
		}
	}

	public int spriteId
	{
		get
		{
			return _spriteId;
		}
		set
		{
			if (value != _spriteId)
			{
				value = Mathf.Clamp(value, 0, collection.spriteDefinitions.Length - 1);
				if (_spriteId < 0 || _spriteId >= collection.spriteDefinitions.Length || GetCurrentVertexCount() != collection.spriteDefinitions[value].positions.Length || collection.spriteDefinitions[_spriteId].complexGeometry != collection.spriteDefinitions[value].complexGeometry)
				{
					_spriteId = value;
					UpdateGeometry();
				}
				else
				{
					_spriteId = value;
					UpdateVertices();
				}
				UpdateMaterial();
				UpdateCollider();
			}
		}
	}

	public void FlipX()
	{
		scale = new Vector3(0f - _scale.x, _scale.y, _scale.z);
	}

	public void FlipY()
	{
		scale = new Vector3(_scale.x, 0f - _scale.y, _scale.z);
	}

	public void SwitchCollectionAndSprite(tk2dSpriteCollectionData newCollection, int newSpriteId)
	{
		if (collection != newCollection)
		{
			collection = newCollection;
			_spriteId = -1;
		}
		spriteId = newSpriteId;
		if (collection != newCollection)
		{
			UpdateMaterial();
		}
	}

	public void MakePixelPerfect()
	{
		float num = 1f;
		tk2dPixelPerfectHelper inst = tk2dPixelPerfectHelper.inst;
		if ((bool)inst)
		{
			num = ((!inst.CameraIsOrtho) ? (inst.scaleK + inst.scaleD * base.transform.position.z) : inst.scaleK);
		}
		else if ((bool)tk2dCamera.inst)
		{
			if (collection.version < 2)
			{
				Debug.LogError("Need to rebuild sprite collection.");
			}
			num = collection.halfTargetHeight;
		}
		else if ((bool)Camera.main)
		{
			if (Camera.main.orthographic)
			{
				num = Camera.main.orthographicSize;
			}
			else
			{
				float zdist = base.transform.position.z - Camera.main.transform.position.z;
				num = tk2dPixelPerfectHelper.CalculateScaleForPerspectiveCamera(Camera.main.fieldOfView, zdist);
			}
		}
		else
		{
			Debug.LogError("Main camera not found.");
		}
		num *= collection.invOrthoSize;
		scale = new Vector3(Mathf.Sign(scale.x) * num, Mathf.Sign(scale.y) * num, Mathf.Sign(scale.z) * num);
	}

	protected abstract void UpdateMaterial();

	protected abstract void UpdateColors();

	protected abstract void UpdateVertices();

	protected abstract void UpdateGeometry();

	protected abstract int GetCurrentVertexCount();

	public abstract void Build();

	public int GetSpriteIdByName(string name)
	{
		return collection.GetSpriteIdByName(name);
	}

	public static T AddComponent<T>(GameObject go, tk2dSpriteCollectionData spriteCollection, int spriteId) where T : tk2dBaseSprite
	{
		T val = go.AddComponent<T>();
		val._spriteId = -1;
		val.collection = spriteCollection;
		val.spriteId = spriteId;
		return val;
	}

	protected int GetNumVertices()
	{
		return collection.spriteDefinitions[spriteId].positions.Length;
	}

	protected int GetNumIndices()
	{
		return collection.spriteDefinitions[spriteId].indices.Length;
	}

	protected void SetPositions(Vector3[] positions, Vector3[] normals, Vector4[] tangents)
	{
		tk2dSpriteDefinition tk2dSpriteDefinition2 = collection.spriteDefinitions[spriteId];
		int numVertices = GetNumVertices();
		for (int i = 0; i < numVertices; i++)
		{
			positions[i].x = tk2dSpriteDefinition2.positions[i].x * _scale.x;
			positions[i].y = tk2dSpriteDefinition2.positions[i].y * _scale.y;
			positions[i].z = tk2dSpriteDefinition2.positions[i].z * _scale.z;
		}
		if (normals.Length > 0)
		{
			for (int j = 0; j < numVertices; j++)
			{
				normals[j] = tk2dSpriteDefinition2.normals[j];
			}
		}
		if (tangents.Length > 0)
		{
			for (int k = 0; k < numVertices; k++)
			{
				tangents[k] = tk2dSpriteDefinition2.tangents[k];
			}
		}
	}

	protected void SetColors(Color[] dest)
	{
		Color color = _color;
		if (collection.premultipliedAlpha)
		{
			color.r *= color.a;
			color.g *= color.a;
			color.b *= color.a;
		}
		int numVertices = GetNumVertices();
		for (int i = 0; i < numVertices; i++)
		{
			dest[i] = color;
		}
	}

	public Bounds GetBounds()
	{
		tk2dSpriteDefinition tk2dSpriteDefinition2 = collection.spriteDefinitions[_spriteId];
		return new Bounds(new Vector3(tk2dSpriteDefinition2.boundsData[0].x * _scale.x, tk2dSpriteDefinition2.boundsData[0].y * _scale.y, tk2dSpriteDefinition2.boundsData[0].z * _scale.z), new Vector3(tk2dSpriteDefinition2.boundsData[1].x * _scale.x, tk2dSpriteDefinition2.boundsData[1].y * _scale.y, tk2dSpriteDefinition2.boundsData[1].z * _scale.z));
	}

	public Bounds GetUntrimmedBounds()
	{
		tk2dSpriteDefinition tk2dSpriteDefinition2 = collection.spriteDefinitions[_spriteId];
		return new Bounds(new Vector3(tk2dSpriteDefinition2.untrimmedBoundsData[0].x * _scale.x, tk2dSpriteDefinition2.untrimmedBoundsData[0].y * _scale.y, tk2dSpriteDefinition2.untrimmedBoundsData[0].z * _scale.z), new Vector3(tk2dSpriteDefinition2.untrimmedBoundsData[1].x * _scale.x, tk2dSpriteDefinition2.untrimmedBoundsData[1].y * _scale.y, tk2dSpriteDefinition2.untrimmedBoundsData[1].z * _scale.z));
	}

	public tk2dSpriteDefinition GetCurrentSpriteDef()
	{
		return collection.spriteDefinitions[_spriteId];
	}

	public void Start()
	{
		if (pixelPerfect)
		{
			MakePixelPerfect();
		}
	}

	protected virtual bool NeedBoxCollider()
	{
		return false;
	}

	protected void UpdateCollider()
	{
		tk2dSpriteDefinition tk2dSpriteDefinition2 = collection.spriteDefinitions[_spriteId];
		// UI buttons own their hit boxes, including those copied at runtime.
		// A sprite without collision must not collapse that input collider.
		if (tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.None && GetComponent<SimpleButton>() != null) return;
		if (tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.Box && boxCollider == null)
		{
			boxCollider = base.gameObject.GetComponent<BoxCollider>();
			if (boxCollider == null)
			{
				boxCollider = base.gameObject.AddComponent<BoxCollider>();
			}
		}
		if (boxCollider != null)
		{
			if (tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.Box)
			{
				boxCollider.center = new Vector3(tk2dSpriteDefinition2.colliderVertices[0].x * _scale.x, tk2dSpriteDefinition2.colliderVertices[0].y * _scale.y, tk2dSpriteDefinition2.colliderVertices[0].z * _scale.z);
				boxCollider.size = (new Vector3(tk2dSpriteDefinition2.colliderVertices[1].x * _scale.x, tk2dSpriteDefinition2.colliderVertices[1].y * _scale.y, tk2dSpriteDefinition2.colliderVertices[1].z * _scale.z)) * 2f;
			}
			else if (tk2dSpriteDefinition2.colliderType != tk2dSpriteDefinition.ColliderType.Unset && boxCollider != null)
			{
				boxCollider.center = new Vector3(0f, 0f, -100000f);
				boxCollider.size = (Vector3.zero) * 2f;
			}
		}
	}

	protected void CreateCollider()
	{
		tk2dSpriteDefinition tk2dSpriteDefinition2 = collection.spriteDefinitions[_spriteId];
		if (tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.None && GetComponent<SimpleButton>() != null) return;
		if (tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.Unset)
		{
			return;
		}
		if (base.GetComponent<Collider>() != null)
		{
			boxCollider = GetComponent<BoxCollider>();
			meshCollider = GetComponent<MeshCollider>();
		}
		if ((NeedBoxCollider() || tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.Box) && meshCollider == null)
		{
			if (boxCollider == null)
			{
				boxCollider = base.gameObject.AddComponent<BoxCollider>();
			}
		}
		else if (tk2dSpriteDefinition2.colliderType == tk2dSpriteDefinition.ColliderType.Mesh && boxCollider == null)
		{
			if (meshCollider == null)
			{
				meshCollider = base.gameObject.AddComponent<MeshCollider>();
			}
			if (meshColliderMesh == null)
			{
				meshColliderMesh = new Mesh();
			}
			meshColliderPositions = new Vector3[tk2dSpriteDefinition2.colliderVertices.Length];
			for (int i = 0; i < meshColliderPositions.Length; i++)
			{
				meshColliderPositions[i] = new Vector3(tk2dSpriteDefinition2.colliderVertices[i].x * _scale.x, tk2dSpriteDefinition2.colliderVertices[i].y * _scale.y, tk2dSpriteDefinition2.colliderVertices[i].z * _scale.z);
			}
			meshColliderMesh.vertices = meshColliderPositions;
			float num = _scale.x * _scale.y * _scale.z;
			meshColliderMesh.triangles = ((!(num >= 0f)) ? tk2dSpriteDefinition2.colliderIndicesBack : tk2dSpriteDefinition2.colliderIndicesFwd);
			meshCollider.sharedMesh = meshColliderMesh;
			meshCollider.convex = tk2dSpriteDefinition2.colliderConvex;
			if ((bool)base.GetComponent<Rigidbody>())
			{
				base.GetComponent<Rigidbody>().centerOfMass = Vector3.zero;
			}
		}
		else if (tk2dSpriteDefinition2.colliderType != tk2dSpriteDefinition.ColliderType.None && Application.isPlaying)
		{
			Debug.LogError("Invalid mesh collider on sprite, please remove and try again.");
		}
		UpdateCollider();
	}

	public bool UsesSpriteCollection(tk2dSpriteCollectionData spriteCollection)
	{
		return collection == spriteCollection;
	}

	public void ForceBuild()
	{
		if (spriteId < 0 || spriteId >= collection.spriteDefinitions.Length)
		{
			spriteId = 0;
		}
		Build();
	}
}
