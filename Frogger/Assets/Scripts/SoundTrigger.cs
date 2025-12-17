using UnityEngine;

public class SoundTrigger : MonoBehaviour
{
    public AudioClip triggerClip;
    bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        triggered = true;

        var handler = FindObjectOfType<SoundHandler>();

        if (handler != null)
            handler.PlayTriggerSFX(triggerClip);
        
    }
}
