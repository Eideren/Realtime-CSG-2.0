using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RealtimeCSG
{
    internal sealed class UnityGridManager
    {
        private static Type m_UnityAnnotationUtility;
        private static PropertyInfo m_UnityShowGridProperty;

        private static bool m_Initialized;
        private static bool m_ReflectionSucceeded;

        private static void InitReflectedData()
        {
            if(m_Initialized)
            {
                return;
            }

            m_Initialized         = true;
            m_ReflectionSucceeded = false;

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

            m_UnityAnnotationUtility = types.FirstOrDefault(t => t.FullName == "UnityEditor.AnnotationUtility");

            if(m_UnityAnnotationUtility != null)
            {
                m_UnityShowGridProperty = m_UnityAnnotationUtility.GetProperty("showGrid", BindingFlags.NonPublic | BindingFlags.Static);

                if(m_UnityShowGridProperty != null)
                {
                    m_UnityShowGridProperty.SetValue(m_UnityAnnotationUtility, false, null);
                }
            }
            else
            {
                m_UnityShowGridProperty = null;
            }

            m_ReflectionSucceeded = m_UnityShowGridProperty != null;
        }

        internal static bool ShowGrid
        {
            get
            {
                InitReflectedData();

                if(m_ReflectionSucceeded && m_UnityShowGridProperty != null)
                {
                    return (bool)m_UnityShowGridProperty.GetValue(m_UnityAnnotationUtility, null);
                }

                return true;
            }
            set
            {
                InitReflectedData();

                if(m_ReflectionSucceeded && m_UnityShowGridProperty != null)
                {
                    m_UnityShowGridProperty.SetValue(m_UnityAnnotationUtility, value, null);
                }
            }
        }
    }
}
