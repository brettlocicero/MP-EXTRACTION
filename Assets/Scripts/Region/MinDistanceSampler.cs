using System.Collections.Generic;
using UnityEngine;

public class MinDistanceSampler
{
    struct PlacedPoint
    {
        public Vector2 position;
        public float radius;
    }

    readonly float minBuffer;
    readonly List<PlacedPoint> placedPoints = new();

    public MinDistanceSampler(float minBuffer)
    {
        this.minBuffer = minBuffer;
    }

    public void AddAcceptedPoint(Vector2 position, float radius)
    {
        placedPoints.Add(new PlacedPoint { position = position, radius = radius });
    }

    public bool IsSpaceAvailable(Vector2 candidate, float radius)
    {
        foreach (PlacedPoint placed in placedPoints)
        {
            float requiredSpacing = placed.radius + radius + minBuffer;

            if (Vector2.Distance(candidate, placed.position) < requiredSpacing)
                return false;
        }

        return true;
    }
}