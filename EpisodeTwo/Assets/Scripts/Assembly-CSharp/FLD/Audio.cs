using System.Collections.Generic;
using UnityEngine;

namespace FLD
{
	public static class Audio
	{
		private static GameObject _audioContainer = null;

		private static List<AudioUnit> _activeAudioUnits = new List<AudioUnit>();

		private static bool _hasLoaded = false;

		private static IPool<AudioUnit> _availableAudioUnits;

		private static bool _muteSound = false;

		private static bool _muteMusic = false;

		private static GameObject AudioContainer
		{
			get
			{
				if (_audioContainer == null)
				{
					_audioContainer = GameObject.Find("_Audio_");
					if (_audioContainer == null)
					{
						_audioContainer = new GameObject("_Audio_");
						Object.DontDestroyOnLoad(_audioContainer);
					}
				}
				return _audioContainer;
			}
		}

		public static bool IsSoundEffectsMuted
		{
			get
			{
				Load();
				return _muteSound;
			}
			set
			{
				_muteSound = value;
				Storage.settings.Set("_muteSound", _muteSound);
				foreach (AudioUnit activeAudioUnit in _activeAudioUnits)
				{
					if (activeAudioUnit != null && activeAudioUnit.Type == AudioUnit.AudioType.SoundEffect)
					{
						activeAudioUnit.IsMute = _muteSound;
					}
				}
			}
		}

		public static bool IsMusicMuted
		{
			get
			{
				Load();
				return _muteMusic;
			}
			set
			{
				_muteMusic = value;
				Storage.settings.Set("_muteMusic", _muteMusic);
				foreach (AudioUnit activeAudioUnit in _activeAudioUnits)
				{
					if (activeAudioUnit != null && activeAudioUnit.Type == AudioUnit.AudioType.Music)
					{
						activeAudioUnit.IsMute = _muteMusic;
					}
				}
			}
		}

		private static void Load()
		{
			if (!_hasLoaded)
			{
				_muteSound = Storage.settings.GetBool("_muteSound");
				_muteMusic = Storage.settings.GetBool("_muteMusic");
				_hasLoaded = true;
			}
		}

		public static void PlayMusicWithCrossFade(AudioClip pClip, float pVolume, float pCrossFadeTime = 1f, bool pLoop = false)
		{
			if (pClip == null)
			{
				StopMusic(pCrossFadeTime);
			}
			else
			{
				if (IsMusicMuted)
				{
					return;
				}
				if (IsSameSoundPlaying("_music_", pClip))
				{
					if (GetAudioUnit("_music_").GetComponent<AudioSource>().volume != pVolume)
					{
						Debug.Log("FadeInMusic");
						ResumeMusic(pCrossFadeTime, pVolume);
					}
				}
				else if (IsSoundPlaying("_music_"))
				{
					CrossFadeMusic(pCrossFadeTime, pClip, pVolume, pLoop);
				}
				else
				{
					PlayMusic(pClip, pVolume, pLoop);
					ResumeMusic(pCrossFadeTime, -1f);
				}
			}
		}

		public static void ClearAllSounds()
		{
			List<AudioUnit> list = _activeAudioUnits.ToList();
			foreach (AudioUnit item in list)
			{
				if (item != null && item.Id != "_music_")
				{
					item.Stop(0.1f);
				}
			}
		}

		public static void PlayMusic(AudioClip pClip, float pVolume, bool pLoop = false)
		{
			if (!IsMusicMuted)
			{
				AudioUnit audioUnit = GetAudioUnit("_music_");
				if (audioUnit != null)
				{
					audioUnit.Remove();
				}
				Play("_music_", pClip, Vector3.zero, null, AudioUnit.AudioType.Music, pLoop);
				SetMusicVolume(pVolume);
				audioUnit = GetAudioUnit("_music_");
				if (audioUnit != null && audioUnit.AudioSource != null)
				{
					audioUnit.AudioSource.priority = 0;
				}
			}
		}

		public static void StopMusic(float pFadeTime = 0f)
		{
			Debug.Log("StopMusic");
			Stop("_music_", pFadeTime);
		}

		public static void SetMusicVolume(float pVolume)
		{
			SetVolume("_music_", pVolume);
		}

		public static void PauseMusic(float pFadeTime = 0f)
		{
			Pause("_music_", pFadeTime);
		}

