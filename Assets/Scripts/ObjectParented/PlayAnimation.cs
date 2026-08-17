using UnityEngine;

public class PlayAnimation : MonoBehaviour // Can be used in unique case where we need to play an animation on a different object from one the script is attached to. e.g., hit button and gate lifts.
{
    public Animation anim;

    public void AnimPlay()
    {
        anim.Play();
    }
}
