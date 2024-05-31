using UnityEngine;

public static class Extensions
{
    public static string Color(this string str, string color) => $"<color={color}>{str}</color>";
    public static string Size(this string str, int sizeMultiplier) => $"<size={12 + sizeMultiplier}>{str}</size>";
    public static string Bold(this string str) => $"<bold>{str}</bold>";
    public static string Italic(this string str) => $"<i>{str}</i>";
    public static bool HasComponent<T>(this GameObject obj) where T : Component => obj.GetComponent<T>() != null;
    public static Vector3 IgnoreX(this Vector3 v) => new Vector3(0, v.y, v.z);
    public static Vector3 IgnoreY(this Vector3 v) => new Vector3(v.x, 0, v.z);
    public static Vector3 IgnoreZ(this Vector3 v) => new Vector3(v.x, v.y, 0);
    public static Vector2 IgnoreX(this Vector2 v) => new Vector2(0, v.y);
    public static Vector2 IgnoreY(this Vector2 v) => new Vector2(v.x, 0);
}
