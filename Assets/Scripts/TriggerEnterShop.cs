using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TriggerEnterShop : MonoBehaviour
{
    public string sceneToLoad; // The name of the scene you want to load

    private bool canLoadScene = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canLoadScene = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canLoadScene = false;
        }
    }

    private void Update()
    {
        if (canLoadScene && Input.GetKeyDown(KeyCode.E))
        {
            LoadScene();
        }
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
