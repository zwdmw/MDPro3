using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3
{
    public sealed class QuestNativeMainMenu : MonoBehaviour
    {
        private const int FallbackQuestOverlayLayer = 24;
        private const float PanelScale = 0.026f;
        private static readonly Vector3 PanelPosition = new Vector3(0f, 13.5f, 18f);
        private static readonly Vector3 DefaultEyePosition = new Vector3(0f, 24f, -42f);

        private Camera xrCamera;
        private Canvas canvas;
        private RectTransform rect;
        private TextMeshProUGUI statusText;
        private string lastState;
        private bool loggedReady;

        public void Initialize(Camera camera)
        {
            xrCamera = camera;
            EnsureCanvas();
            Refresh();
        }

        public void Tick()
        {
            if (canvas == null)
                return;

            var shouldShow = ShouldShow();
            if (canvas.gameObject.activeSelf != shouldShow)
                canvas.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                return;

            PlacePanel();
            RefreshStatus();
            if (!loggedReady)
            {
                Debug.LogFormat("Quest native main menu visible. Pos={0}, Scale={1}, CurrentServant={2}",
                    rect.position,
                    rect.localScale,
                    Program.instance?.currentServant == null ? "<null>" : Program.instance.currentServant.GetType().Name);
                loggedReady = true;
            }
        }

        private bool ShouldShow()
        {
            var program = Program.instance;
            if (program == null)
                return false;
            if (program.currentServant == program.ocgcore)
                return false;
            return true;
        }

        private void EnsureCanvas()
        {
            if (canvas != null)
                return;

            var canvasObject = new GameObject("QuestNativeMainMenu", typeof(RectTransform));
            SetQuestOverlayLayer(canvasObject);
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue - 10;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 2f;
            scaler.referencePixelsPerUnit = 100f;

            var raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1720f, 920f);
            rect.localScale = Vector3.one * PanelScale;
            AddPanelBackground(canvasObject);

            CreateText("Title", rect, new Vector2(70f, -60f), new Vector2(980f, 76f), 42f, TextAlignmentOptions.Left, "MDPro3 Quest");
            CreateText("Subtitle", rect, new Vector2(70f, -126f), new Vector2(1250f, 52f), 25f, TextAlignmentOptions.Left, "世界大屏模式");
            statusText = CreateText("Status", rect, new Vector2(70f, -800f), new Vector2(1560f, 52f), 23f, TextAlignmentOptions.Left, string.Empty);

            var x = 70f;
            var y = -230f;
            var w = 460f;
            var h = 92f;
            var gap = 28f;
            CreateButton("Solo", rect, new Vector2(x, y), new Vector2(w, h), "单人模式", OpenSolo, new Color(0.06f, 0.36f, 0.38f, 0.96f));
            CreateButton("Deck", rect, new Vector2(x, y - (h + gap)), new Vector2(w, h), "卡组编辑", OpenDeckEditor, new Color(0.10f, 0.26f, 0.42f, 0.96f));
            CreateButton("Settings", rect, new Vector2(x, y - (h + gap) * 2f), new Vector2(w, h), "设置", OpenSettings, new Color(0.20f, 0.22f, 0.38f, 0.96f));
            CreateButton("Back", rect, new Vector2(x, y - (h + gap) * 3f), new Vector2(w, h), "返回主菜单", OpenMdproMenu, new Color(0.20f, 0.28f, 0.30f, 0.96f));
            CreateButton("Exit", rect, new Vector2(x + w + 40f, y - (h + gap) * 3f), new Vector2(w, h), "退出应用", QuitGame, new Color(0.38f, 0.12f, 0.16f, 0.96f));

            var note = "这是 Quest 原生世界大屏，不依赖 MDPro 的 RenderTexture。手柄射线直接点击这些按钮。";
            CreateText("Note", rect, new Vector2(620f, -230f), new Vector2(900f, 330f), 28f, TextAlignmentOptions.TopLeft, note);
            PlacePanel();
        }

        private void PlacePanel()
        {
            if (rect == null)
                return;

            rect.SetPositionAndRotation(PanelPosition, ResolvePanelRotation(PanelPosition));
            rect.localScale = Vector3.one * PanelScale;
            if (canvas != null && canvas.worldCamera != xrCamera)
                canvas.worldCamera = xrCamera;
        }

        private Quaternion ResolvePanelRotation(Vector3 position)
        {
            var eye = xrCamera == null ? DefaultEyePosition : xrCamera.transform.position;
            var forward = position - eye;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void Refresh()
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusText == null)
                return;

            var program = Program.instance;
            var servantName = program?.currentServant == null ? "初始化中" : program.currentServant.GetType().Name;
            var state = "当前界面: " + servantName;
            if (state == lastState)
                return;
            lastState = state;
            statusText.text = state;
        }

        private void OpenSolo()
        {
            var program = Program.instance;
            if (program?.menu == null || program.solo == null)
                return;

            Debug.Log("Quest native main menu: open solo.");
            program.menu.OnSolo();
        }

        private void OpenDeckEditor()
        {
            var program = Program.instance;
            if (program?.menu == null || program.selectDeck == null)
                return;

            Debug.Log("Quest native main menu: open deck editor.");
            program.menu.OnEditDeck();
        }

        private void OpenSettings()
        {
            var program = Program.instance;
            if (program?.menu == null || program.setting == null)
                return;

            Debug.Log("Quest native main menu: open settings.");
            program.menu.OnSetting();
        }

        private void OpenMdproMenu()
        {
            var program = Program.instance;
            if (program?.menu == null)
                return;

            Debug.Log("Quest native main menu: return to menu.");
            program.ShiftToServant(program.menu);
        }

        private void QuitGame()
        {
            Debug.Log("Quest native main menu: quit.");
            Program.GameQuit();
        }

        private static void AddPanelBackground(GameObject canvasObject)
        {
            var image = canvasObject.AddComponent<Image>();
            image.color = new Color(0.015f, 0.025f, 0.033f, 0.94f);
            image.raycastTarget = true;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, string value)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            SetQuestOverlayLayer(obj);
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = obj.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            text.enableAutoSizing = true;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            var font = Program.instance?.ui_?.tmpFont;
            if (font != null)
                text.font = font;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, Action onClick, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            SetQuestOverlayLayer(obj);
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f),
                pressedColor = new Color(0.80f, 0.92f, 1f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.28f, 0.28f, 0.28f, 0.65f),
                colorMultiplier = 1f,
                fadeDuration = 0.04f
            };
            button.onClick.AddListener(() => onClick?.Invoke());
            CreateText("Label", rect, new Vector2(18f, -14f), new Vector2(size.x - 36f, size.y - 28f), 31f, TextAlignmentOptions.Center, label);
            return button;
        }

        private static int GetQuestOverlayLayer()
        {
            var layer = LayerMask.NameToLayer("QuestOverlay");
            return layer >= 0 ? layer : FallbackQuestOverlayLayer;
        }

        private static void SetQuestOverlayLayer(GameObject gameObject)
        {
            if (gameObject == null)
                return;
            SetLayerRecursively(gameObject, GetQuestOverlayLayer());
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                if (child != null)
                    SetLayerRecursively(child.gameObject, layer);
        }
    }
}
