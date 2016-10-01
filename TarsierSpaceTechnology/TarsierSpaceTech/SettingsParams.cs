using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    public class TST_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return "Tarsier Space Tech Options"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "Tarsier Space Technologies"; } }
        public override int SectionOrder { get { return 1; } }

        [GameParameters.CustomIntParameterUI("Small ChemCam Window (pixels)", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "Small Size setting of ChemCam Window in pixels")]
        public int ChemwinSml = 250;

        [GameParameters.CustomIntParameterUI("Large ChemCam Window (pixels)", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "Large Size setting of ChemCam Window in pixels")]
        public int ChemwinLge = 500;

        [GameParameters.CustomIntParameterUI("Small Telescope Window (pixels)", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "Small Size setting of Telescope Window in pixels")]
        public int TelewinSml = 300;

        [GameParameters.CustomIntParameterUI("Large Telescope Window (pixels)", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "Large Size setting of Telescope Window in pixels")]
        public int TelewinLge = 600;

        [GameParameters.CustomIntParameterUI("Maximum ChemCam Contracts", minValue = 1, maxValue = 20, stepSize = 1, autoPersistance = true, toolTip = "The maximum number of ChemCam Contracts\nthat can be offered at one time capped at 20", gameMode = GameParameters.GameMode.CAREER)]
        public int maxChemCamContracts = 3;

        [GameParameters.CustomParameterUI("ChemCam contracts restricted", autoPersistance = true, toolTip = "ChemCam Contracts are only offered for bodies that have already been photographed", gameMode = GameParameters.GameMode.CAREER)]
        public bool photoOnlyChemCamContracts = true;

        [GameParameters.CustomParameterUI("Zoom the Star Field", toolTip = "If on, the Star Field (skybox) will zoom with the telescope lens,\nif off the Star Field (skybox) will not zoom.")]
        public bool ZoomSkyBox = true;

        [GameParameters.CustomParameterUI("Use Stock App Launcher Icon", toolTip = "If on, the Stock Application launcher will be used,\nif off will use Blizzy Toolbar if installed.")]
        public bool UseAppLauncher = true;

        [GameParameters.CustomParameterUI("ToolTips On", autoPersistance = true, toolTip = "Turn the Tooltips on and off.")]
        public bool ToolTips = true;

        [GameParameters.CustomParameterUI("Extra Debug Logging", toolTip = "Turn this On to capture lots of extra information\ninto the KSP log for reporting a problem.")]
        public bool debugging = false;

        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
            Debug.Log("Setting difficulty preset");
            switch (preset)
            {
                case GameParameters.Preset.Easy:
                    
                    break;
                case GameParameters.Preset.Normal:
                    
                    break;
                case GameParameters.Preset.Moderate:
                    
                    break;
                case GameParameters.Preset.Hard:
                    
                    break;
                case GameParameters.Preset.Custom:
                    break;
            }
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (member.Name != "UseAppLauncher" && member.Name != "debugging")
                        return false;
                }
            }
            
            return true;
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (HighLogic.fetch != null)
            {
                if (HighLogic.LoadedSceneIsFlight)
                {
                    if (member.Name != "UseAppLauncher" && member.Name != "debugging")
                        return false;
                }
            }
            
            if (member.Name == "UseAppLauncher")
            {
                if (RSTUtils.ToolbarManager.ToolbarAvailable)
                    return true;
                else
                    return false;
            }

            return true;
        }
    }
}
