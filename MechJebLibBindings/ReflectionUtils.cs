/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace MechJebLibBindings
{
    public static class ReflectionUtils
    {
        public static readonly bool IsLoadedProceduralFairing;
        public static readonly bool IsLoadedRealFuels;
        public static readonly bool IsLoadedRealismOverhaul;
        public static readonly bool IsLoadedPrincipia;
        public static readonly bool IsLoadedFAR;
        public static readonly bool IsLoadedRP0;

        // the assemblies already logged as missing
        private static readonly HashSet<string> _missingAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static ReflectionUtils()
        {
            IsLoadedProceduralFairing = IsAssemblyLoaded("ProceduralFairings");
            IsLoadedRealFuels = IsAssemblyLoaded("RealFuels");
            IsLoadedPrincipia = IsAssemblyLoaded("ksp_plugin_adapter");
            IsLoadedFAR = IsAssemblyLoaded("FerramAerospaceResearch");
            IsLoadedRealismOverhaul = IsAssemblyLoaded("RealismOverhaul");
            IsLoadedRP0 = IsAssemblyLoaded("RP-0");
        }

        public static bool IsAssemblyLoaded(string assemblyName)
        {
            foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
                if (string.Equals(loaded.name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public static AssemblyContext Assembly(string assemblyString)
        {
            string assemblyName = "";

            foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    if (string.Equals(loaded.name, assemblyString, StringComparison.OrdinalIgnoreCase))
                        assemblyName = loaded.assembly.FullName;
                }
                catch (Exception)
                {
                    // ignore busted assemblies
                }
            }

            // a mod that isn't installed is the normal case: it is logged once per assembly, and the lookups of its types and
            // members say nothing (on a stock install they logged 63 lines at start-up, for RealFuels, RP-0, FAR and Procedural
            // Fairings). a lookup that fails in an installed mod is still logged each time.
            if (assemblyName == "")
            {
                bool first;
                lock (_missingAssemblies)
                    first = _missingAssemblies.Add(assemblyString);

                if (first)
                    Debug.Log("[MechJeb] ReflectionUtils: could not find assembly " + assemblyString + ", so its lookups are skipped");
            }

            return new AssemblyContext(assemblyName);
        }

        public class AssemblyContext
        {
            private readonly string _assemblyName;

            public AssemblyContext(string assemblyName)
            {
                _assemblyName = assemblyName;
            }

            public bool IsLoaded => _assemblyName != "";

            public ClassContext Class(string className)
            {
                if (!IsLoaded)
                    return new ClassContext(_assemblyName, className, null, false);

                Type? type = null;

                try
                {
                    type = Type.GetType(className + ", " + _assemblyName);
                }
                catch (Exception e)
                {
                    Debug.Log($"[MechJeb] ReflectionUtils: exception looking up type {className}, {_assemblyName}: {e.Message}");
                }

                if (type == null)
                    Debug.Log("[MechJeb] ReflectionUtils: could not find type  " + className + ", " + _assemblyName);

                return new ClassContext(_assemblyName, className, type);
            }
        }

        public class ClassContext
        {
            private readonly string _assemblyName;
            private readonly string _className;
            private readonly Type? _type;

            // false for a mod that isn't installed: its members are not looked up and not logged as missing
            private readonly bool _assemblyLoaded;
            public bool IsValid => _type != null;

            public ClassContext(string assemblyName, string className, Type? type, bool assemblyLoaded = true)
            {
                _assemblyName = assemblyName;
                _className = className;
                _type = type;
                _assemblyLoaded = assemblyLoaded;
            }

            public MethodContext Method(string methodName, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, Type[]? args = null)
            {
                MethodInfo? method = null;

                try
                {
                    if (_type != null)
                        method = args == null ? _type.GetMethod(methodName, flags) : _type.GetMethod(methodName, flags, null, args, null);
                }
                catch (Exception e)
                {
                    Debug.Log($"[MechJeb] ReflectionUtils: exception looking up method {methodName} in {_className}, {_assemblyName}: {e.Message}");
                }

                if (method == null && _assemblyLoaded)
                    Debug.Log($"[MechJeb] ReflectionUtils: could not find method {methodName} in {_className}, {_assemblyName}");

                return new MethodContext(_assemblyName, _className, methodName, method);
            }

            public FieldContext Field(string fieldName, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            {
                FieldInfo? field = null;

                try
                {
                    if (_type != null)
                        field = _type.GetField(fieldName, flags);
                }
                catch (Exception e)
                {
                    Debug.Log($"[MechJeb] ReflectionUtils: exception looking up field {fieldName} in {_className}, {_assemblyName}: {e.Message}");
                }

                if (field == null && _assemblyLoaded)
                    Debug.Log($"[MechJeb] ReflectionUtils: could not find field {fieldName} in {_className}, {_assemblyName}");

                return new FieldContext(_assemblyName, _className, fieldName, field);
            }

            public bool IsInstance(object o) => _type != null && _type.IsInstanceOfType(o);
        }

        public class MethodContext
        {
            private readonly string _assemblyName;
            private readonly string _className;
            private readonly string _methodName;
            private readonly MethodInfo? _method;
            public bool IsValid => _method != null;

            public MethodContext(string assemblyName, string className, string methodName, MethodInfo? method)
            {
                _assemblyName = assemblyName;
                _className = className;
                _methodName = methodName;
                _method = method;
            }

            public MethodInfo? MethodInfo() => _method;

            public object Invoke(object? o, object[] parameters)
            {
                if (_method == null)
                    throw new Exception($"MechJeb reflection bug: method {_methodName} in {_className}, {_assemblyName} is null");

                return _method.Invoke(o, parameters);
            }

            public DelegateContext<T> StaticDelegate<T>() where T : Delegate => new DelegateContext<T>(_assemblyName, _className, _methodName, _method);
        }

        public class DelegateContext<T> where T : Delegate
        {
            private readonly string _assemblyName;
            private readonly string _className;
            private readonly string _methodName;
            private readonly T? _delegate;
            public bool IsValid => _delegate != null;

            public DelegateContext(string assemblyName, string className, string methodName, MethodInfo? method)
            {
                _assemblyName = assemblyName;
                _className = className;
                _methodName = methodName;
                if (method != null)
                {
                    try
                    {
                        _delegate = (T)Delegate.CreateDelegate(typeof(T), method);
                    }
                    catch (Exception)
                    {
                        // _delegate is null
                    }
                }
            }

            public T Call => _delegate ?? throw new Exception("Delegate not found");
        }

        public class FieldContext
        {
            private readonly string _assemblyName;
            private readonly string _className;
            private readonly string _fieldName;
            private readonly FieldInfo? _field;
            public bool IsValid => _field != null;

            public FieldContext(string assemblyName, string className, string fieldName, FieldInfo? field)
            {
                _assemblyName = assemblyName;
                _className = className;
                _fieldName = fieldName;
                _field = field;
            }

            public FieldInfo? FieldInfo() => _field;

            public T GetValue<T>(object e)
            {
                if (_field == null)
                    throw new Exception($"MechJeb reflection bug: field {_fieldName} in {_className}, {_assemblyName} is null");

                object? value = _field.GetValue(e);

                if (value is T val)
                    return val;

                // a field of another primitive type than it is read as, a float read as a double say: a mod can change a field's
                // type between versions, and that should not make every read throw
                if (value != null && value.GetType().IsPrimitive && typeof(T).IsPrimitive)
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        // an overflow: reported below
                    }
                }

                throw new Exception(
                    $"MechJeb reflection bug: field {_fieldName} in {_className}, {_assemblyName} did not return a {typeof(T).Name} (it holds {value?.GetType().Name ?? "null"})");
            }
        }
    }
}
