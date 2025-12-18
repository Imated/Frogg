using System;
using System.Collections;
using UnityEngine;

public class C4 : MonoBehaviour
{
    [SerializeField] private Vector2 force;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var rb = other.transform.parent.GetComponent<Rigidbody2D>();
            rb.AddForce(force * 10000);
            StartCoroutine(nameof(Respawn));
            gameObject.SetActive(false);
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3f);
        gameObject.SetActive(true);
    }
}
