/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
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

        static ReflectionUtils()
        {
            IsLoadedProceduralFairing = IsAssemblyLoaded("ProceduralFairings");
            IsLoadedRealFuels = IsAssemblyLoaded("RealFuels");
            IsLoadedPrincipia = IsAssemblyLoaded("principia.ksp_plugin_adapter");
            IsLoadedFAR = IsAssemblyLoaded("FerramAerospaceResearch");
            IsLoadedRealismOverhaul = IsAssemblyLoaded("RealismOverhaul");
            IsLoadedRP0 = IsAssemblyLoaded("RP0");
        }

        public static bool IsAssemblyLoaded(string assemblyName)
        {
            foreach (AssemblyLoader.LoadedAssembly assembly in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    if (assembly.assembly.GetName().Name == assemblyName)
                        return true;
                }
                catch (InvalidOperationException)
                {
                    // ignore busted assemblies
                }
            }

            return false;
        }

        public static AssemblyContext Assembly(string assemblyString)
        {
            string assemblyName = "";

            foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
                if (loaded.assembly.GetName().Name == assemblyString)
                    assemblyName = loaded.assembly.FullName;

            if (assemblyName == "")
                Debug.Log("[MechJeb] ReflectionUtils: could not find assembly " + assemblyString);

            return new AssemblyContext(assemblyName);
        }

        public class AssemblyContext
        {
            private readonly string _assemblyName;

            public AssemblyContext(string assemblyName)
            {
                _assemblyName = assemblyName;
            }

            public ClassContext Class(string className)
            {
                var type = Type.GetType(className + ", " + _assemblyName);

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
            public bool IsValid => _type != null;

            public ClassContext(string assemblyName, string className, Type? type)
            {
                _assemblyName = assemblyName;
                _className = className;
                _type = type;
            }

            public MethodContext Method(string methodName, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, Type[]? args = null)
            {
                MethodInfo? method = null;

                if (_type != null)
                    method = args == null ? _type.GetMethod(methodName, flags) : _type.GetMethod(methodName, flags, null, args, null);

                if (method == null)
                    Debug.Log($"[MechJeb] ReflectionUtils: could not find method {methodName} in {_className}, {_assemblyName}");

                return new MethodContext(_assemblyName, _className, methodName, method);
            }

            public FieldContext Field(string fieldName, BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            {
                FieldInfo? field = null;
                if (_type != null)
                    field = _type.GetField(fieldName, flags);

                if (field == null)
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
                    catch (ArgumentException)
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

                if (!(_field.GetValue(e) is T val))
                    throw new Exception($"MechJeb reflection bug: field {_fieldName} in {_className}, {_assemblyName} did not return a value");

                return val;
            }
        }
    }
}
