using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MainScreenHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip start;
    [SerializeField] private AudioClip reset;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetProgress()
    {
        audioSource.PlayOneShot(reset);
        UserData.DeleteSave();
    }

    public void NewGame()
    {
        audioSource.PlayOneShot(start);
        SceneManager.LoadScene("HubWorld");
    }
}
