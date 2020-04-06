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
using System.IO;
using Newtonsoft.Json;

namespace Test_Profiling
{
    internal static partial class Diffing_Engine
    {
        public static void RevisionTest(bool propertyLevelDiffing = true, bool logging = false)
        {
            string testName = MethodBase.GetCurrentMethod().Name;

            Console.WriteLine($"\nRunning {testName}");
            Stopwatch sw = Stopwatch.StartNew();

            DiffConfig diffConfig = new DiffConfig() { EnablePropertyDiffing = propertyLevelDiffing, StoreUnchangedObjects = true };

            // First object set
            List<IBHoMObject> currentObjs_Alessio = new List<IBHoMObject>();

            // Create one bar
            Node startNode = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 0 });
            Node endNode = BH.Engine.Structure.Create.Node(new Point() { X = 0, Y = 0, Z = 1 });
            Bar bar = BH.Engine.Structure.Create.Bar(startNode, endNode);

            bar.Name = "bar";
            currentObjs_Alessio.Add(bar as dynamic);

            // First revision
            Revision revision_Alessio = Create.Revision(currentObjs_Alessio, Guid.NewGuid(), "", "", diffConfig); // this will add the hash fragments to the objects

            if(logging) Logger.Log(revision_Alessio.Objects, "rev1-hashes", LogOptions.HashesOnly);

            // Second object set --> clone of the first
            List<IBHoMObject> currentObjs_Eduardo = revision_Alessio.Objects.Select(obj => BH.Engine.Base.Query.DeepClone(obj) as IBHoMObject).ToList();

            // Add a new bar 
            Bar newBar = BH.Engine.Base.Create.RandomObject(typeof(Bar)) as Bar;
            newBar.Name = "newBar";
            currentObjs_Eduardo.Insert(1, newBar as dynamic);

            // The first Bar will be unchanged at this point.

            // Second revision
            Revision revision_Eduardo = Create.Revision(currentObjs_Eduardo, Guid.NewGuid());

            if (logging) Logger.Log(revision_Alessio.Objects, "rev2-hashes", LogOptions.HashesOnly);


            // -------------------------------------------------------- //

            // Check delta

            Delta delta = BH.Engine.Diffing.Create.Delta(revision_Alessio, revision_Eduardo, diffConfig);

            sw.Stop();

            Debug.Assert(delta.Diff.AddedObjects.Count() == 1, "Incorrect number of object identified as new/ToBeCreated.");
            Debug.Assert(delta.Diff.ModifiedObjects.Count() == 0, "Incorrect number of object identified as modified/ToBeUpdated.");
            Debug.Assert(delta.Diff.UnchangedObjects.Count() == 1, "Incorrect number of object identified as UnchangedObjects.");

            long timespan = sw.ElapsedMilliseconds;
            Console.WriteLine($"{testName} concluded successfully in {timespan}");
        }

    }
}
