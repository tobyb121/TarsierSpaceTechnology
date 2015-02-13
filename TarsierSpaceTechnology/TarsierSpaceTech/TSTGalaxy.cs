using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    public class TSTGalaxy : MonoBehaviour, ITargetable
    {

        private Orbit _galaxy_orbit = null;
        private OrbitDriver _galaxy_orbitdriver = null;
        private VesselTargetModes _galaxy_targetmodes = VesselTargetModes.Direction;

        private static Mesh mesh = null;

        private Material mat = new Material(Shader.Find("Unlit/Transparent"));
        public string theName;

        private ConfigNode config;

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

        public void Load(ConfigNode config)
        {
            this.config = config;
            string name = config.GetValue("name");
            string theName = config.GetValue("theName");
            Vector3 pos = ConfigNode.ParseVector3(config.GetValue("location"));
            string textureURL = config.GetValue("textureURL");
            float size = float.Parse(config.GetValue("size"));
            Utils.print("Creating Galaxy: " + name + " " + pos.ToString() + " " + textureURL);
            Utils.print("Setting Name");
            this.name = name;
            this.theName = theName;
            Utils.print("Setting Size");
            this.size = 1e3f * size * ScaledSpace.ScaleFactor;
            Utils.print("Setting Position");
            this.scaledPosition = -130e6f * pos.normalized;
            Utils.print("Setting Texture");
            this.setTexture(GameDatabase.Instance.GetTexture(textureURL, false));
            Utils.print("Finished creating galaxy");
        }

        public void attach(GameObject parent)
        {
            transform.parent = parent.transform;
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
        // ITargetable
        public Vector3 GetFwdVector()
        {
            return Vector3.zero;
        }
        
        public string GetName()
        {
            return this.name;
        }

        public Vector3 GetObtVelocity()
        {
            return Vector3.zero;
        }
        public Orbit GetOrbit()
        {
            return _galaxy_orbit;
        }
        
        public OrbitDriver GetOrbitDriver()
        {
            return _galaxy_orbitdriver;
        }
        
        public Vector3 GetSrfVelocity()
        {
            return Vector3.zero;
        }
        
        public VesselTargetModes GetTargetingMode()
        {
            return _galaxy_targetmodes;
        }
        
        public Transform GetTransform()
        {
            return this.transform;
        }
        
        public Vessel GetVessel()
        {
            return null;
        }

    }
}
