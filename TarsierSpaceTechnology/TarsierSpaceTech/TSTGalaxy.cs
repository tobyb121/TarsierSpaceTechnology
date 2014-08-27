using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    class TSTGalaxy : MonoBehaviour
    {
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
            print("Creating Mat");
            Material mat = new Material(Shader.Find("Unlit/Transparent"));
            print("Getting tex");
            mat.mainTexture = GameDatabase.Instance.GetTexture("TarsierSpaceTech/galaxy1", false);
            print("assinging mat");
            renderer.material = mat;
            gameObject.layer = 10;
            renderer.castShadows = false;
            renderer.receiveShadows = false;
        }

        private static Mesh mesh = null;
        
        public void Update()
        {
            transform.LookAt(transform.parent.position);
            if (Input.GetKeyDown(KeyCode.P))
            {
                transform.localScale *= 10;
                print(transform.localScale);
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                transform.localScale /= 10;
                print(transform.localScale);
            }
        }
    }
}
