﻿// Copyright (c) libplctag.NET contributors
// https://github.com/libplctag/libplctag.NET
//
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using CSharp_DotNetCore;
using System;

namespace CSharpDotNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            ExampleTagDynamic.Run();
            //ExampleSimple.Run();
            Console.ReadKey();
        }
    }
}