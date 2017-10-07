using System.Reflection;
using UnityEngine;
using KSP.Localization;

namespace TarsierSpaceTech
{
    public class TST_SettingsParms : GameParameters.CustomParameterNode

    {
        public override string Title { get { return Localizer.Format("#autoLOC_TST_0010"); } } //#autoLOC_TST_0010 = Tarsier Space Tech Options
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override bool HasPresets { get { return true; } }
        public override string Section { get { return "Tarsier Space Technologies"; } }
        public override string DisplaySection { get { return Localizer.Format("#autoLOC_TST_0009"); //#autoLOC_TST_0009 = Tarsier Space Technologies
            }
        }
        public override int SectionOrder { get { return 1; } }

        [GameParameters.CustomIntParameterUI("#autoLOC_TST_0011", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "#autoLOC_TST_0012")] //#autoLOC_TST_0011 = Small ChemCam Window (pixels) #autoLOC_TST_0012 = Small Size setting of ChemCam Window in pixels
        public int ChemwinSml = 250;

        [GameParameters.CustomIntParameterUI("#autoLOC_TST_0013", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "#autoLOC_TST_0014")] //#autoLOC_TST_0013 = Large ChemCam Window (pixels) #autoLOC_TST_0014 = Large Size setting of ChemCam Window in pixels
        public int ChemwinLge = 500;

        [GameParameters.CustomIntParameterUI("#autoLOC_TST_0015", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "#autoLOC_TST_0016")] //#autoLOC_TST_0015 = Small Telescope Window (pixels) #autoLOC_TST_0016 = Small Size setting of Telescope Window in pixels
        public int TelewinSml = 300;

        [GameParameters.CustomIntParameterUI("#autoLOC_TST_0017", minValue = 256, maxValue = 1024, stepSize = 10, autoPersistance = true, toolTip = "#autoLOC_TST_0018")] //#autoLOC_TST_0017 = Large Telescope Window (pixels) #autoLOC_TST_0018 = Large Size setting of Telescope Window in pixels
        public int TelewinLge = 600;

        [GameParameters.CustomIntParameterUI("#autoLOC_TST_0019", minValue = 1, maxValue = 20, stepSize = 1, autoPersistance = true, toolTip = "#autoLOC_TST_0020", gameMode = GameParameters.GameMode.CAREER)] //#autoLOC_TST_0019 = Maximum ChemCam Contracts #autoLOC_TST_0020 = The maximum number of ChemCam Contracts\nthat can be offered at one time capped at 20
        public int maxChemCamContracts = 3;

        [GameParameters.CustomParameterUI("#autoLOC_TST_0021", autoPersistance = true, toolTip = "#autoLOC_TST_0022", gameMode = GameParameters.GameMode.CAREER)] //#autoLOC_TST_0021 = ChemCam contracts restricted #autoLOC_TST_0022 = ChemCam Contracts are only offered for bodies that have already been photographed
        public bool photoOnlyChemCamContracts = true;

        [GameParameters.CustomParameterUI("#autoLOC_TST_0023", toolTip = "#autoLOC_TST_0024")] //#autoLOC_TST_0023 = Zoom the Star Field #autoLOC_TST_0024 = If on, the Star Field (skybox) will zoom with the telescope lens,\nif off the Star Field (skybox) will not zoom.
        public bool ZoomSkyBox = true;

        [GameParameters.CustomParameterUI("#autoLOC_TST_0025", toolTip = "#autoLOC_TST_0026")] //#autoLOC_TST_0025 = Use Stock App Launcher Icon #autoLOC_TST_0026 = If on, the Stock Application launcher will be used,\nif off will use Blizzy Toolbar if installed.
        public bool UseAppLauncher = true;

        [GameParameters.CustomParameterUI("#autoLOC_TST_0027", autoPersistance = true, toolTip = "#autoLOC_TST_0028")] //#autoLOC_TST_0027 = ToolTips On #autoLOC_TST_0028 = Turn the Tooltips on and off.
        public bool ToolTips = true;

        [GameParameters.CustomParameterUI("#autoLOC_TST_0029", toolTip = "#autoLOC_TST_0030")] //#autoLOC_TST_0029 = Extra Debug Logging #autoLOC_TST_0030 = Turn this On to capture lots of extra information\ninto the KSP log for reporting a problem.
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
