using System;
using System.Collections.Generic;
using UnityEngine;

public class SwipeSelector : MonoBehaviour
{
	public Camera ViewCamera;
	private Camera InputCamera { get { return ViewCamera != null ? ViewCamera : Camera.main; } }
	private const float SwipeThresholdTime = 0.2f;

	private const float SwipeThresholdDistance = 0.1f;

	[SerializeField]
	private Collider _touchCollider;

	[SerializeField]
	private Transform _movingObject;

	[SerializeField]
	private Transform[] _selectableObjects;

	[SerializeField]
	private float _slideTime = 0.3f;

	private int _selectedIndex;

	private bool _isTouching;

	private float _touchStartTime;

	private Vector2 _touchStartPos;

	private Vector3 _moverStartPos;

	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			int selectedIndex = _selectedIndex;
			if (value >= 0 && value < _selectableObjects.Length)
			{
				_selectedIndex = value;
			}
			MoveTo(_selectedIndex);
			if (SelectedIndexChanged != null && selectedIndex != _selectedIndex)
			{
				SelectedIndexChanged(_selectedIndex);
			}
		}
	}

	public event Action<int> SelectedIndexChanged;

	public void Add(Transform pObject)
	{
		pObject.parent = _movingObject;
		List<Transform> list = new List<Transform>(_selectableObjects);
		list.Add(pObject);
		_selectableObjects = list.ToArray();
	}

	public void Add(Transform pObject, Vector3 pAtPosition)
	{
		Add(pObject);
		pObject.localPosition = pAtPosition;
	}

    public void Reorder(Transform[] objects, float spacing, int selected)
    {
        Go.killAllTweensWithTarget(_movingObject);
        _isTouching=false;
        _selectableObjects=objects;
        for(int i=0;i<objects.Length;i++) objects[i].localPosition=new Vector3(i*spacing,0,0);
        _selectedIndex=selected;
        SnapTo(selected);
    }

	private void Start()
	{
		MoveTo(_selectedIndex);
	}

	private void Update()
	{
		if (Input.GetMouseButton(0))
		{
			if (InputCamera == null || _selectableObjects.Length == 0) return;
			Ray ray = InputCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hitInfo;
			if (Physics.Raycast(ray, out hitInfo, float.MaxValue, InputCamera.cullingMask) && hitInfo.collider == _touchCollider)
			{
				if (!_isTouching)
				{
					_isTouching = true;
					BeginTouchAt(Input.mousePosition);
				}
				ContinueTouchAt(Input.mousePosition);
			}
		}
		else if (_isTouching)
		{
			_isTouching = false;
			EndTouchAt(Input.mousePosition);
		}
	}

	private void BeginTouchAt(Vector2 pPos)
	{
		_touchStartPos = pPos;
		_moverStartPos = _movingObject.localPosition;
		_touchStartTime = Time.unscaledTime;
	}

	private void ContinueTouchAt(Vector2 pPos)
	{
		float num = 2f * InputCamera.orthographicSize * InputCamera.aspect;
		Vector3 localPosition = _moverStartPos + new Vector3((pPos.x - _touchStartPos.x) / (float)Screen.width * num, 0f, 0f);
		float num2 = 0f;
		float num3 = 0f - _selectableObjects[_selectableObjects.Length - 1].localPosition.x;
		if (localPosition.x > num2)
		{
			localPosition.x = (localPosition.x - num2) / 3f;
		}
		else if (localPosition.x < num3)
		{
			localPosition.x = num3 + (localPosition.x - num3) / 3f;
		}
		_movingObject.localPosition = localPosition;
	}

	private void EndTouchAt(Vector2 pPos)
	{
		float num = Time.unscaledTime - _touchStartTime;
		float num2 = (pPos.x - _touchStartPos.x) / (float)Screen.width;
		if (num < 0.2f && Mathf.Abs(num2) >= 0.1f)
		{
			if (num2 > 0f)
			{
				SelectedIndex--;
			}
			else
			{
				SelectedIndex++;
			}
		}
		else
		{
			SelectedIndex = GetClosestIndexToPosition();
		}
	}

	private int GetClosestIndexToPosition()
	{
		float x = base.transform.position.x;
		float num = float.MaxValue;
		int result = -1;
		for (int i = 0; i < _selectableObjects.Length; i++)
		{
			float num2 = Mathf.Abs(_selectableObjects[i].position.x - x);
			if (num2 < num)
			{
				result = i;
				num = num2;
			}
		}
		return result;
	}

	public void MoveTo(int pIndex)
	{
		Go.killAllTweensWithTarget(_movingObject);
		Go.to(_movingObject, _slideTime, new GoTweenConfig().localPosition(GetOffsetFor(pIndex)).setEaseType(GoEaseType.QuadOut).setUpdateType(GoUpdateType.TimeScaleIndependentUpdate));
	}

	public void SnapTo(int pIndex)
	{
		_movingObject.localPosition = GetOffsetFor(pIndex);
	}

	public void SlowMoveTo(int pIndex)
	{
		SlowMoveWithDelayTo(pIndex, 0f);
	}

	public void SlowMoveWithDelayTo(int pIndex, float pDelay)
	{
		Go.killAllTweensWithTarget(_movingObject);
		Go.to(_movingObject, _slideTime * 2f, new GoTweenConfig().localPosition(GetOffsetFor(pIndex)).setEaseType(GoEaseType.QuadOut).setDelay(pDelay));
	}

	private Vector3 GetOffsetFor(int pIndex)
	{
		if (pIndex < 0 || pIndex >= _selectableObjects.Length)
		{
			return Vector3.zero;
		}
		return -_selectableObjects[pIndex].localPosition;
	}
}
