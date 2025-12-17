using System;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [SerializeField] private GameObject cam;
    [SerializeField] private float amount;
    private float _length, _startPos;

    private void Awake()
    {
        _startPos = transform.position.x;
        _length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void Update()
    {
        var temp = cam.transform.position.x * (1 - amount);
        var dist = cam.transform.position.x * amount;
        transform.position = new Vector3(_startPos + dist, transform.position.y, transform.position.z);
        if (temp > _startPos + _length)
            _startPos += _length;
        else if (temp < _startPos - _length) _startPos -= _length;
    }
}
