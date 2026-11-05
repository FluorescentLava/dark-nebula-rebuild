public class SimpleText : Entity
{
	protected tk2dTextMesh _textMesh;

	protected tk2dTextMesh textMesh
	{
		get
		{
			if (_textMesh == null)
			{
				_textMesh = GetComponent<tk2dTextMesh>();
			}
			return _textMesh;
		}
	}

    // The bitmap font carries permanent tracking. CJK keeps its fullwidth
    // resting layout and consumes only the transient part of a button animation.
    public float AnimationSpacingOffset { get; private set; }

    public void SetAnimatedSpacing(float spacing, float restingSpacing)
    {
        AnimationSpacingOffset = spacing - restingSpacing;
        Spacing = spacing;
    }

	public float Spacing
	{
		get
		{
			return textMesh.spacing;
		}
		set
		{
			textMesh.spacing = value;
			_textMesh.text = _textMesh.text;
			_textMesh.Commit();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		base.alwaysAlive = true;
		freezable = false;
	}

    public virtual void SetLiteralText(string text)
    {
        textMesh.SetLiteralText(text);
        textMesh.Commit();
    }

	public virtual void SetText(string text)
	{
		textMesh.text = text;
		_textMesh.Commit();
	}

	public virtual void SetStringID(string stringID)
	{
		textMesh.text = Language.Get(stringID);
		_textMesh.Commit();
	}

	public void UseCurrentTextAsStringID()
	{
		if(!textMesh.IsLiteralText) SetStringID(textMesh.text);
	}
}
