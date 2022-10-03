using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private bool _scriptRecompiled = false;

        // Motor Stator
        private const double Pi = 3.1415926535897932384626433832795;
        private const double Radian = 6.28319;
        private const double DAngle = 180.0;

        private readonly List<IMyTerminalBlock> _motorRotor = new List<IMyTerminalBlock>();


        private float RotationAmountInDegrees { set; get; }
        private float StartAngle { set; get; }
        private int NumberOfRotations { set; get; }
        private float RotationSpeed { set; get; }
        private float DesiredAngle { set; get; }
        private bool MotorSetupComplete { set; get; }

        private string _currentAngle = "";

        private enum RotationState
        {
            Rotate,
            SetAngle,
            Stop,
        }

        private RotationState _rotationState = RotationState.Stop;


        // Rig 

        private enum RigStates
        {
            Start,
            Reset,
            Setup,
            Stop
        }

        private RigStates _rigStates = RigStates.Stop;

        // Pistons Vertical

        private readonly List<IMyTerminalBlock> _verticalPistons = new List<IMyTerminalBlock>();
        private float VerticalExtendIncrement { set; get; }
        private float VerticalExtendVelocity { set; get; }
        private float VerticalStartPosition { set; get; }
        private float VerticalCurrentExtendLimit { set; get; }
        private bool VerticalFullExtended { set; get; }
        private bool VerticalSetupComplete { set; get; }

        private enum VerticalPistonStates
        {
            Extend,
            Stop,
        }

        private VerticalPistonStates _verticalPistonStates = VerticalPistonStates.Stop;

        // Pistons Horizontal

        private readonly List<IMyTerminalBlock> _horizontalPistons = new List<IMyTerminalBlock>();
        private float HorizontalExtendVelocity { set; get; }
        private bool HorizontalSetupComplete { set; get; }
        private enum HorizontalPistonStates
        {
            Extend,
            Retract,
            Stop
        }

        private HorizontalPistonStates _horizontalPistonStates = HorizontalPistonStates.Stop;

        // mining states

        private enum MiningStates
        {
            Horizontal,
            Vertical,
            Rotate
        }

        private MiningStates _miningStates = MiningStates.Vertical;


        // Drills
        private readonly List<IMyTerminalBlock> _drills = new List<IMyTerminalBlock>();

        private void Reset()
        {
            // Motor Rotor Starting Values
            StartAngle = 0.0f;
            RotationAmountInDegrees = 12.0f;
            NumberOfRotations = NumberOfRotationsCal();
            RotationSpeed = 0.5f; // 0.05f = half revolution per minute
            MotorSetupComplete = false;
            DesiredAngle = StartAngle;

            // drills
            DrillsStatus(false);

            // Vertical Pistons
            VerticalExtendIncrement = 1.5f; // 2 * 1.5 = 3m drop
            VerticalExtendVelocity = 0.4f;
            VerticalStartPosition = 0.0f;
            VerticalCurrentExtendLimit = VerticalExtendIncrement;
            VerticalFullExtended = !(VerticalStartPosition < 10.0f);
            VerticalSetupComplete = false;

            if (VerticalStartPosition > VerticalCurrentExtendLimit)
            {
                VerticalCurrentExtendLimit += VerticalStartPosition;
            }

            // Horizontal Pistons
            HorizontalExtendVelocity = 0.4f;
            HorizontalSetupComplete = false;


            _verticalPistonStates = VerticalPistonStates.Stop;
            _horizontalPistonStates = HorizontalPistonStates.Stop;
            _miningStates = MiningStates.Vertical;

            _rigStates = RigStates.Setup;
        }

        public Program()
        {
            _scriptRecompiled = true;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            GridTerminalSystem.SearchBlocksOfName("Motor Rotor", _motorRotor, rotor => rotor is IMyMotorStator);
            GridTerminalSystem.SearchBlocksOfName("Vertical Piston", _verticalPistons, piston => piston is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Horizontal Piston", _horizontalPistons, piston => piston is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Drill", _drills, drill => drill is IMyShipDrill);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Motor Setup: " + MotorSetupComplete);
            Echo("Vertical Setup: " + VerticalSetupComplete);
            Echo("Horizontal Setup: " + HorizontalSetupComplete);
            Echo("Rig State: " + _rigStates);
            Echo("Mining State: " + _miningStates);
            Echo("Horizontal State: " + _horizontalPistonStates);
            Echo("Horizontal Velocity: " + HorizontalExtendVelocity);
            Echo("Rotation Count Down: " + NumberOfRotations);

            switch (_rigStates)
            {
                case RigStates.Start:
                    Echo("Rig is in operation.");
                    Mining();
                    break;
                case RigStates.Reset:
                    Echo("Rig is resetting.");
                    Reset();
                    break;
                case RigStates.Setup:
                    Echo("Setting up the rig.");
                    Setup();
                    break;
                case RigStates.Stop:
                    RigStop();
                    if (argument == "reset")
                    {
                        _scriptRecompiled = false;
                        _rigStates = RigStates.Reset;
                    }

                    if (argument == "setup" && !_scriptRecompiled)
                    {
                        _rigStates = RigStates.Setup;
                    }
                    Echo("Rig is stopped.");
                    break;
                default:
                    Echo("Not State Set. Recompile the script.");
                    break;
            }

        }

        private void Mining()
        {
            switch (_miningStates)
            {
                case MiningStates.Horizontal:
                    Echo("Mining Horizontal");
                    MineHorizontal();
                    break;
                case MiningStates.Vertical:
                    Echo("Mining Vertical");
                    MineVertical();
                    break;
                case MiningStates.Rotate:
                    Echo("Mining Rotate");
                    MineRotate();
                    break;
                default:
                    break;
            }
        }

        private void MineRotate()
        {
            switch (_rotationState)
            {
                case RotationState.Rotate:
                    RotateMotor();
                    break;
                case RotationState.SetAngle:
                    SetAngle();
                    break;
                case RotationState.Stop:
                default:
                    StopMotors();
                    break;
            }
        }

        private void RotateMotor()
        {
            foreach (var motor in _motorRotor.Cast<IMyMotorStator>())
            {
                motor.TargetVelocityRPM = RotationSpeed;
                _currentAngle = motor.Angle.ToString();
                if (Math.Abs(motor.Angle - DesiredAngle) < 0.1f)
                {
                    motor.TargetVelocityRPM = 0.0f;
                    _miningStates = MiningStates.Horizontal;
                    _horizontalPistonStates = HorizontalExtendVelocity > 0.0f ? HorizontalPistonStates.Retract : HorizontalPistonStates.Extend;
                    HorizontalExtendVelocity = HorizontalExtendVelocity * -1;
                    StopMotors();
                    NumberOfRotations -= 1;
                    _rotationState = RotationState.Stop;
                }

            }
        }

        private void SetAngle()
        {
            if (NumberOfRotations <= 0)
            {
                _miningStates = MiningStates.Vertical;
                VerticalCurrentExtendLimit += VerticalExtendIncrement;
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

        private void MineHorizontal()
        {
            switch (_horizontalPistonStates)
            {
                case HorizontalPistonStates.Extend:
                case HorizontalPistonStates.Retract:
                    foreach (var piston in _horizontalPistons.Cast<IMyPistonBase>())
                    {
                        piston.Velocity = HorizontalExtendVelocity;
                        if (piston.CurrentPosition >= piston.MaxLimit && piston.Velocity > 0.0f 
                            || piston.CurrentPosition <= piston.MinLimit && piston.Velocity < 0.0f)
                        {
                            _miningStates = MiningStates.Rotate;
                            _rotationState = RotationState.SetAngle;
                        }   
                    }
                    break;
                case HorizontalPistonStates.Stop:
                default:
                    break;
            }
        }

        private void MineVertical()
        {

            foreach (var piston in _verticalPistons.Cast<IMyPistonBase>())
            {
                piston.MaxLimit = VerticalCurrentExtendLimit;
                if (piston.CurrentPosition < piston.MaxLimit)
                {
                    piston.Velocity = VerticalExtendVelocity;
                }
                else
                {
                    piston.Velocity = 0.0f;
                    _miningStates = MiningStates.Horizontal;
                    _horizontalPistonStates = HorizontalExtendVelocity > 0.0f ? HorizontalPistonStates.Retract : HorizontalPistonStates.Extend;
                    HorizontalExtendVelocity = HorizontalExtendVelocity * -1;
                    piston.Velocity = 0.0f;
                }

                if (Math.Abs(piston.HighestPosition - piston.CurrentPosition) < 0.1f)
                {
                    VerticalFullExtended = true;
                }
            }
        }

        private void Setup()
        {
            DrillsStatus(true);

            foreach (var piston in _verticalPistons.Cast<IMyPistonBase>())
            {
                piston.MinLimit = VerticalStartPosition;
                piston.MaxLimit = VerticalStartPosition;

                if (piston.CurrentPosition < VerticalStartPosition)
                {
                    piston.Velocity = VerticalExtendVelocity;
                }
                else if (piston.CurrentPosition > VerticalStartPosition)
                {
                    piston.Velocity = VerticalExtendVelocity * -1;
                }else
                {
                    piston.Velocity = 0.0f;
                    VerticalSetupComplete = true;
                }
            }

            foreach (var piston in _horizontalPistons.Cast<IMyPistonBase>())
            {
                if (piston.CurrentPosition > 0.0f)
                {
                    piston.Velocity = HorizontalExtendVelocity * -1;
                }
                else
                {
                    piston.Velocity = 0.0f;
                    HorizontalSetupComplete = true;
                }
            }


            foreach (var motor in _motorRotor.Cast<IMyMotorStator>())
            {
                if (Math.Abs(motor.Angle - StartAngle) < 0.1f)
                {
                    motor.TargetVelocityRPM = 0.0f;
                    MotorSetupComplete = true;
                }
                else if(motor.Angle > StartAngle)
                {
                    motor.TargetVelocityRPM = RotationSpeed;
                }
                else
                {
                    motor.TargetVelocityRPM = RotationSpeed * -1;
                }
            }

           

            if (!MotorSetupComplete || !VerticalSetupComplete || !HorizontalSetupComplete) return;
            _miningStates = MiningStates.Horizontal;
            _horizontalPistonStates = HorizontalPistonStates.Extend;
            _rigStates = RigStates.Start;
        }

        private void RigStop()
        {
            DrillsStatus(false);
            SetPistonVelocity(_verticalPistons, 0.0f);
            SetPistonVelocity(_horizontalPistons, 0.0f);
            StopMotors();
        }

        private void StopMotors()
        {
            foreach (var motor in _motorRotor.Cast<IMyMotorStator>())
            {
                motor.TargetVelocityRPM = 0.0f;
            }
        }


        private static void SetPistonVelocity(IEnumerable<IMyTerminalBlock> pistons, float velocity)
        {
            foreach (var piston in pistons.Cast<IMyPistonBase>())
            {
                piston.Velocity = velocity;
            }
        }

        private void DrillsStatus(bool status)
        {
            foreach (var drill in _drills.Cast<IMyShipDrill>())
            {
                drill.Enabled = status;
            }
        }

        private int NumberOfRotationsCal()
        {
            return (int)(360.0f / RotationAmountInDegrees);
        }

        private static double ConvertDegToRad(float angle)
        {
            return angle * (Pi / DAngle);
        }
    }
}
