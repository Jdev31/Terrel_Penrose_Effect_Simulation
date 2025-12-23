using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
//using Wacton.Unicolour;
public static class Lorentz
{
    // Boost acting on (t, x, y, z), with c = 1
    public static Matrix<double> Boost4D(Vector3 betaVec)
    {
        double bx = betaVec.x;
        double by = betaVec.y;
        double bz = betaVec.z;

        double beta2 = bx * bx + by * by + bz * bz;

        if (beta2 < 1e-8)
            return DenseMatrix.CreateIdentity(4);

        if (beta2 >= 1.0)
            throw new System.ArgumentException("|beta| must be < 1");

        double gamma = 1.0 / System.Math.Sqrt(1.0 - beta2);
        double gm1 = gamma - 1.0;

        var L = DenseMatrix.Create(4, 4, 0.0);

        // Time row
        L[0, 0] = gamma;
        L[0, 1] = -gamma * bx;
        L[0, 2] = -gamma * by;
        L[0, 3] = -gamma * bz;

        // Time column
        L[1, 0] = -gamma * bx;
        L[2, 0] = -gamma * by;
        L[3, 0] = -gamma * bz;

        // Spatial block
        L[1, 1] = 1 + gm1 * bx * bx / beta2;
        L[1, 2] = gm1 * bx * by / beta2;
        L[1, 3] = gm1 * bx * bz / beta2;

        L[2, 1] = gm1 * by * bx / beta2;
        L[2, 2] = 1 + gm1 * by * by / beta2;
        L[2, 3] = gm1 * by * bz / beta2;

        L[3, 1] = gm1 * bz * bx / beta2;
        L[3, 2] = gm1 * bz * by / beta2;
        L[3, 3] = 1 + gm1 * bz * bz / beta2;

        return L;
    }
}

public static class Doppler
{
    public static float relativistic_doppler(float init_wl, Vector3 object_velocity, Vector3 object_pos, Vector3 observerPos)
    {
        // 1. Get the direction from the object to the observer
        Vector3 toObserver = (observerPos - object_pos).normalized;

        // 2. Get the direction the object is traveling
        Vector3 travelDir = object_velocity.normalized;

        // 3. Find the cosine of the angle between travel and observer
        float cosTheta = Vector3.Dot(travelDir, toObserver);

        float beta = Vector3.Dot(travelDir, object_velocity);

        // 4. Calculate the frequency shift
        return init_wl * (1 - beta * cosTheta) / (Mathf.Sqrt(1 - (beta * beta)));
    }
}
public class Transfomation : MonoBehaviour
{
    // Getting Velity Components, cant set a range on a Vector3 in the inspector, which is why it is split up
    [SerializeField, Range(-0.99f, 0.99f)] float velX;
    [SerializeField, Range(-0.99f, 0.99f)] float velY;
    [SerializeField, Range(-0.99f, 0.99f)] float velZ;
    // Combined into a read only vector3 as intertal reference frame
    Vector3 Velocity => new Vector3(velX, velY, velZ);

    // Modifer to velocity to speed up game time
    [SerializeField]
    private int Beta_modifier;

    // Used to get coordinates of Observer
    public Transform Observer;

    // Array for verticies without Terrel-Penrose effect
    private Vector3[] Actual_vertices;
    // Object mesh that will be edited to show TP effect
    Mesh mesh;

    private Vector3 Beta; 
    [SerializeField]
    // Boolean to quickly ignore terrell rotation
    bool Terrel_rotation = true;

    private Renderer rendererComponent;

    void Start()
    {
        
        Beta = Velocity;
        mesh = GetComponent<MeshFilter>().mesh;

        Actual_vertices = new Vector3[mesh.vertexCount];

        for (int i = 0; i < mesh.vertices.Length; i++)
        {


            // Solve t = beta·x  (c = 1)
            // Get the time for the object at each vertex, where t = zero
            // Not negative velicty as it would be negative negative
            double t = Vector3.Dot(Beta, mesh.vertices[i]);

            //Vector<double> Xp = Lorentz.Boost4D(-Beta) * X;

            // Assuming your mesh was imported from Blender and looks 'wrong'
            var X = Vector<double>.Build.Dense(new double[]
            {
                t,
                mesh.vertices[i].x,  // Unity X (Blender X)
                mesh.vertices[i].y,  // Unity Y (Blender Z)
                mesh.vertices[i].z   // Unity Z (Blender Y)
            });

            //Vector3 beta = new Vector3(0.8f, 0f, 0f);
            //inverse lorenz boost
            Matrix<double> L = Lorentz.Boost4D(Beta);
            Vector<double> Xp = L * X;
            
            Actual_vertices[i] = new Vector3(
                (float)Xp[1],
                (float)Xp[2],
                (float)Xp[3]
            );
            
        }
        mesh.vertices = Actual_vertices;

        

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // Update is called once per frame
    void Update()
    {
        // 1. Move the actual GameObject so it travels through the world
        transform.position += Velocity * Beta_modifier * Time.deltaTime;

        if (Terrel_rotation)
        {
            Vector3[] workingVerts = new Vector3[Actual_vertices.Length];

            for (int i = 0; i < Actual_vertices.Length; i++)
            {
                // 2. Get the 'Rest Frame' vertex in World Space
                // We use the original local coordinates (Actual_vertices) 
                // and find where they would be in the world right now.
                Vector3 worldV = transform.TransformPoint(Actual_vertices[i]);

                // 3. Apply the Light Signal Delay (Terrell-Larmor effect)
                // Since c=1, time delay = distance
                float dist = Vector3.Distance(worldV, Observer.position);
                Vector3 observedWorldV = worldV - (Beta * dist);

                // 4. Convert back to local space for the mesh display
                workingVerts[i] = transform.InverseTransformPoint(observedWorldV);
            }

            mesh.vertices = workingVerts;
            mesh.RecalculateBounds();
        }
        // wavelength thing

        rendererComponent = GetComponent<Renderer>();
        Debug.Log(rendererComponent.material.color);

        /*

        // 1. Create a Unicolour object from your RGB values
        Unicolour colour = new Unicolour(ColourSpace.Rgb255, 255, 100, 0); // An orange-ish color
        // 2. Get the Dominant Wavelength in nanometers (nm)
        double wavelength = (float) colour.DominantWavelength;
        float finalwalvelength = Doppler.relativistic_doppler(wavelength, Beta, Velocity, transform.position, Observer.position);
        Console.WriteLine($"Dominant Wavelength: {wavelength} nm");
        */
    }
}
