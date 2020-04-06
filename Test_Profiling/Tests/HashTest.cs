/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2020, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.oM.Structure.Elements;
using BH.oM.Geometry;
using BH.Engine;
using BH.oM.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using BH.Engine.Diffing;
using System.Diagnostics;
using BH.oM.Diffing;
using BH.Engine.Base;
using System.IO;
using Newtonsoft.Json;

namespace Test_Profiling
{
    internal static partial class Diffing_Engine
    {
        public static void HashTest_CostantHash(bool logging = false)
        {
            string testName = MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"\nRunning {testName}");

            Stopwatch sw = Stopwatch.StartNew();


            // Create one bar
            Node startNode = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 0 });
            Node endNode = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 1 });
            Bar bar = BH.Engine.Structure.Create.Bar(startNode, endNode);
            bar.Name = "bar";

            bar = BH.Engine.Diffing.Modify.SetHistoryFragment(bar);

            // Create another bar identical to the first
            Node startNode2 = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 0 });
            Node endNode2 = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 1 });
            Bar bar2 = BH.Engine.Structure.Create.Bar(startNode, endNode);
            bar2.Name = "bar";

            bar2 = BH.Engine.Diffing.Modify.SetHistoryFragment(bar2);

            if (logging) Logger.Log(new List<object>() { bar, bar2 }, "TwoIdenticalBars", LogOptions.ObjectsAndHashes);

            sw.Stop();
            Debug.Assert(bar.FindFragment<HistoryFragment>().CurrentHash == bar2.FindFragment<HistoryFragment>().CurrentHash);

            long timespan = sw.ElapsedMilliseconds;
            Console.WriteLine($"{testName} concluded successfully in {timespan}");
        }


        public static void HashTest_UnchangedObjectsSameHash(bool logging = false)
        {
            string testName = MethodBase.GetCurrentMethod().Name;
            Console.WriteLine($"\nRunning {testName}");

            Stopwatch sw = Stopwatch.StartNew();


            // Create one bar
            Node startNode = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 0 });
            Node endNode = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 1 });
            Bar bar = BH.Engine.Structure.Create.Bar(startNode, endNode);
            bar.Name = "bar";

            bar = BH.Engine.Diffing.Modify.SetHistoryFragment(bar);

            // Think the object is unchanged and passes through another revision.
            // The following sets its HistoryFragment again. PreviousHash and currentHash will have to be the same.
            bar = BH.Engine.Diffing.Modify.SetHistoryFragment(bar);

            sw.Stop();

            // Check that the HistoryFragment's PreviousHash and currentHash are the same:
            Debug.Assert(bar.FindFragment<HistoryFragment>().CurrentHash == bar.FindFragment<HistoryFragment>().PreviousHash);

            long timespan = sw.ElapsedMilliseconds;
            Console.WriteLine($"{testName} concluded successfully in {timespan}");
        }

    }
}
