using System.Collections.Generic;
using UnityEngine;

public class CurveAnimation
{
	private bool isPaused = true;

	private float startTime;

	public List<CurveAnimationNode> nodes = new List<CurveAnimationNode>();

	public CurveAnimationNode currentNode;

	private CurveAnimationNode nextNode;

	public bool shouldMoveInSyncWithOthers;

	public bool shouldLoop = true;

	private float time;

	private float currentAddTime;

	private Vector2 position;

	public bool isDone
	{
		get
		{
			return !isPaused && (currentNode == null || currentNode.endTime > 9999f);
		}
	}

	public void pause()
	{
		isPaused = true;
	}

	public void rewind()
	{
		prepare();
		startTime = World.AnimationTimeInSeconds;
		calculateTime();
	}

	public void play()
	{
		isPaused = false;
		startTime = World.AnimationTimeInSeconds;
	}

	public void prepare()
	{
		if (nodes.Count >= 2)
		{
			currentNode = nodes[0];
			nextNode = nodes[1];
		}
	}

	private void calculateTime()
	{
		if (shouldMoveInSyncWithOthers)
		{
			time = World.AnimationTimeInSeconds;
		}
		else
		{
			time = World.AnimationTimeInSeconds - startTime;
		}
		if (shouldLoop)
		{
			time %= currentAddTime;
		}
	}

	public Vector2 getPosition()
	{
		return position;
	}

	public void update()
	{
		if (!isPaused)
		{
			calculateTime();
		}
		if (currentNode == null)
		{
			return;
		}
		if (time < currentNode.time)
		{
			rewind();
		}
		while (time >= currentNode.endTime)
		{
			currentNode.exit();
			nextNodes();
			if (currentNode == null)
			{
				return;
			}
		}
		float num = nextNode.time - currentNode.time;
		float t = ((!(num > 0.001f)) ? 1f : ((time - currentNode.time) / num));
		t = currentNode.convertTime(t);
		position = currentNode.getPosition(nextNode, t);
	}

	private void nextNodes()
	{
		int num = nodes.IndexOf(currentNode);
		num++;
		if (num + 1 >= nodes.Count)
		{
			num = 0;
			if (!shouldLoop)
			{
				currentNode = null;
				return;
			}
		}
		currentNode = nodes[num];
		if (currentNode == null)
		{
		}
		currentNode.enter();
		num++;
		nextNode = nodes[num];
	}

	public void addNode(Vector2 p, float duration, CurveAnimationTimeFunction timeFunction)
	{
		CurveAnimationNode item = new CurveAnimationNode(p, currentAddTime, duration, timeFunction);
		currentAddTime += duration;
		nodes.Add(item);
	}
}
