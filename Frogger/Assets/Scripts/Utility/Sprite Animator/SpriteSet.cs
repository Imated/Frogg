using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpriteSet
{
    public string animationId;
    public string nextAnimation;
    public float animationFrameRate;
    public bool loop;
    [Space(4.5f)] public List<Sprite> animationFrames;
}
