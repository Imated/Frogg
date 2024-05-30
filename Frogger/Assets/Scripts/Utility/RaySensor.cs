using UnityEngine;

public class RaySensor : MonoBehaviour
{
    public RaycastHit2D CastAllHit(float rayLength, float rayYOffset, float rayXOffset, LayerMask hitMask, Vector3 dir)
    {
        var position = new Vector3(transform.position.x + rayXOffset, transform.position.y, transform.position.z);
        var origin = position + dir * rayYOffset;
        var hit = Physics2D.Raycast(origin, dir * rayLength, rayLength, hitMask);
        return hit;
    }
    
    public bool CastAll(float rayLength, float rayYOffset, float rayXOffset, float raySideOffset, LayerMask hitMask, Vector3 dir)
    {
        var position = new Vector3(transform.position.x + rayXOffset, transform.position.y, transform.position.z);
        Vector3 leftRayPos;
        Vector3 rightRayPos;

        if (dir == Vector3.down || dir == Vector3.up)
        { 
            leftRayPos = new Vector3(position.x - raySideOffset, position.y);
            rightRayPos = new Vector3(position.x + raySideOffset, position.y);
        }
        else
        {
            leftRayPos = new Vector3(position.x, position.y - raySideOffset);
            rightRayPos = new Vector3(position.x, position.y + raySideOffset);
        }

        var origin = position + dir * rayYOffset;
        var originLeft = leftRayPos + dir * rayYOffset;
        var originRight = rightRayPos + dir * rayYOffset;

        var hit = Physics2D.Raycast(origin, dir * rayLength, rayLength, hitMask);
        var hitLeft = Physics2D.Raycast(originLeft, dir * rayLength, rayLength, hitMask);
        var hitRight = Physics2D.Raycast(originRight, dir * rayLength, rayLength, hitMask);

        return hit && hitLeft && hitRight;
    }
    
    public bool Cast(float rayLength, float rayYOffset, float rayXOffset, float raySideOffset, LayerMask hitMask, Vector3 dir)
    {
        var position = new Vector3(transform.position.x + rayXOffset, transform.position.y, transform.position.z);
        Vector3 leftRayPos;
        Vector3 rightRayPos;

        if (dir == Vector3.down || dir == Vector3.up)
        { 
            leftRayPos = new Vector3(position.x - raySideOffset, position.y);
            rightRayPos = new Vector3(position.x + raySideOffset, position.y);
        }
        else
        {
            leftRayPos = new Vector3(position.x, position.y - raySideOffset);
            rightRayPos = new Vector3(position.x, position.y + raySideOffset);
        }

        var origin = position + dir * rayYOffset;
        var originLeft = leftRayPos + dir * rayYOffset;
        var originRight = rightRayPos + dir * rayYOffset;

        var hit = Physics2D.Raycast(origin, dir * rayLength, rayLength, hitMask);
        var hitLeft = Physics2D.Raycast(originLeft, dir * rayLength, rayLength, hitMask);
        var hitRight = Physics2D.Raycast(originRight, dir * rayLength, rayLength, hitMask);

        return hit || hitLeft || hitRight;
    }

    public void CastGizmos(Color color, float rayLength, float rayYOffset, float rayXOffset, float raySideOffset, Vector3 dir)
    {
        var position = new Vector3(transform.position.x + rayXOffset, transform.position.y, transform.position.z);
        Vector3 leftRayPos;
        Vector3 rightRayPos;

        if (dir == Vector3.down || dir == Vector3.up)
        { 
            leftRayPos = new Vector3(position.x - raySideOffset, position.y);
            rightRayPos = new Vector3(position.x + raySideOffset, position.y);
        }
        else
        {
            leftRayPos = new Vector3(position.x, position.y - raySideOffset);
            rightRayPos = new Vector3(position.x, position.y + raySideOffset);
        }

        var origin = position + dir * rayYOffset;
        var originLeft = leftRayPos + dir * rayYOffset;
        var originRight = rightRayPos + dir * rayYOffset;

        Gizmos.color = color;
        Gizmos.DrawLine(origin, origin + dir * rayLength);
        Gizmos.DrawLine(originLeft, originLeft + dir * rayLength);
        Gizmos.DrawLine(originRight, originRight + dir * rayLength);
    }
}
