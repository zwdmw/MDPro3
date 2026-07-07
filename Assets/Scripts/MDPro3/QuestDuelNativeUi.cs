using System;
using System.Collections;
using System.Collections.Generic;
using MDPro3.Net;
using MDPro3.UI;
using MDPro3.YGOSharp;
using MDPro3.YGOSharp.OCGWrapper.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3
{
    public sealed class QuestDuelNativeUi : MonoBehaviour
    {
        private const int FallbackQuestOverlayLayer = 24;
        private const float PanelScale = 0.014f;
        private const float SmallPanelScale = 0.016f;
        private const float HudScale = 0.017f;
        private const float FloorHudScale = 0.020f;
        private const float CardInfoScale = 0.021f;
        private const float DuelLogPanelScale = 0.026f;
        private const float WorldCanvasDynamicPixelsPerUnit = 5f;
        private const float QuestBoardScaleX = 1.38f;
        private const float QuestBoardScaleZ = 1.34f;
        private const int CardGridPageSize = 12;
        private static readonly Vector3 DuelWorldCenterOnGround = new Vector3(0f, -0.005f, -1.5f);

        private Camera xrCamera;
        private Transform duelWorldAnchor;
        private Canvas cardPanelCanvas;
        private RectTransform cardPanelRect;
        private TextMeshProUGUI cardTitleText;
        private TextMeshProUGUI cardCountText;
        private TextMeshProUGUI cardPageText;
        private TextMeshProUGUI detailNameText;
        private TextMeshProUGUI detailMetaText;
        private TextMeshProUGUI detailDescriptionText;
        private RawImage detailImage;
        private Button cardConfirmButton;
        private Button cardCancelButton;
        private Button cardFinishButton;
        private Button cardPreviousButton;
        private Button cardNextButton;
        private RectTransform cardGridRoot;
        private readonly List<GameObject> cardGridItems = new List<GameObject>();
        private readonly List<GameCard> cards = new List<GameCard>();
        private readonly HashSet<GameCard> selectedCards = new HashSet<GameCard>();
        private readonly List<GameCard> selectedOrder = new List<GameCard>();
        private readonly Dictionary<GameCard, Image> cardItemBackgrounds = new Dictionary<GameCard, Image>();
        private string cardHint;
        private int cardMin;
        private int cardMax;
        private bool cardExitable;
        private bool cardReadOnly;
        private int cardPage;
        private GameCard detailCard;

        private Canvas cardInfoCanvas;
        private RectTransform cardInfoRect;
        private TextMeshProUGUI cardInfoNameText;
        private TextMeshProUGUI cardInfoMetaText;
        private TextMeshProUGUI cardInfoDescriptionText;
        private RawImage cardInfoImage;

        private Canvas optionCanvas;
        private RectTransform optionRect;
        private TextMeshProUGUI optionTitleText;
        private TextMeshProUGUI optionBodyText;
        private RectTransform optionListRoot;
        private readonly List<GameObject> optionRows = new List<GameObject>();
        private GameCard optionAnchorCard;

        private Canvas phaseMenuCanvas;
        private RectTransform phaseMenuRect;
        private TextMeshProUGUI phaseMenuTitleText;
        private RectTransform phaseMenuListRoot;
        private readonly List<GameObject> phaseMenuRows = new List<GameObject>();

        private Canvas phaseHudCanvas;
        private RectTransform phaseHudRect;
        private TextMeshProUGUI phaseHudText;
        private TextMeshProUGUI lifeHudText;
        private Canvas controlHudCanvas;
        private RectTransform controlHudRect;
        private RectTransform phaseHudButtonRoot;
        private RectTransform systemHudButtonRoot;
        private readonly List<GameObject> phaseHudRows = new List<GameObject>();
        private string lastPhaseHudSignature;

        private Canvas duelLogCanvas;
        private RectTransform duelLogRect;
        private TextMeshProUGUI duelLogTitleText;
        private TextMeshProUGUI duelLogBodyText;
        private string lastDuelLogSignature;
        private bool duelLogPanelLogged;

        public bool HasBlockingPanel
        {
            get
            {
                return (cardPanelCanvas != null && cardPanelCanvas.gameObject.activeSelf)
                    || (optionCanvas != null && optionCanvas.gameObject.activeSelf)
                    || (phaseMenuCanvas != null && phaseMenuCanvas.gameObject.activeSelf);
            }
        }

        public void Configure(Camera camera, Transform worldAnchor = null)
        {
            xrCamera = camera;
            duelWorldAnchor = worldAnchor;
            ReparentCanvasesToWorldAnchor();
            UpdatePanelPoses();
        }

        private void ReparentCanvasesToWorldAnchor()
        {
            if (duelWorldAnchor == null)
                return;

            ReparentCanvas(cardPanelCanvas);
            ReparentCanvas(cardInfoCanvas);
            ReparentCanvas(optionCanvas);
            ReparentCanvas(phaseMenuCanvas);
            ReparentCanvas(phaseHudCanvas);
            ReparentCanvas(controlHudCanvas);
            ReparentCanvas(duelLogCanvas);
        }

        private void ReparentCanvas(Canvas canvas)
        {
            if (canvas == null || canvas.transform.parent == duelWorldAnchor)
                return;

            canvas.transform.SetParent(duelWorldAnchor, false);
        }

        public void Tick()
        {
            if (!CanShowDuelUi())
            {
                HideAllQuestUi();
                return;
            }

            UpdatePhaseHud();
            UpdatePanelPoses();
        }

        public bool ShowPhaseMenu(List<string> selections)
        {
            var core = Program.instance?.ocgcore;
            if (!CanShowDuelUi() || core == null || selections == null || selections.Count == 0)
                return false;

            EnsurePhaseMenu();
            ClearRows(phaseMenuRows);
            phaseMenuTitleText.text = "阶段切换";
            AddPhaseMenuButton("战斗阶段", selections.Contains(DuelPhase.BattleStart.ToString()), () => SendPhaseResponse(6));
            AddPhaseMenuButton("主要阶段2", selections.Contains(DuelPhase.Main2.ToString()), () => SendPhaseResponse(2));
            AddPhaseMenuButton("结束阶段", selections.Contains(DuelPhase.End.ToString()), () =>
            {
                var response = selections[0] == DuelPhase.BattleStart.ToString() ? 3 : 7;
                SendPhaseResponse(response);
            });

            phaseMenuTitleText.text = LocalizeQuestLabel(phaseMenuTitleText.text);
            phaseMenuCanvas.gameObject.SetActive(true);
            AudioManager.PlaySE("SE_PHASE_WINDOW_OPEN");
            UpdatePanelPoses();
            return true;
        }

        public bool ShowSelection(List<string> selections, List<int> responses)
        {
            return ShowSelection(selections, responses, null);
        }

        public bool ShowSelectionNearCard(List<string> selections, List<int> responses, GameCard anchorCard)
        {
            return ShowSelection(selections, responses, anchorCard);
        }

        private bool ShowSelection(List<string> selections, List<int> responses, GameCard anchorCard)
        {
            if (!CanShowDuelUi() || Program.instance?.ocgcore == null || selections == null || responses == null)
                return false;

            EnsureOptionPanel();
            optionAnchorCard = anchorCard;
            ClearRows(optionRows);
            optionTitleText.text = selections.Count > 0 ? LocalizeQuestLabel(SanitizeText(selections[0])) : "\u8bf7\u9009\u62e9";
            optionBodyText.text = string.Empty;
            for (var index = 1; index < selections.Count; index += 1)
            {
                var responseIndex = index - 1;
                if (responseIndex >= responses.Count)
                    break;

                var label = LocalizeQuestLabel(SanitizeText(selections[index]));
                var response = responses[responseIndex];
                AddOptionButton(label, () =>
                {
                    if (response != -233)
                        SendIntResponse(response);
                    HideOptionPanel();
                });
            }

            optionCanvas.gameObject.SetActive(true);
            UpdatePanelPoses();
            return true;
        }

        public bool ShowYesOrNo(List<string> selections, Action confirmAction, Action cancelAction)
        {
            if (!CanShowDuelUi() || Program.instance?.ocgcore == null || selections == null)
                return false;

            EnsureOptionPanel();
            optionAnchorCard = null;
            ClearRows(optionRows);
            optionTitleText.text = selections.Count > 0 ? LocalizeQuestLabel(SanitizeText(selections[0])) : "\u786e\u8ba4";
            optionBodyText.text = selections.Count > 1 ? LocalizeQuestLabel(SanitizeText(selections[1])) : string.Empty;
            AddOptionButton(selections.Count > 2 ? LocalizeQuestLabel(SanitizeText(selections[2])) : "\u786e\u5b9a", () =>
            {
                confirmAction?.Invoke();
                HideOptionPanel();
            });
            AddOptionButton(selections.Count > 3 ? LocalizeQuestLabel(SanitizeText(selections[3])) : "\u53d6\u6d88", () =>
            {
                cancelAction?.Invoke();
                HideOptionPanel();
            }, new Color(0.38f, 0.16f, 0.18f, 0.98f));

            optionCanvas.gameObject.SetActive(true);
            AudioManager.PlaySE("SE_SYS_VERIFY");
            UpdatePanelPoses();
            return true;
        }

        public bool ShowPositionSelection(int code, int count, int option1, int option2)
        {
            if (!CanShowDuelUi() || Program.instance?.ocgcore == null)
                return false;

            EnsureOptionPanel();
            optionAnchorCard = null;
            ClearRows(optionRows);
            optionTitleText.text = "选择表示形式";
            var card = CardsManager.Get(code);
            optionBodyText.text = card == null || string.IsNullOrWhiteSpace(card.Name)
                ? "请选择怪兽的表示形式"
                : SanitizeText(card.Name);

            if (count == 3)
            {
                AddPositionOption(1);
                AddPositionOption(4);
                AddPositionOption(8);
            }
            else
            {
                AddPositionOption(option1);
                AddPositionOption(option2);
            }

            optionCanvas.gameObject.SetActive(true);
            AudioManager.PlaySE("SE_SYS_VERIFY");
            UpdatePanelPoses();
            return true;
        }

        public bool ShowCardSelection(string hint, List<GameCard> sourceCards, int min, int max, bool exitable, bool sendable)
        {
            if (!CanShowDuelUi() || Program.instance?.ocgcore == null || sourceCards == null)
                return false;

            HideCardInfoPanel();
            EnsureCardPanel();
            cardReadOnly = Program.instance.ocgcore.currentMessage == GameMessage.ConfirmCards;
            cardHint = string.IsNullOrWhiteSpace(hint) ? "请选择卡片" : SanitizeText(hint);
            cardMin = min;
            cardMax = max <= 0 && !cardReadOnly ? sourceCards.Count : max;
            cardExitable = exitable;
            cardPage = 0;
            detailCard = null;
            cards.Clear();
            selectedCards.Clear();
            selectedOrder.Clear();
            cardItemBackgrounds.Clear();
            foreach (var card in sourceCards)
                if (card != null)
                    cards.Add(card);

            PreselectForcedCardsForSum();
            RebuildCardGrid();
            ShowCardDetail(cards.Count > 0 ? cards[0] : null);
            cardPanelCanvas.gameObject.SetActive(true);
            UpdateCardButtons();
            UpdatePanelPoses();
            return true;
        }

        public bool ShowCardBrowser(string title, List<GameCard> sourceCards)
        {
            if (!CanShowDuelUi() || Program.instance?.ocgcore == null || sourceCards == null)
                return false;

            HideCardInfoPanel();
            EnsureCardPanel();
            cardReadOnly = true;
            cardHint = string.IsNullOrWhiteSpace(title) ? "卡片列表" : title;
            cardMin = 0;
            cardMax = 0;
            cardExitable = true;
            cardPage = 0;
            detailCard = null;
            cards.Clear();
            selectedCards.Clear();
            selectedOrder.Clear();
            cardItemBackgrounds.Clear();
            foreach (var card in sourceCards)
                if (card != null)
                    cards.Add(card);

            RebuildCardGrid();
            ShowCardDetail(cards.Count > 0 ? cards[0] : null);
            cardPanelCanvas.gameObject.SetActive(true);
            UpdateCardButtons();
            UpdatePanelPoses();
            return true;
        }

        public bool ShowCardInfo(GameCard card)
        {
            if (!CanShowDuelUi() || card == null)
                return false;

            EnsureCardInfoPanel();
            if (cardInfoCanvas == null)
                return false;

            var data = card.GetData();
            if (data == null)
            {
                cardInfoNameText.text = "\u672a\u77e5\u5361\u7247";
                cardInfoMetaText.text = string.Empty;
                cardInfoDescriptionText.text = string.Empty;
                cardInfoImage.texture = TextureManager.container == null ? null : TextureManager.container.unknownCard.texture;
            }
            else
            {
                cardInfoNameText.text = SanitizeText(data.Name);
                cardInfoMetaText.text = GetLongCardMeta(data);
                cardInfoDescriptionText.text = SanitizeText(data.GetDescription(true));
                StartCoroutine(LoadCardTexture(cardInfoImage, data.Id));
            }

            cardInfoCanvas.gameObject.SetActive(true);
            UpdatePanelPoses();
            return true;
        }

        public bool ShowLocationBrowser(uint controller, CardLocation location)
        {
            var core = Program.instance?.ocgcore;
            if (!CanShowDuelUi() || core == null)
                return false;

            var list = core.GCS_GetLocationCards((int)controller, (int)location);
            var title = GetControllerName(controller) + " " + GetLocationName(location);
            return ShowCardBrowser(title, list);
        }

        public void HideAllPopups()
        {
            HideCardPanel();
            HideOptionPanel();
            HidePhaseMenu();
        }

        public void HideAllQuestUi()
        {
            HideCardPanel();
            HideCardInfoPanel();
            HideOptionPanel();
            HidePhaseMenu();
            if (phaseHudCanvas != null && phaseHudCanvas.gameObject.activeSelf)
                phaseHudCanvas.gameObject.SetActive(false);
            if (controlHudCanvas != null && controlHudCanvas.gameObject.activeSelf)
                controlHudCanvas.gameObject.SetActive(false);
            if (duelLogCanvas != null && duelLogCanvas.gameObject.activeSelf)
                duelLogCanvas.gameObject.SetActive(false);
            lastPhaseHudSignature = null;
            lastDuelLogSignature = null;
        }

        private static bool CanShowDuelUi()
        {
            var program = Program.instance;
            var core = program == null ? null : program.ocgcore;
            return program != null
                && core != null
                && program.currentServant == core
                && core.showing;
        }

        private void EnsureCardPanel()
        {
            if (cardPanelCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestCardSelectionPanel", out cardPanelCanvas, out cardPanelRect);
            cardPanelRect.sizeDelta = new Vector2(1820f, 980f);
            AddPanelBackground(canvasObject, new Color(0.015f, 0.019f, 0.026f, 0.96f));

            cardTitleText = CreateText("Title", cardPanelRect, new Vector2(32f, -28f), new Vector2(1160f, 56f), 38f, TextAlignmentOptions.Left);
            cardCountText = CreateText("Count", cardPanelRect, new Vector2(1190f, -32f), new Vector2(260f, 48f), 26f, TextAlignmentOptions.Right);
            cardPageText = CreateText("Page", cardPanelRect, new Vector2(690f, -900f), new Vector2(300f, 44f), 24f, TextAlignmentOptions.Center);

            cardGridRoot = CreateRect("CardGrid", cardPanelRect, new Vector2(32f, -104f), new Vector2(1060f, 760f), new Vector2(0f, 1f));

            cardPreviousButton = CreateButton("Prev", cardPanelRect, new Vector2(405f, -892f), new Vector2(180f, 58f), "上一页", () =>
            {
                if (cardPage > 0)
                {
                    cardPage -= 1;
                    RebuildCardGrid();
                }
            });
            cardNextButton = CreateButton("Next", cardPanelRect, new Vector2(1000f, -892f), new Vector2(180f, 58f), "下一页", () =>
            {
                var maxPage = Mathf.Max(0, (cards.Count - 1) / CardGridPageSize);
                if (cardPage < maxPage)
                {
                    cardPage += 1;
                    RebuildCardGrid();
                }
            });

            var detailRoot = CreateRect("Detail", cardPanelRect, new Vector2(1135f, -102f), new Vector2(650f, 768f), new Vector2(0f, 1f));
            AddRectBackground(detailRoot, new Color(0.035f, 0.045f, 0.057f, 0.96f));
            detailImage = CreateRawImage("DetailImage", detailRoot, new Vector2(24f, -24f), new Vector2(260f, 364f));
            detailNameText = CreateText("DetailName", detailRoot, new Vector2(304f, -28f), new Vector2(320f, 92f), 29f, TextAlignmentOptions.TopLeft);
            detailMetaText = CreateText("DetailMeta", detailRoot, new Vector2(304f, -130f), new Vector2(320f, 86f), 22f, TextAlignmentOptions.TopLeft);
            detailDescriptionText = CreateText("DetailDescription", detailRoot, new Vector2(24f, -420f), new Vector2(600f, 320f), 22f, TextAlignmentOptions.TopLeft);
            detailDescriptionText.enableWordWrapping = true;
            detailDescriptionText.overflowMode = TextOverflowModes.Truncate;

            cardCancelButton = CreateButton("Cancel", cardPanelRect, new Vector2(32f, -892f), new Vector2(210f, 62f), "取消", CancelCardSelection, new Color(0.36f, 0.15f, 0.18f, 0.98f));
            cardFinishButton = CreateButton("Finish", cardPanelRect, new Vector2(1578f, -892f), new Vector2(210f, 62f), "关闭", FinishCardSelection, new Color(0.08f, 0.42f, 0.31f, 0.98f));
            cardConfirmButton = CreateButton("Confirm", cardPanelRect, new Vector2(1578f, -892f), new Vector2(210f, 62f), "确定", ConfirmCardSelection, new Color(0.08f, 0.42f, 0.31f, 0.98f));

            canvasObject.SetActive(false);
        }

        private void EnsureCardInfoPanel()
        {
            if (cardInfoCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestCardInfoPanel", out cardInfoCanvas, out cardInfoRect);
            cardInfoRect.sizeDelta = new Vector2(1180f, 860f);
            AddPanelBackground(canvasObject, new Color(0.010f, 0.014f, 0.019f, 0.94f));
            var background = canvasObject.GetComponent<Image>();
            if (background != null)
                background.raycastTarget = false;

            cardInfoImage = CreateRawImage("CardFace", cardInfoRect, new Vector2(32f, -34f), new Vector2(390f, 546f));
            cardInfoNameText = CreateText("Name", cardInfoRect, new Vector2(454f, -34f), new Vector2(690f, 118f), 46f, TextAlignmentOptions.TopLeft);
            cardInfoMetaText = CreateText("Meta", cardInfoRect, new Vector2(454f, -166f), new Vector2(690f, 122f), 34f, TextAlignmentOptions.TopLeft);
            cardInfoDescriptionText = CreateText("Description", cardInfoRect, new Vector2(454f, -318f), new Vector2(690f, 500f), 31f, TextAlignmentOptions.TopLeft);
            cardInfoDescriptionText.enableWordWrapping = true;
            cardInfoDescriptionText.overflowMode = TextOverflowModes.Truncate;
            canvasObject.SetActive(false);
        }

        private void EnsureOptionPanel()
        {
            if (optionCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestDuelOptionPanel", out optionCanvas, out optionRect);
            optionRect.sizeDelta = new Vector2(980f, 620f);
            AddPanelBackground(canvasObject, new Color(0.012f, 0.017f, 0.024f, 0.92f));
            optionTitleText = CreateText("Title", optionRect, new Vector2(34f, -30f), new Vector2(912f, 58f), 38f, TextAlignmentOptions.Left);
            optionBodyText = CreateText("Body", optionRect, new Vector2(34f, -98f), new Vector2(912f, 104f), 27f, TextAlignmentOptions.TopLeft);
            optionBodyText.enableWordWrapping = true;
            optionListRoot = CreateRect("List", optionRect, new Vector2(34f, -214f), new Vector2(912f, 360f), new Vector2(0f, 1f));
            canvasObject.SetActive(false);
        }

        private void EnsurePhaseMenu()
        {
            if (phaseMenuCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestDuelPhaseMenu", out phaseMenuCanvas, out phaseMenuRect);
            phaseMenuRect.sizeDelta = new Vector2(680f, 440f);
            AddPanelBackground(canvasObject, new Color(0.015f, 0.019f, 0.026f, 0.96f));
            phaseMenuTitleText = CreateText("Title", phaseMenuRect, new Vector2(30f, -28f), new Vector2(620f, 52f), 34f, TextAlignmentOptions.Center);
            phaseMenuListRoot = CreateRect("List", phaseMenuRect, new Vector2(40f, -104f), new Vector2(600f, 300f), new Vector2(0f, 1f));
            canvasObject.SetActive(false);
        }

        private void EnsurePhaseHud()
        {
            if (phaseHudCanvas == null)
            {
                var canvasObject = CreateCanvasObject("QuestDuelPhaseHud", out phaseHudCanvas, out phaseHudRect);
                phaseHudRect.sizeDelta = new Vector2(850f, 290f);
                AddPanelBackground(canvasObject, new Color(0.010f, 0.014f, 0.020f, 0.92f));
                lifeHudText = CreateText("LifeText", phaseHudRect, new Vector2(34f, -24f), new Vector2(390f, 220f), 48f, TextAlignmentOptions.Left);
                phaseHudText = CreateText("PhaseText", phaseHudRect, new Vector2(455f, -34f), new Vector2(340f, 190f), 38f, TextAlignmentOptions.Left);
                canvasObject.SetActive(false);
            }

            if (controlHudCanvas == null)
            {
                var controlObject = CreateCanvasObject("QuestDuelControlHud", out controlHudCanvas, out controlHudRect);
                controlHudRect.sizeDelta = new Vector2(780f, 430f);
                AddPanelBackground(controlObject, new Color(0.010f, 0.014f, 0.020f, 0.94f));
                phaseHudButtonRoot = CreateRect("PhaseButtons", controlHudRect, new Vector2(34f, -34f), new Vector2(712f, 160f), new Vector2(0f, 1f));
                systemHudButtonRoot = CreateRect("SystemButtons", controlHudRect, new Vector2(34f, -226f), new Vector2(712f, 150f), new Vector2(0f, 1f));
                controlObject.SetActive(false);
            }
        }

        private void UpdatePhaseHud()
        {
            var core = Program.instance?.ocgcore;
            if (core == null || Program.instance.currentServant != core)
            {
                if (phaseHudCanvas != null && phaseHudCanvas.gameObject.activeSelf)
                    phaseHudCanvas.gameObject.SetActive(false);
                if (controlHudCanvas != null && controlHudCanvas.gameObject.activeSelf)
                    controlHudCanvas.gameObject.SetActive(false);
                if (duelLogCanvas != null && duelLogCanvas.gameObject.activeSelf)
                    duelLogCanvas.gameObject.SetActive(false);
                lastPhaseHudSignature = null;
                lastDuelLogSignature = null;
                return;
            }

            EnsurePhaseHud();
            EnsureDuelLogPanel();
            var canBattle = PhaseButtonHandler.battlePhase;
            var canMain2 = PhaseButtonHandler.main2Phase;
            var canEnd = PhaseButtonHandler.endPhase;
            if (lifeHudText != null)
                lifeHudText.text = "\u6211\u65b9 LP " + Mathf.Max(0, core.life0) + "\n\u5bf9\u65b9 LP " + Mathf.Max(0, core.life1);

            var signature = core.phase + "|" + canBattle + "|" + canMain2 + "|" + canEnd + "|" + core.myTurn + "|" + core.life0 + "|" + core.life1;
            if (signature != lastPhaseHudSignature)
            {
                lastPhaseHudSignature = signature;
                ClearRows(phaseHudRows);
                phaseHudText.text = (core.myTurn ? "我方回合" : "对方回合") + "\n" + GetPhaseName(core.phase);
                phaseHudText.text = LocalizeQuestLabel(phaseHudText.text);
                var index = 0;
                if (canBattle)
                    AddPhaseHudButton(index++, "战斗阶段", () => SendPhaseResponse(6));
                if (canMain2)
                    AddPhaseHudButton(index++, "主要阶段2", () => SendPhaseResponse(2));
                if (canEnd)
                    AddPhaseHudButton(index++, "结束阶段", () =>
                    {
                        var response = core.phase == DuelPhase.BattleStart ? 3 : 7;
                        SendPhaseResponse(response);
                    });
                AddSystemHudButtons();
            }

            if (!phaseHudCanvas.gameObject.activeSelf)
                phaseHudCanvas.gameObject.SetActive(true);
            if (controlHudCanvas != null && !controlHudCanvas.gameObject.activeSelf)
                controlHudCanvas.gameObject.SetActive(true);

            UpdateDuelLogPanel(core);
        }

        private void EnsureDuelLogPanel()
        {
            if (duelLogCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestDuelLogPanel", out duelLogCanvas, out duelLogRect);
            duelLogRect.sizeDelta = new Vector2(980f, 500f);
            AddPanelBackground(canvasObject, new Color(0.010f, 0.014f, 0.019f, 0.92f));
            var background = canvasObject.GetComponent<Image>();
            if (background != null)
                background.raycastTarget = false;

            duelLogTitleText = CreateText("Title", duelLogRect, new Vector2(32f, -22f), new Vector2(916f, 60f), 36f, TextAlignmentOptions.Left);
            duelLogBodyText = CreateText("Body", duelLogRect, new Vector2(32f, -94f), new Vector2(916f, 370f), 32f, TextAlignmentOptions.TopLeft);
            duelLogBodyText.enableWordWrapping = true;
            duelLogBodyText.overflowMode = TextOverflowModes.Truncate;
            duelLogTitleText.text = "\u51b3\u6597\u4fe1\u606f";
            canvasObject.SetActive(false);
            Debug.Log("Quest duel log panel created.");
        }

        private void UpdateDuelLogPanel(OcgCore core)
        {
            if (core == null || duelLogCanvas == null)
                return;

            var phaseText = LocalizeQuestLabel(GetPhaseName(core.phase));
            var turnText = core.myTurn ? "\u6211\u65b9\u56de\u5408" : "\u5bf9\u65b9\u56de\u5408";
            var messageText = LocalizeQuestMessage(core.currentMessage);
            var logText = LocalizeQuestLabel(SanitizeText(OcgCore.lastDuelLog));
            if (string.IsNullOrWhiteSpace(logText))
                logText = "\u7b49\u5f85\u5bf9\u5c40\u4fe1\u606f";

            var signature = turnText + "|" + phaseText + "|" + messageText + "|" + logText;
            if (signature != lastDuelLogSignature)
            {
                lastDuelLogSignature = signature;
                if (duelLogTitleText != null)
                    duelLogTitleText.text = "\u51b3\u6597\u4fe1\u606f";
                if (duelLogBodyText != null)
                {
                    duelLogBodyText.text =
                        "\u56de\u5408: " + turnText + "\n"
                        + "\u9636\u6bb5: " + phaseText + "\n"
                        + "\u72b6\u6001: " + messageText + "\n"
                        + "\u8bb0\u5f55: " + logText;
                }
            }

            if (!duelLogCanvas.gameObject.activeSelf)
            {
                duelLogCanvas.gameObject.SetActive(true);
                if (!duelLogPanelLogged)
                {
                    duelLogPanelLogged = true;
                    Debug.LogFormat(
                        "Quest duel log panel visible. Message={0}, Phase={1}, Log={2}",
                        core.currentMessage,
                        core.phase,
                        logText);
                }
            }
        }

        private void AddPhaseMenuButton(string label, bool interactable, Action onClick)
        {
            var index = phaseMenuRows.Count;
            var row = CreateButton(
                "Phase_" + index,
                phaseMenuListRoot,
                new Vector2(0f, -index * 94f),
                new Vector2(600f, 74f),
                label,
                onClick,
                interactable ? new Color(0.10f, 0.32f, 0.46f, 0.98f) : new Color(0.12f, 0.13f, 0.15f, 0.72f),
                new Vector2(0f, 1f));
            row.interactable = interactable;
            phaseMenuRows.Add(row.gameObject);
        }

        private void AddPhaseHudButton(int index, string label, Action onClick)
        {
            var row = CreateButton(
                "HudPhase_" + index,
                phaseHudButtonRoot,
                new Vector2(index * 238f, -4f),
                new Vector2(220f, 132f),
                label,
                onClick,
                new Color(0.10f, 0.32f, 0.46f, 0.98f),
                new Vector2(0f, 1f));
            phaseHudRows.Add(row.gameObject);
        }

        private void AddSystemHudButtons()
        {
            var surrender = CreateButton(
                "HudSurrender",
                systemHudButtonRoot,
                new Vector2(0f, -4f),
                new Vector2(330f, 124f),
                "\u6295\u964d",
                RequestSurrender,
                new Color(0.48f, 0.16f, 0.18f, 0.98f),
                new Vector2(0f, 1f));
            phaseHudRows.Add(surrender.gameObject);

            var exit = CreateButton(
                "HudExit",
                systemHudButtonRoot,
                new Vector2(356f, -4f),
                new Vector2(330f, 124f),
                "\u9000\u51fa",
                RequestExitDuel,
                new Color(0.22f, 0.23f, 0.28f, 0.98f),
                new Vector2(0f, 1f));
            phaseHudRows.Add(exit.gameObject);
        }

        private void RequestSurrender()
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return;

            var selections = new List<string> { "投降", "确定要投降吗？", "确定", "取消" };
            ShowYesOrNo(selections, () =>
            {
                var localServerDuel = UsesLocalYgoServer();
                core.surrendered = true;
                if (!localServerDuel && TcpHelper.tcpClient != null && TcpHelper.tcpClient.Connected)
                {
                    TcpHelper.CtosMessage_Surrender();
                    if (Room.mode == 2 && !core.tagSurrendered)
                        MessageManager.Cast(InterString.Get("队友投降了。"));
                }
                ReturnDuelToMainMenu(core, "surrender", localServerDuel);
            }, null);
        }

        private void RequestExitDuel()
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return;

            var selections = new List<string> { "退出决斗", "离开当前决斗？", "退出", "取消" };
            ShowYesOrNo(selections, () =>
            {
                var localServerDuel = UsesLocalYgoServer();
                if (!localServerDuel)
                    TcpHelper.CtosMessage_LeaveGame();
                ReturnDuelToMainMenu(core, "leave", localServerDuel);
            }, null);
        }

        private static bool UsesLocalYgoServer()
        {
            return Room.fromSolo || Room.fromLocalHost || YgoServer.ServerRunning();
        }

        private static void ReturnDuelToMainMenu(OcgCore core, string reason, bool localServerDuel = false)
        {
            var program = Program.instance;
            if (program == null)
                return;

            Debug.LogFormat("Quest duel return to main menu requested. Reason={0}, LocalServer={1}", reason, localServerDuel);

            try
            {
                if (program.currentSubServant != null)
                {
                    program.currentSubServant.Hide(-1);
                    program.currentSubServant = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Quest duel sub-servant cleanup failed: " + ex.Message);
            }

            Room.fromSolo = false;
            Room.fromLocalHost = false;
            Room.needSide = false;
            Room.sideWaitingObserver = false;

            if (core != null)
            {
                core.returnAction = null;
                core.returnServant = program.menu;
                if (localServerDuel)
                {
                    TcpHelper.DetachQuestLocalClientWithoutDisconnect();
                    program.ShiftToServant(program.menu);
                }
                else
                {
                    core.OnExit();
                }
            }
            else if (program.menu != null)
            {
                program.ShiftToServant(program.menu);
            }

            if (!localServerDuel)
            {
                try
                {
                    YgoServer.StopServer();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Quest duel server cleanup failed: " + ex.Message);
                }
            }

            QuestXrBootstrap.NotifyQuestDuelReturnedToMainMenu();
        }

        private void AddOptionButton(string label, Action onClick, Color? color = null)
        {
            var index = optionRows.Count;
            var row = CreateButton(
                "Option_" + index,
                optionListRoot,
                new Vector2(0f, -index * 82f),
                new Vector2(912f, 66f),
                label,
                onClick,
                color ?? new Color(0.10f, 0.27f, 0.38f, 0.98f),
                new Vector2(0f, 1f));
            optionRows.Add(row.gameObject);
        }

        private void AddPositionOption(int response)
        {
            string label;
            switch (response)
            {
                case 1:
                    label = "表侧攻击表示";
                    break;
                case 2:
                    label = "里侧攻击表示";
                    break;
                case 4:
                    label = "表侧守备表示";
                    break;
                default:
                    response = 8;
                    label = "里侧守备表示";
                    break;
            }

            var capturedResponse = response;
            AddOptionButton(label, () =>
            {
                SendIntResponse(capturedResponse);
                HideOptionPanel();
            });
        }

        private void RebuildCardGrid()
        {
            EnsureCardPanel();
            ClearRows(cardGridItems);
            cardItemBackgrounds.Clear();
            var start = cardPage * CardGridPageSize;
            var end = Mathf.Min(cards.Count, start + CardGridPageSize);
            for (var index = start; index < end; index += 1)
            {
                var local = index - start;
                var column = local % 4;
                var row = local / 4;
                var card = cards[index];
                var item = CreateCardGridItem(card, column, row);
                cardGridItems.Add(item);
            }

            UpdateCardButtons();
        }

        private GameObject CreateCardGridItem(GameCard card, int column, int row)
        {
            var item = new GameObject("CardItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            SetQuestOverlayLayer(item);
            item.transform.SetParent(cardGridRoot, false);

            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(238f, 222f);
            rect.anchoredPosition = new Vector2(column * 258f, -row * 246f);

            var image = item.GetComponent<Image>();
            image.color = new Color(0.05f, 0.064f, 0.078f, 0.98f);
            image.raycastTarget = true;
            cardItemBackgrounds[card] = image;

            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnCardGridItemClicked(card));

            var face = CreateRawImage("Face", rect, new Vector2(14f, -12f), new Vector2(112f, 156f));
            var name = CreateText("Name", rect, new Vector2(136f, -16f), new Vector2(86f, 118f), 18f, TextAlignmentOptions.TopLeft);
            name.enableWordWrapping = true;
            name.overflowMode = TextOverflowModes.Truncate;
            name.text = GetCardName(card);
            var meta = CreateText("Meta", rect, new Vector2(136f, -142f), new Vector2(86f, 58f), 16f, TextAlignmentOptions.TopLeft);
            meta.enableWordWrapping = true;
            meta.text = GetShortCardMeta(card);
            StartCoroutine(LoadCardTexture(face, card == null ? 0 : card.GetData().Id));
            return item;
        }

        private void OnCardGridItemClicked(GameCard card)
        {
            if (card == null)
                return;

            AudioManager.PlaySE("SE_MENU_SELECT_01");
            ShowCardDetail(card);
            if (cardReadOnly)
            {
                UpdateCardButtons();
                return;
            }

            if (IsForcedSelectedCard(card))
            {
                UpdateCardButtons();
                return;
            }

            if (selectedCards.Contains(card))
            {
                selectedCards.Remove(card);
                selectedOrder.Remove(card);
            }
            else
            {
                if (cardMax == 1)
                {
                    selectedCards.Clear();
                    selectedOrder.Clear();
                }
                else if (cardMax > 0 && selectedCards.Count >= cardMax)
                {
                    return;
                }

                selectedCards.Add(card);
                selectedOrder.Add(card);
            }

            UpdateCardButtons();
        }

        private void ShowCardDetail(GameCard card)
        {
            detailCard = card;
            if (card == null || card.GetData() == null)
            {
                detailNameText.text = "无卡片";
                detailMetaText.text = string.Empty;
                detailDescriptionText.text = string.Empty;
                detailImage.texture = null;
                return;
            }

            var data = card.GetData();
            detailNameText.text = SanitizeText(data.Name);
            detailMetaText.text = GetLongCardMeta(data);
            detailDescriptionText.text = SanitizeText(data.GetDescription(true));
            StartCoroutine(LoadCardTexture(detailImage, data.Id));
        }

        private void UpdateCardButtons()
        {
            if (cardPanelCanvas == null)
                return;

            var maxPage = Mathf.Max(0, (cards.Count - 1) / CardGridPageSize);
            cardTitleText.text = cardHint;
            cardCountText.text = cardReadOnly ? cards.Count + " 张" : selectedCards.Count + "/" + Mathf.Max(cardMax, cardMin);
            cardPageText.text = (cards.Count == 0 ? 0 : cardPage + 1) + "/" + (maxPage + 1);
            cardPreviousButton.interactable = cardPage > 0;
            cardNextButton.interactable = cardPage < maxPage;
            cardCancelButton.gameObject.SetActive(!cardReadOnly && cardExitable);
            cardFinishButton.gameObject.SetActive(cardReadOnly);
            cardConfirmButton.gameObject.SetActive(!cardReadOnly);
            cardConfirmButton.interactable = CanConfirmCardSelection();

            foreach (var pair in cardItemBackgrounds)
            {
                var card = pair.Key;
                var image = pair.Value;
                if (image == null)
                    continue;

                if (card == detailCard)
                    image.color = new Color(0.12f, 0.28f, 0.38f, 0.98f);
                else
                    image.color = new Color(0.05f, 0.064f, 0.078f, 0.98f);

                if (selectedCards.Contains(card))
                    image.color = new Color(0.16f, 0.46f, 0.30f, 0.98f);
                if (IsForcedSelectedCard(card))
                    image.color = new Color(0.40f, 0.30f, 0.12f, 0.98f);
            }
        }

        private bool CanConfirmCardSelection()
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return false;

            if (core.currentMessage == GameMessage.SelectSum)
            {
                var list = new List<GameCard>(selectedOrder);
                var sum = OcgCore.GetSelectLevelSum(list);
                if (core.ES_overFlow)
                    return core.ES_level <= sum[0] || core.ES_level <= sum[1];
                return core.ES_level == sum[0] || core.ES_level == sum[1];
            }

            return selectedCards.Count >= cardMin && (cardMax <= 0 || selectedCards.Count <= cardMax);
        }

        private void ConfirmCardSelection()
        {
            var core = Program.instance?.ocgcore;
            if (core == null || !CanConfirmCardSelection())
                return;

            foreach (var card in selectedOrder)
            {
                if (card != null && card.GetData() != null)
                {
                    core.lastSelectedCard = card.GetData().Id;
                    break;
                }
            }

            switch (core.currentMessage)
            {
                case GameMessage.SelectEffectYn:
                    SendIntResponse(1);
                    break;
                case GameMessage.SelectChain:
                    ConfirmChainSelection();
                    break;
                case GameMessage.SelectUnselect:
                case GameMessage.SelectTribute:
                case GameMessage.SelectSum:
                case GameMessage.SelectCard:
                    ConfirmSelectCardPointers();
                    break;
                case GameMessage.SelectIdleCmd:
                case GameMessage.SelectBattleCmd:
                    ConfirmIdleOrBattleCard();
                    break;
                case GameMessage.AnnounceCard:
                    ConfirmAnnounceCard();
                    break;
                case GameMessage.SortCard:
                case GameMessage.SortChain:
                    ConfirmSortCards();
                    break;
            }

            AudioManager.PlaySE("SE_DUEL_DECIDE");
            core.Sleep(35);
            HideCardPanel();
        }

        private void ConfirmChainSelection()
        {
            var card = selectedOrder.Count > 0 ? selectedOrder[0] : null;
            if (card == null)
                return;

            if (card.effects.Count == 1)
            {
                SendIntResponse(card.effects[0].ptr);
                return;
            }

            ShowEffectSelection(card, false);
        }

        private void ConfirmSelectCardPointers()
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return;

            var packet = new BinaryMaster();
            if (core.currentMessage == GameMessage.SelectUnselect && selectedOrder.Count == 0)
            {
                packet.writer.Write(-1);
            }
            else
            {
                packet.writer.Write((byte)selectedOrder.Count);
                foreach (var card in selectedOrder)
                    packet.writer.Write((byte)card.selectPtr);
            }
            core.SendReturn(packet.Get());
        }

        private void ConfirmIdleOrBattleCard()
        {
            var card = selectedOrder.Count > 0 ? selectedOrder[0] : null;
            if (card == null)
                return;

            var activateButton = FindButton(card, ButtonType.Activate);
            var shouldActivate = activateButton.response != null
                && (card.buttons.Count == 1 || IsActivationHint(cardHint));
            if (shouldActivate)
            {
                if (card.effects.Count == 1)
                {
                    SendIntResponse(card.effects[0].ptr);
                    return;
                }

                ShowEffectSelection(card, true);
                return;
            }

            var actionButton = FindFirstActionButton(card);
            if (actionButton.response != null && actionButton.response.Count > 0)
            {
                SendIntResponse(actionButton.response[0]);
                return;
            }

            if (activateButton.response != null && activateButton.response.Count > 0)
                SendIntResponse(activateButton.response[0]);
        }

        private static bool IsActivationHint(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return false;

            return hint.Contains("效果")
                || hint.Contains("发动")
                || hint.Contains("\u6548\u679c")
                || hint.Contains("\u53d1\u52a8")
                || hint.IndexOf("effect", StringComparison.OrdinalIgnoreCase) >= 0
                || hint.IndexOf("activate", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameCard.DuelButtonInfo FindFirstActionButton(GameCard card)
        {
            foreach (var type in new[]
            {
                ButtonType.SpSummon,
                ButtonType.Summon,
                ButtonType.PenSummon,
                ButtonType.Battle,
                ButtonType.ToAttackPosition,
                ButtonType.ToDefensePosition,
                ButtonType.SetMonster,
                ButtonType.SetSpell,
                ButtonType.SetPendulum
            })
            {
                var button = FindButton(card, type);
                if (button.response != null && button.response.Count > 0)
                    return button;
            }

            return default;
        }

        private static GameCard.DuelButtonInfo FindButton(GameCard card, ButtonType type)
        {
            if (card == null || card.buttons == null)
                return default;

            foreach (var button in card.buttons)
                if (button.type == type && button.response != null && button.response.Count > 0)
                    return button;
            return default;
        }

        private void ConfirmAnnounceCard()
        {
            var core = Program.instance?.ocgcore;
            var card = selectedOrder.Count > 0 ? selectedOrder[0] : null;
            if (core == null || card == null || card.GetData() == null)
                return;

            var packet = new BinaryMaster();
            packet.writer.Write(card.GetData().Id);
            core.SendReturn(packet.Get());
            core.ClearAnnounceCards();
        }

        private void ConfirmSortCards()
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return;

            var bytes = new byte[cards.Count];
            for (var i = 0; i < cards.Count; i += 1)
            {
                var order = selectedOrder.IndexOf(cards[i]);
                bytes[i] = (byte)Mathf.Max(order, 0);
            }

            var packet = new BinaryMaster();
            packet.writer.Write(bytes);
            core.SendReturn(packet.Get());
        }

        private void CancelCardSelection()
        {
            var core = Program.instance?.ocgcore;
            if (core == null || !cardExitable)
                return;

            AudioManager.PlaySE("SE_DUEL_CANCEL");
            switch (core.currentMessage)
            {
                case GameMessage.SelectEffectYn:
                    SendIntResponse(0);
                    break;
                case GameMessage.AnnounceCard:
                    core.ClearAnnounceCards();
                    break;
                case GameMessage.SelectIdleCmd:
                case GameMessage.SelectBattleCmd:
                    break;
                default:
                    SendIntResponse(-1);
                    break;
            }

            core.Sleep(20);
            HideCardPanel();
        }

        private void FinishCardSelection()
        {
            AudioManager.PlaySE("SE_DUEL_DECIDE");
            var core = Program.instance?.ocgcore;
            if (core != null)
                core.Sleep(20);
            OcgCore.messagePass = true;
            HideCardPanel();
        }

        private void ShowEffectSelection(GameCard card, bool includeCancel)
        {
            var selections = new List<string> { "效果选择" };
            var responses = new List<int>();
            for (var i = 0; i < card.effects.Count; i += 1)
            {
                var desc = card.effects[i].desc;
                if (string.IsNullOrWhiteSpace(desc) || desc.Length <= 2)
                    desc = InterString.Get("发动效果");
                selections.Add(desc);
                responses.Add(card.effects[i].ptr);
            }
            if (includeCancel)
            {
                selections.Add(InterString.Get("放弃"));
                responses.Add(-233);
            }
            ShowSelectionNearCard(selections, responses, card);
        }

        private void SendPhaseResponse(int response)
        {
            SendIntResponse(response);
            PhaseButtonHandler.battlePhase = false;
            PhaseButtonHandler.main2Phase = false;
            PhaseButtonHandler.endPhase = false;
            HidePhaseMenu();
            lastPhaseHudSignature = null;
        }

        private void SendIntResponse(int response)
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return;

            var packet = new BinaryMaster();
            packet.writer.Write(response);
            core.SendReturn(packet.Get());
        }

        private void PreselectForcedCardsForSum()
        {
            var core = Program.instance?.ocgcore;
            if (core == null || core.currentMessage != GameMessage.SelectSum || core.cardsMustBeSelected == null)
                return;

            foreach (var forced in core.cardsMustBeSelected)
            {
                if (forced == null)
                    continue;
                foreach (var card in cards)
                {
                    if (card != null && card.md5 == forced.md5)
                    {
                        selectedCards.Add(card);
                        if (!selectedOrder.Contains(card))
                            selectedOrder.Add(card);
                    }
                }
            }
        }

        private bool IsForcedSelectedCard(GameCard card)
        {
            var core = Program.instance?.ocgcore;
            if (core == null || core.currentMessage != GameMessage.SelectSum || core.cardsMustBeSelected == null || card == null)
                return false;

            foreach (var forced in core.cardsMustBeSelected)
                if (forced != null && forced.md5 == card.md5)
                    return true;
            return false;
        }

        private IEnumerator LoadCardTexture(RawImage target, int code)
        {
            if (target == null)
                yield break;

            target.texture = TextureManager.container == null ? null : TextureManager.container.unknownCard.texture;
            if (code <= 0)
                yield break;

            var task = TextureManager.LoadQuestFieldCardTextureAsync(code, true);
            while (!task.IsCompleted)
                yield return null;

            if (target != null && task.Result != null)
                target.texture = task.Result;
        }

        private void HideCardPanel()
        {
            if (cardPanelCanvas != null && cardPanelCanvas.gameObject.activeSelf)
                cardPanelCanvas.gameObject.SetActive(false);
        }

        private void HideCardInfoPanel()
        {
            if (cardInfoCanvas != null && cardInfoCanvas.gameObject.activeSelf)
                cardInfoCanvas.gameObject.SetActive(false);
        }

        private void HideOptionPanel()
        {
            if (optionCanvas != null && optionCanvas.gameObject.activeSelf)
                optionCanvas.gameObject.SetActive(false);
            optionAnchorCard = null;
        }

        private void HidePhaseMenu()
        {
            if (phaseMenuCanvas != null && phaseMenuCanvas.gameObject.activeSelf)
                phaseMenuCanvas.gameObject.SetActive(false);
        }

        private void UpdatePanelPoses()
        {
            if (xrCamera == null)
                return;

            if (cardPanelRect != null && cardPanelCanvas.gameObject.activeSelf)
                PlacePanel(cardPanelRect, DuelWorldCenterOnGround + new Vector3(0f, 9.8f, -24f), PanelScale);
            if (cardInfoRect != null && cardInfoCanvas.gameObject.activeSelf)
                PlacePanel(cardInfoRect, DuelWorldCenterOnGround + new Vector3(30f, 9.8f, -8f), CardInfoScale);
            if (optionRect != null && optionCanvas.gameObject.activeSelf)
                PlacePanel(optionRect, ResolveOptionPanelPosition(), SmallPanelScale);
            if (phaseMenuRect != null && phaseMenuCanvas.gameObject.activeSelf)
                PlacePanel(phaseMenuRect, DuelWorldCenterOnGround + new Vector3(0f, 8.4f, -18f), SmallPanelScale);
            if (phaseHudRect != null && phaseHudCanvas.gameObject.activeSelf)
                PlacePanel(phaseHudRect, DuelWorldCenterOnGround + new Vector3(72f, 24.2f, -11f), FloorHudScale);
            if (controlHudRect != null && controlHudCanvas.gameObject.activeSelf)
                PlacePanel(controlHudRect, DuelWorldCenterOnGround + new Vector3(34f, 12.8f, -44f), FloorHudScale);
            if (duelLogRect != null && duelLogCanvas.gameObject.activeSelf)
                PlacePanel(duelLogRect, DuelWorldCenterOnGround + new Vector3(72f, 16.2f, 2f), DuelLogPanelScale);
        }

        private Vector3 ResolveOptionPanelPosition()
        {
            if (optionAnchorCard == null || optionAnchorCard.p == null)
                return DuelWorldCenterOnGround + new Vector3(0f, 7.2f, -20f);

            var position = GameCard.GetCardPosition(optionAnchorCard.p, optionAnchorCard, optionAnchorCard.overlayParent);
            position.x *= QuestBoardScaleX;
            position.z *= QuestBoardScaleZ;
            position.y = Mathf.Max(position.y + 5.4f, 5.8f);
            position += Vector3.back * 4.8f;
            return DuelWorldCenterOnGround + position;
        }

        private void PlacePanel(RectTransform rect, Vector3 position, float scale)
        {
            if (duelWorldAnchor != null && rect.parent == duelWorldAnchor)
            {
                rect.localPosition = position - DuelWorldCenterOnGround;
                rect.localRotation = Quaternion.identity;
            }
            else
            {
                rect.position = position;
                rect.rotation = Quaternion.identity;
            }
            rect.localScale = Vector3.one * scale;
        }

        private GameObject CreateCanvasObject(string name, out Canvas canvas, out RectTransform rect)
        {
            var canvasObject = new GameObject(name, typeof(RectTransform));
            SetQuestOverlayLayer(canvasObject);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = WorldCanvasDynamicPixelsPerUnit;
            scaler.referencePixelsPerUnit = 100f;

            var raycaster = canvasObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            rect = canvasObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            if (duelWorldAnchor != null)
                canvasObject.transform.SetParent(duelWorldAnchor, false);
            return canvasObject;
        }

        private static void AddPanelBackground(GameObject canvasObject, Color color)
        {
            var image = canvasObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
        }

        private static void AddRectBackground(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Vector2 anchor)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            SetQuestOverlayLayer(obj);
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
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
            text.text = string.Empty;
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

        private static RawImage CreateRawImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            SetQuestOverlayLayer(obj);
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = obj.GetComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            string label,
            Action onClick,
            Color? color = null,
            Vector2? anchorOverride = null)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            SetQuestOverlayLayer(obj);
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            var anchor = anchorOverride ?? new Vector2(0f, 1f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = obj.GetComponent<Image>();
            image.color = color ?? new Color(0.10f, 0.27f, 0.38f, 0.98f);
            image.raycastTarget = true;

            var button = obj.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f),
                pressedColor = new Color(0.78f, 0.90f, 1f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.28f, 0.28f, 0.28f, 0.65f),
                colorMultiplier = 1f,
                fadeDuration = 0.04f
            };
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            var text = CreateText("Label", rect, new Vector2(12f, -8f), new Vector2(size.x - 24f, size.y - 16f), 25f, TextAlignmentOptions.Center);
            text.text = LocalizeQuestLabel(label);
            return button;
        }

        private static string LocalizeQuestLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return string.Empty;

            return label
                .Replace("\u93B4\u621E\u67DF", "\u6211\u65b9")
                .Replace("\u7035\u89C4\u67DF", "\u5bf9\u65b9")
                .Replace("\u93B6\u66E2\u6AB7", "\u6295\u964d")
                .Replace("\u934F\u62BD\u68F4", "\u5173\u95ed")
                .Replace("\u7487\u70FD\u20AC\u590B\u5AE8", "\u8bf7\u9009\u62e9")
                .Replace("\u7EAD\uE1BF\uE17B", "\u786e\u8ba4")
                .Replace("\u7EAD\uE1BC\u757E", "\u786e\u5b9a")
                .Replace("\u9359\u6828\u79F7", "\u53d6\u6d88")
                .Replace("\u93C1\u581F\u7049\u95AB\u590B\u5AE8", "\u6548\u679c\u9009\u62e9")
                .Replace("\u9359\u621D\u59E9\u93C1\u581F\u7049", "\u53d1\u52a8\u6548\u679c")
                .Replace("\u93C0\u60E7\u7D14", "\u653e\u5f03")
                .Replace("\u95C3\u8235\uE18C\u9352\u56E8\u5D32", "\u9636\u6bb5\u5207\u6362")
                .Replace("\u93B4\u6A3B\u679F\u95C3\u8235\uE18C", "\u6218\u6597\u9636\u6bb5")
                .Replace("\u6D93\u660F\uE6E6\u95C3\u8235\uE18C\u0031", "\u4e3b\u8981\u9636\u6bb51")
                .Replace("\u6D93\u660F\uE6E6\u95C3\u8235\uE18C\u0032", "\u4e3b\u8981\u9636\u6bb52")
                .Replace("\u7F01\u64B4\u6F6B\u95C3\u8235\uE18C", "\u7ed3\u675f\u9636\u6bb5")
                .Replace("\u93B4\u621E\u67DF\u9365\u70B2\u608E", "\u6211\u65b9\u56de\u5408")
                .Replace("\u7035\u89C4\u67DF\u9365\u70B2\u608E", "\u5bf9\u65b9\u56de\u5408")
                .Replace("\u93B6\u85C9\u5D31\u95C3\u8235\uE18C", "\u62bd\u5361\u9636\u6bb5")
                .Replace("\u9351\u55D7\uE62C\u95C3\u8235\uE18C", "\u51c6\u5907\u9636\u6bb5");
        }

        private static string LocalizeQuestMessage(GameMessage message)
        {
            switch (message)
            {
                case GameMessage.Waiting:
                    return "\u7b49\u5f85";
                case GameMessage.Start:
                    return "\u51b3\u6597\u5f00\u59cb";
                case GameMessage.SelectBattleCmd:
                    return "\u8bf7\u9009\u62e9\u6218\u6597\u64cd\u4f5c";
                case GameMessage.SelectIdleCmd:
                    return "\u8bf7\u9009\u62e9\u64cd\u4f5c";
                case GameMessage.SelectEffectYn:
                    return "\u662f\u5426\u53d1\u52a8\u6548\u679c";
                case GameMessage.SelectYesNo:
                    return "\u8bf7\u9009\u62e9\u662f/\u5426";
                case GameMessage.SelectOption:
                    return "\u8bf7\u9009\u62e9\u9009\u9879";
                case GameMessage.SelectCard:
                    return "\u8bf7\u9009\u62e9\u5361\u7247";
                case GameMessage.SelectChain:
                    return "\u8bf7\u9009\u62e9\u8fde\u9501";
                case GameMessage.SelectPlace:
                    return "\u8bf7\u9009\u62e9\u533a\u57df";
                case GameMessage.SelectPosition:
                    return "\u8bf7\u9009\u62e9\u8868\u793a\u5f62\u5f0f";
                case GameMessage.SelectTribute:
                    return "\u8bf7\u9009\u62e9\u89e3\u653e\u7d20\u6750";
                case GameMessage.SortChain:
                    return "\u8bf7\u6392\u5217\u8fde\u9501";
                case GameMessage.SelectCounter:
                    return "\u8bf7\u9009\u62e9\u6307\u793a\u7269";
                case GameMessage.SelectSum:
                    return "\u8bf7\u9009\u62e9\u7d20\u6750";
                case GameMessage.SelectDisfield:
                    return "\u8bf7\u9009\u62e9\u65e0\u6548\u533a\u57df";
                case GameMessage.SortCard:
                    return "\u8bf7\u6392\u5217\u5361\u7247";
                case GameMessage.SelectUnselect:
                    return "\u8bf7\u9009\u62e9/\u53d6\u6d88\u9009\u62e9";
                case GameMessage.ConfirmDecktop:
                case GameMessage.ConfirmCards:
                case GameMessage.ConfirmExtratop:
                    return "\u786e\u8ba4\u5361\u7247";
                case GameMessage.NewTurn:
                    return "\u56de\u5408\u5207\u6362";
                case GameMessage.NewPhase:
                    return "\u9636\u6bb5\u5207\u6362";
                case GameMessage.Move:
                    return "\u5361\u7247\u79fb\u52a8";
                case GameMessage.Set:
                    return "\u5361\u7247\u653e\u7f6e";
                case GameMessage.Summoning:
                    return "\u901a\u5e38\u53ec\u5524\u4e2d";
                case GameMessage.Summoned:
                    return "\u901a\u5e38\u53ec\u5524\u6210\u529f";
                case GameMessage.SpSummoning:
                    return "\u7279\u6b8a\u53ec\u5524\u4e2d";
                case GameMessage.SpSummoned:
                    return "\u7279\u6b8a\u53ec\u5524\u6210\u529f";
                case GameMessage.FlipSummoning:
                    return "\u53cd\u8f6c\u53ec\u5524\u4e2d";
                case GameMessage.FlipSummoned:
                    return "\u53cd\u8f6c\u53ec\u5524\u6210\u529f";
                case GameMessage.Chaining:
                    return "\u8fde\u9501\u53d1\u52a8\u4e2d";
                case GameMessage.Chained:
                    return "\u5df2\u8fde\u9501";
                case GameMessage.ChainSolving:
                    return "\u8fde\u9501\u5904\u7406\u4e2d";
                case GameMessage.ChainSolved:
                    return "\u8fde\u9501\u5904\u7406\u5b8c\u6210";
                case GameMessage.ChainEnd:
                    return "\u8fde\u9501\u7ed3\u675f";
                case GameMessage.CardSelected:
                case GameMessage.RandomSelected:
                    return "\u5361\u7247\u5df2\u9009\u62e9";
                case GameMessage.Draw:
                    return "\u62bd\u5361";
                case GameMessage.Damage:
                    return "\u9020\u6210\u4f24\u5bb3";
                case GameMessage.Recover:
                    return "\u751f\u547d\u503c\u6062\u590d";
                case GameMessage.LpUpdate:
                    return "\u751f\u547d\u503c\u66f4\u65b0";
                case GameMessage.Attack:
                    return "\u653b\u51fb\u5ba3\u8a00";
                case GameMessage.Battle:
                    return "\u6218\u6597\u5904\u7406";
                case GameMessage.AttackDisabled:
                    return "\u653b\u51fb\u88ab\u65e0\u6548";
                case GameMessage.ShowHint:
                case GameMessage.PlayerHint:
                case GameMessage.Hint:
                    return "\u63d0\u793a";
                case GameMessage.DuelWinner:
                case GameMessage.Win:
                    return "\u51b3\u6597\u7ed3\u675f";
                default:
                    return message.ToString();
            }
        }

        private static void ClearRows(List<GameObject> rows)
        {
            foreach (var row in rows)
                if (row != null)
                    Destroy(row);
            rows.Clear();
        }

        private static string GetCardName(GameCard card)
        {
            return card == null || card.GetData() == null ? "未知卡片" : SanitizeText(card.GetData().Name);
        }

        private static string GetShortCardMeta(GameCard card)
        {
            var data = card == null ? null : card.GetData();
            if (data == null)
                return string.Empty;
            if (data.HasType(CardType.Monster))
                return "ATK " + data.GetAttackString() + "\nDEF " + data.GetDefenseString();
            if (data.HasType(CardType.Spell))
                return "魔法";
            if (data.HasType(CardType.Trap))
                return "陷阱";
            return string.Empty;
        }

        private static string GetLongCardMeta(Card data)
        {
            if (data == null)
                return string.Empty;
            if (data.HasType(CardType.Monster))
            {
                var levelLabel = data.HasType(CardType.Link) ? "LINK " + data.GetLinkCount() : "等级 " + data.Level;
                return levelLabel + "\nATK " + data.GetAttackString() + " / DEF " + data.GetDefenseString();
            }
            if (data.HasType(CardType.Spell))
                return "魔法卡";
            if (data.HasType(CardType.Trap))
                return "陷阱卡";
            return string.Empty;
        }

        private static string GetPhaseName(DuelPhase phase)
        {
            switch (phase)
            {
                case DuelPhase.Draw:
                    return "抽卡阶段";
                case DuelPhase.Standby:
                    return "准备阶段";
                case DuelPhase.Main1:
                    return "主要阶段1";
                case DuelPhase.BattleStart:
                case DuelPhase.Battle:
                case DuelPhase.BattleStep:
                case DuelPhase.Damage:
                case DuelPhase.DamageCal:
                    return "战斗阶段";
                case DuelPhase.Main2:
                    return "主要阶段2";
                case DuelPhase.End:
                    return "结束阶段";
                default:
                    return phase.ToString();
            }
        }

        private static string GetLocationName(CardLocation location)
        {
            switch (location)
            {
                case CardLocation.Deck:
                    return "主卡组";
                case CardLocation.Extra:
                    return "额外卡组";
                case CardLocation.Grave:
                    return "墓地";
                case CardLocation.Removed:
                    return "除外区";
                case CardLocation.Hand:
                    return "手牌";
                default:
                    return location.ToString();
            }
        }

        private static string GetControllerName(uint controller)
        {
            return controller == 0 ? "我方" : "对方";
        }

        private static string SanitizeText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r", string.Empty);
        }

        private static void SetQuestOverlayLayer(GameObject target)
        {
            if (target == null)
                return;
            SetLayerRecursively(target, GetQuestOverlayLayer());
        }

        private static int GetQuestOverlayLayer()
        {
            var layer = LayerMask.NameToLayer("QuestOverlay");
            return layer >= 0 ? layer : FallbackQuestOverlayLayer;
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
                return;

            target.layer = layer;
            foreach (Transform child in target.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
