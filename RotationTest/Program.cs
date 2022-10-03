using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        private const double Pi = 3.1415926535897932384626433832795;
        private const double Radian = 6.28319;
        private const double DAngle = 180.0;

        private readonly List<IMyTerminalBlock> _motorRotor = new List<IMyTerminalBlock>();


        private float RotationAmountInDegrees { set; get; }
        private float StartAngle { set; get; }
        private int NumberOfRotations { set; get; }
        private float RotationSpeed { set; get; }
        private float DesiredAngle { set; get; }

        private string _currentAngle = "";

        private enum RotationState
        {
            Rotate,
            SetAngle,
            Stop,
        }

        private RotationState _rotationState = RotationState.SetAngle;

        public Program()
        {
            GridTerminalSystem.SearchBlocksOfName("Motor Rotor", _motorRotor, rotor => rotor is IMyMotorStator);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;


            StartAngle = 360;
            RotationAmountInDegrees = 90.0f;
            NumberOfRotations = (int)(StartAngle / RotationAmountInDegrees);
            RotationSpeed = 0.5f;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Desired Angle: " + DesiredAngle);
            Echo("Current Angle: " + _currentAngle);
            Echo("Rotations Remaining: " + NumberOfRotations);

            switch (_rotationState)
            {
                case RotationState.Rotate:
                    Echo("Rotating");
                    RotateMotor();
                    break;
                case RotationState.SetAngle:
                    Echo("Setting Angles");
                    SetAngle();
                    break;
                case RotationState.Stop:
                    Echo("Setting Angles");
                    Stop();
                    break;
                default:
                    Echo("No Set State");
                    break;
            }
        }

        private void Stop()
        {
            foreach (var motor in _motorRotor.Cast<IMyMotorStator>())
            {
                motor.TargetVelocityRPM = 0.0f;
            }
        }

        private void RotateMotor()
        {
            foreach (var motor in _motorRotor.Cast<IMyMotorStator>())
            {
                if (Math.Abs(motor.Angle - DesiredAngle) < 0.1f)
                {
                    motor.TargetVelocityRPM = 0.0f;
                    _rotationState = RotationState.SetAngle;
                    NumberOfRotations -= 1;
                }
                motor.TargetVelocityRPM = RotationSpeed;
                _currentAngle = motor.Angle.ToString();
            }
        }


        private void SetAngle()
        {
            if (NumberOfRotations <= 0)
            {
                _rotationState = RotationState.Stop;
                return;
            }

            foreach (var motor in _motorRotor.Cast<IMyMotorStator>())
            {
                DesiredAngle = motor.Angle + (float)ConvertDegToRad(RotationAmountInDegrees);
                if (DesiredAngle > Radian)
                {
                    DesiredAngle = DesiredAngle - (float)Radian;
                }
            }

            _rotationState = RotationState.Rotate;
        }

        private static double ConvertDegToRad(float angle)
        {
            return angle * (Pi / DAngle);
        }

    }
}
