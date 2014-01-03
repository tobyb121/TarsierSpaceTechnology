using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    class ChemCam : PartModule
    {
        private bool _inEditor = false;

        private Transform _lookTransform;
        private CameraModule _camera;

        private Transform _headTransform;
        private Transform _upperArmTransform;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor)
            {
                _inEditor = true;
                return;
            }
            _lookTransform = Utils.FindChildRecursive(transform,"LookTransform");
            _camera=_lookTransform.gameObject.AddComponent<CameraModule>();
            _camera.fov = 90;

            _headTransform = Utils.FindChildRecursive(transform, "CameraBody");
            _upperArmTransform = Utils.FindChildRecursive(transform, "ArmUpper");

            vessel.OnFlyByWire += new FlightInputCallback(handleInput);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }

        private void handleInput(FlightCtrlState ctrl)
        {
            if (ctrl.X>0)
            {
                _upperArmTransform.forward = Vector3.RotateTowards(_upperArmTransform.forward, _upperArmTransform.right, 0.05f, 1);
            }
            else if (ctrl.X < 0)
            {
                _upperArmTransform.forward = Vector3.RotateTowards(_upperArmTransform.forward, -_upperArmTransform.right, 0.05f, 1);
            }
        }
    }
}
