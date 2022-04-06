using System.Collections.Generic;
using Godot;

public class RadialRaycast2D : Node2D
{
    private const float ROTATION_SPEED = Mathf.Tau * 1f;

    const float ANGLE_OFFSET = .001f;
    const float MIN_ANGLE_DIFFERENCE = ANGLE_OFFSET * .5f;
    const float MIN_SUBDIVISION_ANGLE_DIFFERENCE = .01f;
    const float MIN_SUBDIVISION_POINT_DISTANCE = .01f;
    const float MIN_SUBDIVISION_POINT_DISTANCE_SQUARED = MIN_SUBDIVISION_POINT_DISTANCE * MIN_SUBDIVISION_POINT_DISTANCE;
    const float CIRCLE_RESOLUTION = .05f;

    private Dictionary<Node2D, Vector2[]> pointsPerNode;
    private Dictionary<Node2D, (Vector2, float)[]> circlesPerNode;

    private float rayLength;
    public float RayLength { get => rayLength; }
    private Vector2 rayVector;
    private RayCast2D ray;

    private const int MINIMUM_RAY_COUNT = 4;
    private float[] mandatoryRayAngles = new float[MINIMUM_RAY_COUNT];

    public override void _Ready()
    {
        float angleStep = Mathf.Tau / MINIMUM_RAY_COUNT;
        float currentAngle = angleStep * .5f;
        for (int rayIndex = 0; rayIndex < MINIMUM_RAY_COUNT; rayIndex++, currentAngle = (currentAngle + angleStep) % Mathf.Tau)
        {
            mandatoryRayAngles[rayIndex] = currentAngle;
        }

        ray = GetNode<RayCast2D>("RayCast2D");
        rayLength = ray.CastTo.Length();
        rayVector = new Vector2(-rayLength, 0f);
        updateRayTargets(GetTree(), ray.CollisionMask);
    }

    private void updateRayTargets(SceneTree tree, uint collisionMask)
    {
        pointsPerNode = new Dictionary<Node2D, Vector2[]>();
        circlesPerNode = new Dictionary<Node2D, (Vector2, float)[]>();
        var opaqueBodies = tree.GetNodesInGroup("opaque");
        for (int bodyIndex = 0; bodyIndex < opaqueBodies.Count; bodyIndex++)
        {
            var body = (PhysicsBody2D)opaqueBodies[bodyIndex];
            if ((body.CollisionLayer | collisionMask) == 0)
            {
                continue;
            }
            var children = ((Node)opaqueBodies[bodyIndex]).GetChildren();
            for (int j = 0; j < children.Count; j++)
            {
                if (!(children[j] is Node2D))
                {
                    continue;
                }
                var childNode = (Node2D)children[j];
                if (childNode is CollisionShape2D)
                {
                    var shape = ((CollisionShape2D)childNode).Shape;
                    if (shape is RectangleShape2D)
                    {
                        var rect = (RectangleShape2D)shape;
                        pointsPerNode.Add(childNode, new Vector2[]{
                            new Vector2(-rect.Extents.x, -rect.Extents.y),
                            new Vector2(rect.Extents.x, -rect.Extents.y),
                            new Vector2(-rect.Extents.x, rect.Extents.y),
                            new Vector2(rect.Extents.x, rect.Extents.y)
                        });
                    }
                    else if (shape is SegmentShape2D)
                    {
                        var segment = (SegmentShape2D)shape;
                        pointsPerNode.Add(childNode, new Vector2[] { segment.A, segment.B });
                    }
                    else if (shape is CapsuleShape2D)
                    {
                        var capsule = (CapsuleShape2D)shape;
                        var halfHeight = capsule.Height * .5f;
                        if (halfHeight > 0f)
                        {
                            pointsPerNode.Add(childNode, new Vector2[]{
                                new Vector2(-capsule.Radius, -halfHeight),
                                new Vector2(capsule.Radius, -halfHeight),
                                new Vector2(-capsule.Radius, halfHeight),
                                new Vector2(capsule.Radius, halfHeight)
                            });
                        }
                        circlesPerNode.Add(childNode, new (Vector2, float)[]{
                            (new Vector2(0, -halfHeight), capsule.Radius),
                            (new Vector2(0, halfHeight), capsule.Radius)
                        });
                    }
                    else if (shape is CircleShape2D)
                    {
                        circlesPerNode.Add(childNode, new (Vector2, float)[] { (Vector2.Zero, ((CircleShape2D)shape).Radius) });
                    }
                    else
                    {
                        GD.PushWarning("Cannot handle the shape of node " + childNode.GetPath());
                    }
                }
                else if (childNode is CollisionPolygon2D)
                {
                    pointsPerNode.Add(childNode, ((CollisionPolygon2D)childNode).Polygon);
                }
            }
        }
    }