		public static bool ResumeMusic(float pFadeTime = 0f, float pVolume = -1f)
		{
			if (IsMusicMuted)
			{
				return false;
			}
			return Resume("_music_", pFadeTime, pVolume);
		}

		public static void ChangeMusicWithFade(AudioClip pMusic, float pVolume, float pFadeTime, bool pLoop = false)
		{
			if (!Debug.isDebugBuild || pMusic != null)
			{
			}
			if (pMusic == null)
			{
				StopMusic(pFadeTime);
			}
			else
			{
				if (IsMusicMuted)
				{
					return;
				}
				if (IsSameSoundPlaying("_music_", pMusic))
				{
					if (GetAudioUnit("_music_").GetComponent<AudioSource>().volume != pVolume)
					{
						PauseMusic();
						ResumeMusic(pFadeTime, pVolume);
					}
					return;
				}
				AudioUnit audioUnit = GetAudioUnit("_music_");
				if (audioUnit == null)
				{
					PlayMusic(pMusic, pVolume, pLoop);
					ResumeMusic(pFadeTime, pVolume);
				}
				else
				{
					audioUnit.ChangeTo(pMusic, pVolume, pFadeTime, pLoop);
				}
			}
		}

		private static void CrossFadeMusic(float pFadeTime, AudioClip pMusic, float pVolume, bool pLoop = false)
		{
			AudioUnit audioUnit = GetAudioUnit("_music_");
			if (audioUnit != null)
			{
				audioUnit.Id = "_music_crossfade_" + Random.value;
				audioUnit.gameObject.name = audioUnit.Id;
				if (audioUnit.AudioSource != null)
				{
					audioUnit.AudioSource.loop = false;
				}
				audioUnit.Stop(pFadeTime);
			}
			PlayMusic(pMusic, pVolume, pLoop);
			ResumeMusic(pFadeTime, -1f);
		}

		public static void Pause(string pID, float pFadeTime = 0f)
		{
			if (IsSoundPlaying(pID))
			{
				GetAudioUnit(pID).Pause(pFadeTime);
			}
		}

		public static bool Resume(string pID, float pFadeTime = 0f, float pVolume = -1f)
		{
			AudioUnit audioUnit = GetAudioUnit(pID);
			if (audioUnit == null)
			{
				return false;
			}
			if (pVolume != -1f)
			{
				audioUnit.Resume(pFadeTime, pVolume);
			}
			else
			{
				audioUnit.Resume(pFadeTime, -1f);
			}
			return true;
		}

		public static void PlayRandom(string pID, AudioClip[] pClips)
		{
			Play(pID, GetRandomClip(pClips));
		}

		public static void PlayRandom(string pID, AudioClip[] pClips, Vector3 pPosition)
		{
			Play(pID, GetRandomClip(pClips), pPosition);
		}

		public static void PlayRandom(string pID, AudioClip[] pClips, Transform pTransform)
		{
			Play(pID, GetRandomClip(pClips), pTransform);
		}

		public static void Play(string pID, AudioClip pClip)
		{
			Play(pID, pClip, Vector3.zero, null, AudioUnit.AudioType.SoundEffect, false, false);
		}

		public static void Play(string pID, AudioClip pClip, Vector3 pPosition)
		{
			Play(pID, pClip, pPosition, null);
		}

		public static void Play(string pID, AudioClip pClip, Transform pTransform)
		{
			Play(pID, pClip, Vector3.zero, pTransform);
		}

		private static AudioUnit GetLowPrioActiveAudioUnit()
		{
			AudioUnit audioUnit = null;
			foreach (AudioUnit activeAudioUnit in _activeAudioUnits)
			{
				if (!(activeAudioUnit == null) && !(activeAudioUnit.AudioSource == null) && (audioUnit == null || audioUnit.AudioSource.priority <= activeAudioUnit.AudioSource.priority) && activeAudioUnit.Id != "_music_")
				{
					audioUnit = activeAudioUnit;
				}
			}
			return audioUnit;
		}

