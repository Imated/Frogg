using System;
using System.Collections;
using UnityEngine;

public class C4 : MonoBehaviour
{
    [SerializeField] private Vector2 force;

    private Collider2D col;
    private SpriteRenderer sr;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var rb = other.transform.parent.GetComponent<Rigidbody2D>();
            rb.AddForce(force * 10000);

            StartCoroutine(nameof(Respawn));

            col.enabled = false;
            sr.enabled = false;
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3f);

        col.enabled = true;
        sr.enabled = true;
    }
}
