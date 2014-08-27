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
        private static GameObject galaxy;
        private static GameObject cube;
        public void Start()
        {
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
            galaxy.transform.localPosition = Vector3.zero;
            galaxy.transform.localScale = 1e7f*Vector3.one;
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.collider.enabled = false;
        }

        

        public void Update()
        {
            cube.transform.position = galaxy.transform.position;
            cube.transform.localScale = galaxy.transform.localScale;
            if (Input.GetKeyDown(KeyCode.H))
            {
                galaxy.transform.position+=Vector3.up*1000000;
                Utils.print(galaxy.transform.position);
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                galaxy.transform.position -= Vector3.up * 1000000;
                Utils.print(galaxy.transform.position);
            }
        }

    }
}
