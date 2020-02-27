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
using System.IO;
using System.Security.Cryptography;
using BH.Engine.Serialiser;
using BH.oM.Diffing;
using System.Diagnostics;

namespace Test_Profiling
{
    internal static partial class Diffing_Engine
    {
        public static void Profiling01()
        {
            Console.WriteLine("Running Diffing_Engine Profiling01");

            string path = @"C:\temp\Diffing_Engine-ProfilingTask01.txt";
            File.Delete(path);

            List<int> numberOfObjects = new List<int>() { 10, 100, 1000, 5000, 10000 }; //, 12250, 15000, 17250, 20000, 25000, 30000 };

            bool enablePropertyLevelDiff = false;
            for (int b = 0; b < 2; b++)
            {
                DiffConfig diffconfig = new DiffConfig() { EnablePropertyDiffing = enablePropertyLevelDiff };

                numberOfObjects.ForEach(i =>
                    ProfilingTask(i, diffconfig, path));

                enablePropertyLevelDiff = !enablePropertyLevelDiff;
            }

            Console.WriteLine("Profiling01 concluded.");
        }

        public static void ProfilingTask(int totalObjs, DiffConfig diffconfig, string path = null)
        {
            string introMessage = $"Profiling diffing for {totalObjs} randomly generated and modified objects.";
            introMessage += diffconfig.EnablePropertyDiffing ? " Includes collection-level and property-level diffing." : " Only collection-level diffing.";
            Console.WriteLine(introMessage);

            if (path != null)
            {
                string fName = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);
                fName += diffconfig.EnablePropertyDiffing ? "_propLevel" : "_onlyCollLevel";
                path = Path.Combine(Path.GetDirectoryName(path), fName + ext);
            }

            // Generate random objects
            List<IBHoMObject> currentObjs = Utils.GenerateRandomObjects(typeof(Bar), totalObjs);

            // Create Stream. This assigns the Hashes.
            Revision revision = BH.Engine.Diffing.Create.Revision(currentObjs, diffconfig, "");

            // Modify randomly half the total of objects.
            var readObjs = revision.Objects.Cast<IBHoMObject>().ToList();

            var allIdxs = Enumerable.Range(0, currentObjs.Count).ToList();
            var randIdxs = allIdxs.OrderBy(g => Guid.NewGuid()).Take(currentObjs.Count / 2);
            var remainingIdx = allIdxs.Except(randIdxs).ToList();

            List<IBHoMObject> changedList = randIdxs.Select(idx => readObjs.ElementAt(idx)).ToList();
            changedList.ForEach(obj => obj.Name = "ModifiedName");
            changedList.AddRange(remainingIdx.Select(idx => readObjs.ElementAt(idx)).Cast<IBHoMObject>().ToList());

            // Update stream revision
            Revision updatedRevision = BH.Engine.Diffing.Create.Revision(revision, changedList);

            // Actual diffing
            var timer = new Stopwatch();
            timer.Start();

            Diff diff = BH.Engine.Diffing.Compute.Diffing(revision, updatedRevision, diffconfig);

            timer.Stop();

            var ms = timer.ElapsedMilliseconds;

            string endMessage = $"Total elapsed milliseconds: {ms}";
            Console.WriteLine(endMessage);

            Debug.Assert(diff.ModifiedObjects.Count == totalObjs / 2, "Diffing didn't work.");

            if (path != null)
            {
                System.IO.File.AppendAllText(path, Environment.NewLine + introMessage + Environment.NewLine + endMessage);
                Console.WriteLine($"Results appended in {path}");
            }
        }

    }
}
