/*
 * TSTGyroReactionWheel.cs
 * (C) Copyright 2015, Jamie Leighton
 * Tarsier Space Technologies
 * The original code and concept of TarsierSpaceTech rights go to Tobyb121 on the Kerbal Space Program Forums, which was covered by the MIT license.
 * Original License is here: https://github.com/JPLRepo/TarsierSpaceTechnology/blob/master/LICENSE
 * As such this code continues to be covered by MIT license.
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of TarsierSpaceTech.
 *
 *  TarsierSpaceTech is free software: you can redistribute it and/or modify
 *  it under the terms of the MIT License 
 *  The maths function used in this module was adapted from CactEye2 Mod for KSP licensed under (CC BY 4.0).
 *
 *  TarsierSpaceTech is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
 *
 *  You should have received a copy of the MIT License
 *  along with TarsierSpaceTech.  If not, see <http://opensource.org/licenses/MIT>.
 *
 */

namespace TarsierSpaceTech
{
    class TSTGyroReactionWheel: ModuleReactionWheel
    {
        [KSPField(isPersistant = true)]
        float _basePitchTorque;
        [KSPField(isPersistant = true)]
        float _baseYawTorque;
        [KSPField(isPersistant = true)]
        float _baseRollTorque;
        [KSPField(isPersistant = false)]
        public float powerscale = 0.1f;
        [KSPField(isPersistant = false)]
        public float sensitivity = 1f;
               


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            _basePitchTorque = PitchTorque;
            _baseYawTorque = YawTorque;
            _baseRollTorque = RollTorque;
        }
                       
        public override void OnUpdate()
        {
            base.OnUpdate();
            PitchTorque = _basePitchTorque * (powerscale + ((1 - powerscale) * sensitivity));
            RollTorque  = _baseRollTorque * (powerscale + ((1 - powerscale) * sensitivity));
            YawTorque = _baseYawTorque * (powerscale + ((1 - powerscale) * sensitivity));
            //base.OnUpdate();
        }
    }  
 
 

  
}
