using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SanCun.QuickLogisticsStore
{
    internal static class InputGuard
    {
        private static bool resolved;
        private static Type tmpInputType;
        private static PropertyInfo tmpFocused;
        private static FieldInfo chatInstance;
        private static MethodInfo chatActive;

        internal static bool IsEditing()
        {
            if (VFInput.inputing || VFInput.inFullscreenGUI || VFInput.inScreenshotMode) return true;
            if (!resolved) ResolveOptionalInputs();
            EventSystem events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null)
            {
                GameObject selected = events.currentSelectedGameObject;
                InputField native = selected.GetComponentInParent<InputField>();
                if (native != null && native.isFocused) return true;
                if (tmpInputType != null && tmpFocused != null)
                {
                    Component tmp = selected.GetComponentInParent(tmpInputType);
                    if (tmp != null && (bool)tmpFocused.GetValue(tmp, null)) return true;
                }
            }
            // Nebula 的 IMGUI 聊天不依赖 VFInput.inputing，使用可选反射同时覆盖两种聊天界面。
            if (chatInstance != null && chatActive != null)
            {
                object chat = chatInstance.GetValue(null);
                if (chat != null && (bool)chatActive.Invoke(chat, null)) return true;
            }
            return false;
        }

        private static void ResolveOptionalInputs()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (tmpInputType == null) tmpInputType = assembly.GetType("TMPro.TMP_InputField", false);
                Type chat = assembly.GetType("NebulaWorld.MonoBehaviours.Local.Chat.ChatManager", false);
                if (chat == null) continue;
                chatInstance = chat.GetField("Instance", BindingFlags.Static | BindingFlags.Public);
                chatActive = chat.GetMethod("IsChatViewActive", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            }
            if (tmpInputType != null) tmpFocused = tmpInputType.GetProperty("isFocused");
            resolved = true;
        }
    }
}
