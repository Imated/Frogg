using UnityEngine;

public class Scroll : MonoBehaviour
{
    [SerializeField] private float amount;
    private float _length, _startPos;

    private void Awake()
    {
        _startPos = transform.position.x;
        _length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void Update()
    {
        _startPos += amount * Time.deltaTime;

        transform.position = new Vector3(_startPos, transform.position.y, transform.position.z);

        if (_startPos > _length)
            _startPos -= _length;
    }
}
