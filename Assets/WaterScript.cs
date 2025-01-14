using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterScript : MonoBehaviour
{
    public WaterVertex[] waterVertices;


    [Header("Travel")]

    public float travelDistance;
    public float travelDistanceTotal;
    float travelDistanceUntilNext;
    float travelDistanceUntilNextLast;
    float travelSpeed;
    float travelSpeedTotal;
    public int travelId;

    public float waitForTravel;
    float waitForTravelTimer;

    public AnimationCurve travelSpeedCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));


    [Header("Interpolation")]

    Vector3 interpolationVertex1;
    Vector3 interpolationVertex2;


    [Header("Mesh")]

    public Vector3 rotateUV;

    private Mesh meshWater;
    private Vector3[] meshVertices;
    private Vector2[] meshUV;
    private int[] meshTriangles;
    MeshFilter meshFilter;


    [Header("Particles")]

    public ParticleSystem particlesFoam;
    ParticleSystem.MainModule particlesFoamMain;
    ParticleSystem.ShapeModule particlesFoamShape;


    void Awake()
    {
        if (particlesFoam != null)
        {
            particlesFoam = Instantiate(particlesFoam);
            particlesFoamMain = particlesFoam.main;
            particlesFoamShape = particlesFoam.shape;
        }

        if (waterVertices.Length < 2) return;

        for (int i = 1; i < waterVertices.Length; i++)
        {
            float dis = Vector3.Distance(waterVertices[i - 1].transform.localPosition, waterVertices[i].transform.localPosition);
            travelDistanceTotal += dis;
            waterVertices[i - 1].distance = dis;
        }

        travelDistanceUntilNext = waterVertices[travelId].distance;
        travelSpeed = waterVertices[travelId].speed;
    }

    void OnEnable()
    {
        ResetTravel();

        GetMesh(travelId + 2);
    }

    void Update()
    {
        waitForTravelTimer += Time.deltaTime;

        if (waitForTravelTimer < waitForTravel) return;

        travelSpeedTotal = travelSpeed * travelSpeedCurve.Evaluate(travelDistance / travelDistanceTotal);
        travelDistance += Time.deltaTime * travelSpeedTotal;

        if (travelDistance >= travelDistanceUntilNext)
        {
            TravelNext();
        }

        if (waterVertices.Length < 2) return;

        float interp = (travelDistance - travelDistanceUntilNextLast) / (travelDistanceUntilNext - travelDistanceUntilNextLast);
        InterpolateLastVertices(interp);
    }

    public void GetMesh()
    {
        meshFilter = GetComponent<MeshFilter>();

        meshWater = new Mesh
        {
            name = "Water"
        };

        GetVertices();
        GetTriangles();

        meshWater.vertices = meshVertices;
        meshWater.uv = meshUV;
        meshWater.triangles = meshTriangles;

        meshFilter.mesh = meshWater;
    }

    public void GetMesh(int length)
    {
        meshFilter = GetComponent<MeshFilter>();

        meshWater = new Mesh
        {
            name = "WaterNumbered"
        };

        GetVertices(length);
        GetTriangles();

        meshWater.vertices = meshVertices;
        meshWater.uv = meshUV;
        meshWater.triangles = meshTriangles;

        meshFilter.mesh = meshWater;
    }

    /// <summary>
    /// Gets Vertices needed for mesh.
    /// </summary>
    private void GetVertices()
    {
        meshVertices = new Vector3[waterVertices.Length * 2];
        meshUV = new Vector2[waterVertices.Length * 2];

        for (int i = 0; i < waterVertices.Length; i++)
        {
            WaterVertex vertex = waterVertices[i];

            meshVertices[i * 2] = vertex.transform.localPosition - CalculateWidth(vertex.transform, vertex.width);
            meshVertices[i * 2 + 1] = vertex.transform.localPosition + CalculateWidth(vertex.transform, vertex.width);

            meshUV[i * 2] = GetRotatedUV(meshVertices[i * 2]);
            meshUV[i * 2 + 1] = GetRotatedUV(meshVertices[i * 2 + 1]);
        }
    }
    /// <summary>
    /// Gets Vertices needed for mesh.
    /// 
    /// Alt. Version for custom lengths, like for water traveling.
    /// </summary>
    private void GetVertices(int length)
    {
        if (length > waterVertices.Length) return;

        meshVertices = new Vector3[length * 2];
        meshUV = new Vector2[length * 2];

        for (int i = 0; i < length; i++)
        {
            WaterVertex vertex = waterVertices[i];

            meshVertices[i * 2] = vertex.transform.localPosition - CalculateWidth(vertex.transform, vertex.width);
            meshVertices[i * 2 + 1] = vertex.transform.localPosition + CalculateWidth(vertex.transform, vertex.width);

            meshUV[i * 2] = GetRotatedUV(meshVertices[i * 2]);
            meshUV[i * 2 + 1] = GetRotatedUV(meshVertices[i * 2 + 1]);
        }
    }

    /// <summary>
    /// Always do GetVertices first.
    /// 
    /// Gets triangles needed for mesh.
    /// </summary>
    private void GetTriangles()
    {
        meshTriangles = new int[(meshVertices.Length - 2) * 3];

        for (int i = 1; i < Mathf.Ceil((float)meshVertices.Length / 2); i++)
        {
            int idTriangle = i * 6 - 1;
            int idVertices = i * 2 + 1;

            meshTriangles[idTriangle - 5] = idVertices - 3;
            meshTriangles[idTriangle - 4] = idVertices - 1;
            meshTriangles[idTriangle - 3] = idVertices - 2;

            meshTriangles[idTriangle - 2] = idVertices - 2;
            meshTriangles[idTriangle - 1] = idVertices - 1; 
            meshTriangles[idTriangle] = idVertices;
        }
        
        /* Logging
        string logged = "";
        int ifdfi = 0;
        for (int i = 0; i < meshTriangles.Length; i++)
        {
            logged += meshTriangles[i] + " : ";

            ifdfi++;
            if (ifdfi > 2)
            {
                logged += "| : ";

                ifdfi = 0;
            }
        }

        Debug.Log(logged);
        */
    }

    public void InterpolateLastVertices(float distance)
    {
        meshVertices[^1] = Vector3.Lerp(meshVertices[^3], interpolationVertex1, distance);
        meshVertices[^2] = Vector3.Lerp(meshVertices[^4], interpolationVertex2, distance);

        if (particlesFoam != null)
        {
            Vector3 vertex1 = meshVertices[^1];
            Vector3 vertex2 = meshVertices[^2];

            Quaternion lookRotation = Quaternion.LookRotation(vertex1 - vertex2);
            float vertexDistance = Vector2.Distance(vertex1, vertex2) / 2;

            particlesFoamMain.startSize = vertexDistance * .7f;
            particlesFoamShape.radius = vertexDistance;
            
            particlesFoam.transform.position = transform.TransformDirection((vertex1 + vertex2) / 2) + transform.position;
            particlesFoam.transform.rotation = lookRotation * transform.rotation;
        }

        meshWater.vertices = meshVertices;
        meshWater.uv = meshUV;
        meshFilter.mesh = meshWater;
    }

    /// <summary>
    /// Gets next water vertex for water travel.
    /// 
    /// Triggered everytime travelDistance exceeds travelDistanceUntilNext.
    /// </summary>
    void TravelNext()
    {
        travelId++;

        if (travelId >= waterVertices.Length - 1)
        {
            enabled = false;

            travelSpeed = 0;

            return;
        }

        travelDistanceUntilNextLast = travelDistanceUntilNext;
        travelDistanceUntilNext += waterVertices[travelId].distance;
        travelSpeed = waterVertices[travelId].speed;

        //Check if travelDistance didn't pass UntilNext again.
        if (travelDistance >= travelDistanceUntilNext)
        {
            //Check if travelDistance didn't pass Total, so it doesn't skip parts of the model.
            if (travelDistance >= travelDistanceTotal)
            {
                GetMesh();

                interpolationVertex1 = meshVertices[^1];
                interpolationVertex2 = meshVertices[^2];

                return;
            }

            TravelNext();

            return;
        }

        GetVertices(travelId + 2);
        GetTriangles();

        interpolationVertex1 = meshVertices[^1];
        interpolationVertex2 = meshVertices[^2];

        meshUV[^1] = GetRotatedUV(interpolationVertex1);
        meshUV[^2] = GetRotatedUV(interpolationVertex2);

        meshWater.vertices = meshVertices;
        meshWater.uv = meshUV;
        meshWater.triangles = meshTriangles;

        meshFilter.mesh = meshWater;
    }

    public void ResetTravel()
    {
        travelDistance = 0;

        interpolationVertex1 = Vector3.zero;
        interpolationVertex2 = Vector3.zero;

        travelId = 0;
        travelDistanceUntilNext = waterVertices[0].distance;
        travelDistanceUntilNextLast = 0;
        travelSpeed = waterVertices[0].speed;
        waitForTravelTimer = 0;

        if (waterVertices.Length < 2) return;

        WaterVertex vertex = waterVertices[1];

        interpolationVertex1 = vertex.transform.localPosition + CalculateWidth(vertex.transform, vertex.width);
        interpolationVertex2 = vertex.transform.localPosition - CalculateWidth(vertex.transform, vertex.width);
    }

    Vector3 CalculateWidth(Transform vertex, float width)
    {
        return transform.InverseTransformDirection(vertex.right) * width;
    }

    Vector2 GetRotatedUV(Vector3 coordinate)
    {
        Vector3 rotated = Quaternion.Euler(rotateUV) * transform.InverseTransformPoint(coordinate);
        return new Vector2(rotated.x, rotated.z);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            if (waterVertices.Length < 2 || waterVertices[^1] == null) return;
            
            GetMesh();
        }
    }
}
