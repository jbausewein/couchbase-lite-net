// 
//  Activate.cs
// 
//  Copyright (c) 2017 Couchbase, Inc All rights reserved.
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//  http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Couchbase.Lite.DI;
using Couchbase.Lite.Logging;
using Couchbase.Lite.Util;

using JetBrains.Annotations;

using LiteCore.Interop;

namespace Couchbase.Lite.Support
{
    /// <summary>
    /// The .NET Desktop support class
    /// </summary>
    public static class Unity
    {
        #region Variables

        private static AtomicBool _Activated;
        private static AtomicBool _Ready;

        #endregion

        #region Public Methods

        /// <summary>
        /// Activates the support classes for .NET Core / .NET Framework
        /// </summary>
        public static void Activate()
        {
            if (_Activated.Set(true)) {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
#if NETSTANDARD2_0 || NET461
                var codeBase = Path.GetDirectoryName(typeof(Unity).GetTypeInfo().Assembly.Location);
                if (codeBase == null) {
                    throw new DllNotFoundException(
                        "Couldn't find directory of the loaded support assembly, very weird!");
                }
#else
                var codeBase = AppContext.BaseDirectory;
#endif

                var architecture = IntPtr.Size == 4
                    ? "x86"
                    : "x64";
                
                var nugetBase = codeBase;
                for (int i = 0; i < 2; i++) {
                    nugetBase = Path.GetDirectoryName(nugetBase);
                }

                var dllPath = Path.Combine(codeBase, architecture, "LiteCore.dll");
                var dllPathAsp = Path.Combine(codeBase, "bin", architecture, "LiteCore.dll");
                var dllPathNuget =
                    Path.Combine(nugetBase, "runtimes", $"win7-{architecture}", "native", "LiteCore.dll");
                var foundPath = default(string);
                foreach (var path in new[] { dllPathNuget, dllPath, dllPathAsp }) {
                    foundPath = File.Exists(path) ? path : null;
                    if (foundPath != null) {
                        break;
                    }
                }

                if (foundPath == null) {
                    throw new DllNotFoundException("Could not find LiteCore.dll!  Nothing is going to work!\r\n" +
                                                   "Tried searching in:\r\n" +
                                                   $"{dllPathNuget}\r\n" +
                                                   $"{dllPath}\r\n" +
                                                   $"{dllPathAsp}\r\n");
                }

                const uint loadWithAlteredSearchPath = 8;
                var ptr = LoadLibraryEx(foundPath, IntPtr.Zero, loadWithAlteredSearchPath);
                if (ptr == IntPtr.Zero) {
                    throw new BadImageFormatException("Could not load LiteCore.dll!  Nothing is going to work!\r\n" +
                                                      $"LiteCore found in: ${foundPath}");
                }
            }

            Service.AutoRegister(typeof(Unity).GetTypeInfo().Assembly);
            Service.Register<ILiteCore>(new LiteCoreImpl());
            Service.Register<ILiteCoreRaw>(new LiteCoreRawImpl());
            _Ready.Set(true);
        }

        public static void EnableTextLogging(string directoryPath)
        {
            Log.EnableTextLogging(new FileLogger(directoryPath));
        }

        public static void ExecuteTasks()
        {
            if (_Ready)
            {
                Native.c4_executeTasks();
            }
        }

        #endregion

        #region Private Methods

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        #endregion
    }
}