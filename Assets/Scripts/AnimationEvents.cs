using UnityEngine;

public class AnimationEvents : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject playerHolder;
    private PlayerController otherScript;
    void Start()
    {
        otherScript = playerHolder.GetComponent<PlayerController>(); // Access main player script
    }

    public void FootstepEvent() // When player steps
    { 
        otherScript.AnimationStepEvent();
    }

    public void SlashDoneEvent()  //When slash animation is complete, sheath sword again
    {
        otherScript.MountSwordToBack();
    }

    public void ItemCutscenePause() // Pause player for get item cutscene
    {
        otherScript.GotItemPauseGame();
    }

    public void SetState() // Set state to normal via script if needed
    {
        otherScript.SetState("Normal");
    }

    public void PlayerDie()
    {
        otherScript.DeathEffect();
    }
}
