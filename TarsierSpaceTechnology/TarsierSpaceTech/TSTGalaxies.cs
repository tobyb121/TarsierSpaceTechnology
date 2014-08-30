using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TSTGalaxies : MonoBehaviour
    {
        public static TSTGalaxies Instance;

        private static GameObject baseTransform;
        public static GameObject galaxy;
        private static GameObject cube;
        public void Start()
        {
            Utils.print("Starting Galaxies");
            if (Instance != null)
                Destroy(this);
            else
                Instance = this;
            baseTransform = new GameObject();
            baseTransform.transform.parent = ScaledSun.Instance.transform;
            baseTransform.transform.localPosition = Vector3.zero;
            baseTransform.transform.localRotation = Quaternion.identity;
            
            galaxy=new GameObject("g1",typeof(MeshFilter),typeof(MeshRenderer),typeof(TSTGalaxy));
            galaxy.transform.parent = baseTransform.transform;
            galaxy.transform.localPosition = new Vector3(0f,-130e6f,0f);
            galaxy.transform.localScale = 1e4f*Vector3.one;
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.collider.enabled = false;
        }

        

        public void Update()
        {
            
        }

    }
}
