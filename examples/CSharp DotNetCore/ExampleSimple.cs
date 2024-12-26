// Copyright (c) libplctag.NET contributors
// https://github.com/libplctag/libplctag.NET
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Alpiste.Lib;
using libplctag;
using System;
using System.Reflection;

namespace CSharpDotNetCore
{
    class ExampleSimple
    {
        public static void Run()
        {
            // Example tag configuration for a global DINT tag in an Allen-Bradley CompactLogix/ControlLogix PLC
               /*    var myTag = new Tag()
                     {
                         Name = /*"@udt/707", // "@udt/459", // "@udt/4045", //*//*"Prueba_String20", //"SomeDINT",
                         Gateway = "10.12.68.155", //"10.12.76.118", //"10.10.10.10",
                         Path = "1,0",
                         PlcType = PlcType.ControlLogix,
                         Protocol = Protocol.ab_eip
                     };

                     // Read the value from the PLC and output to console
                     myTag.Read();
        /*             int originalValue = myTag.GetInt32(0);
                     Console.WriteLine($"Original value: {originalValue}");

                     // Write a new value to the PLC, then read it back, and output to console
                     int updatedValue = originalValue + 1; //1234;
                     myTag.SetInt32(0, updatedValue);
                     myTag.Write();
                     Console.WriteLine($"Updated value: {updatedValue}");
                 /**/    //myTag.Dispose();
                         //      System.Threading.Thread.Sleep(2000);

            /*  do
              {
                   PlcTag plcTag = new PlcTag(1000);
              } while (false);  /**/
            /*     do {

                 PlcTag plcTag = new Alpiste.Protocol.AB.AbTag("Prueba_Dint", "10.12.68.155" );
                        var result = plcTag.syncRead();
                     Console.WriteLine($"Original value: {result}");

                     //plcTag.Dispose();
                     plcTag = null;
                 } while (false);  
                 System.Threading.Thread.Sleep(2000);/**/

            Console.WriteLine("Prueba_Bool");
            PlcTag Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_Bool"/* "String20"*/, "10.12.68.155");
            var res = Tag1.syncRead();

            System.Threading.Thread.Sleep(2000);
            /*     Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_Sint", "10.12.68.155");
                 res = Tag1.syncRead();

                 System.Threading.Thread.Sleep(2000);
                 Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_Int", "10.12.68.155");
                 res = Tag1.syncRead();

                 System.Threading.Thread.Sleep(2000);
                 Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_Lint", "10.12.68.155");
                 res = Tag1.syncRead();

                 System.Threading.Thread.Sleep(2000);
                 Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_Real", "10.12.68.155");
                 res = Tag1.syncRead();


                 System.Threading.Thread.Sleep(2000);
            */
            Console.WriteLine("Prueba_String");

            Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_String", "10.12.68.155");
            res = Tag1.syncRead();

            Console.WriteLine("Prueba_String20");

            System.Threading.Thread.Sleep(2000);
            Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_String20", "10.12.68.155");
            res = Tag1.syncRead();

            Console.WriteLine("Prueba_Srting2");
            System.Threading.Thread.Sleep(2000);
            Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_String2", "10.12.68.155");
            res = Tag1.syncRead();

            Console.WriteLine("Prueba_Srting_2");
            System.Threading.Thread.Sleep(2000);
            Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_String_2", "10.12.68.155");
            res = Tag1.syncRead();

            Console.WriteLine("Prueba_Timer");
            System.Threading.Thread.Sleep(2000);
            Tag1 = new Alpiste.Protocol.AB.AbTag("Prueba_Timer", "10.12.68.155");
            res = Tag1.syncRead();

            GC.Collect();
 /**/


        }
    }
}
