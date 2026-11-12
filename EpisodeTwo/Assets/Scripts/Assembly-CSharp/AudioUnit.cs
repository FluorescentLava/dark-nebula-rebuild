using System;
using UnityEngine;

public class AudioUnit : MonoBehaviour
{
	public enum AudioType
	{
		SoundEffect = 0,
		Music = 1
	}

	public string Id = string.Empty;

	public AudioSource AudioSource;

	public AudioType Type;

	private bool _isRemoved = true;

	private bool _isMute;

	private float _volume = 1f;

	private bool _pause;

	private bool _isPaused;

	private bool _stop;

	private float _fadeOutCompleteTime;

	private float _fadeOutDuration;

	private float _fadeOutFromVolume;

	private bool _fadeIn;

	private float _fadeInCompleteTime;

	private float _fadeInDuration;

	private float _pausedAt;

	private AudioClip _changeToMusic;

	private float _changeToFadeTime;

	private bool _changeToLoop;

	private float _changeToVolume;

	public float Volume
	{
		set
		{
			_volume = value;
			if (!_isMute)
			{
				AudioSource.volume = _volume;
			}
		}
	}

	public float FadeVolume
	{
		set
		{
			if (!_isMute)
			{
				AudioSource.volume = value;
			}
		}
	}

	public bool IsMute
	{
		get
		{
			return _isMute;
		}
		set
		{
			if (value)
			{
				_isMute = true;
				if (AudioSource != null)
				{
					AudioSource.volume = 0f;
				}
			}
			else
			{
				_isMute = false;
				if (AudioSource != null)
				{
					AudioSource.volume = _volume;
				}
			}
		}
	}

	public event Action<AudioUnit> Removed;
    private bool positionalStereo;
    public void ConfigureStereo(bool positional)
    {
        positionalStereo = positional;
        AudioSource.spatialBlend = 0f;
        AudioSource.panStereo = 0f;
        AudioSource.dopplerLevel = 0f;
    }
    private void LateUpdate()
    {
        if (!positionalStereo || AudioSource == null || !AudioSource.isPlaying) return;
        Camera camera = World.cameraFollow != null ? World.cameraFollow.GetComponent<Camera>() : Camera.main;
        if (camera == null) return;
        float horizontal = camera.WorldToViewportPoint(transform.position).x;
        AudioSource.panStereo = Mathf.Clamp((horizontal - .5f) * 1.7f, -.85f, .85f);
    }


	public void Play()
	{
		_isRemoved = false;
		_pause = _isPaused = _stop = _fadeIn = false;
		_pausedAt = 0f;
		_changeToMusic = null;
	}

	public void Pause(float pFadeTime = 0f)
	{
		if (_isRemoved || _isPaused) return;
		_pause = true;
		BeginStop(pFadeTime);
	}

	public void Resume(float pFadeTime = 0f, float pVolume = -1f)
	{
		if (_isRemoved || AudioSource == null) return;
		// A HUD resume during a fade must resume the requested track, including
		// when the previous scene's music was paused before the switch finished.
		ApplyPendingMusic();
		_stop = false;
		_pause = false;
		if (pVolume != -1f)
		{
			_volume = pVolume;
		}
		if (pFadeTime <= 0f)
		{
			_fadeIn = false;
			FadeVolume = _volume;
		}
		else
		{
			_fadeInDuration = pFadeTime;
			_fadeInCompleteTime = Time.realtimeSinceStartup + _fadeInDuration;
			_fadeIn = true;
			FadeVolume = 0f;
		}
		if (_isPaused)
		{
			AudioSource.UnPause();
			_isPaused = false;
		}
		else if (!AudioSource.isPlaying && !(AudioListener.pause && !AudioSource.ignoreListenerPause))
		{
			AudioSource.Play();
			AudioSource.time = _pausedAt;
		}
	}

	public void ChangeTo(AudioClip pMusic, float pVolume, float pFadeTime, bool pLoop)
	{
		if (_isRemoved) return;
		Debug.Log("AudioUnity.ChangeTo " + pMusic.name);
		_changeToMusic = pMusic;
		_changeToFadeTime = pFadeTime;
		_changeToLoop = pLoop;
		_changeToVolume = pVolume;
		_pause = false;
		BeginStop(pFadeTime / 2f);
	}

	private void ApplyPendingMusic()
	{
		if (_changeToMusic == null) return;
		AudioSource.Stop();
		AudioSource.clip = _changeToMusic;
		AudioSource.loop = _changeToLoop;
		_volume = _changeToVolume;
		_changeToMusic = null;
		_pausedAt = 0f;
		_isPaused = false;
	}

	private void Update()
	{
		if (_isRemoved) return;
		if (AudioSource == null || AudioSource.clip == null)
		{
			Remove();
			return;
		}
		// A listener pause makes isPlaying false for every ordinary source.
		// Keep its cursor, ownership and fades intact until the movie returns.
		if (AudioListener.pause && !AudioSource.ignoreListenerPause)
		{
			if (_stop) _fadeOutCompleteTime += Time.unscaledDeltaTime;
			if (_fadeIn) _fadeInCompleteTime += Time.unscaledDeltaTime;
			return;
		}
		if (_stop)
		{
			float num = Mathf.Max(0f, _fadeOutCompleteTime - Time.realtimeSinceStartup);
			FadeVolume = _fadeOutDuration > 0f ? _fadeOutFromVolume * (num / _fadeOutDuration) : 0f;
			_pausedAt = AudioSource.time;
			if (num <= 0f)
			{
				if (_pause)
				{
					_pause = false;
					AudioSource.Pause();
					_isPaused = true;
				}
				else
				{
					if (_changeToMusic != null)
					{
						ApplyPendingMusic();
						Resume(_changeToFadeTime / 2f, -1f);
						return;
					}
					Remove();
					return;
				}
				_stop = false;
			}
		}
		else if (_fadeIn)
		{
			float num2 = Mathf.Max(0f, _fadeInCompleteTime - Time.realtimeSinceStartup);
			FadeVolume = _volume * (1f - num2 / _fadeInDuration);
			if (num2 <= 0f)
			{
				_fadeIn = false;
			}
		}
		if (!_isPaused && !_pause && !AudioSource.isPlaying)
		{
			Remove();
		}
	}

	public void Stop(float pFadeTime)
	{
		_changeToMusic = null;
		_pause = false;
		BeginStop(pFadeTime);
	}

	private void BeginStop(float pFadeTime)
	{
		if (_isRemoved) return;
		_fadeIn = false;
		_stop = true;
		_fadeOutDuration = Mathf.Max(0f, pFadeTime);
		_fadeOutCompleteTime = Time.realtimeSinceStartup + _fadeOutDuration;
		if (AudioSource != null)
		{
			_fadeOutFromVolume = AudioSource.volume;
		}
		else
		{
			_fadeOutFromVolume = 0f;
		}
		if (_fadeOutFromVolume == 0f && _changeToMusic == null && !_pause)
		{
			Remove();
		}
	}

	internal void Remove()
	{
		if (!_isRemoved)
		{
			_isRemoved = true;
			_pause = _isPaused = _stop = _fadeIn = false;
			_changeToMusic = null;
			if (AudioSource != null) AudioSource.Stop();
			if (Removed != null)
			{
				Removed(this);
			}
		}
	}
}
