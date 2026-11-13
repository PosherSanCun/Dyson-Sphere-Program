using System;
using UnityEngine;
using UnityEngine.UI;

namespace SanCun.QuickLogisticsStore
{
    internal sealed class StoreButton : IDisposable
    {
        private readonly UIInventoryWindow window;
        private readonly Action store;
        private GameObject root;
        private RectTransform rect;
        private Text title;
        private UIButton tip;

        internal StoreButton(UIInventoryWindow window, Action store) { this.window = window; this.store = store; }

        internal void Open(string shortcut)
        {
            if (window.deliveryPanel == null || !window.deliveryPanel.active) return;
            if (root == null) Create();
            Position();
            tip.tips.tipText = "将背包物品补入已设置物品的物流格，放不下的留在背包。\n快捷键：" + shortcut;
        }

        private void Create()
        {
            title = FindTitle();
            if (title == null) throw new InvalidOperationException("未找到物流标题，未创建按钮。");
            try
            {
                root = new GameObject("sancun-quick-logistics-store", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                rect = root.GetComponent<RectTransform>();
                rect.SetParent(title.rectTransform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(20f, 20f);
                Image background = root.GetComponent<Image>();
                background.color = Color.white;
                Button button = root.GetComponent<Button>();
                button.targetGraphic = background;
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.16f, 0.37f, 0.44f, 0.72f);
                colors.highlightedColor = new Color(0.24f, 0.63f, 0.72f, 0.95f);
                colors.pressedColor = new Color(0.10f, 0.28f, 0.35f, 1f);
                colors.selectedColor = colors.normalColor;
                colors.fadeDuration = 0.08f;
                button.colors = colors;
                Navigation navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                button.onClick.AddListener(Click);
                tip = root.AddComponent<UIButton>();
                tip.button = button;
                // 原生 LateUpdate 会直接读取数组长度；运行期新建按钮没有预制体的序列化默认值。
                tip.transitions = new UIButton.Transition[0];
                tip.tips.tipTitle = "收纳到物流";
                tip.tips.corner = 3;
                tip.tips.width = 270;
                tip.tips.delay = 0.35f;
                GameObject glyph = new GameObject("store-icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(StoreIcon));
                RectTransform glyphRect = glyph.GetComponent<RectTransform>();
                glyphRect.SetParent(rect, false);
                glyphRect.anchorMin = glyphRect.anchorMax = glyphRect.pivot = new Vector2(0.5f, 0.5f);
                glyphRect.sizeDelta = new Vector2(14f, 14f);
                StoreIcon icon = glyph.GetComponent<StoreIcon>();
                icon.color = new Color(0.78f, 0.94f, 0.97f, 1f);
                icon.raycastTarget = false;
            }
            catch { Dispose(); throw; }
        }

        private Text FindTitle()
        {
            // 当前游戏场景中的原生标题名；标题与动态物流格面板可能是兄弟节点。
            foreach (Text candidate in window.GetComponentsInChildren<Text>(true))
                if (candidate.gameObject.name == "delivery-title-text") return candidate;
            foreach (Text candidate in window.deliveryPanel.GetComponentsInChildren<Text>(true))
            {
                if (candidate == window.deliveryPanel.stateTitleText) continue;
                string text = (candidate.text ?? "").Trim();
                if (text == "物流" || text == "物流背包" || text.Equals("Logistics", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals("Logistics inventory", StringComparison.OrdinalIgnoreCase)) return candidate;
            }
            return null;
        }

        private void Position()
        {
            float width = Mathf.Min(title.preferredWidth, title.rectTransform.rect.width);
            float left = 0f;
            TextAnchor alignment = title.alignment;
            if (alignment == TextAnchor.UpperCenter || alignment == TextAnchor.MiddleCenter || alignment == TextAnchor.LowerCenter)
                left = (title.rectTransform.rect.width - width) * 0.5f;
            else if (alignment == TextAnchor.UpperRight || alignment == TextAnchor.MiddleRight || alignment == TextAnchor.LowerRight)
                left = title.rectTransform.rect.width - width;
            rect.anchoredPosition = new Vector2(left + width + 6f, 0f);
        }

        private void Click() { store(); }

        public void Dispose()
        {
            if (root != null)
            {
                Button button = root.GetComponent<Button>();
                if (button != null) button.onClick.RemoveListener(Click);
                root.SetActive(false);
                UnityEngine.Object.Destroy(root);
            }
            root = null;
            rect = null;
            tip = null;
            title = null;
        }
    }
}
