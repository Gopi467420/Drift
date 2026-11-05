using UnityEngine;

public static class Utility 
{

    public static Vector3 FindRandominSphere(Vector3 refPosition,float Radius)
    {
        Vector3 position = refPosition + (Random.insideUnitSphere * Radius) ;
        return position;
    }

    public static Vector3 GetPointOnCircle(
     Vector3 refPosition, float radius, float angle)
    {
        float angleRad = angle * Mathf.Deg2Rad;

        float xCor = refPosition.x + radius * Mathf.Cos(angleRad);
        float yCor = refPosition.y;
        float zCor = refPosition.z + radius * Mathf.Sin(angleRad);

        return new Vector3(xCor, yCor, zCor);
    }

    public static bool IsInRange( Vector3 from, Vector3 to, float rangeValue)
    {
        return Vector3.Distance(from, to) < rangeValue;
    }

} 
