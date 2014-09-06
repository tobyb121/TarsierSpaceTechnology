using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class TSTGalaxies : MonoBehaviour
    {
        public static TSTGalaxies Instance;

        internal GameObject baseTransform;
        private List<TSTGalaxy> _galaxies = new List<TSTGalaxy>();

        public static List<TSTGalaxy> Galaxies
        {
            get
            {
                return Instance._galaxies;
            }
        }

        public void Start()
        {
            Utils.print("Starting Galaxies");
            if (Instance != null)
                Destroy(this);
            else
                Instance = this;
            baseTransform = new GameObject();
            baseTransform.transform.localPosition = Vector3.zero;
            baseTransform.transform.localRotation = Quaternion.identity;
            if (ScaledSun.Instance != null)
                baseTransform.transform.parent = ScaledSun.Instance.transform;
            else
                baseTransform.SetActive(false);

            UrlDir.UrlConfig[] galaxyCfgs = GameDatabase.Instance.GetConfigs("GALAXY");
            foreach (UrlDir.UrlConfig cfg in galaxyCfgs){
                GameObject go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(TSTGalaxy));
                go.transform.parent = baseTransform.transform;
                TSTGalaxy galaxy = go.GetComponent<TSTGalaxy>();
                galaxy.Load(cfg.config);
                Utils.print("Adding Galaxy");
                Galaxies.Add(galaxy);
            }
        }
    }
}
