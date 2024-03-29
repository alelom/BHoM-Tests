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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BH.Engine.Base;
using BH.oM.Geometry;

namespace Tests
{
    class Program
    {
        static void Main(string[] args = null)
        {

            Bar bar = new Bar();
            IGeometry geom = bar.IGeometry3D();

            if (geom == null)
                return;



            Console.WriteLine("Press `Enter` to repeat tests. `Esc` to exit. Any other key to continue on Profiling.");

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Enter)
                Main();

            if (keyInfo.Key == ConsoleKey.Escape)
                return;


            Console.WriteLine("Press `Enter` to repeat all / any other key to close.");

            keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.Enter)
                Main();

        }
    }
}
