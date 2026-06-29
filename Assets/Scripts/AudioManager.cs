using UnityEngine;

/// <summary>
/// Central audio hub. Create ONE empty GameObject named "AudioManager", add this
/// component, and drag your clips into the fields. All AudioSources are created at
/// runtime (you don't add any). Other scripts call AudioManager.Instance.X().
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips (drag from Assets/audios)")]
    public AudioClip backgroundMusic;   // looping, scene 1
    public AudioClip drinkingSound;     // when drinking
    public AudioClip gameOver;          // caught by NPC / lost pee game
    public AudioClip dormDoorOpening;   // at the dorm door
    public AudioClip peeSound;          // looping, while peeing
    public AudioClip doNote;            // target1 hit
    public AudioClip reNote;            // target2 hit
    public AudioClip miNote;            // target3 hit
    public AudioClip youWin;            // pee game in range

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource _music, _pee, _sfx;
    private readonly AudioSource[] _notes = new AudioSource[3];
    private readonly float[] _noteHitTime = { -10f, -10f, -10f };
    private const float NoteHold = 0.15f; // keep a note playing this long after the last hit

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _music = NewSource(true, musicVolume);
        _pee = NewSource(true, sfxVolume);
        _sfx = NewSource(false, sfxVolume);
        for (int i = 0; i < 3; i++) _notes[i] = NewSource(true, sfxVolume);
        _notes[0].clip = doNote;
        _notes[1].clip = reNote;
        _notes[2].clip = miNote;
    }

    private AudioSource NewSource(bool loop, float vol)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = loop;
        s.playOnAwake = false;
        s.volume = vol;
        return s;
    }

    private void Start() => PlayMusic();

    // ---- music ----
    public void PlayMusic()
    {
        if (backgroundMusic == null) return;
        _music.clip = backgroundMusic;
        _music.volume = musicVolume;
        _music.Play();
    }
    public void StopMusic() => _music.Stop();

    // ---- one-shots ----
    public void PlayDrinking() => OneShot(drinkingSound);
    public void PlayGameOver() => OneShot(gameOver);
    public void PlayDormDoor() => OneShot(dormDoorOpening);
    public void PlayWin() => OneShot(youWin);
    private void OneShot(AudioClip c) { if (c != null) _sfx.PlayOneShot(c, sfxVolume); }

    // ---- looping pee ----
    public void StartPee()
    {
        if (peeSound == null) return;
        _pee.clip = peeSound;
        if (!_pee.isPlaying) _pee.Play();
    }
    public void StopPee() => _pee.Stop();

    // ---- do/re/mi: keep playing while droplets keep hitting that target ----
    public void NoteHit(int index)
    {
        if (index >= 0 && index < 3) _noteHitTime[index] = Time.time;
    }

    private void Update()
    {
        for (int i = 0; i < 3; i++)
        {
            bool active = _notes[i].clip != null && Time.time - _noteHitTime[i] <= NoteHold;
            if (active) { if (!_notes[i].isPlaying) _notes[i].Play(); }
            else { if (_notes[i].isPlaying) _notes[i].Stop(); }
        }
    }
}
