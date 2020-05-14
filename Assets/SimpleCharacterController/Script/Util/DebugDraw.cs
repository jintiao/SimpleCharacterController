﻿using UnityEngine;

public class DebugDraw
{
    public static void Circle(Vector3 center, Vector3 normal, float radius, Color color)
    {
        Vector3 v1;
        Vector3 v2;
        CalculatePlaneVectorsFromNormal(normal, out v1, out v2);
        CircleInternal(center, v1, v2, radius, color);
    }

    public static void Sphere(Vector3 center, float radius)
    {
        Sphere(center, radius, Color.white);
    }

    public static void Sphere(Vector3 center, float radius, Color color, float duration = 0)
    {
        CircleInternal(center, Vector3.right, Vector3.up, radius, color, duration);
        CircleInternal(center, Vector3.forward, Vector3.up, radius, color, duration);
        CircleInternal(center, Vector3.right, Vector3.forward, radius, color, duration);
    }
             
    public static void Capsule(Vector3 center, Vector3 dir, float radius, float height, Color color, float duration = 0)
    {
        var cylinderHeight = height - radius*2;
        var v = Vector3.Angle(dir,Vector3.up) > 0.001 ? Vector3.Cross(dir, Vector3.up) : Vector3.Cross(dir, Vector3.left);
        
        CircleInternal(center + dir*cylinderHeight*0.5f, dir, v, radius, color, duration);
        CircleInternal(center - dir*cylinderHeight*0.5f, dir, v, radius, color, duration);
        
        Cylinder(center, dir, radius, cylinderHeight * 0.5f, color, duration);
    }
    
    public static void Cylinder(Vector3 center, Vector3 normal, float radius, float halfHeight, Color color, float duration = 0)
    {
        Vector3 v1;
        Vector3 v2;
        CalculatePlaneVectorsFromNormal(normal, out v1, out v2);

        var offset = normal * halfHeight;
        CircleInternal(center - offset, v1, v2, radius, color, duration);
        CircleInternal(center + offset, v1, v2, radius, color, duration);

        const int segments = 20;
        float arc = Mathf.PI * 2.0f / segments;
        for (var i = 0; i < segments; i++)
        {
            Vector3 p = center + v1 * Mathf.Cos(arc * i) * radius + v2 * Mathf.Sin(arc * i) * radius;
            Debug.DrawLine(p - offset, p + offset, color, duration);
        }
    }
    
    static void CircleInternal(Vector3 center, Vector3 v1, Vector3 v2, float radius, Color color, float duration = 0)
    {
        const int segments = 20;
        float arc = Mathf.PI * 2.0f / segments;
        Vector3 p1 = center + v1 * radius;
        for (var i = 1; i <= segments; i++)
        {
            Vector3 p2 = center + v1 * Mathf.Cos(arc * i) * radius + v2 * Mathf.Sin(arc * i) * radius;
            Debug.DrawLine(p1, p2, color, duration);
            p1 = p2;
        }
    }

    static void CalculatePlaneVectorsFromNormal(Vector3 normal, out Vector3 v1, out Vector3 v2)
    {
        if (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) < 0.99)
        {
            v1 = Vector3.Cross(Vector3.up, normal).normalized;
            v2 = Vector3.Cross(normal, v1);
        }
        else
        {
            v1 = Vector3.Cross(Vector3.left, normal).normalized;
            v2 = Vector3.Cross(normal, v1);
        }
    }

    public static void Arrow(Vector3 pos, float angle, Color color, float length = 1f, float tipSize = 0.25f, float width = 0.5f)
    {
        var angleRot = Quaternion.AngleAxis(angle, Vector3.up);
        var dir = angleRot * Vector3.forward;
        Arrow(pos, dir, color, length, tipSize, width);           
    }
        
    public static void Arrow(Vector3 pos, Vector2 direction, Color color, float length = 1f, float tipSize = 0.25f, float width = 0.5f)
    {
        var dir = new Vector3(direction.x, 0f, direction.y);
        Arrow(pos, dir, color, length, tipSize, width);
    }
        
    public static void Arrow(Vector3 pos, Vector3 direction, Color color, float length=1f, float tipSize=0.25f, float width=0.5f)
    {
        direction.Normalize();
            
        var sideLen = length - length * tipSize;
        var widthOffset = Vector3.Cross(direction, Vector3.up) * width;

        var baseLeft = pos + widthOffset * 0.3f;
        var baseRight = pos - widthOffset * 0.3f;
        var tip = pos + direction * length;
        var upCornerInRight = pos - widthOffset * 0.3f + direction * sideLen;
        var upCornerInLeft = pos + widthOffset * 0.3f + direction * sideLen;
        var upCornerOutRight = pos - widthOffset * 0.5f + direction * sideLen;
        var upCornerOutLeft = pos + widthOffset * 0.5f + direction * sideLen;
        
        Debug.DrawLine(baseLeft, baseRight, color);
        Debug.DrawLine(baseRight, upCornerInRight, color);
        Debug.DrawLine(upCornerInRight, upCornerOutRight, color);
        Debug.DrawLine(upCornerOutRight, tip, color);
        Debug.DrawLine(tip, upCornerOutLeft, color);
        Debug.DrawLine(upCornerOutLeft, upCornerInLeft, color);
        Debug.DrawLine(upCornerInLeft, baseLeft, color);
    }
}
