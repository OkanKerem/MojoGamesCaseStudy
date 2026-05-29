using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _unitClickClip;
    [SerializeField] private AudioClip _matchClip;
    [SerializeField] private AudioClip _boxBreakClip;
    [SerializeField] [Range(0f, 1f)] private float _volume = 1f;

    private void Awake()
    {
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        GameplayEvents.UnitSelected += PlayUnitClick;
        GameplayEvents.UnitsMatched += PlayMatch;
        GameplayEvents.BoxesBroken += PlayBoxBreak;
    }

    private void OnDisable()
    {
        GameplayEvents.UnitSelected -= PlayUnitClick;
        GameplayEvents.UnitsMatched -= PlayMatch;
        GameplayEvents.BoxesBroken -= PlayBoxBreak;
    }

    public void PlayUnitClick()
    {
        PlayClip(_unitClickClip);
    }

    private void PlayMatch(Vector3 _)
    {
        PlayMatch();
    }

    public void PlayMatch()
    {
        PlayClip(_matchClip);
    }

    public void PlayBoxBreak()
    {
        PlayClip(_boxBreakClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || _audioSource == null)
        {
            return;
        }

        _audioSource.PlayOneShot(clip, _volume);
    }
}
