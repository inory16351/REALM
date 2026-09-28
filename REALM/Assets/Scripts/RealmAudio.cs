using UnityEngine;

namespace Realm
{
    // Background music driven by the server's phase. Two AudioSources are kept
    // so a track change can cross-fade instead of cutting; whichever is idle
    // picks up the new clip and the pair swap roles.
    public class RealmAudio : MonoBehaviour
    {
        [SerializeField] public RealmSplash splash;
        [Range(0f, 1f)]
        [SerializeField] public float volume = 0.45f;
        [SerializeField] public float fadeSeconds = 1.4f;

        private AudioSource _front;
        private AudioSource _back;
        private AudioClip _title, _lobby, _round, _discussion, _results;
        private AudioClip _current;
        private float _fade = 1f;          // 1 = fade finished
        private string _phase;

        private void Awake()
        {
            _front = gameObject.AddComponent<AudioSource>();
            _back = gameObject.AddComponent<AudioSource>();
            foreach (var src in new[] { _front, _back })
            {
                src.playOnAwake = false;
                src.loop = true;
                src.volume = 0f;
                src.spatialBlend = 0f;     // pure 2D, unaffected by listener position
                src.ignoreListenerPause = true;
            }

            _title = Resources.Load<AudioClip>("bgm_title");
            _lobby = Resources.Load<AudioClip>("bgm_lobby");
            _round = Resources.Load<AudioClip>("bgm_round");
            _discussion = Resources.Load<AudioClip>("bgm_discussion");
            _results = Resources.Load<AudioClip>("bgm_results");
        }

        private void Start()
        {
            if (RealmNetworkManager.Instance != null)
                RealmNetworkManager.Instance.OnStateUpdated += OnStateUpdated;
        }

        private void OnDestroy()
        {
            if (RealmNetworkManager.Instance != null)
                RealmNetworkManager.Instance.OnStateUpdated -= OnStateUpdated;
        }

        private void OnStateUpdated(GameState state)
        {
            _phase = state != null ? state.phase : null;
        }

        private void Update()
        {
            var wanted = WantedClip();
            if (wanted != _current) SwitchTo(wanted);

            if (_fade >= 1f)
            {
                EnsurePlaying();
                _front.volume = _current != null ? volume : 0f;
                return;
            }

            EnsurePlaying();
            _fade = Mathf.Clamp01(_fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            _front.volume = volume * _fade;
            _back.volume = volume * (1f - _fade);
            if (_fade >= 1f && _back.isPlaying) _back.Stop();
        }

        // The splash owns the title track; after that the server phase decides.
        // An unknown or missing phase falls back to the lobby track rather than
        // silence, which is what the waiting room needs anyway.
        private AudioClip WantedClip()
        {
            if (splash != null && splash.gameObject.activeInHierarchy) return _title;

            switch (_phase)
            {
                case "round": return _round;
                case "discussion":
                case "actions":
                case "accusation": return _discussion;
                case "results": return _results;
                default: return _lobby;
            }
        }

        private void SwitchTo(AudioClip clip)
        {
            _current = clip;

            var swap = _back;
            _back = _front;
            _front = swap;

            _front.clip = clip;
            _front.volume = 0f;
            if (clip != null)
            {
                // These are streaming clips, so the data may not be resident
                // yet. Play() on an unloaded clip silently does nothing, which
                // is why the track has to be started once it is ready instead.
                if (clip.loadState != AudioDataLoadState.Loaded) clip.LoadAudioData();
                _front.Play();
            }
            _fade = 0f;

            // Nothing was playing before, so there is nothing to fade out of.
            if (!_back.isPlaying) _fade = 1f;
        }

        // Retries the start until the streaming clip is actually running.
        private void EnsurePlaying()
        {
            if (_current == null || _front.isPlaying) return;
            if (_front.clip != _current) _front.clip = _current;
            if (_current.loadState == AudioDataLoadState.Unloaded) { _current.LoadAudioData(); return; }
            if (_current.loadState == AudioDataLoadState.Loaded) _front.Play();
        }

        public void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);
        }
    }
}