    public Vector2[] GetRaycastPolygon()
    {
        // Make a list of the necessary raycasting angles
        Vector2 globalPosition = GlobalPosition;
        var angles = new List<float>();
        foreach (var entry in pointsPerNode)
        {
            foreach (var point in entry.Value)
            {
                Vector2 targetGlobalPos = entry.Key.ToGlobal(point);
                float angle = globalPosition.AngleToPoint(targetGlobalPos);
                ray.CastTo = new Vector2(new Vector2(-globalPosition.DistanceTo(targetGlobalPos) + .1f, 0).Rotated(angle));
                ray.ForceRaycastUpdate();
                if (!ray.IsColliding())
                {
                    angles.Add(angle);
                }
            }
        }
        foreach (var entry in circlesPerNode)
        {
            foreach (var circle in entry.Value)
            {
                Vector2 targetGlobalPos = entry.Key.ToGlobal(circle.Item1);
                var squaredDistanceToTarget = globalPosition.DistanceSquaredTo(targetGlobalPos);
                float radius = circle.Item2;
                var squaredRadius = radius * radius;
                if (squaredDistanceToTarget < squaredRadius)
                {
                    Visible = false;
                    return null;
                }
                var angleToCenter = globalPosition.AngleToPoint(targetGlobalPos);
                var angleVariation = Mathf.Atan2(radius, Mathf.Sqrt(squaredDistanceToTarget - squaredRadius));
                float minAngle = angleToCenter - angleVariation;
                float maxAngle = angleToCenter + angleVariation;
                for (float angle = minAngle; angle < maxAngle; angle += CIRCLE_RESOLUTION)
                {
                    angles.Add(angle);
                }
                // angles.Add(minAngle);
                angles.Add(maxAngle);
            }
        }
        Visible = true;
        int initialAngleCount = angles.Count;
        for (int angleIndex = 0; angleIndex < initialAngleCount; angleIndex++)
        {
            angles.Add((angles[angleIndex] + ANGLE_OFFSET + Mathf.Tau) % Mathf.Tau);
            angles[angleIndex] = (angles[angleIndex] - ANGLE_OFFSET + Mathf.Tau * 2f) % Mathf.Tau;
        }
        angles.AddRange(mandatoryRayAngles);
        angles.Sort();
        var finalAngles = new List<float>(angles.Count);
        float lastAngle = angles[0];
        finalAngles.Add(angles[0]);
        for (int angleIndex = 1; angleIndex < angles.Count; angleIndex++)
        {
            float angle = angles[angleIndex];
            if (Mathf.Min(angle - lastAngle, angles[0] + Mathf.Tau - angle) >= MIN_ANGLE_DIFFERENCE)
            {
                lastAngle = angle;
                finalAngles.Add(angle);
            }
        }
        angles = finalAngles;

        // Do raycasting
        var anglesWithResults = new List<(float, Vector2, Object, int)>(angles.Count);
        foreach (float angle in angles)
        {
            anglesWithResults.Add(castRay(angle));
        }

        // If collision shapes intersect, the polygon will be incorrect, so subdivide some angles if necessary
        var subdividedResult = new List<Vector2>(anglesWithResults.Count);
        int count = anglesWithResults.Count;
        for (int angleIndex = 1; angleIndex < count; angleIndex++)
        {
            subdividedResult.Add(anglesWithResults[angleIndex - 1].Item2);
            subdivideAngleIfNecessary(anglesWithResults[angleIndex - 1], anglesWithResults[angleIndex], subdividedResult);
        }
        subdividedResult.Add(anglesWithResults[count - 1].Item2);
        subdivideAngleIfNecessary(anglesWithResults[count - 1], anglesWithResults[0], subdividedResult);

        // Finally, remove any coplanar points because they are useless
        var withoutCoplanar = new List<Vector2>(subdividedResult.Count);
        withoutCoplanar.Add(subdividedResult[0]);
        withoutCoplanar.Add(subdividedResult[1]);
        for (int angleIndex = 2; angleIndex < subdividedResult.Count; angleIndex++)
        {
            if (pointsAreCoplanar(withoutCoplanar[withoutCoplanar.Count - 2], withoutCoplanar[withoutCoplanar.Count - 1], subdividedResult[angleIndex]))
            {
                withoutCoplanar[withoutCoplanar.Count - 1] = subdividedResult[angleIndex];
            }
            else
            {
                withoutCoplanar.Add(subdividedResult[angleIndex]);
            }
        }

        return withoutCoplanar.ToArray();
    }

    private void subdivideAngleIfNecessary((float, Vector2, Object, int) first, (float, Vector2, Object, int) second, List<Vector2> resultList)
    {
        if ((first.Item3 == second.Item3 && first.Item4 == second.Item4) || first.Item2.DistanceSquaredTo(second.Item2) < MIN_SUBDIVISION_POINT_DISTANCE_SQUARED)
        {
            return;
        }
        var middleAngle = getAngleAverage(first.Item1, second.Item1);
        if (getAngleDifference(middleAngle, first.Item1) < MIN_SUBDIVISION_ANGLE_DIFFERENCE)
        {
            return;
        }
        var middle = castRay(middleAngle);
        if (pointsAreCoplanar(first.Item2, middle.Item2, second.Item2))
        {
            return;
        }
        subdivideAngleIfNecessary(first, middle, resultList);
        resultList.Add(middle.Item2);
        subdivideAngleIfNecessary(middle, second, resultList);
    }

    private static float getAngleAverage(float first, float second)
    {
        return Mathf.Abs(first - second) < Mathf.Pi
            ? ((first + second) * .5f) % Mathf.Tau
            : ((first + second + Mathf.Tau) * .5f) % Mathf.Tau;
    }

    private static float getAngleDifference(float first, float second)
    {
        float difference = Mathf.Abs(first - second);
        return difference < Mathf.Pi
            ? difference
            : Mathf.Tau - difference;
    }

    private (float, Vector2, Object, int) castRay(float angle)
    {
        ray.CastTo = new Vector2(rayVector.Rotated(angle));
        ray.ForceRaycastUpdate();
        return (angle, ray.IsColliding() ? ToLocal(ray.GetCollisionPoint()) : ray.CastTo, ray.GetCollider(), ray.GetColliderShape());
    }

    private static bool pointsAreCoplanar(Vector2 first, Vector2 second, Vector2 third)
    {
        // If the slopes between two pairs of points are the same, then the 3 points are coplanar
        // This formula is adjusted so that there is multiplication instead of division
        return Mathf.Abs((third.y - second.y) * (second.x - first.x) - (second.y - first.y) * (third.x - second.x)) < 0.0001f;
    }
}
