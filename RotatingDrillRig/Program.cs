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

        /*
         * this script allows for a rotaing mine of 360 degrees.          
         */

        // Degrees to Radians converters
        private const double Pi = 3.1415926535897932384626433832795;
        private const double Radian = 6.28319;
        private const double DAngle = 180.0;


        // Block Lists
        private readonly List<IMyTerminalBlock> _motorRotor = new List<IMyTerminalBlock>();
        private readonly List<IMyTerminalBlock> _verticalPistons = new List<IMyTerminalBlock>();
        private readonly List<IMyTerminalBlock> _horizontalPistons = new List<IMyTerminalBlock>();
        private readonly List<IMyTerminalBlock> _drills = new List<IMyTerminalBlock>();





        // Enum for controlling the rig
        private enum RigState
        {
            Reset,
            Setup,
            Horizontal,
            Rotate,
            Vertical,
            Stop
        }

        public Program()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            /*
             *  1: Reset the rig;
             *  2: Setup the rig;
             *  3: Mine Horizontal
             *  4: Rotate to new desired rotation
             *  5: Repeats steos 3; 4 untill one complet cycle
             *  6: Drill down to next depth level
             *  8: return to setp 5
             */
        }
    }
}
