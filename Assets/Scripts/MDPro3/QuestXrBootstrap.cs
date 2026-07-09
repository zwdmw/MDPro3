using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using MDPro3.YGOSharp.OCGWrapper.Enums;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.Rendering.Universal;

namespace MDPro3
{
    [DefaultExecutionOrder(-5000)]
    public sealed class QuestXrBootstrap : MonoBehaviour
    {
        private const float DuelWorldUnitsPerMeter = 16f;
        private const bool UsePassthroughMixedReality = false;
        private const bool QuestEnableHandInteractionPointer = false;
        private const bool QuestUseNativeMainMenu = false;
        private const bool QuestNativeDuelFrontendOnly = true;
        private const bool QuestUseWorldSpaceMdproUi = false;
        private const float UiCanvasDistance = 46f;
        private const float UiCanvasHeightOffset = 4f;
        private const float UiCanvasWorldScale = 0.024f;
        private const float UiCanvasSideOffset = 0f;
        private const float UiCanvasVerticalStep = 7.5f;
        private const float UiCanvasMaxWidth = 56f;
        private const float UiCanvasMaxHeight = 31f;
        private const float WorldCanvasDepthStep = 0.025f;
        private const float WorldCanvasPanelOffset = 0.12f;
        private const float WorldCanvasPanelPadding = 1.4f;
        private const int FallbackQuestOverlayLayer = 24;
        private const int UiRenderTextureFixedWidth = 2880;
        private const int UiRenderTextureFixedHeight = 1620;
        private const float QuestEyeTextureResolutionScale = 1.35f;
        private const float QuestRenderViewportScale = 1f;
        private const float QuestUrpRenderScale = 1.15f;
        private static bool UiRenderTextureNeedsVerticalFlip
        {
            get
            {
                return false;
            }
        }
        private const bool UiRenderTextureNeedsHorizontalFlip = true;
        private const float UiRenderPanelWidth = 92f;
        private const float UiRenderPanelHeightOffset = 28f;
        private const float UiRenderPanelSideOffset = 0f;
        private const float UiRenderPanelForwardOffset = 54f;
        private const float QuestMainUiRecenterDistance = 92f;
        private const float ControllerRayLength = 30f * DuelWorldUnitsPerMeter;
        private const float ControllerRayStartWidth = 0.012f * DuelWorldUnitsPerMeter;
        private const float ControllerRayEndWidth = 0.004f * DuelWorldUnitsPerMeter;
        private const float ControllerCursorScale = 0.055f * DuelWorldUnitsPerMeter;
        private const float QuestDuelActionMenuScale = 0.034f;
        private const float QuestDuelActionMenuWidth = 680f;
        private const float QuestDuelActionMenuSingleColumnWidth = 360f;
        private const float QuestDuelActionMenuTwoColumnWidth = 560f;
        private const float QuestDuelActionRadialMenuWidth = 760f;
        private const float QuestDuelActionRadialMenuHeight = 390f;
        private const float QuestDuelActionRadialButtonWidth = 200f;
        private const float QuestDuelActionRadialButtonHeight = 76f;
        private const float QuestDuelActionMenuPadding = 14f;
        private const float QuestDuelActionItemHeight = 108f;
        private const float QuestDuelActionItemGap = 12f;
        private const float QuestDuelActionCardYOffset = 0.34f;
        private const float QuestDuelActionCardForwardOffset = 0.26f;
        private const float QuestDuelActionStemWidth = 5.5f;
        private const float QuestDuelActionStemMaxLength = 160f;
        private const float QuestDuelActionFloatAmplitude = 5.0f;
        private const float QuestDuelActionHoverScaleBoost = 0.12f;
        private const float QuestCardInfoHoverDelay = 0.10f;
        private const float QuestNoActionNoticeCooldown = 0.55f;
        private const float QuestDebugAutoActionStableDelay = 1.15f;
        private const float QuestDebugAutoActionRetryDelay = 3.5f;
        private const int QuestDebugAutoActionMaxPerDuel = 2;
        private static readonly MDPro3.UI.ButtonType[] QuestDebugAutoIdleActionPriority =
        {
            MDPro3.UI.ButtonType.Summon,
            MDPro3.UI.ButtonType.SpSummon,
            MDPro3.UI.ButtonType.SetMonster,
            MDPro3.UI.ButtonType.SetSpell
        };
        private const int FallbackGridLineCount = 33;
        private const float FallbackGridSpacing = 10f;
        private const float DuelGroundY = -0.005f;
        private const float DuelWorldTargetWidth = 112f;
        private const float DuelWorldTargetDepth = 92f;
        private const float DuelGameplayTableWidth = 106f;
        private const float DuelGameplayTableDepth = 90f;
        private const float DuelGameplayTableHeight = 4f;
        private const float DuelArenaWidth = 116f;
        private const float DuelArenaDepth = 94f;
        private const float DuelArenaLineY = 0.055f;
        private const float DuelArenaLineWidth = 0.055f;
        private const float DuelZoneWidth = 10.4f;
        private const float DuelZoneDepth = 11.1f;
        private const float DuelZoneGap = 0.9f;
        private const float DuelBoardScaleX = 1.38f;
        private const float DuelBoardScaleZ = 1.34f;
        private static readonly float[] DuelMainZoneX = { -28.0f, -14.0f, 0f, 14.0f, 28.0f };
        private const float DuelBaseTemplateMinArea = 80f;
        private const float DuelBaseTemplateMinHorizontalSize = 8f;
        private const float DuelBaseTemplateMaxRelativeThickness = 0.35f;
        private const float DuelGroundSnapThreshold = 0.025f;
        private const float DuelWorldBoundsLogInterval = 4f;
        private const float ControllerPoseDiagnosticInterval = 5f;
        private const float QuestInputRecoveryWarmupDuration = 18f;
        private const float QuestInputRecoveryPollInterval = 0.75f;
        private const float QuestInputMissingPosePollInterval = 2f;
        private const float QuestInputDeviceLogInterval = 4f;
        private const float QuestInputXrRestartDelay = 8f;
        private const float QuestInputXrRestartCooldown = 20f;
        private const int QuestInputXrRestartMaxAttempts = 1;
        private const float MdproCameraConfigureInterval = 0.5f;
        private const float QuestMainMenuUiRecoverySuppressDuration = 0.75f;
        private const float QuestMainMenuUiRecoveryDuration = 1.8f;
        private const float QuestMainMenuUiRecoveryInterval = 0.18f;
        private const float UiRenderCameraDepthBase = -200f;
        private const float UiRenderCameraDepthUi = 200f;
        private const int QuestDuelSleepCompressionThreshold = 20;
        private const int QuestDuelSleepCompressionMax = 55;
        private const float QuestDuelSleepCompressionScale = 0.35f;
        private const float QuestDuelSleepCompressionLogInterval = 3f;
        private const int QuestChainSleepCompressionMax = 12;
        private static bool QuestVerboseRuntimeDiagnostics => QuestRuntimeDebugSettings.VerboseDiagnostics;
        private const float QuestGraphicRaycasterCacheRefreshInterval = 1f;
        private const int QuestPhysicsRaycastBufferSize = 96;
        private const float QuestFoveatedRenderingLevel = 0.65f;
        private const float QuestPreviewCutinScale = 13.5f;
        private const float QuestPreviewMateTargetHeight = 30f;
        private const float QuestPreviewMateGroupSpacing = 18f;
        private const float QuestPreviewMoveSpeed = 18f;
        private const float QuestPreviewDepthSpeed = 22f;
        private const float QuestPreviewScaleSpeed = 1.2f;
        private const float QuestPreviewRayRotateSensitivityX = 260f;
        private const float QuestPreviewRayRotateSensitivityY = 190f;
        private const float QuestPreviewPitchLimit = 75f;
        private const float QuestPreviewHitPadding = 1.8f;
        private const string QuestAndroidDuelFieldNearPath = "MasterDuel/Mat/Mat_001_near";
        private const string QuestAndroidDuelFieldFarPath = "MasterDuel/Mat/Mat_001_far";
        private const float QuestAndroidDuelFieldRetryDelay = 6f;
        private const float QuestAndroidDuelFieldTargetWidth = DuelArenaWidth * 1.10f;
        private const float QuestAndroidDuelFieldTargetDepth = DuelArenaDepth * 1.10f;
        private const float QuestAndroidDuelFieldGroundY = DuelGroundY - 0.035f;
        private const float QuestThumbstickDeadZone = 0.18f;
        private const float QuestPlayerMoveSpeed = 14f;
        private const float QuestPlayerVerticalSpeed = 10f;
        private const float QuestWorldScaleSpeed = 0.65f;
        private const float QuestWorldScaleMinPositive = 0.0001f;
        private const float QuestPlayerYawTurnSpeed = 90f;
        private const float QuestWorldFastControlMultiplier = 3f;
        private const float QuestWorldControlAxisSmoothing = 8f;
        private const float QuestWorldControlLogInterval = 3f;
        private const float QuestDuelInactiveResetDelay = 1.25f;
        private static readonly Quaternion DuelWorldFloorRotation = Quaternion.identity;
        private static readonly Vector3 DuelEyePosition = new Vector3(0f, 24f, -54f);
        private static readonly Vector3 DuelLookTarget = new Vector3(0f, DuelGroundY + 0.5f, -1.5f);
        private static readonly Vector3 DuelWorldCenterOnGround = new Vector3(0f, DuelGroundY, -1.5f);
        private static readonly Vector3 QuestPreviewCutinDefaultOffset = new Vector3(0f, 18f, -18f);
        private static readonly Vector3 QuestPreviewMateDefaultOffset = new Vector3(25f, 0f, -14f);
        private static readonly InputFeatureUsage<Vector3> PointerPositionUsage = new InputFeatureUsage<Vector3>("PointerPosition");
        private static readonly InputFeatureUsage<Quaternion> PointerRotationUsage = new InputFeatureUsage<Quaternion>("PointerRotation");
        private static readonly InputFeatureUsage<Vector3> LowerPointerPositionUsage = new InputFeatureUsage<Vector3>("pointerPosition");
        private static readonly InputFeatureUsage<Quaternion> LowerPointerRotationUsage = new InputFeatureUsage<Quaternion>("pointerRotation");
        private static readonly InputFeatureUsage<Vector3> AimPositionUsage = new InputFeatureUsage<Vector3>("aimPosition");
        private static readonly InputFeatureUsage<Quaternion> AimRotationUsage = new InputFeatureUsage<Quaternion>("aimRotation");
        private static readonly InputFeatureUsage<Vector3> UpperAimPositionUsage = new InputFeatureUsage<Vector3>("AimPosition");
        private static readonly InputFeatureUsage<Quaternion> UpperAimRotationUsage = new InputFeatureUsage<Quaternion>("AimRotation");
        private static readonly List<UnityEngine.XR.InputDevice> ControllerDevices = new List<UnityEngine.XR.InputDevice>();
        private static readonly List<InputFeatureUsage> ControllerFeatureUsages = new List<InputFeatureUsage>();
        private static readonly List<XRDisplaySubsystem> XrDisplaySubsystems = new List<XRDisplaySubsystem>();

        private Camera xrCamera;
        private XROrigin xrOrigin;
        private Transform worldUiAnchor;
        private Transform duelWorldAnchor;
        private Transform anchoredDuelContainer;
        private Mouse virtualMouse;
        private Vector2 lastQueuedMousePos;
        private bool gameCamerasConfigured;
        private bool mdproCameraRenderModeInitialized;
        private bool lastMdproCameraRenderToUiPanel;
        private float lastMdproCameraConfigureTime;
        private float lastQuestDuelSleepCompressionLog;
        private bool worldUiLogged;
        private bool duelWorldRootLogged;
        private bool trackingOriginCalibrated;
        private bool trackingOriginCalibratedWithUserPresence;
        private bool trackingOriginModeRequested;
        private bool trackingOriginModeLogged;
        private bool passthroughConfigured;
        private bool passthroughVisibilityLogged;
        private float createdAt;
        private float lastTrackingWaitLog;
        private readonly HashSet<Canvas> configuredWorldCanvases = new HashSet<Canvas>();
        private readonly Dictionary<Canvas, int> worldCanvasSlots = new Dictionary<Canvas, int>();
        private readonly Dictionary<Canvas, GameObject> worldCanvasPanels = new Dictionary<Canvas, GameObject>();
        private readonly Dictionary<Canvas, RawImage> worldCanvasDecorations = new Dictionary<Canvas, RawImage>();
        private readonly HashSet<Canvas> loadingWorldCanvasDecorations = new HashSet<Canvas>();
        private readonly Dictionary<Canvas, LegacyCanvasState> suppressedLegacyDuelCanvases = new Dictionary<Canvas, LegacyCanvasState>();
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private readonly List<GraphicRaycaster> cachedGraphicRaycasters = new List<GraphicRaycaster>();
        private readonly RaycastHit[] questRaycastHits = new RaycastHit[QuestPhysicsRaycastBufferSize];
        private float nextGraphicRaycasterCacheRefreshTime;
        private bool worldUiAnchorLocked;
        private Vector3 lockedWorldUiAnchorPosition;
        private Quaternion lockedWorldUiAnchorRotation;
        private bool worldUiAnchorPoseLogged;
        private float lastWorldCanvasDiagnosticsLog;
        private float lastQuestDecorativeUiCleanupLog;
        private Transform fallbackEnvironmentRoot;
        private LineRenderer controllerRayLine;
        private Transform controllerRayCursor;
        private Renderer controllerRayCursorRenderer;
        private Material controllerRayMaterial;
        private Material controllerRayCursorMaterial;
        private Material fallbackGridMaterial;
        private Material duelFloorMaterial;
        private Material duelArenaLineMaterial;
        private Material worldCanvasPanelMaterial;
        private Material uiRenderPanelMaterial;
        private Material uiRenderPanelBackdropMaterial;
        private Transform duelArenaRoot;
        private Transform questDuelFieldRoot;
        private GameObject questNearField;
        private GameObject questFarField;
        private GameObject questDuelFieldSurface;
        private readonly List<Mesh> questDuelFieldSurfaceMeshes = new List<Mesh>();
        private readonly List<Material> questDuelFieldSurfaceMaterials = new List<Material>();
        private bool questDuelFieldLoadInProgress;
        private bool questDuelFieldLoadSucceeded;
        private bool questDuelFieldLoggedReady;
        private float questDuelFieldNextRetryTime;
        private int questDuelFieldLoadAttempt;
        private RenderTexture uiRenderTexture;
        private GameObject uiRenderPanel;
        private GameObject uiRenderPanelBackdrop;
        private MeshRenderer uiRenderPanelRenderer;
        private MeshRenderer uiRenderPanelBackdropRenderer;
        private int uiRenderTextureWidth;
        private int uiRenderTextureHeight;
        private bool uiRenderPanelLogged;
        private bool uiRenderPanelDiagnosticsLogged;
        private float lastUiRenderPanelDiagnosticsLog;
        private float lastUiRenderPanelHitDiagnosticsLog;
        private bool uiRenderCameraLogged;
        private bool wasQuestNativeDuelActive;
        private float questMainMenuUiRecoveryUntil;
        private float lastQuestMainMenuUiRecoveryTime;
        private bool questGraphicsSafeModeLogged;
        private CompositionLayer passthroughCompositionLayer;
        private ARSession arSession;
        private ARInputManager arInputManager;
        private ARCameraManager arCameraManager;
        private ARCameraBackground arCameraBackground;
        private AROcclusionManager arOcclusionManager;
        private bool arPassthroughLogged;
        private bool defaultSceneLayerLogged;
        private int lastAppliedXrCullingMask;
        private PointerEventData questPointerEventData;
        private EventSystem questPointerEventSystem;
        private GameObject questHoveredUi;
        private GameObject questPressedUi;
        private bool lastQuestPointerPressed;
        private bool questPointerEligibleForClick;
        private bool suppressPointerUntilReleased;
        private bool hasLastQuestDirectPointerPosition;
        private Vector2 lastQuestDirectPointerPosition;
        private bool directUiInputLogged;
        private bool controllerPoseFallbackLogged;
        private bool missingControllerPoseLogged;
        private float lastControllerPoseDiagnosticLog;
        private bool handInteractionPoseLogged;
        private bool questInputCallbacksRegistered;
        private float questInputRecoveryUntil;
        private float lastQuestInputRecoveryTime;
        private float lastQuestInputDeviceLog;
        private int questControllerPoseMask = -1;
        private string lastQuestInputDeviceSummary;
        private bool questInputXrRestartInProgress;
        private int questInputXrRestartAttempts;
        private float questInputXrRestartNotBefore;
        private GameObject lastLoggedQuestUi;
        private float lastQuestPointerStatusLog;
        private float lastDuelWorldBoundsLog;
        private float lastDuelRendererDiagnosticLog;
        private Transform optionalSceneryFilteredContainer;
        private int hiddenOptionalDuelSceneryRenderers;
        private int hiddenOptionalDuelSceneryColliders;
        private bool optionalDuelSceneryLogged;
        private Transform lockedDuelAlignmentContainer;
        private Vector3 lockedDuelAnchorPosition;
        private Quaternion lockedDuelAnchorRotation;
        private Vector3 lockedDuelAnchorScale;
        private Vector3 lockedDuelContainerLocalPosition;
        private Quaternion lockedDuelContainerLocalRotation;
        private Vector3 lockedDuelContainerLocalScale;
        private Transform originalFieldHiddenContainer;
        private int hiddenOriginalDuelFieldRenderers;
        private bool originalDuelFieldLogged;
        private Transform legacyResidueContainer;
        private int hiddenLegacyResidueRenderers;
        private bool legacyResidueLogged;
        private Transform earlySuppressedDuelContainer;
        private bool earlyDuelSuppressionLogged;
        private GameObject lastLoggedQuestPressedUi;
        private GameObject lastLoggedQuestClickedUi;
        private GameCard questPressedCard;
        private GameCard questInfoHoverCard;
        private QuestPileProxyHit questPressedPile;
        private bool questDirectCardClickEligible;
        private bool questPileClickEligible;
        private bool lastQuestCardPointerPressed;
        private bool lastQuestPilePointerPressed;
        private bool questInfoHoverShown;
        private float questInfoHoverStartTime;
        private GameCard lastLoggedQuestCardHit;
        private float lastQuestCardHitLog;
        private GameCard lastQuestNoActionNoticeCard;
        private float lastQuestNoActionNoticeTime;
        private GameObject questPressedDuelActionUi;
        private QuestDuelAction questPressedDuelAction;
        private Canvas questDuelActionMenuCanvas;
        private RectTransform questDuelActionMenuRect;
        private Image questDuelActionMenuBackground;
        private RectTransform questDuelActionMenuStemRect;
        private Image questDuelActionMenuStemImage;
        private QuestDuelWorldPresenter questDuelWorldPresenter;
        private QuestDuelNativeUi questDuelNativeUi;
        private QuestNativeMainMenu questNativeMainMenu;
        private readonly List<QuestDuelAction> questDuelActions = new List<QuestDuelAction>();
        private readonly List<GameObject> questDuelActionRows = new List<GameObject>();
        private readonly Dictionary<GameObject, QuestDuelAction> questDuelActionByRow = new Dictionary<GameObject, QuestDuelAction>();
        private float lastQuestDuelActionMenuLog;
        private bool questDuelActionMenuRadial;
        private float lastDirectUiPointerExceptionLog;
        private float lastQuestWorldControlLog;
        private Vector3 questPlayerWorldOffset;
        private float questDuelWorldScaleMultiplier = 1f;
        private bool questDuelWorldUserTransformInitialized;
        private Vector3 questDuelWorldUserAnchorPosition;
        private float questPlayerYawOffsetDegrees;
        private string questDebugAutoActionSignature = string.Empty;
        private float questDebugAutoActionSignatureStartTime = -999f;
        private float questDebugAutoActionLastAttemptTime = -999f;
        private int questDebugAutoActionsExecuted;
        private readonly HashSet<string> questDebugAutoActionExecutedSignatures = new HashSet<string>();
        private Transform questCutinWorldRoot;
        private Transform questMatePreviewWorldRoot;
        private float lastQuestPreviewLog;
        private float lastQuestPreviewControlLog;
        private Vector3 questCutinPreviewOffset;
        private Vector3 questMatePreviewOffset;
        private Vector2 questCutinPreviewRotation;
        private Vector2 questMatePreviewRotation;
        private float questCutinPreviewScaleMultiplier = 1f;
        private float questMatePreviewScaleMultiplier = 1f;
        private bool questPreviewRayDragging;
        private bool questPreviewRayDragMate;
        private float questPreviewRayDragDistance;
        private Vector2 questPreviewRayLastViewport;
        private Transform questPreviewRayDragTarget;
        private Transform questSelectedMatePreview;
        private bool questPreviewDeletePressedLastFrame;
        private bool questMainUiRecenterPressedLastFrame;
        private bool questMainUiRecenterActive;
        private bool questDebugAutoFrameApplied;
        private float lastQuestDebugForcedViewApplyTime = -999f;
        private float lastQuestNativeDuelActiveTime = -999f;
        private Vector2 smoothedQuestRightWorldAxis;
        private Vector2 smoothedQuestLeftWorldAxis;
        private bool questRuntimePerformanceTuningLogged;
        private bool questCardInfoPinButtonLastFrame;
        private bool questCardInfoTriggerLastFrame;

        private InputAction pointerPositionAction;
        private InputAction pointerRotationAction;
        private InputAction pointerPressAction;
        private InputAction handPointerPositionAction;
        private InputAction handPointerRotationAction;
        private InputAction handPointerPressAction;
        private InputAction leftThumbstickAction;
        private InputAction leftPrimaryButtonAction;
        private InputAction rightThumbstickAction;
        private InputAction rightThumbstickClickAction;
        private InputAction rightPrimaryButtonAction;
        private InputAction rightSecondaryButtonAction;
        private InputActionAsset uiActionAsset;
        private InputAction uiPointAction;
        private InputAction uiClickAction;
        private InputActionReference uiPointReference;
        private InputActionReference uiClickReference;
        private static QuestXrBootstrap activeInstance;
        private static bool questTweenCapacityConfigured;
        private static float questNativeDuelFrontendSuppressedUntil;

        private sealed class QuestDuelAction
        {
            public GameCard Card;
            public List<int> Response;
            public string Hint;
            public MDPro3.UI.ButtonType Type;
            public uint Location;
            public uint Controller;
            public uint Sequence;
            public int DisplayIndex;
            public MDPro3.UI.DuelButton LegacyButton;

            public int FirstResponse
            {
                get { return Response == null || Response.Count == 0 ? int.MinValue : Response[0]; }
            }

            public static QuestDuelAction FromLegacyButton(MDPro3.UI.DuelButton button)
            {
                if (button == null)
                    return null;

                return new QuestDuelAction
                {
                    Card = button.cookieCard,
                    Response = button.response,
                    Hint = button.hint,
                    Type = button.type,
                    Location = button.location,
                    Controller = button.controller,
                    Sequence = button.sequence,
                    LegacyButton = button
                };
            }

            public static QuestDuelAction FromCardButton(GameCard card, GameCard.DuelButtonInfo button)
            {
                if (card == null || button.response == null || button.response.Count == 0)
                    return null;

                return new QuestDuelAction
                {
                    Card = card,
                    Response = button.response,
                    Hint = button.hint,
                    Type = button.type,
                    Location = card.p == null ? 0 : card.p.location,
                    Controller = card.p == null ? 0 : card.p.controller,
                    Sequence = card.p == null ? 0 : card.p.sequence
                };
            }
        }

        private struct LegacyCanvasState
        {
            public bool canvasEnabled;
            public bool hadRaycaster;
            public bool raycasterEnabled;
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit x, RaycastHit y)
            {
                return x.distance.CompareTo(y.distance);
            }
        }

        public static void ResetQuestInputAfterTransition()
        {
            activeInstance?.ResetPointerState();
            UserInput.ClearPointerOverride();
        }

        public static void NotifyQuestDuelReturnedToMainMenu()
        {
            questNativeDuelFrontendSuppressedUntil = Time.unscaledTime + QuestMainMenuUiRecoverySuppressDuration;
            activeInstance?.HandleQuestDuelReturnedToMainMenu();
        }

        private void HandleQuestDuelReturnedToMainMenu()
        {
            questDuelNativeUi?.HideAllQuestUi();
            questDuelWorldPresenter?.SetVisible(false);
            ClearQuestDuelActionMenu();
            ResetPointerState();
            UserInput.ClearPointerOverride();
            SetControllerRayVisible(false, false);
            StartQuestMainMenuUiRecoveryWindow();

            anchoredDuelContainer = null;
            lockedDuelAlignmentContainer = null;
            originalFieldHiddenContainer = null;
            legacyResidueContainer = null;
            earlySuppressedDuelContainer = null;
            earlyDuelSuppressionLogged = false;
            duelWorldRootLogged = false;
            mdproCameraRenderModeInitialized = false;
            wasQuestNativeDuelActive = false;
            worldUiLogged = false;
            uiRenderCameraLogged = false;
            uiRenderPanelLogged = false;
            uiRenderPanelDiagnosticsLogged = false;
            lastUiRenderPanelDiagnosticsLog = -999f;
            lastUiRenderPanelHitDiagnosticsLog = -999f;
            lastAppliedXrCullingMask = int.MinValue;

            ApplyResolvedXrCullingMask();
            ForceQuestMainMenuUiRecovery("duel-return");
            Debug.Log("Quest native duel frontend released for main menu.");
        }

        private void ForceQuestMainMenuUiRecovery(string reason)
        {
            questDuelNativeUi?.HideAllQuestUi();
            var restoredCanvasCount = RestoreLegacyDuelCanvases();
            mdproCameraRenderModeInitialized = false;
            lastMdproCameraConfigureTime = -999f;

            EnsureUiRenderTexture();
            EnsureUiRenderPanel();
            RepairUiRenderPanelRenderer();
            ConfigureMdproCameras(true);
            ConfigureOverlayCanvases();
            ConfigureUiRenderPanel();
            LogQuestMainMenuUiRecovery(reason, restoredCanvasCount);
        }

        private void StartQuestMainMenuUiRecoveryWindow()
        {
            questMainMenuUiRecoveryUntil = Time.unscaledTime + QuestMainMenuUiRecoveryDuration;
            lastQuestMainMenuUiRecoveryTime = -999f;
        }

        private void MaintainQuestMainMenuUiRecovery(bool questNativeDuelActive)
        {
            if (questNativeDuelActive || Time.unscaledTime > questMainMenuUiRecoveryUntil)
                return;
            if (Time.unscaledTime - lastQuestMainMenuUiRecoveryTime < QuestMainMenuUiRecoveryInterval)
                return;

            lastQuestMainMenuUiRecoveryTime = Time.unscaledTime;
            ForceQuestMainMenuUiRecovery("duel-return-maintain");
        }

        public static bool IsQuestFastNativeDuelActive()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return activeInstance != null
                && QuestNativeDuelFrontendOnly
                && IsQuestNativeDuelActive();
#else
            return false;
#endif
        }

        public static int AdjustQuestDuelSleep(int framesIn100)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!IsQuestFastNativeDuelActive() || framesIn100 <= 0)
                return framesIn100;

            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null || !IsQuestChainMessage(core.currentMessage))
                return framesIn100;

            var adjusted = Mathf.Min(framesIn100, QuestChainSleepCompressionMax);
            if (adjusted < framesIn100
                && activeInstance != null
                && Time.unscaledTime - activeInstance.lastQuestDuelSleepCompressionLog >= QuestDuelSleepCompressionLogInterval)
            {
                activeInstance.lastQuestDuelSleepCompressionLog = Time.unscaledTime;
                Debug.LogFormat("Quest duel sleep compressed: original={0}, adjusted={1}", framesIn100, adjusted);
            }

            return adjusted;
#else
            return framesIn100;
#endif
        }

        private static bool IsQuestChainMessage(GameMessage message)
        {
            return message == GameMessage.Chaining
                || message == GameMessage.Chained
                || message == GameMessage.ChainSolving
                || message == GameMessage.ChainSolved
                || message == GameMessage.ChainEnd
                || message == GameMessage.ChainNegated
                || message == GameMessage.ChainDisabled;
        }

        private static bool CanShowQuestNativeDuelUi()
        {
            return activeInstance != null && IsQuestNativeDuelActive();
        }

        public static bool ShowQuestDuelButton(MDPro3.UI.DuelButton button)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi() || button == null)
                return false;

            activeInstance.ShowQuestDuelActionButton(button);
            return true;
#else
            return false;
#endif
        }

        public static bool HideQuestDuelButton(MDPro3.UI.DuelButton button)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (activeInstance == null)
                return false;

            activeInstance.HideQuestDuelActionButton(button);
            return true;
#else
            return false;
#endif
        }

        public static bool ShowQuestPhaseMenu(List<string> selections)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            activeInstance.EnsureQuestDuelNativeUi();
            return activeInstance.questDuelNativeUi != null
                && activeInstance.questDuelNativeUi.ShowPhaseMenu(selections);
#else
            return false;
#endif
        }

        public static bool ShowQuestSelectCardPanel(string hint, List<GameCard> cards, int min, int max, bool exitable, bool sendable)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            if (ShouldUseQuestFieldDirectCardSelection(cards))
            {
                activeInstance.EnsureQuestDuelNativeUi();
                activeInstance.questDuelNativeUi?.HideAllPopups();
                Debug.LogFormat(
                    "Quest field card selection uses direct table targets: message={0}, count={1}, min={2}, max={3}, exitable={4}, sendable={5}",
                    Program.instance == null || Program.instance.ocgcore == null ? GameMessage.Waiting : Program.instance.ocgcore.currentMessage,
                    cards == null ? 0 : cards.Count,
                    min,
                    max,
                    exitable,
                    sendable);
                return false;
            }

            activeInstance.EnsureQuestDuelNativeUi();
            return activeInstance.questDuelNativeUi != null
                && activeInstance.questDuelNativeUi.ShowCardSelection(hint, cards, min, max, exitable, sendable);
#else
            return false;
#endif
        }

        private static bool ShouldUseQuestFieldDirectCardSelection(List<GameCard> cards)
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null || cards == null || cards.Count == 0)
                return false;

            switch (core.currentMessage)
            {
                case GameMessage.SelectCard:
                case GameMessage.SelectUnselect:
                case GameMessage.SelectTribute:
                case GameMessage.SelectSum:
                    break;
                default:
                    return false;
            }

            foreach (var card in cards)
                if (!IsQuestDirectFieldSelectableCard(card))
                    return false;

            return true;
        }

        private static bool IsQuestDirectFieldSelectableCard(GameCard card)
        {
            if (card == null || card.p == null)
                return false;

            var location = card.p.location;
            if ((location & (uint)CardLocation.Overlay) > 0)
                return false;

            return (location & (uint)CardLocation.Onfield) > 0;
        }

        public static bool ShowQuestSelectionPanel(List<string> selections, List<int> responses)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            activeInstance.EnsureQuestDuelNativeUi();
            return activeInstance.questDuelNativeUi != null
                && activeInstance.questDuelNativeUi.ShowSelection(selections, responses);
#else
            return false;
#endif
        }

        public static bool ShowQuestInputPanel(List<string> selections, Action<string> confirmAction, Action cancelAction)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null || core.currentMessage != GameMessage.AnnounceCard || confirmAction == null)
                return false;

            Debug.Log("Quest native input bridge: AnnounceCard uses direct candidate list.");
            confirmAction(string.Empty);
            return true;
#else
            return false;
#endif
        }

        public static bool ShowQuestYesOrNoPanel(List<string> selections, Action confirmAction, Action cancelAction)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            activeInstance.EnsureQuestDuelNativeUi();
            return activeInstance.questDuelNativeUi != null
                && activeInstance.questDuelNativeUi.ShowYesOrNo(selections, confirmAction, cancelAction);
#else
            return false;
#endif
        }

        public static bool PrepareQuestMonsterCutin(GameObject root)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (activeInstance == null || root == null)
                return false;

            activeInstance.AttachMonsterCutinToWorld(root);
            return true;
#else
            return false;
#endif
        }

        public static bool PrepareQuestMatePreview(Mate mate)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (activeInstance == null || mate == null)
                return false;

            activeInstance.AttachMatePreviewToWorld(mate);
            return true;
#else
            return false;
#endif
        }

        public static void DetachQuestMatePreview(Mate mate)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (activeInstance == null || mate == null)
                return;

            activeInstance.DetachMatePreviewFromWorld(mate);
#endif
        }

        public static bool ShowQuestPositionPanel(int code, int count, int option1, int option2)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            activeInstance.EnsureQuestDuelNativeUi();
            return activeInstance.questDuelNativeUi != null
                && activeInstance.questDuelNativeUi.ShowPositionSelection(code, count, option1, option2);
#else
            return false;
#endif
        }

        public static bool ShowQuestLocationBrowser(uint controller, CardLocation location)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!CanShowQuestNativeDuelUi())
                return false;

            activeInstance.EnsureQuestDuelNativeUi();
            return activeInstance.questDuelNativeUi != null
                && activeInstance.questDuelNativeUi.ShowLocationBrowser(controller, location);
#else
            return false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (FindObjectOfType<QuestXrBootstrap>() != null)
                return;

            var go = new GameObject("QuestXrBootstrap");
            go.AddComponent<QuestXrBootstrap>();
            DontDestroyOnLoad(go);
#endif
        }

        private void Awake()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            activeInstance = this;
            QuestDuelNativeUi.UiVisibilityChanged += HandleQuestNativeUiVisibilityChanged;
            createdAt = Time.unscaledTime;
            QuestRuntimeDebugSettings.Initialize();
            ConfigureQuestTweenCapacity();
            ConfigureFloorTrackingOrigin();
            CreateXrCamera();
            if (UsePassthroughMixedReality)
                ConfigurePassthroughScene();
            else
                ConfigureVirtualWorldScene();
            CreateFallbackEnvironment();
            EnsureControllerRayVisual();
            CreatePoseActions();
            CreateVirtualMouseActions();
            EnsureVirtualMouse();
            RegisterQuestInputCallbacks();
            RequestQuestInputRefresh("startup");
#endif
        }

        private static void ConfigureQuestTweenCapacity()
        {
            if (questTweenCapacityConfigured)
                return;

            questTweenCapacityConfigured = true;
            DOTween.SetTweensCapacity(1200, 200);
            Debug.Log("Quest XR DOTween capacity configured: tweeners=1200, sequences=200");
        }

        private void Update()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            UpdateQuestInputRecovery();
            CalibrateTrackingOriginIfReady();
            RecalibrateTrackingOriginWhenUserPresenceAppears();
            ConfigureFloorTrackingOrigin();
            PreSuppressQuestDuelPresentation();
            var questNativeDuelActive = IsQuestNativeDuelActive();
            if (wasQuestNativeDuelActive && !questNativeDuelActive)
            {
                questNativeDuelFrontendSuppressedUntil = Mathf.Max(
                    questNativeDuelFrontendSuppressedUntil,
                    Time.unscaledTime + QuestMainMenuUiRecoverySuppressDuration);
                StartQuestMainMenuUiRecoveryWindow();
                ForceQuestMainMenuUiRecovery("duel-state-exit");
            }
            wasQuestNativeDuelActive = questNativeDuelActive;
            ConfigureMdproCameras();
            MaintainQuestWorldVisibility();
            SuppressQuestDecorativeUiBackgrounds();
            ConfigureDuelWorldRoot();
            UpdateQuestWorldControls();
            ConfigureOverlayCanvases();
            ConfigureUiRenderPanel();
            ConfigureUiInputModule();
            EnsureQuestNativeMainMenu();
            questNativeMainMenu?.Tick();
            if (questNativeDuelActive)
            {
                lastQuestNativeDuelActiveTime = Time.unscaledTime;
                if (questMainUiRecenterActive)
                {
                    questMainUiRecenterActive = false;
                    ReapplyXrOriginForCurrentHeadPose();
                    Debug.Log("Quest main UI recenter cleared on duel entry.");
                }
                EnsureQuestDuelNativeUi();
                questDuelNativeUi?.Tick();
                ApplyQuestDebugAutoFrameIfNeeded();
                ApplyQuestDebugAutoDuelActionsIfNeeded();
            }
            else
            {
                questDuelNativeUi?.HideAllQuestUi();
                ClearQuestDuelActionMenu();
                ResetQuestDebugAutoDuelActions();
                questDebugAutoFrameApplied = false;
                lastQuestDebugForcedViewApplyTime = -999f;
                if (Time.unscaledTime - lastQuestNativeDuelActiveTime >= QuestDuelInactiveResetDelay)
                    ResetQuestWorldControlsWhenNoDuel();
            }
            UpdateQuestMainUiRecenterInput(questNativeDuelActive);
            MaintainQuestMainMenuUiRecovery(questNativeDuelActive);
            UpdateQuestDuelActionMenuPose();
            UpdateQuestPointer();
#endif
        }

        private void LateUpdate()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            PreSuppressQuestDuelPresentation();
            SuppressQuestBlackOverlays();
            SuppressQuestLegacyDuelVisualsLate();
#endif
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (activeInstance == this)
                activeInstance = null;
            QuestDuelNativeUi.UiVisibilityChanged -= HandleQuestNativeUiVisibilityChanged;
            UnregisterQuestInputCallbacks();
            pointerPositionAction?.Dispose();
            pointerRotationAction?.Dispose();
            pointerPressAction?.Dispose();
            handPointerPositionAction?.Dispose();
            handPointerRotationAction?.Dispose();
            handPointerPressAction?.Dispose();
            leftThumbstickAction?.Dispose();
            leftPrimaryButtonAction?.Dispose();
            rightThumbstickAction?.Dispose();
            rightThumbstickClickAction?.Dispose();
            rightPrimaryButtonAction?.Dispose();
            rightSecondaryButtonAction?.Dispose();
            if (uiActionAsset != null)
                Destroy(uiActionAsset);
            if (controllerRayMaterial != null)
                Destroy(controllerRayMaterial);
            if (controllerRayCursorMaterial != null)
                Destroy(controllerRayCursorMaterial);
            if (fallbackGridMaterial != null)
                Destroy(fallbackGridMaterial);
            if (duelFloorMaterial != null)
                Destroy(duelFloorMaterial);
            if (duelArenaLineMaterial != null)
                Destroy(duelArenaLineMaterial);
            if (worldCanvasPanelMaterial != null)
                Destroy(worldCanvasPanelMaterial);
            if (uiRenderPanelMaterial != null)
                Destroy(uiRenderPanelMaterial);
            if (uiRenderPanelBackdropMaterial != null)
                Destroy(uiRenderPanelBackdropMaterial);
            ClearQuestDuelFieldSurface();
            if (uiRenderTexture != null)
            {
                uiRenderTexture.Release();
                Destroy(uiRenderTexture);
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (hasFocus)
                RequestQuestInputRefresh("application-focus");
#endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!pauseStatus)
                RequestQuestInputRefresh("application-resume");
#endif
        }

        private void RegisterQuestInputCallbacks()
        {
            if (questInputCallbacksRegistered)
                return;

            InputDevices.deviceConnected += OnQuestXrDeviceChanged;
            InputDevices.deviceDisconnected += OnQuestXrDeviceChanged;
            InputDevices.deviceConfigChanged += OnQuestXrDeviceChanged;
            InputSystem.onDeviceChange += OnQuestInputSystemDeviceChanged;
            questInputCallbacksRegistered = true;
        }

        private void UnregisterQuestInputCallbacks()
        {
            if (!questInputCallbacksRegistered)
                return;

            InputDevices.deviceConnected -= OnQuestXrDeviceChanged;
            InputDevices.deviceDisconnected -= OnQuestXrDeviceChanged;
            InputDevices.deviceConfigChanged -= OnQuestXrDeviceChanged;
            InputSystem.onDeviceChange -= OnQuestInputSystemDeviceChanged;
            questInputCallbacksRegistered = false;
        }

        private void OnQuestXrDeviceChanged(UnityEngine.XR.InputDevice device)
        {
            RequestQuestInputRefresh("xr-device:" + (device.isValid ? device.name : "<invalid>"));
        }

        private void OnQuestInputSystemDeviceChanged(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                case InputDeviceChange.Enabled:
                case InputDeviceChange.UsageChanged:
                case InputDeviceChange.ConfigurationChanged:
                case InputDeviceChange.SoftReset:
                case InputDeviceChange.HardReset:
                    RequestQuestInputRefresh("input-system:" + change + ":" + (device == null ? "<null>" : device.displayName));
                    break;
            }
        }

        private void RequestQuestInputRefresh(string reason)
        {
            var now = Time.unscaledTime;
            questInputRecoveryUntil = Mathf.Max(questInputRecoveryUntil, now + QuestInputRecoveryWarmupDuration);
            lastQuestInputRecoveryTime = -999f;
            LogQuestInputDeviceSummary("request-" + reason, true);
        }

        private void UpdateQuestInputRecovery()
        {
            var poseMask = GetQuestControllerPoseMask();
            if (poseMask != questControllerPoseMask)
            {
                questControllerPoseMask = poseMask;
                if (poseMask != 0)
                    missingControllerPoseLogged = false;
                LogQuestInputDeviceSummary("pose-state-" + DescribeQuestControllerPoseMask(poseMask), true);
            }

            var now = Time.unscaledTime;
            var missingPose = poseMask == 0;
            var inWarmup = now <= questInputRecoveryUntil;
            if (missingPose)
                TryStartQuestXrRestartForInputRecovery(now);
            if (!missingPose && !inWarmup)
                return;

            var interval = missingPose ? QuestInputMissingPosePollInterval : QuestInputRecoveryPollInterval;
            if (now - lastQuestInputRecoveryTime < interval)
            {
                LogQuestInputDeviceSummary(missingPose ? "missing-pose" : "warmup", false);
                return;
            }

            RefreshQuestInputBindings(missingPose ? "missing-pose" : "warmup");
        }

        private void TryStartQuestXrRestartForInputRecovery(float now)
        {
            if (questInputXrRestartInProgress
                || questInputXrRestartAttempts >= QuestInputXrRestartMaxAttempts
                || now < questInputXrRestartNotBefore
                || now - createdAt < QuestInputXrRestartDelay)
                return;

            if (IsQuestNativeDuelActive())
                return;

            StartCoroutine(RestartQuestXrForInputRecovery());
        }

        private IEnumerator RestartQuestXrForInputRecovery()
        {
            questInputXrRestartInProgress = true;
            questInputXrRestartAttempts += 1;
            questInputXrRestartNotBefore = Time.unscaledTime + QuestInputXrRestartCooldown;

            Debug.LogFormat(
                "Quest XR input recovery restarting XR loader. Attempt={0}, XRDevices={1}, Input={2}",
                questInputXrRestartAttempts,
                DescribeCurrentXRDevices(),
                DescribeInputSystemQuestDevices());

            var manager = XRGeneralSettings.Instance == null ? null : XRGeneralSettings.Instance.Manager;
            if (manager == null)
            {
                Debug.LogWarning("Quest XR input recovery cannot restart XR loader: XR manager is null.");
                questInputXrRestartInProgress = false;
                yield break;
            }

            try
            {
                manager.StopSubsystems();
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Quest XR input recovery StopSubsystems failed: {0}", ex.Message);
            }

            yield return null;
            yield return new WaitForSecondsRealtime(0.35f);

            try
            {
                manager.DeinitializeLoader();
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Quest XR input recovery DeinitializeLoader failed: {0}", ex.Message);
            }

            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            try
            {
                manager.InitializeLoaderSync();
                manager.StartSubsystems();
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Quest XR input recovery Initialize/Start failed: {0}", ex.Message);
            }

            trackingOriginModeRequested = false;
            trackingOriginCalibrated = false;
            trackingOriginCalibratedWithUserPresence = false;
            gameCamerasConfigured = false;
            lastMdproCameraConfigureTime = 0f;
            questControllerPoseMask = -1;
            RequestQuestInputRefresh("xr-loader-restart");
            ConfigureFloorTrackingOrigin();
            ConfigureMdproCameras(true);

            Debug.LogFormat(
                "Quest XR input recovery XR loader restart finished. ActiveLoader={0}, XRDevices={1}, Input={2}",
                manager.activeLoader == null ? "<null>" : manager.activeLoader.name,
                DescribeCurrentXRDevices(),
                DescribeInputSystemQuestDevices());

            questInputXrRestartInProgress = false;
        }

        private void RefreshQuestInputBindings(string reason)
        {
            lastQuestInputRecoveryTime = Time.unscaledTime;

            foreach (var device in InputSystem.devices)
            {
                if (device == null || !IsLikelyQuestInputSystemDevice(device))
                    continue;

                try
                {
                    if (!device.enabled)
                        InputSystem.EnableDevice(device);
                }
                catch (Exception ex)
                {
                    Debug.LogWarningFormat("Quest XR input device enable failed: {0}: {1}", device.displayName, ex.Message);
                }
            }

            ReenableQuestInputAction(pointerPositionAction);
            ReenableQuestInputAction(pointerRotationAction);
            ReenableQuestInputAction(pointerPressAction);
            ReenableQuestInputAction(leftThumbstickAction);
            ReenableQuestInputAction(leftPrimaryButtonAction);
            ReenableQuestInputAction(rightThumbstickAction);
            ReenableQuestInputAction(rightThumbstickClickAction);
            ReenableQuestInputAction(rightPrimaryButtonAction);
            ReenableQuestInputAction(rightSecondaryButtonAction);

            LogQuestInputDeviceSummary("refresh-" + reason, true);
        }

        private static void ReenableQuestInputAction(InputAction action)
        {
            if (action == null)
                return;

            try
            {
                if (action.enabled)
                    action.Disable();
                action.Enable();
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("Quest XR input action refresh failed: {0}: {1}", action.name, ex.Message);
            }
        }

        private void LogQuestInputDeviceSummary(string reason, bool force)
        {
            var now = Time.unscaledTime;
            var poseMask = GetQuestControllerPoseMask();
            ControllerDevices.Clear();
            InputDevices.GetDevices(ControllerDevices);
            var summary = "Pose=" + DescribeQuestControllerPoseMask(poseMask)
                + "; XR=" + DescribeXRDevices(ControllerDevices)
                + "; Input=" + DescribeInputSystemQuestDevices();

            if (!force
                && summary == lastQuestInputDeviceSummary
                && now - lastQuestInputDeviceLog < QuestInputDeviceLogInterval)
                return;

            lastQuestInputDeviceLog = now;
            lastQuestInputDeviceSummary = summary;
            Debug.LogFormat("Quest XR input recovery [{0}]: {1}", reason, summary);
        }

        private static int GetQuestControllerPoseMask()
        {
            var mask = 0;
            if (TryReadControllerPose(XRNode.LeftHand, out _, out _, out _, out _))
                mask |= 1;
            if (TryReadControllerPose(XRNode.RightHand, out _, out _, out _, out _))
                mask |= 2;
            return mask;
        }

        private static string DescribeQuestControllerPoseMask(int mask)
        {
            if (mask == 3)
                return "Left+Right";
            if (mask == 1)
                return "Left";
            if (mask == 2)
                return "Right";
            return "None";
        }

        private static bool IsLikelyQuestInputSystemDevice(UnityEngine.InputSystem.InputDevice device)
        {
            if (device == null)
                return false;

            var descriptor = ((device.layout ?? string.Empty)
                + " " + (device.name ?? string.Empty)
                + " " + (device.displayName ?? string.Empty)).ToLowerInvariant();
            if (descriptor.Contains("xr")
                || descriptor.Contains("openxr")
                || descriptor.Contains("oculus")
                || descriptor.Contains("meta")
                || descriptor.Contains("touch")
                || descriptor.Contains("controller")
                || descriptor.Contains("hand"))
                return true;

            foreach (var usage in device.usages)
            {
                var usageName = usage.ToString().ToLowerInvariant();
                if (usageName.Contains("left")
                    || usageName.Contains("right")
                    || usageName.Contains("hand")
                    || usageName.Contains("controller"))
                    return true;
            }

            return false;
        }

        private void ResetPointerState()
        {
            if (questHoveredUi != null && questPointerEventData != null)
                ExecuteEvents.ExecuteHierarchy(questHoveredUi, questPointerEventData, ExecuteEvents.pointerExitHandler);

            questHoveredUi = null;
            questPressedUi = null;
            questPressedDuelActionUi = null;
            questPressedDuelAction = null;
            questPressedCard = null;
            questInfoHoverCard = null;
            questPressedPile = null;
            questDirectCardClickEligible = false;
            questPileClickEligible = false;
            questInfoHoverShown = false;
            lastQuestPointerPressed = false;
            lastQuestCardPointerPressed = false;
            lastQuestPilePointerPressed = false;
            questPointerEligibleForClick = false;
            suppressPointerUntilReleased = true;
            questCardInfoPinButtonLastFrame = false;
            questCardInfoTriggerLastFrame = false;
            hasLastQuestDirectPointerPosition = false;
            lastLoggedQuestPressedUi = null;
            lastLoggedQuestClickedUi = null;
            if (virtualMouse != null)
                QueueVirtualMouse(lastQueuedMousePos, false);
            if (questPointerEventData != null)
            {
                questPointerEventData.eligibleForClick = false;
                questPointerEventData.pointerPress = null;
                questPointerEventData.rawPointerPress = null;
            }
        }

        private void CreateXrCamera()
        {
            var rigObject = new GameObject("QuestXrWorldOrigin");
            rigObject.transform.SetParent(transform, false);
            rigObject.transform.localPosition = Vector3.zero;
            rigObject.transform.localRotation = Quaternion.identity;
            rigObject.transform.localScale = Vector3.one * DuelWorldUnitsPerMeter;
            xrOrigin = rigObject.AddComponent<XROrigin>();
            xrOrigin.Origin = rigObject;

            var cameraObject = new GameObject("QuestXrImmersiveCamera");
            cameraObject.transform.SetParent(rigObject.transform, false);
            xrCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            ConfigureXrCameraForVirtualWorld();
            xrCamera.nearClipPlane = 0.03f;
            xrCamera.farClipPlane = 1000f;
            xrCamera.depth = 100f;
            xrOrigin.Camera = xrCamera;

            var poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
            poseDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            poseDriver.UseRelativeTransform = false;

            var cameraData = xrCamera.GetUniversalAdditionalCameraData();
            cameraData.renderType = CameraRenderType.Base;
            cameraData.allowXRRendering = true;
            cameraData.allowHDROutput = false;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            PlaceXrOrigin(Vector3.zero, Quaternion.identity);
        }

        private void ConfigureXrCameraForVirtualWorld()
        {
            if (xrCamera == null)
                return;

            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
            xrCamera.allowHDR = false;
            xrCamera.allowMSAA = true;
            xrCamera.depthTextureMode = DepthTextureMode.None;

            var data = xrCamera.GetUniversalAdditionalCameraData();
            data.allowXRRendering = true;
            data.allowHDROutput = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.renderPostProcessing = false;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.antialiasing = AntialiasingMode.None;

            if (QualitySettings.antiAliasing != 4)
                QualitySettings.antiAliasing = 4;

            try
            {
                if (Mathf.Abs(XRSettings.eyeTextureResolutionScale - QuestEyeTextureResolutionScale) > 0.01f)
                    XRSettings.eyeTextureResolutionScale = QuestEyeTextureResolutionScale;
                if (Mathf.Abs(XRSettings.renderViewportScale - QuestRenderViewportScale) > 0.01f)
                    XRSettings.renderViewportScale = QuestRenderViewportScale;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Quest XR render resolution tuning failed: " + ex.Message);
            }

            var pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset
                ?? GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (pipelineAsset != null)
            {
                if (pipelineAsset.msaaSampleCount != 4)
                    pipelineAsset.msaaSampleCount = 4;
                if (Mathf.Abs(pipelineAsset.renderScale - QuestUrpRenderScale) > 0.01f)
                    pipelineAsset.renderScale = QuestUrpRenderScale;
            }

            ApplyQuestRuntimePerformanceTuning();

            if (!questGraphicsSafeModeLogged)
            {
                questGraphicsSafeModeLogged = true;
                Debug.LogFormat(
                    "Quest XR graphics clarity mode applied. CameraMSAA={0}, QualityAA={1}, CameraAA={2}, URPMSAA={3}, URPRenderScale={4:F2}, EyeScale={5:F2}, ViewportScale={6:F2}, UiRT={7}x{8}/AA1",
                    xrCamera.allowMSAA,
                    QualitySettings.antiAliasing,
                    data.antialiasing,
                    pipelineAsset == null ? -1 : pipelineAsset.msaaSampleCount,
                    pipelineAsset == null ? -1f : pipelineAsset.renderScale,
                    XRSettings.eyeTextureResolutionScale,
                    XRSettings.renderViewportScale,
                    UiRenderTextureFixedWidth,
                    UiRenderTextureFixedHeight);
            }
        }

        private void ApplyQuestRuntimePerformanceTuning()
        {
            try
            {
                XrDisplaySubsystems.Clear();
                SubsystemManager.GetSubsystems(XrDisplaySubsystems);
                foreach (var display in XrDisplaySubsystems)
                {
                    if (display == null || !display.running)
                        continue;

                    display.foveatedRenderingLevel = Mathf.Clamp01(QuestFoveatedRenderingLevel);
                    display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.GazeAllowed;
                    if (!questRuntimePerformanceTuningLogged)
                    {
                        questRuntimePerformanceTuningLogged = true;
                        Debug.LogFormat(
                            "Quest XR runtime performance tuning applied. Foveation={0:F2}, Flags={1}",
                            display.foveatedRenderingLevel,
                            display.foveatedRenderingFlags);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!questRuntimePerformanceTuningLogged)
                {
                    questRuntimePerformanceTuningLogged = true;
                    Debug.LogWarning("Quest XR runtime performance tuning failed: " + ex.Message);
                }
            }
        }

        private void ConfigurePassthroughScene()
        {
            if (xrCamera == null)
                return;

            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            EnsureArPassthroughComponents();
            ConfigureDefaultSceneLayerForPassthrough();
            EnsurePassthroughCompositionLayerFallback();
            passthroughConfigured = true;
            Debug.Log("Quest XR passthrough background requested.");
        }

        private void ConfigureVirtualWorldScene()
        {
            if (xrCamera == null)
                return;

            ConfigureXrCameraForVirtualWorld();
            DisablePassthroughComponents();
            passthroughConfigured = true;
            Debug.Log("Quest XR virtual 3D duel world enabled.");
        }

        private void DisablePassthroughComponents()
        {
            if (arCameraBackground == null && xrCamera != null)
                arCameraBackground = xrCamera.GetComponent<ARCameraBackground>();
            if (arCameraBackground != null)
                arCameraBackground.enabled = false;

            if (arCameraManager == null && xrCamera != null)
                arCameraManager = xrCamera.GetComponent<ARCameraManager>();
            if (arCameraManager != null)
                arCameraManager.enabled = false;

            if (arOcclusionManager == null && xrCamera != null)
                arOcclusionManager = xrCamera.GetComponent<AROcclusionManager>();
            if (arOcclusionManager != null)
            {
                arOcclusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Disabled;
                arOcclusionManager.requestedHumanDepthMode = UnityEngine.XR.ARSubsystems.HumanSegmentationDepthMode.Disabled;
                arOcclusionManager.requestedHumanStencilMode = UnityEngine.XR.ARSubsystems.HumanSegmentationStencilMode.Disabled;
                arOcclusionManager.enabled = false;
            }

            if (arInputManager != null)
                arInputManager.enabled = false;
            if (arSession != null)
                arSession.enabled = false;

            if (passthroughCompositionLayer != null)
                passthroughCompositionLayer.gameObject.SetActive(false);
            if (TryFindCompositionLayer<PassthroughLayerData>(out var existingLayer))
            {
                passthroughCompositionLayer = existingLayer;
                passthroughCompositionLayer.gameObject.SetActive(false);
            }
        }

        private void ConfigureFloorTrackingOrigin()
        {
            if (trackingOriginModeRequested)
                return;

            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetInstances(subsystems);
            foreach (var subsystem in subsystems)
            {
                if (subsystem == null || !subsystem.running)
                    continue;

                if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor) ||
                    subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device))
                {
                    trackingOriginModeRequested = true;
                    if (!trackingOriginModeLogged)
                    {
                        Debug.Log("Quest XR floor tracking origin requested.");
                        trackingOriginModeLogged = true;
                    }
                    return;
                }
            }
        }

        private void EnsureArPassthroughComponents()
        {
            if (xrCamera == null)
                return;

            if (arSession == null)
            {
                arSession = FindObjectOfType<ARSession>(true);
                if (arSession == null)
                {
                    var sessionObject = new GameObject("Quest AR Session");
                    sessionObject.transform.SetParent(transform, false);
                    arSession = sessionObject.AddComponent<ARSession>();
                    arInputManager = sessionObject.AddComponent<ARInputManager>();
                }
            }

            if (arInputManager == null && arSession != null)
                arInputManager = arSession.GetComponent<ARInputManager>() ?? arSession.gameObject.AddComponent<ARInputManager>();

            arSession.enabled = true;
            if (arInputManager != null)
                arInputManager.enabled = true;

            arCameraManager = xrCamera.GetComponent<ARCameraManager>();
            if (arCameraManager == null)
                arCameraManager = xrCamera.gameObject.AddComponent<ARCameraManager>();

            arCameraManager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
            arCameraManager.enabled = true;

            arCameraBackground = xrCamera.GetComponent<ARCameraBackground>();
            if (arCameraBackground == null)
                arCameraBackground = xrCamera.gameObject.AddComponent<ARCameraBackground>();

            arCameraBackground.enabled = true;

            arOcclusionManager = xrCamera.GetComponent<AROcclusionManager>();
            if (arOcclusionManager != null)
            {
                arOcclusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Disabled;
                arOcclusionManager.requestedHumanDepthMode = UnityEngine.XR.ARSubsystems.HumanSegmentationDepthMode.Disabled;
                arOcclusionManager.requestedHumanStencilMode = UnityEngine.XR.ARSubsystems.HumanSegmentationStencilMode.Disabled;
                arOcclusionManager.enabled = false;
            }

            if (!arPassthroughLogged)
            {
                Debug.Log("Quest XR ARCamera passthrough components enabled.");
                arPassthroughLogged = true;
            }
        }

        private void ConfigureDefaultSceneLayerForPassthrough()
        {
            var manager = CompositionLayerManager.Instance;
            var defaultLayer = manager == null ? null : manager.DefaultSceneCompositionLayer;
            if (defaultLayer == null && TryFindCompositionLayer<DefaultLayerData>(out var foundDefaultLayer))
                defaultLayer = foundDefaultLayer;

            if (defaultLayer?.LayerData == null)
                return;

            defaultLayer.LayerData.BlendType = BlendType.Premultiply;
            if (!defaultSceneLayerLogged)
            {
                Debug.Log("Quest XR default scene composition layer set to premultiplied alpha.");
                defaultSceneLayerLogged = true;
            }
        }

        private void EnsurePassthroughCompositionLayerFallback()
        {
            if (TryFindCompositionLayer<PassthroughLayerData>(out var existingLayer))
            {
                passthroughCompositionLayer = existingLayer;
                if (!existingLayer.gameObject.activeSelf)
                    existingLayer.gameObject.SetActive(true);
                return;
            }

            if (Time.unscaledTime - createdAt < 1.5f && XRSettings.isDeviceActive)
                return;

            EnsurePassthroughCompositionLayer();
        }

        private static bool TryFindCompositionLayer<TLayerData>(out CompositionLayer layer)
            where TLayerData : LayerData
        {
            layer = null;

            var manager = CompositionLayerManager.Instance;
            var layers = manager == null ? null : manager.CompositionLayers;
            if (layers == null)
                return false;

            foreach (var candidate in layers)
            {
                if (candidate != null && candidate.LayerData is TLayerData)
                {
                    layer = candidate;
                    return true;
                }
            }

            return false;
        }

        private void EnsurePassthroughCompositionLayer()
        {
            if (passthroughCompositionLayer != null)
            {
                if (!passthroughCompositionLayer.gameObject.activeSelf)
                    passthroughCompositionLayer.gameObject.SetActive(true);
                return;
            }

            var passthroughObject = new GameObject("Quest Passthrough Layer");
            passthroughObject.transform.SetParent(transform, false);
            passthroughCompositionLayer = passthroughObject.AddComponent<CompositionLayer>();
            passthroughCompositionLayer.ChangeLayerDataType(new PassthroughLayerData());
            passthroughCompositionLayer.TryChangeLayerOrder(
                passthroughCompositionLayer.Order,
                CompositionLayerManager.GetFirstUnusedLayer(false));
        }

        private void CreateFallbackEnvironment()
        {
            if (fallbackEnvironmentRoot != null)
                return;

            var worldObject = new GameObject("QuestVirtualDuelWorld");
            fallbackEnvironmentRoot = worldObject.transform;
            fallbackEnvironmentRoot.SetParent(transform, false);
            fallbackGridMaterial = CreateColorMaterial(
                "Quest World Reference Grid Material",
                new Color(0.10f, 0.28f, 0.34f, 0.18f),
                true);
            duelFloorMaterial = CreateColorMaterial(
                "Quest Duel Matte Floor Material",
                new Color(0.018f, 0.023f, 0.030f, 1f),
                false);
            duelArenaLineMaterial = CreateColorMaterial(
                "Quest Anime Duel Arena Line Material",
                new Color(0.16f, 0.72f, 0.68f, 0.38f),
                true);

            var lightObject = new GameObject("QuestVirtualWorldKeyLight");
            lightObject.transform.SetParent(fallbackEnvironmentRoot, false);
            lightObject.transform.SetLocalPositionAndRotation(new Vector3(-40f, 72f, -34.5f), Quaternion.Euler(50f, -35f, 0f));
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.82f, 0.92f, 1f, 1f);

            CreateDuelMatteFloor();

            var half = (FallbackGridLineCount - 1) * 0.5f * FallbackGridSpacing;
            var y = 0.001f;
            for (var index = 0; index < FallbackGridLineCount; index += 1)
            {
                var offset = -half + index * FallbackGridSpacing;
                CreateReferenceLine(
                    new Vector3(-half, y, offset),
                    new Vector3(half, y, offset));
                CreateReferenceLine(
                    new Vector3(offset, y, -half),
                    new Vector3(offset, y, half));
            }

            CreateAnimeDuelArena();
            AttachFallbackEnvironmentToDuelWorldAnchor();
        }

        private void CreateDuelMatteFloor()
        {
            var floorObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floorObject.name = "QuestDuelMatteFloor";
            SetQuestOverlayLayer(floorObject);
            floorObject.transform.SetParent(fallbackEnvironmentRoot, false);
            floorObject.transform.localPosition = new Vector3(0f, DuelGroundY - 0.018f, -1.5f);
            floorObject.transform.localRotation = Quaternion.identity;
            floorObject.transform.localScale = new Vector3(DuelArenaWidth * 1.08f, 0.02f, DuelArenaDepth * 1.10f);
            var collider = floorObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            var renderer = floorObject.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = duelFloorMaterial;
        }

        private void CreateReferenceLine(Vector3 start, Vector3 end)
        {
            var lineObject = new GameObject("QuestWorldReferenceLine");
            SetQuestOverlayLayer(lineObject);
            lineObject.transform.SetParent(fallbackEnvironmentRoot, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = false;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.018f;
            line.endWidth = 0.018f;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.material = fallbackGridMaterial;
            line.startColor = new Color(0.16f, 0.58f, 0.70f, 0.045f);
            line.endColor = new Color(0.16f, 0.58f, 0.70f, 0.045f);
        }

        private void CreateAnimeDuelArena()
        {
            if (duelArenaRoot != null)
                return;

            var arenaObject = new GameObject("QuestAnimeDuelArena");
            SetQuestOverlayLayer(arenaObject);
            duelArenaRoot = arenaObject.transform;
            duelArenaRoot.SetParent(fallbackEnvironmentRoot, false);
            duelArenaRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            var halfWidth = DuelArenaWidth * 0.5f;
            var halfDepth = DuelArenaDepth * 0.5f;
            var y = DuelArenaLineY;
            CreateArenaLine(new Vector3(-halfWidth, y, -halfDepth), new Vector3(halfWidth, y, -halfDepth));
            CreateArenaLine(new Vector3(halfWidth, y, -halfDepth), new Vector3(halfWidth, y, halfDepth));
            CreateArenaLine(new Vector3(halfWidth, y, halfDepth), new Vector3(-halfWidth, y, halfDepth));
            CreateArenaLine(new Vector3(-halfWidth, y, halfDepth), new Vector3(-halfWidth, y, -halfDepth));
            CreateArenaLine(new Vector3(-halfWidth, y, 0f), new Vector3(halfWidth, y, 0f));

            CreateMainZoneRows(y + 0.012f);
            CreatePileZones(y + 0.018f);
        }

        private void CreateMainZoneRows(float y)
        {
            CreateMainZoneRow(y, ScaleDuelBoardZ(-9.48f));
            CreateMainZoneRow(y, ScaleDuelBoardZ(-18f));
            CreateMainZoneRow(y, ScaleDuelBoardZ(9.51f));
            CreateMainZoneRow(y, ScaleDuelBoardZ(18f));
        }

        private void CreateMainZoneRow(float y, float z)
        {
            foreach (var x in DuelMainZoneX)
                CreateArenaRectangle(new Vector3(x, y, z), DuelZoneWidth, DuelZoneDepth);
        }

        private void CreatePileZones(float y)
        {
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(26.86f, y, -23.93f)), "Deck");
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(-26.86f, y, -23.93f)), "Extra");
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(25.74f, y, -14.26f)), "Grave");
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(27.58f, y, -8.02f)), "Banish");

            CreatePileZone(ScaleDuelBoardPoint(new Vector3(-26.86f, y, 23.93f)), "Deck");
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(26.86f, y, 23.93f)), "Extra");
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(-25.74f, y, 14.26f)), "Grave");
            CreatePileZone(ScaleDuelBoardPoint(new Vector3(-27.58f, y, 8.02f)), "Banish");
        }

        private void CreatePileZone(Vector3 center, string label)
        {
            CreateArenaRectangle(center, DuelZoneWidth, DuelZoneDepth * 0.82f);
        }

        private void CreateArenaRectangle(Vector3 center, float width, float depth)
        {
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            CreateArenaLine(new Vector3(center.x - halfWidth, center.y, center.z - halfDepth), new Vector3(center.x + halfWidth, center.y, center.z - halfDepth));
            CreateArenaLine(new Vector3(center.x + halfWidth, center.y, center.z - halfDepth), new Vector3(center.x + halfWidth, center.y, center.z + halfDepth));
            CreateArenaLine(new Vector3(center.x + halfWidth, center.y, center.z + halfDepth), new Vector3(center.x - halfWidth, center.y, center.z + halfDepth));
            CreateArenaLine(new Vector3(center.x - halfWidth, center.y, center.z + halfDepth), new Vector3(center.x - halfWidth, center.y, center.z - halfDepth));
        }

        private static float ScaleDuelBoardZ(float z)
        {
            return z * DuelBoardScaleZ;
        }

        private static Vector3 ScaleDuelBoardPoint(Vector3 point)
        {
            point.x *= DuelBoardScaleX;
            point.z *= DuelBoardScaleZ;
            return point;
        }

        private void CreateArenaLine(Vector3 start, Vector3 end)
        {
            var lineObject = new GameObject("QuestAnimeDuelArenaLine");
            SetQuestOverlayLayer(lineObject);
            lineObject.transform.SetParent(duelArenaRoot == null ? fallbackEnvironmentRoot : duelArenaRoot, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = false;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = DuelArenaLineWidth;
            line.endWidth = DuelArenaLineWidth;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.material = duelArenaLineMaterial ?? fallbackGridMaterial;
            line.startColor = new Color(0.18f, 0.86f, 0.78f, 0.30f);
            line.endColor = new Color(0.18f, 0.86f, 0.78f, 0.30f);
        }

        private static Material CreateColorMaterial(string name, Color color, bool transparent)
        {
            var shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Sprites/Default") ??
                Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            if (transparent)
            {
                material.renderQueue = (int)RenderQueue.Transparent;
                if (material.HasProperty("_Surface"))
                    material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend"))
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend"))
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite"))
                    material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHABLEND_ON");
            }

            return material;
        }

        private void CreatePoseActions()
        {
            pointerPositionAction = new InputAction("Quest Pointer Position", InputActionType.PassThrough);
            pointerPositionAction.AddBinding("<XRController>{RightHand}/devicePosition");
            pointerPositionAction.AddBinding("<XRController>{LeftHand}/devicePosition");
            AddQuestControllerBinding(pointerPositionAction, "pointerPosition");
            AddQuestControllerBinding(pointerPositionAction, "devicePosition");
            pointerRotationAction = new InputAction("Quest Pointer Rotation", InputActionType.PassThrough);
            pointerRotationAction.AddBinding("<XRController>{RightHand}/deviceRotation");
            pointerRotationAction.AddBinding("<XRController>{LeftHand}/deviceRotation");
            AddQuestControllerBinding(pointerRotationAction, "pointerRotation");
            AddQuestControllerBinding(pointerRotationAction, "deviceRotation");
            pointerPressAction = new InputAction("Quest Pointer Press", InputActionType.Button);
            pointerPressAction.AddBinding("<XRController>{RightHand}/trigger");
            pointerPressAction.AddBinding("<XRController>{LeftHand}/trigger");
            AddQuestControllerBinding(pointerPressAction, "trigger");
            AddQuestControllerBinding(pointerPressAction, "triggerPressed");

            if (QuestEnableHandInteractionPointer)
            {
                handPointerPositionAction = new InputAction("Quest Hand Pointer Position", InputActionType.PassThrough);
                handPointerPositionAction.AddBinding("<HandInteraction>{RightHand}/pointer/position");
                handPointerPositionAction.AddBinding("<HandInteraction>{LeftHand}/pointer/position");
                handPointerPositionAction.AddBinding("<HandInteraction>{RightHand}/pinchPosition");
                handPointerPositionAction.AddBinding("<HandInteraction>{LeftHand}/pinchPosition");
                handPointerRotationAction = new InputAction("Quest Hand Pointer Rotation", InputActionType.PassThrough);
                handPointerRotationAction.AddBinding("<HandInteraction>{RightHand}/pointer/rotation");
                handPointerRotationAction.AddBinding("<HandInteraction>{LeftHand}/pointer/rotation");
                handPointerRotationAction.AddBinding("<HandInteraction>{RightHand}/pinchRotation");
                handPointerRotationAction.AddBinding("<HandInteraction>{LeftHand}/pinchRotation");
                handPointerPressAction = new InputAction("Quest Hand Pointer Press", InputActionType.Button);
                handPointerPressAction.AddBinding("<HandInteraction>{RightHand}/pointerActivateValue");
                handPointerPressAction.AddBinding("<HandInteraction>{LeftHand}/pointerActivateValue");
                handPointerPressAction.AddBinding("<HandInteraction>{RightHand}/pinchValue");
                handPointerPressAction.AddBinding("<HandInteraction>{LeftHand}/pinchValue");
                handPointerPressAction.AddBinding("<HandInteraction>{RightHand}/pinchTouched");
                handPointerPressAction.AddBinding("<HandInteraction>{LeftHand}/pinchTouched");
            }

            leftThumbstickAction = new InputAction("Quest Left Thumbstick", InputActionType.PassThrough);
            leftThumbstickAction.expectedControlType = "Vector2";
            leftThumbstickAction.AddBinding("<XRController>{LeftHand}/primary2DAxis");
            leftThumbstickAction.AddBinding("<XRController>{LeftHand}/thumbstick");
            AddQuestControllerBinding(leftThumbstickAction, "thumbstick", leftHand: true, rightHand: false);
            leftPrimaryButtonAction = new InputAction("Quest Left X Button", InputActionType.Button);
            leftPrimaryButtonAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            AddQuestControllerBinding(leftPrimaryButtonAction, "primaryButton", leftHand: true, rightHand: false);
            rightThumbstickAction = new InputAction("Quest Right Thumbstick", InputActionType.PassThrough);
            rightThumbstickAction.expectedControlType = "Vector2";
            rightThumbstickAction.AddBinding("<XRController>{RightHand}/primary2DAxis");
            rightThumbstickAction.AddBinding("<XRController>{RightHand}/thumbstick");
            AddQuestControllerBinding(rightThumbstickAction, "thumbstick", leftHand: false, rightHand: true);
            rightThumbstickClickAction = new InputAction("Quest Right Thumbstick Click", InputActionType.Button);
            rightThumbstickClickAction.AddBinding("<XRController>{RightHand}/primary2DAxisClick");
            rightThumbstickClickAction.AddBinding("<XRController>{RightHand}/thumbstickClicked");
            AddQuestControllerBinding(rightThumbstickClickAction, "primary2DAxisClick", leftHand: false, rightHand: true);
            AddQuestControllerBinding(rightThumbstickClickAction, "thumbstickClicked", leftHand: false, rightHand: true);
            rightPrimaryButtonAction = new InputAction("Quest Right A Button", InputActionType.Button);
            rightPrimaryButtonAction.AddBinding("<XRController>{RightHand}/primaryButton");
            AddQuestControllerBinding(rightPrimaryButtonAction, "primaryButton", leftHand: false, rightHand: true);
            rightSecondaryButtonAction = new InputAction("Quest Right B Button", InputActionType.Button);
            rightSecondaryButtonAction.AddBinding("<XRController>{RightHand}/secondaryButton");
            AddQuestControllerBinding(rightSecondaryButtonAction, "secondaryButton", leftHand: false, rightHand: true);

            pointerPositionAction.Enable();
            pointerRotationAction.Enable();
            pointerPressAction.Enable();
            leftThumbstickAction.Enable();
            leftPrimaryButtonAction.Enable();
            rightThumbstickAction.Enable();
            rightThumbstickClickAction.Enable();
            rightPrimaryButtonAction.Enable();
            rightSecondaryButtonAction.Enable();
            try
            {
                handPointerPositionAction?.Enable();
                handPointerRotationAction?.Enable();
                handPointerPressAction?.Enable();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarningFormat("Quest XR hand interaction actions unavailable: {0}", ex.Message);
                handPointerPositionAction.Dispose();
                handPointerRotationAction.Dispose();
                handPointerPressAction.Dispose();
                handPointerPositionAction = null;
                handPointerRotationAction = null;
                handPointerPressAction = null;
            }
        }

        private static void AddQuestControllerBinding(
            InputAction action,
            string control,
            bool leftHand = true,
            bool rightHand = true)
        {
            if (action == null || string.IsNullOrEmpty(control))
                return;

            if (rightHand)
            {
                action.AddBinding("<QuestTouchPlusController>{RightHand}/" + control);
                action.AddBinding("<OculusTouchController>{RightHand}/" + control);
                action.AddBinding("<XRInputV1::Oculus::OculusTouchControllerOpenXR>{RightHand}/" + control);
            }

            if (leftHand)
            {
                action.AddBinding("<QuestTouchPlusController>{LeftHand}/" + control);
                action.AddBinding("<OculusTouchController>{LeftHand}/" + control);
                action.AddBinding("<XRInputV1::Oculus::OculusTouchControllerOpenXR>{LeftHand}/" + control);
            }
        }

        private void CreateVirtualMouseActions()
        {
            uiActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            uiActionAsset.name = "QuestVirtualMouseActions";
            var map = uiActionAsset.AddActionMap("UI");
            uiPointAction = map.AddAction("Quest UI Point", InputActionType.PassThrough);
            uiPointAction.expectedControlType = "Vector2";
            uiPointAction.AddBinding("<Mouse>/position");
            uiClickAction = map.AddAction("Quest UI Click", InputActionType.PassThrough);
            uiClickAction.expectedControlType = "Button";
            uiClickAction.AddBinding("<Mouse>/leftButton");
            uiActionAsset.Enable();
            uiPointReference = InputActionReference.Create(uiPointAction);
            uiClickReference = InputActionReference.Create(uiClickAction);
        }

        private void EnsureVirtualMouse()
        {
            virtualMouse = Mouse.current;
            if (virtualMouse == null)
                virtualMouse = InputSystem.AddDevice<Mouse>("QuestVirtualMouse");
            virtualMouse.MakeCurrent();
        }

        private void ConfigureMdproCameras(bool force = false)
        {
            if (Program.instance == null || Program.instance.camera_ == null || xrCamera == null)
                return;

            var cameraManager = Program.instance.camera_;
            var renderToUiPanel = !QuestUseWorldSpaceMdproUi
                && !QuestUseNativeMainMenu
                && !(QuestNativeDuelFrontendOnly && IsQuestNativeDuelActive());
            var now = Time.unscaledTime;
            if (!force
                && mdproCameraRenderModeInitialized
                && lastMdproCameraRenderToUiPanel == renderToUiPanel
                && now - lastMdproCameraConfigureTime < MdproCameraConfigureInterval)
                return;

            mdproCameraRenderModeInitialized = true;
            lastMdproCameraRenderToUiPanel = renderToUiPanel;
            lastMdproCameraConfigureTime = now;

            if (renderToUiPanel)
            {
                EnsureUiRenderTexture();
            }
            DetachMdproCamerasFromUrpStacks(cameraManager);
            foreach (var camera in GetMdproCameras())
            {
                if (camera == null || camera == xrCamera)
                    continue;

                var data = camera.GetUniversalAdditionalCameraData();
                data.allowXRRendering = false;
                data.allowHDROutput = false;
                data.renderPostProcessing = false;

                var isUiRenderCamera = IsUiRenderCamera(camera, cameraManager);
                if (isUiRenderCamera)
                {
                    if (camera == cameraManager.cameraUIBlur)
                    {
                        camera.targetTexture = null;
                        camera.enabled = false;
                        continue;
                    }

                    camera.allowHDR = false;
                    camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    if (renderToUiPanel && !camera.gameObject.activeSelf)
                        camera.gameObject.SetActive(true);
                    camera.enabled = renderToUiPanel;

                    if (!renderToUiPanel)
                    {
                        camera.targetTexture = null;
                        continue;
                    }

                    data.renderType = CameraRenderType.Base;
                    camera.targetTexture = uiRenderTexture;
                    ConfigureUiRenderCameraOrder(camera, cameraManager);
                    if (camera == cameraManager.camera2D)
                    {
                        camera.clearFlags = CameraClearFlags.SolidColor;
                    }
                    else
                    {
                        camera.clearFlags = CameraClearFlags.Depth;
                    }
                }
                else
                {
                    ConfigureLegacy3DCameraForXrScene(camera);
                }
            }

            if (UsePassthroughMixedReality)
                MakeCameraTransparent(xrCamera);
            else
                ConfigureXrCameraForVirtualWorld();
            ApplyResolvedXrCullingMask();

            if (!gameCamerasConfigured)
            {
                Debug.LogFormat(
                    "Quest XR immersive world camera enabled. EyeXZ={0}, LookAt={1}, Scale={2:F1} world-units/meter",
                    DuelEyePosition,
                    DuelLookTarget,
                    DuelWorldUnitsPerMeter);
                gameCamerasConfigured = true;
            }

            if (!uiRenderCameraLogged)
            {
                Debug.Log("Quest XR UI is rendered to a fixed world panel; 3D duel content is rendered directly by the XR camera.");
                uiRenderCameraLogged = true;
            }
        }

        private void DetachMdproCamerasFromUrpStacks(CameraManager cameraManager)
        {
            if (cameraManager == null)
                return;

            var mdproCameras = new HashSet<Camera>(GetMdproCameras());
            var removed = 0;
            foreach (var camera in FindObjectsOfType<Camera>(true))
            {
                if (camera == null)
                    continue;

                var data = camera.GetUniversalAdditionalCameraData();
                if (data == null || data.renderType != CameraRenderType.Base)
                    continue;
                if (data.cameraStack == null || data.cameraStack.Count == 0)
                    continue;

                for (var index = data.cameraStack.Count - 1; index >= 0; index -= 1)
                {
                    var stackedCamera = data.cameraStack[index];
                    if (stackedCamera == null || mdproCameras.Contains(stackedCamera))
                    {
                        data.cameraStack.RemoveAt(index);
                        removed += 1;
                    }
                }
            }

            if (removed > 0)
                Debug.LogFormat("Quest XR detached MDPro cameras from URP stacks: removed={0}", removed);
        }

        private static void ConfigureLegacy3DCameraForXrScene(Camera camera)
        {
            if (camera == null)
                return;

            camera.targetTexture = null;
            camera.allowHDR = false;
            if (camera.enabled)
                camera.enabled = false;

            var data = camera.GetUniversalAdditionalCameraData();
            data.allowXRRendering = false;
            data.allowHDROutput = false;
            data.renderPostProcessing = false;
            data.requiresColorOption = CameraOverrideOption.Off;
        }

        private int ResolveQuestCullingMask()
        {
            if (ShouldSuppressQuestNativeDuelFrontendNow())
                return 1 << GetQuestOverlayLayer();

            var mask = ~0;
            ExcludeLayerFromMask(ref mask, "2D");
            ExcludeLayerFromMask(ref mask, "UI");
            ExcludeLayerFromMask(ref mask, "QuestOverlay");
            ExcludeLayerFromMask(ref mask, "CardPicture");
            ExcludeLayerFromMask(ref mask, "RenderTextureTarget");
            ExcludeLayerFromMask(ref mask, "Matching0");
            ExcludeLayerFromMask(ref mask, "Matching1");
            ExcludeLayerFromMask(ref mask, "DuelOverlay2D");
            ExcludeLayerFromMask(ref mask, "DuelOverlayEffect2D");
            ExcludeLayerFromMask(ref mask, "DragDrop");
            ExcludeLayerFromMask(ref mask, "UIBlur");

            return mask | (1 << GetQuestOverlayLayer());
        }

        private void ApplyResolvedXrCullingMask()
        {
            if (xrCamera == null)
                return;

            xrCamera.cullingMask = ResolveQuestCullingMask();
            if (xrCamera.cullingMask != lastAppliedXrCullingMask)
            {
                lastAppliedXrCullingMask = xrCamera.cullingMask;
                Debug.LogFormat("Quest XR camera culling mask updated: 0x{0:X8}", xrCamera.cullingMask);
            }
        }

        private static bool ShouldRenderDuelWorldLayers()
        {
            var cameraManager = Program.instance?.camera_;
            if (cameraManager?.cameraMain != null && cameraManager.cameraMain.gameObject.activeInHierarchy)
                return true;

            var duelContainer = Program.instance == null ? null : Program.instance.container_3D;
            return duelContainer != null && duelContainer.GetComponentsInChildren<Renderer>(false).Length > 0;
        }

        private static void ExcludeLayerFromMask(ref int mask, string layerName)
        {
            var layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask &= ~(1 << layer);
        }

        private static bool IsUiRenderCamera(Camera camera, CameraManager cameraManager)
        {
            if (camera == null || cameraManager == null)
                return false;

            return camera == cameraManager.camera2D
                || camera == cameraManager.cameraDuelOverlay2D
                || camera == cameraManager.cameraDuelOverlayEffect2D
                || camera == cameraManager.cameraUI
                || camera == cameraManager.cameraUIBlur;
        }

        private static void ConfigureUiRenderCameraOrder(Camera camera, CameraManager cameraManager)
        {
            if (camera == null || cameraManager == null)
                return;

            if (camera == cameraManager.camera2D)
                camera.depth = UiRenderCameraDepthBase;
            else if (camera == cameraManager.cameraDuelOverlay2D)
                camera.depth = UiRenderCameraDepthBase + 20f;
            else if (camera == cameraManager.cameraDuelOverlayEffect2D)
                camera.depth = UiRenderCameraDepthBase + 30f;
            else if (camera == cameraManager.cameraUI)
                camera.depth = UiRenderCameraDepthUi;
            else
                camera.depth = UiRenderCameraDepthBase + 10f;
        }

        private Camera[] GetMdproCameras()
        {
            var cameraManager = Program.instance.camera_;
            return new[]
            {
                cameraManager.cameraMain,
                cameraManager.camera2D,
                cameraManager.cameraDuelOverlay3D,
                cameraManager.cameraDuelOverlayEffect3D,
                cameraManager.cameraDuelOverlay2D,
                cameraManager.cameraDuelOverlayEffect2D,
                cameraManager.cameraUI,
                cameraManager.cameraUIBlur
            };
        }

        private void MaintainQuestPassthroughVisibility()
        {
            MakeCameraTransparent(xrCamera);
            EnsureArPassthroughComponents();
            ConfigureDefaultSceneLayerForPassthrough();
            EnsurePassthroughCompositionLayerFallback();
            SuppressQuestBlackOverlays();

            var cameraManager = Program.instance?.camera_;
            var uiManager = Program.instance?.ui_;
            if (!passthroughVisibilityLogged && uiManager != null && cameraManager != null)
            {
                Debug.Log("Quest XR passthrough visibility layers made transparent.");
                passthroughVisibilityLogged = true;
            }
        }

        private void MaintainQuestWorldVisibility()
        {
            if (UsePassthroughMixedReality)
            {
                MaintainQuestPassthroughVisibility();
                return;
            }

            ConfigureXrCameraForVirtualWorld();
            DisablePassthroughComponents();
            CreateFallbackEnvironment();
            SuppressQuestBlackOverlays();

            var cameraManager = Program.instance?.camera_;
            var uiManager = Program.instance?.ui_;
            if (!passthroughVisibilityLogged && uiManager != null && cameraManager != null)
            {
                Debug.Log("Quest XR virtual world visibility configured.");
                passthroughVisibilityLogged = true;
            }
        }

        private void ConfigureDuelWorldRoot()
        {
            if (!IsQuestNativeDuelActive())
            {
                questDuelWorldPresenter?.SetVisible(false);
                SetQuestAndroidDuelFieldVisible(false);
                return;
            }

            var duelContainer = Program.instance == null ? null : Program.instance.container_3D;
            if (duelContainer == null)
                return;

            EnsureDuelWorldAnchor();
            if (duelWorldAnchor == null)
                return;
            AttachFallbackEnvironmentToDuelWorldAnchor();
            EnsureQuestAndroidDuelField();

            if (anchoredDuelContainer != duelContainer || duelContainer.parent != duelWorldAnchor)
            {
                duelContainer.SetParent(duelWorldAnchor, false);
                anchoredDuelContainer = duelContainer;
                lockedDuelAlignmentContainer = null;
                questDuelWorldUserTransformInitialized = false;
                questDuelWorldScaleMultiplier = 1f;
                originalFieldHiddenContainer = null;
                legacyResidueContainer = null;
                SuppressLegacyDuelContainerImmediately(duelContainer);
            }

            HideOptionalDuelScenery(duelContainer);
            SuppressLegacyDuelPresentationResidues(duelContainer);
            if (HasLockedDuelWorldAlignment(duelContainer))
            {
                ApplyLockedDuelWorldAlignment(duelContainer);
                HideOriginalDuelFieldVisuals(duelContainer);
            }
            else
            {
                duelContainer.localPosition = Vector3.zero;
                duelContainer.localRotation = Quaternion.identity;
                AlignDuelWorldToGround(duelContainer);
                if (HasLockedDuelWorldAlignment(duelContainer))
                    HideOriginalDuelFieldVisuals(duelContainer);
            }

            EnsureQuestDuelWorldPresenter();
            questDuelWorldPresenter.Configure(xrCamera, duelWorldAnchor);
            questDuelWorldPresenter.Sync(duelContainer);

            if (!duelWorldRootLogged)
            {
                Debug.LogFormat(
                    "Quest XR duel world anchored. Container={0}, AnchorPos={1}, AnchorRot={2}, AnchorScale={3}",
                    GetTransformPath(duelContainer),
                    duelWorldAnchor.position,
                    duelWorldAnchor.rotation.eulerAngles,
                    duelWorldAnchor.lossyScale);
                duelWorldRootLogged = true;
            }
        }

        private void SuppressQuestLegacyDuelVisualsLate()
        {
            if (!IsQuestNativeDuelActive())
                return;

            var duelContainer = Program.instance == null ? null : Program.instance.container_3D;
            if (duelContainer == null)
                return;

            HideOptionalDuelScenery(duelContainer);
            HideOriginalDuelFieldVisuals(duelContainer);
            SuppressLegacyDuelPresentationResidues(duelContainer);
        }

        private void EnsureDuelWorldAnchor()
        {
            if (duelWorldAnchor != null)
                return;

            var anchorObject = new GameObject("QuestDuelWorldAnchor");
            duelWorldAnchor = anchorObject.transform;
            duelWorldAnchor.SetParent(transform, true);
            duelWorldAnchor.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            duelWorldAnchor.localScale = Vector3.one;
            AttachFallbackEnvironmentToDuelWorldAnchor();
        }

        private void AttachFallbackEnvironmentToDuelWorldAnchor()
        {
            if (fallbackEnvironmentRoot == null || duelWorldAnchor == null)
                return;

            if (fallbackEnvironmentRoot.parent != duelWorldAnchor)
                fallbackEnvironmentRoot.SetParent(duelWorldAnchor, false);

            fallbackEnvironmentRoot.localPosition = Vector3.zero;
            fallbackEnvironmentRoot.localRotation = Quaternion.identity;
            fallbackEnvironmentRoot.localScale = Vector3.one;
        }

        private void EnsureQuestAndroidDuelField()
        {
            if (duelWorldAnchor == null)
                return;

            if (questDuelFieldRoot == null)
            {
                var fieldObject = new GameObject("QuestAndroidDuelFieldRoot");
                SetQuestOverlayLayer(fieldObject);
                questDuelFieldRoot = fieldObject.transform;
            }

            if (questDuelFieldRoot.parent != duelWorldAnchor)
                questDuelFieldRoot.SetParent(duelWorldAnchor, false);
            questDuelFieldRoot.localRotation = Quaternion.identity;

            SetQuestAndroidDuelFieldVisible(true);
            if (questDuelFieldLoadSucceeded || questDuelFieldLoadInProgress)
                return;

            if (Time.unscaledTime < questDuelFieldNextRetryTime)
                return;

            StartCoroutine(LoadQuestAndroidDuelField());
        }

        private IEnumerator LoadQuestAndroidDuelField()
        {
            if (questDuelFieldRoot == null)
                yield break;

            questDuelFieldLoadInProgress = true;
            questDuelFieldLoadAttempt += 1;
            questDuelFieldLoggedReady = false;
            ClearQuestAndroidDuelFieldObjects();

            Debug.LogFormat(
                "Quest Android duel field load started. Attempt={0}, Near={1}, Far={2}, Root={3}",
                questDuelFieldLoadAttempt,
                QuestAndroidDuelFieldNearPath,
                QuestAndroidDuelFieldFarPath,
                GetTransformPath(questDuelFieldRoot));

            GameObject near = null;
            GameObject far = null;
            yield return LoadQuestAndroidDuelFieldAsset(QuestAndroidDuelFieldNearPath, loaded => near = loaded);
            yield return LoadQuestAndroidDuelFieldAsset(QuestAndroidDuelFieldFarPath, loaded => far = loaded);

            questNearField = near;
            questFarField = far;
            SetQuestAndroidDuelFieldObjectsActive(true);
            var builtSurface = TryBuildQuestDuelFieldSurfaceFromResources(out var surfaceSource);
            SetQuestAndroidDuelFieldObjectsActive(false);
            var renderers = CountQuestAndroidDuelFieldRenderers();
            questDuelFieldLoadSucceeded = builtSurface && renderers > 0;
            questDuelFieldLoadInProgress = false;

            if (!questDuelFieldLoadSucceeded)
            {
                questDuelFieldNextRetryTime = Time.unscaledTime + QuestAndroidDuelFieldRetryDelay;
                if (questDuelFieldRoot != null)
                    questDuelFieldRoot.gameObject.SetActive(false);
                UpdateQuestProceduralGroundVisibility();
                Debug.LogWarningFormat(
                    "Quest Android duel field load produced no visible renderers. Attempt={0}, RetryIn={1:F1}s",
                    questDuelFieldLoadAttempt,
                    QuestAndroidDuelFieldRetryDelay);
                yield break;
            }

            SetQuestAndroidDuelFieldVisible(true);
            UpdateQuestProceduralGroundVisibility();
            Debug.LogFormat(
                "Quest Android duel field resource surface ready. Source={0}, VisibleRenderers={1}",
                surfaceSource,
                renderers);
        }

        private IEnumerator LoadQuestAndroidDuelFieldAsset(string path, Action<GameObject> onLoaded)
        {
            GameObject loadedObject = null;
            var loader = ABLoader.LoadFromFileAsync(path, cache: true, copy: true, suppressOptionalScenery: false);
            while (loader.MoveNext())
            {
                if (loader.Current != null)
                {
                    loadedObject = loader.Current;
                    loadedObject.SetActive(false);
                }
                yield return null;
            }

            loader.Dispose();
            if (loadedObject == null)
            {
                Debug.LogWarningFormat("Quest Android duel field asset missing or empty: {0}", path);
                onLoaded?.Invoke(null);
                yield break;
            }

            loadedObject.name = "QuestAndroidField_" + path.Replace('/', '_');
            ConfigureQuestAndroidDuelFieldObject(loadedObject, path);
            loadedObject.transform.SetParent(questDuelFieldRoot, false);
            loadedObject.transform.localPosition = Vector3.zero;
            loadedObject.transform.localRotation = Quaternion.identity;
            loadedObject.transform.localScale = Vector3.one;
            onLoaded?.Invoke(loadedObject);

            Debug.LogFormat(
                "Quest Android duel field asset attached. Path={0}, Renderers={1}, Root={2}",
                path,
                CountSourceRenderers(loadedObject.transform),
                GetTransformPath(loadedObject.transform));
        }

        private void ConfigureQuestAndroidDuelFieldObject(GameObject root, string sourcePath)
        {
            if (root == null)
                return;

            SetQuestOverlayLayer(root);
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (camera == null)
                    continue;

                camera.enabled = false;
                camera.targetTexture = null;
                var data = camera.GetUniversalAdditionalCameraData();
                data.allowXRRendering = false;
                data.allowHDROutput = false;
                data.renderPostProcessing = false;
            }

            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas != null)
                    canvas.enabled = false;
            foreach (var raycaster in root.GetComponentsInChildren<GraphicRaycaster>(true))
                if (raycaster != null)
                    raycaster.enabled = false;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                if (collider != null)
                    collider.enabled = false;

            var sourceRenderers = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                sourceRenderers += 1;
                renderer.enabled = true;
                renderer.forceRenderingOff = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            Debug.LogFormat(
                "Quest Android duel field source prepared. Source={0}, SourceRenderers={1}",
                sourcePath,
                sourceRenderers);
        }

        private bool TryBuildQuestDuelFieldSurfaceFromResources(out string sourceDescription)
        {
            sourceDescription = string.Empty;
            ClearQuestDuelFieldSurface();
            if (questDuelFieldRoot == null)
                return false;

            var samples = CollectQuestDuelFieldMaterialSamples();
            if (samples.Count == 0)
            {
                Debug.LogWarning("Quest Android duel field resource surface could not find suitable material samples.");
                return false;
            }

            var baseSample = samples[0];
            var accentSample = samples.Count > 1 ? samples[1] : baseSample;
            var detailSample = samples.Count > 2 ? samples[2] : accentSample;
            LogQuestDuelFieldMaterialCandidates(samples);

            questDuelFieldSurface = new GameObject("QuestDuelFieldResourceLayer");
            SetQuestOverlayLayer(questDuelFieldSurface);
            questDuelFieldSurface.transform.SetParent(questDuelFieldRoot, false);
            questDuelFieldSurface.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            questDuelFieldSurface.transform.localScale = Vector3.one;

            var baseMaterial = CreateQuestDuelFieldSurfaceMaterial(
                baseSample.Material,
                "Base/" + baseSample.Description,
                new Color(0.92f, 0.96f, 1f, 1f),
                false,
                QuestAndroidDuelFieldTargetWidth / 5.4f,
                QuestAndroidDuelFieldTargetDepth / 4.8f);
            if (baseMaterial == null)
            {
                Debug.LogWarning("Quest Android duel field resource surface could not create base material.");
                return false;
            }

            var zoneMaterial = CreateQuestDuelFieldSurfaceMaterial(
                accentSample.Material,
                "Zone/" + accentSample.Description,
                new Color(0.18f, 0.80f, 0.76f, 0.24f),
                true,
                1.15f,
                1.15f);
            var pileMaterial = CreateQuestDuelFieldSurfaceMaterial(
                detailSample.Material,
                "Pile/" + detailSample.Description,
                new Color(0.36f, 0.60f, 0.98f, 0.22f),
                true,
                1f,
                1f);
            var centerMaterial = CreateQuestDuelFieldSurfaceMaterial(
                accentSample.Material,
                "Center/" + accentSample.Description,
                new Color(0.96f, 0.82f, 0.30f, 0.16f),
                true,
                2.4f,
                0.55f);

            CreateQuestDuelFieldSurfacePart(
                "QuestDuelFieldResourceBase",
                new Vector3(0f, QuestAndroidDuelFieldGroundY, DuelWorldCenterOnGround.z),
                QuestAndroidDuelFieldTargetWidth,
                QuestAndroidDuelFieldTargetDepth,
                baseMaterial);

            if (centerMaterial != null)
            {
                CreateQuestDuelFieldSurfacePart(
                    "QuestDuelFieldResourceCenterBand",
                    new Vector3(0f, DuelArenaLineY - 0.034f, 0f),
                    QuestAndroidDuelFieldTargetWidth * 0.86f,
                    3.2f,
                    centerMaterial);
            }

            if (zoneMaterial != null)
                CreateQuestDuelFieldZonePads(zoneMaterial);
            if (pileMaterial != null)
                CreateQuestDuelFieldPilePads(pileMaterial);

            questDuelFieldRoot.localPosition = Vector3.zero;
            questDuelFieldRoot.localRotation = Quaternion.identity;
            questDuelFieldRoot.localScale = Vector3.one;
            questDuelFieldLoggedReady = true;
            sourceDescription = baseSample.Description;

            Debug.LogFormat(
                "Quest Android duel field resource layer built. Base={0}, Accent={1}, Detail={2}, Parts={3}, Size=({4:F1},{5:F1})",
                baseSample.Description,
                accentSample.Description,
                detailSample.Description,
                CountRenderableRenderers(questDuelFieldSurface.transform),
                QuestAndroidDuelFieldTargetWidth,
                QuestAndroidDuelFieldTargetDepth);
            return true;
        }

        private void CreateQuestDuelFieldZonePads(Material material)
        {
            var y = DuelArenaLineY - 0.026f;
            var rows = new[]
            {
                ScaleDuelBoardZ(-9.48f),
                ScaleDuelBoardZ(-18f),
                ScaleDuelBoardZ(9.51f),
                ScaleDuelBoardZ(18f)
            };

            foreach (var z in rows)
                foreach (var x in DuelMainZoneX)
                    CreateQuestDuelFieldSurfacePart(
                        "QuestDuelFieldResourceZonePad",
                        new Vector3(x, y, z),
                        DuelZoneWidth * 0.94f,
                        DuelZoneDepth * 0.94f,
                        material);
        }

        private void CreateQuestDuelFieldPilePads(Material material)
        {
            var y = DuelArenaLineY - 0.022f;
            var pads = new[]
            {
                ScaleDuelBoardPoint(new Vector3(26.86f, y, -23.93f)),
                ScaleDuelBoardPoint(new Vector3(-26.86f, y, -23.93f)),
                ScaleDuelBoardPoint(new Vector3(25.74f, y, -14.26f)),
                ScaleDuelBoardPoint(new Vector3(27.58f, y, -8.02f)),
                ScaleDuelBoardPoint(new Vector3(-26.86f, y, 23.93f)),
                ScaleDuelBoardPoint(new Vector3(26.86f, y, 23.93f)),
                ScaleDuelBoardPoint(new Vector3(-25.74f, y, 14.26f)),
                ScaleDuelBoardPoint(new Vector3(-27.58f, y, 8.02f))
            };

            foreach (var center in pads)
                CreateQuestDuelFieldSurfacePart(
                    "QuestDuelFieldResourcePilePad",
                    center,
                    DuelZoneWidth * 0.92f,
                    DuelZoneDepth * 0.76f,
                    material);
        }

        private void CreateQuestDuelFieldSurfacePart(string name, Vector3 localPosition, float width, float depth, Material material)
        {
            if (questDuelFieldSurface == null || material == null)
                return;

            var part = new GameObject(name);
            SetQuestOverlayLayer(part);
            part.transform.SetParent(questDuelFieldSurface.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;

            var mesh = CreateQuestDuelFieldSurfaceMesh(width, depth);
            questDuelFieldSurfaceMeshes.Add(mesh);
            var filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.forceRenderingOff = false;
        }

        private List<QuestDuelFieldMaterialSample> CollectQuestDuelFieldMaterialSamples()
        {
            var candidates = new List<QuestDuelFieldMaterialSample>();
            CollectQuestDuelFieldMaterialSamples(questNearField, candidates);
            CollectQuestDuelFieldMaterialSamples(questFarField, candidates);
            candidates.Sort((left, right) => right.Score.CompareTo(left.Score));

            var selected = new List<QuestDuelFieldMaterialSample>();
            var seenKeys = new HashSet<string>();
            foreach (var candidate in candidates)
            {
                var key = candidate.Texture != null
                    ? "tex:" + candidate.Texture.GetInstanceID()
                    : "mat:" + (candidate.Material == null ? 0 : candidate.Material.GetInstanceID());
                if (!seenKeys.Add(key))
                    continue;

                selected.Add(candidate);
                if (selected.Count >= 8)
                    break;
            }

            return selected;
        }

        private void CollectQuestDuelFieldMaterialSamples(GameObject root, List<QuestDuelFieldMaterialSample> candidates)
        {
            if (root == null || candidates == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (TryCreateQuestDuelFieldMaterialSample(renderer, root.name, out var sample))
                    candidates.Add(sample);
        }

        private bool TryCreateQuestDuelFieldMaterialSample(Renderer renderer, string sourceName, out QuestDuelFieldMaterialSample sample)
        {
            sample = null;
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is SpriteRenderer || renderer is LineRenderer)
                return false;

            var material = ResolveQuestDuelFieldSourceMaterial(renderer);
            if (material == null)
                return false;

            var path = GetTransformPath(renderer.transform);
            var normalized = (sourceName + "/" + path).ToLowerInvariant();
            if (IsQuestDuelFieldResourceExcludedPath(normalized))
                return false;

            var localBounds = ConvertWorldBoundsToLocal(questDuelFieldRoot, renderer.bounds);
            var size = localBounds.size;
            var horizontalArea = Mathf.Max(size.x, 0.001f) * Mathf.Max(size.z, 0.001f);
            var horizontalSize = Mathf.Max(size.x, size.z);
            if (horizontalSize < 0.25f || horizontalArea < 0.05f)
                return false;

            var flatness = horizontalSize / Mathf.Max(size.y, 0.025f);
            var score = horizontalArea * Mathf.Clamp(flatness, 1f, 80f);
            if (HasQuestDuelFieldSurfaceName(normalized))
                score *= 3f;
            var texture = ResolveQuestDuelFieldTexture(material);
            if (texture != null)
                score *= 2.4f;
            if (size.x > 2f && size.z > 2f)
                score *= 1.75f;
            if (normalized.Contains("field") || normalized.Contains("floor") || normalized.Contains("mat") || normalized.Contains("base"))
                score *= 1.45f;
            if (normalized.Contains("far"))
                score *= 0.72f;

            sample = new QuestDuelFieldMaterialSample
            {
                Material = material,
                Texture = texture,
                Score = score,
                Description = sourceName + "/" + path + " material=" + material.name + " texture=" + (texture == null ? "<none>" : texture.name) + " bounds=" + size
            };
            return true;
        }

        private void LogQuestDuelFieldMaterialCandidates(List<QuestDuelFieldMaterialSample> samples)
        {
            if (samples == null || samples.Count == 0)
                return;

            var count = Mathf.Min(samples.Count, 5);
            for (var i = 0; i < count; i += 1)
            {
                var sample = samples[i];
                Debug.LogFormat(
                    "Quest Android duel field material candidate #{0}: Score={1:F2}, Source={2}, Texture={3}",
                    i + 1,
                    sample.Score,
                    sample.Description,
                    sample.Texture == null ? "<none>" : sample.Texture.name);
            }
        }

        private sealed class QuestDuelFieldMaterialSample
        {
            public Material Material;
            public Texture Texture;
            public float Score;
            public string Description;
        }

        private static bool IsQuestDuelFieldResourceExcludedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var normalized = path.ToLowerInvariant();
            return normalized.Contains("camera") ||
                normalized.Contains("canvas") ||
                normalized.Contains("ui") ||
                normalized.Contains("effect") ||
                normalized.Contains("fxp") ||
                normalized.Contains("particle") ||
                normalized.Contains("sky") ||
                normalized.Contains("cloud") ||
                normalized.Contains("background") ||
                normalized.Contains("wall") ||
                normalized.Contains("outside") ||
                normalized.Contains("tree") ||
                normalized.Contains("leaf") ||
                normalized.Contains("foliage") ||
                normalized.Contains("bush") ||
                normalized.Contains("shrub") ||
                normalized.Contains("flower") ||
                normalized.Contains("rabbit") ||
                normalized.Contains("bunny") ||
                normalized.Contains("usagi") ||
                normalized.Contains("character") ||
                normalized.Contains("monster") ||
                normalized.Contains("mate");
        }

        private static bool HasQuestDuelFieldSurfaceName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return path.Contains("field") ||
                path.Contains("ground") ||
                path.Contains("floor") ||
                path.Contains("table") ||
                path.Contains("mat") ||
                path.Contains("stage") ||
                path.Contains("base") ||
                path.Contains("plate") ||
                path.Contains("board") ||
                path.Contains("duel");
        }

        private static Material ResolveQuestDuelFieldSourceMaterial(Renderer renderer)
        {
            if (renderer == null)
                return null;

            foreach (var material in renderer.sharedMaterials)
                if (material != null)
                    return material;
            return renderer.sharedMaterial;
        }

        private static Texture ResolveQuestDuelFieldTexture(Material material)
        {
            if (material == null)
                return null;

            string[] textureNames =
            {
                "_BaseMap",
                "_MainTex",
                "_BaseColorMap",
                "_Albedo",
                "_Diffuse",
                "_EmissionMap"
            };

            foreach (var textureName in textureNames)
            {
                if (!material.HasProperty(textureName))
                    continue;
                var texture = material.GetTexture(textureName);
                if (texture != null)
                    return texture;
            }

            return material.mainTexture;
        }

        private static Color ResolveQuestDuelFieldColor(Material material)
        {
            if (material == null)
                return new Color(0.16f, 0.22f, 0.24f, 1f);

            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");
            return new Color(0.16f, 0.22f, 0.24f, 1f);
        }

        private Material CreateQuestDuelFieldSurfaceMaterial(
            Material source,
            string sourceDescription,
            Color tint,
            bool transparent,
            float uvScaleX,
            float uvScaleY)
        {
            var sourceColor = ResolveQuestDuelFieldColor(source);
            var color = transparent
                ? tint
                : new Color(sourceColor.r * tint.r, sourceColor.g * tint.g, sourceColor.b * tint.b, 1f);
            if (color.maxColorComponent < 0.18f && !transparent)
                color = new Color(0.22f, 0.27f, 0.30f, 1f);

            var material = CreateColorMaterial("Quest Duel Field Resource Surface Material", color, transparent);
            var texture = ResolveQuestDuelFieldTexture(source);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
                material.mainTexture = texture;
                material.mainTextureScale = new Vector2(Mathf.Max(uvScaleX, 0.05f), Mathf.Max(uvScaleY, 0.05f));
            }

            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.18f);
            material.renderQueue = transparent ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
            material.name = "Quest Duel Field Resource Surface Material (" + sourceDescription + ")";
            questDuelFieldSurfaceMaterials.Add(material);
            return material;
        }

        private static Mesh CreateQuestDuelFieldSurfaceMesh(float width, float depth)
        {
            var halfWidth = width * 0.5f;
            var halfDepth = depth * 0.5f;
            var mesh = new Mesh
            {
                name = "QuestDuelFieldResourceSurfaceMesh",
                vertices = new[]
                {
                    new Vector3(-halfWidth, 0f, -halfDepth),
                    new Vector3(halfWidth, 0f, -halfDepth),
                    new Vector3(halfWidth, 0f, halfDepth),
                    new Vector3(-halfWidth, 0f, halfDepth)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                },
                triangles = new[] { 0, 2, 1, 0, 3, 2 }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
        private int CountQuestAndroidDuelFieldRenderers()
        {
            return CountRenderableRenderers(questDuelFieldRoot);
        }

        private static int CountRenderableRenderers(Transform root)
        {
            if (root == null)
                return 0;

            var count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (IsQuestAndroidDuelFieldRendererUsable(renderer))
                    count += 1;
            return count;
        }

        private static int CountSourceRenderers(Transform root)
        {
            if (root == null)
                return 0;

            var count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer != null && !(renderer is ParticleSystemRenderer))
                    count += 1;
            return count;
        }

        private static bool IsQuestAndroidDuelFieldRendererUsable(Renderer renderer)
        {
            return renderer != null
                && renderer.enabled
                && !renderer.forceRenderingOff
                && !(renderer is ParticleSystemRenderer);
        }

        private void SetQuestAndroidDuelFieldObjectsActive(bool active)
        {
            if (questNearField != null && questNearField.activeSelf != active)
                questNearField.SetActive(active);
            if (questFarField != null && questFarField.activeSelf != active)
                questFarField.SetActive(active);
        }

        private void SetQuestAndroidDuelFieldVisible(bool visible)
        {
            if (questDuelFieldRoot != null && questDuelFieldRoot.gameObject.activeSelf != visible)
                questDuelFieldRoot.gameObject.SetActive(visible);
            UpdateQuestProceduralGroundVisibility();
        }

        private void UpdateQuestProceduralGroundVisibility()
        {
            if (fallbackEnvironmentRoot == null)
                return;

            var showProceduralGround = !questDuelFieldLoadSucceeded
                || questDuelFieldRoot == null
                || !questDuelFieldRoot.gameObject.activeSelf;

            for (var i = 0; i < fallbackEnvironmentRoot.childCount; i += 1)
            {
                var child = fallbackEnvironmentRoot.GetChild(i);
                if (child == null)
                    continue;

                if (child.name == "QuestDuelMatteFloor" || child.name == "QuestWorldReferenceLine")
                    child.gameObject.SetActive(showProceduralGround);
            }
        }

        private void ClearQuestAndroidDuelFieldObjects()
        {
            questNearField = null;
            questFarField = null;
            ClearQuestDuelFieldSurface();
            if (questDuelFieldRoot == null)
                return;

            for (var i = questDuelFieldRoot.childCount - 1; i >= 0; i -= 1)
            {
                var child = questDuelFieldRoot.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        private void ClearQuestDuelFieldSurface()
        {
            if (questDuelFieldSurface != null)
            {
                Destroy(questDuelFieldSurface);
                questDuelFieldSurface = null;
            }

            foreach (var mesh in questDuelFieldSurfaceMeshes)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
            questDuelFieldSurfaceMeshes.Clear();

            foreach (var material in questDuelFieldSurfaceMaterials)
            {
                if (material != null)
                    Destroy(material);
            }
            questDuelFieldSurfaceMaterials.Clear();
        }

        private void EnsureQuestDuelWorldPresenter()
        {
            if (questDuelWorldPresenter != null)
                return;

            questDuelWorldPresenter = gameObject.AddComponent<QuestDuelWorldPresenter>();
        }

        private void EnsureQuestDuelNativeUi()
        {
            if (questDuelNativeUi == null)
                questDuelNativeUi = gameObject.AddComponent<QuestDuelNativeUi>();

            questDuelNativeUi.Configure(xrCamera, duelWorldAnchor);
        }

        private void EnsureQuestNativeMainMenu()
        {
            if (!QuestUseNativeMainMenu)
                return;

            if (questNativeMainMenu == null)
                questNativeMainMenu = gameObject.AddComponent<QuestNativeMainMenu>();

            questNativeMainMenu.Initialize(xrCamera);
        }

        private void PreSuppressQuestDuelPresentation()
        {
            if (!QuestNativeDuelFrontendOnly || Program.instance == null)
                return;

            if (!ShouldSuppressQuestNativeDuelFrontendNow())
                return;

            var duelContainer = Program.instance.container_3D;
            if (duelContainer != null && earlySuppressedDuelContainer != duelContainer)
            {
                earlySuppressedDuelContainer = duelContainer;
                earlyDuelSuppressionLogged = false;
            }

            var hiddenRenderers = duelContainer == null ? 0 : SuppressLegacyDuelContainerImmediately(duelContainer);
            var disabledCameras = ForceMdproDuelCamerasOff();
            ApplyResolvedXrCullingMask();

            if (!earlyDuelSuppressionLogged)
            {
                Debug.LogFormat(
                    "Quest XR early duel presentation suppression active. Container={0}, RenderersHiddenNow={1}, CamerasDisabledNow={2}",
                    duelContainer == null ? "<pending>" : GetTransformPath(duelContainer),
                    hiddenRenderers,
                    disabledCameras);
                earlyDuelSuppressionLogged = true;
            }
        }

        private static bool ShouldSuppressQuestNativeDuelFrontendNow()
        {
            if (!QuestNativeDuelFrontendOnly || Program.instance == null)
                return false;

            var core = Program.instance.ocgcore;
            return IsOcgCoreVisibleServant(core);
        }

        private int ForceMdproDuelCamerasOff()
        {
            if (Program.instance == null || Program.instance.camera_ == null)
                return 0;

            var disabled = 0;
            foreach (var camera in GetMdproCameras())
            {
                if (camera == null || camera == xrCamera)
                    continue;

                if (camera.enabled)
                {
                    camera.enabled = false;
                    disabled += 1;
                }

                if (camera.targetTexture != null)
                    camera.targetTexture = null;

                var data = camera.GetUniversalAdditionalCameraData();
                data.allowXRRendering = false;
                data.allowHDROutput = false;
                data.renderPostProcessing = false;
            }

            return disabled;
        }

        private static int SuppressLegacyDuelContainerImmediately(Transform duelContainer)
        {
            if (!QuestNativeDuelFrontendOnly || duelContainer == null)
                return 0;

            var hiddenRenderers = 0;
            foreach (var renderer in duelContainer.GetComponentsInChildren<Renderer>(true))
                if (renderer != null)
                {
                    if (!renderer.forceRenderingOff)
                        hiddenRenderers += 1;
                    renderer.forceRenderingOff = true;
                }
            foreach (var collider in duelContainer.GetComponentsInChildren<Collider>(true))
                if (collider != null)
                    collider.enabled = false;
            foreach (var canvas in duelContainer.GetComponentsInChildren<Canvas>(true))
                if (canvas != null)
                    canvas.enabled = false;
            return hiddenRenderers;
        }

        private void AttachMonsterCutinToWorld(GameObject root)
        {
            if (root == null)
                return;

            EnsureQuestCutinWorldRoot();
            if (questCutinWorldRoot == null)
                return;

            root.transform.SetParent(questCutinWorldRoot, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * QuestPreviewCutinScale;
            ConfigureQuestPreviewObject(root);
            EnsureQuestPreviewHitTarget(root, false, root.transform);
            LogQuestPreviewAttachment("Cutin", root.transform, QuestPreviewCutinScale);
        }

        private void AttachMatePreviewToWorld(Mate mate)
        {
            if (mate == null)
                return;

            EnsureQuestMatePreviewWorldRoot();
            if (questMatePreviewWorldRoot == null)
                return;

            mate.parent = questMatePreviewWorldRoot;
            mate.transform.SetParent(questMatePreviewWorldRoot, false);
            mate.transform.localPosition = Vector3.zero;
            mate.transform.localRotation = Quaternion.identity;
            questSelectedMatePreview = mate.transform;
            ConfigureQuestPreviewObject(mate.gameObject);
            var previewScale = Mathf.Max(questMatePreviewScaleMultiplier, QuestWorldScaleMinPositive);
            questMatePreviewWorldRoot.localScale = Vector3.one;
            FitQuestPreviewToHeight(mate.transform, questMatePreviewWorldRoot, QuestPreviewMateTargetHeight);
            EnsureQuestPreviewHitTarget(mate.gameObject, true, mate.transform);
            LayoutQuestMatePreviews();
            questMatePreviewWorldRoot.localScale = Vector3.one * previewScale;
            LogQuestPreviewAttachment("Mate", mate.transform, QuestPreviewMateTargetHeight);
        }

        private void DetachMatePreviewFromWorld(Mate mate)
        {
            if (mate == null)
                return;

            if (questMatePreviewWorldRoot != null && mate.transform.IsChildOf(questMatePreviewWorldRoot))
            {
                mate.transform.SetParent(null, true);
                LayoutQuestMatePreviews();
            }
        }

        private void LayoutQuestMatePreviews()
        {
            if (questMatePreviewWorldRoot == null)
                return;

            var mateTransforms = new List<Transform>();
            for (int i = 0; i < questMatePreviewWorldRoot.childCount; i++)
            {
                var child = questMatePreviewWorldRoot.GetChild(i);
                if (child != null && child.GetComponent<Mate>() != null)
                    mateTransforms.Add(child);
            }

            var count = mateTransforms.Count;
            if (count == 0)
            {
                questSelectedMatePreview = null;
                return;
            }

            var center = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                var slot = i - center;
                var child = mateTransforms[i];
                var localPosition = child.localPosition;
                localPosition.x = slot * QuestPreviewMateGroupSpacing;
                localPosition.z = 0f;
                child.localPosition = localPosition;
                child.SetSiblingIndex(i);
                EnsureQuestPreviewHitTarget(child.gameObject, true, child);
            }

            if (questSelectedMatePreview == null || !questSelectedMatePreview.IsChildOf(questMatePreviewWorldRoot))
                questSelectedMatePreview = mateTransforms[count - 1];

            Debug.LogFormat("Quest mate preview layout updated. Count={0}, Spacing={1:F1}", count, QuestPreviewMateGroupSpacing);
        }

        private void EnsureQuestCutinWorldRoot()
        {
            if (questCutinWorldRoot == null)
            {
                var rootObject = new GameObject("QuestMonsterCutinWorldRoot");
                SetQuestOverlayLayer(rootObject);
                questCutinWorldRoot = rootObject.transform;
            }

            PlaceQuestPreviewRoot(
                questCutinWorldRoot,
                QuestPreviewCutinDefaultOffset + questCutinPreviewOffset,
                true,
                questCutinPreviewRotation,
                questCutinPreviewScaleMultiplier);
        }

        private void EnsureQuestMatePreviewWorldRoot()
        {
            if (questMatePreviewWorldRoot == null)
            {
                var rootObject = new GameObject("QuestMatePreviewWorldRoot");
                SetQuestOverlayLayer(rootObject);
                questMatePreviewWorldRoot = rootObject.transform;
            }

            PlaceQuestPreviewRoot(
                questMatePreviewWorldRoot,
                QuestPreviewMateDefaultOffset + questMatePreviewOffset,
                false,
                questMatePreviewRotation,
                questMatePreviewScaleMultiplier);
        }

        private void PlaceQuestPreviewRoot(
            Transform root,
            Vector3 localOffset,
            bool pitchTowardEye,
            Vector2 rotationOffset,
            float scaleMultiplier = 1f)
        {
            if (root == null)
                return;

            root.SetParent(transform, true);
            var baseRotation = GetDuelBaseYawRotation();
            var position = DuelWorldCenterOnGround
                + baseRotation * new Vector3(localOffset.x, 0f, localOffset.z)
                + Vector3.up * localOffset.y;
            var forward = DuelEyePosition - position;
            if (!pitchTowardEye)
                forward.y = 0f;
            var rootRotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : baseRotation * Quaternion.Euler(0f, 180f, 0f);
            rootRotation *= Quaternion.Euler(rotationOffset.y, rotationOffset.x, 0f);

            root.SetPositionAndRotation(position, rootRotation);
            root.localScale = Vector3.one * Mathf.Max(scaleMultiplier, QuestWorldScaleMinPositive);
            SetQuestOverlayLayer(root.gameObject);
        }

        private void FitQuestPreviewToHeight(Transform target, Transform stageRoot, float targetHeight)
        {
            if (target == null || stageRoot == null)
                return;

            if (!TryGetRendererBounds(target.gameObject, out var bounds) || bounds.size.y < 0.001f)
                return;

            var factor = Mathf.Clamp(targetHeight / bounds.size.y, 0.02f, 500f);
            target.localScale *= factor;

            if (!TryGetRendererBounds(target.gameObject, out bounds))
                return;

            var localCenter = stageRoot.InverseTransformPoint(bounds.center);
            target.localPosition -= new Vector3(localCenter.x, 0f, localCenter.z);

            if (!TryGetRendererBounds(target.gameObject, out bounds))
                return;

            target.position += Vector3.up * (stageRoot.position.y - bounds.min.y);
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            if (root == null)
            {
                bounds = default;
                return false;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            bounds = default;
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            return hasBounds;
        }

        private static void EnsureQuestPreviewHitTarget(GameObject root, bool matePreview, Transform targetRoot = null)
        {
            if (root == null || !TryGetRendererBounds(root, out var worldBounds))
                return;

            var collider = root.GetComponent<BoxCollider>();
            if (collider == null)
                collider = root.AddComponent<BoxCollider>();

            var localBounds = TransformWorldBoundsToLocal(root.transform, worldBounds);
            var lossyScale = root.transform.lossyScale;
            var maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z), 0.001f);
            var padding = QuestPreviewHitPadding / maxScale;
            collider.center = localBounds.center;
            collider.size = localBounds.size + Vector3.one * padding;
            collider.isTrigger = false;

            var hitTarget = root.GetComponent<QuestPreviewHitTarget>();
            if (hitTarget == null)
                hitTarget = root.AddComponent<QuestPreviewHitTarget>();
            hitTarget.MatePreview = matePreview;
            hitTarget.TargetRoot = targetRoot != null ? targetRoot : root.transform;
            SetQuestOverlayLayer(root);
        }

        private static Bounds TransformWorldBoundsToLocal(Transform target, Bounds worldBounds)
        {
            var min = worldBounds.min;
            var max = worldBounds.max;
            var localBounds = new Bounds(target.InverseTransformPoint(min), Vector3.zero);
            EncapsulateLocalPoint(target, ref localBounds, new Vector3(min.x, min.y, max.z));
            EncapsulateLocalPoint(target, ref localBounds, new Vector3(min.x, max.y, min.z));
            EncapsulateLocalPoint(target, ref localBounds, new Vector3(min.x, max.y, max.z));
            EncapsulateLocalPoint(target, ref localBounds, new Vector3(max.x, min.y, min.z));
            EncapsulateLocalPoint(target, ref localBounds, new Vector3(max.x, min.y, max.z));
            EncapsulateLocalPoint(target, ref localBounds, new Vector3(max.x, max.y, min.z));
            EncapsulateLocalPoint(target, ref localBounds, max);
            return localBounds;
        }

        private static void ConfigureQuestPreviewObject(GameObject root)
        {
            if (root == null)
                return;

            SetQuestOverlayLayer(root);
            if (root.GetComponent<QuestPreviewCameraSuppressor>() == null)
                root.AddComponent<QuestPreviewCameraSuppressor>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                renderer.forceRenderingOff = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                if (renderer is SkinnedMeshRenderer skinned)
                    skinned.updateWhenOffscreen = true;

                var materials = renderer.sharedMaterials;
                for (var index = 0; index < materials.Length; index += 1)
                    if (materials[index] != null)
                        ConfigureAlwaysVisibleOverlayMaterial(materials[index]);
            }

            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                if (animator != null)
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (camera == null)
                    continue;

                camera.enabled = false;
                camera.targetTexture = null;
                var data = camera.GetUniversalAdditionalCameraData();
                data.allowXRRendering = false;
                data.allowHDROutput = false;
                data.renderPostProcessing = false;
            }

            var questOverlayMask = 1 << GetQuestOverlayLayer();
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                if (light != null)
                {
                    light.enabled = true;
                    light.cullingMask = questOverlayMask;
                }
        }

        private void LogQuestPreviewAttachment(string kind, Transform root, float targetScaleOrHeight)
        {
            if (root == null || Time.unscaledTime - lastQuestPreviewLog < 1.5f)
                return;

            lastQuestPreviewLog = Time.unscaledTime;
            Debug.LogFormat(
                "Quest immersive preview attached. Type={0}, Pos={1}, Rot={2}, LocalScale={3}, Target={4:F1}",
                kind,
                root.position,
                root.rotation.eulerAngles,
                root.localScale,
                targetScaleOrHeight);
        }

        private sealed class QuestPreviewCameraSuppressor : MonoBehaviour
        {
            private void LateUpdate()
            {
                foreach (var camera in GetComponentsInChildren<Camera>(true))
                {
                    if (camera == null)
                        continue;

                    camera.enabled = false;
                    camera.targetTexture = null;
                }
            }
        }

        private sealed class QuestPreviewHitTarget : MonoBehaviour
        {
            public bool MatePreview;
            public Transform TargetRoot;
        }

        private void AlignDuelWorldToGround(Transform duelContainer)
        {
            if (duelContainer == null || duelWorldAnchor == null)
                return;

            if (!TryGetDuelGroundBounds(duelContainer, out var localBounds, out var boundsSource))
            {
                if (Time.unscaledTime - lastDuelWorldBoundsLog > DuelWorldBoundsLogInterval)
                {
                    Debug.Log("Quest XR duel world ground-fit waiting for visible duel renderers.");
                    LogDuelRendererDiagnostics(duelContainer);
                    lastDuelWorldBoundsLog = Time.unscaledTime;
                }
                return;
            }

            var targetScale = ResolveDuelWorldScale(localBounds);
            duelWorldAnchor.SetPositionAndRotation(DuelWorldCenterOnGround, DuelWorldFloorRotation);
            duelWorldAnchor.localScale = Vector3.one * targetScale;
            duelContainer.localPosition = -new Vector3(localBounds.center.x, localBounds.min.y, localBounds.center.z);
            duelContainer.localRotation = Quaternion.identity;
            duelContainer.localScale = Vector3.one;

            var worldBounds = TransformBoundsToWorld(duelContainer, localBounds);
            var offset = DuelWorldCenterOnGround - new Vector3(worldBounds.center.x, worldBounds.min.y, worldBounds.center.z);
            if (offset.sqrMagnitude > DuelGroundSnapThreshold * DuelGroundSnapThreshold)
                duelWorldAnchor.position += offset;

            if (Time.unscaledTime - lastDuelWorldBoundsLog > DuelWorldBoundsLogInterval)
            {
                Debug.LogFormat(
                    "Quest XR duel world fixed to grid. Source={0}, LocalMin={1}, LocalMax={2}, Scale={3:F3}, WorldMin={4}, WorldMax={5}, Anchor={6}, ContainerLocal={7}",
                    boundsSource,
                    localBounds.min,
                    localBounds.max,
                    targetScale,
                    worldBounds.min,
                    worldBounds.max,
                    duelWorldAnchor.position,
                    duelContainer.localPosition);
                lastDuelWorldBoundsLog = Time.unscaledTime;
            }

            LockDuelWorldAlignment(duelContainer);
        }

        private bool HasLockedDuelWorldAlignment(Transform duelContainer)
        {
            return duelContainer != null && duelWorldAnchor != null && lockedDuelAlignmentContainer == duelContainer;
        }

        private void LockDuelWorldAlignment(Transform duelContainer)
        {
            if (duelContainer == null || duelWorldAnchor == null)
                return;

            lockedDuelAlignmentContainer = duelContainer;
            lockedDuelAnchorPosition = duelWorldAnchor.position;
            lockedDuelAnchorRotation = duelWorldAnchor.rotation;
            lockedDuelAnchorScale = duelWorldAnchor.localScale;
            lockedDuelContainerLocalPosition = duelContainer.localPosition;
            lockedDuelContainerLocalRotation = duelContainer.localRotation;
            lockedDuelContainerLocalScale = duelContainer.localScale;
            if (!questDuelWorldUserTransformInitialized)
            {
                questDuelWorldUserAnchorPosition = lockedDuelAnchorPosition;
                questDuelWorldUserTransformInitialized = true;
            }
            ApplyQuestDuelWorldUserTransform();
        }

        private void ApplyLockedDuelWorldAlignment(Transform duelContainer)
        {
            if (!HasLockedDuelWorldAlignment(duelContainer))
                return;

            duelWorldAnchor.SetPositionAndRotation(lockedDuelAnchorPosition, lockedDuelAnchorRotation);
            duelWorldAnchor.localScale = lockedDuelAnchorScale;
            duelContainer.localPosition = lockedDuelContainerLocalPosition;
            duelContainer.localRotation = lockedDuelContainerLocalRotation;
            duelContainer.localScale = lockedDuelContainerLocalScale;
            ApplyQuestDuelWorldUserTransform();
        }

        private void ApplyQuestDuelWorldUserTransform()
        {
            if (duelWorldAnchor == null || lockedDuelAlignmentContainer == null)
                return;

            var multiplier = Mathf.Max(questDuelWorldScaleMultiplier, QuestWorldScaleMinPositive);
            questDuelWorldScaleMultiplier = multiplier;
            if (!questDuelWorldUserTransformInitialized)
            {
                questDuelWorldUserAnchorPosition = lockedDuelAnchorPosition;
                questDuelWorldUserTransformInitialized = true;
            }

            duelWorldAnchor.SetPositionAndRotation(questDuelWorldUserAnchorPosition, lockedDuelAnchorRotation);
            duelWorldAnchor.localScale = lockedDuelAnchorScale * multiplier;
        }

        private bool TryGetDuelGroundBounds(Transform root, out Bounds bounds, out string source)
        {
            if (TryGetGameplayTableBounds(out bounds, out source))
                return true;

            if (TryFindDuelBaseTemplateBounds(root, out bounds, out source))
                return true;

            if (TryCollectDuelBounds(root, true, out bounds))
            {
                source = "field/table renderers";
                return true;
            }

            if (TryCollectDuelBounds(root, false, out bounds))
            {
                source = "visible duel renderers";
                return true;
            }

            source = string.Empty;
            bounds = default;
            return false;
        }

        private static bool TryGetGameplayTableBounds(out Bounds bounds, out string source)
        {
            bounds = new Bounds(
                new Vector3(0f, DuelGameplayTableHeight * 0.5f, 0f),
                new Vector3(DuelGameplayTableWidth, DuelGameplayTableHeight, DuelGameplayTableDepth));
            source = "fixed gameplay table layout";
            return true;
        }

        private bool TryFindDuelBaseTemplateBounds(Transform root, out Bounds bounds, out string source)
        {
            bounds = default;
            source = string.Empty;
            Renderer bestRenderer = null;
            var bestScore = float.NegativeInfinity;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!TryScoreDuelBaseTemplate(root, renderer, out var localBounds, out var score))
                    continue;

                if (score <= bestScore)
                    continue;

                bestRenderer = renderer;
                bestScore = score;
                bounds = localBounds;
            }

            if (bestRenderer == null)
                return false;

            source = "base template renderer: " + GetTransformPath(bestRenderer.transform);
            return true;
        }

        private bool TryScoreDuelBaseTemplate(Transform root, Renderer renderer, out Bounds localBounds, out float score)
        {
            localBounds = default;
            score = 0f;
            if (root == null || renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;

            if (renderer is ParticleSystemRenderer || renderer is SpriteRenderer || renderer is LineRenderer)
                return false;

            var transformPath = GetTransformPath(renderer.transform);
            if (string.IsNullOrEmpty(transformPath))
                return false;

            var path = transformPath.ToLowerInvariant();
            if (IsDuelRendererPathExcluded(path))
                return false;

            localBounds = ConvertWorldBoundsToLocal(root, renderer.bounds);
            var size = localBounds.size;
            var horizontalSize = Mathf.Max(size.x, size.z);
            var horizontalArea = Mathf.Max(size.x, 0f) * Mathf.Max(size.z, 0f);
            if (horizontalSize < DuelBaseTemplateMinHorizontalSize || horizontalArea < DuelBaseTemplateMinArea)
                return false;

            var relativeThickness = size.y / Mathf.Max(horizontalSize, 0.001f);
            if (relativeThickness > DuelBaseTemplateMaxRelativeThickness && !HasDuelBaseTemplateName(path))
                return false;

            score = horizontalArea * Mathf.Clamp(horizontalSize / Mathf.Max(size.y, 0.02f), 1f, 80f);
            if (HasDuelBaseTemplateName(path))
                score *= 3f;
            if (path.Contains("cube"))
                score *= 1.75f;
            if (path.Contains("field") || path.Contains("ground") || path.Contains("floor"))
                score *= 1.5f;
            return true;
        }

        private static bool HasDuelBaseTemplateName(string path)
        {
            return path.Contains("field") ||
                path.Contains("ground") ||
                path.Contains("floor") ||
                path.Contains("table") ||
                path.Contains("mat") ||
                path.Contains("stage") ||
                path.Contains("terrain") ||
                path.Contains("base") ||
                path.Contains("cube") ||
                path.Contains("duel");
        }

        private bool TryCollectDuelBounds(Transform root, bool stableOnly, out Bounds bounds)
        {
            bounds = default;
            var initialized = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                if (!IsDuelGroundBoundsCandidate(renderer, stableOnly))
                    continue;

                var rendererBounds = ConvertWorldBoundsToLocal(root, renderer.bounds);
                if (!initialized)
                {
                    bounds = rendererBounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            return initialized;
        }

        private static Bounds ConvertWorldBoundsToLocal(Transform root, Bounds worldBounds)
        {
            var min = worldBounds.min;
            var max = worldBounds.max;
            var localBounds = new Bounds(root.InverseTransformPoint(min), Vector3.zero);
            EncapsulateLocalPoint(root, ref localBounds, new Vector3(min.x, min.y, max.z));
            EncapsulateLocalPoint(root, ref localBounds, new Vector3(min.x, max.y, min.z));
            EncapsulateLocalPoint(root, ref localBounds, new Vector3(min.x, max.y, max.z));
            EncapsulateLocalPoint(root, ref localBounds, new Vector3(max.x, min.y, min.z));
            EncapsulateLocalPoint(root, ref localBounds, new Vector3(max.x, min.y, max.z));
            EncapsulateLocalPoint(root, ref localBounds, new Vector3(max.x, max.y, min.z));
            EncapsulateLocalPoint(root, ref localBounds, max);
            return localBounds;
        }

        private static void EncapsulateLocalPoint(Transform root, ref Bounds bounds, Vector3 worldPoint)
        {
            bounds.Encapsulate(root.InverseTransformPoint(worldPoint));
        }

        private static Bounds TransformBoundsToWorld(Transform transform, Bounds localBounds)
        {
            var min = localBounds.min;
            var max = localBounds.max;
            var worldBounds = new Bounds(transform.TransformPoint(min), Vector3.zero);
            EncapsulateWorldPoint(transform, ref worldBounds, new Vector3(min.x, min.y, max.z));
            EncapsulateWorldPoint(transform, ref worldBounds, new Vector3(min.x, max.y, min.z));
            EncapsulateWorldPoint(transform, ref worldBounds, new Vector3(min.x, max.y, max.z));
            EncapsulateWorldPoint(transform, ref worldBounds, new Vector3(max.x, min.y, min.z));
            EncapsulateWorldPoint(transform, ref worldBounds, new Vector3(max.x, min.y, max.z));
            EncapsulateWorldPoint(transform, ref worldBounds, new Vector3(max.x, max.y, min.z));
            EncapsulateWorldPoint(transform, ref worldBounds, max);
            return worldBounds;
        }

        private static void EncapsulateWorldPoint(Transform transform, ref Bounds bounds, Vector3 localPoint)
        {
            bounds.Encapsulate(transform.TransformPoint(localPoint));
        }

        private static float ResolveDuelWorldScale(Bounds localBounds)
        {
            var width = Mathf.Max(localBounds.size.x, 0.001f);
            var depth = Mathf.Max(localBounds.size.z, 0.001f);
            var scaleByWidth = DuelWorldTargetWidth / width;
            var scaleByDepth = DuelWorldTargetDepth / depth;
            return Mathf.Max(Mathf.Min(scaleByWidth, scaleByDepth), QuestWorldScaleMinPositive);
        }

        private bool IsDuelGroundBoundsCandidate(Renderer renderer, bool stableOnly)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;

            var transformPath = GetTransformPath(renderer.transform);
            if (string.IsNullOrEmpty(transformPath))
                return false;

            var path = transformPath.ToLowerInvariant();
            if (IsDuelRendererPathExcluded(path))
            {
                return false;
            }

            var extents = renderer.bounds.extents;
            if (extents.x > 180f || extents.y > 180f || extents.z > 180f)
                return false;

            if (!stableOnly)
                return true;

            return path.Contains("field") ||
                path.Contains("mat") ||
                path.Contains("table") ||
                path.Contains("board") ||
                path.Contains("floor") ||
                path.Contains("ground");
        }

        private static bool IsDuelRendererPathExcluded(string path)
        {
            return IsOptionalDuelSceneryPath(path) ||
                path.Contains("canvas") ||
                path.Contains("ui") ||
                path.Contains("camera") ||
                path.Contains("background") ||
                path.Contains("backdrop") ||
                path.Contains("sky") ||
                path.Contains("sphere") ||
                path.Contains("greenbackground") ||
                path.Contains("closeup") ||
                path.Contains("fieldmonsterglb") ||
                path.Contains("summon") ||
                path.Contains("effect") ||
                path.Contains("fxp_") ||
                path.Contains("highlight") ||
                path.Contains("selector");
        }

        private void HideOptionalDuelScenery(Transform duelContainer)
        {
            if (duelContainer == null)
                return;

            if (optionalSceneryFilteredContainer != duelContainer)
            {
                optionalSceneryFilteredContainer = duelContainer;
                hiddenOptionalDuelSceneryRenderers = 0;
                hiddenOptionalDuelSceneryColliders = 0;
                optionalDuelSceneryLogged = false;
            }

            foreach (var renderer in duelContainer.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                var path = GetTransformPath(renderer.transform).ToLowerInvariant();
                if (!IsOptionalDuelSceneryPath(path))
                    continue;

                var root = FindOptionalDuelSceneryRoot(duelContainer, renderer.transform);
                HideOptionalDuelSceneryRoot(root);
            }

            if (!optionalDuelSceneryLogged && hiddenOptionalDuelSceneryRenderers > 0)
            {
                Debug.LogFormat(
                    "Quest XR hidden optional duel scenery. Renderers={0}, Colliders={1}",
                    hiddenOptionalDuelSceneryRenderers,
                    hiddenOptionalDuelSceneryColliders);
                optionalDuelSceneryLogged = true;
            }
        }

        private void HideOptionalDuelSceneryRoot(Transform root)
        {
            if (root == null)
                return;

            if (root.gameObject.activeSelf)
                root.gameObject.SetActive(false);

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                renderer.enabled = false;
                hiddenOptionalDuelSceneryRenderers += 1;
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || !collider.enabled)
                    continue;

                collider.enabled = false;
                hiddenOptionalDuelSceneryColliders += 1;
            }
        }

        private static Transform FindOptionalDuelSceneryRoot(Transform duelContainer, Transform transform)
        {
            if (transform == null)
                return null;

            var current = transform;
            Transform best = null;
            while (current != null && current != duelContainer)
            {
                var name = current.name.ToLowerInvariant();
                if (IsOptionalDuelSceneryObjectName(name))
                    best = current;
                current = current.parent;
            }

            return best ?? transform;
        }

        private static bool IsOptionalDuelSceneryObjectName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.Contains("outside") ||
                name.Contains("scenery") ||
                name.Contains("landscape") ||
                name.Contains("tree") ||
                name.Contains("leaf") ||
                name.Contains("foliage") ||
                name.Contains("grass") ||
                name.Contains("bush") ||
                name.Contains("shrub") ||
                name.Contains("flower") ||
                name.Contains("rabbit") ||
                name.Contains("bunny") ||
                name.Contains("usagi") ||
                IsOptionalMateSceneryToken(name) ||
                name.Contains("stand") ||
                name.Contains("mascot") ||
                name.Contains("prop") ||
                name.Contains("rock") ||
                name.Contains("stone") ||
                name.Contains("cliff") ||
                name.Contains("mountain") ||
                name.Contains("cloud") ||
                name.Contains("sky");
        }

        private static bool IsOptionalDuelSceneryPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var sceneryRegion = path.Contains("/outside/") ||
                path.Contains("outside") ||
                path.Contains("scenery") ||
                path.Contains("landscape");

            return sceneryRegion ||
                path.Contains("tree") ||
                path.Contains("leaf") ||
                path.Contains("foliage") ||
                path.Contains("grass") ||
                path.Contains("bush") ||
                path.Contains("shrub") ||
                path.Contains("flower") ||
                path.Contains("rabbit") ||
                path.Contains("bunny") ||
                path.Contains("usagi") ||
                IsOptionalMateSceneryToken(path) ||
                path.Contains("stand") ||
                path.Contains("mascot") ||
                path.Contains("prop") ||
                path.Contains("rock") ||
                path.Contains("stone") ||
                path.Contains("cliff") ||
                path.Contains("mountain") ||
                path.Contains("cloud") ||
                path.Contains("sky") ||
                path.Contains("leafshadow") ||
                path.Contains("treeshadow");
        }

        private static bool IsOptionalMateSceneryToken(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.Contains("/mate/") ||
                value.Contains("\\mate\\") ||
                value.Contains("duelmate") ||
                value.Contains("mate_") ||
                value.Contains("_mate") ||
                value.EndsWith("/mate") ||
                value.EndsWith("\\mate");
        }

        private void HideOriginalDuelFieldVisuals(Transform duelContainer)
        {
            if (duelContainer == null)
                return;

            if (originalFieldHiddenContainer != duelContainer)
            {
                originalFieldHiddenContainer = duelContainer;
                hiddenOriginalDuelFieldRenderers = 0;
                originalDuelFieldLogged = false;
            }

            foreach (var renderer in duelContainer.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                var path = GetTransformPath(renderer.transform).ToLowerInvariant();
                if (!IsOriginalDuelFieldVisualPath(path))
                    continue;

                renderer.enabled = false;
                hiddenOriginalDuelFieldRenderers += 1;
            }

            if (!originalDuelFieldLogged && hiddenOriginalDuelFieldRenderers > 0)
            {
                Debug.LogFormat(
                    "Quest XR hidden original duel field visuals. Renderers={0}",
                    hiddenOriginalDuelFieldRenderers);
                originalDuelFieldLogged = true;
            }
        }

        private static bool IsOriginalDuelFieldVisualPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.Contains("/duelfield/"))
                return false;

            return !path.Contains("card") &&
                !path.Contains("deck") &&
                !path.Contains("monster") &&
                !path.Contains("selector") &&
                !path.Contains("place") &&
                !path.Contains("ui") &&
                !path.Contains("lp") &&
                !path.Contains("name") &&
                !path.Contains("timer");
        }

        private void SuppressLegacyDuelPresentationResidues(Transform duelContainer)
        {
            if (duelContainer == null)
                return;

            if (legacyResidueContainer != duelContainer)
            {
                legacyResidueContainer = duelContainer;
                hiddenLegacyResidueRenderers = 0;
                legacyResidueLogged = false;
            }

            SuppressFaceUpCardBackResidues();

            foreach (var renderer in duelContainer.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var path = GetTransformPath(renderer.transform).ToLowerInvariant();
                if (!IsLegacyDuelPresentationResiduePath(path, renderer))
                    continue;

                if (!renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = true;
                    hiddenLegacyResidueRenderers += 1;
                }
            }

            if (!legacyResidueLogged && hiddenLegacyResidueRenderers > 0)
            {
                Debug.LogFormat(
                    "Quest XR suppressed legacy duel presentation residues. Renderers={0}",
                    hiddenLegacyResidueRenderers);
                legacyResidueLogged = true;
            }
        }

        private void SuppressFaceUpCardBackResidues()
        {
            var cards = Program.instance?.ocgcore?.cards;
            if (cards == null)
                return;

            foreach (var card in cards)
            {
                if (card == null || card.model == null || card.manager == null || card.p == null)
                    continue;

                var cardModel = card.manager.GetElement<Transform>("CardModel");
                if (cardModel == null || cardModel.childCount < 2)
                    continue;

                var backRenderer = cardModel.GetChild(0).GetComponent<Renderer>();
                var faceRenderer = cardModel.GetChild(1).GetComponent<Renderer>();
                if (backRenderer == null || faceRenderer == null)
                    continue;

                var isFaceUp = (card.p.position & (uint)CardPosition.FaceUp) > 0;
                var shouldSuppressBack = isFaceUp
                    && faceRenderer.enabled
                    && faceRenderer.gameObject.activeInHierarchy
                    && faceRenderer.sharedMaterial != null;

                if (backRenderer.forceRenderingOff != shouldSuppressBack)
                {
                    backRenderer.forceRenderingOff = shouldSuppressBack;
                    if (shouldSuppressBack)
                        hiddenLegacyResidueRenderers += 1;
                }
            }
        }

        private static bool IsLegacyDuelPresentationResiduePath(string path, Renderer renderer)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (path.Contains("duelbutton") ||
                path.Contains("duel_button") ||
                path.Contains("buttonicon") ||
                path.Contains("dummycardmodel_back"))
                return true;

            if ((path.Contains("fallbackdeckcard") || path.Contains("cardmodel_back"))
                && path.Contains("fallback"))
                return true;

            var materialName = renderer == null || renderer.sharedMaterial == null
                ? string.Empty
                : renderer.sharedMaterial.name.ToLowerInvariant();
            if ((path.Contains("fallback") || path.Contains("dummy"))
                && (materialName.Contains("fallbackprotector") ||
                    materialName.Contains("protectorfallback")))
                return true;

            return false;
        }

        private void LogDuelRendererDiagnostics(Transform root)
        {
            if (root == null || Time.unscaledTime - lastDuelRendererDiagnosticLog < DuelWorldBoundsLogInterval)
                return;

            lastDuelRendererDiagnosticLog = Time.unscaledTime;
            var builder = new System.Text.StringBuilder();
            var count = 0;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                var path = GetTransformPath(renderer.transform);
                var localBounds = ConvertWorldBoundsToLocal(root, renderer.bounds);
                if (builder.Length > 0)
                    builder.Append(" | ");
                builder.Append(path);
                builder.Append(" localMin=");
                builder.Append(localBounds.min);
                builder.Append(" localMax=");
                builder.Append(localBounds.max);
                builder.Append(" layer=");
                builder.Append(LayerMask.LayerToName(renderer.gameObject.layer));
                count += 1;
                if (count >= 20)
                    break;
            }

            Debug.LogFormat(
                "Quest XR duel renderer diagnostics. CountLogged={0}, Renderers={1}",
                count,
                builder.Length == 0 ? "<none>" : builder.ToString());
        }

        private void SuppressQuestBlackOverlays()
        {
            var cameraManager = Program.instance?.camera_;
            if (cameraManager != null)
            {
                MakeSpriteTransparent(cameraManager.black);
            }

            var uiManager = Program.instance?.ui_;
            if (uiManager != null)
            {
                if (IsQuestNativeDuelActive())
                    HideCanvasGroup(uiManager.wallpaper);
                MakeImageTransparent(uiManager.blackBack);
                HideTransitionOverlay(uiManager.transition);
            }
        }

        private static void MakeCameraTransparent(Camera camera)
        {
            if (camera == null)
                return;

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.allowHDR = false;

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.allowHDROutput = false;
            data.requiresColorOption = CameraOverrideOption.Off;
        }

        private static void HideCanvasGroup(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            if (canvasGroup.gameObject.activeSelf)
                canvasGroup.gameObject.SetActive(false);
        }

        private static void MakeImageTransparent(Image image)
        {
            if (image == null)
                return;

            var color = image.color;
            color.a = 0f;
            image.color = color;
            image.raycastTarget = false;
        }

        private static void HideTransitionOverlay(RectTransform transition)
        {
            if (transition == null)
                return;

            transition.sizeDelta = Vector2.zero;
            foreach (var graphic in transition.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic == null)
                    continue;

                var color = graphic.color;
                color.a = 0f;
                graphic.color = color;
                graphic.raycastTarget = false;
            }

            foreach (var renderer in transition.GetComponentsInChildren<CanvasRenderer>(true))
            {
                if (renderer != null)
                    renderer.SetAlpha(0f);
            }
        }

        private static void MakeSpriteTransparent(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null)
                return;

            var color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }

        private void SuppressQuestDecorativeUiBackgrounds()
        {
            var preserved = 0;
            foreach (var image in FindObjectsOfType<RawImage>(true))
            {
                if (image == null || image.gameObject == null)
                    continue;

                if (!image.gameObject.name.StartsWith("QuestDecorativeUiBackground", StringComparison.Ordinal))
                    continue;

                image.raycastTarget = false;
                image.transform.SetAsFirstSibling();
                preserved += 1;
            }

            if (worldCanvasDecorations.Count > 0)
            {
                foreach (var pair in worldCanvasDecorations)
                {
                    if (pair.Value != null)
                    {
                        pair.Value.raycastTarget = false;
                        pair.Value.transform.SetAsFirstSibling();
                        preserved += 1;
                    }
                }
            }

            if (preserved > 0 || Time.unscaledTime - lastQuestDecorativeUiCleanupLog > 10f)
            {
                lastQuestDecorativeUiCleanupLog = Time.unscaledTime;
                Debug.LogFormat("Quest XR decorative UI backgrounds preserved: count={0}", preserved);
            }
        }

        private bool EnsureUiRenderTexture()
        {
            var width = UiRenderTextureFixedWidth;
            var height = UiRenderTextureFixedHeight;
            if (uiRenderTexture != null
                && uiRenderTextureWidth == width
                && uiRenderTextureHeight == height
                && uiRenderTexture.IsCreated())
                return false;

            if (uiRenderTexture != null)
            {
                uiRenderTexture.Release();
                Destroy(uiRenderTexture);
            }
            uiRenderTextureWidth = width;
            uiRenderTextureHeight = height;
            uiRenderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32)
            {
                name = "Quest UI Render Texture",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1,
                wrapMode = TextureWrapMode.Clamp
            };
            uiRenderTexture.Create();

            if (uiRenderPanelMaterial != null)
                SetMaterialTexture(uiRenderPanelMaterial, uiRenderTexture);
            return true;
        }

        private void ConfigureUiRenderPanel()
        {
            if (xrCamera == null || Program.instance?.camera_ == null)
                return;

            if (QuestUseNativeMainMenu
                || QuestUseWorldSpaceMdproUi
                || (QuestNativeDuelFrontendOnly && IsQuestNativeDuelActive()))
            {
                HideUiRenderPanel();
                return;
            }

            EnsureWorldUiAnchor();
            var renderTextureRecreated = EnsureUiRenderTexture();
            EnsureUiRenderPanel();
            if (renderTextureRecreated)
                ConfigureMdproCameras(true);

            if (uiRenderPanel == null)
                return;

            if (!uiRenderPanel.activeSelf)
                uiRenderPanel.SetActive(true);
            RepairUiRenderPanelRenderer();

            var panelRotation = worldUiAnchor == null ? GetDuelBaseYawRotation() : worldUiAnchor.rotation;
            var anchorPosition = worldUiAnchor == null
                ? DuelWorldCenterOnGround + GetDuelBaseYawRotation() * Vector3.forward * UiRenderPanelForwardOffset + Vector3.up * UiRenderPanelHeightOffset
                : worldUiAnchor.position + panelRotation * Vector3.right * UiRenderPanelSideOffset;
            var aspect = uiRenderTextureHeight <= 0 ? 16f / 9f : (float)uiRenderTextureWidth / uiRenderTextureHeight;
            var width = UiRenderPanelWidth;
            var height = width / Mathf.Max(aspect, 0.01f);

            uiRenderPanel.transform.SetPositionAndRotation(anchorPosition, panelRotation);
            uiRenderPanel.transform.localScale = new Vector3(width, height, 1f);
            LogUiRenderPanelDiagnostics(anchorPosition, panelRotation, width, height);

            if (!uiRenderPanelLogged)
            {
                Debug.LogFormat("Quest XR UI render panel enabled. Texture={0}x{1}, Panel={2:F1}x{3:F1}, HorizontalFlip={4}, VerticalFlip={5}",
                    uiRenderTextureWidth,
                    uiRenderTextureHeight,
                    width,
                    height,
                    UiRenderTextureNeedsHorizontalFlip,
                    UiRenderTextureNeedsVerticalFlip);
                uiRenderPanelLogged = true;
            }
        }

        private void LogUiRenderPanelDiagnostics(Vector3 anchorPosition, Quaternion panelRotation, float width, float height)
        {
            if (!QuestVerboseRuntimeDiagnostics)
                return;

            if (xrCamera == null || Time.unscaledTime - lastUiRenderPanelDiagnosticsLog < 5f)
                return;

            lastUiRenderPanelDiagnosticsLog = Time.unscaledTime;
            var center = xrCamera.WorldToViewportPoint(anchorPosition);
            var right = xrCamera.WorldToViewportPoint(anchorPosition + panelRotation * Vector3.right * (width * 0.5f));
            var left = xrCamera.WorldToViewportPoint(anchorPosition - panelRotation * Vector3.right * (width * 0.5f));
            var top = xrCamera.WorldToViewportPoint(anchorPosition + panelRotation * Vector3.up * (height * 0.5f));
            var bottom = xrCamera.WorldToViewportPoint(anchorPosition - panelRotation * Vector3.up * (height * 0.5f));
            var cameraManager = Program.instance?.camera_;
            var uiCamera = cameraManager == null ? null : cameraManager.cameraUI;
            var activeCanvasCount = 0;
            var visibleGraphicCount = 0;
            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || !canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy)
                    continue;

                activeCanvasCount += 1;
                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(false))
                {
                    if (graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy && graphic.color.a > 0.01f)
                        visibleGraphicCount += 1;
                }
            }

            Debug.LogFormat(
                "Quest XR UI panel diagnostics. Pos={0}, Rot={1}, Size={2:F1}x{3:F1}, ViewportCenter={4}, L/R/T/B=({5},{6},{7},{8}), RT={9}x{10}/{11}, UICamera={12}, UICameraEnabled={13}, UICameraTarget={14}, UICameraDepth={15:F1}, Camera2DDepth={16:F1}, ActiveRootCanvases={17}, VisibleGraphics={18}",
                anchorPosition,
                panelRotation.eulerAngles,
                width,
                height,
                center,
                left,
                right,
                top,
                bottom,
                uiRenderTextureWidth,
                uiRenderTextureHeight,
                uiRenderTexture != null && uiRenderTexture.IsCreated(),
                uiCamera == null ? "<null>" : GetTransformPath(uiCamera.transform),
                uiCamera != null && uiCamera.enabled && uiCamera.gameObject.activeInHierarchy,
                uiCamera == null || uiCamera.targetTexture == null ? "<null>" : uiCamera.targetTexture.name,
                uiCamera == null ? 0f : uiCamera.depth,
                cameraManager == null || cameraManager.camera2D == null ? 0f : cameraManager.camera2D.depth,
                activeCanvasCount,
                visibleGraphicCount);
        }

        private void LogQuestMainMenuUiRecovery(string reason, int restoredCanvasCount)
        {
            var program = Program.instance;
            var cameraManager = program == null ? null : program.camera_;
            var uiCamera = cameraManager == null ? null : cameraManager.cameraUI;
            var core = program == null ? null : program.ocgcore;
            var activeRootCanvasCount = 0;
            var enabledRootCanvasCount = 0;
            var enabledRaycasterCount = 0;
            var visibleGraphicCount = 0;

            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || !canvas.isRootCanvas || !canvas.gameObject.activeInHierarchy)
                    continue;

                activeRootCanvasCount += 1;
                if (canvas.enabled)
                    enabledRootCanvasCount += 1;
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null && raycaster.enabled)
                    enabledRaycasterCount += 1;

                foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(false))
                {
                    if (graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy && graphic.color.a > 0.01f)
                        visibleGraphicCount += 1;
                }
            }

            Debug.LogFormat(
                "Quest main menu UI recovery. Reason={0}, RestoredLegacyCanvases={1}, SuppressedUntil={2:F2}, CurrentServant={3}, MenuShowing={4}, CoreShowing={5}, CoreMessage={6}, CoreCards={7}, PanelActive={8}, PanelRenderer={9}, PanelMaterial={10}, PanelTexture={11}, RT={12}x{13}/{14}, UICamera={15}, UICameraEnabled={16}, UICameraTarget={17}, UICameraDepth={18:F1}, Camera2DDepth={19:F1}, ActiveRootCanvases={20}, EnabledRootCanvases={21}, EnabledRaycasters={22}, VisibleGraphics={23}, XrMask=0x{24:X8}",
                reason,
                restoredCanvasCount,
                questNativeDuelFrontendSuppressedUntil,
                program == null || program.currentServant == null ? "<null>" : program.currentServant.name,
                program != null && program.menu != null && program.menu.showing,
                core != null && core.showing,
                core == null ? "<null>" : core.currentMessage.ToString(),
                core == null || core.cards == null ? -1 : core.cards.Count,
                uiRenderPanel != null && uiRenderPanel.activeInHierarchy,
                uiRenderPanelRenderer != null && uiRenderPanelRenderer.enabled,
                uiRenderPanelMaterial != null,
                uiRenderPanelMaterial == null || uiRenderPanelMaterial.mainTexture == null ? "<null>" : uiRenderPanelMaterial.mainTexture.name,
                uiRenderTextureWidth,
                uiRenderTextureHeight,
                uiRenderTexture != null && uiRenderTexture.IsCreated(),
                uiCamera == null ? "<null>" : GetTransformPath(uiCamera.transform),
                uiCamera != null && uiCamera.enabled && uiCamera.gameObject.activeInHierarchy,
                uiCamera == null || uiCamera.targetTexture == null ? "<null>" : uiCamera.targetTexture.name,
                uiCamera == null ? 0f : uiCamera.depth,
                cameraManager == null || cameraManager.camera2D == null ? 0f : cameraManager.camera2D.depth,
                activeRootCanvasCount,
                enabledRootCanvasCount,
                enabledRaycasterCount,
                visibleGraphicCount,
                xrCamera == null ? 0 : xrCamera.cullingMask);
        }

        private void EnsureUiRenderPanel()
        {
            if (uiRenderPanel != null)
            {
                RepairUiRenderPanelRenderer();
                return;
            }

            uiRenderPanelMaterial = CreateColorMaterial(
                "Quest UI Render Panel Material",
                Color.white,
                false);
            SetMaterialTexture(uiRenderPanelMaterial, uiRenderTexture);
            ConfigureUiRenderPanelMaterial(uiRenderPanelMaterial);

            uiRenderPanel = new GameObject("QuestUiRenderPanel");
            uiRenderPanel.AddComponent<MeshFilter>().sharedMesh = CreateUiRenderPanelMesh();
            uiRenderPanel.AddComponent<MeshRenderer>();
            uiRenderPanel.name = "QuestUiRenderPanel";
            SetQuestOverlayLayer(uiRenderPanel);

            uiRenderPanelRenderer = uiRenderPanel.GetComponent<MeshRenderer>();
            if (uiRenderPanelRenderer != null)
            {
                uiRenderPanelRenderer.sharedMaterial = uiRenderPanelMaterial;
                uiRenderPanelRenderer.shadowCastingMode = ShadowCastingMode.Off;
                uiRenderPanelRenderer.receiveShadows = false;
            }
            EnsureUiRenderPanelBackdrop();
        }

        private void RepairUiRenderPanelRenderer()
        {
            if (uiRenderPanel == null)
                return;

            if (uiRenderPanelRenderer == null)
                uiRenderPanelRenderer = uiRenderPanel.GetComponent<MeshRenderer>();
            if (uiRenderPanelRenderer == null)
                return;

            if (uiRenderPanelMaterial == null)
            {
                uiRenderPanelMaterial = CreateColorMaterial(
                    "Quest UI Render Panel Material",
                    Color.white,
                    false);
            }

            SetMaterialTexture(uiRenderPanelMaterial, uiRenderTexture);
            ConfigureUiRenderPanelMaterial(uiRenderPanelMaterial);
            if (uiRenderPanelRenderer.sharedMaterial != uiRenderPanelMaterial)
                uiRenderPanelRenderer.sharedMaterial = uiRenderPanelMaterial;
            if (!uiRenderPanelRenderer.enabled)
                uiRenderPanelRenderer.enabled = true;
            uiRenderPanelRenderer.shadowCastingMode = ShadowCastingMode.Off;
            uiRenderPanelRenderer.receiveShadows = false;
            EnsureUiRenderPanelBackdrop();
            RepairUiRenderPanelBackdrop();
        }

        private void EnsureUiRenderPanelBackdrop()
        {
            if (uiRenderPanel == null || uiRenderPanelBackdrop != null)
                return;

            if (uiRenderPanelBackdropMaterial == null)
            {
                uiRenderPanelBackdropMaterial = CreateColorMaterial(
                    "Quest UI Render Panel Backdrop Material",
                    new Color(0.006f, 0.010f, 0.014f, 1f),
                    false);
                ConfigureUiRenderPanelBackdropMaterial(uiRenderPanelBackdropMaterial);
            }

            uiRenderPanelBackdrop = new GameObject("QuestUiRenderPanelBackdrop");
            uiRenderPanelBackdrop.transform.SetParent(uiRenderPanel.transform, false);
            uiRenderPanelBackdrop.transform.localPosition = new Vector3(0f, 0f, 0.018f);
            uiRenderPanelBackdrop.transform.localRotation = Quaternion.identity;
            uiRenderPanelBackdrop.transform.localScale = new Vector3(1.025f, 1.025f, 1f);
            uiRenderPanelBackdrop.AddComponent<MeshFilter>().sharedMesh = CreateUiRenderPanelMesh();
            uiRenderPanelBackdrop.AddComponent<MeshRenderer>();
            SetQuestOverlayLayer(uiRenderPanelBackdrop);

            uiRenderPanelBackdropRenderer = uiRenderPanelBackdrop.GetComponent<MeshRenderer>();
            RepairUiRenderPanelBackdrop();
        }

        private void RepairUiRenderPanelBackdrop()
        {
            if (uiRenderPanelBackdrop == null)
                return;

            if (uiRenderPanelBackdropRenderer == null)
                uiRenderPanelBackdropRenderer = uiRenderPanelBackdrop.GetComponent<MeshRenderer>();
            if (uiRenderPanelBackdropRenderer == null)
                return;

            if (uiRenderPanelBackdropMaterial == null)
            {
                uiRenderPanelBackdropMaterial = CreateColorMaterial(
                    "Quest UI Render Panel Backdrop Material",
                    new Color(0.006f, 0.010f, 0.014f, 1f),
                    false);
            }

            ConfigureUiRenderPanelBackdropMaterial(uiRenderPanelBackdropMaterial);
            if (uiRenderPanelBackdropRenderer.sharedMaterial != uiRenderPanelBackdropMaterial)
                uiRenderPanelBackdropRenderer.sharedMaterial = uiRenderPanelBackdropMaterial;
            if (!uiRenderPanelBackdropRenderer.enabled)
                uiRenderPanelBackdropRenderer.enabled = true;
            uiRenderPanelBackdropRenderer.shadowCastingMode = ShadowCastingMode.Off;
            uiRenderPanelBackdropRenderer.receiveShadows = false;
        }

        private static Mesh CreateUiRenderPanelMesh()
        {
            var mesh = new Mesh { name = "QuestUiRenderPanelMesh" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            var leftU = UiRenderTextureNeedsHorizontalFlip ? 1f : 0f;
            var rightU = UiRenderTextureNeedsHorizontalFlip ? 0f : 1f;
            var bottomV = UiRenderTextureNeedsVerticalFlip ? 1f : 0f;
            var topV = UiRenderTextureNeedsVerticalFlip ? 0f : 1f;
            mesh.uv = new[]
            {
                new Vector2(leftU, bottomV),
                new Vector2(rightU, bottomV),
                new Vector2(rightU, topV),
                new Vector2(leftU, topV)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private bool HasVisibleUiRenderCamera()
        {
            var cameraManager = Program.instance?.camera_;
            if (cameraManager == null)
                return false;

            foreach (var camera in GetMdproCameras())
            {
                if (!IsUiRenderCamera(camera, cameraManager))
                    continue;
                if (camera != null && camera.gameObject.activeInHierarchy && camera.enabled)
                    return true;
            }

            return false;
        }

        private static void SetMaterialTexture(Material material, Texture texture)
        {
            if (material == null)
                return;

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            ApplyUiRenderPanelTextureTransform(material);
        }

        private static void ApplyUiRenderPanelTextureTransform(Material material)
        {
            if (material == null)
                return;

            var scale = Vector2.one;
            var offset = Vector2.zero;

            material.mainTextureScale = scale;
            material.mainTextureOffset = offset;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTextureScale("_BaseMap", scale);
                material.SetTextureOffset("_BaseMap", offset);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
            }
        }

        private void ClearQuestRenderTexturesBeforeCameraRender()
        {
            ClearRenderTexture(uiRenderTexture, Color.clear);
        }

        private static void ClearRenderTexture(RenderTexture renderTexture, Color clearColor)
        {
            if (renderTexture == null || !renderTexture.IsCreated())
                return;

            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, clearColor);
            RenderTexture.active = previous;
        }

        private void ConfigureOverlayCanvases()
        {
            if (xrCamera == null)
                return;

            CleanupWorldCanvases();
            if (QuestNativeDuelFrontendOnly && IsQuestNativeDuelActive())
            {
                SuppressLegacyDuelCanvases();
                foreach (var panel in worldCanvasPanels.Values)
                {
                    if (panel != null && panel.activeSelf)
                        panel.SetActive(false);
                }
                configuredWorldCanvases.Clear();
                worldCanvasSlots.Clear();

                foreach (var canvas in FindObjectsOfType<Canvas>(true))
                {
                    if (canvas == null || !canvas.isRootCanvas)
                        continue;
                    if (IsOffscreenCardRenderCanvas(canvas))
                        ConfigureOffscreenCardRenderCanvas(canvas);
                    else if (IsQuestNativeCanvas(canvas) && canvas.worldCamera == null)
                        canvas.worldCamera = xrCamera;
                }

                HideUiRenderPanel();
                return;
            }

            RestoreLegacyDuelCanvases();
            if (QuestUseNativeMainMenu)
            {
                foreach (var panel in worldCanvasPanels.Values)
                {
                    if (panel != null && panel.activeSelf)
                        panel.SetActive(false);
                }
                configuredWorldCanvases.Clear();
                worldCanvasSlots.Clear();

                foreach (var canvas in FindObjectsOfType<Canvas>(true))
                {
                    if (canvas == null || !canvas.isRootCanvas)
                        continue;
                    if (IsOffscreenCardRenderCanvas(canvas))
                    {
                        ConfigureOffscreenCardRenderCanvas(canvas);
                        continue;
                    }
                    if (IsQuestNativeCanvas(canvas) && canvas.worldCamera != xrCamera)
                        canvas.worldCamera = xrCamera;
                }

                HideUiRenderPanel();
                if (!worldUiLogged)
                {
                    Debug.Log("Quest XR native world main menu is used; legacy UI RenderTexture panel is disabled.");
                    worldUiLogged = true;
                }
                return;
            }

            if (QuestUseWorldSpaceMdproUi)
            {
                foreach (var panel in worldCanvasPanels.Values)
                {
                    if (panel != null && panel.activeSelf)
                        panel.SetActive(false);
                }
                configuredWorldCanvases.Clear();
                worldCanvasSlots.Clear();

                foreach (var canvas in FindObjectsOfType<Canvas>(true))
                {
                    if (canvas == null || !canvas.isRootCanvas)
                        continue;
                    if (IsQuestNativeCanvas(canvas))
                        continue;
                    if (IsOffscreenCardRenderCanvas(canvas))
                    {
                        ConfigureOffscreenCardRenderCanvas(canvas);
                        continue;
                    }

                    ConfigureWorldCanvas(canvas);
                    configuredWorldCanvases.Add(canvas);
                }

                HideUiRenderPanel();
                if (!worldUiLogged)
                {
                    Debug.Log("Quest XR MDPro UI canvases are placed directly in world space.");
                    worldUiLogged = true;
                }
                return;
            }

            foreach (var panel in worldCanvasPanels.Values)
            {
                if (panel != null && panel.activeSelf)
                    panel.SetActive(false);
            }
            configuredWorldCanvases.Clear();
            worldCanvasSlots.Clear();
            CleanupWorldCanvases();

            var uiCamera = Program.instance?.camera_?.cameraUI;
            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null)
                    continue;

                if (!canvas.isRootCanvas)
                    continue;
                if (IsQuestNativeCanvas(canvas))
                    continue;
                if (IsOffscreenCardRenderCanvas(canvas))
                {
                    ConfigureOffscreenCardRenderCanvas(canvas);
                    continue;
                }

                ConfigureCanvasForUiRenderPanel(canvas, uiCamera);
            }

            if (!worldUiLogged)
            {
                Debug.Log("Quest XR screen-space UI canvases are rendered to the fixed world panel.");
                worldUiLogged = true;
            }
        }

        private void SuppressLegacyDuelCanvases()
        {
            var stale = new List<Canvas>();
            foreach (var pair in suppressedLegacyDuelCanvases)
                if (pair.Key == null)
                    stale.Add(pair.Key);
            foreach (var canvas in stale)
                suppressedLegacyDuelCanvases.Remove(canvas);

            foreach (var canvas in FindObjectsOfType<Canvas>(true))
            {
                if (canvas == null || !canvas.isRootCanvas)
                    continue;
                if (IsQuestNativeCanvas(canvas) || IsOffscreenCardRenderCanvas(canvas))
                    continue;
                if (!ShouldSuppressLegacyDuelCanvas(canvas))
                    continue;

                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (!suppressedLegacyDuelCanvases.ContainsKey(canvas))
                {
                    suppressedLegacyDuelCanvases[canvas] = new LegacyCanvasState
                    {
                        canvasEnabled = canvas.enabled,
                        hadRaycaster = raycaster != null,
                        raycasterEnabled = raycaster != null && raycaster.enabled
                    };
                }

                if (canvas.enabled)
                    canvas.enabled = false;
                if (raycaster != null && raycaster.enabled)
                    raycaster.enabled = false;
                if (worldCanvasPanels.TryGetValue(canvas, out var panel) && panel != null && panel.activeSelf)
                    panel.SetActive(false);
            }
        }

        private int RestoreLegacyDuelCanvases()
        {
            if (suppressedLegacyDuelCanvases.Count == 0)
                return 0;

            var restored = new List<Canvas>();
            var restoredCount = 0;
            foreach (var pair in suppressedLegacyDuelCanvases)
            {
                var canvas = pair.Key;
                if (canvas == null)
                {
                    restored.Add(canvas);
                    continue;
                }

                canvas.enabled = pair.Value.canvasEnabled;
                if (pair.Value.hadRaycaster)
                {
                    var raycaster = canvas.GetComponent<GraphicRaycaster>();
                    if (raycaster != null)
                        raycaster.enabled = pair.Value.raycasterEnabled;
                }
                restored.Add(canvas);
                restoredCount += 1;
            }

            foreach (var canvas in restored)
                suppressedLegacyDuelCanvases.Remove(canvas);
            return restoredCount;
        }

        private static bool ShouldSuppressLegacyDuelCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;
            if (!IsQuestNativeDuelActive())
                return false;

            var path = GetTransformPath(canvas.transform).ToLowerInvariant();
            if (path.Contains("cardrenderer") || path.Contains("card render"))
                return false;

            if (path.Contains("popup")
                || path.Contains("sidepanel")
                || path.Contains("side panel")
                || path.Contains("chatpanel")
                || path.Contains("cardlist")
                || path.Contains("card list")
                || path.Contains("carddetail")
                || path.Contains("card detail")
                || path.Contains("duelbutton")
                || path.Contains("duel button")
                || path.Contains("phase")
                || path.Contains("description")
                || path.Contains("log"))
                return true;

            var ui = Program.instance == null ? null : Program.instance.ui_;
            if (ui != null && canvas.transform.IsChildOf(ui.transform))
                return true;

            return Program.instance != null && Program.instance.currentServant == Program.instance.ocgcore;
        }

        private void ConfigureCanvasForUiRenderPanel(Canvas canvas, Camera uiCamera)
        {
            if (canvas == null)
                return;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                canvas.renderMode = RenderMode.ScreenSpaceCamera;

            if (!IsQuestNativeCanvas(canvas))
                canvas.renderMode = RenderMode.ScreenSpaceCamera;

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                canvas.worldCamera = uiCamera != null ? uiCamera : xrCamera;
                canvas.planeDistance = 10f;
            }
            else if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                canvas.worldCamera = xrCamera;

            canvas.pixelPerfect = false;
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        private static bool IsQuestNativeCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;

            var current = canvas.transform;
            while (current != null)
            {
                if (current.name.StartsWith("Quest", System.StringComparison.Ordinal))
                    return true;
                current = current.parent;
            }

            return false;
        }

        private static bool IsOffscreenCardRenderCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;

            var cardRenderer = Program.instance == null ? null : Program.instance.cardRenderer;
            if (cardRenderer != null && canvas.transform.IsChildOf(cardRenderer.transform))
                return true;

            var path = GetTransformPath(canvas.transform).ToLowerInvariant();
            return path.Contains("cardrenderer") || path.Contains("card render");
        }

        private static void ConfigureOffscreenCardRenderCanvas(Canvas canvas)
        {
            if (canvas == null)
                return;

            var renderCamera = Program.instance?.camera_?.cameraRenderTexture;
            canvas.renderMode = renderCamera == null ? RenderMode.ScreenSpaceOverlay : RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = renderCamera;
            canvas.pixelPerfect = false;
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
        }

        private void CleanupWorldCanvases()
        {
            configuredWorldCanvases.RemoveWhere(canvas => canvas == null);

            var staleCanvases = new List<Canvas>();
            foreach (var pair in worldCanvasSlots)
            {
                if (pair.Key == null)
                    staleCanvases.Add(pair.Key);
            }

            foreach (var canvas in staleCanvases)
                worldCanvasSlots.Remove(canvas);

            staleCanvases.Clear();
            foreach (var pair in worldCanvasPanels)
            {
                if (pair.Key == null)
                {
                    if (pair.Value != null)
                        Destroy(pair.Value);
                    staleCanvases.Add(pair.Key);
                }
            }

            foreach (var canvas in staleCanvases)
                worldCanvasPanels.Remove(canvas);

            staleCanvases.Clear();
            foreach (var pair in worldCanvasDecorations)
            {
                if (pair.Key == null)
                {
                    if (pair.Value != null)
                        Destroy(pair.Value.gameObject);
                    staleCanvases.Add(pair.Key);
                }
            }

            foreach (var canvas in staleCanvases)
            {
                worldCanvasDecorations.Remove(canvas);
                loadingWorldCanvasDecorations.Remove(canvas);
            }

            if (worldCanvasDecorations.Count > 0)
            {
                foreach (var pair in worldCanvasDecorations)
                    if (pair.Value != null)
                        Destroy(pair.Value.gameObject);
                worldCanvasDecorations.Clear();
                loadingWorldCanvasDecorations.Clear();
            }
        }

        private void ConfigureWorldCanvas(Canvas canvas)
        {
            EnsureWorldUiAnchor();
            var originalSortingOrder = canvas.sortingOrder;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;
            SetQuestOverlayLayer(canvas.gameObject);
            canvas.pixelPerfect = false;
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(originalSortingOrder, GetWorldCanvasSortingOrder(canvas));
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = Mathf.Max(scaler.dynamicPixelsPerUnit, 2f);
                scaler.referencePixelsPerUnit = Mathf.Max(scaler.referencePixelsPerUnit, 100f);
            }

            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            NormalizeCanvasRect(canvas);
            FlattenWorldCanvasGraphicDepth(canvas);
            PlaceWorldCanvas(canvas);
        }

        private static void NormalizeCanvasRect(Canvas canvas)
        {
            var rect = canvas.transform as RectTransform;
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1920f, 1080f);
        }

        private void EnsureWorldUiAnchor()
        {
            if (worldUiAnchor != null)
                return;

            var anchorObject = new GameObject("QuestWorldUiAnchor");
            worldUiAnchor = anchorObject.transform;
            worldUiAnchor.SetParent(transform, true);
            RepositionWorldUiAnchor();
        }

        private void PlaceWorldCanvas(Canvas canvas)
        {
            EnsureWorldUiAnchor();
            GetWorldUiPose(canvas, out var position, out var rotation);

            SetQuestOverlayLayer(canvas.gameObject);
            canvas.transform.SetPositionAndRotation(position, rotation);
            SetWorldScale(canvas.transform, GetWorldCanvasScale(canvas));
            HideWorldCanvasPanel(canvas);
            LogWorldCanvasDiagnostics(canvas);
        }

        private void LogWorldCanvasDiagnostics(Canvas canvas)
        {
            if (canvas == null || xrCamera == null || Time.unscaledTime - lastWorldCanvasDiagnosticsLog < 5f)
                return;

            lastWorldCanvasDiagnosticsLog = Time.unscaledTime;
            var rect = canvas.transform as RectTransform;
            var center = rect == null ? Vector3.zero : xrCamera.WorldToViewportPoint(rect.TransformPoint(Vector3.zero));
            var right = rect == null ? Vector3.zero : xrCamera.WorldToViewportPoint(rect.TransformPoint(Vector3.right));
            var up = rect == null ? Vector3.zero : xrCamera.WorldToViewportPoint(rect.TransformPoint(Vector3.up));
            var visibleGraphics = 0;
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (graphic != null && graphic.enabled && graphic.gameObject.activeInHierarchy && graphic.color.a > 0.01f)
                    visibleGraphics += 1;
            }

            Debug.LogFormat(
                "Quest XR world canvas diagnostics. Path={0}, Pos={1}, Rot={2}, Scale={3}, Size={4}, ViewCenter={5}, RightDelta={6:F3}, UpDelta={7:F3}, Layer={8}, VisibleGraphics={9}",
                GetTransformPath(canvas.transform),
                canvas.transform.position,
                canvas.transform.rotation.eulerAngles,
                canvas.transform.lossyScale,
                rect == null ? Vector2.zero : rect.rect.size,
                center,
                right.x - center.x,
                up.y - center.y,
                LayerMask.LayerToName(canvas.gameObject.layer),
                visibleGraphics);
        }

        private static void FlattenWorldCanvasGraphicDepth(Canvas canvas)
        {
            if (canvas == null)
                return;

            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect == null || rect == canvas.transform)
                    continue;

                var position = rect.localPosition;
                if (Mathf.Abs(position.z) > 0.001f)
                {
                    position.z = 0f;
                    rect.localPosition = position;
                }
            }
        }

        private void EnsureWorldCanvasDecoration(Canvas canvas)
        {
            return;
        }

        private IEnumerator LoadWorldCanvasDecoration(Canvas canvas, RawImage image)
        {
            yield break;
        }

        private void UpdateWorldCanvasDecoration(Canvas canvas)
        {
            if (canvas == null)
                return;
            if (!worldCanvasDecorations.TryGetValue(canvas, out var image) || image == null)
                return;

            if (image.transform.GetSiblingIndex() != 0)
                image.transform.SetAsFirstSibling();

            var shouldShow = ShouldShowWorldCanvasDecoration(canvas);
            if (image.gameObject.activeSelf != shouldShow)
                image.gameObject.SetActive(shouldShow);
        }

        private static bool ShouldShowWorldCanvasDecoration(Canvas canvas)
        {
            if (canvas == null || IsQuestNativeDuelActive())
                return false;

            var path = GetTransformPath(canvas.transform).ToLowerInvariant();
            if (path.Contains("popup")
                || path.Contains("carddetail")
                || path.Contains("card detail")
                || path.Contains("deckedit")
                || path.Contains("deck edit"))
                return false;

            return true;
        }

        private void CorrectWorldCanvasProjection(Canvas canvas)
        {
            if (canvas == null || xrCamera == null)
                return;

            var rect = canvas.transform as RectTransform;
            if (rect == null)
                return;

            var center = xrCamera.WorldToScreenPoint(rect.TransformPoint(Vector3.zero));
            var right = xrCamera.WorldToScreenPoint(rect.TransformPoint(Vector3.right));
            var up = xrCamera.WorldToScreenPoint(rect.TransformPoint(Vector3.up));
            if (center.z <= 0f || right.z <= 0f || up.z <= 0f)
                return;

            var localScale = rect.localScale;
            var changed = false;
            if (right.x < center.x)
            {
                localScale.x = -localScale.x;
                changed = true;
            }
            if (up.y < center.y)
            {
                localScale.y = -localScale.y;
                changed = true;
            }

            if (changed)
                rect.localScale = localScale;
        }

        private void HideUiRenderPanel()
        {
            if (uiRenderPanel != null && uiRenderPanel.activeSelf)
                uiRenderPanel.SetActive(false);
        }

        private static bool IsOcgCoreVisibleServant(OcgCore core)
        {
            return Program.instance != null
                && core != null
                && Program.instance.currentServant == core
                && core.showing;
        }

        private static bool IsQuestNativeDuelActive()
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (!IsOcgCoreVisibleServant(core))
                return false;

            if (core.currentMessage != GameMessage.Waiting)
                return true;

            if (core.cards != null && core.cards.Count > 0)
                return true;
            if (core.turns > 0)
                return true;
            if (core.life0 > 0 || core.life1 > 0)
                return true;

            return false;
        }

        private void EnsureWorldCanvasPanel(Canvas canvas)
        {
            if (canvas == null || worldCanvasPanels.ContainsKey(canvas))
                return;

            if (worldCanvasPanelMaterial == null)
            {
                worldCanvasPanelMaterial = CreateColorMaterial(
                    "Quest World Canvas Panel Material",
                    new Color(0.012f, 0.015f, 0.021f, 1f),
                    false);
            }

            var panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
            panel.name = "QuestWorldCanvasPanel_" + canvas.name;
            SetQuestOverlayLayer(panel);
            var collider = panel.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var renderer = panel.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = worldCanvasPanelMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            worldCanvasPanels[canvas] = panel;
        }

        private void UpdateWorldCanvasPanel(Canvas canvas)
        {
            if (canvas == null)
                return;

            EnsureWorldCanvasPanel(canvas);
            if (!worldCanvasPanels.TryGetValue(canvas, out var panel) || panel == null)
                return;

            var shouldShow = canvas.enabled && canvas.gameObject.activeInHierarchy && HasVisibleCanvasGraphics(canvas);
            if (panel.activeSelf != shouldShow)
                panel.SetActive(shouldShow);
            if (!shouldShow)
                return;

            var rect = canvas.transform as RectTransform;
            if (rect == null)
                return;

            var width = Mathf.Max(rect.rect.width, rect.sizeDelta.x, 1f);
            var height = Mathf.Max(rect.rect.height, rect.sizeDelta.y, 1f);
            var scale = GetWorldCanvasScale(canvas);
            panel.transform.SetPositionAndRotation(
                canvas.transform.position + canvas.transform.forward * WorldCanvasPanelOffset,
                canvas.transform.rotation);
            panel.transform.localScale = new Vector3(
                width * scale + WorldCanvasPanelPadding,
                height * scale + WorldCanvasPanelPadding,
                1f);
        }

        private void HideWorldCanvasPanel(Canvas canvas)
        {
            if (canvas == null)
                return;

            if (worldCanvasPanels.TryGetValue(canvas, out var panel) && panel != null && panel.activeSelf)
                panel.SetActive(false);
        }

        private static bool HasVisibleCanvasGraphics(Canvas canvas)
        {
            if (canvas == null)
                return false;

            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>(false))
            {
                if (graphic == null || !graphic.enabled || !graphic.gameObject.activeInHierarchy)
                    continue;

                var color = graphic.color;
                if (color.a > 0.01f)
                    return true;
            }

            return false;
        }

        private void RepositionWorldUiAnchor()
        {
            if (worldUiAnchor == null)
                return;

            if (worldUiAnchorLocked)
            {
                worldUiAnchor.SetPositionAndRotation(lockedWorldUiAnchorPosition, lockedWorldUiAnchorRotation);
                worldUiAnchor.localScale = Vector3.one;
                return;
            }

            var rotation = GetDuelBaseYawRotation() * Quaternion.Euler(0f, 180f, 0f);
            var position = DuelWorldCenterOnGround
                + GetDuelBaseYawRotation() * Vector3.right * UiRenderPanelSideOffset
                + GetDuelBaseYawRotation() * Vector3.forward * UiRenderPanelForwardOffset
                + Vector3.up * UiRenderPanelHeightOffset;

            lockedWorldUiAnchorPosition = position;
            lockedWorldUiAnchorRotation = rotation;
            worldUiAnchorLocked = true;
            worldUiAnchor.SetPositionAndRotation(position, rotation);
            worldUiAnchor.localScale = Vector3.one;
            if (!worldUiAnchorPoseLogged)
            {
                worldUiAnchorPoseLogged = true;
                Debug.LogFormat(
                    "Quest XR world UI anchor locked to virtual world. Center={0}, Anchor={1}, Rot={2}, ForwardOffset={3:F1}, Width={4:F1}",
                    DuelWorldCenterOnGround,
                    position,
                    rotation.eulerAngles,
                    UiRenderPanelForwardOffset,
                    UiRenderPanelWidth);
            }
        }

        private void GetWorldUiPose(Canvas canvas, out Vector3 position, out Quaternion rotation)
        {
            if (worldUiAnchor == null)
                RepositionWorldUiAnchor();

            rotation = worldUiAnchor == null ? GetDuelBaseYawRotation() : worldUiAnchor.rotation;
            var anchorPosition = worldUiAnchor == null ? DuelLookTarget : worldUiAnchor.position;
            var slotOffset = GetWorldCanvasOffset(canvas);
            position = anchorPosition + rotation * slotOffset;
        }

        private Vector3 GetWorldCanvasOffset(Canvas canvas)
        {
            var depthIndex = GetWorldCanvasDepthIndex(canvas);
            return Vector3.back * (WorldCanvasDepthStep * depthIndex);
        }

        private int GetWorldCanvasSortingOrder(Canvas canvas)
        {
            var name = GetTransformPath(canvas == null ? null : canvas.transform).ToLowerInvariant();
            if (name.Contains("popup"))
                return 500;
            if (name.Contains("side") || name.Contains("chat") || name.Contains("sub"))
                return 300;
            if (name.Contains("blur"))
                return 200;
            return 100;
        }

        private int GetWorldCanvasDepthIndex(Canvas canvas)
        {
            if (canvas == null)
                return 0;

            if (worldCanvasSlots.TryGetValue(canvas, out var depthIndex))
                return depthIndex;

            depthIndex = worldCanvasSlots.Count;
            worldCanvasSlots[canvas] = depthIndex;
            return depthIndex;
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var path = transform.name;
            var parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static float GetWorldCanvasScale(Canvas canvas)
        {
            var scale = UiCanvasWorldScale;
            var rect = canvas == null ? null : canvas.transform as RectTransform;
            if (rect == null)
                return scale;

            var width = Mathf.Max(rect.rect.width, rect.sizeDelta.x);
            var height = Mathf.Max(rect.rect.height, rect.sizeDelta.y);
            if (width > 1f)
                scale = Mathf.Min(scale, UiCanvasMaxWidth / width);
            if (height > 1f)
                scale = Mathf.Min(scale, UiCanvasMaxHeight / height);

            return scale;
        }

        private static void SetWorldScale(Transform target, float scale)
        {
            var parent = target.parent;
            if (parent == null)
            {
                target.localScale = new Vector3(scale, scale, scale);
                return;
            }

            var parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                SafeDivide(scale, parentScale.x),
                SafeDivide(scale, parentScale.y),
                SafeDivide(scale, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) < 0.0001f ? value : value / divisor;
        }

        private void ConfigureUiInputModule()
        {
            if (uiPointReference == null || uiClickReference == null || EventSystem.current == null)
                return;

            var module = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (module == null)
                return;

            if (module.point != uiPointReference)
                module.point = uiPointReference;
            if (module.leftClick != uiClickReference)
                module.leftClick = uiClickReference;
        }

        private void CalibrateTrackingOriginIfReady()
        {
            if (trackingOriginCalibrated || xrCamera == null)
                return;

            xrCamera.orthographic = false;
            xrCamera.fieldOfView = 70f;
            xrCamera.nearClipPlane = 0.03f;
            xrCamera.farClipPlane = 1000f;
            if (UsePassthroughMixedReality)
            {
                xrCamera.clearFlags = CameraClearFlags.SolidColor;
                xrCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                ConfigureXrCameraForVirtualWorld();
            }

            if (!passthroughConfigured)
            {
                if (UsePassthroughMixedReality)
                    ConfigurePassthroughScene();
                else
                    ConfigureVirtualWorldScene();
                passthroughConfigured = true;
            }

            var localPosition = xrCamera.transform.localPosition;
            var localRotation = xrCamera.transform.localRotation;
            if (TryReadHeadPose(out var trackedPosition, out var trackedRotation))
            {
                localPosition = trackedPosition;
                localRotation = trackedRotation;
            }

            var elapsed = Time.unscaledTime - createdAt;
            var hasUserPresence = TryReadUserPresence(out var userPresent);
            var userPresenceReady = !hasUserPresence || userPresent || elapsed > 20f;
            var poseLooksInitialized = localPosition.sqrMagnitude > 0.0001f
                || Quaternion.Dot(localRotation, Quaternion.identity) < 0.9999f;
            var runtimeReady = XRSettings.isDeviceActive && elapsed > 3f;
            var fallbackReady = XRSettings.isDeviceActive && elapsed > 6f;
            if (!runtimeReady || !userPresenceReady || (!poseLooksInitialized && !fallbackReady))
            {
                if (Time.unscaledTime - lastTrackingWaitLog > 1f)
                {
                    Debug.LogFormat(
                        "Quest XR waiting for tracked head pose. Active={0}, Elapsed={1:F1}, UserPresence={2}/{3}, LocalHeadPos={4}, LocalHeadRot={5}",
                        XRSettings.isDeviceActive,
                        elapsed,
                        hasUserPresence ? userPresent.ToString() : "unsupported",
                        userPresenceReady,
                        localPosition,
                        localRotation.eulerAngles);
                    lastTrackingWaitLog = Time.unscaledTime;
                }
                return;
            }

            PlaceXrOrigin(localPosition, localRotation);
            trackingOriginCalibrated = true;
            trackingOriginCalibratedWithUserPresence = hasUserPresence && userPresent;
            Debug.LogFormat(
                "Quest XR tracking origin fixed. UserPresence={0}/{1}, LocalHeadPos={2}, LocalHeadRot={3}, OriginPos={4}, OriginRot={5}, Scale={6:F1}",
                hasUserPresence ? userPresent.ToString() : "unsupported",
                trackingOriginCalibratedWithUserPresence,
                localPosition,
                localRotation.eulerAngles,
                xrOrigin.transform.position,
                xrOrigin.transform.rotation.eulerAngles,
                DuelWorldUnitsPerMeter);
        }

        private void RecalibrateTrackingOriginWhenUserPresenceAppears()
        {
            if (!trackingOriginCalibrated || trackingOriginCalibratedWithUserPresence || xrOrigin == null)
                return;

            if (!TryReadUserPresence(out var userPresent) || !userPresent)
                return;

            if (!TryReadHeadPose(out var localPosition, out var localRotation))
                return;

            if (IsQuestNativeDuelActive())
            {
                trackingOriginCalibratedWithUserPresence = true;
                Debug.Log("Quest XR user-presence recalibration skipped during native duel to preserve player view yaw.");
                return;
            }

            PlaceXrOrigin(localPosition, localRotation);
            trackingOriginCalibratedWithUserPresence = true;
            Debug.LogFormat(
                "Quest XR tracking origin recentered after user presence. LocalHeadPos={0}, LocalHeadRot={1}, OriginPos={2}, OriginRot={3}",
                localPosition,
                localRotation.eulerAngles,
                xrOrigin.transform.position,
                xrOrigin.transform.rotation.eulerAngles);
        }

        private static bool TryReadHeadPose(out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            var device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            var hasPosition = device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyePosition, out position);
            var hasRotation = device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.centerEyeRotation, out rotation);
            if (hasPosition && hasRotation)
                return true;

            position = InputTracking.GetLocalPosition(XRNode.CenterEye);
            rotation = InputTracking.GetLocalRotation(XRNode.CenterEye);
            return position.sqrMagnitude > 0.0001f || Quaternion.Dot(rotation, Quaternion.identity) < 0.9999f;
        }

        private static bool TryReadUserPresence(out bool userPresent)
        {
            var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headDevice.isValid && headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out userPresent))
                return true;

            var centerEyeDevice = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
            if (centerEyeDevice.isValid && centerEyeDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out userPresent))
                return true;

            userPresent = false;
            return false;
        }

        private void PlaceXrOrigin(Vector3 localHeadPosition, Quaternion localHeadRotation)
        {
            if (xrOrigin == null)
                return;

            var baseRotation = GetCurrentDuelViewYawRotation();
            var headYawRotation = GetYawRotation(localHeadRotation);
            var originRotation = baseRotation * Quaternion.Inverse(headYawRotation);
            var scaledHeadOffset = localHeadPosition * DuelWorldUnitsPerMeter;
            var targetEyePosition = DuelEyePosition + questPlayerWorldOffset;
            xrOrigin.transform.localScale = Vector3.one * DuelWorldUnitsPerMeter;
            xrOrigin.transform.SetPositionAndRotation(targetEyePosition - originRotation * scaledHeadOffset, originRotation);

            RefreshWorldUiAfterXrOriginMove();
        }

        private void RepositionXrOriginPreservingCurrentView()
        {
            if (xrOrigin == null)
                return;

            if (!TryReadHeadPose(out var localHeadPosition, out _))
                localHeadPosition = Vector3.zero;

            var originRotation = xrOrigin.transform.rotation;
            var scaledHeadOffset = localHeadPosition * DuelWorldUnitsPerMeter;
            var targetEyePosition = DuelEyePosition + questPlayerWorldOffset;
            xrOrigin.transform.localScale = Vector3.one * DuelWorldUnitsPerMeter;
            xrOrigin.transform.SetPositionAndRotation(targetEyePosition - originRotation * scaledHeadOffset, originRotation);
            RefreshWorldUiAfterXrOriginMove();
        }

        private void ApplyQuestViewYawDelta(float yawDeltaDegrees)
        {
            if (xrOrigin == null || Mathf.Abs(yawDeltaDegrees) < 0.001f)
                return;

            if (!TryReadHeadPose(out var localHeadPosition, out _))
                localHeadPosition = Vector3.zero;

            var headWorldPosition = xrCamera == null
                ? DuelEyePosition + questPlayerWorldOffset
                : xrCamera.transform.position;
            var originRotation = Quaternion.Euler(0f, yawDeltaDegrees, 0f) * xrOrigin.transform.rotation;
            var scaledHeadOffset = localHeadPosition * DuelWorldUnitsPerMeter;
            xrOrigin.transform.localScale = Vector3.one * DuelWorldUnitsPerMeter;
            xrOrigin.transform.SetPositionAndRotation(headWorldPosition - originRotation * scaledHeadOffset, originRotation);
            RefreshWorldUiAfterXrOriginMove();
        }

        private void RefreshWorldUiAfterXrOriginMove()
        {
            if (worldUiAnchor != null)
            {
                RepositionWorldUiAnchor();
                foreach (var canvas in configuredWorldCanvases)
                {
                    if (canvas != null)
                        PlaceWorldCanvas(canvas);
                }
            }
        }

        private void UpdateQuestMainUiRecenterInput(bool questNativeDuelActive)
        {
            var held = ReadQuestButton(leftPrimaryButtonAction, XRNode.LeftHand, false);
            var pressed = held && !questMainUiRecenterPressedLastFrame;
            questMainUiRecenterPressedLastFrame = held;
            if (!pressed)
                return;

            var program = Program.instance;
            if (questNativeDuelActive || (program != null && program.currentServant == program.ocgcore))
            {
                Debug.Log("Quest main UI recenter skipped during duel.");
                return;
            }

            RecenterQuestUserToMainUi();
        }

        private void RecenterQuestUserToMainUi()
        {
            if (xrOrigin == null)
                return;

            ConfigureUiRenderPanel();
            if (!TryGetMainUiRecenterPose(out var panelPosition, out var panelRotation))
            {
                Debug.LogWarning("Quest main UI recenter failed: no main UI panel pose.");
                return;
            }

            var panelForward = panelRotation * Vector3.forward;
            panelForward.y = 0f;
            if (panelForward.sqrMagnitude < 0.0001f)
                panelForward = GetDuelBaseYawRotation() * Vector3.back;
            panelForward.Normalize();

            var targetEyePosition = panelPosition + panelForward * QuestMainUiRecenterDistance;
            targetEyePosition.y = panelPosition.y;

            var lookDirection = panelPosition - targetEyePosition;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude < 0.0001f)
                lookDirection = -panelForward;
            var targetViewYaw = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            if (!TryReadHeadPose(out var localHeadPosition, out var localHeadRotation))
            {
                localHeadPosition = Vector3.zero;
                localHeadRotation = Quaternion.identity;
            }

            PlaceXrOriginAtTargetEyePose(targetEyePosition, targetViewYaw, localHeadPosition, localHeadRotation);
            questMainUiRecenterActive = true;
            Debug.LogFormat(
                "Quest main UI recentered. Panel={0}, TargetEye={1}, TargetYaw={2}, Distance={3:F1}",
                panelPosition,
                targetEyePosition,
                targetViewYaw.eulerAngles,
                QuestMainUiRecenterDistance);
        }

        private bool TryGetMainUiRecenterPose(out Vector3 position, out Quaternion rotation)
        {
            if (uiRenderPanel != null && uiRenderPanel.activeInHierarchy)
            {
                position = uiRenderPanel.transform.position;
                rotation = uiRenderPanel.transform.rotation;
                return true;
            }

            EnsureWorldUiAnchor();
            if (worldUiAnchor != null)
            {
                position = worldUiAnchor.position;
                rotation = worldUiAnchor.rotation;
                return true;
            }

            position = default;
            rotation = Quaternion.identity;
            return false;
        }

        private void PlaceXrOriginAtTargetEyePose(
            Vector3 targetEyePosition,
            Quaternion targetViewYawRotation,
            Vector3 localHeadPosition,
            Quaternion localHeadRotation)
        {
            if (xrOrigin == null)
                return;

            var headYawRotation = GetYawRotation(localHeadRotation);
            var originRotation = targetViewYawRotation * Quaternion.Inverse(headYawRotation);
            var scaledHeadOffset = localHeadPosition * DuelWorldUnitsPerMeter;
            xrOrigin.transform.localScale = Vector3.one * DuelWorldUnitsPerMeter;
            xrOrigin.transform.SetPositionAndRotation(targetEyePosition - originRotation * scaledHeadOffset, originRotation);
            RefreshWorldUiAfterXrOriginMove();
        }

        private void UpdateQuestWorldControls()
        {
            if (!trackingOriginCalibrated || xrOrigin == null)
                return;

            if (UpdateQuestPreviewControls())
                return;

            if (!IsQuestNativeDuelActive())
                return;

            var rawRightAxis = ReadQuestThumbstick(rightThumbstickAction, XRNode.RightHand);
            var rawLeftAxis = ReadQuestThumbstick(leftThumbstickAction, XRNode.LeftHand);
            var rotateViewHeld = ReadQuestButton(rightSecondaryButtonAction, XRNode.RightHand, true);
            var fastControlHeld = ReadQuestButton(rightPrimaryButtonAction, XRNode.RightHand, false);
            var speedMultiplier = fastControlHeld ? QuestWorldFastControlMultiplier : 1f;
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            var rightAxis = Vector2.MoveTowards(smoothedQuestRightWorldAxis, rawRightAxis, QuestWorldControlAxisSmoothing * dt);
            var leftAxis = Vector2.MoveTowards(smoothedQuestLeftWorldAxis, rawLeftAxis, QuestWorldControlAxisSmoothing * dt);
            smoothedQuestRightWorldAxis = rightAxis;
            smoothedQuestLeftWorldAxis = leftAxis;
            var positionChanged = false;
            var viewYawChanged = false;
            var scaleChanged = false;

            if (rightAxis.sqrMagnitude > 0f)
            {
                if (rotateViewHeld)
                {
                    if (Mathf.Abs(rightAxis.x) > 0f)
                    {
                        questPlayerYawOffsetDegrees = NormalizeDegrees(
                            questPlayerYawOffsetDegrees + rightAxis.x * QuestPlayerYawTurnSpeed * speedMultiplier * dt);
                        viewYawChanged = true;
                    }
                }
                else
                {
                    var viewYaw = GetCurrentDuelViewYawRotation();
                    var forward = xrCamera == null ? viewYaw * Vector3.forward : xrCamera.transform.forward;
                    var right = xrCamera == null ? viewYaw * Vector3.right : xrCamera.transform.right;
                    forward.y = 0f;
                    right.y = 0f;
                    if (forward.sqrMagnitude < 0.0001f)
                        forward = viewYaw * Vector3.forward;
                    if (right.sqrMagnitude < 0.0001f)
                        right = viewYaw * Vector3.right;
                    forward.Normalize();
                    right.Normalize();

                    questPlayerWorldOffset += (right * rightAxis.x + forward * rightAxis.y) * QuestPlayerMoveSpeed * speedMultiplier * dt;
                    positionChanged = true;
                }
            }

            if (Mathf.Abs(leftAxis.y) > 0f)
            {
                questPlayerWorldOffset.y += leftAxis.y * QuestPlayerVerticalSpeed * speedMultiplier * dt;
                positionChanged = true;
            }

            if (Mathf.Abs(leftAxis.x) > 0f)
            {
                var previousScale = questDuelWorldScaleMultiplier;
                var targetScale = Mathf.Max(
                    questDuelWorldScaleMultiplier + leftAxis.x * QuestWorldScaleSpeed * speedMultiplier * dt,
                    QuestWorldScaleMinPositive);
                SetQuestDuelWorldScale(targetScale);
                scaleChanged = Mathf.Abs(previousScale - questDuelWorldScaleMultiplier) > 0.0001f;
            }

            if (viewYawChanged)
                ApplyQuestViewYawDelta(rightAxis.x * QuestPlayerYawTurnSpeed * speedMultiplier * dt);
            if (positionChanged)
                RepositionXrOriginPreservingCurrentView();
            if (scaleChanged)
                ApplyQuestDuelWorldUserTransform();

            if ((positionChanged || viewYawChanged || scaleChanged) && Time.unscaledTime - lastQuestWorldControlLog >= QuestWorldControlLogInterval)
            {
                lastQuestWorldControlLog = Time.unscaledTime;
                Debug.LogFormat(
                    "Quest world controls active. PlayerOffset={0}, DuelWorldScale={1:F2}, RightAxis={2}, LeftAxis={3}, Fast={4}, RotateView={5}, Yaw={6:F1}",
                    questPlayerWorldOffset,
                    questDuelWorldScaleMultiplier,
                    rightAxis,
                    leftAxis,
                    fastControlHeld,
                    rotateViewHeld,
                    questPlayerYawOffsetDegrees);
            }
        }

        private bool UpdateQuestPreviewControls()
        {
            if (!TryGetActiveQuestPreviewControlTarget(out var matePreview))
            {
                questPreviewDeletePressedLastFrame = false;
                return false;
            }

            var rightAxis = ReadQuestThumbstick(rightThumbstickAction, XRNode.RightHand);
            var leftAxis = ReadQuestThumbstick(leftThumbstickAction, XRNode.LeftHand);
            var resetHeld = ReadQuestButton(rightPrimaryButtonAction, XRNode.RightHand, false);
            var deleteHeld = matePreview && ReadQuestButton(rightSecondaryButtonAction, XRNode.RightHand, true);
            var deletePressed = deleteHeld && !questPreviewDeletePressedLastFrame;
            questPreviewDeletePressedLastFrame = deleteHeld;
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            if (matePreview)
            {
                if (deletePressed && TryDeleteSelectedQuestMatePreview())
                    return true;

                ApplyQuestPreviewControlDelta(
                    "Mate",
                    ref questMatePreviewOffset,
                    ref questMatePreviewRotation,
                    ref questMatePreviewScaleMultiplier,
                    questMatePreviewWorldRoot,
                    QuestPreviewMateDefaultOffset,
                    false,
                    leftAxis,
                    rightAxis,
                    resetHeld,
                    dt);
            }
            else
            {
                ApplyQuestPreviewControlDelta(
                    "Cutin",
                    ref questCutinPreviewOffset,
                    ref questCutinPreviewRotation,
                    ref questCutinPreviewScaleMultiplier,
                    questCutinWorldRoot,
                    QuestPreviewCutinDefaultOffset,
                    true,
                    leftAxis,
                    rightAxis,
                    resetHeld,
                    dt);
            }

            return true;
        }

        private bool TryGetActiveQuestPreviewControlTarget(out bool matePreview)
        {
            matePreview = false;
            var program = Program.instance;
            if (program == null || program.currentServant == null)
                return false;

            if (program.currentServant == program.mate)
            {
                matePreview = true;
                return true;
            }

            if (program.currentServant == program.cutin)
                return true;

            return false;
        }

        private void ApplyQuestPreviewControlDelta(
            string previewType,
            ref Vector3 offset,
            ref Vector2 rotationOffset,
            ref float scaleMultiplier,
            Transform root,
            Vector3 defaultOffset,
            bool pitchTowardEye,
            Vector2 leftAxis,
            Vector2 rightAxis,
            bool resetHeld,
            float dt)
        {
            var changed = false;
            if (resetHeld)
            {
                changed = offset.sqrMagnitude > 0.0001f
                    || rotationOffset.sqrMagnitude > 0.0001f
                    || Mathf.Abs(scaleMultiplier - 1f) > 0.0001f;
                offset = Vector3.zero;
                rotationOffset = Vector2.zero;
                scaleMultiplier = 1f;
                questPreviewRayDragging = false;
            }

            var rayRotated = UpdateQuestPreviewRayRotation(
                previewType,
                root,
                defaultOffset,
                offset,
                pitchTowardEye,
                ref rotationOffset,
                scaleMultiplier);
            changed |= rayRotated;

            if (leftAxis.sqrMagnitude > 0f)
            {
                offset.x += leftAxis.x * QuestPreviewMoveSpeed * dt;
                offset.y += leftAxis.y * QuestPreviewMoveSpeed * dt;
                changed = true;
            }

            if (Mathf.Abs(rightAxis.y) > 0f)
            {
                offset.z += rightAxis.y * QuestPreviewDepthSpeed * dt;
                changed = true;
            }

            if (Mathf.Abs(rightAxis.x) > 0f)
            {
                scaleMultiplier = Mathf.Max(
                    scaleMultiplier + rightAxis.x * QuestPreviewScaleSpeed * dt,
                    QuestWorldScaleMinPositive);
                changed = true;
            }

            if (!changed)
                return;

            if (root != null)
                PlaceQuestPreviewRoot(root, defaultOffset + offset, pitchTowardEye, rotationOffset, scaleMultiplier);

            if (Time.unscaledTime - lastQuestPreviewControlLog >= QuestWorldControlLogInterval)
            {
                lastQuestPreviewControlLog = Time.unscaledTime;
                Debug.LogFormat(
                    "Quest preview controls active. Type={0}, Offset={1}, Rotation={2}, Scale={3:F2}, LeftAxis={4}, RightAxis={5}, Reset={6}",
                    previewType,
                    offset,
                    rotationOffset,
                    scaleMultiplier,
                    leftAxis,
                    rightAxis,
                    resetHeld);
            }
        }

        private bool UpdateQuestPreviewRayRotation(
            string previewType,
            Transform root,
            Vector3 defaultOffset,
            Vector3 offset,
            bool pitchTowardEye,
            ref Vector2 rotationOffset,
            float scaleMultiplier)
        {
            if (root == null)
            {
                questPreviewRayDragging = false;
                return false;
            }

            var ray = BuildPointerRay(out var hasControllerPose);
            var pressed = hasControllerPose && IsRightControllerTriggerPressed();
            if (!pressed)
            {
                questPreviewRayDragging = false;
                questPreviewRayDragTarget = null;
                return false;
            }

            var matePreview = previewType == "Mate";
            if (!questPreviewRayDragging)
            {
                if (!TryGetQuestPreviewHit(ray, matePreview, out var hitDistance, out var hitTarget))
                    return false;

                questPreviewRayDragging = true;
                questPreviewRayDragMate = matePreview;
                questPreviewRayDragTarget = hitTarget;
                if (matePreview && hitTarget != null)
                    questSelectedMatePreview = hitTarget;
                questPreviewRayDragDistance = Mathf.Clamp(hitDistance, 0.25f, GetQuestControllerRayLength());
                questPreviewRayLastViewport = GetQuestPreviewRayViewport(ray, questPreviewRayDragDistance);
                suppressPointerUntilReleased = true;
                return false;
            }

            if (questPreviewRayDragMate != matePreview)
                return false;

            suppressPointerUntilReleased = true;
            var viewport = GetQuestPreviewRayViewport(ray, questPreviewRayDragDistance);
            var delta = viewport - questPreviewRayLastViewport;
            questPreviewRayLastViewport = viewport;
            if (delta.sqrMagnitude < 0.000001f)
                return false;

            if (matePreview && questPreviewRayDragTarget != null)
            {
                RotateQuestMatePreviewTarget(questPreviewRayDragTarget, delta);
                return true;
            }

            rotationOffset.x = NormalizeDegrees(rotationOffset.x + delta.x * QuestPreviewRayRotateSensitivityX);
            rotationOffset.y = Mathf.Clamp(
                rotationOffset.y - delta.y * QuestPreviewRayRotateSensitivityY,
                -QuestPreviewPitchLimit,
                QuestPreviewPitchLimit);
            PlaceQuestPreviewRoot(root, defaultOffset + offset, pitchTowardEye, rotationOffset, scaleMultiplier);
            return true;
        }

        private Vector2 GetQuestPreviewRayViewport(Ray ray, float distance)
        {
            if (xrCamera == null)
                return Vector2.zero;

            var point = ray.origin + ray.direction.normalized * Mathf.Max(distance, 0.25f);
            var viewport = xrCamera.WorldToViewportPoint(point);
            return new Vector2(viewport.x, viewport.y);
        }

        private bool TryGetQuestPreviewHit(Ray ray, bool matePreview, out float distance, out Transform targetRoot)
        {
            distance = float.PositiveInfinity;
            targetRoot = null;
            var hitCount = RaycastNonAllocSorted(ray);
            if (hitCount == 0)
                return false;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = questRaycastHits[i];
                if (hit.collider == null)
                    continue;

                var target = hit.collider.GetComponentInParent<QuestPreviewHitTarget>();
                if (target == null || target.MatePreview != matePreview)
                    continue;

                distance = hit.distance;
                targetRoot = target.TargetRoot != null ? target.TargetRoot : target.transform;
                return true;
            }

            return false;
        }

        private void RotateQuestMatePreviewTarget(Transform target, Vector2 viewportDelta)
        {
            if (target == null)
                return;

            var localEuler = target.localEulerAngles;
            localEuler.y = NormalizeDegrees(localEuler.y + viewportDelta.x * QuestPreviewRayRotateSensitivityX);
            localEuler.x = Mathf.Clamp(
                NormalizeDegrees(localEuler.x - viewportDelta.y * QuestPreviewRayRotateSensitivityY),
                -QuestPreviewPitchLimit,
                QuestPreviewPitchLimit);
            target.localEulerAngles = localEuler;
        }

        private bool TryDeleteSelectedQuestMatePreview()
        {
            if (questSelectedMatePreview == null)
            {
                var ray = BuildPointerRay(out var hasControllerPose);
                if (!hasControllerPose || !TryGetQuestPreviewHit(ray, true, out _, out questSelectedMatePreview))
                    return false;
            }

            var targetMate = questSelectedMatePreview.GetComponent<Mate>()
                ?? questSelectedMatePreview.GetComponentInParent<Mate>()
                ?? questSelectedMatePreview.GetComponentInChildren<Mate>();
            if (targetMate == null)
                return false;

            var deletedCode = targetMate.code;
            questPreviewRayDragging = false;
            questPreviewRayDragTarget = null;
            questSelectedMatePreview = null;

            var mateView = Program.instance == null ? null : Program.instance.mate;
            var deleted = mateView != null && mateView.DeletePreviewMate(targetMate);
            if (deleted)
            {
                LayoutQuestMatePreviews();
                Debug.LogFormat("Quest mate preview deleted. Code={0}", deletedCode);
            }
            return deleted;
        }

        private void ReapplyXrOriginForCurrentHeadPose()
        {
            if (TryReadHeadPose(out var localPosition, out var localRotation))
                PlaceXrOrigin(localPosition, localRotation);
            else
                PlaceXrOrigin(Vector3.zero, Quaternion.identity);
        }

        private void SetQuestDuelWorldScale(float targetScale)
        {
            targetScale = Mathf.Max(targetScale, QuestWorldScaleMinPositive);
            if (!questDuelWorldUserTransformInitialized)
            {
                questDuelWorldUserAnchorPosition = duelWorldAnchor == null ? lockedDuelAnchorPosition : duelWorldAnchor.position;
                questDuelWorldUserTransformInitialized = true;
            }

            var previousScale = Mathf.Max(questDuelWorldScaleMultiplier, QuestWorldScaleMinPositive);
            if (Mathf.Abs(previousScale - targetScale) < 0.0001f)
                return;

            var pivot = xrCamera == null ? DuelEyePosition + questPlayerWorldOffset : xrCamera.transform.position;
            var ratio = targetScale / previousScale;
            questDuelWorldUserAnchorPosition = pivot + (questDuelWorldUserAnchorPosition - pivot) * ratio;
            questDuelWorldScaleMultiplier = targetScale;
        }

        private static Vector2 ReadQuestThumbstick(InputAction action, XRNode node)
        {
            var value = Vector2.zero;
            try
            {
                if (action != null)
                    value = action.ReadValue<Vector2>();
            }
            catch
            {
                value = Vector2.zero;
            }

            if (value.sqrMagnitude < 0.0001f)
            {
                if (TryReadQuestPrimaryAxisFromDevices(node, out var axis))
                    value = axis;
            }

            value.x = Mathf.Abs(value.x) < QuestThumbstickDeadZone ? 0f : value.x;
            value.y = Mathf.Abs(value.y) < QuestThumbstickDeadZone ? 0f : value.y;
            return Vector2.ClampMagnitude(value, 1f);
        }

        private static bool ReadQuestButton(InputAction action, XRNode node, bool secondaryButton)
        {
            try
            {
                if (action != null && action.ReadValue<float>() > 0.5f)
                    return true;
            }
            catch
            {
            }

            return TryReadQuestButtonFromDevices(node, secondaryButton);
        }

        private static bool ReadQuestPrimaryAxisClick(InputAction action, XRNode node)
        {
            try
            {
                if (action != null && action.ReadValue<float>() > 0.5f)
                    return true;
            }
            catch
            {
            }

            return TryReadQuestPrimaryAxisClickFromDevices(node);
        }

        private static bool TryReadQuestPrimaryAxisFromDevices(XRNode node, out Vector2 axis)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (TryReadQuestPrimaryAxis(device, out axis))
                return true;

            var hand = GetHandCharacteristic(node);
            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand,
                ControllerDevices);
            foreach (var candidate in ControllerDevices)
                if (TryReadQuestPrimaryAxis(candidate, out axis))
                    return true;

            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(hand, ControllerDevices);
            foreach (var candidate in ControllerDevices)
                if (TryReadQuestPrimaryAxis(candidate, out axis))
                    return true;

            axis = Vector2.zero;
            return false;
        }

        private static bool TryReadQuestPrimaryAxis(UnityEngine.XR.InputDevice device, out Vector2 axis)
        {
            if (device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out axis))
                return true;

            axis = Vector2.zero;
            return false;
        }

        private static bool TryReadQuestButtonFromDevices(XRNode node, bool secondaryButton)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (TryReadQuestButton(device, secondaryButton))
                return true;

            var hand = GetHandCharacteristic(node);
            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand,
                ControllerDevices);
            foreach (var candidate in ControllerDevices)
                if (TryReadQuestButton(candidate, secondaryButton))
                    return true;

            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(hand, ControllerDevices);
            foreach (var candidate in ControllerDevices)
                if (TryReadQuestButton(candidate, secondaryButton))
                    return true;

            return false;
        }

        private static bool TryReadQuestPrimaryAxisClickFromDevices(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (TryReadQuestPrimaryAxisClick(device))
                return true;

            var hand = GetHandCharacteristic(node);
            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand,
                ControllerDevices);
            foreach (var candidate in ControllerDevices)
                if (TryReadQuestPrimaryAxisClick(candidate))
                    return true;

            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(hand, ControllerDevices);
            foreach (var candidate in ControllerDevices)
                if (TryReadQuestPrimaryAxisClick(candidate))
                    return true;

            return false;
        }

        private static bool TryReadQuestButton(UnityEngine.XR.InputDevice device, bool secondaryButton)
        {
            if (!device.isValid)
                return false;

            var usage = secondaryButton
                ? UnityEngine.XR.CommonUsages.secondaryButton
                : UnityEngine.XR.CommonUsages.primaryButton;
            return device.TryGetFeatureValue(usage, out var pressed) && pressed;
        }

        private static bool TryReadQuestPrimaryAxisClick(UnityEngine.XR.InputDevice device)
        {
            return device.isValid
                && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out var pressed)
                && pressed;
        }

        private static InputDeviceCharacteristics GetHandCharacteristic(XRNode node)
        {
            return node == XRNode.LeftHand
                ? InputDeviceCharacteristics.Left
                : InputDeviceCharacteristics.Right;
        }

        private void ResetQuestWorldControlsWhenNoDuel()
        {
            smoothedQuestRightWorldAxis = Vector2.zero;
            smoothedQuestLeftWorldAxis = Vector2.zero;

            if (questPlayerWorldOffset.sqrMagnitude < 0.0001f && Mathf.Abs(questPlayerYawOffsetDegrees) < 0.001f)
                return;

            questPlayerWorldOffset = Vector3.zero;
            questPlayerYawOffsetDegrees = 0f;
            if (trackingOriginCalibrated && xrOrigin != null)
                ReapplyXrOriginForCurrentHeadPose();
            Debug.Log("Quest world player offset reset outside duel.");
        }

        private void ApplyQuestDebugAutoFrameIfNeeded()
        {
            if (!QuestRuntimeDebugSettings.AutoFrameDuelView)
                return;
            if (!trackingOriginCalibrated || xrOrigin == null || duelWorldAnchor == null)
                return;

            ResolveQuestDebugViewFrame(out var eyeOffset, out var lookAt, out var yawOffset, out var worldScale, out var viewName);
            if (!questDebugAutoFrameApplied)
            {
                questDebugAutoFrameApplied = true;
                lastQuestDebugForcedViewApplyTime = -999f;
                questPlayerWorldOffset = eyeOffset;
                questPlayerYawOffsetDegrees = yawOffset;
                smoothedQuestRightWorldAxis = Vector2.zero;
                smoothedQuestLeftWorldAxis = Vector2.zero;
                if (Mathf.Abs(questDuelWorldScaleMultiplier - worldScale) > 0.0001f)
                    SetQuestDuelWorldScale(worldScale);

                ApplyQuestDuelWorldUserTransform();

                Debug.LogFormat(
                    "Quest debug auto frame applied. View={0}, PlayerOffset={1}, LookAt={2}, Yaw={3:F1}, DuelWorldScale={4:F2}, ForcedRotation={5}",
                    viewName,
                    questPlayerWorldOffset,
                    lookAt,
                    questPlayerYawOffsetDegrees,
                    questDuelWorldScaleMultiplier,
                    QuestRuntimeDebugSettings.AutoFrameDuelView);
            }

            const float refreshInterval = 0.5f;
            if (Time.unscaledTime - lastQuestDebugForcedViewApplyTime < refreshInterval)
                return;

            ApplyQuestDebugForcedView(DuelEyePosition + questPlayerWorldOffset, lookAt, yawOffset);
            lastQuestDebugForcedViewApplyTime = Time.unscaledTime;
        }

        private void ResetQuestDebugAutoDuelActions()
        {
            questDebugAutoActionSignature = string.Empty;
            questDebugAutoActionSignatureStartTime = -999f;
            questDebugAutoActionLastAttemptTime = -999f;
            questDebugAutoActionsExecuted = 0;
            questDebugAutoActionExecutedSignatures.Clear();
        }

        private void ApplyQuestDebugAutoDuelActionsIfNeeded()
        {
            if (!QuestRuntimeDebugSettings.AutoDuelActions)
                return;
            if (!QuestRuntimeDebugSettings.AutoEnterSolo || !Room.fromSolo)
                return;

            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null)
                return;
            if (questDebugAutoActionsExecuted >= QuestDebugAutoActionMaxPerDuel)
                return;
            if (core.currentMessage != GameMessage.SelectIdleCmd)
                return;
            if (!core.myTurn || core.currentPopup != null)
                return;

            var now = Time.unscaledTime;
            var signature = BuildQuestDebugAutoActionSignature(core);
            if (string.IsNullOrEmpty(signature))
                return;

            if (!string.Equals(signature, questDebugAutoActionSignature, StringComparison.Ordinal))
            {
                questDebugAutoActionSignature = signature;
                questDebugAutoActionSignatureStartTime = now;
                Debug.LogFormat(
                    "Quest debug auto action state observed: message={0}, phase={1}, signature={2}",
                    core.currentMessage,
                    core.phase,
                    signature);
                return;
            }

            if (questDebugAutoActionExecutedSignatures.Contains(signature))
                return;
            if (now - questDebugAutoActionSignatureStartTime < QuestDebugAutoActionStableDelay)
                return;
            if (now - questDebugAutoActionLastAttemptTime < QuestDebugAutoActionRetryDelay)
                return;

            questDebugAutoActionLastAttemptTime = now;
            if (!TryFindQuestDebugAutoIdleAction(core, out var action))
            {
                Debug.LogFormat(
                    "Quest debug auto action skipped: no safe idle action. message={0}, phase={1}, cards={2}",
                    core.currentMessage,
                    core.phase,
                    core.cards == null ? 0 : core.cards.Count);
                questDebugAutoActionExecutedSignatures.Add(signature);
                return;
            }

            questDebugAutoActionExecutedSignatures.Add(signature);
            questDebugAutoActionsExecuted += 1;
            Debug.LogFormat(
                "Quest debug auto action executed: count={0}, label={1}, type={2}, response={3}, card={4}, id={5}, location={6}, sequence={7}, message={8}, phase={9}",
                questDebugAutoActionsExecuted,
                GetQuestDuelActionLabel(action),
                action.Type,
                action.FirstResponse,
                GetQuestDebugCardName(action.Card),
                GetQuestDebugCardId(action.Card),
                action.Card == null || action.Card.p == null ? 0 : action.Card.p.location,
                action.Card == null || action.Card.p == null ? 0 : action.Card.p.sequence,
                core.currentMessage,
                core.phase);
            ClearQuestDuelActionMenu();
            ExecuteQuestDuelActionResponse(action);
            ResetPointerState();
        }

        private static string BuildQuestDebugAutoActionSignature(OcgCore core)
        {
            if (core == null || core.cards == null)
                return string.Empty;

            var builder = new StringBuilder(256);
            builder.Append(core.currentMessage)
                .Append('|')
                .Append(core.phase)
                .Append('|')
                .Append(core.myTurn ? '1' : '0');

            var actionableCount = 0;
            foreach (var card in core.cards)
            {
                if (card == null || card.p == null || card.buttons == null || card.buttons.Count == 0)
                    continue;
                if (card.p.controller != 0)
                    continue;

                actionableCount += card.buttons.Count;
                builder.Append(';')
                    .Append(GetQuestDebugCardId(card))
                    .Append('@')
                    .Append(card.p.location)
                    .Append(':')
                    .Append(card.p.sequence);

                foreach (var button in card.buttons)
                {
                    builder.Append(',')
                        .Append(button.type)
                        .Append('=');
                    if (button.response == null || button.response.Count == 0)
                        builder.Append("none");
                    else
                        builder.Append(button.response[0]);
                }
            }

            if (actionableCount == 0)
                return string.Empty;

            builder.Append(";count=").Append(actionableCount);
            return builder.ToString();
        }

        private static bool TryFindQuestDebugAutoIdleAction(OcgCore core, out QuestDuelAction action)
        {
            action = null;
            if (core == null || core.cards == null)
                return false;

            foreach (var actionType in QuestDebugAutoIdleActionPriority)
            {
                foreach (var card in core.cards)
                {
                    if (!TryGetQuestDebugAutoButton(card, actionType, out var button))
                        continue;

                    action = QuestDuelAction.FromCardButton(card, button);
                    if (action != null)
                        return true;
                }
            }

            return false;
        }

        private static bool TryGetQuestDebugAutoButton(
            GameCard card,
            MDPro3.UI.ButtonType actionType,
            out GameCard.DuelButtonInfo button)
        {
            button = default(GameCard.DuelButtonInfo);
            if (card == null || card.p == null || card.buttons == null || card.buttons.Count == 0)
                return false;
            if (card.p.controller != 0)
                return false;
            if (!IsQuestDebugAutoActionLocationAllowed(card, actionType))
                return false;

            foreach (var candidate in card.buttons)
            {
                if (candidate.type != actionType)
                    continue;
                if (candidate.response == null || candidate.response.Count == 0)
                    continue;

                button = candidate;
                return true;
            }

            return false;
        }

        private static bool IsQuestDebugAutoActionLocationAllowed(GameCard card, MDPro3.UI.ButtonType actionType)
        {
            if (card == null || card.p == null)
                return false;

            var location = card.p.location;
            var inHand = (location & (uint)CardLocation.Hand) != 0;
            var inMonsterZone = (location & (uint)CardLocation.MonsterZone) != 0;
            var inSpellZone = (location & (uint)CardLocation.SpellZone) != 0;
            var inGrave = (location & (uint)CardLocation.Grave) != 0;
            var inRemoved = (location & (uint)CardLocation.Removed) != 0;
            var inDeck = (location & (uint)CardLocation.Deck) != 0;
            var inExtra = (location & (uint)CardLocation.Extra) != 0;
            switch (actionType)
            {
                case MDPro3.UI.ButtonType.Summon:
                case MDPro3.UI.ButtonType.SetMonster:
                case MDPro3.UI.ButtonType.SetSpell:
                    return inHand;
                case MDPro3.UI.ButtonType.SpSummon:
                    return inHand || inMonsterZone || inSpellZone || inGrave || inRemoved || inDeck || inExtra;
                default:
                    return false;
            }
        }

        private static int GetQuestDebugCardId(GameCard card)
        {
            return card == null || card.GetData() == null ? 0 : card.GetData().Id;
        }

        private static string GetQuestDebugCardName(GameCard card)
        {
            var data = card == null ? null : card.GetData();
            if (data == null || string.IsNullOrWhiteSpace(data.Name))
                return "<unknown>";
            return data.Name;
        }

        private static void ResolveQuestDebugViewFrame(
            out Vector3 eyeOffset,
            out Vector3 lookAt,
            out float yawOffset,
            out float worldScale,
            out string viewName)
        {
            var preset = (QuestRuntimeDebugSettings.DebugViewPreset ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(preset))
                preset = "field";

            viewName = preset;
            eyeOffset = new Vector3(0f, 4f, -18f);
            lookAt = DuelLookTarget + new Vector3(0f, 1.1f, 0f);
            yawOffset = 0f;
            worldScale = 1f;

            switch (preset)
            {
                case "overview":
                case "wide":
                case "all":
                    eyeOffset = new Vector3(0f, 18f, -42f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(0f, 2.5f, -2f);
                    break;
                case "hand":
                case "player":
                    eyeOffset = new Vector3(0f, -1.5f, -24f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(0f, 9.0f, -42f);
                    break;
                case "field":
                case "duel":
                case "board":
                    eyeOffset = new Vector3(0f, 4f, -18f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(0f, 1.6f, -1.5f);
                    break;
                case "close":
                case "near":
                    eyeOffset = new Vector3(0f, -3.5f, -9f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(0f, 1.25f, -3.5f);
                    break;
                case "leftinfo":
                case "left":
                    eyeOffset = new Vector3(-54f, 8f, -14f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(-18f, 18f, -12f);
                    yawOffset = 24f;
                    break;
                case "rightinfo":
                case "right":
                    eyeOffset = new Vector3(54f, 8f, -14f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(18f, 18f, -12f);
                    yawOffset = -24f;
                    break;
                case "opponent":
                case "enemy":
                    eyeOffset = new Vector3(0f, 12f, 70f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(0f, 2f, -1.5f);
                    yawOffset = 180f;
                    break;
                case "menu":
                case "mainui":
                case "ui":
                    eyeOffset = new Vector3(0f, 4f, -8f);
                    lookAt = DuelWorldCenterOnGround + new Vector3(0f, 28f, 52.5f);
                    break;
            }

            if (QuestRuntimeDebugSettings.HasDebugViewOffset)
                eyeOffset = QuestRuntimeDebugSettings.DebugViewOffset;
            if (QuestRuntimeDebugSettings.HasDebugViewLookAt)
                lookAt = QuestRuntimeDebugSettings.DebugViewLookAt;
            if (QuestRuntimeDebugSettings.HasDebugViewYaw)
                yawOffset = QuestRuntimeDebugSettings.DebugViewYawDegrees;
            if (QuestRuntimeDebugSettings.HasDebugViewScale)
                worldScale = Mathf.Max(QuestRuntimeDebugSettings.DebugViewScale, QuestWorldScaleMinPositive);
        }

        private void ApplyQuestDebugForcedView(Vector3 targetEyePosition, Vector3 lookAt, float yawOffsetDegrees)
        {
            if (xrOrigin == null)
                return;

            if (!TryReadHeadPose(out var localHeadPosition, out var localHeadRotation))
            {
                localHeadPosition = Vector3.zero;
                localHeadRotation = Quaternion.identity;
            }

            var direction = lookAt - targetEyePosition;
            if (direction.sqrMagnitude < 0.0001f)
                direction = GetDuelBaseRotation() * Vector3.forward;

            var targetViewRotation = Quaternion.Euler(0f, yawOffsetDegrees, 0f) * Quaternion.LookRotation(direction.normalized, Vector3.up);
            var originRotation = targetViewRotation * Quaternion.Inverse(localHeadRotation);
            var scaledHeadOffset = localHeadPosition * DuelWorldUnitsPerMeter;
            xrOrigin.transform.localScale = Vector3.one * DuelWorldUnitsPerMeter;
            xrOrigin.transform.SetPositionAndRotation(targetEyePosition - originRotation * scaledHeadOffset, originRotation);
            RefreshWorldUiAfterXrOriginMove();
        }

        private static Quaternion GetDuelBaseRotation()
        {
            var direction = DuelLookTarget - DuelEyePosition;
            if (direction.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static Quaternion GetDuelBaseYawRotation()
        {
            var direction = DuelLookTarget - DuelEyePosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private Quaternion GetCurrentDuelViewYawRotation()
        {
            return Quaternion.Euler(0f, questPlayerYawOffsetDegrees, 0f) * GetDuelBaseYawRotation();
        }

        private static Quaternion GetYawRotation(Quaternion rotation)
        {
            var forward = rotation * Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static float NormalizeDegrees(float degrees)
        {
            while (degrees > 180f)
                degrees -= 360f;
            while (degrees < -180f)
                degrees += 360f;
            return degrees;
        }

        private void ShowQuestDuelActionButton(MDPro3.UI.DuelButton button)
        {
            var action = QuestDuelAction.FromLegacyButton(button);
            if (action == null || action.Response == null || action.Response.Count == 0)
                return;

            if (questDuelActions.Count > 0
                && !IsSameQuestDuelButtonGroup(questDuelActions[0], action))
                ClearQuestDuelActionMenu();

            if (!questDuelActions.Exists(existing => existing.LegacyButton == button))
                questDuelActions.Add(action);

            questDuelNativeUi?.HideCardInfo();
            EnsureQuestDuelActionMenu();
            RebuildQuestDuelActionMenu();
            if (questDuelActionMenuCanvas != null && !questDuelActionMenuCanvas.gameObject.activeSelf)
                questDuelActionMenuCanvas.gameObject.SetActive(true);
            InvalidateGraphicRaycasterCache();
            UpdateQuestDuelActionMenuPose();

            var now = Time.unscaledTime;
            if (now - lastQuestDuelActionMenuLog > 0.35f)
            {
                lastQuestDuelActionMenuLog = now;
                Debug.LogFormat(
                    "Quest duel action menu shown: buttons={0}, card={1}, location={2}, controller={3}, response={4}",
                    questDuelActions.Count,
                    action.Card == null ? "<none>" : action.Card.GetData().Id.ToString(),
                    action.Location,
                    action.Controller,
                    action.FirstResponse);
            }
        }

        private void HideQuestDuelActionButton(MDPro3.UI.DuelButton button)
        {
            if (button == null)
                return;

            var removed = questDuelActions.RemoveAll(action => action != null && action.LegacyButton == button);
            if (removed == 0)
                return;

            if (questDuelActions.Count == 0)
            {
                HideQuestDuelActionMenu();
                return;
            }

            RebuildQuestDuelActionMenu();
            UpdateQuestDuelActionMenuPose();
        }

        private void ShowQuestDuelCardActions(GameCard card)
        {
            if (card == null || card.buttons == null || card.buttons.Count == 0)
            {
                ClearQuestDuelActionMenu();
                return;
            }

            var actions = new List<QuestDuelAction>();
            var buttons = new List<GameCard.DuelButtonInfo>(card.buttons);
            buttons.Sort((left, right) => left.type.CompareTo(right.type));
            foreach (var button in buttons)
            {
                var action = QuestDuelAction.FromCardButton(card, button);
                if (action != null)
                    actions.Add(action);
            }

            ShowQuestDuelActions(actions);
        }

        private void ShowQuestDuelActions(List<QuestDuelAction> actions)
        {
            questDuelActions.Clear();
            if (actions != null)
            {
                foreach (var action in actions)
                    if (action != null && action.Response != null && action.Response.Count > 0)
                        questDuelActions.Add(action);
            }
            questDuelActions.Sort(CompareQuestDuelActions);
            ApplyQuestDuelActionDisplayIndexes(questDuelActions);

            if (questDuelActions.Count == 0)
            {
                HideQuestDuelActionMenu();
                return;
            }

            questDuelNativeUi?.HideCardInfo();
            EnsureQuestDuelActionMenu();
            RebuildQuestDuelActionMenu();
            if (questDuelActionMenuCanvas != null)
                questDuelActionMenuCanvas.gameObject.SetActive(true);
            InvalidateGraphicRaycasterCache();
            UpdateQuestDuelActionMenuPose();

            var first = questDuelActions[0];
            Debug.LogFormat(
                "Quest duel native action menu shown: actions={0}, card={1}, location={2}, controller={3}, response={4}",
                questDuelActions.Count,
                first.Card == null || first.Card.GetData() == null ? "<none>" : first.Card.GetData().Id.ToString(),
                first.Location,
                first.Controller,
                first.FirstResponse);
        }

        private void ClearQuestDuelActionMenu()
        {
            questDuelActions.Clear();
            HideQuestDuelActionMenu();
        }

        private static void ApplyQuestDuelActionDisplayIndexes(List<QuestDuelAction> actions)
        {
            if (actions == null || actions.Count <= 1)
                return;

            foreach (var action in actions)
                if (action != null)
                    action.DisplayIndex = 0;

            var activateCountByCard = new Dictionary<GameCard, int>();
            foreach (var action in actions)
            {
                if (action == null || action.Type != MDPro3.UI.ButtonType.Activate || action.Card == null)
                    continue;
                if (!activateCountByCard.ContainsKey(action.Card))
                    activateCountByCard[action.Card] = 0;
                activateCountByCard[action.Card] += 1;
            }

            var activateIndexByCard = new Dictionary<GameCard, int>();
            foreach (var action in actions)
            {
                if (action == null || action.Type != MDPro3.UI.ButtonType.Activate || action.Card == null)
                    continue;
                if (!activateCountByCard.TryGetValue(action.Card, out var count) || count <= 1)
                    continue;

                if (!activateIndexByCard.ContainsKey(action.Card))
                    activateIndexByCard[action.Card] = 0;
                activateIndexByCard[action.Card] += 1;
                action.DisplayIndex = activateIndexByCard[action.Card];
            }
        }

        private void HideQuestDuelActionMenu()
        {
            questDuelNativeUi?.HideActionHint();
            foreach (var row in questDuelActionRows)
            {
                if (row != null)
                    Destroy(row);
            }
            questDuelActionRows.Clear();
            questDuelActionByRow.Clear();
            questDuelActionMenuRadial = false;

            if (questDuelActionMenuCanvas != null && questDuelActionMenuCanvas.gameObject.activeSelf)
                questDuelActionMenuCanvas.gameObject.SetActive(false);
        }

        private void EnsureQuestDuelActionMenu()
        {
            if (questDuelActionMenuCanvas != null)
                return;

            var menuObject = new GameObject("QuestDuelActionMenu", typeof(RectTransform));
            SetQuestOverlayLayer(menuObject);
            if (duelWorldAnchor != null)
                menuObject.transform.SetParent(duelWorldAnchor, false);
            questDuelActionMenuCanvas = menuObject.AddComponent<Canvas>();
            questDuelActionMenuCanvas.renderMode = RenderMode.WorldSpace;
            questDuelActionMenuCanvas.worldCamera = xrCamera;
            questDuelActionMenuCanvas.overrideSorting = true;
            questDuelActionMenuCanvas.sortingOrder = short.MaxValue;
            questDuelActionMenuCanvas.pixelPerfect = false;

            var scaler = menuObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;
            scaler.referencePixelsPerUnit = 100f;

            var raycaster = menuObject.AddComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = false;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            questDuelActionMenuRect = menuObject.GetComponent<RectTransform>();
            questDuelActionMenuRect.anchorMin = new Vector2(0.5f, 0.5f);
            questDuelActionMenuRect.anchorMax = new Vector2(0.5f, 0.5f);
            questDuelActionMenuRect.pivot = new Vector2(0.5f, 0.5f);
            questDuelActionMenuRect.sizeDelta = new Vector2(QuestDuelActionMenuSingleColumnWidth, QuestDuelActionItemHeight + QuestDuelActionMenuPadding * 2f);

            questDuelActionMenuBackground = menuObject.AddComponent<Image>();
            questDuelActionMenuBackground.color = new Color(0.02f, 0.025f, 0.032f, 0f);
            questDuelActionMenuBackground.raycastTarget = false;

            var stemObject = new GameObject("QuestDuelActionSourceStem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            SetQuestOverlayLayer(stemObject);
            stemObject.transform.SetParent(questDuelActionMenuRect, false);
            questDuelActionMenuStemRect = stemObject.GetComponent<RectTransform>();
            questDuelActionMenuStemRect.anchorMin = new Vector2(0.5f, 0.5f);
            questDuelActionMenuStemRect.anchorMax = new Vector2(0.5f, 0.5f);
            questDuelActionMenuStemRect.pivot = new Vector2(0.5f, 0.5f);
            questDuelActionMenuStemRect.sizeDelta = new Vector2(QuestDuelActionStemWidth, 80f);
            questDuelActionMenuStemImage = stemObject.GetComponent<Image>();
            questDuelActionMenuStemImage.color = new Color(0.40f, 0.92f, 1f, 0.62f);
            questDuelActionMenuStemImage.raycastTarget = false;
            stemObject.SetActive(false);

            menuObject.SetActive(false);
        }

        private void RebuildQuestDuelActionMenu()
        {
            EnsureQuestDuelActionMenu();
            questDuelNativeUi?.HideActionHint();
            foreach (var row in questDuelActionRows)
            {
                if (row != null)
                    Destroy(row);
            }
            questDuelActionRows.Clear();
            questDuelActionByRow.Clear();

            questDuelActions.RemoveAll(action => action == null || action.Response == null || action.Response.Count == 0);
            if (questDuelActions.Count == 0)
            {
                HideQuestDuelActionMenu();
                return;
            }
            questDuelActions.Sort(CompareQuestDuelActions);

            var count = questDuelActions.Count;
            questDuelActionMenuRadial = ShouldUseQuestDuelActionRadialLayout(questDuelActions);
            var columns = questDuelActionMenuRadial ? 1 : ResolveQuestDuelActionColumns(count);
            if (questDuelActionMenuRadial)
            {
                questDuelActionMenuRect.sizeDelta = new Vector2(QuestDuelActionRadialMenuWidth, QuestDuelActionRadialMenuHeight);
            }
            else
            {
                var rows = Mathf.CeilToInt(count / (float)columns);
                var menuWidth = ResolveQuestDuelActionMenuWidth(columns);
                var height = QuestDuelActionMenuPadding * 2f
                    + QuestDuelActionItemHeight * rows
                    + QuestDuelActionItemGap * Mathf.Max(0, rows - 1);
                questDuelActionMenuRect.sizeDelta = new Vector2(menuWidth, height);
            }

            for (var index = 0; index < count; index += 1)
            {
                var action = questDuelActions[index];
                var row = CreateQuestDuelActionRow(action, index, columns, questDuelActionMenuRadial);
                questDuelActionRows.Add(row);
            }
        }

        private static bool ShouldUseQuestDuelActionRadialLayout(List<QuestDuelAction> actions)
        {
            if (actions == null || actions.Count == 0 || actions.Count > 5)
                return false;

            GameCard card = null;
            foreach (var action in actions)
            {
                if (action == null || action.Card == null)
                    return false;
                if (card == null)
                {
                    card = action.Card;
                    continue;
                }
                if (card != action.Card)
                    return false;
            }

            return true;
        }

        private static int ResolveQuestDuelActionColumns(int count)
        {
            if (count <= 1)
                return 1;
            if (count <= 3)
                return count;
            return 2;
        }

        private static float ResolveQuestDuelActionMenuWidth(int columns)
        {
            if (columns <= 1)
                return QuestDuelActionMenuSingleColumnWidth;
            if (columns == 2)
                return QuestDuelActionMenuTwoColumnWidth;
            return QuestDuelActionMenuWidth;
        }

        private GameObject CreateQuestDuelActionRow(QuestDuelAction action, int index, int columns, bool radialLayout)
        {
            var rowObject = new GameObject("QuestDuelAction_" + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            SetQuestOverlayLayer(rowObject);
            rowObject.transform.SetParent(questDuelActionMenuRect, false);

            columns = Mathf.Max(1, columns);
            var width = QuestDuelActionRadialButtonWidth;
            var height = QuestDuelActionRadialButtonHeight;
            var position = radialLayout
                ? ResolveQuestDuelActionRadialPosition(index, questDuelActions.Count)
                : ResolveQuestDuelActionGridPosition(index, columns, out width, out height);

            var rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = radialLayout ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            rowRect.anchorMax = rowRect.anchorMin;
            rowRect.pivot = radialLayout ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(width, height);
            rowRect.anchoredPosition = position;

            var image = rowObject.GetComponent<Image>();
            image.color = GetQuestDuelActionColor(action);
            image.raycastTarget = true;
            AddQuestDuelActionButtonChrome(rowObject.transform, rowRect.sizeDelta, ResolveQuestDuelActionAccent(action));

            var uiButton = rowObject.GetComponent<Button>();
            uiButton.targetGraphic = image;
            uiButton.transition = Selectable.Transition.ColorTint;
            uiButton.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f),
                pressedColor = new Color(0.78f, 0.9f, 1f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f),
                colorMultiplier = 1f,
                fadeDuration = 0.04f
            };
            uiButton.onClick.AddListener(() => ExecuteQuestDuelAction(action));
            questDuelActionByRow[rowObject] = action;
            var floater = rowObject.AddComponent<QuestDuelActionButtonFloat>();
            floater.Configure(
                rowRect.anchoredPosition,
                index * 0.63f,
                QuestDuelActionFloatAmplitude,
                QuestDuelActionHoverScaleBoost,
                () => ShowQuestDuelActionHint(action),
                () => HideQuestDuelActionHint(action));

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            SetQuestOverlayLayer(labelObject);
            labelObject.transform.SetParent(rowObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 4f);
            labelRect.offsetMax = new Vector2(-16f, -4f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = GetQuestDuelActionLabel(action);
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.fontSize = radialLayout ? 32f : columns == 1 ? 38f : 32f;
            label.enableAutoSizing = true;
            label.fontSizeMin = radialLayout ? 20f : 22f;
            label.fontSizeMax = radialLayout ? 32f : columns == 1 ? 38f : 32f;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            var font = Program.instance?.ui_?.tmpFont;
            if (font != null)
                label.font = font;

            return rowObject;
        }

        private Vector2 ResolveQuestDuelActionGridPosition(int index, int columns, out float width, out float height)
        {
            var column = index % columns;
            var row = index / columns;
            var menuWidth = questDuelActionMenuRect == null
                ? ResolveQuestDuelActionMenuWidth(columns)
                : questDuelActionMenuRect.sizeDelta.x;
            width = (menuWidth - QuestDuelActionMenuPadding * 2f - QuestDuelActionItemGap * (columns - 1)) / columns;
            height = QuestDuelActionItemHeight;
            var x = -menuWidth * 0.5f
                + QuestDuelActionMenuPadding
                + width * 0.5f
                + column * (width + QuestDuelActionItemGap);
            var y = -QuestDuelActionMenuPadding - row * (QuestDuelActionItemHeight + QuestDuelActionItemGap);
            return new Vector2(x, y);
        }

        private static Vector2 ResolveQuestDuelActionRadialPosition(int index, int count)
        {
            if (count <= 1)
                return new Vector2(0f, 58f);

            if (count == 2)
                return new Vector2(index == 0 ? -128f : 128f, 66f);

            if (count == 3)
            {
                switch (index)
                {
                    case 0:
                        return new Vector2(-230f, 46f);
                    case 1:
                        return new Vector2(0f, 122f);
                    default:
                        return new Vector2(230f, 46f);
                }
            }

            if (count == 4)
            {
                switch (index)
                {
                    case 0:
                        return new Vector2(-230f, 84f);
                    case 1:
                        return new Vector2(230f, 84f);
                    case 2:
                        return new Vector2(-120f, -54f);
                    default:
                        return new Vector2(120f, -54f);
                }
            }

            switch (index)
            {
                case 0:
                    return new Vector2(-260f, 82f);
                case 1:
                    return new Vector2(0f, 138f);
                case 2:
                    return new Vector2(260f, 82f);
                case 3:
                    return new Vector2(-140f, -56f);
                default:
                    return new Vector2(140f, -56f);
            }
        }

        private static void AddQuestDuelActionButtonChrome(Transform parent, Vector2 size, Color accent)
        {
            if (parent == null)
                return;

            var top = CreateQuestDuelActionDecor("ActionTopLine", parent, Vector2.zero, new Vector2(size.x, 7f), new Color(accent.r, accent.g, accent.b, 0.82f));
            top.SetAsFirstSibling();
            var left = CreateQuestDuelActionDecor("ActionLeftLine", parent, Vector2.zero, new Vector2(6f, size.y), new Color(accent.r, accent.g, accent.b, 0.58f));
            left.SetAsFirstSibling();
            var glow = CreateQuestDuelActionDecor(
                "ActionGlow",
                parent,
                new Vector2(10f, -size.y + 10f),
                new Vector2(size.x - 20f, 5f),
                new Color(accent.r, accent.g, accent.b, 0.42f));
            glow.SetAsFirstSibling();
        }

        private static RectTransform CreateQuestDuelActionDecor(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
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
            image.raycastTarget = false;
            return rect;
        }

        private void ShowQuestDuelActionHint(QuestDuelAction action)
        {
            if (action == null)
                return;

            EnsureQuestDuelNativeUi();
            questDuelNativeUi?.ShowActionHint(
                action.Card,
                GetQuestDuelActionLabel(action),
                BuildQuestDuelActionHint(action),
                action.Card == null ? null : GetQuestCardInfoBounds(action.Card));
        }

        private void HideQuestDuelActionHint(QuestDuelAction action)
        {
            questDuelNativeUi?.HideActionHint();
        }

        private static string BuildQuestDuelActionHint(QuestDuelAction action)
        {
            if (action == null)
                return string.Empty;

            var effectText = ResolveQuestDuelActionEffectText(action);
            if (!string.IsNullOrWhiteSpace(effectText))
                return effectText;

            var hint = NormalizeQuestDuelActionText(action.Hint);
            if (!string.IsNullOrWhiteSpace(hint))
                return hint;

            switch (action.Type)
            {
                case MDPro3.UI.ButtonType.Activate:
                    return "\u53d1\u52a8\u8fd9\u5f20\u5361\u6216\u8fd9\u4e2a\u6548\u679c\u3002";
                case MDPro3.UI.ButtonType.Battle:
                    return "\u9009\u62e9\u653b\u51fb\u5bf9\u8c61\u6216\u8fdb\u884c\u76f4\u63a5\u653b\u51fb\u3002";
                case MDPro3.UI.ButtonType.SpSummon:
                    return "\u5c06\u8fd9\u5f20\u5361\u7279\u6b8a\u53ec\u5524\u5230\u573a\u4e0a\u3002";
                case MDPro3.UI.ButtonType.Summon:
                    return "\u901a\u5e38\u53ec\u5524\u8fd9\u53ea\u602a\u517d\u3002";
                case MDPro3.UI.ButtonType.PenSummon:
                    return "\u8fdb\u884c\u7075\u6446\u53ec\u5524\u3002";
                case MDPro3.UI.ButtonType.SetMonster:
                    return "\u91cc\u4fa7\u5b88\u5907\u8868\u793a\u653e\u7f6e\u8fd9\u53ea\u602a\u517d\u3002";
                case MDPro3.UI.ButtonType.SetSpell:
                    return "\u5c06\u8fd9\u5f20\u9b54\u6cd5/\u9677\u9631\u5361\u76d6\u653e\u5230\u573a\u4e0a\u3002";
                case MDPro3.UI.ButtonType.SetPendulum:
                    return "\u5c06\u8fd9\u5f20\u5361\u653e\u7f6e\u5230\u7075\u6446\u533a\u3002";
                case MDPro3.UI.ButtonType.ToAttackPosition:
                    return "\u5c06\u8fd9\u53ea\u602a\u517d\u6539\u4e3a\u653b\u51fb\u8868\u793a\u3002";
                case MDPro3.UI.ButtonType.ToDefensePosition:
                    return "\u5c06\u8fd9\u53ea\u602a\u517d\u6539\u4e3a\u5b88\u5907\u8868\u793a\u3002";
                default:
                    return "\u6267\u884c\u9009\u4e2d\u7684\u51b3\u6597\u64cd\u4f5c\u3002";
            }
        }

        private static string ResolveQuestDuelActionEffectText(QuestDuelAction action)
        {
            if (action == null || action.Card == null || action.Card.effects == null || action.Card.effects.Count == 0)
                return string.Empty;

            if (action.Response != null && action.Response.Count > 0)
            {
                var text = string.Empty;
                var matchedCount = 0;
                foreach (var response in action.Response)
                {
                    foreach (var effect in action.Card.effects)
                    {
                        if (effect == null || effect.ptr != response)
                            continue;

                        var desc = NormalizeQuestDuelActionText(effect.desc);
                        if (string.IsNullOrWhiteSpace(desc))
                            continue;

                        matchedCount += 1;
                        if (matchedCount > 1)
                            text += "\n";
                        text += matchedCount + ". " + desc;
                        break;
                    }

                    if (matchedCount >= 3)
                        break;
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (action.Response.Count > matchedCount)
                        text += "\n...";
                    return text;
                }
            }

            if (action.Type == MDPro3.UI.ButtonType.Activate
                && action.DisplayIndex > 0
                && action.DisplayIndex <= action.Card.effects.Count)
                return NormalizeQuestDuelActionText(action.Card.effects[action.DisplayIndex - 1].desc);

            return string.Empty;
        }

        private static string NormalizeQuestDuelActionText(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r", string.Empty).Trim();
        }

        private void ExecuteQuestDuelAction(QuestDuelAction action)
        {
            if (action == null || action.Response == null || action.Response.Count == 0)
                return;

            Debug.LogFormat(
                "Quest duel action clicked: label={0}, type={1}, response={2}",
                GetQuestDuelActionLabel(action),
                action.Type,
                action.FirstResponse);
            ClearQuestDuelActionMenu();
            ExecuteQuestDuelActionResponse(action);
            ResetPointerState();
        }

        private void ExecuteQuestDuelActionResponse(QuestDuelAction action)
        {
            var core = Program.instance?.ocgcore;
            if (core == null || action == null || action.Response == null || action.Response.Count == 0)
                return;

            AudioManager.PlaySE("SE_DUEL_DECIDE");
            var response = action.FirstResponse;
            if (action.Card != null)
                questDuelWorldPresenter?.SetSelectionSourceCard(action.Card);
            if (response >= 0)
            {
                if ((core.currentMessage == GameMessage.SelectBattleCmd || core.currentMessage == GameMessage.SelectIdleCmd)
                    && action.Response.Count > 1
                    && action.Type == MDPro3.UI.ButtonType.Activate)
                {
                    if (ShowQuestEffectSelection(action.Card, true))
                        return;
                }

                SendQuestDuelIntResponse(response);
                return;
            }

            if (response == -1 || response == -2)
            {
                var responseCards = new List<GameCard>();
                foreach (var card in core.cards)
                {
                    if (card == null || card.p == null || card.buttons == null)
                        continue;
                    if (card.p.controller != action.Controller)
                        continue;
                    if ((card.p.location & action.Location) == 0)
                        continue;

                    foreach (var button in card.buttons)
                    {
                        if (button.type == action.Type)
                        {
                            responseCards.Add(card);
                            break;
                        }
                    }
                }

                var hint = action.Type == MDPro3.UI.ButtonType.Activate
                    ? "\u9009\u62e9\u6548\u679c\u53d1\u52a8\u3002"
                    : "\u9009\u62e9\u602a\u517d\u7279\u6b8a\u53ec\u5524\u3002";
                if (!ShowQuestSelectCardPanel(hint, responseCards, 1, 1, true, false))
                    core.ShowPopupSelectCard(hint, responseCards, 1, 1, true, false);
                return;
            }

            if (response == -3)
            {
                foreach (var place in core.places)
                    if (place != null
                        && place.p != null
                        && place.p.controller == action.Controller
                        && place.p.location == action.Location
                        && place.p.sequence == action.Sequence)
                        place.SelectCardInThisZone();
                return;
            }

            if (response == -4)
            {
                core.FieldSelectedSend();
                return;
            }

            if (response == -5)
                core.FieldSelectedCancel();
        }

        private static void SendQuestDuelIntResponse(int response)
        {
            var core = Program.instance?.ocgcore;
            if (core == null)
                return;

            var packet = new BinaryMaster();
            packet.writer.Write(response);
            core.SendReturn(packet.Get());
        }

        private static bool ShowQuestEffectSelection(GameCard card, bool includeCancel)
        {
            if (card == null || card.effects == null || card.effects.Count == 0)
                return false;

            var selections = new List<string> { "\u6548\u679c\u9009\u62e9" };
            var responses = new List<int>();
            foreach (var effect in card.effects)
            {
                var desc = effect.desc;
                if (string.IsNullOrWhiteSpace(desc) || desc.Length <= 2)
                    desc = "\u53d1\u52a8\u6548\u679c";
                selections.Add(desc);
                responses.Add(effect.ptr);
            }

            if (includeCancel)
            {
                selections.Add("\u653e\u5f03");
                responses.Add(-233);
            }

            if (activeInstance != null)
            {
                activeInstance.EnsureQuestDuelNativeUi();
                if (activeInstance.questDuelNativeUi != null)
                    return activeInstance.questDuelNativeUi.ShowSelectionNearCard(selections, responses, card);
            }

            return ShowQuestSelectionPanel(selections, responses);
        }

        private QuestDuelAction ResolveQuestDuelAction(GameObject uiObject)
        {
            if (uiObject == null)
                return null;

            var current = uiObject.transform;
            while (current != null)
            {
                if (questDuelActionByRow.TryGetValue(current.gameObject, out var action))
                    return action;
                if (questDuelActionMenuRect != null && current == questDuelActionMenuRect)
                    break;
                current = current.parent;
            }

            return null;
        }

        private void UpdateQuestDuelActionMenuPose()
        {
            if (questDuelActionMenuCanvas == null || !questDuelActionMenuCanvas.gameObject.activeSelf)
                return;

            questDuelActions.RemoveAll(action => action == null || action.Response == null || action.Response.Count == 0);
            if (questDuelActions.Count == 0)
            {
                HideQuestDuelActionMenu();
                return;
            }

            if (questDuelActionMenuCanvas.worldCamera != xrCamera)
                questDuelActionMenuCanvas.worldCamera = xrCamera;
            if (duelWorldAnchor != null && questDuelActionMenuRect.parent != duelWorldAnchor)
                questDuelActionMenuRect.SetParent(duelWorldAnchor, false);

            var action = questDuelActions[0];
            var position = ResolveQuestDuelActionMenuPosition(action);
            if (duelWorldAnchor != null && questDuelActionMenuRect.parent == duelWorldAnchor)
            {
                questDuelActionMenuRect.localPosition = duelWorldAnchor.InverseTransformPoint(position);
                questDuelActionMenuRect.localRotation = Quaternion.Inverse(duelWorldAnchor.rotation) * ResolveQuestDuelActionMenuRotation(position);
            }
            else
            {
                questDuelActionMenuRect.position = position;
                questDuelActionMenuRect.rotation = ResolveQuestDuelActionMenuRotation(position);
            }
            questDuelActionMenuRect.localScale = Vector3.one * QuestDuelActionMenuScale;
            UpdateQuestDuelActionMenuStem(action);
        }

        private Vector3 ResolveQuestDuelActionMenuPosition(QuestDuelAction action)
        {
            if (action == null)
                return DuelWorldCenterOnGround + new Vector3(0f, QuestDuelActionCardYOffset, -18f);

            if (action.Card != null
                && questDuelWorldPresenter != null
                && questDuelWorldPresenter.TryGetCardActionAnchor(action.Card, out var actionAnchor, out var actionRadius))
                return ResolveQuestDuelActionMenuPositionNearAnchor(actionAnchor, actionRadius);

            if (action.Card != null
                && questDuelWorldPresenter != null
                && questDuelWorldPresenter.TryGetCardWorldBounds(action.Card, out var questCardBounds))
                return ResolveQuestDuelActionMenuPositionNearBounds(questCardBounds);

            if (action.Card != null && action.Card.model != null)
            {
                var cardObject = action.Card.model;
                var position = cardObject.transform.position;
                if (TryGetObjectBounds(cardObject, out var bounds))
                    return ResolveQuestDuelActionMenuPositionNearBounds(bounds);

                return position + new Vector3(0f, QuestDuelActionCardYOffset, 0f);
            }

            var response = action.FirstResponse;
            if (IsQuestFieldSelectionAction(response))
                return DuelWorldCenterOnGround + new Vector3(0f, QuestDuelActionCardYOffset, -23f);

            var gps = new GPS
            {
                location = action.Location,
                controller = action.Controller,
                sequence = action.Sequence
            };
            var zonePosition = GameCard.GetCardPosition(gps);
            if (float.IsNaN(zonePosition.x) || zonePosition.sqrMagnitude < 0.001f)
                zonePosition = DuelWorldCenterOnGround;
            else
                zonePosition = ScaleDuelBoardPoint(zonePosition);

            return zonePosition + new Vector3(0f, QuestDuelActionCardYOffset, 0f);
        }

        private void UpdateQuestDuelActionMenuStem(QuestDuelAction action)
        {
            if (questDuelActionMenuStemRect == null || questDuelActionMenuStemImage == null || questDuelActionMenuRect == null)
                return;

            if (questDuelActionMenuRadial)
            {
                questDuelActionMenuStemRect.gameObject.SetActive(false);
                return;
            }

            if (!TryResolveQuestDuelActionSourceAnchor(action, out var sourceAnchor))
            {
                questDuelActionMenuStemRect.gameObject.SetActive(false);
                return;
            }

            var localSource = questDuelActionMenuRect.InverseTransformPoint(sourceAnchor);
            var menuHeight = questDuelActionMenuRect.sizeDelta.y;
            var start = new Vector2(0f, -menuHeight * 0.5f + 10f);
            var target = new Vector2(
                Mathf.Clamp(localSource.x, -questDuelActionMenuRect.sizeDelta.x * 0.42f, questDuelActionMenuRect.sizeDelta.x * 0.42f),
                localSource.y);
            var delta = target - start;
            var length = Mathf.Clamp(delta.magnitude, 34f, QuestDuelActionStemMaxLength);
            if (delta.sqrMagnitude < 0.0001f)
                delta = Vector2.down;
            delta.Normalize();

            questDuelActionMenuStemRect.gameObject.SetActive(true);
            questDuelActionMenuStemRect.SetAsFirstSibling();
            questDuelActionMenuStemRect.anchoredPosition = start + delta * (length * 0.5f);
            questDuelActionMenuStemRect.sizeDelta = new Vector2(QuestDuelActionStemWidth, length);
            questDuelActionMenuStemRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            var accent = ResolveQuestDuelActionAccent(action);
            questDuelActionMenuStemImage.color = new Color(accent.r, accent.g, accent.b, 0.58f);
        }

        private bool TryResolveQuestDuelActionSourceAnchor(QuestDuelAction action, out Vector3 anchor)
        {
            anchor = default;
            if (action == null || action.Card == null || questDuelWorldPresenter == null)
                return false;

            if (questDuelWorldPresenter.TryGetCardActionAnchor(action.Card, out anchor, out _))
                return true;

            if (questDuelWorldPresenter.TryGetCardWorldBounds(action.Card, out var bounds))
            {
                anchor = bounds.center;
                return true;
            }

            return false;
        }

        private Vector3 ResolveQuestDuelActionMenuPositionNearAnchor(Vector3 anchor, float radius)
        {
            if (questDuelActionMenuRadial)
            {
                var toViewerForRadial = ResolvePlanarDirectionToViewer(anchor);
                var radialOffset = Mathf.Clamp(radius * 0.06f + QuestDuelActionCardForwardOffset, 0.34f, 0.86f);
                return anchor + Vector3.up * (QuestDuelActionCardYOffset + 2.35f) + toViewerForRadial * radialOffset;
            }

            var height = questDuelActionMenuRect == null
                ? QuestDuelActionItemHeight + QuestDuelActionMenuPadding * 2f
                : Mathf.Max(questDuelActionMenuRect.sizeDelta.y, QuestDuelActionItemHeight + QuestDuelActionMenuPadding * 2f);
            var menuHeightWorld = height * QuestDuelActionMenuScale;
            var toViewer = ResolvePlanarDirectionToViewer(anchor);
            var planarOffset = Mathf.Clamp(radius * 0.10f + QuestDuelActionCardForwardOffset, 0.45f, 1.25f);
            return anchor + Vector3.up * (QuestDuelActionCardYOffset + menuHeightWorld * 0.5f) + toViewer * planarOffset;
        }

        private Vector3 ResolveQuestDuelActionMenuPositionNearBounds(Bounds bounds)
        {
            var center = bounds.center;
            if (questDuelActionMenuRadial)
            {
                var radialY = bounds.max.y + QuestDuelActionCardYOffset + 2.35f;
                return new Vector3(center.x, radialY, center.z)
                    + ResolvePlanarDirectionToViewer(center) * 0.58f;
            }

            var height = questDuelActionMenuRect == null
                ? QuestDuelActionItemHeight + QuestDuelActionMenuPadding * 2f
                : Mathf.Max(questDuelActionMenuRect.sizeDelta.y, QuestDuelActionItemHeight + QuestDuelActionMenuPadding * 2f);
            var menuHeightWorld = height * QuestDuelActionMenuScale;
            var y = bounds.max.y + menuHeightWorld * 0.5f + 0.22f;
            var planarOffset = Mathf.Clamp(
                Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.18f + QuestDuelActionCardForwardOffset,
                0.42f,
                1.16f);

            return new Vector3(center.x, y, center.z) + ResolvePlanarDirectionToViewer(center) * planarOffset;
        }

        private Vector3 ResolvePlanarDirectionToViewer(Vector3 position)
        {
            var viewer = xrCamera == null ? DuelEyePosition + questPlayerWorldOffset : xrCamera.transform.position;
            var direction = viewer - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.back;
            return direction.normalized;
        }

        private Quaternion ResolveQuestDuelActionMenuRotation(Vector3 position)
        {
            var toViewer = ResolvePlanarDirectionToViewer(position);
            var forward = -toViewer;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private static bool TryGetObjectBounds(GameObject gameObject, out Bounds bounds)
        {
            bounds = default;
            if (gameObject == null)
                return false;

            var renderers = gameObject.GetComponentsInChildren<Renderer>(false);
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private static bool IsSameQuestDuelButtonGroup(QuestDuelAction left, QuestDuelAction right)
        {
            if (left == null || right == null)
                return false;

            if (left.Card != null || right.Card != null)
                return left.Card != null && left.Card == right.Card;

            var leftResponse = left.FirstResponse;
            var rightResponse = right.FirstResponse;
            if (IsQuestFieldSelectionAction(leftResponse) && IsQuestFieldSelectionAction(rightResponse))
                return true;

            if (left.Location != right.Location || left.Controller != right.Controller)
                return false;

            if (leftResponse == -3 || rightResponse == -3)
                return left.Sequence == right.Sequence;

            return true;
        }

        private static int CompareQuestDuelActions(QuestDuelAction left, QuestDuelAction right)
        {
            var priority = GetQuestDuelActionPriority(left).CompareTo(GetQuestDuelActionPriority(right));
            if (priority != 0)
                return priority;

            var leftLabel = GetQuestDuelActionLabel(left);
            var rightLabel = GetQuestDuelActionLabel(right);
            return string.Compare(leftLabel, rightLabel, StringComparison.Ordinal);
        }

        private static int GetQuestDuelActionPriority(QuestDuelAction action)
        {
            if (action == null)
                return 999;

            switch (action.Type)
            {
                case MDPro3.UI.ButtonType.Battle:
                    return 10;
                case MDPro3.UI.ButtonType.Activate:
                    return 20;
                case MDPro3.UI.ButtonType.SpSummon:
                case MDPro3.UI.ButtonType.PenSummon:
                    return 30;
                case MDPro3.UI.ButtonType.Summon:
                    return 40;
                case MDPro3.UI.ButtonType.SetMonster:
                case MDPro3.UI.ButtonType.SetSpell:
                case MDPro3.UI.ButtonType.SetPendulum:
                    return 50;
                case MDPro3.UI.ButtonType.ToAttackPosition:
                case MDPro3.UI.ButtonType.ToDefensePosition:
                    return 60;
                case MDPro3.UI.ButtonType.Select:
                case MDPro3.UI.ButtonType.Decide:
                    return 70;
                case MDPro3.UI.ButtonType.Cancel:
                    return 900;
                default:
                    return 500;
            }
        }

        private static bool IsQuestFieldSelectionAction(int response)
        {
            return response == -4 || response == -5;
        }

        private static string GetQuestDuelActionLabel(QuestDuelAction action)
        {
            if (action == null)
                return "\u64cd\u4f5c";
            if (action.Type == MDPro3.UI.ButtonType.SetSpell || action.Type == MDPro3.UI.ButtonType.SetMonster)
                return "\u653e\u7f6e";
            if (action.Type == MDPro3.UI.ButtonType.SetPendulum)
                return "\u7075\u6446\u653e\u7f6e";

            switch (action.Type)
            {
                case MDPro3.UI.ButtonType.Select:
                    return "\u9009\u62e9";
                case MDPro3.UI.ButtonType.Decide:
                    return "\u786e\u5b9a";
                case MDPro3.UI.ButtonType.Cancel:
                    return "\u53d6\u6d88";
                case MDPro3.UI.ButtonType.Activate:
                    return action.DisplayIndex > 0 ? "\u53d1\u52a8 " + action.DisplayIndex : "\u53d1\u52a8\u6548\u679c";
                case MDPro3.UI.ButtonType.Battle:
                    return "\u653b\u51fb";
                case MDPro3.UI.ButtonType.ToAttackPosition:
                    return "\u653b\u51fb\u8868\u793a";
                case MDPro3.UI.ButtonType.ToDefensePosition:
                    return "\u5b88\u5907\u8868\u793a";
                case MDPro3.UI.ButtonType.SpSummon:
                    return GetQuestSummonActionLabel(action.Card);
                case MDPro3.UI.ButtonType.Summon:
                    return "\u901a\u5e38\u53ec\u5524";
                case MDPro3.UI.ButtonType.PenSummon:
                    return "\u7075\u6446\u53ec\u5524";
                default:
                    if (!string.IsNullOrWhiteSpace(action.Hint))
                        return GetCompactQuestDuelActionHint(action.Hint);
                    return action.Type.ToString();
            }
        }

        private static string GetCompactQuestDuelActionHint(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
                return "\u64cd\u4f5c";

            hint = hint.Trim();
            return hint.Length <= 8 ? hint : hint.Substring(0, 8) + "\u2026";
        }

        private static string GetQuestSummonActionLabel(GameCard card)
        {
            var data = card == null ? null : card.GetData();
            if (data == null)
                return "\u7279\u6b8a\u53ec\u5524";
            if (data.HasType(CardType.Fusion))
                return "\u878d\u5408\u53ec\u5524";
            if (data.HasType(CardType.Synchro))
                return "\u540c\u8c03\u53ec\u5524";
            if (data.HasType(CardType.Xyz))
                return "\u8d85\u91cf\u53ec\u5524";
            if (data.HasType(CardType.Link))
                return "\u8fde\u63a5\u53ec\u5524";
            if (data.HasType(CardType.Ritual))
                return "\u4eea\u5f0f\u53ec\u5524";
            return "\u7279\u6b8a\u53ec\u5524";
        }

        private static Color GetQuestDuelActionColor(QuestDuelAction action)
        {
            if (action == null)
                return new Color(0.12f, 0.18f, 0.24f, 0.96f);

            switch (action.Type)
            {
                case MDPro3.UI.ButtonType.Activate:
                case MDPro3.UI.ButtonType.SpSummon:
                case MDPro3.UI.ButtonType.PenSummon:
                case MDPro3.UI.ButtonType.SetPendulum:
                    return new Color(0.62f, 0.45f, 0.11f, 0.96f);
                case MDPro3.UI.ButtonType.Battle:
                    return new Color(0.58f, 0.16f, 0.12f, 0.96f);
                case MDPro3.UI.ButtonType.Decide:
                    return new Color(0.08f, 0.42f, 0.31f, 0.96f);
                case MDPro3.UI.ButtonType.Cancel:
                    return new Color(0.32f, 0.12f, 0.16f, 0.96f);
                default:
                    return new Color(0.12f, 0.22f, 0.34f, 0.96f);
            }
        }

        private static Color ResolveQuestDuelActionAccent(QuestDuelAction action)
        {
            if (action == null)
                return new Color(0.34f, 0.88f, 1f, 1f);

            switch (action.Type)
            {
                case MDPro3.UI.ButtonType.Activate:
                case MDPro3.UI.ButtonType.SpSummon:
                case MDPro3.UI.ButtonType.PenSummon:
                case MDPro3.UI.ButtonType.SetPendulum:
                    return new Color(1f, 0.78f, 0.28f, 1f);
                case MDPro3.UI.ButtonType.Battle:
                    return new Color(1f, 0.34f, 0.24f, 1f);
                case MDPro3.UI.ButtonType.Decide:
                    return new Color(0.25f, 1f, 0.68f, 1f);
                case MDPro3.UI.ButtonType.Cancel:
                    return new Color(1f, 0.34f, 0.42f, 1f);
                default:
                    return new Color(0.28f, 0.86f, 1f, 1f);
            }
        }

        private void UpdateQuestPointer()
        {
            if (xrCamera == null)
                return;

            var ray = BuildPointerRay(out var hasControllerPose);
            if (!hasControllerPose)
            {
                UpdateQuestCardPointer(null, null, float.PositiveInfinity, false);
                UpdateQuestPilePointer(null, float.PositiveInfinity, false);
                UserInput.ClearPointerOverride();
                QueueVirtualMouse(lastQueuedMousePos, false);
                if (lastQuestPointerPressed || questHoveredUi != null)
                    UpdateDirectUiPointer(null, default, lastQueuedMousePos, false);
                questCardInfoPinButtonLastFrame = false;
                questCardInfoTriggerLastFrame = false;
                UpdateControllerRayVisual(ray, false, false, float.PositiveInfinity);
                LogQuestPointerStatus(false, false, false, null, lastQueuedMousePos, float.PositiveInfinity);
                return;
            }

            var isPressed = IsRightControllerTriggerPressed();
            if (suppressPointerUntilReleased)
            {
                if (isPressed)
                    isPressed = false;
                else
                    suppressPointerUntilReleased = false;
            }
            var uiRaycast = default(RaycastResult);
            GameObject currentUi = null;
            var hasUiHit = false;
            var hasUiPanelHit = false;
            var uiDistance = float.PositiveInfinity;
            var screenPosition = default(Vector2);
            if (TryGetUiRenderPanelHit(ray, out var panelScreenPosition, out var panelDistance))
            {
                hasUiPanelHit = true;
                screenPosition = panelScreenPosition;
                uiDistance = panelDistance;
                if (TryGetPanelUiHit(panelScreenPosition, out uiRaycast, out currentUi, out var resolvedPanelScreenPosition))
                {
                    hasUiHit = true;
                    screenPosition = resolvedPanelScreenPosition;
                }
            }
            if (!hasUiHit
                && !hasUiPanelHit
                && TryGetControllerUiHit(ray, out var worldScreenPosition, out var worldRaycast, out var worldUi, out var worldDistance))
            {
                screenPosition = worldScreenPosition;
                uiRaycast = worldRaycast;
                currentUi = worldUi;
                uiDistance = worldDistance;
                hasUiHit = true;
            }
            if (!hasUiHit && !hasUiPanelHit)
                screenPosition = ResolvePointerScreenPosition(ray, false);

            GameCard questCard = null;
            GameObject questCardHitObject = null;
            var questCardDistance = float.PositiveInfinity;
            var useMdproFieldSelection = IsQuestNativeDuelFieldSelectionActive();
            var useQuestDirectFieldCardSelection = IsQuestDirectFieldCardSelectionActive();
            var nativeBlockingPanelOpen = questDuelNativeUi != null && questDuelNativeUi.HasBlockingPanel;
            var hasCardHit = !nativeBlockingPanelOpen
                && !hasUiHit
                && !useMdproFieldSelection
                && TryGetQuestCardHit(ray, out questCard, out questCardHitObject, out questCardDistance);
            UpdateQuestCardPointer(questCard, questCardHitObject, questCardDistance, isPressed && !useMdproFieldSelection);

            QuestPileProxyHit questPileHit = null;
            var questPileDistance = float.PositiveInfinity;
            var hasPileHit = !nativeBlockingPanelOpen
                && !hasUiHit
                && !useMdproFieldSelection
                && !useQuestDirectFieldCardSelection
                && !hasCardHit
                && TryGetQuestPileHit(ray, out questPileHit, out questPileDistance);
            UpdateQuestPilePointer(questPileHit, questPileDistance, isPressed && !useMdproFieldSelection && !hasCardHit);

            UpdateQuestCardInfoPinInput(
                questCard,
                hasCardHit,
                hasPileHit,
                hasUiHit || hasUiPanelHit,
                nativeBlockingPanelOpen,
                useMdproFieldSelection,
                isPressed);

            var nativeDuelObjectPressed = (hasCardHit || hasPileHit) && !useMdproFieldSelection;
            UserInput.SetWorldPointerOverride(ray, screenPosition, isPressed && !nativeDuelObjectPressed);
            var useDirectUiPointer = hasUiHit || hasUiPanelHit || ShouldUseDirectUiPointerFallback();
            QueueVirtualMouse(screenPosition, isPressed && !useDirectUiPointer && !nativeDuelObjectPressed);
            if (useDirectUiPointer)
                UpdateDirectUiPointer(currentUi, uiRaycast, screenPosition, isPressed);
            else if (lastQuestPointerPressed || questHoveredUi != null)
                UpdateDirectUiPointer(null, default, screenPosition, false);
            UpdateControllerRayVisual(ray, hasControllerPose, hasUiHit || hasUiPanelHit, uiDistance);
            LogQuestPointerStatus(hasControllerPose, isPressed, hasUiHit || hasUiPanelHit, currentUi, screenPosition, uiDistance);
        }

        private static bool IsQuestNativeDuelFieldSelectionActive()
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null)
                return false;

            return core.currentMessage == GameMessage.SelectPlace
                || core.currentMessage == GameMessage.SelectDisfield;
        }

        private static bool IsQuestDirectFieldCardSelectionActive()
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null || core.places == null)
                return false;

            foreach (var place in core.places)
                if (place != null && place.cardSelecting && place.cookieCard != null)
                    return true;

            return false;
        }

        private void UpdateQuestCardInfoPinInput(
            GameCard card,
            bool hasCardHit,
            bool hasPileHit,
            bool hasUiHit,
            bool nativeBlockingPanelOpen,
            bool useMdproFieldSelection,
            bool triggerPressed)
        {
            var pinHeld = ReadQuestPrimaryAxisClick(rightThumbstickClickAction, XRNode.RightHand);
            var pinPressed = pinHeld && !questCardInfoPinButtonLastFrame;
            questCardInfoPinButtonLastFrame = pinHeld;

            var overEmptyWorld = !hasCardHit
                && !hasPileHit
                && !hasUiHit
                && !nativeBlockingPanelOpen
                && !useMdproFieldSelection;

            if (pinPressed)
            {
                EnsureQuestDuelNativeUi();
                if (hasCardHit && card != null)
                {
                    var pinned = questDuelNativeUi != null && questDuelNativeUi.TogglePinnedCardInfo(card);
                    Debug.LogFormat(
                        "Quest card detail pin toggled: pinned={0}, id={1}, location={2}, controller={3}, sequence={4}",
                        pinned,
                        card.GetData() == null ? 0 : card.GetData().Id,
                        card.p == null ? 0 : card.p.location,
                        card.p == null ? 0 : card.p.controller,
                        card.p == null ? 0 : card.p.sequence);
                }
                else if (overEmptyWorld)
                {
                    questDuelNativeUi?.ClearPinnedCardInfo();
                    Debug.Log("Quest card detail pin cleared by thumbstick click in empty world.");
                }
            }

            var triggerPressedThisFrame = triggerPressed && !questCardInfoTriggerLastFrame;
            questCardInfoTriggerLastFrame = triggerPressed;
            if (triggerPressedThisFrame && overEmptyWorld)
            {
                EnsureQuestDuelNativeUi();
                questDuelNativeUi?.ClearPinnedCardInfo();
                Debug.Log("Quest card detail pin cleared by empty-world trigger click.");
            }
        }

        private void UpdateQuestCardPointer(GameCard card, GameObject hitObject, float distance, bool pressed)
        {
            questDuelWorldPresenter?.SetHoveredCard(card);
            UpdateQuestCardInfoHover(card);

            if (card != null)
                LogQuestCardHit(card, hitObject, distance, pressed);

            if (pressed && !lastQuestCardPointerPressed)
            {
                questPressedCard = card;
                questDirectCardClickEligible = card != null;
                lastQuestCardPointerPressed = true;
                return;
            }

            if (!pressed && lastQuestCardPointerPressed)
            {
                if (questDirectCardClickEligible
                    && questPressedCard != null
                    && card == questPressedCard
                    && Program.instance?.ocgcore != null
                    && !Program.instance.ocgcore.handCardDraged)
                {
                    Debug.LogFormat(
                        "Quest XR direct card click: id={0}, buttons={1}, location={2}, controller={3}, message={4}, phase={5}, myTurn={6}, hit={7}",
                        questPressedCard.GetData().Id,
                        questPressedCard.buttons == null ? -1 : questPressedCard.buttons.Count,
                        questPressedCard.p == null ? 0 : questPressedCard.p.location,
                        questPressedCard.p == null ? 0 : questPressedCard.p.controller,
                        Program.instance.ocgcore.currentMessage,
                        Program.instance.ocgcore.phase,
                        Program.instance.ocgcore.myTurn,
                        hitObject == null ? "<none>" : GetTransformPath(hitObject.transform));
                    if (!TrySelectQuestFieldCard(questPressedCard))
                        HandleQuestCardClick(questPressedCard);
                }

                questPressedCard = null;
                questDirectCardClickEligible = false;
                lastQuestCardPointerPressed = false;
            }
        }

        private void UpdateQuestCardInfoHover(GameCard card)
        {
            if (card == null)
            {
                questInfoHoverCard = null;
                questInfoHoverShown = false;
                questDuelNativeUi?.HideCardInfo();
                return;
            }

            if (card != questInfoHoverCard)
            {
                questInfoHoverCard = card;
                questInfoHoverStartTime = Time.unscaledTime;
                questInfoHoverShown = false;
                questDuelNativeUi?.HideCardInfo();
                return;
            }

            if (questInfoHoverShown)
            {
                questDuelNativeUi?.UpdateCardInfoAnchor(GetQuestCardInfoBounds(card));
                return;
            }

            if (Time.unscaledTime - questInfoHoverStartTime < QuestCardInfoHoverDelay)
                return;

            EnsureQuestDuelNativeUi();
            if (questDuelNativeUi == null || questDuelNativeUi.HasBlockingPanel)
                return;

            if (questDuelNativeUi.ShowCardInfo(card, GetQuestCardInfoBounds(card)))
                questInfoHoverShown = true;
        }

        private void HandleQuestCardClick(GameCard card)
        {
            if (card == null || Program.instance?.ocgcore == null)
                return;

            AudioManager.PlaySE("SE_DUEL_SELECT");
            EnsureQuestDuelNativeUi();

            if (Program.instance.ocgcore.currentPopup != null)
            {
                ClearQuestDuelActionMenu();
                ShowQuestNoActionNotice(card);
                return;
            }

            if (card.buttons != null && card.buttons.Count > 0)
                ShowQuestDuelCardActions(card);
            else
            {
                ClearQuestDuelActionMenu();
                ShowQuestNoActionNotice(card);
            }
        }

        private void ShowQuestNoActionNotice(GameCard card)
        {
            var now = Time.unscaledTime;
            if (card == lastQuestNoActionNoticeCard && now - lastQuestNoActionNoticeTime < QuestNoActionNoticeCooldown)
                return;

            lastQuestNoActionNoticeCard = card;
            lastQuestNoActionNoticeTime = now;
            var reason = ResolveQuestNoActionNotice(card);
            questDuelWorldPresenter?.ShowCardNotice(card, reason);
            Debug.LogFormat(
                "Quest XR no card action: id={0}, reason={1}, buttons={2}, location={3}, controller={4}, message={5}, phase={6}, myTurn={7}",
                card == null || card.GetData() == null ? 0 : card.GetData().Id,
                reason,
                card == null || card.buttons == null ? -1 : card.buttons.Count,
                card == null || card.p == null ? 0 : card.p.location,
                card == null || card.p == null ? 0 : card.p.controller,
                Program.instance?.ocgcore == null ? GameMessage.Waiting : Program.instance.ocgcore.currentMessage,
                Program.instance?.ocgcore == null ? DuelPhase.Draw : Program.instance.ocgcore.phase,
                Program.instance?.ocgcore != null && Program.instance.ocgcore.myTurn);
        }

        private static string ResolveQuestNoActionNotice(GameCard card)
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null)
                return "\u5f53\u524d\u4e0d\u53ef\u64cd\u4f5c";

            if (core.currentPopup != null)
                return "\u5148\u5904\u7406\u5f53\u524d\u9009\u62e9";

            if (core.currentMessage == GameMessage.Waiting)
                return "\u7b49\u5f85\u51b3\u6597\u5904\u7406";

            if (core.currentMessage == GameMessage.SelectChain || core.currentMessage == GameMessage.SortChain)
                return "\u5148\u5904\u7406\u8fde\u9501";

            if (core.currentMessage == GameMessage.SelectCard
                || core.currentMessage == GameMessage.SelectUnselect
                || core.currentMessage == GameMessage.SelectSum
                || core.currentMessage == GameMessage.SelectTribute
                || core.currentMessage == GameMessage.SelectCounter)
                return "\u8bf7\u9009\u62e9\u9ad8\u4eae\u76ee\u6807";

            if (card != null && card.p != null && card.p.controller != 0)
                return core.myTurn ? "\u5bf9\u65b9\u5361\u7247" : "\u5bf9\u65b9\u64cd\u4f5c\u4e2d";

            if (!core.myTurn)
                return "\u5bf9\u65b9\u56de\u5408";

            if (core.currentMessage == GameMessage.SelectIdleCmd || core.currentMessage == GameMessage.SelectBattleCmd)
                return "\u5f53\u524d\u65f6\u70b9\u4e0d\u53ef\u64cd\u4f5c";

            return "\u5f53\u524d\u4e0d\u53ef\u64cd\u4f5c";
        }

        private Bounds? GetQuestCardInfoBounds(GameCard card)
        {
            if (card != null
                && questDuelWorldPresenter != null
                && questDuelWorldPresenter.TryGetCardInfoBounds(card, out var infoBounds))
                return infoBounds;

            if (card != null
                && questDuelWorldPresenter != null
                && questDuelWorldPresenter.TryGetCardWorldBounds(card, out var bounds))
                return bounds;

            return null;
        }

        private bool TrySelectQuestFieldCard(GameCard card)
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (card == null || core == null)
                return false;
            if (!core.IsSelectingFieldCard(card))
                return false;

            var place = FindQuestFieldSelectionPlace(card);
            if (place == null)
                return false;

            AudioManager.PlaySE("SE_DUEL_SELECT");
            EnsureQuestDuelNativeUi();
            questDuelNativeUi?.HideAllPopups();
            ClearQuestDuelActionMenu();
            questDuelWorldPresenter?.SetSelectionSourceCard(null);
            Debug.LogFormat(
                "Quest XR selected field card: id={0}, selectPtr={1}, location={2}, controller={3}, sequence={4}, message={5}",
                card.GetData() == null ? 0 : card.GetData().Id,
                card.selectPtr,
                card.p == null ? 0 : card.p.location,
                card.p == null ? 0 : card.p.controller,
                card.p == null ? 0 : card.p.sequence,
                core.currentMessage);
            place.SelectCardInThisZone();
            return true;
        }

        private static MDPro3.UI.PlaceSelector FindQuestFieldSelectionPlace(GameCard card)
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (card == null || core == null || core.places == null)
                return null;

            foreach (var place in core.places)
                if (place != null && place.cardSelecting && place.cookieCard == card)
                    return place;

            return null;
        }

        private void UpdateQuestPilePointer(QuestPileProxyHit pile, float distance, bool pressed)
        {
            if (pressed && !lastQuestPilePointerPressed)
            {
                questPressedPile = pile;
                questPileClickEligible = pile != null;
                lastQuestPilePointerPressed = true;
                return;
            }

            if (!pressed && lastQuestPilePointerPressed)
            {
                if (questPileClickEligible && questPressedPile != null && pile == questPressedPile)
                {
                    Debug.LogFormat(
                        "Quest XR pile click: controller={0}, location={1}, distance={2:F2}",
                        questPressedPile.Controller,
                        questPressedPile.Location,
                        distance);
                    ShowQuestLocationBrowser(questPressedPile.Controller, questPressedPile.Location);
                    ResetPointerState();
                }

                questPressedPile = null;
                questPileClickEligible = false;
                lastQuestPilePointerPressed = false;
            }
        }

        private void LogQuestCardHit(GameCard card, GameObject hitObject, float distance, bool pressed)
        {
            if (!QuestVerboseRuntimeDiagnostics)
                return;

            var now = Time.unscaledTime;
            if (!pressed && card == lastLoggedQuestCardHit && now - lastQuestCardHitLog < 1f)
                return;
            if (pressed && now - lastQuestCardHitLog < 0.25f)
                return;

            lastLoggedQuestCardHit = card;
            lastQuestCardHitLog = now;
            Debug.LogFormat(
                "Quest XR card hit: id={0}, buttons={1}, location={2}, controller={3}, hit={4}, distance={5:F2}",
                card.GetData().Id,
                card.buttons == null ? -1 : card.buttons.Count,
                card.p == null ? 0 : card.p.location,
                card.p == null ? 0 : card.p.controller,
                hitObject == null ? "<none>" : GetTransformPath(hitObject.transform),
                distance);
        }

        private bool TryGetQuestCardHit(Ray ray, out GameCard card, out GameObject hitObject, out float distance)
        {
            card = null;
            hitObject = null;
            distance = float.PositiveInfinity;

            var hitCount = RaycastNonAllocSorted(ray);
            if (hitCount == 0)
                return false;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = questRaycastHits[i];
                var candidateObject = hit.collider == null ? null : hit.collider.gameObject;
                if (candidateObject == null || ShouldIgnoreQuestCardHit(candidateObject))
                    continue;

                var candidateCard = ResolveQuestProxyCardFromHit(candidateObject);
                if (candidateCard == null)
                    continue;

                card = candidateCard;
                hitObject = candidateObject;
                distance = hit.distance;
                return true;
            }

            for (var i = 0; i < hitCount; i++)
            {
                var hit = questRaycastHits[i];
                var candidateObject = hit.collider == null ? null : hit.collider.gameObject;
                if (candidateObject == null || ShouldIgnoreQuestCardHit(candidateObject))
                    continue;

                var candidateCard = ResolveGameCardFromHit(candidateObject);
                if (candidateCard == null)
                    continue;

                card = candidateCard;
                hitObject = candidateObject;
                distance = hit.distance;
                return true;
            }

            return false;
        }

        private static GameCard ResolveQuestProxyCardFromHit(GameObject hitObject)
        {
            if (hitObject == null)
                return null;

            var hit = hitObject.GetComponent<QuestCardProxyHit>()
                ?? hitObject.GetComponentInParent<QuestCardProxyHit>();
            return hit == null ? null : hit.Card;
        }

        private bool TryGetQuestPileHit(Ray ray, out QuestPileProxyHit pile, out float distance)
        {
            pile = null;
            distance = float.PositiveInfinity;

            var hitCount = RaycastNonAllocSorted(ray);
            if (hitCount == 0)
                return false;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = questRaycastHits[i];
                var candidateObject = hit.collider == null ? null : hit.collider.gameObject;
                if (candidateObject == null)
                    continue;

                var candidatePile = candidateObject.GetComponent<QuestPileProxyHit>()
                    ?? candidateObject.GetComponentInParent<QuestPileProxyHit>()
                    ?? candidateObject.GetComponentInChildren<QuestPileProxyHit>();
                if (candidatePile == null || candidatePile.Location == CardLocation.Unknown)
                    continue;

                pile = candidatePile;
                distance = hit.distance;
                return true;
            }

            return false;
        }

        private static GameCard ResolveGameCardFromHit(GameObject hitObject)
        {
            if (hitObject == null)
                return null;

            if (activeInstance != null
                && activeInstance.questDuelWorldPresenter != null
                && activeInstance.questDuelWorldPresenter.TryResolveCard(hitObject, out var questProxyCard))
                return questProxyCard;

            var cardMono = hitObject.GetComponent<GameCardMono>()
                ?? hitObject.GetComponentInParent<GameCardMono>()
                ?? hitObject.GetComponentInChildren<GameCardMono>();
            if (cardMono != null && cardMono.cookieCard != null)
                return cardMono.cookieCard;

            var hitTransform = hitObject.transform;
            var core = Program.instance?.ocgcore;
            if (core?.cards == null)
                return null;

            foreach (var candidate in core.cards)
            {
                if (candidate == null || candidate.model == null)
                    continue;
                if (hitTransform == candidate.model.transform || hitTransform.IsChildOf(candidate.model.transform))
                    return candidate;
            }

            return null;
        }

        private static bool ShouldIgnoreQuestCardHit(GameObject hitObject)
        {
            if (hitObject == null)
                return true;
            if (hitObject.GetComponentInParent<CustomFieldMonsterVisual>() != null)
                return true;

            var name = hitObject.name;
            return name == "Closeup"
                || name.StartsWith("QuestDuelGroundCollider", System.StringComparison.Ordinal)
                || name.StartsWith("FieldMonsterGLB", System.StringComparison.Ordinal)
                || name == "Model";
        }

        private static bool ShouldUseDirectUiPointerFallback()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            return eventSystem.currentInputModule is not InputSystemUIInputModule;
        }

        private Ray BuildPointerRay(out bool hasControllerPose)
        {
            if (TryReadRightControllerPose(
                    out var controllerPosition,
                    out var controllerRotation,
                    out var usesAimPose,
                    out var positionEstimated))
            {
                hasControllerPose = true;
                var origin = xrOrigin == null ? null : xrOrigin.transform;
                var worldPosition = origin == null
                    ? DuelEyePosition + controllerPosition * DuelWorldUnitsPerMeter
                    : origin.TransformPoint(controllerPosition);
                var worldRotation = (origin == null ? GetDuelBaseRotation() : origin.rotation) * controllerRotation;
                if (positionEstimated && !controllerPoseFallbackLogged)
                {
                    Debug.Log("Quest XR controller ray using head-relative fallback position.");
                    controllerPoseFallbackLogged = true;
                }

                return SelectBestPointerRay(worldPosition, worldRotation, usesAimPose);
            }

            if (TryReadHandInteractionPose(out var handPosition, out var handRotation))
            {
                hasControllerPose = true;
                var origin = xrOrigin == null ? null : xrOrigin.transform;
                var worldPosition = origin == null
                    ? DuelEyePosition + handPosition * DuelWorldUnitsPerMeter
                    : origin.TransformPoint(handPosition);
                var worldRotation = (origin == null ? GetDuelBaseRotation() : origin.rotation) * handRotation;
                if (!handInteractionPoseLogged)
                {
                    Debug.Log("Quest XR pointer using OpenXR hand interaction pose.");
                    handInteractionPoseLogged = true;
                }

                return SelectBestPointerRay(worldPosition, worldRotation, true);
            }

            hasControllerPose = false;
            LogMissingControllerPose();
            return default;
        }

        private Ray SelectBestPointerRay(Vector3 worldPosition, Quaternion worldRotation, bool usesAimPose)
        {
            var forwardDirection = worldRotation * Vector3.forward;
            var backDirection = worldRotation * Vector3.back;
            var forwardRay = new Ray(worldPosition, forwardDirection.normalized);
            var backRay = new Ray(worldPosition, backDirection.normalized);
            var forwardScore = ScorePointerRay(forwardRay);
            var backScore = ScorePointerRay(backRay);

            if (Mathf.Abs(forwardScore - backScore) > 0.001f)
                return forwardScore > backScore ? forwardRay : backRay;

            var referencePoint = worldUiAnchor == null ? DuelLookTarget : worldUiAnchor.position;
            var referenceDirection = referencePoint - worldPosition;
            if (referenceDirection.sqrMagnitude > 0.0001f)
            {
                referenceDirection.Normalize();
                var forwardDot = Vector3.Dot(forwardRay.direction, referenceDirection);
                var backDot = Vector3.Dot(backRay.direction, referenceDirection);
                if (Mathf.Abs(forwardDot - backDot) > 0.001f)
                    return forwardDot > backDot ? forwardRay : backRay;
            }

            return usesAimPose ? backRay : forwardRay;
        }

        private float ScorePointerRay(Ray ray)
        {
            var score = 0f;
            var rayLength = GetQuestControllerRayLength();
            if (TryGetUiRenderPanelHit(ray, out _, out var panelDistance))
                score += 1200f - Mathf.Min(panelDistance, rayLength);
            if (TryGetAnyCanvasPlaneDistance(ray, out var uiDistance))
                score += 1000f - Mathf.Min(uiDistance, rayLength);
            if (TryGetWorldHit(ray, out _, out var worldDistance))
                score += 100f - Mathf.Min(worldDistance, rayLength) * 0.01f;

            var referencePoint = worldUiAnchor == null ? DuelLookTarget : worldUiAnchor.position;
            var referenceDirection = referencePoint - ray.origin;
            if (referenceDirection.sqrMagnitude > 0.0001f)
                score += Vector3.Dot(ray.direction, referenceDirection.normalized);
            return score;
        }

        private bool TryReadRightControllerPose(
            out Vector3 position,
            out Quaternion rotation,
            out bool usesAimPose,
            out bool positionEstimated)
        {
            if (TryReadControllerPose(XRNode.RightHand, out position, out rotation, out usesAimPose, out positionEstimated))
                return true;
            if (TryReadControllerPose(XRNode.LeftHand, out position, out rotation, out usesAimPose, out positionEstimated))
                return true;
            if (TryReadInputSystemControllerPose(true, out position, out rotation, out positionEstimated))
            {
                usesAimPose = false;
                return true;
            }
            if (TryReadInputSystemControllerPose(false, out position, out rotation, out positionEstimated))
            {
                usesAimPose = false;
                return true;
            }

            position = pointerPositionAction == null ? Vector3.zero : pointerPositionAction.ReadValue<Vector3>();
            rotation = pointerRotationAction == null ? Quaternion.identity : pointerRotationAction.ReadValue<Quaternion>();
            usesAimPose = false;
            positionEstimated = false;
            if (IsFinite(position) && IsFinite(rotation)
                && (position.sqrMagnitude > 0.0001f || Quaternion.Dot(rotation, Quaternion.identity) < 0.9999f))
            {
                if (position.sqrMagnitude <= 0.0001f)
                {
                    position = EstimateControllerLocalPosition(XRNode.RightHand);
                    positionEstimated = true;
                }

                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            positionEstimated = false;
            return false;
        }

        private static bool TryReadInputSystemControllerPose(
            bool rightHand,
            out Vector3 position,
            out Quaternion rotation,
            out bool positionEstimated)
        {
            foreach (var device in InputSystem.devices)
            {
                if (device == null || !IsInputSystemControllerForHand(device, rightHand))
                    continue;

                var positionControl = device.TryGetChildControl<Vector3Control>("devicePosition")
                    ?? device.TryGetChildControl<Vector3Control>("pointerPosition")
                    ?? device.TryGetChildControl<Vector3Control>("aimPosition");
                var rotationControl = device.TryGetChildControl<QuaternionControl>("deviceRotation")
                    ?? device.TryGetChildControl<QuaternionControl>("pointerRotation")
                    ?? device.TryGetChildControl<QuaternionControl>("aimRotation");
                if (rotationControl == null)
                    continue;

                rotation = NormalizeQuaternion(rotationControl.ReadValue());
                if (!IsFinite(rotation))
                    continue;

                position = positionControl == null ? Vector3.zero : positionControl.ReadValue();
                if (!IsFinite(position))
                    position = Vector3.zero;

                var hasRotation = Quaternion.Dot(rotation, Quaternion.identity) < 0.9999f;
                var hasPosition = position.sqrMagnitude > 0.0001f;
                if (!hasRotation && !hasPosition)
                    continue;

                if (!hasPosition)
                    position = EstimateControllerLocalPosition(rightHand ? XRNode.RightHand : XRNode.LeftHand);
                positionEstimated = !hasPosition;
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            positionEstimated = false;
            return false;
        }

        private static bool IsInputSystemControllerForHand(UnityEngine.InputSystem.InputDevice device, bool rightHand)
        {
            var descriptor = ((device.layout ?? string.Empty) + " " + (device.name ?? string.Empty) + " " + (device.displayName ?? string.Empty)).ToLowerInvariant();
            if (!descriptor.Contains("xr") && !descriptor.Contains("controller") && !descriptor.Contains("touch"))
                return false;

            var handName = rightHand ? "right" : "left";
            var oppositeHandName = rightHand ? "left" : "right";
            if (descriptor.Contains(oppositeHandName) && !descriptor.Contains(handName))
                return false;
            if (descriptor.Contains(handName))
                return true;

            foreach (var usage in device.usages)
            {
                var usageName = usage.ToString().ToLowerInvariant();
                if (usageName.Contains(oppositeHandName) && !usageName.Contains(handName))
                    return false;
                if (usageName.Contains(handName))
                    return true;
            }

            return false;
        }

        private static bool TryReadControllerPose(
            XRNode node,
            out Vector3 position,
            out Quaternion rotation,
            out bool usesAimPose,
            out bool positionEstimated)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (TryReadControllerPoseFromDevice(device, node, out position, out rotation, out usesAimPose, out positionEstimated))
                return true;

            var hand = node == XRNode.LeftHand
                ? InputDeviceCharacteristics.Left
                : InputDeviceCharacteristics.Right;
            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand,
                ControllerDevices);
            foreach (var candidate in ControllerDevices)
            {
                if (TryReadControllerPoseFromDevice(candidate, node, out position, out rotation, out usesAimPose, out positionEstimated))
                    return true;
            }

            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(hand, ControllerDevices);
            foreach (var candidate in ControllerDevices)
            {
                if (TryReadControllerPoseFromDevice(candidate, node, out position, out rotation, out usesAimPose, out positionEstimated))
                    return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            usesAimPose = false;
            positionEstimated = false;
            return false;
        }

        private static bool TryReadControllerPoseFromDevice(
            UnityEngine.XR.InputDevice device,
            XRNode node,
            out Vector3 position,
            out Quaternion rotation,
            out bool usesAimPose,
            out bool positionEstimated)
        {
            if (!device.isValid)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                usesAimPose = false;
                positionEstimated = false;
                return false;
            }

            if (TryGetPointerPose(device, out position, out rotation))
            {
                usesAimPose = true;
                positionEstimated = false;
                return true;
            }

            if (TryGetPointerRotation(device, out rotation))
            {
                position = EstimateControllerLocalPosition(node);
                usesAimPose = true;
                positionEstimated = true;
                return true;
            }

            var hasPosition = device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out position);
            var hasRotation = device.isValid && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out rotation);
            if (hasPosition && hasRotation && IsFinite(position) && IsFinite(rotation))
            {
                rotation = NormalizeQuaternion(rotation);
                usesAimPose = false;
                positionEstimated = false;
                return true;
            }

            if (hasRotation && IsFinite(rotation))
            {
                position = EstimateControllerLocalPosition(node);
                rotation = NormalizeQuaternion(rotation);
                usesAimPose = false;
                positionEstimated = true;
                return true;
            }

            var trackedPosition = InputTracking.GetLocalPosition(node);
            var trackedRotation = InputTracking.GetLocalRotation(node);
            var hasTrackedPosition = trackedPosition.sqrMagnitude > 0.0001f;
            var hasTrackedRotation = Quaternion.Dot(trackedRotation, Quaternion.identity) < 0.9999f;
            if (hasTrackedRotation && IsFinite(trackedRotation))
            {
                position = hasTrackedPosition ? trackedPosition : EstimateControllerLocalPosition(node);
                rotation = NormalizeQuaternion(trackedRotation);
                usesAimPose = false;
                positionEstimated = !hasTrackedPosition;
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            usesAimPose = false;
            positionEstimated = false;
            return false;
        }

        private bool TryReadHandInteractionPose(out Vector3 position, out Quaternion rotation)
        {
            position = handPointerPositionAction == null ? Vector3.zero : handPointerPositionAction.ReadValue<Vector3>();
            rotation = handPointerRotationAction == null ? Quaternion.identity : handPointerRotationAction.ReadValue<Quaternion>();
            if (!IsFinite(position) || !IsFinite(rotation))
                return false;

            rotation = NormalizeQuaternion(rotation);
            return position.sqrMagnitude > 0.0001f || Quaternion.Dot(rotation, Quaternion.identity) < 0.9999f;
        }

        private void LogMissingControllerPose()
        {
            if (missingControllerPoseLogged
                && Time.unscaledTime - lastControllerPoseDiagnosticLog < ControllerPoseDiagnosticInterval)
                return;

            lastControllerPoseDiagnosticLog = Time.unscaledTime;
            ControllerDevices.Clear();
            InputDevices.GetDevices(ControllerDevices);

            Debug.LogWarningFormat(
                "Quest XR no controller/hand pointer pose available; head-locked ray fallback is disabled. XRDevices={0}; InputSystemDevices={1}",
                DescribeXRDevices(ControllerDevices),
                DescribeInputSystemDevices());
            missingControllerPoseLogged = true;
        }

        private static string DescribeXRDevices(List<UnityEngine.XR.InputDevice> devices)
        {
            if (devices == null || devices.Count == 0)
                return "<none>";

            var names = new System.Text.StringBuilder();
            foreach (var device in devices)
            {
                if (names.Length > 0)
                    names.Append("; ");

                names.Append(device.name);
                names.Append(" [");
                names.Append(device.characteristics);
                names.Append("]");

                ControllerFeatureUsages.Clear();
                if (device.isValid && device.TryGetFeatureUsages(ControllerFeatureUsages) && ControllerFeatureUsages.Count > 0)
                {
                    names.Append(" features=");
                    for (var i = 0; i < ControllerFeatureUsages.Count; i += 1)
                    {
                        if (i > 0)
                            names.Append(",");
                        names.Append(ControllerFeatureUsages[i].name);
                        if (i >= 11 && ControllerFeatureUsages.Count > 12)
                        {
                            names.Append(",...");
                            break;
                        }
                    }
                }
            }

            return names.ToString();
        }

        private static string DescribeCurrentXRDevices()
        {
            ControllerDevices.Clear();
            InputDevices.GetDevices(ControllerDevices);
            return DescribeXRDevices(ControllerDevices);
        }

        private static string DescribeInputSystemDevices()
        {
            var names = new System.Text.StringBuilder();
            foreach (var device in InputSystem.devices)
            {
                if (device == null)
                    continue;
                if (names.Length > 0)
                    names.Append("; ");
                names.Append(device.layout);
                names.Append("/");
                names.Append(device.name);
                names.Append("/");
                names.Append(device.displayName);
                names.Append(" usages=");
                for (var i = 0; i < device.usages.Count; i += 1)
                {
                    if (i > 0)
                        names.Append(",");
                    names.Append(device.usages[i]);
                }
                names.Append(" controls=");
                var controlLimit = Mathf.Min(device.allControls.Count, 18);
                for (var i = 0; i < controlLimit; i += 1)
                {
                    if (i > 0)
                        names.Append(",");
                    names.Append(device.allControls[i].path);
                }
                if (device.allControls.Count > controlLimit)
                    names.Append(",...");
            }

            return names.Length == 0 ? "<none>" : names.ToString();
        }

        private static string DescribeInputSystemQuestDevices()
        {
            var names = new System.Text.StringBuilder();
            foreach (var device in InputSystem.devices)
            {
                if (device == null || !IsLikelyQuestInputSystemDevice(device))
                    continue;

                if (names.Length > 0)
                    names.Append("; ");
                names.Append(device.layout);
                names.Append("/");
                names.Append(device.name);
                names.Append("/");
                names.Append(device.displayName);
                names.Append(device.enabled ? "/enabled" : "/disabled");
                names.Append(" usages=");
                for (var i = 0; i < device.usages.Count; i += 1)
                {
                    if (i > 0)
                        names.Append(",");
                    names.Append(device.usages[i]);
                }
            }

            return names.Length == 0 ? "<none>" : names.ToString();
        }

        private static bool TryGetPointerPose(UnityEngine.XR.InputDevice device, out Vector3 position, out Quaternion rotation)
        {
            if (TryGetPoseFeatures(device, UpperAimPositionUsage, UpperAimRotationUsage, out position, out rotation))
                return true;

            if (TryGetPoseFeatures(device, AimPositionUsage, AimRotationUsage, out position, out rotation))
                return true;

            var hasUpperPosition = device.TryGetFeatureValue(PointerPositionUsage, out var upperPosition);
            var hasUpperRotation = device.TryGetFeatureValue(PointerRotationUsage, out var upperRotation);
            if (hasUpperPosition && hasUpperRotation && IsFinite(upperPosition) && IsFinite(upperRotation))
            {
                position = upperPosition;
                rotation = NormalizeQuaternion(upperRotation);
                return true;
            }

            var hasLowerPosition = device.TryGetFeatureValue(LowerPointerPositionUsage, out var lowerPosition);
            var hasLowerRotation = device.TryGetFeatureValue(LowerPointerRotationUsage, out var lowerRotation);
            if (hasLowerPosition && hasLowerRotation && IsFinite(lowerPosition) && IsFinite(lowerRotation))
            {
                position = lowerPosition;
                rotation = NormalizeQuaternion(lowerRotation);
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private static bool TryGetPoseFeatures(
            UnityEngine.XR.InputDevice device,
            InputFeatureUsage<Vector3> positionUsage,
            InputFeatureUsage<Quaternion> rotationUsage,
            out Vector3 position,
            out Quaternion rotation)
        {
            var hasPosition = device.TryGetFeatureValue(positionUsage, out position);
            var hasRotation = device.TryGetFeatureValue(rotationUsage, out rotation);
            if (hasPosition && hasRotation && IsFinite(position) && IsFinite(rotation))
            {
                rotation = NormalizeQuaternion(rotation);
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        private static bool TryGetPointerRotation(UnityEngine.XR.InputDevice device, out Quaternion rotation)
        {
            if (device.TryGetFeatureValue(UpperAimRotationUsage, out rotation) && IsFinite(rotation))
            {
                rotation = NormalizeQuaternion(rotation);
                return true;
            }

            if (device.TryGetFeatureValue(AimRotationUsage, out rotation) && IsFinite(rotation))
            {
                rotation = NormalizeQuaternion(rotation);
                return true;
            }

            if (device.TryGetFeatureValue(PointerRotationUsage, out rotation) && IsFinite(rotation))
            {
                rotation = NormalizeQuaternion(rotation);
                return true;
            }

            if (device.TryGetFeatureValue(LowerPointerRotationUsage, out rotation) && IsFinite(rotation))
            {
                rotation = NormalizeQuaternion(rotation);
                return true;
            }

            rotation = Quaternion.identity;
            return false;
        }

        private static Vector3 EstimateControllerLocalPosition(XRNode node)
        {
            var headPosition = Vector3.zero;
            var headRotation = Quaternion.identity;
            TryReadHeadPose(out headPosition, out headRotation);

            var x = node == XRNode.LeftHand ? -0.28f : 0.28f;
            var offset = new Vector3(x, -0.18f, 0.42f);
            return headPosition + headRotation * offset;
        }

        private bool IsRightControllerTriggerPressed()
        {
            if (pointerPressAction != null && pointerPressAction.IsPressed())
                return true;
            if (handPointerPressAction != null && handPointerPressAction.IsPressed())
                return true;

            return IsControllerTriggerPressed(XRNode.RightHand) || IsControllerTriggerPressed(XRNode.LeftHand);
        }

        private static bool IsControllerTriggerPressed(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (IsControllerTriggerPressed(device))
                return true;

            var hand = node == XRNode.LeftHand
                ? InputDeviceCharacteristics.Left
                : InputDeviceCharacteristics.Right;
            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller | hand,
                ControllerDevices);
            foreach (var candidate in ControllerDevices)
            {
                if (IsControllerTriggerPressed(candidate))
                    return true;
            }

            ControllerDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(hand, ControllerDevices);
            foreach (var candidate in ControllerDevices)
            {
                if (IsControllerTriggerPressed(candidate))
                    return true;
            }

            return false;
        }

        private static bool IsControllerTriggerPressed(UnityEngine.XR.InputDevice device)
        {
            if (!device.isValid)
                return false;

            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var triggerButton) && triggerButton)
                return true;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out var trigger) && trigger > 0.55f)
                return true;
            return false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w) &&
                value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.0001f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static Quaternion NormalizeQuaternion(Quaternion value)
        {
            var length = Mathf.Sqrt(value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w);
            return length <= 0.0001f
                ? Quaternion.identity
                : new Quaternion(value.x / length, value.y / length, value.z / length, value.w / length);
        }

        private Vector2 ResolvePointerScreenPosition(Ray ray, bool includeUiPanel = true)
        {
            if (includeUiPanel && TryGetUiRenderPanelHit(ray, out var uiPanelScreenPosition, out _))
                return uiPanelScreenPosition;

            if (TryGetCanvasHit(ray, out _, out var canvasScreenPosition, out _))
                return canvasScreenPosition;

            if (TryGetWorldHit(ray, out var hitPoint, out _))
            {
                var mainCamera = Program.instance?.camera_?.cameraMain;
                if (mainCamera != null)
                {
                    var mainScreen = mainCamera.WorldToScreenPoint(hitPoint);
                    if (mainScreen.z > 0f)
                        return new Vector2(mainScreen.x, mainScreen.y);
                }
            }

            var projected = xrCamera.WorldToScreenPoint(ray.origin + ray.direction * 3f);
            if (projected.z > 0f)
                return new Vector2(projected.x, projected.y);

            return new Vector2(-1000f, -1000f);
        }

        private bool TryGetCanvasHit(Ray ray, out Vector3 hitPoint, out Vector2 screenPosition, out float distance)
        {
            hitPoint = default;
            screenPosition = default;
            distance = float.PositiveInfinity;

            foreach (var canvas in configuredWorldCanvases)
            {
                if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    continue;

                var rect = canvas.transform as RectTransform;
                if (rect == null)
                    continue;

                var plane = new Plane(rect.forward, rect.position);
                if (!plane.Raycast(ray, out var hitDistance) || hitDistance < 0f || hitDistance >= distance)
                    continue;

                var candidatePoint = ray.GetPoint(hitDistance);
                if (!TryGetCanvasScreenPoint(rect, candidatePoint, out var candidateScreen))
                    continue;

                hitPoint = candidatePoint;
                screenPosition = candidateScreen;
                distance = hitDistance;
            }

            return distance < float.PositiveInfinity;
        }

        private bool TryGetCanvasScreenPoint(RectTransform rect, Vector3 worldPoint, out Vector2 screenPosition)
        {
            screenPosition = default;
            if (rect == null)
                return false;

            var localPoint = rect.InverseTransformPoint(worldPoint);
            var width = Mathf.Max(rect.rect.width, rect.sizeDelta.x, 1f);
            var height = Mathf.Max(rect.rect.height, rect.sizeDelta.y, 1f);
            if (localPoint.x < -width * 0.5f || localPoint.x > width * 0.5f
                || localPoint.y < -height * 0.5f || localPoint.y > height * 0.5f)
                return false;

            if (xrCamera != null)
            {
                var projected = xrCamera.WorldToScreenPoint(worldPoint);
                if (projected.z > 0f && IsFinite(projected))
                {
                    screenPosition = new Vector2(projected.x, projected.y);
                    return true;
                }
            }

            screenPosition = new Vector2(localPoint.x + width * 0.5f, localPoint.y + height * 0.5f);
            return true;
        }

        private bool TryGetUiRenderPanelHit(Ray ray, out Vector2 screenPosition, out float distance)
        {
            screenPosition = default;
            distance = float.PositiveInfinity;

            if (uiRenderPanel == null || !uiRenderPanel.activeInHierarchy || uiRenderTexture == null)
                return false;

            var panelTransform = uiRenderPanel.transform;
            var plane = new Plane(panelTransform.forward, panelTransform.position);
            if (!plane.Raycast(ray, out var hitDistance) || hitDistance < 0f)
                return false;

            var localPoint = panelTransform.InverseTransformPoint(ray.GetPoint(hitDistance));
            if (Mathf.Abs(localPoint.x) > 0.5f || Mathf.Abs(localPoint.y) > 0.5f)
                return false;

            var width = uiRenderTextureWidth > 0 ? uiRenderTextureWidth : Screen.width;
            var height = uiRenderTextureHeight > 0 ? uiRenderTextureHeight : Screen.height;
            var u = Mathf.Clamp01(localPoint.x + 0.5f);
            var v = Mathf.Clamp01(localPoint.y + 0.5f);
            if (UiRenderTextureNeedsHorizontalFlip)
                u = 1f - u;
            if (UiRenderTextureNeedsVerticalFlip)
                v = 1f - v;
            screenPosition = new Vector2(u * width, v * height);
            distance = hitDistance;
            if (QuestVerboseRuntimeDiagnostics && Time.unscaledTime - lastUiRenderPanelHitDiagnosticsLog > 3f)
            {
                lastUiRenderPanelHitDiagnosticsLog = Time.unscaledTime;
                Debug.LogFormat(
                    "Quest XR UI panel hit diagnostics. Local={0}, UV=({1:F3},{2:F3}), Screen=({3:F0},{4:F0}), Distance={5:F2}, HorizontalFlip={6}, VerticalFlip={7}",
                    localPoint,
                    u,
                    v,
                    screenPosition.x,
                    screenPosition.y,
                    distance,
                    UiRenderTextureNeedsHorizontalFlip,
                    UiRenderTextureNeedsVerticalFlip);
            }
            return true;
        }

        private bool TryGetPanelUiHit(
            Vector2 panelScreenPosition,
            out RaycastResult raycast,
            out GameObject target,
            out Vector2 resolvedScreenPosition)
        {
            if (TryGetScreenSpaceUiHit(panelScreenPosition, out raycast, out target))
            {
                resolvedScreenPosition = panelScreenPosition;
                return true;
            }

            var scaledScreenPosition = ScalePanelPositionToScreen(panelScreenPosition);
            if ((scaledScreenPosition - panelScreenPosition).sqrMagnitude > 0.25f
                && TryGetScreenSpaceUiHit(scaledScreenPosition, out raycast, out target))
            {
                resolvedScreenPosition = scaledScreenPosition;
                return true;
            }

            resolvedScreenPosition = panelScreenPosition;
            raycast = default;
            target = null;
            return false;
        }

        private Vector2 ScalePanelPositionToScreen(Vector2 panelScreenPosition)
        {
            var sourceWidth = uiRenderTextureWidth > 0 ? uiRenderTextureWidth : Screen.width;
            var sourceHeight = uiRenderTextureHeight > 0 ? uiRenderTextureHeight : Screen.height;
            var targetWidth = Screen.width > 0 ? Screen.width : sourceWidth;
            var targetHeight = Screen.height > 0 ? Screen.height : sourceHeight;

            if (sourceWidth <= 0 || sourceHeight <= 0)
                return panelScreenPosition;

            return new Vector2(
                panelScreenPosition.x / sourceWidth * targetWidth,
                panelScreenPosition.y / sourceHeight * targetHeight);
        }

        private bool EnsureQuestPointerEventData(EventSystem eventSystem)
        {
            if (eventSystem == null)
                return false;

            if (questPointerEventData == null || questPointerEventSystem != eventSystem)
            {
                questPointerEventSystem = eventSystem;
                questPointerEventData = new PointerEventData(eventSystem)
                {
                    pointerId = -907,
                    button = PointerEventData.InputButton.Left
                };
            }

            return true;
        }

        private void UpdateDirectUiPointer(GameObject currentUi, RaycastResult raycast, Vector2 screenPosition, bool pressed)
        {
            try
            {
                UpdateDirectUiPointerUnsafe(currentUi, raycast, screenPosition, pressed);
            }
            catch (Exception ex)
            {
                ResetDirectUiPointerState();
                if (Time.unscaledTime - lastDirectUiPointerExceptionLog > 2f)
                {
                    Debug.LogWarning("Quest XR direct UI pointer recovered from exception: " + ex.Message);
                    lastDirectUiPointerExceptionLog = Time.unscaledTime;
                }
            }
        }

        private void UpdateDirectUiPointerUnsafe(GameObject currentUi, RaycastResult raycast, Vector2 screenPosition, bool pressed)
        {
            var eventSystem = EventSystem.current;
            if (!EnsureQuestPointerEventData(eventSystem))
                return;

            questPointerEventData.Reset();
            questPointerEventData.pointerId = -907;
            questPointerEventData.button = PointerEventData.InputButton.Left;
            questPointerEventData.position = screenPosition;
            questPointerEventData.delta = hasLastQuestDirectPointerPosition
                ? screenPosition - lastQuestDirectPointerPosition
                : Vector2.zero;
            questPointerEventData.scrollDelta = Vector2.zero;
            questPointerEventData.useDragThreshold = true;
            questPointerEventData.pointerCurrentRaycast = raycast;
            lastQuestDirectPointerPosition = screenPosition;
            hasLastQuestDirectPointerPosition = true;

            if (questHoveredUi != null && !questHoveredUi.activeInHierarchy)
                questHoveredUi = null;
            if (questPressedUi != null && !questPressedUi.activeInHierarchy)
                questPressedUi = null;

            if (currentUi != questHoveredUi)
            {
                if (questHoveredUi != null)
                    ExecuteEvents.ExecuteHierarchy(questHoveredUi, questPointerEventData, ExecuteEvents.pointerExitHandler);
                if (currentUi != null)
                    ExecuteEvents.ExecuteHierarchy(currentUi, questPointerEventData, ExecuteEvents.pointerEnterHandler);
                questHoveredUi = currentUi;
            }

            if (pressed && !lastQuestPointerPressed && currentUi != null)
            {
                questPointerEventData.pressPosition = screenPosition;
                questPointerEventData.pointerPressRaycast = raycast;
                questPointerEventData.eligibleForClick = true;
                questPointerEligibleForClick = true;
                questPointerEventData.rawPointerPress = currentUi;
                questPointerEventData.pointerPress = ExecuteEvents.ExecuteHierarchy(currentUi, questPointerEventData, ExecuteEvents.pointerDownHandler)
                    ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentUi);
                questPressedUi = questPointerEventData.pointerPress;
                questPressedDuelAction = ResolveQuestDuelAction(currentUi);
                questPressedDuelActionUi = questPressedDuelAction == null ? null : currentUi;
                var selected = ExecuteEvents.GetEventHandler<ISelectHandler>(currentUi);
                if (selected != null)
                    eventSystem.SetSelectedGameObject(selected, questPointerEventData);
                if (questPressedUi != null && questPressedUi != lastLoggedQuestPressedUi)
                {
                    Debug.LogFormat("Quest XR UI press: {0} screen=({1:F0},{2:F0})",
                        GetTransformPath(questPressedUi.transform),
                        screenPosition.x,
                        screenPosition.y);
                    lastLoggedQuestPressedUi = questPressedUi;
                }

                if (!directUiInputLogged)
                {
                    Debug.Log("Quest XR direct UI pointer enabled.");
                    directUiInputLogged = true;
                }
            }

            if (!pressed && lastQuestPointerPressed)
            {
                var lockedDuelAction = questPressedDuelAction;
                var canExecuteLockedDuelAction = questPointerEligibleForClick && lockedDuelAction != null;
                if (questPressedUi != null)
                {
                    questPointerEventData.pointerPress = questPressedUi;
                    questPointerEventData.eligibleForClick = questPointerEligibleForClick;
                    ExecuteEvents.Execute(questPressedUi, questPointerEventData, ExecuteEvents.pointerUpHandler);

                    if (questPointerEligibleForClick && !canExecuteLockedDuelAction)
                    {
                        ExecuteEvents.Execute(questPressedUi, questPointerEventData, ExecuteEvents.pointerClickHandler);
                        if (questPressedUi != lastLoggedQuestClickedUi)
                        {
                            Debug.LogFormat("Quest XR UI click: {0} screen=({1:F0},{2:F0})",
                                GetTransformPath(questPressedUi.transform),
                                screenPosition.x,
                                screenPosition.y);
                            lastLoggedQuestClickedUi = questPressedUi;
                        }
                    }
                }

                questPointerEventData.eligibleForClick = false;
                questPointerEligibleForClick = false;
                questPointerEventData.pointerPress = null;
                questPointerEventData.rawPointerPress = null;
                questPressedUi = null;
                questPressedDuelActionUi = null;
                questPressedDuelAction = null;

                if (canExecuteLockedDuelAction)
                {
                    Debug.LogFormat("Quest XR duel action release executes locked menu item: {0}",
                        GetQuestDuelActionLabel(lockedDuelAction));
                    lastQuestPointerPressed = false;
                    ExecuteQuestDuelAction(lockedDuelAction);
                    return;
                }
            }

            lastQuestPointerPressed = pressed;
        }

        private void ResetDirectUiPointerState()
        {
            questHoveredUi = null;
            questPressedUi = null;
            questPressedDuelActionUi = null;
            questPressedDuelAction = null;
            questPointerEligibleForClick = false;
            lastQuestPointerPressed = false;
            hasLastQuestDirectPointerPosition = false;
            if (questPointerEventData != null)
            {
                questPointerEventData.eligibleForClick = false;
                questPointerEventData.pointerPress = null;
                questPointerEventData.rawPointerPress = null;
            }
        }

        private void LogQuestPointerStatus(
            bool hasControllerPose,
            bool pressed,
            bool hasUiHit,
            GameObject currentUi,
            Vector2 screenPosition,
            float uiDistance)
        {
            if (!QuestVerboseRuntimeDiagnostics)
                return;

            var now = Time.unscaledTime;
            if (!pressed && now - lastQuestPointerStatusLog < 3f)
                return;
            if (pressed && now - lastQuestPointerStatusLog < 0.35f)
                return;

            lastQuestPointerStatusLog = now;
            Debug.LogFormat(
                "Quest XR pointer status: controllerPose={0}, pressed={1}, panelOrUiHit={2}, target={3}, screen=({4:F0},{5:F0}), distance={6:F2}",
                hasControllerPose,
                pressed,
                hasUiHit,
                currentUi == null ? "<none>" : GetTransformPath(currentUi.transform),
                screenPosition.x,
                screenPosition.y,
                uiDistance);
        }

        private bool TryGetControllerUiHit(Ray ray, out Vector2 screenPosition, out RaycastResult raycast, out GameObject target, out float distance)
        {
            screenPosition = default;
            raycast = default;
            target = null;
            distance = float.PositiveInfinity;

            if (!EnsureQuestPointerEventData(EventSystem.current))
                return false;

            foreach (var raycaster in GetCachedGraphicRaycasters())
            {
                if (raycaster == null || !raycaster.enabled || !raycaster.gameObject.activeInHierarchy)
                    continue;

                if (!TryGetRaycasterScreenPosition(raycaster, ray, out var candidateScreenPosition, out var candidateDistance))
                    continue;

                var candidateTarget = RaycastUi(raycaster, candidateScreenPosition, out var candidateRaycast);
                if (candidateTarget == null)
                    continue;

                if (target == null || IsBetterUiHit(candidateDistance, candidateRaycast, distance, raycast))
                {
                    screenPosition = candidateScreenPosition;
                    raycast = candidateRaycast;
                    target = candidateTarget;
                    distance = candidateDistance;
                }
            }

            return target != null;
        }

        private bool TryGetScreenSpaceUiHit(Vector2 screenPosition, out RaycastResult raycast, out GameObject target)
        {
            raycast = default;
            target = null;

            if (!EnsureQuestPointerEventData(EventSystem.current))
                return false;

            foreach (var raycaster in GetCachedGraphicRaycasters())
            {
                if (raycaster == null || !raycaster.enabled || !raycaster.gameObject.activeInHierarchy)
                    continue;

                var candidateTarget = RaycastUi(raycaster, screenPosition, out var candidateRaycast);
                if (candidateTarget == null)
                    continue;

                if (target == null || IsBetterScreenSpaceUiHit(candidateRaycast, raycast))
                {
                    raycast = candidateRaycast;
                    target = candidateTarget;
                }
            }

            if (QuestVerboseRuntimeDiagnostics && target != null && target != lastLoggedQuestUi)
            {
                Debug.LogFormat("Quest XR UI hit: {0} screen=({1:F0},{2:F0})",
                    GetTransformPath(target.transform),
                    screenPosition.x,
                    screenPosition.y);
                lastLoggedQuestUi = target;
            }

            return target != null;
        }

        private List<GraphicRaycaster> GetCachedGraphicRaycasters()
        {
            var now = Time.unscaledTime;
            if (cachedGraphicRaycasters.Count == 0 || now >= nextGraphicRaycasterCacheRefreshTime)
                RefreshGraphicRaycasterCache(now);
            return cachedGraphicRaycasters;
        }

        private void InvalidateGraphicRaycasterCache()
        {
            cachedGraphicRaycasters.Clear();
            nextGraphicRaycasterCacheRefreshTime = 0f;
        }

        private void HandleQuestNativeUiVisibilityChanged()
        {
            InvalidateGraphicRaycasterCache();
            if (questDuelNativeUi != null && questDuelNativeUi.HasBlockingPanel)
            {
                questDuelWorldPresenter?.SetHoveredCard(null);
                questInfoHoverCard = null;
                questInfoHoverShown = false;
                ClearQuestDuelActionMenu();
            }
        }

        private void RefreshGraphicRaycasterCache(float now)
        {
            cachedGraphicRaycasters.Clear();
            var raycasters = FindObjectsOfType<GraphicRaycaster>();
            foreach (var raycaster in raycasters)
            {
                if (raycaster != null)
                    cachedGraphicRaycasters.Add(raycaster);
            }
            nextGraphicRaycasterCacheRefreshTime = now + QuestGraphicRaycasterCacheRefreshInterval;
        }

        private bool TryGetRaycasterScreenPosition(GraphicRaycaster raycaster, Ray ray, out Vector2 screenPosition, out float distance)
        {
            screenPosition = default;
            distance = float.PositiveInfinity;

            var canvas = raycaster.GetComponent<Canvas>();
            var rootCanvas = canvas == null ? null : canvas.rootCanvas;
            if (rootCanvas == null || rootCanvas.renderMode != RenderMode.WorldSpace)
                return false;

            if (canvas != null && canvas.worldCamera != xrCamera)
                canvas.worldCamera = xrCamera;
            if (rootCanvas.worldCamera != xrCamera)
                rootCanvas.worldCamera = xrCamera;

            return TryGetCanvasPlaneScreenPosition(rootCanvas, ray, out screenPosition, out distance);
        }

        private bool TryGetCanvasPlaneScreenPosition(Canvas canvas, Ray ray, out Vector2 screenPosition, out float distance)
        {
            screenPosition = default;
            distance = float.PositiveInfinity;

            if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy || xrCamera == null)
                return false;

            var rect = canvas.transform as RectTransform;
            if (rect == null)
                return false;

            var plane = new Plane(rect.forward, rect.position);
            if (!plane.Raycast(ray, out var hitDistance) || hitDistance < 0f)
                return false;

            var candidatePoint = ray.GetPoint(hitDistance);
            if (!TryGetCanvasScreenPoint(rect, candidatePoint, out var candidateScreen))
                return false;

            screenPosition = candidateScreen;
            distance = hitDistance;
            return true;
        }

        private bool TryGetAnyCanvasPlaneDistance(Ray ray, out float distance)
        {
            distance = float.PositiveInfinity;
            foreach (var canvas in configuredWorldCanvases)
            {
                if (!TryGetCanvasPlaneScreenPosition(canvas, ray, out _, out var candidateDistance))
                    continue;
                if (candidateDistance < distance)
                    distance = candidateDistance;
            }

            return distance < float.PositiveInfinity;
        }

        private static bool IsBetterUiHit(float distance, RaycastResult result, float currentDistance, RaycastResult current)
        {
            if (distance < currentDistance - 0.001f)
                return true;
            if (Mathf.Abs(distance - currentDistance) > 0.001f)
                return false;
            if (result.sortingOrder != current.sortingOrder)
                return result.sortingOrder > current.sortingOrder;
            return result.depth > current.depth;
        }

        private static bool IsBetterScreenSpaceUiHit(RaycastResult result, RaycastResult current)
        {
            if (result.sortingLayer != current.sortingLayer)
                return result.sortingLayer > current.sortingLayer;
            if (result.sortingOrder != current.sortingOrder)
                return result.sortingOrder > current.sortingOrder;
            if (result.depth != current.depth)
                return result.depth > current.depth;
            return result.index < current.index;
        }

        private GameObject RaycastUi(GraphicRaycaster raycaster, Vector2 screenPosition, out RaycastResult raycast)
        {
            raycast = default;
            uiRaycastResults.Clear();
            questPointerEventData.Reset();
            questPointerEventData.pointerId = -907;
            questPointerEventData.button = PointerEventData.InputButton.Left;
            questPointerEventData.position = screenPosition;
            raycaster.Raycast(questPointerEventData, uiRaycastResults);

            foreach (var result in uiRaycastResults)
            {
                if (result.gameObject == null || !result.gameObject.activeInHierarchy)
                    continue;
                var target = ResolveInteractiveUiTarget(result.gameObject);
                if (target == null)
                    continue;

                raycast = result;
                return target;
            }

            return null;
        }

        private static GameObject ResolveInteractiveUiTarget(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            return ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IPointerDownHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IPointerUpHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<ISelectHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IScrollHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IBeginDragHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IDragHandler>(gameObject)
                ?? ExecuteEvents.GetEventHandler<IEndDragHandler>(gameObject);
        }

        private bool TryGetWorldHit(Ray ray, out Vector3 hitPoint, out float distance)
        {
            distance = float.PositiveInfinity;
            var hitCount = RaycastNonAllocSorted(ray);
            if (hitCount > 0)
            {
                for (var i = 0; i < hitCount; i++)
                {
                    var hit = questRaycastHits[i];
                    if (hit.collider == null)
                        continue;

                    hitPoint = hit.point;
                    distance = hit.distance;
                    return true;
                }
            }

            hitPoint = default;
            return false;
        }

        private int RaycastNonAllocSorted(Ray ray)
        {
            var hitCount = Physics.RaycastNonAlloc(ray, questRaycastHits, GetQuestControllerRayLength());
            if (hitCount > 1)
                System.Array.Sort(questRaycastHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            return hitCount;
        }

        private float GetQuestControllerRayLength()
        {
            var scale = IsQuestNativeDuelActive()
                ? Mathf.Max(questDuelWorldScaleMultiplier, 1f)
                : 1f;
            return ControllerRayLength * scale;
        }

        private void EnsureControllerRayVisual()
        {
            if (controllerRayLine != null)
                return;

            controllerRayMaterial = CreateColorMaterial(
                "Quest Controller Ray Material",
                new Color(0.35f, 0.9f, 1f, 0.85f),
                true);
            ConfigureAlwaysVisibleOverlayMaterial(controllerRayMaterial);

            var rayObject = new GameObject("Quest Right Controller Ray");
            SetQuestOverlayLayer(rayObject);
            controllerRayLine = rayObject.AddComponent<LineRenderer>();
            controllerRayLine.positionCount = 2;
            controllerRayLine.useWorldSpace = true;
            controllerRayLine.startWidth = ControllerRayStartWidth;
            controllerRayLine.endWidth = ControllerRayEndWidth;
            controllerRayLine.numCapVertices = 4;
            controllerRayLine.alignment = LineAlignment.View;
            controllerRayLine.material = controllerRayMaterial;
            controllerRayLine.startColor = new Color(0.35f, 0.9f, 1f, 0.85f);
            controllerRayLine.endColor = new Color(0.35f, 0.9f, 1f, 0.15f);
            controllerRayLine.enabled = false;

            controllerRayCursorMaterial = CreateColorMaterial(
                "Quest Controller Cursor Material",
                new Color(1f, 0.78f, 0.25f, 0.9f),
                true);
            ConfigureAlwaysVisibleOverlayMaterial(controllerRayCursorMaterial);

            var cursorObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursorObject.name = "Quest Right Controller Cursor";
            SetQuestOverlayLayer(cursorObject);
            var collider = cursorObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            controllerRayCursor = cursorObject.transform;
            controllerRayCursor.localScale = Vector3.one * ControllerCursorScale;
            controllerRayCursorRenderer = cursorObject.GetComponent<MeshRenderer>();
            if (controllerRayCursorRenderer != null)
            {
                controllerRayCursorRenderer.sharedMaterial = controllerRayCursorMaterial;
                controllerRayCursorRenderer.shadowCastingMode = ShadowCastingMode.Off;
                controllerRayCursorRenderer.receiveShadows = false;
                controllerRayCursorRenderer.enabled = false;
            }
        }

        private void UpdateControllerRayVisual(Ray ray, bool hasControllerPose, bool hasUiHit, float uiDistance)
        {
            EnsureControllerRayVisual();
            if (controllerRayLine == null || !hasControllerPose)
            {
                SetControllerRayVisible(false, false);
                return;
            }

            var hasHit = hasUiHit;
            if (TryGetWorldHit(ray, out _, out var worldDistance) && worldDistance < uiDistance)
            {
                uiDistance = worldDistance;
                hasHit = true;
            }

            var rayLength = GetQuestControllerRayLength();
            var distance = hasHit ? uiDistance : rayLength;
            var end = ray.origin + ray.direction.normalized * Mathf.Clamp(distance, 0.2f, rayLength);
            controllerRayLine.enabled = true;
            controllerRayLine.SetPosition(0, ray.origin);
            controllerRayLine.SetPosition(1, end);

            var startColor = hasHit
                ? new Color(1f, 0.78f, 0.25f, 0.95f)
                : hasControllerPose
                    ? new Color(0.35f, 0.9f, 1f, 0.85f)
                    : new Color(0.85f, 0.95f, 1f, 0.45f);
            var endColor = hasHit
                ? new Color(1f, 0.78f, 0.25f, 0.35f)
                : hasControllerPose
                    ? new Color(0.35f, 0.9f, 1f, 0.15f)
                    : new Color(0.85f, 0.95f, 1f, 0.08f);
            controllerRayLine.startColor = startColor;
            controllerRayLine.endColor = endColor;
            ApplyMaterialColor(controllerRayMaterial, startColor);

            if (controllerRayCursor != null)
                controllerRayCursor.position = end;
            if (controllerRayCursorRenderer != null)
                controllerRayCursorRenderer.enabled = hasHit;
        }

        private void SetControllerRayVisible(bool lineVisible, bool cursorVisible)
        {
            if (controllerRayLine != null)
                controllerRayLine.enabled = lineVisible;
            if (controllerRayCursorRenderer != null)
                controllerRayCursorRenderer.enabled = cursorVisible;
        }

        private static void ApplyMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void ConfigureAlwaysVisibleOverlayMaterial(Material material)
        {
            if (material == null)
                return;

            material.renderQueue = 5000;
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_ZTest"))
                material.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
        }

        private static void ConfigureUiRenderPanelMaterial(Material material)
        {
            if (material == null)
                return;

            ConfigureAlwaysVisibleOverlayMaterial(material);
            material.renderQueue = 4990;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_ZTest"))
                material.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private static void ConfigureUiRenderPanelBackdropMaterial(Material material)
        {
            if (material == null)
                return;

            ConfigureAlwaysVisibleOverlayMaterial(material);
            material.renderQueue = 4980;
            var color = new Color(0.006f, 0.010f, 0.014f, 1f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_ZTest"))
                material.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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
            var transform = gameObject.transform;
            for (var i = 0; i < transform.childCount; i += 1)
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        private sealed class QuestDuelActionButtonFloat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private RectTransform rect;
            private Vector2 basePosition;
            private float phase;
            private float amplitude;
            private float hoverScaleBoost;
            private float hoverBlend;
            private bool hovered;
            private Action hoverEnterAction;
            private Action hoverExitAction;

            public void Configure(
                Vector2 anchoredPosition,
                float phaseOffset,
                float bobAmplitude,
                float scaleBoost,
                Action onHoverEnter = null,
                Action onHoverExit = null)
            {
                rect = transform as RectTransform;
                basePosition = anchoredPosition;
                phase = phaseOffset;
                amplitude = Mathf.Max(0f, bobAmplitude);
                hoverScaleBoost = Mathf.Max(0f, scaleBoost);
                hoverEnterAction = onHoverEnter;
                hoverExitAction = onHoverExit;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                hovered = true;
                hoverEnterAction?.Invoke();
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                hovered = false;
                hoverExitAction?.Invoke();
            }

            private void Update()
            {
                if (rect == null)
                    rect = transform as RectTransform;
                if (rect == null)
                    return;

                hoverBlend = Mathf.MoveTowards(hoverBlend, hovered ? 1f : 0f, Time.unscaledDeltaTime * 8.5f);
                var bob = Mathf.Sin(Time.unscaledTime * 2.2f + phase) * amplitude;
                rect.anchoredPosition = basePosition + Vector2.up * (bob + hoverBlend * amplitude * 0.85f);
                rect.localScale = Vector3.one * (1f + hoverBlend * hoverScaleBoost);
            }
        }

        private void QueueVirtualMouse(Vector2 screenPosition, bool pressed)
        {
            EnsureVirtualMouse();

            var mouseState = new MouseState
            {
                position = screenPosition,
                delta = screenPosition - lastQueuedMousePos
            }.WithButton(MouseButton.Left, pressed);

            InputSystem.QueueStateEvent(virtualMouse, mouseState);
            lastQueuedMousePos = screenPosition;
        }
    }
}
