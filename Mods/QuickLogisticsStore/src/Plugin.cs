using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace SanCun.QuickLogisticsStore
{
    [BepInPlugin(Id, "Quick Logistics Store", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "com.sancun.dsp.quicklogisticsstore";
        public const string Version = "1.0.1";
        private static Plugin instance;
        private readonly Dictionary<UIInventoryWindow, StoreButton> views = new Dictionary<UIInventoryWindow, StoreButton>();
        private ConfigEntry<KeyboardShortcut> shortcut;
        private Harmony harmony;
        private bool transferring;
        private float nextActionTime;

        private void Awake()
        {
            instance = this;
            shortcut = Config.Bind("快捷键", "收纳物流", new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl, KeyCode.LeftShift),
                "打开背包后，将物品补入已设置物品的机甲物流格。可改为其他组合键；聊天、搜索和暂停菜单中不触发。");
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Quick Logistics Store " + Version + " 已加载；打开背包后使用 " + shortcut.Value + " 或物流标题旁的收纳按钮。");
        }

        private void LateUpdate()
        {
            if (!shortcut.Value.IsDown()) return;
            Store();
        }

        private void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
            foreach (StoreButton view in views.Values) view.Dispose();
            views.Clear();
            instance = null;
        }

        internal static void Open(UIInventoryWindow window)
        {
            if (instance == null) return;
            try
            {
                StoreButton view;
                if (!instance.views.TryGetValue(window, out view))
                {
                    view = new StoreButton(window, instance.Store);
                    instance.views.Add(window, view);
                }
                view.Open(instance.shortcut.Value.ToString());
            }
            catch (Exception error) { instance.Logger.LogError("创建物流收纳按钮失败：" + error); }
        }

        internal static void DestroyWindow(UIInventoryWindow window)
        {
            if (instance == null) return;
            StoreButton view;
            if (!instance.views.TryGetValue(window, out view)) return;
            view.Dispose();
            instance.views.Remove(window);
        }

        private void Store()
        {
            if (transferring || Time.unscaledTime < nextActionTime) return;
            if (!GameMain.isRunning || GameMain.instance == null || GameMain.instance.isMenuDemo || GameMain.isLoading || GameMain.isEnded ||
                GameMain.isFullscreenPaused || GameMain.isPaused || GameMain.mainPlayer == null ||
                !GameMain.mainPlayer.isAlive || InputGuard.IsEditing()) return;
            UIRoot root = UIRoot.instance;
            if (root == null || root.uiGame == null) return;
            UIInventoryWindow window = root.uiGame.inventoryWindow;
            if (window == null || !window.active)
            {
                UIRealtimeTip.Popup("请先打开背包，再使用物流收纳", false, 0);
                return;
            }
            transferring = true;
            nextActionTime = Time.unscaledTime + 0.25f;
            try
            {
                StoreResult result = NativeStore.Execute(window, GameMain.mainPlayer);
                UIRealtimeTip.Popup(result.Message, false, 0);
                if (result.Count > 0) Logger.LogInfo("物流收纳：" + result.Types + " 种，" + result.Count + " 件。");
            }
            catch (Exception error)
            {
                Logger.LogError("物流收纳失败：" + error);
                UIRealtimeTip.Popup("物流收纳未完成，请查看模组日志", true, 0);
            }
            finally { transferring = false; }
        }
    }

    [HarmonyPatch(typeof(UIInventoryWindow), "_OnOpen")]
    internal static class InventoryOpenPatch
    {
        private static void Postfix(UIInventoryWindow __instance) { Plugin.Open(__instance); }
    }

    [HarmonyPatch(typeof(UIInventoryWindow), "_OnDestroy")]
    internal static class InventoryDestroyPatch
    {
        private static void Prefix(UIInventoryWindow __instance) { Plugin.DestroyWindow(__instance); }
    }
}
