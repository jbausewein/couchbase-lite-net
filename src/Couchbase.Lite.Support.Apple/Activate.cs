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
using System.Runtime.InteropServices;
using Couchbase.Lite.DI;
using Couchbase.Lite.Logging;
using Couchbase.Lite.Util;
using LiteCore;
using LiteCore.Interop;
using ObjCRuntime;

namespace LiteCore.Interop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void C4UnityCallback(C4Socket socket, C4Address address, C4Slice options, C4LogDomain logDomain, C4Replicator replicator);
}

namespace Couchbase.Lite.Support
{
    /// <summary>
    /// Support classes for Xamarin iOS
    /// </summary>
    public static class Unity
    {
        #region Variables

        private static AtomicBool _Activated = false;
        private static AtomicBool _Ready;
        private static AtomicBool _Hack;

        #endregion

        #region Public Methods

        /// <summary>
        /// Activates the Xamarin iOS specific support classes
        /// </summary>
        public static void Activate(string path)
        {
            if (_Activated.Set(true))
            {
                return;
            }

            Console.WriteLine("Loading support items");
            Service.Register<IDefaultDirectoryResolver>(new DefaultDirectoryResolver(path));
            Service.Register<ILiteCore>(new LiteCoreImpl());
            Service.Register<ILiteCoreRaw>(new LiteCoreRawImpl());

            // hack for unity il2cpp 
            if (_Hack)
            {
                Hack(Callback);
            }

            _Ready.Set(true);
        }

        public static void EnableTextLogging(string directoryPath)
        {
            Log.EnableTextLogging(new iOSDefaultLogger());
        }

        public static void ExecuteTasks()
        {
            if (_Ready)
            {
                Native.c4_executeTasks();
            }
        }

        #endregion

        #region Unity Hack

        [MonoPInvokeCallback(typeof(C4UnityCallback))]
        private static void Callback(C4Socket socket, C4Address address, C4Slice options, C4LogDomain logDomain, C4Replicator replicator)
        {
        }

        [DllImport(Constants.DllNameIos, CallingConvention = CallingConvention.Cdecl)]
        private static extern void Hack(C4UnityCallback callback);

        #endregion
    }
}
