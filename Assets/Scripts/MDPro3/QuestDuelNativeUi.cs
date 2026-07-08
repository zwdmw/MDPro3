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
        public static event Action UiVisibilityChanged;

        private const int FallbackQuestOverlayLayer = 24;
        private const float PanelScale = 0.014f;
        private const float SmallPanelScale = 0.016f;
        private const float AnchoredOptionPanelScale = 0.017f;
        private const float DefaultOptionPanelWidth = 980f;
        private const float DefaultOptionPanelHeight = 620f;
        private const float AnchoredOptionPanelWidth = 520f;
        private const float AnchoredOptionPanelBaseHeight = 122f;
        private const float AnchoredOptionPanelButtonWidth = 420f;
        private const float AnchoredOptionPanelButtonHeight = 76f;
        private const float AnchoredOptionPanelButtonGap = 12f;
        private const float HudScale = 0.017f;
        private const float FloorHudScale = 0.041f;
        private const float ControlHudScale = 0.033f;
        private const float CardInfoScale = 0.028f;
        private const float HoverCardInfoScale = 0.0175f;
        private const float SideCardDetailScale = 0.037f;
        private const float DuelLogPanelScale = 0.050f;
        private const float CardSelectorScale = 0.0185f;
        private const float WorldCanvasDynamicPixelsPerUnit = 13f;
        private const float QuestBoardScaleX = 1.38f;
        private const float QuestBoardScaleZ = 1.34f;
        private const int CardGridPageSize = 12;
        private const int CardGridColumns = 6;
        private const float CardGridItemWidth = 236f;
        private const float CardGridItemHeight = 346f;
        private const float CardGridCardWidth = 224f;
        private const float CardGridCardHeight = 315f;
        private const float CardGridColumnSpacing = 238f;
        private const float CardGridRowSpacing = 402f;
        private const float CardGridRowOffsetX = 68f;
        private const float CardGridFanAngle = 7.8f;
        private const float CardGridCurveDrop = 34f;
        private const int MaxPresentationLogLines = 7;
        private const float CardGridFloatAmplitude = 18f;
        private const float CardGridFloatSelectedLift = 46f;
        private const float CardGridFloatDetailLift = 22f;
        private const float CardGridFloatHoverLift = 118f;
        private const float CardGridHoverScaleBoost = 0.34f;
        private static readonly Vector3 DuelWorldCenterOnGround = new Vector3(0f, -0.005f, -1.5f);
        private static readonly Quaternion RightSideWallRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        private static readonly Quaternion LeftSideWallRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        private static readonly Color HudPanelBackground = new Color(0.008f, 0.012f, 0.018f, 0.90f);
        private static readonly Color HudPanelInner = new Color(0.020f, 0.031f, 0.041f, 0.62f);
        private static readonly Color HudAccentCyan = new Color(0.16f, 0.90f, 1f, 0.64f);
        private static readonly Color HudAccentGold = new Color(1f, 0.72f, 0.20f, 0.58f);
        private static readonly Color HudAccentRed = new Color(1f, 0.30f, 0.36f, 0.62f);

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
        private readonly Dictionary<GameCard, QuestFloatingCardGridItem> cardItemFloaters = new Dictionary<GameCard, QuestFloatingCardGridItem>();
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
        private TextMeshProUGUI cardInfoStateText;
        private TextMeshProUGUI cardInfoActionText;
        private TextMeshProUGUI cardInfoDescriptionText;
        private RawImage cardInfoImage;
        private bool cardInfoUseWorldPose;
        private Vector3 cardInfoWorldPosition;
        private Quaternion cardInfoWorldRotation;
        private Canvas cardDetailCanvas;
        private RectTransform cardDetailRect;
        private RawImage cardDetailImage;
        private TextMeshProUGUI cardDetailNameText;
        private TextMeshProUGUI cardDetailMetaText;
        private TextMeshProUGUI cardDetailStateText;
        private TextMeshProUGUI cardDetailDescriptionText;
        private TextMeshProUGUI cardDetailActionTitleText;
        private TextMeshProUGUI cardDetailActionText;
        private GameCard cardDetailHoverCard;
        private GameCard cardDetailPinnedCard;
        private GameCard cardDetailActionRestoreCard;
        private bool cardDetailShowingActionHint;
        private int cardInfoTextureRequestCode;
        private int cardDetailTextureRequestCode;

        private Canvas optionCanvas;
        private RectTransform optionRect;
        private Image optionBackgroundImage;
        private TextMeshProUGUI optionTitleText;
        private TextMeshProUGUI optionBodyText;
        private RectTransform optionListRoot;
        private readonly List<GameObject> optionRows = new List<GameObject>();
        private GameCard optionAnchorCard;
        private bool optionAnchoredCompact;
        private int optionAnchoredCount;

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
        private RectTransform phaseTrackRoot;
        private RectTransform phaseHudButtonRoot;
        private RectTransform systemHudButtonRoot;
        private readonly List<GameObject> phaseHudRows = new List<GameObject>();
        private readonly List<GameObject> phaseTrackRows = new List<GameObject>();
        private string lastPhaseHudSignature;
        private float lifeHudFlashStartTime;
        private float lifeHudFlashDuration;
        private float lifeHudFlashScale;
        private Color lifeHudFlashColor = Color.white;

        private Canvas duelLogCanvas;
        private RectTransform duelLogRect;
        private TextMeshProUGUI duelLogTitleText;
        private TextMeshProUGUI duelLogEventText;
        private TextMeshProUGUI duelLogStatusText;
        private TextMeshProUGUI duelLogPromptText;
        private TextMeshProUGUI duelLogBodyText;
        private Image duelLogEventAccentImage;
        private readonly List<string> presentationLogLines = new List<string>();
        private string lastImportantEventText;
        private Color lastImportantEventColor = HudAccentCyan;
        private float lastImportantEventTime;
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

        private void OnEnable()
        {
            DuelPresentationDirector.EventRaised += HandlePresentationEvent;
        }

        private void OnDisable()
        {
            DuelPresentationDirector.EventRaised -= HandlePresentationEvent;
            ResetDuelLogState();
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
            ReparentCanvas(cardDetailCanvas);
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
            HideCardInfoPanel();
            NotifyUiVisibilityChanged();
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
            optionAnchoredCompact = anchorCard != null && responses.Count <= 5;
            optionAnchorCard = optionAnchoredCompact ? anchorCard : null;
            ConfigureOptionPanelLayout(optionAnchoredCompact, Mathf.Max(1, responses.Count));
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
            HideCardInfoPanel();
            NotifyUiVisibilityChanged();
            UpdatePanelPoses();
            return true;
        }

        public bool ShowYesOrNo(List<string> selections, Action confirmAction, Action cancelAction)
        {
            if (!CanShowDuelUi() || Program.instance?.ocgcore == null || selections == null)
                return false;

            EnsureOptionPanel();
            optionAnchorCard = null;
            optionAnchoredCompact = false;
            ConfigureOptionPanelLayout(false, Mathf.Max(2, selections.Count - 1));
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
            HideCardInfoPanel();
            NotifyUiVisibilityChanged();
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
            optionAnchoredCompact = false;
            ConfigureOptionPanelLayout(false, count == 3 ? 3 : 2);
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
            HideCardInfoPanel();
            NotifyUiVisibilityChanged();
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
            cardItemFloaters.Clear();
            foreach (var card in sourceCards)
                if (card != null)
                    cards.Add(card);

            PreselectForcedCardsForSum();
            RebuildCardGrid();
            ShowCardDetail(cards.Count > 0 ? cards[0] : null);
            cardPanelCanvas.gameObject.SetActive(true);
            NotifyUiVisibilityChanged();
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
            cardItemFloaters.Clear();
            foreach (var card in sourceCards)
                if (card != null)
                    cards.Add(card);

            RebuildCardGrid();
            ShowCardDetail(cards.Count > 0 ? cards[0] : null);
            cardPanelCanvas.gameObject.SetActive(true);
            NotifyUiVisibilityChanged();
            UpdateCardButtons();
            UpdatePanelPoses();
            return true;
        }

        public bool ShowCardInfo(GameCard card)
        {
            return ShowCardInfo(card, null);
        }

        public bool ShowCardInfo(GameCard card, Bounds? worldBounds)
        {
            if (!CanShowDuelUi() || card == null)
                return false;

            EnsureCardInfoPanel();
            EnsureCardDetailPanel();
            if (cardInfoCanvas == null)
                return false;

            UpdateCardInfoWorldPose(worldBounds);
            cardDetailHoverCard = card;

            var data = card.GetData();
            if (data == null)
            {
                cardInfoNameText.text = "\u672a\u77e5\u5361\u7247";
                cardInfoMetaText.text = string.Empty;
                cardInfoStateText.text = BuildCardRuntimeState(card);
                cardInfoActionText.text = BuildCardActionSummary(card);
                cardInfoDescriptionText.text = string.Empty;
                cardInfoImage.texture = TextureManager.container == null ? null : TextureManager.container.unknownCard.texture;
                cardInfoTextureRequestCode = 0;
                PopulateCardDetailForPreview(card, null);
            }
            else
            {
                cardInfoNameText.text = SanitizeText(data.Name);
                cardInfoMetaText.text = BuildCardInfoMeta(data);
                cardInfoStateText.text = BuildCardRuntimeState(card);
                cardInfoActionText.text = BuildCardActionSummary(card);
                cardInfoDescriptionText.text = string.Empty;
                StartCoroutine(LoadCardInfoTexture(data.Id));
                PopulateCardDetailForPreview(card, data);
            }

            cardInfoCanvas.gameObject.SetActive(true);
            if (cardDetailCanvas != null)
                cardDetailCanvas.gameObject.SetActive(true);
            UpdatePanelPoses();
            return true;
        }

        public void ShowActionHint(GameCard card, string actionTitle, string actionDescription)
        {
            if (!CanShowDuelUi())
                return;

            EnsureCardDetailPanel();
            if (cardDetailCanvas == null)
                return;

            cardDetailShowingActionHint = true;
            cardDetailActionRestoreCard = cardDetailPinnedCard ?? cardDetailHoverCard ?? card;
            var data = card == null ? null : card.GetData();
            PopulateCardDetail(card, data, actionTitle, actionDescription);
            cardDetailCanvas.gameObject.SetActive(true);
            UpdatePanelPoses();
        }

        public void HideActionHint()
        {
            cardDetailShowingActionHint = false;
            RestoreCardDetailAfterActionHint();
        }

        public bool TogglePinnedCardInfo(GameCard card)
        {
            if (!CanShowDuelUi() || card == null)
                return false;

            if (cardDetailPinnedCard == card)
            {
                ClearPinnedCardInfo();
                return false;
            }

            EnsureCardDetailPanel();
            if (cardDetailCanvas == null)
                return false;

            cardDetailPinnedCard = card;
            if (!cardDetailShowingActionHint)
                PopulatePinnedCardDetail();
            cardDetailCanvas.gameObject.SetActive(true);
            UpdatePanelPoses();
            return true;
        }

        public void ClearPinnedCardInfo()
        {
            cardDetailPinnedCard = null;
            if (!cardDetailShowingActionHint)
                RestoreCardDetailAfterActionHint();
        }

        public void UpdateCardInfoAnchor(Bounds? worldBounds)
        {
            if (cardInfoCanvas == null || !cardInfoCanvas.gameObject.activeSelf)
                return;

            UpdateCardInfoWorldPose(worldBounds);
            UpdatePanelPoses();
        }

        public void HideCardInfo()
        {
            HideCardInfoPanel();
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
            HideCardDetailPanel();
            cardDetailHoverCard = null;
            cardDetailPinnedCard = null;
            cardDetailActionRestoreCard = null;
            cardDetailShowingActionHint = false;
            HideOptionPanel();
            HidePhaseMenu();
            if (phaseHudCanvas != null && phaseHudCanvas.gameObject.activeSelf)
                phaseHudCanvas.gameObject.SetActive(false);
            if (controlHudCanvas != null && controlHudCanvas.gameObject.activeSelf)
                controlHudCanvas.gameObject.SetActive(false);
            if (duelLogCanvas != null && duelLogCanvas.gameObject.activeSelf)
                duelLogCanvas.gameObject.SetActive(false);
            lastPhaseHudSignature = null;
            ResetDuelLogState();
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
            cardPanelRect.sizeDelta = new Vector2(2440f, 1240f);
            AddPanelBackground(canvasObject, new Color(0f, 0f, 0f, 0f));
            var panelBackground = canvasObject.GetComponent<Image>();
            if (panelBackground != null)
                panelBackground.raycastTarget = false;

            cardTitleText = CreateText("Title", cardPanelRect, new Vector2(80f, -38f), new Vector2(1360f, 56f), 38f, TextAlignmentOptions.Left);
            cardCountText = CreateText("Count", cardPanelRect, new Vector2(1460f, -42f), new Vector2(230f, 48f), 26f, TextAlignmentOptions.Right);
            cardPageText = CreateText("Page", cardPanelRect, new Vector2(760f, -1104f), new Vector2(300f, 44f), 24f, TextAlignmentOptions.Center);

            cardGridRoot = CreateRect("FloatingCardFan", cardPanelRect, new Vector2(84f, -172f), new Vector2(1600f, 900f), new Vector2(0f, 1f));

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

            var detailRoot = CreateRect("FloatingDetail", cardPanelRect, new Vector2(1780f, -136f), new Vector2(560f, 760f), new Vector2(0f, 1f));
            AddRectBackground(detailRoot, new Color(0.018f, 0.024f, 0.032f, 0.54f));
            detailImage = CreateRawImage("DetailImage", detailRoot, new Vector2(24f, -24f), new Vector2(236f, 330f));
            detailNameText = CreateText("DetailName", detailRoot, new Vector2(284f, -28f), new Vector2(252f, 112f), 31f, TextAlignmentOptions.TopLeft);
            detailMetaText = CreateText("DetailMeta", detailRoot, new Vector2(284f, -154f), new Vector2(252f, 98f), 23f, TextAlignmentOptions.TopLeft);
            detailDescriptionText = CreateText("DetailDescription", detailRoot, new Vector2(24f, -390f), new Vector2(512f, 292f), 23f, TextAlignmentOptions.TopLeft);
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
            cardInfoRect.sizeDelta = new Vector2(1020f, 620f);
            AddPanelBackground(canvasObject, HudPanelBackground);
            var background = canvasObject.GetComponent<Image>();
            if (background != null)
                background.raycastTarget = false;

            AddHudPanelChrome(cardInfoRect, HudAccentCyan);
            AddHudSection(cardInfoRect, "CardImageSection", new Vector2(30f, -42f), new Vector2(300f, 426f), HudAccentCyan);
            AddHudSection(cardInfoRect, "CardMetaSection", new Vector2(356f, -42f), new Vector2(292f, 250f), HudAccentGold);
            AddHudSection(cardInfoRect, "CardStateSection", new Vector2(676f, -42f), new Vector2(308f, 250f), HudAccentCyan);
            AddHudSection(cardInfoRect, "CardActionSection", new Vector2(356f, -330f), new Vector2(628f, 220f), HudAccentGold);
            CreateHudCaption("CardMetaCaption", cardInfoRect, new Vector2(382f, -60f), new Vector2(230f, 32f), "\u5361\u7247\u6458\u8981");
            CreateHudCaption("CardStateCaption", cardInfoRect, new Vector2(702f, -60f), new Vector2(230f, 32f), "\u5f53\u524d\u72b6\u6001");
            CreateHudCaption("CardActionCaption", cardInfoRect, new Vector2(382f, -348f), new Vector2(230f, 32f), "\u53ef\u7528\u64cd\u4f5c");

            cardInfoImage = CreateRawImage("CardFace", cardInfoRect, new Vector2(58f, -70f), new Vector2(246f, 344f));
            cardInfoNameText = CreateText("Name", cardInfoRect, new Vector2(44f, -486f), new Vector2(286f, 88f), 38f, TextAlignmentOptions.TopLeft);
            cardInfoMetaText = CreateText("Meta", cardInfoRect, new Vector2(386f, -104f), new Vector2(232f, 154f), 33f, TextAlignmentOptions.TopLeft);
            cardInfoStateText = CreateText("State", cardInfoRect, new Vector2(706f, -104f), new Vector2(248f, 154f), 33f, TextAlignmentOptions.TopLeft);
            cardInfoActionText = CreateText("Actions", cardInfoRect, new Vector2(386f, -390f), new Vector2(548f, 126f), 34f, TextAlignmentOptions.TopLeft);
            cardInfoDescriptionText = CreateText("Description", cardInfoRect, Vector2.zero, Vector2.zero, 1f, TextAlignmentOptions.TopLeft);
            cardInfoDescriptionText.gameObject.SetActive(false);
            cardInfoNameText.overflowMode = TextOverflowModes.Ellipsis;
            cardInfoMetaText.enableWordWrapping = true;
            cardInfoStateText.enableWordWrapping = true;
            cardInfoActionText.enableWordWrapping = true;
            cardInfoDescriptionText.enableWordWrapping = true;
            cardInfoMetaText.overflowMode = TextOverflowModes.Truncate;
            cardInfoStateText.overflowMode = TextOverflowModes.Truncate;
            cardInfoActionText.overflowMode = TextOverflowModes.Truncate;
            cardInfoDescriptionText.overflowMode = TextOverflowModes.Truncate;
            cardInfoNameText.fontSizeMin = 28f;
            cardInfoMetaText.fontSizeMin = 25f;
            cardInfoStateText.fontSizeMin = 25f;
            cardInfoActionText.fontSizeMin = 26f;
            cardInfoDescriptionText.fontSizeMin = 1f;
            canvasObject.SetActive(false);
        }

        private void EnsureCardDetailPanel()
        {
            if (cardDetailCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestCardDetailScreen", out cardDetailCanvas, out cardDetailRect);
            cardDetailRect.sizeDelta = new Vector2(1200f, 1160f);
            AddPanelBackground(canvasObject, new Color(0.006f, 0.010f, 0.016f, 0.88f));
            var background = canvasObject.GetComponent<Image>();
            if (background != null)
                background.raycastTarget = false;

            AddHudPanelChrome(cardDetailRect, HudAccentCyan);
            AddHudSection(cardDetailRect, "DetailFaceSection", new Vector2(34f, -44f), new Vector2(350f, 520f), HudAccentCyan);
            AddHudSection(cardDetailRect, "DetailMetaSection", new Vector2(414f, -44f), new Vector2(748f, 246f), HudAccentGold);
            AddHudSection(cardDetailRect, "DetailTextSection", new Vector2(34f, -596f), new Vector2(744f, 500f), HudAccentCyan);
            AddHudSection(cardDetailRect, "DetailActionSection", new Vector2(810f, -596f), new Vector2(352f, 500f), HudAccentGold);
            CreateHudCaption("DetailMetaCaption", cardDetailRect, new Vector2(440f, -62f), new Vector2(260f, 34f), "\u5361\u7247\u8be6\u60c5");
            CreateHudCaption("DetailTextCaption", cardDetailRect, new Vector2(60f, -614f), new Vector2(260f, 34f), "\u6548\u679c\u6587\u672c");
            CreateHudCaption("DetailActionCaption", cardDetailRect, new Vector2(836f, -614f), new Vector2(260f, 34f), "\u64cd\u4f5c\u8bf4\u660e");

            cardDetailImage = CreateRawImage("CardFace", cardDetailRect, new Vector2(72f, -84f), new Vector2(276f, 386f));
            cardDetailNameText = CreateText("Name", cardDetailRect, new Vector2(414f, -316f), new Vector2(748f, 154f), 52f, TextAlignmentOptions.TopLeft);
            cardDetailMetaText = CreateText("Meta", cardDetailRect, new Vector2(446f, -100f), new Vector2(328f, 164f), 37f, TextAlignmentOptions.TopLeft);
            cardDetailStateText = CreateText("State", cardDetailRect, new Vector2(806f, -100f), new Vector2(320f, 164f), 35f, TextAlignmentOptions.TopLeft);
            cardDetailDescriptionText = CreateText("Description", cardDetailRect, new Vector2(64f, -656f), new Vector2(666f, 398f), 34f, TextAlignmentOptions.TopLeft);
            cardDetailActionTitleText = CreateText("ActionTitle", cardDetailRect, new Vector2(840f, -658f), new Vector2(276f, 70f), 38f, TextAlignmentOptions.TopLeft);
            cardDetailActionText = CreateText("ActionText", cardDetailRect, new Vector2(840f, -742f), new Vector2(276f, 300f), 32f, TextAlignmentOptions.TopLeft);

            cardDetailNameText.overflowMode = TextOverflowModes.Ellipsis;
            cardDetailMetaText.enableWordWrapping = true;
            cardDetailStateText.enableWordWrapping = true;
            cardDetailDescriptionText.enableWordWrapping = true;
            cardDetailActionTitleText.enableWordWrapping = true;
            cardDetailActionText.enableWordWrapping = true;
            cardDetailMetaText.overflowMode = TextOverflowModes.Truncate;
            cardDetailStateText.overflowMode = TextOverflowModes.Truncate;
            cardDetailDescriptionText.overflowMode = TextOverflowModes.Truncate;
            cardDetailActionText.overflowMode = TextOverflowModes.Truncate;
            cardDetailDescriptionText.fontSizeMin = 26f;
            cardDetailActionText.fontSizeMin = 24f;
            canvasObject.SetActive(false);
        }

        private void EnsureOptionPanel()
        {
            if (optionCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestDuelOptionPanel", out optionCanvas, out optionRect);
            optionRect.sizeDelta = new Vector2(DefaultOptionPanelWidth, DefaultOptionPanelHeight);
            AddPanelBackground(canvasObject, new Color(0.012f, 0.017f, 0.024f, 0.92f));
            optionBackgroundImage = canvasObject.GetComponent<Image>();
            optionTitleText = CreateText("Title", optionRect, new Vector2(34f, -30f), new Vector2(912f, 58f), 38f, TextAlignmentOptions.Left);
            optionBodyText = CreateText("Body", optionRect, new Vector2(34f, -98f), new Vector2(912f, 104f), 27f, TextAlignmentOptions.TopLeft);
            optionBodyText.enableWordWrapping = true;
            optionListRoot = CreateRect("List", optionRect, new Vector2(34f, -214f), new Vector2(912f, 360f), new Vector2(0f, 1f));
            canvasObject.SetActive(false);
        }

        private void ConfigureOptionPanelLayout(bool anchoredCompact, int optionCount)
        {
            if (optionRect == null || optionTitleText == null || optionBodyText == null || optionListRoot == null)
                return;

            optionCount = Mathf.Max(1, optionCount);
            optionAnchoredCount = anchoredCompact ? optionCount : 0;
            if (anchoredCompact)
            {
                if (optionBackgroundImage != null)
                {
                    optionBackgroundImage.color = new Color(0.006f, 0.012f, 0.018f, 0.30f);
                    optionBackgroundImage.raycastTarget = false;
                }
                var height = AnchoredOptionPanelBaseHeight
                    + optionCount * AnchoredOptionPanelButtonHeight
                    + Mathf.Max(0, optionCount - 1) * AnchoredOptionPanelButtonGap;
                optionRect.sizeDelta = new Vector2(AnchoredOptionPanelWidth, Mathf.Clamp(height, 230f, 600f));
                optionTitleText.rectTransform.anchoredPosition = new Vector2(44f, -18f);
                optionTitleText.rectTransform.sizeDelta = new Vector2(AnchoredOptionPanelWidth - 88f, 48f);
                optionTitleText.fontSize = 30f;
                optionTitleText.fontSizeMin = 22f;
                optionTitleText.alignment = TextAlignmentOptions.Center;
                optionBodyText.gameObject.SetActive(false);
                optionListRoot.anchoredPosition = new Vector2(24f, -78f);
                optionListRoot.sizeDelta = new Vector2(AnchoredOptionPanelWidth - 48f, optionRect.sizeDelta.y - 96f);
            }
            else
            {
                optionAnchoredCount = 0;
                if (optionBackgroundImage != null)
                {
                    optionBackgroundImage.color = new Color(0.012f, 0.017f, 0.024f, 0.92f);
                    optionBackgroundImage.raycastTarget = true;
                }
                optionRect.sizeDelta = new Vector2(DefaultOptionPanelWidth, DefaultOptionPanelHeight);
                optionTitleText.rectTransform.anchoredPosition = new Vector2(34f, -30f);
                optionTitleText.rectTransform.sizeDelta = new Vector2(912f, 58f);
                optionTitleText.fontSize = 38f;
                optionTitleText.alignment = TextAlignmentOptions.Left;
                optionBodyText.gameObject.SetActive(true);
                optionListRoot.anchoredPosition = new Vector2(34f, -214f);
                optionListRoot.sizeDelta = new Vector2(912f, 360f);
            }
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
                phaseHudRect.sizeDelta = new Vector2(1120f, 430f);
                AddPanelBackground(canvasObject, HudPanelBackground);
                AddHudPanelChrome(phaseHudRect, HudAccentCyan);
                AddHudSection(phaseHudRect, "LifeSection", new Vector2(34f, -48f), new Vector2(468f, 292f), HudAccentCyan);
                AddHudSection(phaseHudRect, "PhaseSection", new Vector2(540f, -48f), new Vector2(520f, 292f), HudAccentGold);
                CreateHudCaption("LifeCaption", phaseHudRect, new Vector2(58f, -54f), new Vector2(250f, 34f), "LP");
                CreateHudCaption("PhaseCaption", phaseHudRect, new Vector2(564f, -54f), new Vector2(250f, 34f), "\u9636\u6bb5");
                lifeHudText = CreateText("LifeText", phaseHudRect, new Vector2(58f, -92f), new Vector2(420f, 206f), 60f, TextAlignmentOptions.Left);
                phaseHudText = CreateText("PhaseText", phaseHudRect, new Vector2(564f, -94f), new Vector2(456f, 116f), 45f, TextAlignmentOptions.Left);
                phaseTrackRoot = CreateRect("PhaseTrack", phaseHudRect, new Vector2(564f, -242f), new Vector2(470f, 92f), new Vector2(0f, 1f));
                canvasObject.SetActive(false);
            }

            if (controlHudCanvas == null)
            {
                var controlObject = CreateCanvasObject("QuestDuelControlHud", out controlHudCanvas, out controlHudRect);
                controlHudRect.sizeDelta = new Vector2(900f, 470f);
                AddPanelBackground(controlObject, HudPanelBackground);
                AddHudPanelChrome(controlHudRect, HudAccentGold);
                AddHudSection(controlHudRect, "PhaseControlSection", new Vector2(34f, -70f), new Vector2(796f, 166f), HudAccentGold);
                AddHudSection(controlHudRect, "SystemControlSection", new Vector2(34f, -280f), new Vector2(796f, 142f), HudAccentRed);
                CreateHudCaption("ActionCaption", controlHudRect, new Vector2(44f, -30f), new Vector2(360f, 34f), "\u53ef\u7528\u64cd\u4f5c");
                CreateHudCaption("SystemCaption", controlHudRect, new Vector2(44f, -240f), new Vector2(360f, 34f), "\u7cfb\u7edf");
                phaseHudButtonRoot = CreateRect("PhaseButtons", controlHudRect, new Vector2(52f, -88f), new Vector2(756f, 138f), new Vector2(0f, 1f));
                systemHudButtonRoot = CreateRect("SystemButtons", controlHudRect, new Vector2(52f, -298f), new Vector2(756f, 116f), new Vector2(0f, 1f));
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
                ResetDuelLogState();
                return;
            }

            EnsurePhaseHud();
            EnsureDuelLogPanel();
            var canBattle = PhaseButtonHandler.battlePhase;
            var canMain2 = PhaseButtonHandler.main2Phase;
            var canEnd = PhaseButtonHandler.endPhase;
            if (lifeHudText != null)
                lifeHudText.text = FormatLifeHud(core.life0, core.life1);
            UpdateLifeHudFlash();

            var signature = core.phase + "|" + canBattle + "|" + canMain2 + "|" + canEnd + "|" + core.myTurn + "|" + core.life0 + "|" + core.life1;
            if (signature != lastPhaseHudSignature)
            {
                lastPhaseHudSignature = signature;
                ClearRows(phaseHudRows);
                phaseHudText.text = (core.myTurn ? "我方回合" : "对方回合") + "\n" + GetPhaseName(core.phase);
                phaseHudText.text = LocalizeQuestLabel(phaseHudText.text);
                phaseHudText.text = (core.myTurn ? "\u6211\u65b9\u56de\u5408" : "\u5bf9\u65b9\u56de\u5408") + "\n" + GetPhaseName(core.phase);
                RebuildPhaseTrack(core.phase);
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

        private void UpdateLifeHudFlash()
        {
            if (lifeHudText == null)
                return;

            if (lifeHudFlashDuration <= 0f)
            {
                lifeHudText.color = Color.white;
                lifeHudText.transform.localScale = Vector3.one;
                return;
            }

            var progress = Mathf.Clamp01((Time.unscaledTime - lifeHudFlashStartTime) / lifeHudFlashDuration);
            if (progress >= 1f)
            {
                lifeHudFlashDuration = 0f;
                lifeHudText.color = Color.white;
                lifeHudText.transform.localScale = Vector3.one;
                return;
            }

            var pulse = Mathf.Sin(progress * Mathf.PI);
            lifeHudText.color = Color.Lerp(Color.white, lifeHudFlashColor, pulse);
            lifeHudText.transform.localScale = Vector3.one * Mathf.Lerp(1f, lifeHudFlashScale, pulse);
        }

        private static string FormatLifeHud(int myLife, int opponentLife)
        {
            return "<color=#8DF6FF>\u6211\u65b9</color> <size=70>LP " + Mathf.Max(0, myLife) + "</size>\n"
                + "<color=#FFB86B>\u5bf9\u65b9</color> <size=58>LP " + Mathf.Max(0, opponentLife) + "</size>";
        }

        private void RebuildPhaseTrack(DuelPhase currentPhase)
        {
            if (phaseTrackRoot == null)
                return;

            ClearRows(phaseTrackRows);
            AddPhaseTrackNode(0, "\u62bd\u5361", currentPhase == DuelPhase.Draw, HudAccentCyan);
            AddPhaseTrackNode(1, "\u51c6\u5907", currentPhase == DuelPhase.Standby, HudAccentCyan);
            AddPhaseTrackNode(2, "\u4e3b1", currentPhase == DuelPhase.Main1, HudAccentGold);
            AddPhaseTrackNode(
                3,
                "\u6218\u6597",
                currentPhase == DuelPhase.BattleStart
                    || currentPhase == DuelPhase.BattleStep
                    || currentPhase == DuelPhase.Battle
                    || currentPhase == DuelPhase.Damage
                    || currentPhase == DuelPhase.DamageCal,
                HudAccentRed);
            AddPhaseTrackNode(4, "\u4e3b2", currentPhase == DuelPhase.Main2, HudAccentGold);
            AddPhaseTrackNode(5, "\u7ed3\u675f", currentPhase == DuelPhase.End, HudAccentCyan);
        }

        private void AddPhaseTrackNode(int index, string label, bool active, Color accent)
        {
            var width = 72f;
            var gap = 8f;
            var rect = CreateRect("PhaseNode_" + index, phaseTrackRoot, new Vector2(index * (width + gap), 0f), new Vector2(width, 76f), new Vector2(0f, 1f));
            AddRectBackground(rect, active
                ? new Color(accent.r, accent.g, accent.b, 0.78f)
                : new Color(0.05f, 0.07f, 0.09f, 0.78f));

            var rail = CreateRect("Rail", rect, Vector2.zero, new Vector2(width, 5f), new Vector2(0f, 1f));
            AddRectBackground(rail, active
                ? new Color(1f, 1f, 1f, 0.86f)
                : new Color(accent.r, accent.g, accent.b, 0.38f));

            var text = CreateText("Label", rect, new Vector2(4f, -16f), new Vector2(width - 8f, 44f), active ? 25f : 22f, TextAlignmentOptions.Center);
            text.text = label;
            text.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            text.color = active ? Color.white : new Color(0.70f, 0.86f, 0.92f, 0.84f);
            phaseTrackRows.Add(rect.gameObject);
        }

        private void EnsureDuelLogPanel()
        {
            if (duelLogCanvas != null)
                return;

            var canvasObject = CreateCanvasObject("QuestDuelLogPanel", out duelLogCanvas, out duelLogRect);
            duelLogRect.sizeDelta = new Vector2(1480f, 980f);
            AddPanelBackground(canvasObject, HudPanelBackground);
            var background = canvasObject.GetComponent<Image>();
            if (background != null)
                background.raycastTarget = false;

            AddHudPanelChrome(duelLogRect, HudAccentCyan);
            AddHudSection(duelLogRect, "LogEventSection", new Vector2(40f, -118f), new Vector2(1380f, 178f), HudAccentGold);
            AddHudSection(duelLogRect, "LogStatusSection", new Vector2(40f, -326f), new Vector2(650f, 282f), HudAccentCyan);
            AddHudSection(duelLogRect, "LogPromptSection", new Vector2(728f, -326f), new Vector2(692f, 282f), HudAccentGold);
            AddHudSection(duelLogRect, "LogRecentSection", new Vector2(40f, -646f), new Vector2(1380f, 264f), HudAccentCyan);
            var eventAccent = CreateRect("LogEventAccent", duelLogRect, new Vector2(40f, -118f), new Vector2(16f, 178f), new Vector2(0f, 1f));
            duelLogEventAccentImage = eventAccent.gameObject.AddComponent<Image>();
            duelLogEventAccentImage.color = HudAccentGold;
            duelLogEventAccentImage.raycastTarget = false;
            duelLogTitleText = CreateText("Title", duelLogRect, new Vector2(58f, -34f), new Vector2(1180f, 78f), 64f, TextAlignmentOptions.Left);
            CreateHudCaption("EventCaption", duelLogRect, new Vector2(66f, -132f), new Vector2(300f, 34f), "\u5f53\u524d\u4e8b\u4ef6");
            CreateHudCaption("StatusCaption", duelLogRect, new Vector2(66f, -340f), new Vector2(300f, 34f), "\u6218\u51b5");
            CreateHudCaption("PromptCaption", duelLogRect, new Vector2(754f, -340f), new Vector2(300f, 34f), "\u5f53\u524d\u64cd\u4f5c");
            CreateHudCaption("RecentCaption", duelLogRect, new Vector2(66f, -660f), new Vector2(300f, 34f), "\u6700\u8fd1\u4e8b\u4ef6");
            duelLogEventText = CreateText("EventBody", duelLogRect, new Vector2(72f, -174f), new Vector2(1296f, 96f), 66f, TextAlignmentOptions.Left);
            duelLogStatusText = CreateText("StatusBody", duelLogRect, new Vector2(68f, -380f), new Vector2(584f, 206f), 38f, TextAlignmentOptions.TopLeft);
            duelLogPromptText = CreateText("PromptBody", duelLogRect, new Vector2(756f, -380f), new Vector2(622f, 206f), 41f, TextAlignmentOptions.TopLeft);
            duelLogBodyText = CreateText("RecentBody", duelLogRect, new Vector2(68f, -702f), new Vector2(1300f, 176f), 38f, TextAlignmentOptions.TopLeft);
            duelLogEventText.enableWordWrapping = true;
            duelLogStatusText.enableWordWrapping = true;
            duelLogPromptText.enableWordWrapping = true;
            duelLogBodyText.enableWordWrapping = true;
            duelLogEventText.overflowMode = TextOverflowModes.Truncate;
            duelLogStatusText.overflowMode = TextOverflowModes.Truncate;
            duelLogPromptText.overflowMode = TextOverflowModes.Truncate;
            duelLogBodyText.overflowMode = TextOverflowModes.Truncate;
            duelLogEventText.fontSizeMin = 50f;
            duelLogStatusText.fontSizeMin = 31f;
            duelLogPromptText.fontSizeMin = 37f;
            duelLogBodyText.fontSizeMin = 31f;
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
            var statusText = BuildDuelStatusText(core, turnText, phaseText);
            var promptText = BuildDuelPromptText(core, messageText);
            var eventText = BuildCurrentEventText(messageText, logText);
            var eventColor = ResolveCurrentEventColor();
            var detailText = BuildPresentationLogText(logText);

            var signature = statusText + "|" + promptText + "|" + eventText + "|" + ColorUtility.ToHtmlStringRGBA(eventColor) + "|" + detailText;
            if (signature != lastDuelLogSignature)
            {
                lastDuelLogSignature = signature;
                if (duelLogTitleText != null)
                    duelLogTitleText.text = "\u51b3\u6597\u4fe1\u606f";
                if (duelLogEventText != null)
                {
                    duelLogEventText.text = eventText;
                    duelLogEventText.color = eventColor;
                }
                if (duelLogEventAccentImage != null)
                    duelLogEventAccentImage.color = new Color(eventColor.r, eventColor.g, eventColor.b, 0.92f);
                if (duelLogStatusText != null)
                    duelLogStatusText.text = statusText;
                if (duelLogPromptText != null)
                    duelLogPromptText.text = promptText;
                if (duelLogBodyText != null)
                    duelLogBodyText.text = detailText;
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

        private void HandlePresentationEvent(DuelPresentationEvent evt)
        {
            var line = FormatPresentationLogLine(evt);
            if (string.IsNullOrEmpty(line))
                return;

            presentationLogLines.Insert(0, line);
            while (presentationLogLines.Count > MaxPresentationLogLines)
                presentationLogLines.RemoveAt(presentationLogLines.Count - 1);
            CaptureImportantEvent(evt, line);
            lastDuelLogSignature = null;
            TriggerLifeHudFlash(evt);
        }

        private void ResetDuelLogState()
        {
            presentationLogLines.Clear();
            lastImportantEventText = null;
            lastImportantEventTime = 0f;
            lastImportantEventColor = HudAccentCyan;
            lastDuelLogSignature = null;
        }

        private void TriggerLifeHudFlash(DuelPresentationEvent evt)
        {
            if (evt == null)
                return;

            if (evt.Kind == DuelPresentationKind.Damage)
            {
                lifeHudFlashColor = evt.Controller == 0
                    ? new Color(1f, 0.22f, 0.18f, 1f)
                    : new Color(1f, 0.56f, 0.18f, 1f);
            }
            else if (evt.Kind == DuelPresentationKind.Recover)
            {
                lifeHudFlashColor = new Color(0.34f, 1f, 0.52f, 1f);
            }
            else
            {
                return;
            }

            lifeHudFlashStartTime = Time.unscaledTime;
            lifeHudFlashDuration = evt.Weight >= DuelPresentationWeight.Heavy ? 0.64f : 0.46f;
            lifeHudFlashScale = evt.Weight >= DuelPresentationWeight.Heavy ? 1.12f : 1.07f;
        }

        private string BuildPresentationLogText(string fallbackLogText)
        {
            fallbackLogText = TrimForHud(fallbackLogText, 82);
            if (presentationLogLines.Count == 0)
                return fallbackLogText;

            var text = string.Empty;
            var maxLines = Mathf.Min(4, presentationLogLines.Count);
            for (var i = 0; i < maxLines; i += 1)
                text += "\u2022 " + TrimForHud(presentationLogLines[i], 58) + "\n";

            if (!string.IsNullOrWhiteSpace(fallbackLogText))
                text += "\u65e5\u5fd7: " + fallbackLogText;
            return text.TrimEnd();
        }

        private string BuildCurrentEventText(string messageText, string logText)
        {
            if (!string.IsNullOrWhiteSpace(lastImportantEventText))
            {
                var age = Time.unscaledTime - lastImportantEventTime;
                if (age < 14f)
                    return TrimForHud(lastImportantEventText, 46);
            }

            if (!string.IsNullOrWhiteSpace(messageText))
                return TrimForHud(messageText, 46);
            return TrimForHud(logText, 46);
        }

        private Color ResolveCurrentEventColor()
        {
            if (string.IsNullOrWhiteSpace(lastImportantEventText))
                return Color.white;

            var age = Time.unscaledTime - lastImportantEventTime;
            if (age <= 3.5f)
                return lastImportantEventColor;
            if (age >= 14f)
                return Color.white;
            return Color.Lerp(Color.white, lastImportantEventColor, 0.52f);
        }

        private void CaptureImportantEvent(DuelPresentationEvent evt, string fallbackLine)
        {
            if (evt == null)
                return;

            var text = FormatImportantEventText(evt);
            if (string.IsNullOrWhiteSpace(text))
                text = fallbackLine;
            if (string.IsNullOrWhiteSpace(text))
                return;

            lastImportantEventText = text;
            lastImportantEventColor = ResolveEventColor(evt);
            lastImportantEventTime = Time.unscaledTime;
        }

        private static string BuildDuelStatusText(OcgCore core, string turnText, string phaseText)
        {
            if (core == null)
                return string.Empty;

            var myHand = SafeGetHandCount(core, true);
            var opHand = SafeGetHandCount(core, false);
            var myDeck = SafeGetLocationCount(core, CardLocation.Deck, 0);
            var opDeck = SafeGetLocationCount(core, CardLocation.Deck, 1);
            var myGrave = SafeGetLocationCount(core, CardLocation.Grave, 0);
            var opGrave = SafeGetLocationCount(core, CardLocation.Grave, 1);
            var myBanished = SafeGetLocationCount(core, CardLocation.Removed, 0);
            var opBanished = SafeGetLocationCount(core, CardLocation.Removed, 1);
            var myExtra = SafeGetLocationCount(core, CardLocation.Extra, 0);
            var opExtra = SafeGetLocationCount(core, CardLocation.Extra, 1);
            return "<size=50><b>" + turnText + "</b></size>  " + phaseText + "\n"
                + "<color=#8DF6FF>\u6211\u65b9</color> LP " + Mathf.Max(0, core.life0)
                + "  \u724c\u7ec4 " + myDeck + "  \u624b\u724c " + myHand + "\n"
                + "\u5893 " + myGrave + "  \u9664\u5916 " + myBanished + "  \u989d\u5916 " + myExtra + "\n"
                + "<color=#FFB86B>\u5bf9\u65b9</color> LP " + Mathf.Max(0, core.life1)
                + "  \u724c\u7ec4 " + opDeck + "  \u624b\u724c " + opHand + "\n"
                + "\u5893 " + opGrave + "  \u9664\u5916 " + opBanished + "  \u989d\u5916 " + opExtra;
        }

        private static string BuildDuelPromptText(OcgCore core, string messageText)
        {
            if (core == null)
                return string.Empty;

            var actionable = CountActionableCards(core);
            var selectable = CountSelectableTargets(core);
            var text = "<size=50><b>" + TrimForHud(messageText, 24) + "</b></size>";
            if (selectable > 0)
                text += "\n<color=#69FFE0>\u53ef\u9009\u76ee\u6807 " + selectable + "</color>\n\u7528\u5c04\u7ebf\u70b9\u573a\u4e0a\u9ad8\u4eae\u5361";
            else if (actionable > 0)
                text += "\n<color=#FFD36B>\u53ef\u64cd\u4f5c\u5361 " + actionable + "</color>\n\u70b9\u51fb\u5361\u7247\u67e5\u770b\u64cd\u4f5c";
            else
                text += "\n\u7b49\u5f85\u51b3\u6597\u5f15\u64ce\u5904\u7406";

            var phaseActions = BuildPhaseActionText();
            if (!string.IsNullOrEmpty(phaseActions))
                text += "\n" + phaseActions;
            return text;
        }

        private static string BuildPhaseActionText()
        {
            var text = string.Empty;
            if (PhaseButtonHandler.battlePhase)
                text += "\u6218\u6597 ";
            if (PhaseButtonHandler.main2Phase)
                text += "\u4e3b2 ";
            if (PhaseButtonHandler.endPhase)
                text += "\u7ed3\u675f ";
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            return "\u53ef\u5207\u9636\u6bb5: " + text.Trim();
        }

        private static int CountActionableCards(OcgCore core)
        {
            if (core == null || core.cards == null)
                return 0;

            var count = 0;
            foreach (var card in core.cards)
            {
                if (card == null || card.p == null || card.buttons == null || card.buttons.Count == 0)
                    continue;
                if ((card.p.location & (uint)(CardLocation.Hand | CardLocation.Onfield | CardLocation.Grave | CardLocation.Removed)) == 0)
                    continue;
                count += 1;
            }
            return count;
        }

        private static int CountSelectableTargets(OcgCore core)
        {
            if (core == null || core.places == null)
                return 0;

            var count = 0;
            foreach (var place in core.places)
                if (place != null && place.cardSelecting && place.cookieCard != null)
                    count += 1;
            return count;
        }

        private static int SafeGetHandCount(OcgCore core, bool mine)
        {
            if (core == null)
                return 0;

            try
            {
                return Mathf.Max(0, mine ? core.GetMyHandCount() : core.GetOpHandCount());
            }
            catch
            {
                return 0;
            }
        }

        private static int SafeGetLocationCount(OcgCore core, CardLocation location, uint controller)
        {
            if (core == null)
                return 0;

            try
            {
                return Mathf.Max(0, core.GetLocationCardCount(location, controller));
            }
            catch
            {
                return 0;
            }
        }

        private static string TrimForHud(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = SanitizeText(LocalizeQuestLabel(text)).Replace("\n", " ").Trim();
            if (maxChars <= 0 || text.Length <= maxChars)
                return text;
            return text.Substring(0, Mathf.Max(0, maxChars - 3)) + "...";
        }

        private static string FormatPresentationLogLine(DuelPresentationEvent evt)
        {
            if (evt == null)
                return string.Empty;

            var player = GetPresentationControllerName(evt.Controller);
            var cardName = GetCardName(evt.Card);
            switch (evt.Kind)
            {
                case DuelPresentationKind.CardMoved:
                    return FormatMovePresentationLine(evt, player, cardName);
                case DuelPresentationKind.CardSet:
                    return "\u3010\u653e\u7f6e\u3011" + player + " " + cardName;
                case DuelPresentationKind.CardSummoned:
                    return "\u3010\u53ec\u5524\u3011" + player + " " + GetSummonLogName(evt.SummonKind) + " " + cardName;
                case DuelPresentationKind.CardActivated:
                    return evt.ChainIndex > 1
                        ? "\u3010\u8fde\u9501\u3011" + evt.ChainIndex + ": " + cardName
                        : "\u3010\u53d1\u52a8\u3011" + player + " " + cardName;
                case DuelPresentationKind.ChainStacked:
                    return "\u3010\u8fde\u9501\u3011" + evt.ChainIndex + ": " + cardName;
                case DuelPresentationKind.CardDestroyed:
                    return "\u3010\u7834\u574f\u3011" + cardName;
                case DuelPresentationKind.CardSentToGrave:
                    return "\u3010\u9001\u5893\u3011" + cardName;
                case DuelPresentationKind.CardBanished:
                    return "\u3010\u9664\u5916\u3011" + cardName;
                case DuelPresentationKind.AttackDeclared:
                    return "\u3010\u653b\u51fb\u3011" + cardName + " \u2192 " + GetPresentationTargetName(evt);
                case DuelPresentationKind.AttackImpact:
                    return "\u3010\u547d\u4e2d\u3011" + cardName;
                case DuelPresentationKind.Damage:
                    return "\u3010\u4f24\u5bb3\u3011" + GetPresentationControllerName(evt.Controller) + " LP -" + Mathf.Abs(evt.Value);
                case DuelPresentationKind.Recover:
                    return "\u3010\u6062\u590d\u3011" + GetPresentationControllerName(evt.Controller) + " LP +" + Mathf.Abs(evt.Value);
                case DuelPresentationKind.PhaseChanged:
                    return "\u3010\u9636\u6bb5\u3011" + player + " " + GetPhaseName(evt.Phase);
                default:
                    return string.Empty;
            }
        }

        private static string FormatImportantEventText(DuelPresentationEvent evt)
        {
            if (evt == null)
                return string.Empty;

            var player = GetPresentationControllerName(evt.Controller);
            var cardName = GetCardName(evt.Card);
            switch (evt.Kind)
            {
                case DuelPresentationKind.CardSet:
                    return player + " \u653e\u7f6e " + cardName;
                case DuelPresentationKind.CardSummoned:
                    return GetSummonLogName(evt.SummonKind) + ": " + cardName;
                case DuelPresentationKind.CardActivated:
                    return evt.ChainIndex > 1
                        ? "\u8fde\u9501 " + evt.ChainIndex + ": " + cardName
                        : player + " \u53d1\u52a8 " + cardName;
                case DuelPresentationKind.ChainStacked:
                    return "\u8fde\u9501 " + evt.ChainIndex + ": " + cardName;
                case DuelPresentationKind.CardDestroyed:
                    return "\u7834\u574f: " + cardName;
                case DuelPresentationKind.CardSentToGrave:
                    return "\u9001\u53bb\u5893\u5730: " + cardName;
                case DuelPresentationKind.CardBanished:
                    return "\u9664\u5916: " + cardName;
                case DuelPresentationKind.AttackDeclared:
                    return evt.Final
                        ? "\u51b3\u80dc\u653b\u51fb: " + cardName + " \u2192 " + GetPresentationTargetName(evt)
                        : "\u653b\u51fb\u5ba3\u8a00: " + cardName + " \u2192 " + GetPresentationTargetName(evt);
                case DuelPresentationKind.AttackImpact:
                    return "\u6218\u6597\u547d\u4e2d: " + cardName;
                case DuelPresentationKind.Damage:
                    return player + " LP -" + Mathf.Abs(evt.Value);
                case DuelPresentationKind.Recover:
                    return player + " LP +" + Mathf.Abs(evt.Value);
                case DuelPresentationKind.PhaseChanged:
                    return player + " \u8fdb\u5165 " + GetPhaseName(evt.Phase);
                default:
                    return string.Empty;
            }
        }

        private static Color ResolveEventColor(DuelPresentationEvent evt)
        {
            if (evt == null)
                return Color.white;

            switch (evt.Kind)
            {
                case DuelPresentationKind.Damage:
                case DuelPresentationKind.CardDestroyed:
                case DuelPresentationKind.AttackDeclared:
                case DuelPresentationKind.AttackImpact:
                    return new Color(1f, 0.38f, 0.22f, 1f);
                case DuelPresentationKind.Recover:
                    return new Color(0.36f, 1f, 0.58f, 1f);
                case DuelPresentationKind.CardActivated:
                case DuelPresentationKind.ChainStacked:
                    return new Color(1f, 0.80f, 0.26f, 1f);
                case DuelPresentationKind.CardSummoned:
                    return ResolveSummonHudColor(evt.SummonKind);
                case DuelPresentationKind.CardBanished:
                    return new Color(0.35f, 0.82f, 1f, 1f);
                case DuelPresentationKind.CardSentToGrave:
                    return new Color(0.72f, 0.78f, 0.90f, 1f);
                case DuelPresentationKind.PhaseChanged:
                    return new Color(0.62f, 0.94f, 1f, 1f);
                default:
                    return Color.white;
            }
        }

        private static Color ResolveSummonHudColor(DuelPresentationSummonKind summonKind)
        {
            switch (summonKind)
            {
                case DuelPresentationSummonKind.Fusion:
                    return new Color(0.86f, 0.46f, 1f, 1f);
                case DuelPresentationSummonKind.Synchro:
                    return new Color(0.92f, 1f, 0.96f, 1f);
                case DuelPresentationSummonKind.Xyz:
                    return new Color(0.98f, 0.86f, 0.28f, 1f);
                case DuelPresentationSummonKind.Link:
                    return new Color(0.36f, 0.68f, 1f, 1f);
                case DuelPresentationSummonKind.Ritual:
                    return new Color(0.38f, 0.58f, 1f, 1f);
                case DuelPresentationSummonKind.Pendulum:
                    return new Color(0.42f, 1f, 0.88f, 1f);
                case DuelPresentationSummonKind.Tribute:
                    return new Color(1f, 0.62f, 0.22f, 1f);
                default:
                    return new Color(0.58f, 1f, 0.74f, 1f);
            }
        }

        private static string FormatMovePresentationLine(DuelPresentationEvent evt, string player, string cardName)
        {
            switch (evt.MoveKind)
            {
                case DuelPresentationMoveKind.Draw:
                    return "\u3010\u62bd\u5361\u3011" + player;
                case DuelPresentationMoveKind.ToHand:
                    return "\u3010\u5165\u624b\u3011" + cardName;
                case DuelPresentationMoveKind.ToField:
                    return "\u3010\u4e0a\u573a\u3011" + cardName;
                case DuelPresentationMoveKind.Released:
                    return "\u3010\u89e3\u653e\u3011" + cardName;
                case DuelPresentationMoveKind.Material:
                    return "\u3010\u7d20\u6750\u3011" + cardName;
                case DuelPresentationMoveKind.Overlay:
                    return "\u3010\u53e0\u653e\u3011" + cardName;
                default:
                    return string.Empty;
            }
        }

        private static string GetPresentationTargetName(DuelPresentationEvent evt)
        {
            if (evt == null || evt.Direct || evt.TargetCard == null)
                return "\u76f4\u63a5\u653b\u51fb";
            return GetCardName(evt.TargetCard);
        }

        private static string GetPresentationControllerName(int controller)
        {
            return controller == 0 ? "\u6211\u65b9" : "\u5bf9\u65b9";
        }

        private static string GetSummonLogName(DuelPresentationSummonKind summonKind)
        {
            switch (summonKind)
            {
                case DuelPresentationSummonKind.Tribute:
                    return "\u4e0a\u7ea7\u53ec\u5524";
                case DuelPresentationSummonKind.Flip:
                    return "\u7ffb\u8f6c\u53ec\u5524";
                case DuelPresentationSummonKind.Special:
                    return "\u7279\u6b8a\u53ec\u5524";
                case DuelPresentationSummonKind.Fusion:
                    return "\u878d\u5408\u53ec\u5524";
                case DuelPresentationSummonKind.Synchro:
                    return "\u540c\u8c03\u53ec\u5524";
                case DuelPresentationSummonKind.Xyz:
                    return "\u8d85\u91cf\u53ec\u5524";
                case DuelPresentationSummonKind.Link:
                    return "\u8fde\u63a5\u53ec\u5524";
                case DuelPresentationSummonKind.Ritual:
                    return "\u4eea\u5f0f\u53ec\u5524";
                case DuelPresentationSummonKind.Pendulum:
                    return "\u7075\u6446\u53ec\u5524";
                default:
                    return "\u53ec\u5524";
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
            AddButtonChrome(row, HudAccentCyan);
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
            AddButtonChrome(surrender, HudAccentRed);
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
            AddButtonChrome(exit, HudAccentGold);
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
            var rowWidth = optionAnchoredCompact ? AnchoredOptionPanelButtonWidth : 912f;
            var rowHeight = optionAnchoredCompact ? AnchoredOptionPanelButtonHeight : 66f;
            var rowGap = optionAnchoredCompact ? AnchoredOptionPanelButtonHeight + AnchoredOptionPanelButtonGap : 82f;
            var rowColor = color ?? (optionAnchoredCompact
                ? ResolveAnchoredOptionColor(label)
                : new Color(0.10f, 0.27f, 0.38f, 0.98f));
            var row = CreateButton(
                "Option_" + index,
                optionListRoot,
                optionAnchoredCompact
                    ? ResolveAnchoredOptionButtonPosition(index, Mathf.Max(optionAnchoredCount, 1))
                    : new Vector2(0f, -index * rowGap),
                new Vector2(rowWidth, rowHeight),
                label,
                onClick,
                rowColor,
                new Vector2(0f, 1f));
            if (optionAnchoredCompact)
            {
                var labelText = row.GetComponentInChildren<TextMeshProUGUI>();
                if (labelText != null)
                {
                    labelText.fontSize = 30f;
                    labelText.fontSizeMax = 30f;
                    labelText.fontSizeMin = 18f;
                    labelText.fontStyle = FontStyles.Bold;
                    labelText.enableWordWrapping = true;
                    labelText.overflowMode = TextOverflowModes.Truncate;
                }

                AddButtonChrome(row, ResolveAnchoredOptionAccentColor(label));
            }
            optionRows.Add(row.gameObject);
        }

        private static Vector2 ResolveAnchoredOptionButtonPosition(int index, int optionCount)
        {
            optionCount = Mathf.Max(1, optionCount);
            var rootWidth = AnchoredOptionPanelWidth - 48f;
            var baseX = Mathf.Max(0f, (rootWidth - AnchoredOptionPanelButtonWidth) * 0.5f);
            var center = (optionCount - 1) * 0.5f;
            var offset = index - center;
            var fan = Mathf.Clamp(offset * 18f, -34f, 34f);
            var y = -index * (AnchoredOptionPanelButtonHeight + AnchoredOptionPanelButtonGap)
                - Mathf.Abs(offset) * 3.5f;
            return new Vector2(baseX + fan, y);
        }

        private static Color ResolveAnchoredOptionColor(string label)
        {
            label = LocalizeQuestLabel(label ?? string.Empty);
            if (ContainsAny(label, "\u653b\u51fb", "Battle"))
                return new Color(0.42f, 0.12f, 0.08f, 0.95f);
            if (ContainsAny(label, "\u53d1\u52a8", "\u6548\u679c", "Activate"))
                return new Color(0.07f, 0.34f, 0.44f, 0.95f);
            if (ContainsAny(label, "\u7279\u6b8a\u53ec\u5524", "\u901a\u5e38\u53ec\u5524", "\u53ec\u5524", "\u7075\u6446", "Summon"))
                return new Color(0.08f, 0.36f, 0.22f, 0.95f);
            if (ContainsAny(label, "\u653e\u7f6e", "\u8bbe\u7f6e", "Set"))
                return new Color(0.12f, 0.22f, 0.44f, 0.95f);
            if (ContainsAny(label, "\u53d6\u6d88", "\u653e\u5f03", "Cancel"))
                return new Color(0.36f, 0.13f, 0.16f, 0.95f);
            return new Color(0.10f, 0.26f, 0.34f, 0.95f);
        }

        private static Color ResolveAnchoredOptionAccentColor(string label)
        {
            label = LocalizeQuestLabel(label ?? string.Empty);
            if (ContainsAny(label, "\u653b\u51fb", "Battle"))
                return new Color(1f, 0.36f, 0.18f, 0.82f);
            if (ContainsAny(label, "\u53d1\u52a8", "\u6548\u679c", "Activate"))
                return new Color(0.30f, 0.95f, 1f, 0.82f);
            if (ContainsAny(label, "\u7279\u6b8a\u53ec\u5524", "\u901a\u5e38\u53ec\u5524", "\u53ec\u5524", "\u7075\u6446", "Summon"))
                return new Color(0.44f, 1f, 0.62f, 0.82f);
            if (ContainsAny(label, "\u653e\u7f6e", "\u8bbe\u7f6e", "Set"))
                return new Color(0.50f, 0.72f, 1f, 0.82f);
            if (ContainsAny(label, "\u53d6\u6d88", "\u653e\u5f03", "Cancel"))
                return HudAccentRed;
            return HudAccentGold;
        }

        private static bool ContainsAny(string text, params string[] patterns)
        {
            if (string.IsNullOrEmpty(text) || patterns == null)
                return false;

            foreach (var pattern in patterns)
                if (!string.IsNullOrEmpty(pattern) && text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
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
            cardItemFloaters.Clear();
            var start = cardPage * CardGridPageSize;
            var end = Mathf.Min(cards.Count, start + CardGridPageSize);
            for (var index = start; index < end; index += 1)
            {
                var local = index - start;
                var column = local % CardGridColumns;
                var row = local / CardGridColumns;
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
            rect.sizeDelta = new Vector2(CardGridItemWidth, CardGridItemHeight);
            var basePosition = ResolveFloatingCardGridPosition(column, row);
            var baseRotation = ResolveFloatingCardGridRotation(column, row);
            rect.anchoredPosition = basePosition;
            rect.localRotation = Quaternion.Euler(0f, 0f, baseRotation);
            var floater = item.AddComponent<QuestFloatingCardGridItem>();
            floater.Configure(
                rect,
                basePosition,
                baseRotation,
                Mathf.Abs(card == null ? column + row * CardGridColumns : card.md5 % 997) * 0.017f,
                CardGridFloatAmplitude,
                CardGridFloatSelectedLift,
                CardGridFloatDetailLift,
                CardGridFloatHoverLift);
            if (card != null)
                cardItemFloaters[card] = floater;

            var image = item.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);
            image.raycastTarget = true;
            cardItemBackgrounds[card] = image;

            var button = item.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => OnCardGridItemClicked(card));

            var shadow = CreateRect("FloatShadow", rect, new Vector2(34f, -328f), new Vector2(162f, 22f), new Vector2(0f, 1f));
            AddRectBackground(shadow, new Color(0f, 0f, 0f, 0.34f));
            var glowRect = CreateRect("HoverGlow", rect, new Vector2(-14f, 10f), new Vector2(CardGridCardWidth + 42f, CardGridCardHeight + 40f), new Vector2(0f, 1f));
            AddRectBackground(glowRect, new Color(0.28f, 0.82f, 1f, 0f));
            glowRect.SetAsFirstSibling();
            var hoverGlow = glowRect.GetComponent<Image>();
            var face = CreateRawImage("Face", rect, new Vector2(6f, -4f), new Vector2(CardGridCardWidth, CardGridCardHeight));
            var hoverName = CreateText("HoverName", rect, new Vector2(-52f, -CardGridCardHeight - 54f), new Vector2(CardGridItemWidth + 104f, 46f), 25f, TextAlignmentOptions.Center);
            hoverName.text = GetCardName(card);
            hoverName.color = new Color(1f, 1f, 1f, 0f);
            StartCoroutine(LoadCardTexture(face, card == null || card.GetData() == null ? 0 : card.GetData().Id));
            floater.BindHoverVisuals(hoverGlow, hoverName, CardGridHoverScaleBoost, () =>
            {
                ShowCardDetail(card);
                UpdateCardButtons();
            });
            return item;
        }

        private static Vector2 ResolveFloatingCardGridPosition(int column, int row)
        {
            var center = (CardGridColumns - 1) * 0.5f;
            var offset = column - center;
            var x = column * CardGridColumnSpacing + row * CardGridRowOffsetX;
            var y = -row * CardGridRowSpacing - offset * offset * CardGridCurveDrop;
            return new Vector2(x, y);
        }

        private static float ResolveFloatingCardGridRotation(int column, int row)
        {
            var center = (CardGridColumns - 1) * 0.5f;
            var offset = column - center;
            var rowTilt = row == 0 ? 1.0f : 0.74f;
            return -offset * CardGridFanAngle * rowTilt;
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
                    image.color = new Color(0.10f, 0.32f, 0.45f, 0.28f);
                else
                    image.color = new Color(0f, 0f, 0f, 0.01f);

                if (selectedCards.Contains(card))
                    image.color = new Color(0.12f, 0.82f, 0.44f, 0.34f);
                if (IsForcedSelectedCard(card))
                    image.color = new Color(1f, 0.70f, 0.18f, 0.38f);

                if (cardItemFloaters.TryGetValue(card, out var floater) && floater != null)
                    floater.SetState(
                        selectedCards.Contains(card),
                        card == detailCard,
                        IsForcedSelectedCard(card),
                        cardReadOnly);
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

        private void PopulateCardDetailForPreview(GameCard card, Card data)
        {
            if (cardDetailShowingActionHint || cardDetailPinnedCard != null)
                return;

            PopulateCardDetail(card, data, null, null);
        }

        private void PopulatePinnedCardDetail()
        {
            if (cardDetailPinnedCard == null)
                return;

            PopulateCardDetail(
                cardDetailPinnedCard,
                cardDetailPinnedCard.GetData(),
                "\u5df2\u56fa\u5b9a\uff1a" + GetCardName(cardDetailPinnedCard),
                BuildPinnedCardActionText(cardDetailPinnedCard));
        }

        private void RestoreCardDetailAfterActionHint()
        {
            if (cardDetailShowingActionHint)
                return;

            if (cardDetailActionRestoreCard != null && cardDetailPinnedCard == null)
            {
                cardDetailHoverCard = cardDetailActionRestoreCard;
                cardDetailActionRestoreCard = null;
            }

            if (cardDetailPinnedCard != null)
            {
                cardDetailActionRestoreCard = null;
                PopulatePinnedCardDetail();
                if (cardDetailCanvas != null)
                    cardDetailCanvas.gameObject.SetActive(true);
                UpdatePanelPoses();
                return;
            }

            if (cardDetailHoverCard != null)
            {
                PopulateCardDetail(cardDetailHoverCard, cardDetailHoverCard.GetData(), null, null);
                if (cardDetailCanvas != null)
                    cardDetailCanvas.gameObject.SetActive(true);
                UpdatePanelPoses();
                return;
            }

            cardDetailActionRestoreCard = null;
            HideCardDetailPanel();
        }

        private static string BuildPinnedCardActionText(GameCard card)
        {
            var text = BuildCardActionSummary(card);
            if (!string.IsNullOrWhiteSpace(text))
                text += "\n\n";
            text += "\u8fd9\u5f20\u5361\u7684\u8be6\u60c5\u5df2\u4fdd\u6301\u663e\u793a\u3002\n\u53f3\u624b\u6447\u6746\u518d\u6309\u4e00\u6b21\u53d6\u6d88\u56fa\u5b9a\uff1b\u70b9\u51fb\u7a7a\u767d\u533a\u57df\u4e5f\u4f1a\u5173\u95ed\u3002";
            return text;
        }

        private void PopulateCardDetail(GameCard card, Card data, string actionTitle, string actionDescription)
        {
            if (cardDetailNameText == null)
                return;

            if (data == null)
            {
                cardDetailNameText.text = "\u672a\u77e5\u5361\u7247";
                cardDetailMetaText.text = string.Empty;
                cardDetailStateText.text = BuildCardRuntimeState(card);
                cardDetailDescriptionText.text = "\u65e0\u6548\u679c\u6587\u672c";
                cardDetailTextureRequestCode = 0;
                if (cardDetailImage != null)
                    cardDetailImage.texture = TextureManager.container == null ? null : TextureManager.container.unknownCard.texture;
            }
            else
            {
                cardDetailNameText.text = SanitizeText(data.Name);
                cardDetailMetaText.text = BuildCardInfoMeta(data);
                cardDetailStateText.text = BuildCardRuntimeState(card);
                cardDetailDescriptionText.text = BuildCardDescriptionText(data);
                StartCoroutine(LoadCardDetailTexture(data.Id));
            }

            var title = string.IsNullOrWhiteSpace(actionTitle) ? "\u5f53\u524d\u64cd\u4f5c" : SanitizeText(actionTitle);
            var body = string.IsNullOrWhiteSpace(actionDescription) ? BuildCardActionSummary(card) : SanitizeText(actionDescription);
            cardDetailActionTitleText.text = LocalizeQuestLabel(title);
            cardDetailActionText.text = LocalizeQuestLabel(body);
        }

        private IEnumerator LoadCardInfoTexture(int code)
        {
            cardInfoTextureRequestCode = code;
            if (cardInfoImage == null)
                yield break;

            cardInfoImage.texture = TextureManager.container == null ? null : TextureManager.container.unknownCard.texture;
            if (code <= 0)
                yield break;

            var task = TextureManager.LoadQuestFieldCardTextureAsync(code, true);
            while (!task.IsCompleted)
                yield return null;

            if (cardInfoImage != null && cardInfoTextureRequestCode == code && task.Result != null)
                cardInfoImage.texture = task.Result;
        }

        private IEnumerator LoadCardDetailTexture(int code)
        {
            cardDetailTextureRequestCode = code;
            if (cardDetailImage == null)
                yield break;

            cardDetailImage.texture = TextureManager.container == null ? null : TextureManager.container.unknownCard.texture;
            if (code <= 0)
                yield break;

            var task = TextureManager.LoadQuestFieldCardTextureAsync(code, true);
            while (!task.IsCompleted)
                yield return null;

            if (cardDetailImage != null && cardDetailTextureRequestCode == code && task.Result != null)
                cardDetailImage.texture = task.Result;
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
            {
                cardPanelCanvas.gameObject.SetActive(false);
                NotifyUiVisibilityChanged();
            }
        }

        private void HideCardInfoPanel()
        {
            if (cardInfoCanvas != null && cardInfoCanvas.gameObject.activeSelf)
                cardInfoCanvas.gameObject.SetActive(false);
            cardInfoUseWorldPose = false;
            cardDetailHoverCard = null;
            if (!cardDetailShowingActionHint)
                RestoreCardDetailAfterActionHint();
        }

        private void HideCardDetailPanel()
        {
            if (cardDetailCanvas != null && cardDetailCanvas.gameObject.activeSelf)
                cardDetailCanvas.gameObject.SetActive(false);
        }

        private void HideOptionPanel()
        {
            if (optionCanvas != null && optionCanvas.gameObject.activeSelf)
            {
                optionCanvas.gameObject.SetActive(false);
                NotifyUiVisibilityChanged();
            }
            optionAnchorCard = null;
            optionAnchoredCompact = false;
            optionAnchoredCount = 0;
        }

        private void HidePhaseMenu()
        {
            if (phaseMenuCanvas != null && phaseMenuCanvas.gameObject.activeSelf)
            {
                phaseMenuCanvas.gameObject.SetActive(false);
                NotifyUiVisibilityChanged();
            }
        }

        private static void NotifyUiVisibilityChanged()
        {
            UiVisibilityChanged?.Invoke();
        }

        private void UpdatePanelPoses()
        {
            if (xrCamera == null)
                return;

            if (cardPanelRect != null && cardPanelCanvas.gameObject.activeSelf)
                PlacePanel(cardPanelRect, DuelWorldCenterOnGround + new Vector3(0f, 10.8f, -18f), CardSelectorScale);
            if (cardInfoRect != null && cardInfoCanvas.gameObject.activeSelf)
            {
                if (cardInfoUseWorldPose)
                    PlacePanelWorld(cardInfoRect, cardInfoWorldPosition, cardInfoWorldRotation, HoverCardInfoScale);
                else
                    PlacePanel(
                        cardInfoRect,
                        DuelWorldCenterOnGround + new Vector3(-82f, 25.2f, -48f),
                        LeftSideWallRotation,
                        CardInfoScale);
            }
            if (cardDetailRect != null && cardDetailCanvas.gameObject.activeSelf)
                PlacePanel(
                    cardDetailRect,
                    DuelWorldCenterOnGround + new Vector3(-60.8f, 34.0f, -18f),
                    LeftSideWallRotation,
                    SideCardDetailScale);
            if (optionRect != null && optionCanvas.gameObject.activeSelf)
            {
                var optionPosition = ResolveOptionPanelPosition();
                if (optionAnchoredCompact)
                    PlacePanel(optionRect, optionPosition, ResolveFacingViewerRotationInDuelSpace(optionPosition), AnchoredOptionPanelScale);
                else
                    PlacePanel(optionRect, optionPosition, SmallPanelScale);
            }
            if (phaseMenuRect != null && phaseMenuCanvas.gameObject.activeSelf)
                PlacePanel(phaseMenuRect, DuelWorldCenterOnGround + new Vector3(0f, 8.4f, -18f), SmallPanelScale);
            if (phaseHudRect != null && phaseHudCanvas.gameObject.activeSelf)
                PlacePanel(
                    phaseHudRect,
                    DuelWorldCenterOnGround + new Vector3(60.8f, 41.5f, -55f),
                    RightSideWallRotation,
                    FloorHudScale);
            if (controlHudRect != null && controlHudCanvas.gameObject.activeSelf)
                PlacePanel(
                    controlHudRect,
                    DuelWorldCenterOnGround + new Vector3(60.8f, 11.4f, -47f),
                    RightSideWallRotation,
                    ControlHudScale);
            if (duelLogRect != null && duelLogCanvas.gameObject.activeSelf)
                PlacePanel(
                    duelLogRect,
                    DuelWorldCenterOnGround + new Vector3(60.8f, 28.0f, 13.5f),
                    RightSideWallRotation,
                    DuelLogPanelScale);
        }

        private Vector3 ResolveOptionPanelPosition()
        {
            if (optionAnchorCard == null || optionAnchorCard.p == null)
                return DuelWorldCenterOnGround + new Vector3(0f, 7.2f, -20f);

            var position = GameCard.GetCardPosition(optionAnchorCard.p, optionAnchorCard, optionAnchorCard.overlayParent);
            position.x *= QuestBoardScaleX;
            position.z *= QuestBoardScaleZ;
            position.y = Mathf.Max(position.y + 4.8f, 5.8f);
            var duelPosition = DuelWorldCenterOnGround + position;
            return duelPosition + ResolvePlanarDirectionToViewerInDuelSpace(duelPosition) * 4.6f;
        }

        private Vector3 ResolvePlanarDirectionToViewerInDuelSpace(Vector3 duelPosition)
        {
            Vector3 viewer;
            if (xrCamera == null)
            {
                viewer = DuelWorldCenterOnGround + new Vector3(0f, 24f, -54f);
            }
            else if (duelWorldAnchor != null)
            {
                viewer = duelWorldAnchor.InverseTransformPoint(xrCamera.transform.position) + DuelWorldCenterOnGround;
            }
            else
            {
                viewer = xrCamera.transform.position;
            }

            var direction = viewer - duelPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.back;
            return direction.normalized;
        }

        private Quaternion ResolveFacingViewerRotationInDuelSpace(Vector3 duelPosition)
        {
            var toViewer = ResolvePlanarDirectionToViewerInDuelSpace(duelPosition);
            var forward = -toViewer;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void PlacePanel(RectTransform rect, Vector3 position, float scale)
        {
            PlacePanel(rect, position, Quaternion.identity, scale);
        }

        private void PlacePanel(RectTransform rect, Vector3 position, Quaternion rotation, float scale)
        {
            if (duelWorldAnchor != null && rect.parent == duelWorldAnchor)
            {
                rect.localPosition = position - DuelWorldCenterOnGround;
                rect.localRotation = rotation;
            }
            else
            {
                rect.position = position;
                rect.rotation = rotation;
            }
            rect.localScale = Vector3.one * scale;
        }

        private void PlacePanelWorld(RectTransform rect, Vector3 position, Quaternion rotation, float scale)
        {
            if (duelWorldAnchor != null && rect.parent == duelWorldAnchor)
            {
                rect.localPosition = duelWorldAnchor.InverseTransformPoint(position);
                rect.localRotation = Quaternion.Inverse(duelWorldAnchor.rotation) * rotation;
            }
            else
            {
                rect.position = position;
                rect.rotation = rotation;
            }

            rect.localScale = Vector3.one * scale;
        }

        private void UpdateCardInfoWorldPose(Bounds? worldBounds)
        {
            cardInfoUseWorldPose = worldBounds.HasValue;
            if (!worldBounds.HasValue || cardInfoRect == null)
                return;

            var bounds = worldBounds.Value;
            var center = bounds.center;
            var toViewer = ResolvePlanarDirectionToViewer(center);
            var parentScale = duelWorldAnchor == null
                ? 1f
                : Mathf.Max(duelWorldAnchor.lossyScale.x, 0.0001f);
            var panelHeightWorld = cardInfoRect.sizeDelta.y * HoverCardInfoScale * parentScale;
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            var viewerOffset = Mathf.Clamp(radius * 0.20f, parentScale * 0.24f, parentScale * 1.05f);
            var verticalLift = panelHeightWorld * 0.58f + Mathf.Clamp(parentScale * 0.50f, 0.34f, 1.55f);
            cardInfoWorldPosition = new Vector3(center.x, bounds.max.y + verticalLift, center.z)
                + toViewer * viewerOffset;
            cardInfoWorldRotation = ResolveFacingViewerRotation(cardInfoWorldPosition);
        }

        private Vector3 ResolvePlanarDirectionToViewer(Vector3 position)
        {
            var viewer = xrCamera == null ? DuelWorldCenterOnGround + new Vector3(0f, 24f, -54f) : xrCamera.transform.position;
            var direction = viewer - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.back;
            return direction.normalized;
        }

        private Quaternion ResolveFacingViewerRotation(Vector3 position)
        {
            var toViewer = ResolvePlanarDirectionToViewer(position);
            var forward = -toViewer;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
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

        private static void AddHudPanelChrome(RectTransform rect, Color accent)
        {
            if (rect == null)
                return;

            var size = rect.sizeDelta;
            var inner = CreateRect("ChromeInner", rect, new Vector2(18f, -24f), new Vector2(size.x - 36f, size.y - 48f), new Vector2(0f, 1f));
            AddRectBackground(inner, HudPanelInner);

            var top = CreateRect("ChromeTop", rect, Vector2.zero, new Vector2(size.x, 12f), new Vector2(0f, 1f));
            AddRectBackground(top, accent);

            var left = CreateRect("ChromeLeft", rect, new Vector2(0f, -12f), new Vector2(10f, size.y - 24f), new Vector2(0f, 1f));
            AddRectBackground(left, new Color(accent.r, accent.g, accent.b, accent.a * 0.72f));

            var bottom = CreateRect("ChromeBottom", rect, new Vector2(18f, -size.y + 14f), new Vector2(size.x - 36f, 4f), new Vector2(0f, 1f));
            AddRectBackground(bottom, new Color(accent.r, accent.g, accent.b, accent.a * 0.42f));
        }

        private static RectTransform AddHudSection(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color accent)
        {
            var section = CreateRect(name, parent, anchoredPosition, size, new Vector2(0f, 1f));
            AddRectBackground(section, new Color(0.030f, 0.044f, 0.056f, 0.54f));

            var rail = CreateRect(name + "Rail", section, Vector2.zero, new Vector2(7f, size.y), new Vector2(0f, 1f));
            AddRectBackground(rail, new Color(accent.r, accent.g, accent.b, accent.a * 0.72f));

            var line = CreateRect(name + "TopLine", section, Vector2.zero, new Vector2(size.x, 3f), new Vector2(0f, 1f));
            AddRectBackground(line, new Color(accent.r, accent.g, accent.b, accent.a * 0.58f));
            return section;
        }

        private static TextMeshProUGUI CreateHudCaption(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label)
        {
            var text = CreateText(name, parent, anchoredPosition, size, 26f, TextAlignmentOptions.Left);
            text.text = label;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.68f, 0.94f, 1f, 0.96f);
            return text;
        }

        private static void AddButtonChrome(Button button, Color accent)
        {
            if (button == null)
                return;

            var rect = button.transform as RectTransform;
            if (rect == null)
                return;

            var size = rect.sizeDelta;
            var top = CreateRect("ButtonTopLine", rect, Vector2.zero, new Vector2(size.x, 6f), new Vector2(0f, 1f));
            AddRectBackground(top, new Color(accent.r, accent.g, accent.b, accent.a * 0.78f));
            top.SetAsFirstSibling();

            var left = CreateRect("ButtonLeftLine", rect, Vector2.zero, new Vector2(5f, size.y), new Vector2(0f, 1f));
            AddRectBackground(left, new Color(accent.r, accent.g, accent.b, accent.a * 0.58f));
            left.SetAsFirstSibling();
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

            var labelFontSize = Mathf.Clamp(size.y * 0.30f, 25f, 42f);
            var text = CreateText("Label", rect, new Vector2(12f, -8f), new Vector2(size.x - 24f, size.y - 16f), labelFontSize, TextAlignmentOptions.Center);
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
            if (card == null || card.GetData() == null)
                return "\u672a\u77e5\u5361\u7247";
            return card == null || card.GetData() == null ? "未知卡片" : SanitizeText(card.GetData().Name);
        }

        private static string GetShortCardMeta(GameCard card)
        {
            var data = card == null ? null : card.GetData();
            if (data == null)
                return string.Empty;
            if (data.HasType(CardType.Monster))
                return "\u653b\u51fb " + data.GetAttackString() + "\n\u5b88\u5907 " + data.GetDefenseString();
            if (data.HasType(CardType.Spell))
                return "魔法";
            if (data.HasType(CardType.Trap))
                return "陷阱";
            return string.Empty;
        }

        private static string BuildCardInfoMeta(Card data)
        {
            if (data == null)
                return string.Empty;

            if (data.HasType(CardType.Monster))
            {
                var text = "<b>" + GetMonsterFrameLabel(data) + "</b>\n";
                text += GetMonsterGradeText(data) + "  " + GetAttributeLabel(data.Attribute) + "\n";
                text += GetRaceLabel(data.Race) + " / " + GetMonsterSubtypeText(data) + "\n";
                text += "<color=#FFD36B>\u653b\u51fb</color> " + data.GetAttackString();
                if (data.HasType(CardType.Link))
                    text += "  <color=#69D7FF>LINK</color> " + data.GetLinkCount();
                else
                    text += "  <color=#69D7FF>\u5b88\u5907</color> " + data.GetDefenseString();
                return text;
            }

            if (data.HasType(CardType.Spell))
                return "<b>\u9b54\u6cd5\u5361</b>\n" + GetSpellTrapSubtypeText(data);
            if (data.HasType(CardType.Trap))
                return "<b>\u9677\u9631\u5361</b>\n" + GetSpellTrapSubtypeText(data);
            return "\u5361\u7247";
        }

        private static string BuildCardRuntimeState(GameCard card)
        {
            if (card == null || card.p == null)
                return "\u672a\u77e5\u72b6\u6001";

            var data = card.GetData();
            var text = GetControllerName(card.p.controller) + "\n";
            text += GetLocationName((CardLocation)card.p.location);
            text += "  #" + ((int)card.p.sequence + 1) + "\n";
            text += GetPositionText(card.p.position, data) + "\n";
            if (data != null && data.HasType(CardType.Monster))
            {
                text += "\u5f53\u524d \u653b\u51fb " + data.GetAttackString();
                if (!data.HasType(CardType.Link))
                    text += " / \u5b88\u5907 " + data.GetDefenseString();
                if (data.Attack != data.rAttack || data.Defense != data.rDefense)
                    text += "\n<color=#FFD36B>\u6570\u503c\u5df2\u53d8\u5316</color>";
            }
            else
            {
                text += IsFaceUp(card.p.position) ? "\u5df2\u516c\u5f00" : "\u672a\u516c\u5f00";
            }
            return text;
        }

        private static string BuildCardActionSummary(GameCard card)
        {
            if (card == null || card.buttons == null || card.buttons.Count == 0)
                return "\u6682\u65e0\u53ef\u7528\u64cd\u4f5c";

            var labels = new List<string>();
            foreach (var button in card.buttons)
            {
                var label = GetCardActionLabel(button.type);
                if (!string.IsNullOrEmpty(label) && !labels.Contains(label))
                    labels.Add(label);
            }

            if (labels.Count == 0)
                return "\u6682\u65e0\u53ef\u7528\u64cd\u4f5c";

            var text = string.Empty;
            for (var i = 0; i < labels.Count && i < 5; i += 1)
                text += "\u2022 " + labels[i] + "\n";
            if (labels.Count > 5)
                text += "\u2022 ...";
            return text.TrimEnd();
        }

        private static string BuildCardDescriptionText(Card data)
        {
            if (data == null)
                return string.Empty;

            var text = SanitizeText(data.GetDescription(true));
            text = LocalizeQuestLabel(text).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return "\u65e0\u6548\u679c\u6587\u672c";
            return text;
        }

        private static string GetMonsterFrameLabel(Card data)
        {
            if (data == null)
                return "\u602a\u517d\u5361";
            if (data.HasType(CardType.Link))
                return "\u8fde\u63a5\u602a\u517d";
            if (data.HasType(CardType.Xyz))
                return "\u8d85\u91cf\u602a\u517d";
            if (data.HasType(CardType.Synchro))
                return "\u540c\u8c03\u602a\u517d";
            if (data.HasType(CardType.Fusion))
                return "\u878d\u5408\u602a\u517d";
            if (data.HasType(CardType.Ritual))
                return "\u4eea\u5f0f\u602a\u517d";
            if (data.HasType(CardType.Token))
                return "\u884d\u751f\u7269";
            if (data.HasType(CardType.Normal) && !data.HasType(CardType.Effect))
                return "\u901a\u5e38\u602a\u517d";
            return "\u6548\u679c\u602a\u517d";
        }

        private static string GetMonsterGradeText(Card data)
        {
            if (data == null)
                return string.Empty;
            if (data.HasType(CardType.Link))
                return "LINK " + data.GetLinkCount();
            if (data.HasType(CardType.Xyz))
                return "\u9636\u7ea7 " + Mathf.Max(0, data.Level);
            return "\u7b49\u7ea7 " + Mathf.Max(0, data.Level);
        }

        private static string GetMonsterSubtypeText(Card data)
        {
            if (data == null)
                return string.Empty;

            var labels = new List<string>();
            if (data.HasType(CardType.Pendulum))
                labels.Add("\u7075\u6446");
            if (data.HasType(CardType.Tuner))
                labels.Add("\u8c03\u6574");
            if (data.HasType(CardType.Spirit))
                labels.Add("\u7075\u9b42");
            if (data.HasType(CardType.Union))
                labels.Add("\u540c\u76df");
            if (data.HasType(CardType.Dual))
                labels.Add("\u4e8c\u91cd");
            if (data.HasType(CardType.Flip))
                labels.Add("\u53cd\u8f6c");
            if (data.HasType(CardType.Toon))
                labels.Add("\u5361\u901a");
            if (data.HasType(CardType.Effect))
                labels.Add("\u6548\u679c");
            if (labels.Count == 0)
                labels.Add("\u901a\u5e38");
            return string.Join(" / ", labels);
        }

        private static string GetSpellTrapSubtypeText(Card data)
        {
            if (data == null)
                return string.Empty;

            var labels = new List<string>();
            if (data.HasType(CardType.Field))
                labels.Add("\u573a\u5730");
            if (data.HasType(CardType.Equip))
                labels.Add("\u88c5\u5907");
            if (data.HasType(CardType.QuickPlay))
                labels.Add("\u901f\u653b");
            if (data.HasType(CardType.Ritual))
                labels.Add("\u4eea\u5f0f");
            if (data.HasType(CardType.Continuous))
                labels.Add("\u6c38\u7eed");
            if (data.HasType(CardType.Counter))
                labels.Add("\u53cd\u51fb");
            return labels.Count == 0 ? "\u901a\u5e38" : string.Join(" / ", labels);
        }

        private static string GetAttributeLabel(int attribute)
        {
            switch ((CardAttribute)attribute)
            {
                case CardAttribute.Earth:
                    return "\u5730\u5c5e\u6027";
                case CardAttribute.Water:
                    return "\u6c34\u5c5e\u6027";
                case CardAttribute.Fire:
                    return "\u708e\u5c5e\u6027";
                case CardAttribute.Wind:
                    return "\u98ce\u5c5e\u6027";
                case CardAttribute.Light:
                    return "\u5149\u5c5e\u6027";
                case CardAttribute.Dark:
                    return "\u6697\u5c5e\u6027";
                case CardAttribute.Divine:
                    return "\u795e\u5c5e\u6027";
                default:
                    return "\u5c5e\u6027 " + attribute;
            }
        }

        private static string GetRaceLabel(int race)
        {
            switch ((CardRace)race)
            {
                case CardRace.Warrior:
                    return "\u6218\u58eb\u65cf";
                case CardRace.SpellCaster:
                    return "\u9b54\u6cd5\u4f7f\u65cf";
                case CardRace.Fairy:
                    return "\u5929\u4f7f\u65cf";
                case CardRace.Fiend:
                    return "\u6076\u9b54\u65cf";
                case CardRace.Zombie:
                    return "\u4e0d\u6b7b\u65cf";
                case CardRace.Machine:
                    return "\u673a\u68b0\u65cf";
                case CardRace.Aqua:
                    return "\u6c34\u65cf";
                case CardRace.Pyro:
                    return "\u708e\u65cf";
                case CardRace.Rock:
                    return "\u5ca9\u77f3\u65cf";
                case CardRace.WindBeast:
                    return "\u9e1f\u517d\u65cf";
                case CardRace.Plant:
                    return "\u690d\u7269\u65cf";
                case CardRace.Insect:
                    return "\u6606\u866b\u65cf";
                case CardRace.Thunder:
                    return "\u96f7\u65cf";
                case CardRace.Dragon:
                    return "\u9f99\u65cf";
                case CardRace.Beast:
                    return "\u517d\u65cf";
                case CardRace.BeastWarrior:
                    return "\u517d\u6218\u58eb\u65cf";
                case CardRace.Dinosaur:
                    return "\u6050\u9f99\u65cf";
                case CardRace.Fish:
                    return "\u9c7c\u65cf";
                case CardRace.SeaSerpent:
                    return "\u6d77\u9f99\u65cf";
                case CardRace.Reptile:
                    return "\u722c\u866b\u7c7b\u65cf";
                case CardRace.Psycho:
                    return "\u5ff5\u52a8\u529b\u65cf";
                case CardRace.DivineBeast:
                    return "\u5e7b\u795e\u517d\u65cf";
                case CardRace.CreatorGod:
                    return "\u521b\u9020\u795e\u65cf";
                case CardRace.Wyrm:
                    return "\u5e7b\u9f99\u65cf";
                case CardRace.Cyberse:
                    return "\u7535\u5b50\u754c\u65cf";
                case CardRace.Illustion:
                    return "\u5e7b\u60f3\u9b54\u65cf";
                default:
                    return "\u79cd\u65cf " + race;
            }
        }

        private static string GetPositionText(int position, Card data)
        {
            if ((position & (int)CardPosition.FaceDown) > 0)
                return (position & (int)CardPosition.Defence) > 0 ? "\u91cc\u4fa7\u5b88\u5907" : "\u91cc\u4fa7\u653b\u51fb";
            if (data != null && data.HasType(CardType.Link))
                return "\u8868\u4fa7\u8fde\u63a5";
            return (position & (int)CardPosition.Defence) > 0 ? "\u8868\u4fa7\u5b88\u5907" : "\u8868\u4fa7\u653b\u51fb";
        }

        private static bool IsFaceUp(int position)
        {
            return (position & (int)CardPosition.FaceUp) > 0;
        }

        private static string GetCardActionLabel(ButtonType type)
        {
            switch (type)
            {
                case ButtonType.Activate:
                    return "\u53d1\u52a8";
                case ButtonType.Battle:
                    return "\u653b\u51fb";
                case ButtonType.SpSummon:
                    return "\u7279\u6b8a\u53ec\u5524";
                case ButtonType.Summon:
                    return "\u901a\u5e38\u53ec\u5524";
                case ButtonType.PenSummon:
                    return "\u7075\u6446\u53ec\u5524";
                case ButtonType.SetMonster:
                case ButtonType.SetSpell:
                    return "\u653e\u7f6e";
                case ButtonType.SetPendulum:
                    return "\u7075\u6446\u653e\u7f6e";
                case ButtonType.ToAttackPosition:
                    return "\u653b\u51fb\u8868\u793a";
                case ButtonType.ToDefensePosition:
                    return "\u5b88\u5907\u8868\u793a";
                case ButtonType.Select:
                    return "\u9009\u62e9";
                case ButtonType.Decide:
                    return "\u786e\u5b9a";
                case ButtonType.Cancel:
                    return "\u53d6\u6d88";
                default:
                    return type.ToString();
            }
        }

        private static string GetLongCardMeta(Card data)
        {
            if (data == null)
                return string.Empty;
            if (data.HasType(CardType.Monster))
            {
                var levelLabel = data.HasType(CardType.Link) ? "LINK " + data.GetLinkCount() : "等级 " + data.Level;
                return levelLabel + "\n\u653b\u51fb " + data.GetAttackString() + " / \u5b88\u5907 " + data.GetDefenseString();
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
                    return "\u62bd\u5361\u9636\u6bb5";
                case DuelPhase.Standby:
                    return "\u51c6\u5907\u9636\u6bb5";
                case DuelPhase.Main1:
                    return "\u4e3b\u8981\u9636\u6bb51";
                case DuelPhase.BattleStart:
                case DuelPhase.Battle:
                case DuelPhase.BattleStep:
                case DuelPhase.Damage:
                case DuelPhase.DamageCal:
                    return "\u6218\u6597\u9636\u6bb5";
                case DuelPhase.Main2:
                    return "\u4e3b\u8981\u9636\u6bb52";
                case DuelPhase.End:
                    return "\u7ed3\u675f\u9636\u6bb5";
            }

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
            var value = (uint)location;
            if ((value & (uint)CardLocation.Hand) > 0)
                return "\u624b\u724c";
            if ((value & (uint)CardLocation.MonsterZone) > 0)
                return "\u602a\u517d\u533a";
            if ((value & (uint)CardLocation.SpellZone) > 0)
                return "\u9b54\u9677\u533a";
            if ((value & (uint)CardLocation.Deck) > 0)
                return "\u4e3b\u5361\u7ec4";
            if ((value & (uint)CardLocation.Extra) > 0)
                return "\u989d\u5916\u5361\u7ec4";
            if ((value & (uint)CardLocation.Grave) > 0)
                return "\u5893\u5730";
            if ((value & (uint)CardLocation.Removed) > 0)
                return "\u9664\u5916\u533a";
            if ((value & (uint)CardLocation.Overlay) > 0)
                return "\u53e0\u653e\u7d20\u6750";
            if ((value & (uint)CardLocation.FieldZone) > 0)
                return "\u573a\u5730\u533a";
            if ((value & (uint)CardLocation.PendulumZone) > 0)
                return "\u7075\u6446\u533a";
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
            return controller == 0 ? "\u6211\u65b9" : "\u5bf9\u65b9";
        }

        private static string SanitizeText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r", string.Empty);
        }

        private sealed class QuestFloatingCardGridItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private RectTransform rect;
            private Vector2 baseAnchoredPosition;
            private Vector3 baseScale = Vector3.one;
            private float baseRotationZ;
            private float phase;
            private float amplitude;
            private float selectedLift;
            private float detailLift;
            private float hoverLift;
            private float hoverScaleBoost;
            private float hoverBlend;
            private Image hoverGlow;
            private TextMeshProUGUI hoverName;
            private Action hoverAction;
            private int originalSiblingIndex = -1;
            private bool restoreSiblingWhenSettled;
            private bool selected;
            private bool detail;
            private bool forced;
            private bool readOnly;
            private bool pointerHovered;

            public void Configure(
                RectTransform rectTransform,
                Vector2 basePosition,
                float baseRotationDegrees,
                float phaseOffset,
                float bobAmplitude,
                float selectedLiftAmount,
                float detailLiftAmount,
                float hoverLiftAmount)
            {
                rect = rectTransform;
                baseAnchoredPosition = basePosition;
                baseScale = rect == null ? Vector3.one : rect.localScale;
                baseRotationZ = baseRotationDegrees;
                phase = phaseOffset;
                amplitude = bobAmplitude;
                selectedLift = selectedLiftAmount;
                detailLift = detailLiftAmount;
                hoverLift = hoverLiftAmount;
            }

            public void BindHoverVisuals(Image glowImage, TextMeshProUGUI nameText, float scaleBoost, Action onHovered)
            {
                hoverGlow = glowImage;
                hoverName = nameText;
                hoverAction = onHovered;
                hoverScaleBoost = Mathf.Max(0f, scaleBoost);
                SetHoverVisualAlpha(0f);
            }

            public void SetState(bool isSelected, bool isDetail, bool isForced, bool isReadOnly)
            {
                selected = isSelected;
                detail = isDetail;
                forced = isForced;
                readOnly = isReadOnly;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                pointerHovered = true;
                hoverAction?.Invoke();
                restoreSiblingWhenSettled = false;
                if (rect != null)
                {
                    originalSiblingIndex = rect.GetSiblingIndex();
                    rect.SetAsLastSibling();
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                pointerHovered = false;
                restoreSiblingWhenSettled = true;
            }

            private void OnDisable()
            {
                pointerHovered = false;
                restoreSiblingWhenSettled = false;
                hoverBlend = 0f;
                SetHoverVisualAlpha(0f);
                if (rect != null)
                {
                    rect.anchoredPosition = baseAnchoredPosition;
                    rect.localRotation = Quaternion.Euler(0f, 0f, baseRotationZ);
                    rect.localScale = baseScale;
                }
            }

            private void Update()
            {
                if (rect == null)
                    return;

                var lift = 0f;
                if (selected || forced)
                    lift += selectedLift + (forced ? 8f : 0f);
                if (detail && !selected && !forced)
                    lift += detailLift;
                if (pointerHovered)
                    lift += hoverLift;

                hoverBlend = Mathf.MoveTowards(hoverBlend, pointerHovered ? 1f : 0f, Time.unscaledDeltaTime * 7.5f);
                var bob = Mathf.Sin(Time.unscaledTime * 2.35f + phase) * amplitude;
                var targetPosition = baseAnchoredPosition + new Vector2(0f, lift + bob);
                var scale = 1f
                    + ((selected || forced) ? 0.065f : 0f)
                    + (detail ? 0.028f : 0f)
                    + hoverBlend * hoverScaleBoost
                    + (readOnly ? -0.012f : 0f);
                var delta = Mathf.Clamp01(Time.unscaledDeltaTime * 12f);
                var targetRotationZ = hoverBlend > 0.01f || selected || forced
                    ? Mathf.Lerp(baseRotationZ, 0f, Mathf.Lerp(0.58f, 0.88f, hoverBlend))
                    : baseRotationZ;
                rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, targetPosition, delta);
                rect.localRotation = Quaternion.Slerp(rect.localRotation, Quaternion.Euler(0f, 0f, targetRotationZ), delta);
                rect.localScale = Vector3.Lerp(rect.localScale, baseScale * scale, delta);
                SetHoverVisualAlpha(hoverBlend);

                if (restoreSiblingWhenSettled && hoverBlend <= 0.01f)
                {
                    RestoreSiblingIndex();
                    restoreSiblingWhenSettled = false;
                }
            }

            private void RestoreSiblingIndex()
            {
                if (rect == null || originalSiblingIndex < 0)
                    return;

                var parent = rect.parent;
                if (parent == null)
                    return;

                rect.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, parent.childCount - 1));
            }

            private void SetHoverVisualAlpha(float alpha)
            {
                if (hoverGlow != null)
                {
                    var color = hoverGlow.color;
                    color.a = 0.38f * alpha;
                    hoverGlow.color = color;
                }

                if (hoverName != null)
                {
                    var color = hoverName.color;
                    color.a = 0.94f * alpha;
                    hoverName.color = color;
                }
            }
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
