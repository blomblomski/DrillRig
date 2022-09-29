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
        readonly List<IMyTerminalBlock> _verticalPistions = new List<IMyTerminalBlock>();
        readonly List<IMyTerminalBlock> _horizontalPistons = new List<IMyTerminalBlock>();        
        readonly List<IMyTerminalBlock> _motorRotor = new List<IMyTerminalBlock>();

        private enum RigStates
        {
            reset,
            start,
            stop,
            setup
        }

        private RigStates _rigStates = RigStates.stop;

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
        float RoationAmountInDegrees { set; get; } // how much the rig will rotate per rotation.

        // pistions
        float ExtendPistionVelocity { set; get; } // How fast the vertical booms extend.  More booms reduce the speed
        float RetractPistionVelocity { set; get; } // How fast the booms will reset to default positions.
        float MaxLimitDown { set; get; } // how far each pistion can extend
        float PistionDownStartPosition { set; get; } // Set the starting extention of the pistions.

        // drills
        private bool DrillsEnabled { set; get; }
        private bool RigSetupComplete { set; get; }

        /// <summary>
        /// Set the default values for the rig.
        /// </summary>
        private void Reset()
        {
            StartingAngle = 0.0f;
            RoationAmountInDegrees = 15.0f;
            NumberOfRotations = (int)(360 / RoationAmountInDegrees); // 360 / 15 = 24

            ExtendPistionVelocity = 0.3f;
            RetractPistionVelocity = 2.0f;
            MaxLimitDown = 2.0f; // At each drop extend the pistons by this value
            PistionDownStartPosition = 0.0f; // How extended dp you need the pistions to be at the start

            if(PistionDownStartPosition > MaxLimitDown)
            {
                MaxLimitDown += PistionDownStartPosition;
            }
            
            DrillsEnabled = false;

            _rigStates = RigStates.stop;
        }


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GridTerminalSystem.SearchBlocksOfName("Drill", _drills, drills => drills is IMyShipDrill);
            GridTerminalSystem.SearchBlocksOfName("Vertical Piston", _verticalPistions, verticalPistions => verticalPistions is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Horizontal Piston", _horizontalPistons, horizontalPistons => horizontalPistons is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Motor Rotor", _motorRotor, motorRotor => motorRotor is IMyMotorStator);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (_rigStates)
            {
                case RigStates.setup:
                    RigSetupComplete = false;
                    SetupTheRig();
                    Echo("Setting up the Rig.");
                    if (RigSetupComplete) // auto start the rig
                    {
                        _rigStates = RigStates.start;
                    }
                    break;
                case RigStates.reset:
                    Echo("Resetting the Rig.");
                    Reset();
                    switch (argument)
                    {
                        case "setup":
                            _rigStates = RigStates.setup;
                            break;
                        case "stop":
                            _rigStates = RigStates.stop;
                            break;
                        default:
                            Echo("Please enter setup or stop to proceed");
                            break;
                    }
                    break;
                case RigStates.start:
                    Echo("The Rig is running.");
                    Start();
                    break;
                case RigStates.stop:
                    Echo("The Rig is stopped.");
                    RigStop();
                    break;
                default:
                    _rigStates = RigStates.stop;
                    break;
            }
        }

        private void Start()
        {
            switch (_miningStates)
            {
                case MiningStates.horizontal:
                    break;
                case MiningStates.vertical:
                    Echo("Drill descending");
                    MiningVertical();
                    break;
                case MiningStates.rotate:
                    break;
                default:
                    _miningStates = MiningStates.horizontal;
                    break;
            }
        }

        private void MiningHorizontal()
        {
            foreach (var piston in _horizontalPistons.Cast<IMyPistonBase>())
            {
                if (Math.Abs(piston.CurrentPosition - piston.HighestPosition) < 0.1f || Math.Abs(piston.CurrentPosition - piston.LowestPosition) < 0.01f)
                {
                    //piston.Velocity = piston.Velocity * -1;
                    _miningStates = MiningStates.rotate;
                    foreach (var stator in _motorRotor.Cast<IMyMotorStator>())
                    {
                        if (NumberOfRotations > 0)
                        { 
                            stator.UpperLimitDeg += RoationAmountInDegrees;
                        }
                        else
                        {
                            _rigStates = RigStates.stop;
                        }
                        
                    }
                }
            }
        }

        private void MiningVertical()
        {
            foreach (var piston in _verticalPistions.Cast<IMyPistonBase>())
            {
                piston.MaxLimit = MaxLimitDown;
                if (piston.CurrentPosition < MaxLimitDown)
                {
                    piston.Velocity = ExtendPistionVelocity;
                }
                else
                {
                    piston.Velocity = 0.0f;
                    _miningStates = MiningStates.horizontal;
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

            foreach (var piston in _verticalPistions.Cast<IMyPistonBase>())
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

            foreach (var pistonBase in _verticalPistions.Cast<IMyPistonBase>())
            {
                pistonBase.MinLimit = PistionDownStartPosition;
                pistonBase.MaxLimit = PistionDownStartPosition;

                if(pistonBase.CurrentPosition < PistionDownStartPosition)
                {
                    pistonBase.Velocity = 0.5f;
                }
                else if(pistonBase.CurrentPosition > PistionDownStartPosition)
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
                if(myPiston.CurrentPosition > 0.0f)
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
                if (motorStator.Angle > motorStator.UpperLimitRad)
                {
                    motorStator.TargetVelocityRPM = -0.5f;
                }
                else if(motorStator.Angle < motorStator.UpperLimitRad)
                {
                    motorStator.TargetVelocityRPM = 0.5f;
                }
                else
                {
                    motorStator.TargetVelocityRPM = 0.0f;
                }
            }

            var vPiston = new List<IMyPistonBase>(_verticalPistions.Cast<IMyPistonBase>());
            var hPiston = new List<IMyPistonBase>(_horizontalPistons.Cast<IMyPistonBase>());
            var motor = new List<IMyMotorStator>(_motorRotor.Cast<IMyMotorStator>());

            if(vPiston.All(o => o.Velocity == 0.0f) && hPiston.All(o => o.Velocity == 0.0f) && motor.All(o => o.TargetVelocityRPM == 0.0f))
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
