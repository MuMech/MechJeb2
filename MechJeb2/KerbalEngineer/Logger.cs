// 
//     Kerbal Engineer Redux
// 
//     Copyright (C) 2014 CYBUTEK
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEngine;

#endregion

namespace KerbalEngineer
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class Logger : MonoBehaviour
    {
        #region Constants

        private static readonly string fileName;
        private static readonly AssemblyName assemblyName;

        #endregion

        #region Fields

        private static readonly List<string[]> messages = new List<string[]>();

        #endregion

        #region Initialisation

        static Logger()
        {
            assemblyName = Assembly.GetExecutingAssembly().GetName();
            fileName = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "log");
            File.Delete(fileName);

            lock (messages)
            {
                messages.Add(new[] {"Executing: " + assemblyName.Name + " - " + assemblyName.Version});
                messages.Add(new[] {"Assembly: " + Assembly.GetExecutingAssembly().Location});
            }
            Blank();
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        #endregion

        #region Printing

        public static void Blank()
        {
            lock (messages)
            {
                messages.Add(new string[] {});
            }
        }

        public static void Log(object obj)
        {
            lock (messages)
            {
                try
                {
                    if (obj is IEnumerable)
                    {
                        messages.Add(new[] {"Log " + DateTime.Now.TimeOfDay, obj.ToString()});
                        foreach (var o in obj as IEnumerable)
                        {
                            messages.Add(new[] {"\t", o.ToString()});
                        }
                    }
                    else
                    {
                        messages.Add(new[] {"Log " + DateTime.Now.TimeOfDay, obj.ToString()});
                    }
                }
                catch (Exception ex)
                {
                    Exception(ex);
                }
            }
        }

        public static void Log(string name, object obj)
        {
            lock (messages)
            {
                try
                {
                    if (obj is IEnumerable)
                    {
                        messages.Add(new[] {"Log " + DateTime.Now.TimeOfDay, name});
                        foreach (var o in obj as IEnumerable)
                        {
                            messages.Add(new[] {"\t", o.ToString()});
                        }
                    }
                    else
                    {
                        messages.Add(new[] {"Log " + DateTime.Now.TimeOfDay, obj.ToString()});
                    }
                }
                catch (Exception ex)
                {
                    Exception(ex);
                }
            }
        }

        public static void Log(string message)
        {
            lock (messages)
            {
                messages.Add(new[] {"Log " + DateTime.Now.TimeOfDay, message});
            }
        }

        public static void Warning(string message)
        {
            lock (messages)
            {
                messages.Add(new[] {"Warning " + DateTime.Now.TimeOfDay, message});
            }
        }

        public static void Error(string message)
        {
            lock (messages)
            {
                messages.Add(new[] {"Error " + DateTime.Now.TimeOfDay, message});
            }
        }

        public static void Exception(Exception ex)
        {
            lock (messages)
            {
                messages.Add(new[] {"Exception " + DateTime.Now.TimeOfDay, ex.Message});
                messages.Add(new[] {string.Empty, ex.StackTrace});
                Blank();
            }
        }

        public static void Exception(Exception ex, string location)
        {
            lock (messages)
            {
                messages.Add(new[] {"Exception " + DateTime.Now.TimeOfDay, location + " // " + ex.Message});
                messages.Add(new[] {string.Empty, ex.StackTrace});
                Blank();
            }
        }

        #endregion

        #region Flushing

        public static void Flush()
        {
            lock (messages)
            {
                if (messages.Count > 0)
                {
                    using (var file = File.AppendText(fileName))
                    {
                        foreach (var message in messages)
                        {
                            file.WriteLine(message.Length > 0 ? message.Length > 1 ? "[" + message[0] + "]: " + message[1] : message[0] : string.Empty);
                            if (message.Length > 0)
                            {
                                print(message.Length > 1 ? assemblyName.Name + " -> " + message[1] : assemblyName.Name + " -> " + message[0]);
                            }
                        }
                    }
                    messages.Clear();
                }
            }
        }

        private void LateUpdate()
        {
            Flush();
        }

        #endregion

        #region Destruction

        private void OnDestroy()
        {
            Flush();
        }

        ~Logger()
        {
            Flush();
        }

        #endregion
    }
}