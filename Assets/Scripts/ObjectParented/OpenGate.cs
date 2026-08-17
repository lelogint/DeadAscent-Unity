using UnityEngine;

public class OpenGate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private Animation gateOpen;
    
    public void CheckChildren()
    {
        if(transform.childCount <= 1)
        {
            gateOpen.Play();
        }
    }
}
