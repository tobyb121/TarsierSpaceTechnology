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
        public List<TSTGalaxy> galaxies = new List<TSTGalaxy>();

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

            UrlDir.UrlConfig[] galaxyCfgs = GameDatabase.Instance.GetConfigs("GALAXY");
            foreach (UrlDir.UrlConfig cfg in galaxyCfgs){
                string name = cfg.config.GetValue("name");
                Vector3 pos = ConfigNode.ParseVector3(cfg.config.GetValue("location"));
                string textureURL = cfg.config.GetValue("textureURL");
                float size = float.Parse(cfg.config.GetValue("size"));

                Utils.print("Creating Galaxy: " + name + " " + pos.ToString() + " " + textureURL);
        
                GameObject go=new GameObject("galaxy_"+name,typeof(MeshFilter),typeof(MeshRenderer),typeof(TSTGalaxy));
                go.transform.parent = baseTransform.transform;                

                TSTGalaxy galaxy = go.GetComponent<TSTGalaxy>();
                Utils.print("Setting Name");
                galaxy.name = name;
                Utils.print("Setting Size");
                galaxy.size = 1e3f * size * ScaledSpace.ScaleFactor;
                Utils.print("Setting Position");
                galaxy.scaledPosition = -130e6f * pos.normalized;
                Utils.print("Setting Texture");
                galaxy.setTexture(GameDatabase.Instance.GetTexture(textureURL, false));
                Utils.print("Adding Galaxy");
                galaxies.Add(galaxy);
                Utils.print("Finished creating galaxy");
            }
        }

        

        public void Update()
        {
            
        }

    }
}
