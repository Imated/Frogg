using System.Net.Mime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RollCredits : MonoBehaviour
{
    public string sceneName;
    private void OnCollisionEnter2D(Collision2D other)
    {
        SceneManager.LoadScene(sceneName);
    }
}
