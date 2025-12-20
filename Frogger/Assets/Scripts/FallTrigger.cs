using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class FallNarrator2D : MonoBehaviour
{
    public enum FallTier { Small, Medium, Big, Huge, Catastrophic }

    public bool debug = true;

    public Player player;
    public AudioSource audioSource;

    public float minFallToTrigger = 0.75f;
    public float globalCooldownSeconds = 0.25f;

    public float mediumMin = 3f;
    public float bigMin = 8f;
    public float hugeMin = 15f;
    public float catastrophicMin = 28f;

    public int recentBlockCount = 8;

    public float smallDelay = 0.0f;
    public float mediumDelay = 0.5f;
    public float bigDelay = 1.0f;
    public float hugeDelay = 2.0f;
    public float catastrophicDelay = 4.0f;

    public AudioClip[] small = new AudioClip[10];
    public AudioClip[] medium = new AudioClip[10];
    public AudioClip[] big = new AudioClip[10];
    public AudioClip[] huge = new AudioClip[10];
    public AudioClip[] catastrophic = new AudioClip[10];

    private bool _wasSupported;
    private bool _unsupported;
    private float _peakY;
    private float _lastLineTime = -999f;

    private readonly Dictionary<FallTier, Queue<int>> _recentByTier = new();

    private FieldInfo _isGroundedField;
    private FieldInfo _isStickingField;

    private void Awake()
    {
        foreach (FallTier tier in Enum.GetValues(typeof(FallTier)))
            _recentByTier[tier] = new Queue<int>(recentBlockCount);

        _isGroundedField = typeof(Player).GetField("_isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);
        _isStickingField = typeof(Player).GetField("_isSticking", BindingFlags.Instance | BindingFlags.NonPublic);

        if (debug) Debug.Log("[FallNarrator2D] Awake");
    }

    private void OnEnable()
    {
        if (debug) Debug.Log("[FallNarrator2D] Enabled");
    }

    private void Start()
    {
        if (debug)
            Debug.Log($"[FallNarrator2D] Start player={(player ? player.name : "NULL")} audioSource={(audioSource ? audioSource.name : "NULL")}");
    }

    private void Update()
    {
        if (player == null) return;

        bool supported = IsSupported();

        if (_wasSupported && !supported)
        {
            _unsupported = true;
            _peakY = player.transform.position.y;
        }

        if (_unsupported && !supported)
        {
            float y = player.transform.position.y;
            if (y > _peakY) _peakY = y;
        }

        if (!_wasSupported && supported && _unsupported)
        {
            _unsupported = false;

            float fallHeight = Mathf.Max(0f, _peakY - player.transform.position.y);

            if (fallHeight >= minFallToTrigger && Time.time - _lastLineTime >= globalCooldownSeconds)
            {
                FallTier tier = GetTier(fallHeight);
                int index = PickLineIndex(tier);
                AudioClip clip = GetClip(tier, index);
                float delay = GetDelay(tier);

                RememberPick(tier, index);
                _lastLineTime = Time.time;

                if (debug)
                    Debug.Log($"[FALL] Tier={tier} Height={fallHeight:F2} Index={index} Clip={(clip ? clip.name : "NULL")} Delay={delay:F2}");

                if (clip != null && audioSource != null)
                    StartCoroutine(PlayAfterDelay(clip, delay));
            }
        }

        _wasSupported = supported;
    }

    private bool IsSupported()
    {
        bool grounded = false;
        bool sticking = false;

        if (_isGroundedField != null)
            grounded = (bool)_isGroundedField.GetValue(player);

        if (_isStickingField != null)
            sticking = (bool)_isStickingField.GetValue(player);

        return grounded || sticking;
    }

    private IEnumerator PlayAfterDelay(AudioClip clip, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private FallTier GetTier(float fallHeight)
    {
        if (fallHeight < mediumMin) return FallTier.Small;
        if (fallHeight < bigMin) return FallTier.Medium;
        if (fallHeight < hugeMin) return FallTier.Big;
        if (fallHeight < catastrophicMin) return FallTier.Huge;
        return FallTier.Catastrophic;
    }

    private float GetDelay(FallTier tier)
    {
        return tier switch
        {
            FallTier.Small => smallDelay,
            FallTier.Medium => mediumDelay,
            FallTier.Big => bigDelay,
            FallTier.Huge => hugeDelay,
            FallTier.Catastrophic => catastrophicDelay,
            _ => 0f
        };
    }

    private AudioClip[] GetArray(FallTier tier)
    {
        return tier switch
        {
            FallTier.Small => small,
            FallTier.Medium => medium,
            FallTier.Big => big,
            FallTier.Huge => huge,
            FallTier.Catastrophic => catastrophic,
            _ => small
        };
    }

    private AudioClip GetClip(FallTier tier, int index)
    {
        var arr = GetArray(tier);
        if (arr == null || arr.Length == 0) return null;
        index = Mathf.Clamp(index, 0, arr.Length - 1);
        return arr[index];
    }

    private int PickLineIndex(FallTier tier)
    {
        var arr = GetArray(tier);
        int count = arr?.Length ?? 0;
        if (count <= 1) return 0;

        HashSet<int> blocked = new HashSet<int>();
        foreach (int idx in _recentByTier[tier])
            blocked.Add(idx);

        List<int> candidates = new List<int>(count);
        for (int i = 0; i < count; i++)
            if (!blocked.Contains(i))
                candidates.Add(i);

        if (candidates.Count == 0)
        {
            int last = GetMostRecent(tier);
            for (int i = 0; i < count; i++)
                if (i != last)
                    candidates.Add(i);
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private int GetMostRecent(FallTier tier)
    {
        var q = _recentByTier[tier];
        if (q.Count == 0) return -1;
        int last = -1;
        foreach (var v in q) last = v;
        return last;
    }

    private void RememberPick(FallTier tier, int index)
    {
        Queue<int> q = _recentByTier[tier];
        q.Enqueue(index);

        int limit = Mathf.Clamp(recentBlockCount, 0, 10);
        while (q.Count > limit)
            q.Dequeue();
    }
}
