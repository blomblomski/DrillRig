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
        readonly List<IMyTerminalBlock> _drills = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _verticalPistons = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _horizontalPistons = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _motorRotor = new List<IMyTerminalBlock>();

        private enum RigStates
        {
            Reset,
            Start,
            Stop,
            Setup
        }

        private RigStates _rigStates = RigStates.Stop;

        private enum MiningStates
        {
            horizontal,
            vertical,
            rotate,
        }

        private MiningStates _miningStates = MiningStates.vertical;

        // default values

        private const double Pi = 3.1415926535897932384626433832795;

        private const double DAngle = 180.0;

        // rotation
        int NumberOfRotations { set; get; } // how many time the rig will rotate before one whole revolution 
        float StartingAngle { set; get; } // the position that the rig will start from.
        float RotationAmountInDegrees { set; get; } // how much the rig will rotate per rotation.

        private float RotationSpeed { set; get; }

        // pistions
        private float
            ExtendPistonVelocity { set; get; } // How fast the vertical booms extend.  More booms reduce the speed

        private float HorizontalPistonVelocity { set; get; }
        float RetractPistonVelocity { set; get; } // How fast the booms will reset to default positions.
        float MaxLimitDown { set; get; } // how far each piston can extend
        float PistionDownStartPosition { set; get; } // Set the starting extention of the pistions.

        // drills
        private bool DrillsEnabled { set; get; }
        private bool RigSetupComplete { set; get; }

        private bool IsMaxDepth { set; get; }
        
        private bool UpdateDepthDrill { set; get; }

        private string test = "what";

        /// <summary>
        /// Set the default values for the rig.
        /// </summary>
        private void Reset()
        {
            test = "reset";
            StartingAngle = 0.0f;
            RotationAmountInDegrees = 90.0f;
            NumberOfRotations = (int)(360 / RotationAmountInDegrees); // 360 / 15 = 24

            ExtendPistonVelocity = 1.5f;
            HorizontalPistonVelocity = 1.5f;
            RetractPistonVelocity = 2.0f;
            MaxLimitDown = 2.0f; // At each drop extend the pistons by this value
            PistionDownStartPosition = 0.0f; // How extended you need the pistons to be at the start
            UpdateDepthDrill = true;
            RotationSpeed = 0.5f;
            
            if (PistionDownStartPosition > MaxLimitDown)
            {
                MaxLimitDown += PistionDownStartPosition;
            }

            IsMaxDepth = false;
            DrillsEnabled = false;

            _rigStates = RigStates.Stop;
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.SearchBlocksOfName("Drill", _drills, drills => drills is IMyShipDrill);
            GridTerminalSystem.SearchBlocksOfName("Vertical Piston", _verticalPistons,
                verticalPistions => verticalPistions is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Horizontal Piston", _horizontalPistons,
                horizontalPistons => horizontalPistons is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Motor Rotor", _motorRotor,
                motorRotor => motorRotor is IMyMotorStator);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Current Extenstion limit: " + PistionDownStartPosition + " " +test);
            Echo("Current Hor velocity: " + HorizontalPistonVelocity);
            Echo("Rotations Left: " + NumberOfRotations);
            switch (_rigStates)
            {
                case RigStates.Setup:
                    RigSetupComplete = false;
                    SetupTheRig();
                    Echo("Setting up the Rig.");
                    if (RigSetupComplete) // auto start the rig
                    {
                        _rigStates = RigStates.Start;
                    }

                    break;
                case RigStates.Reset:
                    Echo("Resetting the Rig.");
                    Reset();
                    break;
                case RigStates.Start:
                    Echo("The Rig is running.");
                    DrillsEnabled = true;
                    DrillSettings(DrillsEnabled);
                    Start();
                    break;
                case RigStates.Stop:
                    Echo("The Rig is stopped.");
                    RigStop();
                    switch (argument)
                    {
                        case "setup":
                            _rigStates = RigStates.Setup;
                            break;
                        case "reset":
                            _rigStates = RigStates.Reset;
                            break;
                        default:
                            Echo("Please enter setup or stop to proceed");
                            break;
                    }
                    break;
                default:
                    _rigStates = RigStates.Stop;
                    break;
            }
        }

        private void Start()
        {
            switch (_miningStates)
            {
                case MiningStates.horizontal:
                    Echo("Booms moving to target location");
                    MiningHorizontal();
                    break;
                case MiningStates.vertical:
                    Echo("Drill descending");
                    MiningVertical();
                    break;
                case MiningStates.rotate:
                    Echo("Rig rotating to target location");
                    MiningRotate();
                    break;
                default:
                    _miningStates = MiningStates.horizontal;
                    break;
            }
        }

        private void MiningRotate()
        {
            foreach (var stator in _motorRotor.Cast<IMyMotorStator>())
            {
                stator.TargetVelocityRPM = RotationSpeed;

                if (stator.Angle >= stator.UpperLimitRad && RotationSpeed > 0.1f)
                {
                    NumberOfRotations -= 1;
                    _miningStates = MiningStates.horizontal;
                    HorizontalPistonVelocity = HorizontalPistonVelocity * -1;
                }

                if (stator.Angle <= stator.LowerLimitDeg && RotationSpeed < 0.1f)
                {
                    NumberOfRotations -= 1;
                    _miningStates = MiningStates.horizontal;
                    HorizontalPistonVelocity = HorizontalPistonVelocity * -1;
                }
            }
        }

        private void MiningHorizontal()
        {
            foreach (var piston in _horizontalPistons.Cast<IMyPistonBase>())
            {
                piston.Velocity = HorizontalPistonVelocity;
                Echo(" " + Math.Abs(piston.CurrentPosition - piston.HighestPosition));
                
                if (!(Math.Abs(piston.CurrentPosition - piston.HighestPosition) < 0.1f) && piston.Velocity > 0.0f ||
                    !(Math.Abs(piston.CurrentPosition - piston.LowestPosition) < 0.1f) && piston.Velocity < 0.0f) continue;

                piston.Velocity = 0.0f;
                _miningStates = MiningStates.rotate;
                foreach (var stator in _motorRotor.Cast<IMyMotorStator>())
                {
                    if (NumberOfRotations > 0)
                    {
                        UpdateDepthDrill = true;
                        if (RotationSpeed > 0.1f)
                        {
                            stator.UpperLimitDeg += RotationAmountInDegrees;
                        }
                        else if (RotationSpeed < -0.1f)
                        {
                            stator.LowerLimitDeg -= RotationAmountInDegrees;
                        }
                    }
                    else
                    {
                        _miningStates = MiningStates.vertical;
                        if (UpdateDepthDrill)
                        {
                            UpdateDepthDrill = false;
                            MaxLimitDown += 2.0f;
                            RotationSpeed = RotationSpeed * -1;
                            if (stator.TargetVelocityRPM > 0.0f)
                            { 
                                stator.LowerLimitDeg = stator.UpperLimitDeg - RotationAmountInDegrees;
                            }

                            if (stator.TargetVelocityRPM < 0.0f)
                            {
                                stator.UpperLimitDeg = stator.LowerLimitDeg + RotationAmountInDegrees;
                            }
                            
                        }
                        
                        NumberOfRotations = (int)(StartingAngle / RotationAmountInDegrees);
                        if (IsMaxDepth)
                        {
                            _rigStates = RigStates.Stop;
                        }
                    }
                }
            }
        }

        private void MiningVertical()
        {
            foreach (var piston in _verticalPistons.Cast<IMyPistonBase>())
            {
                piston.MaxLimit = MaxLimitDown;
                if (piston.CurrentPosition < MaxLimitDown)
                {
                    piston.Velocity = ExtendPistonVelocity;
                }
                else
                {
                    piston.Velocity = 0.0f;
                    HorizontalPistonVelocity = HorizontalPistonVelocity * -1;
                    _miningStates = MiningStates.horizontal;
                }

                if (Math.Abs(piston.HighestPosition - piston.CurrentPosition) < 0.01f)
                {
                    IsMaxDepth = true;
                }
            }
        }

        private void RigStop()
        {
            const float target = 0.0f;
            DrillsEnabled = false;
            DrillSettings(DrillsEnabled);

            foreach (var piston in _horizontalPistons.Cast<IMyPistonBase>())
            {
                piston.Velocity = target;
            }

            foreach (var piston in _verticalPistons.Cast<IMyPistonBase>())
            {
                piston.Velocity = target;
            }

            foreach (var stator in _motorRotor.Cast<IMyMotorStator>())
            {
                stator.TargetVelocityRPM = target;
            }
        }

        private void SetupTheRig()
        {
            DrillSettings(DrillsEnabled);

            foreach (var pistonBase in _verticalPistons.Cast<IMyPistonBase>())
            {
                pistonBase.MinLimit = PistionDownStartPosition;
                pistonBase.MaxLimit = PistionDownStartPosition;

                if (pistonBase.CurrentPosition < PistionDownStartPosition)
                {
                    pistonBase.Velocity = 0.5f;
                }
                else if (pistonBase.CurrentPosition > PistionDownStartPosition)
                {
                    pistonBase.Velocity = -0.5f;
                }
                else
                {
                    pistonBase.Velocity = 0.0f;
                }
            }

            foreach (var myPiston in _horizontalPistons.Cast<IMyPistonBase>())
            {
                if (myPiston.CurrentPosition > 0.0f)
                {
                    myPiston.Velocity = -0.5f;
                }
                else
                {
                    myPiston.Velocity = 0.0f;
                }
            }

            foreach (var motorStator in _motorRotor.Cast<IMyMotorStator>())
            {
                motorStator.UpperLimitDeg = StartingAngle;
                motorStator.LowerLimitDeg = StartingAngle;
                
                if (motorStator.Angle > motorStator.UpperLimitRad)
                {
                    motorStator.TargetVelocityRPM = RotationSpeed;
                }
                else if (motorStator.Angle < motorStator.UpperLimitRad)
                {
                    motorStator.TargetVelocityRPM = RotationSpeed * -1;
                }
                else
                {
                    motorStator.TargetVelocityRPM = 0.0f;
                }
            }

            var vPiston = new List<IMyPistonBase>(_verticalPistons.Cast<IMyPistonBase>());
            var hPiston = new List<IMyPistonBase>(_horizontalPistons.Cast<IMyPistonBase>());
            var motor = new List<IMyMotorStator>(_motorRotor.Cast<IMyMotorStator>());

            if (vPiston.All(o => o.Velocity == 0.0f) && hPiston.All(o => o.Velocity == 0.0f) &&
                motor.All(o => o.TargetVelocityRPM == 0.0f))
            {
                RigSetupComplete = true;
            }
        }

        private void DrillSettings(bool status = false)
        {
            // Set the Drills to the default state
            foreach (var shipDrill in _drills.Cast<IMyShipDrill>())
            {
                shipDrill.Enabled = status;
            }
        }

        private double ConvertDegToRad(float angle)
        {
            return angle * (Pi / DAngle);
        }
        
    }
}