		private static void Play(string pID, AudioClip pClip, Vector3 pPosition, Transform pTransform, AudioUnit.AudioType pType = AudioUnit.AudioType.SoundEffect, bool pLoop = false, bool pSpatial = true)
		{
			if ((pType == AudioUnit.AudioType.SoundEffect ? IsSoundEffectsMuted : IsMusicMuted) || pClip == null || IsSoundPlaying(pID))
			{
				return;
			}
			if (pID == "extralife")
			{
				Debug.Log("debug");
			}
			if (_availableAudioUnits == null)
			{
				_availableAudioUnits = new Pool<AudioUnit>(CreateAudioUnit, 16);
			}
			AudioUnit audioUnit = _availableAudioUnits.Take();
			if (audioUnit == null)
			{
				if (Debug.isDebugBuild)
				{
					Debug.Log("Don't allow to create more audiounits... drop this sound " + pID);
				}
				return;
			}
			audioUnit.gameObject.name = pID;
			audioUnit.Id = pID;
			audioUnit.Type = pType;
			audioUnit.Volume = 1f;
			audioUnit.IsMute = ((pType != AudioUnit.AudioType.Music) ? _muteSound : _muteMusic);
			audioUnit.AudioSource.clip = pClip;
			audioUnit.AudioSource.loop = pLoop;
			audioUnit.AudioSource.playOnAwake = false;
			audioUnit.AudioSource.priority = 128;
			audioUnit.ConfigureStereo(pSpatial && pType == AudioUnit.AudioType.SoundEffect);
			audioUnit.AudioSource.Play();
			audioUnit.Play();
			if (pTransform == null)
			{
				audioUnit.transform.parent = AudioContainer.transform;
			}
			else
			{
				audioUnit.transform.parent = pTransform;
			}
			audioUnit.transform.localPosition = pPosition;
			_activeAudioUnits.Add(audioUnit);
		}

		private static AudioUnit CreateAudioUnit()
		{
			GameObject gameObject = new GameObject();
			gameObject.transform.parent = AudioContainer.transform;
			gameObject.transform.localPosition = Vector3.zero;
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			AudioUnit audioUnit = gameObject.AddComponent<AudioUnit>();
			audioUnit.AudioSource = audioSource;
			audioUnit.Removed += OnAudioRemoved;
			return audioUnit;
		}

		public static void PlayLoop(string pID, AudioClip pClip)
		{
			Play(pID, pClip, Vector3.zero, null, AudioUnit.AudioType.SoundEffect, true, false);
		}

		public static void Stop(string pID)
		{
			Stop(pID, 0f);
		}

		public static void Stop(string pID, float pFadeTime)
		{
			if (IsSoundPlaying(pID))
			{
				GetAudioUnit(pID).Stop(pFadeTime);
			}
		}

		public static void SetVolume(string pID, float pVolume)
		{
			if (IsSoundPlaying(pID))
			{
				GetAudioUnit(pID).Volume = pVolume;
			}
		}

		private static void OnAudioRemoved(AudioUnit pUnit)
		{
			if (!(pUnit == null) && _activeAudioUnits != null && _availableAudioUnits != null && !(AudioContainer == null))
			{
				if (pUnit.gameObject.name == "_music_")
				{
					Debug.Log("OnAudioRemoved _music_");
				}
				_activeAudioUnits.Remove(pUnit);
				pUnit.gameObject.name += " (Free)";
				_availableAudioUnits.Recycle(pUnit);
				pUnit.transform.parent = AudioContainer.transform;
			}
		}

		public static bool IsSoundPlaying(string pID)
		{
			return GetAudioUnit(pID) != null;
		}

		public static bool IsSameSoundPlaying(string pID, AudioClip sound)
		{
			AudioUnit audioUnit = GetAudioUnit(pID);
			return audioUnit != null && audioUnit.AudioSource != null && audioUnit.AudioSource.clip != null && audioUnit.AudioSource.clip.Equals(sound);
		}

		public static AudioUnit GetAudioUnit(string pID)
		{
			for (int i = 0; i < _activeAudioUnits.Count; i++)
			{
				if (_activeAudioUnits[i] == null)
				{
					_activeAudioUnits.RemoveAt(i--);
					continue;
				}
				if (_activeAudioUnits[i].Id.Equals(pID))
				{
					return _activeAudioUnits[i];
				}
			}
			return null;
		}

		private static AudioClip GetRandomClip(AudioClip[] pClips)
		{
			int num = pClips.Length;
			if (num == 0)
			{
				return null;
			}
			return pClips[Random.Range(0, num)];
		}
	}
}
