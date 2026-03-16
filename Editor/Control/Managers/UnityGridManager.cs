using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal sealed class UnityGridManager
    {
        private static Type UnityAnnotationUtility;
        private static PropertyInfo UnityShowGridProperty;

        private static bool initialized = false;
        private static bool reflectionSucceeded = false;

        private static void InitReflectedData()
        {
            if(initialized)
            {
                return;
            }

            initialized         = true;
            reflectionSucceeded = false;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> types      = new List<Type>();

            foreach(Assembly assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch
                {
                }
            }

            UnityAnnotationUtility = types.FirstOrDefault(t => t.FullName == "UnityEditor.AnnotationUtility");

            if(UnityAnnotationUtility != null)
            {
                UnityShowGridProperty = UnityAnnotationUtility.GetProperty(
                    "showGrid", BindingFlags.NonPublic | BindingFlags.Static
                );

                if(UnityShowGridProperty != null)
                {
                    UnityShowGridProperty.SetValue(UnityAnnotationUtility, false, null);
                }
            }
            else
            {
                UnityShowGridProperty = null;
            }

            reflectionSucceeded = UnityShowGridProperty != null;
        }

        internal static bool ShowGrid
        {
            get
            {
                InitReflectedData();

                if(reflectionSucceeded && UnityShowGridProperty != null)
                {
                    return (bool)UnityShowGridProperty.GetValue(UnityAnnotationUtility, null);
                }

                return true;
            }
            set
            {
                InitReflectedData();

                if(reflectionSucceeded && UnityShowGridProperty != null)
                {
                    UnityShowGridProperty.SetValue(UnityAnnotationUtility, value, null);
                }
            }
        }
    }
}
