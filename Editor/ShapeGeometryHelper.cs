using System.Collections.Generic;
using TriangleNet;
using TriangleNet.Geometry;
using UnityEngine;
using EGIS.ShapeFileLib;


public class ShapeGeometryHelper : MonoBehaviour
{
    [SerializeField] string shapefile;
    [SerializeField] Material areaMat;
    [SerializeField] float defualtHeight;

    private List<List<Vector2>> _Contours;

    // Start is called before the first frame update
    void Start()
    {
        // ShapeFile shp = new ShapeFile("");
        Debug.Log(Application.dataPath + "/plugins/LandscapeDesignTool/ShapeFiles/" + shapefile);
        ShapeFile shp = new ShapeFile(Application.dataPath + "/plugins/LandscapeDesignTool/ShapeFiles/" + shapefile);
        ShapeFileEnumerator sfEnum = shp.GetShapeFileEnumerator();

        _Contours = new List<List<Vector2>>();

        while (sfEnum.MoveNext())
        {
            System.Collections.ObjectModel.ReadOnlyCollection<PointD[]> pointRecords = sfEnum.Current;


            int i = 0;
            foreach (PointD[] pts in pointRecords)
            {
                if (pts.Length < 3) continue;
                List<Vector2> contour = new List<Vector2>();
                _Contours.Add(contour);

                Debug.Log(string.Format("[NumPoints:{0}]", pts.Length));


                for (int n = 0; n < pts.Length; ++n)
                {
                    Vector2 p = new Vector2((float)(pts[n].X + 655576.0), (float)(pts[n].Y + 217265.0));
                    Debug.Log(pts[n].X + "," + pts[n].Y);
                    //  Vertex vtx = new Vertex((float)pts[n].X, (float)pts[n].Y);
                    contour.Add(p);
                }


                i++;
            }

        }
        GenerateTriangle();
        GenerateWall();
    }

    void GenerateWall()
    {

        RaycastHit hit;

        foreach (List<Vector2> cont in _Contours)
        {
            Vector3[] nv = new Vector3[cont.Count * 2];
            int[] triangles = new int[cont.Count * 2 * 3];

            int i = 0;
            int j = 0;
            int n = 0;

            foreach (Vector2 pt in cont)
            {
                Vector3 tmpv = new Vector3(pt.x, 3000, pt.y);
                if (Physics.Raycast(tmpv, new Vector3(0, -1, 0), out hit, Mathf.Infinity))
                {
                    nv[i++] = new Vector3(pt.x, hit.point.y, pt.y);
                    nv[i++] = new Vector3(pt.x, hit.point.y + defualtHeight, pt.y);
                }
                else
                {
                    nv[i++] = new Vector3(pt.x, 0, pt.y);
                    nv[i++] = new Vector3(pt.x, defualtHeight, pt.y);
                }


            }
            foreach (Vector2 pt in cont)
            {
                triangles[n++] = j;
                triangles[n++] = j + 1;
                triangles[n++] = j + 2;
                triangles[n++] = j + 1;
                triangles[n++] = j + 3;
                triangles[n++] = j + 2;
                if (j < cont.Count) j++;
            }

            GameObject go = new GameObject("ShapeItem");
            var mf = go.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            mf.mesh = mesh;
            mesh.vertices = nv;
            mesh.triangles = triangles;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = areaMat;
            go.transform.parent = gameObject.transform;
        }
    }



    void GenerateTriangle()
    {
        // Debug.Log("GenerateTriangle");
        RaycastHit hit;

        foreach (List<Vector2> cont in _Contours)
        {
            Polygon poly = new Polygon();
            poly.Add(cont);
            var triangleNetMesh = (TriangleNetMesh)poly.Triangulate();

            GameObject go = new GameObject("ShapeItem");

            var mf = go.AddComponent<MeshFilter>();
            var mesh = triangleNetMesh.GenerateUnityMesh();

            Vector3[] nv = new Vector3[mesh.vertices.Length];

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 ov = mesh.vertices[i];
                Vector3 tmpv = new Vector3(ov.x, 3000, ov.y);
                if (Physics.Raycast(tmpv, new Vector3(0, -1, 0), out hit, Mathf.Infinity))
                {
                    nv[i] = new Vector3(ov.x, hit.point.y + defualtHeight, ov.y);
                }
                else
                {

                    nv[i] = new Vector3(ov.x, 0, ov.y);
                }
            }
            mesh.vertices = nv;

            Debug.Log(mesh.bounds.ToString());

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = areaMat;

            mf.mesh = mesh;

            mesh.RecalculateBounds();

            go.transform.parent = gameObject.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
