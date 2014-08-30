using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    public class TSTGalaxy : MonoBehaviour
    {

        private static Mesh mesh = null;

        private Material mat = new Material(Shader.Find("Unlit/Transparent"));
        public new string name;

        private float _size = 1e3f;
        public float size
        {
            get { return _size * ScaledSpace.ScaleFactor; }
            set {
                _size = value / ScaledSpace.ScaleFactor;
                transform.localScale = _size * Vector3.one;
            }
        }
        
        public void Start()
        {
            if (mesh == null)
            {
                Utils.print("Generating GalaxyMesh");
                mesh = new Mesh();
                mesh.vertices = new Vector3[]{
                    new Vector3(-1,0.75f,0),
                    new Vector3(-1,-0.75f,0),
                    new Vector3(1,0.75f,0),
                    new Vector3(1,-0.75f,0)
                };
                mesh.uv = new Vector2[]{
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                    new Vector2(1, 1),
                    new Vector2(1, 0)
                };

                mesh.triangles = new int[]{
                    0,1,2,
                    3,2,1
                    };
               mesh.RecalculateNormals();
                
            }
            gameObject.GetComponent<MeshFilter>().mesh = mesh;
            renderer.material = mat;
            gameObject.layer = 10;
            renderer.castShadows = false;
            renderer.receiveShadows = false;
        }
        
        public void Update()
        {
            transform.LookAt(transform.parent.position);
        }


        public void setTexture(Texture texture)
        {
            mat.mainTexture = texture;
        }

        public Vector3 scaledPosition{
            get
            {
                return transform.localPosition;
            }
            set
            {
                transform.localPosition = value;
            }

        }

        public Vector3 position
        {
            get
            {
                return ScaledSpace.ScaledToLocalSpace(transform.position);
            }
            set{
                transform.position = ScaledSpace.LocalToScaledSpace(value);
            }
        }
    }
}
