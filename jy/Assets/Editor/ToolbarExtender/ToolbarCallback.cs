using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace UnityToolbarExtender
{
    [InitializeOnLoad]
    public class ToolbarCallback
    {
        static Type m_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type m_guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
#if UNITY_2020_1_OR_NEWER
        static Type m_iWindowBackendType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IWindowBackend");
        static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#else
        static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
        static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static ScriptableObject m_currentToolbar;

        public static Action OnToolbarGUI;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (m_currentToolbar != null)
                {
#if UNITY_2020_1_OR_NEWER
                    var windowBackend = m_windowBackend.GetValue(m_currentToolbar);

                    // Get it's visual tree
                    var visualTree = (VisualElement)m_viewVisualTree.GetValue(windowBackend, null);
#else
					// Get it's visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(m_currentToolbar, null);
#endif
                    // Get first child which 'happens' to be toolbar IMGUIContainer 
                    var container = (IMGUIContainer)visualTree[0];

                    // (Re)attach handler ToolBar-->GUIView-->visualTree-->IMGUIContainer-->m_OnGUIHandler
                    // IMGUIContainer 每次刷新的时候调用到ToolbarCallback::OnGUI
                    var handler = (Action)m_imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    m_imguiContainerOnGui.SetValue(container, handler);
                }
            }
        }

        static void OnGUI()
        {
            var handler = OnToolbarGUI;
            if (handler != null)
            {
                handler();
            }
        }
    }

}

