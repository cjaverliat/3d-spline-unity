using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.Assertions;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CurveMeshProvider : MonoBehaviour
{
    public Vector3[] points;
    
    public Mesh lineMesh;
    public Mesh jointMesh;
    
    public void OnValidate()
    {
        Assert.IsNotNull(lineMesh);
        Assert.IsNotNull(jointMesh);
        Assert.IsTrue(points.Length > 0);

        var totalLengthSquared = 0.0f;
        
        for (var i = 0; i < points.Length - 1; i++)
        {
            totalLengthSquared += (points[i] - points[i + 1]).sqrMagnitude;
        }


        var nMeshes = points.Length + points.Length - 1;
        var combine = new CombineInstance[points.Length + points.Length - 1];

        var meshIdx = 0;
        var lengthSquaredOffset = 0.0f;
        
        for (var i = 0; i < points.Length - 1; i++)
        {
            var segmentLengthSquared = (points[i] - points[i + 1]).sqrMagnitude;
            var t0 = lengthSquaredOffset / totalLengthSquared;
            var t1 = (lengthSquaredOffset + segmentLengthSquared) / totalLengthSquared;
            combine[meshIdx++] = CreateJointMesh(points[i], 1f, Color.Lerp(Color.white, Color.black, t0));
            combine[meshIdx++] = CreateLineMesh(points[i], points[i + 1], 1f, Color.Lerp(Color.white, Color.black, t0), Color.Lerp(Color.white, Color.black, t1));
            lengthSquaredOffset += segmentLengthSquared;
        }
        
        combine[nMeshes - 1] = CreateJointMesh(points[points.Length - 1], 1f, Color.black);
        
        gameObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        gameObject.GetComponent<MeshFilter>().mesh.Optimize();
    }

    private CombineInstance CreateJointMesh(Vector3 position, float radius, Color color)
    {
        var transformMatrix = Matrix4x4.identity;
        transformMatrix *= Matrix4x4.Translate(position);
        transformMatrix *= Matrix4x4.Scale(new Vector3(radius, radius, radius));
        transformMatrix *= Matrix4x4.Scale(new Vector3(2, 2, 2));

        var newJointMesh = new Mesh();
        newJointMesh.SetVertices(jointMesh.vertices);
        newJointMesh.SetNormals(jointMesh.normals);
        newJointMesh.SetTriangles(jointMesh.triangles, 0);

        var colors = new Color[jointMesh.vertices.Length];

        for (var i = 0; i < jointMesh.vertices.Length; ++i)
        {
            colors[i] = color;
        }

        newJointMesh.SetColors(colors);
        
        return new CombineInstance
        {
            mesh = newJointMesh,
            transform = transformMatrix
        };
    }
    
    private CombineInstance CreateLineMesh(Vector3 from, Vector3 to, float radius, Color colorStart, Color colorEnd)
    {
        var vector = to - from;
        var rotation = Quaternion.FromToRotation(Vector3.up, vector.normalized);
        var height = vector.magnitude;

        var transformMatrix = Matrix4x4.identity;
        transformMatrix *= Matrix4x4.Translate(from);
        transformMatrix *= Matrix4x4.Rotate(rotation);
        transformMatrix *= Matrix4x4.Scale(new Vector3(radius, height, radius));
        transformMatrix *= Matrix4x4.Scale(new Vector3(2, 0.5f, 2));
        transformMatrix *= Matrix4x4.Translate(new Vector3(0, 1, 0));

        var newLineMesh = new Mesh();
        newLineMesh.SetVertices(lineMesh.vertices);
        newLineMesh.SetNormals(lineMesh.normals);
        newLineMesh.SetTriangles(lineMesh.triangles, 0);

        var colors = new Color[lineMesh.vertices.Length];

        for (var i = 0; i < lineMesh.vertices.Length; ++i)
        {
            colors[i] = Color.Lerp(colorStart, colorEnd, (lineMesh.vertices[i].y + 1) / 2);
        }

        newLineMesh.SetColors(colors);

        return new CombineInstance
        {
            mesh = newLineMesh,
            transform = transformMatrix
        };
    }

}