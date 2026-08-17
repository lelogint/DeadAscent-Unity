using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics; 
using UnityEngine;

public class AnimationHandler : MonoBehaviour
{

    public Animator animator;
    public Dictionary<string, string> currentAnimation = new Dictionary<string, string>() // Define each animation layer in here
    {
        {"Base", ""},
        {"Attack", "Empty"},
    };

    void Start()
    {

    }

    void Update()
    {

    }

    private float RunGetTimePos(string layer = "Base") // Assume if no layer is entered, default to "Base"
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(LayerIndexFromString(layer));
        float normalizedTime = stateInfo.normalizedTime;
        return normalizedTime % 1f;
    }

    public void IsPlaying(string layer, string animName)
    {
        if (currentAnimation[layer] == animName)
        {
            currentAnimation[layer] = null;
        }
    }

    public void EndAfterComplete(string layer, string animName, float transitionTime = 0.1f)
    {
        if (currentAnimation[layer] == animName)
        {
            currentAnimation[layer] = null;
            int layerIndex = LayerIndexFromString(layer);
            animator.CrossFade("Empty", transitionTime, layerIndex, 0f);
        }
    }


    private int LayerIndexFromString(string layer)
    { // Define our layer indexes from strings in here - I would assume it's more performant than retrieving from animator.
        switch (layer)
        {
            case "Base":
                return 0;
            case "Attack":
                return 1;
            default:
                return 0;
        }
    }

    public void SetAnimation(string animation, string layer = "Base", float crossfade = 0.15f, float initialSpeed = 1f, float normalizedTime = 0f)
    {
        if (currentAnimation[layer] != animation)
        {
            int layerIndex = LayerIndexFromString(layer);
            currentAnimation[layer] = animation;
            animator.CrossFade(animation, crossfade, layerIndex, normalizedTime);
            animator.SetFloat("AnimSpeed", initialSpeed);
        }
    }

    public void RunAnims(float speed, bool bashing = false)
    {
        float runAnimSpeed = (0.4f + (speed / 16f)) * (speed / math.abs(speed));
        float absSpeed = math.abs(speed);
        if (currentAnimation["Base"] != "AirDash" && currentAnimation["Base"] != "Bash")
        {
            if (bashing == true)
            {
                float timePosition = RunGetTimePos();
                SetAnimation("Run", "Base", 0.15f, runAnimSpeed, timePosition);
                animator.SetFloat("AnimSpeed", runAnimSpeed);
                return;
            }
            if (absSpeed < 1)
            {
                SetAnimation("Idle");
            }
            else if (absSpeed < 15f)
            {
                float timePosition = RunGetTimePos();
                SetAnimation("Walk", "Base", 0.15f, runAnimSpeed, timePosition);
                animator.SetFloat("AnimSpeed", runAnimSpeed);
            }
            else
            {
                float timePosition = RunGetTimePos();
                SetAnimation("Run", "Base", 0.15f, runAnimSpeed, timePosition);
                animator.SetFloat("AnimSpeed", runAnimSpeed);
            }
        }
    }
}

