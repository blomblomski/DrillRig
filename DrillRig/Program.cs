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
        readonly List<IMyTerminalBlock> drills = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> vPistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> hPistons = new List<IMyTerminalBlock>();
        List<IMyTerminalBlock> rotorBase = new List<IMyTerminalBlock>();
        

        float extendVelocity = 0.1f;
        float retractVelocity = -1f;


        float rotateAmount = 12.0f;
        float rotorBaseStartAngle = 0.0f;
        float maxAngle = 360.0f;

        float maxLimitDownPiston = 1.5f;
        float HorizontalMaxMin { set; get; }

        bool drilling = false;
        bool drillingDown = false;
        bool horizontalDrillingSet = false;
        bool miningComplete = false;        

        string rotatorOutput = "";
        string angleOutput = "";
        string maxLimit = "";

        bool DrillsEnabled { set; get; }
        float PistonsDownStartPos { set; get; }

        enum operations
        {
            restart,
            mining,
            stop
        }

        operations op = operations.stop;

        enum mining
        {
            down,
            horizontal,
            rotate,
            up,
        }

        mining mine = mining.down;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            //GridTerminalSystem.GetBlocksOfType<IMyShipDrill>(shipDrills);
            //GridTerminalSystem.SearchBlocksOfName(mineName + "drill", shipDrills, shipDrills => shipDrills is IMyShipDrill);
            GridTerminalSystem.SearchBlocksOfName("drill", drills, drills => drills is IMyShipDrill);
            GridTerminalSystem.SearchBlocksOfName("Vertical Piston Down", vPistons, vPistons => vPistons is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Horizontal Piston", hPistons, hPistons => hPistons is IMyPistonBase);
            GridTerminalSystem.SearchBlocksOfName("Rotor Base", rotorBase, rotorBase => rotorBase is IMyMotorStator);

            PistonsDownStartPos = 1.5f; // set the start position of the vertical down pistons.
            HorizontalMaxMin = 10.0f;
            maxLimitDownPiston += PistonsDownStartPos;
            maxAngle -= rotateAmount;

            if (vPistons.Count() > 0)
            {
                foreach (IMyPistonBase pistonBase in vPistons)
                {
                    if (!checkPistonPos(PistonsDownStartPos, pistonBase))
                    {
                        op = operations.restart;
                    }
                }
            }
            else
            {
                Echo("No Vertical Pistons");
                op = operations.stop;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="updateSource"></param>
        public void Main(string argument, UpdateType updateSource)
        {   
            switch (op)
            {
                case operations.mining:
                    Echo("Current State: Mining");
                    Mining();
                    break;
                case operations.restart:
                    Echo("Current State: Restart");
                    restart();
                    break;
                case operations.stop:
                    Echo("Current State: Stopped");
                    Echo("Max Limit: " + maxLimit);
                    if (argument == "mine")
                    {
                        op = operations.mining;
                    }
                    break;
                default:
                    break;

            }
        }


        public void Mining()
        {
            DrillsEnabled = true;
            setDrills(DrillsEnabled);
            Echo("Max Limit: " + maxLimit);

            // drop
            // extend
            // rotate
            switch (mine)
            {
                case mining.down:
                    Echo("Mining State: Down");                    
                    MiningDown();
                    break;
                case mining.up:
                    break;
                case mining.horizontal:
                    Echo("Mining State: Horizontal");
                    MiningHorizontal();
                    break;
                case mining.rotate:
                    Echo("Mining State: Rotating");
                    Echo(rotatorOutput);
                    Echo(angleOutput);
                    MiningRotate();
                    break;
                default:
                    Echo("No Mining State Selectable");
                    break;
            }
        }

        public void MiningRotate()
        {
            // set new angle
            foreach (IMyMotorStator stator in rotorBase)
            {

                angleOutput = stator.Angle.ToString();
                rotatorOutput = stator.UpperLimitRad.ToString();
                if (stator.Angle >= stator.UpperLimitRad)
                {
                    mine = mining.horizontal;
                }                
                if(stator.Angle > maxAngle)
                {
                    mine = mining.down;                    
                    if(maxLimitDownPiston >= 10)
                    {
                        op = operations.stop;
                    }
                    maxLimitDownPiston = Math.Min(maxLimitDownPiston += 1.0f, 10.0f);
                }
            }

        }

        public void MiningHorizontal()
        {
            if (!horizontalDrillingSet)
            {
                foreach (IMyPistonBase pistonBase in hPistons)
                {
                    if (pistonBase.Velocity < 0.0f)
                    {
                        pistonBase.Velocity = 0.1f;
                    }
                    else
                    {
                        pistonBase.Velocity = -0.1f;
                    }
                    
                    
                }

                horizontalDrillingSet = !horizontalDrillingSet;
            }

            if (horizontalDrillingSet)
            {
                foreach (IMyPistonBase pistonBase in hPistons)
                {
                    if(HorizontalMaxMin == pistonBase.CurrentPosition)
                    {
                        mine = mining.rotate;                        
                        horizontalDrillingSet = !horizontalDrillingSet;
                        if(HorizontalMaxMin <= 5.0f)
                        {
                            HorizontalMaxMin = 10.0f;
                        }
                        else
                        {
                            HorizontalMaxMin = 0.0f;
                        }

                        // set new angle
                        foreach(IMyMotorStator stator in rotorBase)
                        {
                            stator.UpperLimitDeg += rotateAmount;
                        }
                    }
                }
            }
        }

        public void MiningDown()
        {
            foreach (IMyPistonBase pistonBase in vPistons)
            {
                pistonBase.MaxLimit = maxLimitDownPiston;

                if (checkPistonPos(pistonBase.MaxLimit, pistonBase))
                {
                    drillingDown = false;
                    pistonBase.Velocity = 0.0f;
                    mine = mining.horizontal;
                }
                else if (pistonBase.CurrentPosition > pistonBase.MaxLimit)
                {
                    pistonBase.Velocity = retractVelocity;
                }
                else
                {
                    drillingDown = true;
                    pistonBase.Velocity = extendVelocity;
                }
            } // end of foreach
        }

        public bool checkPistonPos(float checkPos, IMyPistonBase pistonBase)
        {
            if (pistonBase.CurrentPosition == checkPos)
            {
                return true;
            }

            return false;
        }

        public void restart()
        {
            foreach (IMyPistonBase pistonBase in vPistons)            {
                
                maxLimit = maxLimitDownPiston.ToString();

                if (pistonBase.CurrentPosition >= PistonsDownStartPos)
                {
                    pistonBase.Velocity = retractVelocity;
                    pistonBase.MinLimit = PistonsDownStartPos;
                }
                else
                {
                    pistonBase.Velocity = 0.0f;
                }
                pistonBase.MaxLimit = PistonsDownStartPos;
            }

            foreach (IMyPistonBase pistonBase in hPistons)
            {
                pistonBase.Velocity = retractVelocity;                
            }

            foreach(IMyMotorStator motorRotor in rotorBase)
            {
                motorRotor.UpperLimitDeg = rotorBaseStartAngle;
                motorRotor.LowerLimitDeg = rotorBaseStartAngle;
                motorRotor.TargetVelocityRPM = extendVelocity;
            }

            if (DrillsEnabled)
            {
                DrillsEnabled = false;
                foreach (IMyShipDrill drill in drills)
                {
                    drill.Enabled = DrillsEnabled;
                }

            }

            List<IMyPistonBase> pistonDown = new List<IMyPistonBase>(vPistons.Cast<IMyPistonBase>());
            List<IMyPistonBase> pistonHor = new List<IMyPistonBase>(hPistons.Cast<IMyPistonBase>());
            if (pistonDown.Any(o => o.CurrentPosition >= PistonsDownStartPos) || pistonHor.Any(o => o.CurrentPosition != 0.0f))
            {
                Echo("Restart Not complete");
            }
            else
            {
                op = operations.stop;
            }
        }

        public void setDrills(bool state)
        {
            // Turn on the drills.
            foreach (IMyShipDrill drill in drills)
            {
                if (!drill.Enabled) drill.Enabled = state;

            }
        }
    }
}
