using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public static class DoTweenExtensions
{
    public static Tweener DoSetPosition(this LineRenderer target, int index, Vector3 endValue, float duration)
    {
        return DOTween.To((DOGetter<Vector3>) (() => target.GetPosition(index)), (DOSetter<Vector3>) (x =>
        {
            target.SetPosition(index, x);
        }), endValue, duration).SetTarget(target);
    }
}