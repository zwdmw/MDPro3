using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using MDPro3.YGOSharp;
using MDPro3.YGOSharp.OCGWrapper.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace MDPro3
{
    public enum DuelPresentationKind
    {
        CardMoved,
        CardSet,
        CardSummoned,
        CardActivated,
        ChainStacked,
        CardDestroyed,
        CardSentToGrave,
        CardBanished,
        AttackDeclared,
        AttackImpact,
        Damage,
        Recover,
        PhaseChanged
    }

    public enum DuelPresentationMoveKind
    {
        Generic,
        Draw,
        Set,
        ToField,
        ToHand,
        ToGrave,
        Banished,
        Destroyed,
        Released,
        Material,
        Overlay
    }

    public enum DuelPresentationWeight
    {
        Light,
        Medium,
        Heavy,
        Finisher
    }

    public sealed class DuelPresentationEvent
    {
        public DuelPresentationKind Kind;
        public DuelPresentationMoveKind MoveKind;
        public DuelPresentationWeight Weight;
        public GameCard Card;
        public GameCard TargetCard;
        public GPS From;
        public GPS To;
        public int Controller;
        public int Value;
        public int ChainIndex;
        public bool Direct;
        public bool Final;
        public DuelPhase Phase;
        public uint Reason;
    }

    public static class DuelPresentationDirector
    {
        public static event Action<DuelPresentationEvent> EventRaised;

        public static void NotifyCardMoved(GameCard card, GPS from, GPS to, GameMessage message)
        {
            if (card == null || from == null || to == null)
                return;

            var moveKind = ResolveMoveKind(from, to, message);
            var weight = ResolveMoveWeight(card, from, to, moveKind);
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.CardMoved,
                MoveKind = moveKind,
                Weight = weight,
                Card = card,
                From = CloneGps(from),
                To = CloneGps(to),
                Controller = (int)to.controller,
                Reason = to.reason
            });

            if ((to.reason & (uint)CardReason.DESTROY) > 0)
                NotifyCardDestroyed(card, weight, from, to);

            if ((to.location & (uint)CardLocation.Grave) > 0)
                NotifyCardSentToGrave(card, weight, from, to);
            else if ((to.location & (uint)CardLocation.Removed) > 0)
                NotifyCardBanished(card, weight, from, to);
        }

        public static void NotifyCardSet(GameCard card)
        {
            RaiseForCard(DuelPresentationKind.CardSet, card, DuelPresentationWeight.Medium);
        }

        public static void NotifyCardSummoned(GameCard card, bool special, bool strong)
        {
            RaiseForCard(
                DuelPresentationKind.CardSummoned,
                card,
                strong ? DuelPresentationWeight.Heavy : special ? DuelPresentationWeight.Medium : DuelPresentationWeight.Light);
        }

        public static void NotifyCardActivated(GameCard card, int chainIndex)
        {
            if (card == null)
                return;

            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.CardActivated,
                Weight = chainIndex >= 3 ? DuelPresentationWeight.Heavy : DuelPresentationWeight.Medium,
                Card = card,
                Controller = card.p == null ? 0 : (int)card.p.controller,
                ChainIndex = chainIndex
            });
        }

        public static void NotifyChainStacked(GameCard card, int chainIndex)
        {
            if (card == null)
                return;

            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.ChainStacked,
                Weight = chainIndex >= 3 ? DuelPresentationWeight.Heavy : DuelPresentationWeight.Medium,
                Card = card,
                Controller = card.p == null ? 0 : (int)card.p.controller,
                ChainIndex = chainIndex
            });
        }

        public static void NotifyAttackDeclared(GameCard attacker, GameCard target, bool direct, bool final)
        {
            if (attacker == null)
                return;

            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.AttackDeclared,
                Weight = final ? DuelPresentationWeight.Finisher : ResolveAttackWeight(attacker),
                Card = attacker,
                TargetCard = target,
                Controller = attacker.p == null ? 0 : (int)attacker.p.controller,
                Direct = direct,
                Final = final
            });
        }

        public static void NotifyAttackImpact(GameCard attacker, GameCard target, bool direct, bool final)
        {
            if (attacker == null)
                return;

            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.AttackImpact,
                Weight = final ? DuelPresentationWeight.Finisher : ResolveAttackWeight(attacker),
                Card = attacker,
                TargetCard = target,
                Controller = attacker.p == null ? 0 : (int)attacker.p.controller,
                Direct = direct,
                Final = final
            });
        }

        public static void NotifyDamage(int player, int value, int remainingLife, bool final)
        {
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.Damage,
                Weight = final ? DuelPresentationWeight.Finisher : value >= 2000 ? DuelPresentationWeight.Heavy : DuelPresentationWeight.Medium,
                Controller = player,
                Value = value,
                Final = final
            });
        }

        public static void NotifyRecover(int player, int value)
        {
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.Recover,
                Weight = value >= 2000 ? DuelPresentationWeight.Medium : DuelPresentationWeight.Light,
                Controller = player,
                Value = value
            });
        }

        public static void NotifyPhaseChanged(int player, DuelPhase phase)
        {
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.PhaseChanged,
                Weight = phase == DuelPhase.BattleStart || phase == DuelPhase.Battle ? DuelPresentationWeight.Medium : DuelPresentationWeight.Light,
                Controller = player,
                Phase = phase
            });
        }

        private static void NotifyCardDestroyed(GameCard card, DuelPresentationWeight weight, GPS from, GPS to)
        {
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.CardDestroyed,
                MoveKind = DuelPresentationMoveKind.Destroyed,
                Weight = weight < DuelPresentationWeight.Heavy ? DuelPresentationWeight.Heavy : weight,
                Card = card,
                From = CloneGps(from),
                To = CloneGps(to),
                Controller = to == null ? 0 : (int)to.controller,
                Reason = to == null ? 0 : to.reason
            });
        }

        private static void NotifyCardSentToGrave(GameCard card, DuelPresentationWeight weight, GPS from, GPS to)
        {
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.CardSentToGrave,
                MoveKind = DuelPresentationMoveKind.ToGrave,
                Weight = weight,
                Card = card,
                From = CloneGps(from),
                To = CloneGps(to),
                Controller = to == null ? 0 : (int)to.controller,
                Reason = to == null ? 0 : to.reason
            });
        }

        private static void NotifyCardBanished(GameCard card, DuelPresentationWeight weight, GPS from, GPS to)
        {
            Raise(new DuelPresentationEvent
            {
                Kind = DuelPresentationKind.CardBanished,
                MoveKind = DuelPresentationMoveKind.Banished,
                Weight = weight < DuelPresentationWeight.Medium ? DuelPresentationWeight.Medium : weight,
                Card = card,
                From = CloneGps(from),
                To = CloneGps(to),
                Controller = to == null ? 0 : (int)to.controller,
                Reason = to == null ? 0 : to.reason
            });
        }

        private static void RaiseForCard(DuelPresentationKind kind, GameCard card, DuelPresentationWeight weight)
        {
            if (card == null)
                return;

            Raise(new DuelPresentationEvent
            {
                Kind = kind,
                Weight = weight,
                Card = card,
                Controller = card.p == null ? 0 : (int)card.p.controller,
                To = CloneGps(card.p)
            });
        }

        private static DuelPresentationMoveKind ResolveMoveKind(GPS from, GPS to, GameMessage message)
        {
            if ((to.reason & (uint)CardReason.DESTROY) > 0)
                return DuelPresentationMoveKind.Destroyed;
            if ((to.reason & (uint)CardReason.RELEASE) > 0)
                return DuelPresentationMoveKind.Released;
            if ((to.reason & (uint)CardReason.MATERIAL) > 0)
                return DuelPresentationMoveKind.Material;
            if ((to.location & (uint)CardLocation.Grave) > 0)
                return DuelPresentationMoveKind.ToGrave;
            if ((to.location & (uint)CardLocation.Removed) > 0)
                return DuelPresentationMoveKind.Banished;
            if ((to.location & (uint)CardLocation.Overlay) > 0)
                return DuelPresentationMoveKind.Overlay;
            if ((from.location & (uint)CardLocation.Deck) > 0 && (to.location & (uint)CardLocation.Hand) > 0)
                return DuelPresentationMoveKind.Draw;
            if (message == GameMessage.Draw)
                return DuelPresentationMoveKind.Draw;
            if ((to.location & (uint)CardLocation.Hand) > 0)
                return DuelPresentationMoveKind.ToHand;
            if ((to.location & (uint)CardLocation.Onfield) > 0)
                return DuelPresentationMoveKind.ToField;
            return DuelPresentationMoveKind.Generic;
        }

        private static DuelPresentationWeight ResolveMoveWeight(GameCard card, GPS from, GPS to, DuelPresentationMoveKind moveKind)
        {
            switch (moveKind)
            {
                case DuelPresentationMoveKind.Destroyed:
                case DuelPresentationMoveKind.Banished:
                    return DuelPresentationWeight.Heavy;
                case DuelPresentationMoveKind.ToGrave:
                case DuelPresentationMoveKind.Released:
                case DuelPresentationMoveKind.Material:
                case DuelPresentationMoveKind.ToField:
                    return DuelPresentationWeight.Medium;
                case DuelPresentationMoveKind.Draw:
                case DuelPresentationMoveKind.ToHand:
                    return DuelPresentationWeight.Light;
            }

            var data = card == null ? null : card.GetData();
            if (data != null && data.HasType(CardType.Monster) && GameCard.NeedStrongSummon(data))
                return DuelPresentationWeight.Heavy;
            return DuelPresentationWeight.Light;
        }

        private static DuelPresentationWeight ResolveAttackWeight(GameCard card)
        {
            var data = card == null ? null : card.GetData();
            if (data == null)
                return DuelPresentationWeight.Medium;
            if (data.Attack >= 3000)
                return DuelPresentationWeight.Heavy;
            return DuelPresentationWeight.Medium;
        }

        private static GPS CloneGps(GPS value)
        {
            if (value == null)
                return null;

            return new GPS
            {
                controller = value.controller,
                location = value.location,
                sequence = value.sequence,
                position = value.position,
                reason = value.reason
            };
        }

        private static void Raise(DuelPresentationEvent evt)
        {
            var handler = EventRaised;
            if (handler == null || evt == null)
                return;

            foreach (Action<DuelPresentationEvent> subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Duel presentation subscriber failed: " + ex.Message);
                }
            }
        }
    }

    public sealed class QuestDuelWorldPresenter : MonoBehaviour
    {
        private enum QuestCardInteractionState
        {
            None,
            Actionable,
            SelectionTarget
        }

        private const int FallbackQuestOverlayLayer = 24;
        private const float CardWidth = 5.05f;
        private const float CardHeight = 7.12f;
        private const float CardThickness = 0.14f;
        private const float TableCardY = 0.52f;
        private const float HandCardY = 11.1f;
        private const float PlayerHandBaseZ = -45.6f;
        private const float OpponentHandBaseZ = 38.2f;
        private const float HandCardBaseSpacing = 5.95f;
        private const float HandCardMinSpacing = 3.92f;
        private const float HandCardFanDepth = 0.95f;
        private const float HandCardFanYaw = 6.2f;
        private const float HandCardMaxFanYaw = 38f;
        private const float HandCardFanRoll = 1.55f;
        private const float HandCardFloatAmplitude = 0.22f;
        private const float HandCardCenterLift = 0.42f;
        private const float HandCardEdgeDrop = 0.18f;
        private const float HandCardCenterScale = 1.07f;
        private const float HandCardEdgeScale = 0.96f;
        private const float HandCardHoverScale = 0.06f;
        private const float HandAccentBaseWidth = CardWidth * 0.82f;
        private const float HandAccentBaseDepth = 0.18f;
        private const float HandAccentZ = -CardHeight * 0.49f;
        private const float PileCardY = 0.5f;
        private const float PortraitHeight = 26f;
        private const float PortraitMaxWidth = 18f;
        private const float PowerLabelY = CardThickness + 1.18f;
        private const float PowerLabelZ = -4.62f;
        private const float PowerLabelScale = 0.43f;
        private const float InteractionLabelY = CardThickness + 1.64f;
        private const float InteractionLabelZ = 4.30f;
        private const float InteractionLabelScale = 0.52f;
        private const float ActionMarkerBaseWidth = CardWidth * 0.72f;
        private const float ActionMarkerBaseDepth = 0.34f;
        private const float ActionMarkerZ = CardHeight * 0.5f + 0.14f;
        private const float ActionableCardLift = 0.82f;
        private const float SelectionTargetCardLift = 1.68f;
        private const float HoveredCardLift = 0.46f;
        private const float ActionableCardBob = 0.15f;
        private const float SelectionTargetCardBob = 0.30f;
        private const float QuestBoardScaleX = 1.38f;
        private const float QuestBoardScaleZ = 1.34f;
        private const float ProxyDiagnosticsInterval = 3f;
        private const float LegacySuppressionRescanInterval = 0.5f;
        private const bool QuestVerboseProxyDiagnostics = false;
        private const bool QuestAutoDebugCapture = false;
        private const int MaxPresentationTransients = 56;
        private const int MaxAutomaticDebugCaptures = 6;
        private const float SelectionGuideSourceHoldSeconds = 8f;
        private const int SelectionGuideCircleSegments = 56;
        private const string PreferredCardBackRelativePath = "texture/duel/opponent.jpg";

        private Camera xrCamera;
        private Transform worldAnchor;
        private Transform proxyRoot;
        private Transform pileRoot;
        private Transform presentationRoot;
        private Material cardBackMaterial;
        private Material cardSideMaterial;
        private Material placeholderFaceMaterial;
        private Material highlightMaterial;
        private Material actionHighlightMaterial;
        private Material targetHighlightMaterial;
        private Material handAccentMaterial;
        private Material pileFaceMaterial;
        private Material portraitMaterial;
        private static Texture2D fallbackCardBackTexture;
        private static Texture2D preferredCardBackTexture;
        private readonly Dictionary<GameCard, QuestCardProxy> cardProxies = new Dictionary<GameCard, QuestCardProxy>();
        private readonly Dictionary<string, QuestPileProxy> pileProxies = new Dictionary<string, QuestPileProxy>();
        private readonly List<PresentationTransient> presentationTransients = new List<PresentationTransient>();
        private readonly List<GameCard> visibleCards = new List<GameCard>();
        private readonly List<GameCard> staleCards = new List<GameCard>();
        private readonly List<GameObject> selectionGuideObjects = new List<GameObject>();
        private readonly List<Material> selectionGuideMaterials = new List<Material>();
        private GameCard hoveredCard;
        private GameCard selectionSourceCard;
        private string selectionGuideSignature;
        private float selectionSourceValidUntil;
        private float lastDiagnosticsTime;
        private float lastDebugCaptureTime;
        private int automaticDebugCaptureCount;
        private int hiddenLegacyRendererCount;
        private int disabledLegacyColliderCount;
        private bool legacySuppressionLogged;
        private Transform lastSuppressedLegacyDuelContainer;
        private float nextLegacySuppressionScanTime;
        private readonly List<Renderer> cachedLegacyRenderers = new List<Renderer>();
        private readonly List<Collider> cachedLegacyColliders = new List<Collider>();
        private readonly List<Canvas> cachedLegacyCanvases = new List<Canvas>();

        public void Configure(Camera camera, Transform anchor)
        {
            xrCamera = camera;
            worldAnchor = anchor;
            EnsureRoot();
        }

        public void SetVisible(bool visible)
        {
            if (proxyRoot != null && proxyRoot.gameObject.activeSelf != visible)
                proxyRoot.gameObject.SetActive(visible);
        }

        private void OnEnable()
        {
            DuelPresentationDirector.EventRaised += HandleDuelPresentationEvent;
        }

        private void OnDisable()
        {
            DuelPresentationDirector.EventRaised -= HandleDuelPresentationEvent;
            ClearPresentationTransients();
            ClearSelectionGuides();
        }

        public void Sync(Transform legacyDuelContainer)
        {
            EnsureRoot();
            if (proxyRoot == null)
                return;

            SuppressLegacyDuelContainer(legacyDuelContainer);
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null || core.cards == null)
            {
                HideAllProxies();
                ClearSelectionGuides();
                return;
            }

            visibleCards.Clear();
            foreach (var card in core.cards)
            {
                if (IsQuestVisibleCard(card))
                    visibleCards.Add(card);
            }

            foreach (var card in visibleCards)
            {
                var proxy = EnsureCardProxy(card);
                UpdateCardProxy(proxy, card);
            }

            RemoveStaleCardProxies();
            UpdatePileProxies(core);
            UpdateFieldSelectionGuides(core);
            LogDiagnostics(core, legacyDuelContainer);
            CaptureDebugFrameIfUseful();
        }

        public bool TryResolveCard(GameObject hitObject, out GameCard card)
        {
            card = null;
            if (hitObject == null)
                return false;

            var hit = hitObject.GetComponent<QuestCardProxyHit>()
                ?? hitObject.GetComponentInParent<QuestCardProxyHit>()
                ?? hitObject.GetComponentInChildren<QuestCardProxyHit>();
            if (hit == null || hit.Card == null)
                return false;

            card = hit.Card;
            return true;
        }

        public void SetHoveredCard(GameCard card)
        {
            hoveredCard = card;
        }

        public void SetSelectionSourceCard(GameCard card)
        {
            selectionSourceCard = card;
            selectionSourceValidUntil = card == null ? 0f : Time.unscaledTime + SelectionGuideSourceHoldSeconds;
            selectionGuideSignature = null;
        }

        public bool TryGetCardWorldBounds(GameCard card, out Bounds bounds)
        {
            bounds = default;
            if (card == null)
                return false;

            if (!cardProxies.TryGetValue(card, out var proxy) || proxy == null || proxy.Root == null)
                return false;
            if (!proxy.Root.activeInHierarchy)
                return false;

            return TryCollectWorldBounds(proxy.Root, out bounds);
        }

        public bool TryGetCardActionAnchor(GameCard card, out Vector3 anchor, out float radius)
        {
            anchor = default;
            radius = 0f;
            if (card == null)
                return false;

            if (!cardProxies.TryGetValue(card, out var proxy) || proxy == null || proxy.Root == null)
                return false;
            if (!proxy.Root.activeInHierarchy)
                return false;

            var scale = Mathf.Max(
                Mathf.Abs(proxy.Transform.lossyScale.x),
                Mathf.Abs(proxy.Transform.lossyScale.y),
                Mathf.Abs(proxy.Transform.lossyScale.z),
                0.001f);
            anchor = proxy.Transform.position + Vector3.up * Mathf.Max(0.85f, scale * 1.08f);
            radius = Mathf.Max(CardWidth, CardHeight) * 0.5f * scale;
            return true;
        }

        public bool TryResolvePile(GameObject hitObject, out uint controller, out CardLocation location)
        {
            controller = 0;
            location = CardLocation.Unknown;
            if (hitObject == null)
                return false;

            var hit = hitObject.GetComponent<QuestPileProxyHit>()
                ?? hitObject.GetComponentInParent<QuestPileProxyHit>()
                ?? hitObject.GetComponentInChildren<QuestPileProxyHit>();
            if (hit == null)
                return false;

            controller = hit.Controller;
            location = hit.Location;
            return true;
        }

        private void UpdateFieldSelectionGuides(OcgCore core)
        {
            if (core == null || core.places == null || presentationRoot == null)
            {
                ClearSelectionGuides();
                return;
            }

            var targets = new List<GameCard>();
            foreach (var place in core.places)
            {
                if (place == null || !place.cardSelecting || place.cookieCard == null)
                    continue;
                if (!cardProxies.ContainsKey(place.cookieCard))
                    continue;
                if (!targets.Contains(place.cookieCard))
                    targets.Add(place.cookieCard);
            }

            if (targets.Count == 0)
            {
                ClearSelectionGuides();
                if (Time.unscaledTime > selectionSourceValidUntil)
                    selectionSourceCard = null;
                return;
            }

            targets.Sort((left, right) =>
            {
                var leftId = left == null ? 0 : left.md5;
                var rightId = right == null ? 0 : right.md5;
                return leftId.CompareTo(rightId);
            });

            var source = ResolveValidSelectionSource(targets);
            var signature = BuildSelectionGuideSignature(source, targets);
            if (signature == selectionGuideSignature && selectionGuideObjects.Count > 0)
                return;

            ClearSelectionGuides();
            selectionGuideSignature = signature;
            BuildSelectionGuides(source, targets);
        }

        private GameCard ResolveValidSelectionSource(List<GameCard> targets)
        {
            if (selectionSourceCard != null && Time.unscaledTime <= selectionSourceValidUntil && !targets.Contains(selectionSourceCard))
            {
                if (TryGetCardWorldPoint(selectionSourceCard, out _))
                    return selectionSourceCard;
            }

            if (hoveredCard != null && !targets.Contains(hoveredCard) && TryGetCardWorldPoint(hoveredCard, out _))
                return hoveredCard;

            return null;
        }

        private static string BuildSelectionGuideSignature(GameCard source, List<GameCard> targets)
        {
            var signature = source == null ? "source:none" : "source:" + source.md5;
            signature += "|targets:";
            if (targets != null)
                foreach (var target in targets)
                    signature += target == null ? "0," : target.md5 + ",";
            return signature;
        }

        private void BuildSelectionGuides(GameCard source, List<GameCard> targets)
        {
            if (targets == null || targets.Count == 0)
                return;

            var sourcePoint = Vector3.zero;
            var hasSource = source != null && TryGetSelectionGuideSourcePoint(source, out sourcePoint);
            foreach (var target in targets)
            {
                if (target == null || !TryGetCardWorldBounds(target, out var bounds))
                    continue;

                var radius = Mathf.Max(bounds.extents.x, bounds.extents.z, 0.4f) + 0.34f;
                var center = bounds.center;
                center.y = bounds.max.y + 0.08f;
                CreateSelectionTargetRing(center, radius);

                if (hasSource)
                    CreateSelectionGuideArc(sourcePoint, bounds.center + Vector3.up * 0.62f, targets.Count);
            }
        }

        private bool TryGetSelectionGuideSourcePoint(GameCard source, out Vector3 point)
        {
            if (TryGetCardActionAnchor(source, out var anchor, out _))
            {
                point = anchor + Vector3.up * 0.34f;
                return true;
            }

            if (TryGetCardWorldPoint(source, out point))
            {
                point += Vector3.up * 0.92f;
                return true;
            }

            return false;
        }

        private void CreateSelectionTargetRing(Vector3 center, float radius)
        {
            var ringObject = new GameObject("QuestSelectionTargetRing");
            SetQuestOverlayLayer(ringObject);
            ringObject.transform.SetParent(presentationRoot, false);
            var line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = SelectionGuideCircleSegments;
            line.numCapVertices = 4;
            line.alignment = LineAlignment.View;
            line.startWidth = 0.16f;
            line.endWidth = 0.16f;
            var color = new Color(0.24f, 1f, 0.82f, 0.90f);
            var material = CreateMaterial("QuestSelectionTargetRingMaterial", color, true);
            line.material = material;
            var localCenter = presentationRoot.InverseTransformPoint(center);
            var localRadius = radius / GetPresentationRootWorldScale();

            for (var index = 0; index < SelectionGuideCircleSegments; index += 1)
            {
                var angle = Mathf.PI * 2f * index / SelectionGuideCircleSegments;
                var position = localCenter + new Vector3(Mathf.Cos(angle) * localRadius, 0f, Mathf.Sin(angle) * localRadius);
                line.SetPosition(index, position);
            }

            selectionGuideObjects.Add(ringObject);
            selectionGuideMaterials.Add(material);
        }

        private void CreateSelectionGuideArc(Vector3 from, Vector3 to, int targetCount)
        {
            if ((to - from).sqrMagnitude < 0.01f)
                return;

            var lineObject = new GameObject("QuestSelectionGuideArc");
            SetQuestOverlayLayer(lineObject);
            lineObject.transform.SetParent(presentationRoot, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 14;
            line.numCapVertices = 5;
            line.alignment = LineAlignment.View;
            line.startWidth = Mathf.Clamp(0.12f + targetCount * 0.012f, 0.12f, 0.22f);
            line.endWidth = 0.045f;
            var color = new Color(0.22f, 0.82f, 1f, 0.64f);
            var material = CreateMaterial("QuestSelectionGuideArcMaterial", color, true);
            line.material = material;

            var localFrom = presentationRoot.InverseTransformPoint(from);
            var localTo = presentationRoot.InverseTransformPoint(to);
            var planarDistance = Vector3.Distance(new Vector3(localFrom.x, 0f, localFrom.z), new Vector3(localTo.x, 0f, localTo.z));
            var apex = Mathf.Clamp(planarDistance * 0.16f, 0.65f, 2.6f);
            for (var index = 0; index < line.positionCount; index += 1)
            {
                var t = index / (float)(line.positionCount - 1);
                var position = Vector3.Lerp(localFrom, localTo, t);
                position.y += Mathf.Sin(t * Mathf.PI) * apex;
                line.SetPosition(index, position);
            }

            selectionGuideObjects.Add(lineObject);
            selectionGuideMaterials.Add(material);
        }

        private float GetPresentationRootWorldScale()
        {
            if (presentationRoot == null)
                return 1f;

            var scale = presentationRoot.lossyScale;
            return Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z), 0.001f);
        }

        private void ClearSelectionGuides()
        {
            foreach (var material in selectionGuideMaterials)
                if (material != null)
                    Destroy(material);
            selectionGuideMaterials.Clear();

            foreach (var guide in selectionGuideObjects)
                if (guide != null)
                    Destroy(guide);
            selectionGuideObjects.Clear();
            selectionGuideSignature = null;
        }

        private void EnsureRoot()
        {
            if (worldAnchor == null)
                return;

            if (proxyRoot == null)
            {
                var rootObject = new GameObject("QuestNativeDuelVisibleLayer");
                SetQuestOverlayLayer(rootObject);
                proxyRoot = rootObject.transform;
            }

            if (proxyRoot.parent != worldAnchor)
                proxyRoot.SetParent(worldAnchor, false);

            proxyRoot.localPosition = Vector3.zero;
            proxyRoot.localRotation = Quaternion.identity;
            proxyRoot.localScale = Vector3.one;
            if (!proxyRoot.gameObject.activeSelf)
                proxyRoot.gameObject.SetActive(true);

            if (pileRoot == null)
            {
                var pileObject = new GameObject("QuestNativeDuelPiles");
                SetQuestOverlayLayer(pileObject);
                pileRoot = pileObject.transform;
                pileRoot.SetParent(proxyRoot, false);
            }

            if (presentationRoot == null)
            {
                var presentationObject = new GameObject("QuestNativeDuelPresentation");
                SetQuestOverlayLayer(presentationObject);
                presentationRoot = presentationObject.transform;
                presentationRoot.SetParent(proxyRoot, false);
            }
        }

        private void HideAllProxies()
        {
            foreach (var proxy in cardProxies.Values)
            {
                if (proxy.Root != null && proxy.Root.activeSelf)
                    proxy.Root.SetActive(false);
            }

            foreach (var pile in pileProxies.Values)
            {
                if (pile.Root != null && pile.Root.activeSelf)
                    pile.Root.SetActive(false);
            }
        }

        private void HandleDuelPresentationEvent(DuelPresentationEvent evt)
        {
            if (evt == null || worldAnchor == null)
                return;

            EnsureRoot();
            if (presentationRoot == null || proxyRoot == null || !proxyRoot.gameObject.activeInHierarchy)
                return;

            switch (evt.Kind)
            {
                case DuelPresentationKind.CardMoved:
                    PlayCardMovePresentation(evt);
                    break;
                case DuelPresentationKind.CardSet:
                    PlayCardPulse(evt.Card, new Color(0.30f, 0.42f, 0.95f, 0.70f), evt.Weight, 1.18f);
                    break;
                case DuelPresentationKind.CardSummoned:
                    PlayCardPulse(evt.Card, ResolveSummonColor(evt.Card), evt.Weight, 1.65f);
                    PlaySummonPortraitRise(evt.Card, evt.Weight);
                    PlayFloatingText(ResolveCardWorldPoint(evt.Card), "\u53ec\u5524", ResolveSummonColor(evt.Card), evt.Weight);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.CardActivated:
                    PlayCardPulse(evt.Card, new Color(0.45f, 0.92f, 1f, 0.82f), evt.Weight, 1.32f);
                    PlayFloatingText(ResolveCardWorldPoint(evt.Card), evt.ChainIndex > 1 ? "CHAIN " + evt.ChainIndex : "\u53d1\u52a8", new Color(0.45f, 0.92f, 1f, 1f), evt.Weight);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.ChainStacked:
                    PlayCardPulse(evt.Card, new Color(1f, 0.78f, 0.24f, 0.82f), evt.Weight, 1.42f);
                    PlayFloatingText(ResolveCardWorldPoint(evt.Card), "CHAIN " + evt.ChainIndex, new Color(1f, 0.82f, 0.28f, 1f), evt.Weight);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.CardDestroyed:
                    PlayCardPulse(evt.Card, new Color(1f, 0.18f, 0.12f, 0.86f), evt.Weight, 1.55f);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.CardSentToGrave:
                    PlayMoveLine(evt, new Color(0.62f, 0.68f, 0.78f, 0.92f));
                    PlayPulseAt(ResolveGpsWorldPoint(evt.To, evt.Card), new Color(0.45f, 0.52f, 0.70f, 0.55f), evt.Weight, 1.12f);
                    break;
                case DuelPresentationKind.CardBanished:
                    PlayMoveLine(evt, new Color(0.36f, 0.82f, 1f, 0.96f));
                    PlayPulseAt(ResolveGpsWorldPoint(evt.To, evt.Card), new Color(0.20f, 0.68f, 1f, 0.66f), evt.Weight, 1.26f);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.AttackDeclared:
                    PlayAttackLine(evt);
                    PlayFloatingText(ResolveCardWorldPoint(evt.Card), evt.Final ? "FINAL" : "\u653b\u51fb", new Color(1f, 0.38f, 0.18f, 1f), evt.Weight);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.AttackImpact:
                    PlayPulseAt(ResolveAttackTargetPoint(evt), new Color(1f, 0.52f, 0.18f, 0.90f), evt.Weight, evt.Final ? 2.2f : 1.55f);
                    SendPresentationHaptic(evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.Damage:
                    PlayDamageText(evt.Controller, -Mathf.Abs(evt.Value), evt.Final ? DuelPresentationWeight.Finisher : evt.Weight);
                    SendPresentationHaptic(evt.Final ? DuelPresentationWeight.Finisher : evt.Weight, evt.Controller);
                    break;
                case DuelPresentationKind.Recover:
                    PlayDamageText(evt.Controller, Mathf.Abs(evt.Value), evt.Weight);
                    break;
                case DuelPresentationKind.PhaseChanged:
                    PlayPhaseText(evt.Controller, evt.Phase);
                    break;
            }
        }

        private void PlayCardMovePresentation(DuelPresentationEvent evt)
        {
            if (evt == null)
                return;

            switch (evt.MoveKind)
            {
                case DuelPresentationMoveKind.Draw:
                    PlayMoveLine(evt, new Color(0.42f, 0.82f, 1f, 0.74f));
                    PlayPulseAt(ResolveGpsWorldPoint(evt.To, evt.Card), new Color(0.42f, 0.82f, 1f, 0.45f), evt.Weight, 0.82f);
                    break;
                case DuelPresentationMoveKind.ToField:
                    PlayPulseAt(ResolveGpsWorldPoint(evt.To, evt.Card), new Color(0.34f, 1f, 0.62f, 0.48f), evt.Weight, 1.0f);
                    break;
                case DuelPresentationMoveKind.ToHand:
                    PlayMoveLine(evt, new Color(0.70f, 0.78f, 1f, 0.52f));
                    break;
                case DuelPresentationMoveKind.Material:
                case DuelPresentationMoveKind.Released:
                    PlayMoveLine(evt, new Color(1f, 0.78f, 0.28f, 0.70f));
                    PlayCardPulse(evt.Card, new Color(1f, 0.78f, 0.28f, 0.58f), DuelPresentationWeight.Medium, 1.0f);
                    break;
            }
        }

        private void PlayMoveLine(DuelPresentationEvent evt, Color color)
        {
            if (evt == null)
                return;

            var from = ResolveGpsWorldPoint(evt.From, evt.Card);
            var to = ResolveGpsWorldPoint(evt.To, evt.Card);
            PlayLine(from, to, color, evt.Weight, 0.38f);
        }

        private void PlayAttackLine(DuelPresentationEvent evt)
        {
            var from = ResolveCardWorldPoint(evt.Card);
            var to = ResolveAttackTargetPoint(evt);
            var outer = evt.Final ? new Color(1f, 0.10f, 0.04f, 0.68f) : new Color(1f, 0.42f, 0.10f, 0.46f);
            var inner = evt.Final ? new Color(1f, 0.84f, 0.34f, 1f) : new Color(1f, 0.78f, 0.22f, 0.95f);
            PlayLine(from, to, outer, evt.Weight, evt.Final ? 0.72f : 0.48f, 2.8f);
            PlayLine(from, to, inner, evt.Weight, evt.Final ? 0.62f : 0.38f, 1.0f);
        }

        private void PlayCardPulse(GameCard card, Color color, DuelPresentationWeight weight, float size)
        {
            PlayPulseAt(ResolveCardWorldPoint(card), color, weight, size);
        }

        private void PlaySummonPortraitRise(GameCard card, DuelPresentationWeight weight)
        {
            if (card == null || !cardProxies.TryGetValue(card, out var proxy) || proxy == null || proxy.Portrait == null)
                return;

            StartCoroutine(PlaySummonPortraitRiseDelayed(proxy, weight));
        }

        private IEnumerator PlaySummonPortraitRiseDelayed(QuestCardProxy proxy, DuelPresentationWeight weight)
        {
            for (var frame = 0; frame < 18; frame += 1)
            {
                if (proxy == null || proxy.Portrait == null)
                    yield break;
                if (proxy.Portrait.activeInHierarchy && proxy.PortraitRenderer != null && proxy.PortraitRenderer.sharedMaterial != null)
                    break;
                yield return null;
            }

            if (proxy == null || proxy.Portrait == null || !proxy.Portrait.activeInHierarchy)
                yield break;

            var targetScale = proxy.Portrait.transform.localScale;
            var targetPosition = proxy.Portrait.transform.localPosition;
            var startScale = targetScale * 0.22f;
            var startPosition = targetPosition - new Vector3(0f, Mathf.Max(2.2f, PortraitHeight * 0.16f), 0f);
            var duration = weight >= DuelPresentationWeight.Heavy ? 0.48f : 0.34f;

            proxy.Portrait.transform.DOKill(false);
            proxy.Portrait.transform.localScale = startScale;
            proxy.Portrait.transform.localPosition = startPosition;
            DOTween.Sequence()
                .Join(proxy.Portrait.transform.DOLocalMove(targetPosition, duration).SetEase(Ease.OutCubic))
                .Join(proxy.Portrait.transform.DOScale(targetScale * (weight >= DuelPresentationWeight.Heavy ? 1.10f : 1.04f), duration * 0.72f).SetEase(Ease.OutBack))
                .Append(proxy.Portrait.transform.DOScale(targetScale, duration * 0.28f).SetEase(Ease.InOutSine));
        }

        private void PlayPulseAt(Vector3 position, Color color, DuelPresentationWeight weight, float size)
        {
            if (presentationRoot == null)
                return;

            var pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pulse.name = "QuestPresentationPulse";
            SetQuestOverlayLayer(pulse);
            Destroy(pulse.GetComponent<Collider>());
            pulse.transform.SetParent(presentationRoot, true);
            pulse.transform.position = position + Vector3.up * 0.04f;
            pulse.transform.localRotation = Quaternion.identity;
            var radius = size * ResolveWeightScale(weight);
            pulse.transform.localScale = new Vector3(radius * 0.18f, 0.018f, radius * 0.18f);

            var material = CreateMaterial("QuestPresentationPulseMaterial", color, true);
            QuestCardProxy.ConfigureRenderer(pulse.GetComponent<MeshRenderer>(), material);
            RegisterPresentationTransient(pulse, material);

            var targetScale = new Vector3(radius, 0.018f, radius);
            var duration = ResolveWeightDuration(weight);
            DOTween.Sequence()
                .Join(pulse.transform.DOScale(targetScale, duration).SetEase(Ease.OutCubic))
                .Join(DOTween.To(() => color.a, alpha =>
                {
                    var c = color;
                    c.a = alpha;
                    SetMaterialColor(material, c);
                }, 0f, duration).SetEase(Ease.OutQuad))
                .OnComplete(() =>
                {
                    DestroyPresentationTransient(pulse, material);
                });
        }

        private void PlayLine(Vector3 from, Vector3 to, Color color, DuelPresentationWeight weight, float duration, float widthMultiplier = 1f)
        {
            if (presentationRoot == null || (to - from).sqrMagnitude < 0.01f)
                return;

            var lineObject = new GameObject("QuestPresentationLine");
            SetQuestOverlayLayer(lineObject);
            lineObject.transform.SetParent(presentationRoot, true);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 5;
            line.alignment = LineAlignment.View;
            line.startWidth = 0.10f * ResolveWeightScale(weight) * widthMultiplier;
            line.endWidth = 0.03f * ResolveWeightScale(weight) * widthMultiplier;
            line.SetPosition(0, from + Vector3.up * 0.32f);
            line.SetPosition(1, from + Vector3.up * 0.32f);
            var material = CreateMaterial("QuestPresentationLineMaterial", color, true);
            line.material = material;
            RegisterPresentationTransient(lineObject, material);
            var end = to + Vector3.up * 0.32f;
            DOTween.Sequence()
                .Join(DOTween.To(() => 0f, t =>
                {
                    if (line != null)
                        line.SetPosition(1, Vector3.Lerp(from + Vector3.up * 0.32f, end, t));
                }, 1f, Mathf.Max(0.12f, duration * 0.55f)).SetEase(Ease.OutCubic))
                .AppendInterval(duration * 0.35f)
                .Append(DOTween.To(() => color.a, alpha =>
                {
                    var c = color;
                    c.a = alpha;
                    SetMaterialColor(material, c);
                    if (line != null)
                    {
                        line.startColor = c;
                        line.endColor = new Color(c.r, c.g, c.b, c.a * 0.35f);
                    }
                }, 0f, duration * 0.45f))
                .OnComplete(() =>
                {
                    DestroyPresentationTransient(lineObject, material);
                });
        }

        private void PlayFloatingText(Vector3 position, string content, Color color, DuelPresentationWeight weight)
        {
            if (presentationRoot == null || string.IsNullOrEmpty(content))
                return;

            var textObject = new GameObject("QuestPresentationText");
            SetQuestOverlayLayer(textObject);
            textObject.transform.SetParent(presentationRoot, true);
            textObject.transform.position = position + Vector3.up * (1.4f + ResolveWeightScale(weight) * 0.18f);
            textObject.transform.localScale = Vector3.one * (0.58f * ResolveWeightScale(weight));
            var text = textObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 3.6f;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.text = content;
            text.color = color;
            text.outlineWidth = 0.24f;
            text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
            FaceTextToCamera(textObject.transform);
            RegisterPresentationTransient(textObject, null);

            var startColor = color;
            var duration = 0.85f + ResolveWeightScale(weight) * 0.12f;
            DOTween.Sequence()
                .Join(textObject.transform.DOMove(textObject.transform.position + Vector3.up * (0.95f * ResolveWeightScale(weight)), duration).SetEase(Ease.OutCubic))
                .Join(DOTween.To(() => startColor.a, alpha =>
                {
                    var c = startColor;
                    c.a = alpha;
                    if (text != null)
                        text.color = c;
                }, 0f, duration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    DestroyPresentationTransient(textObject, null);
                });
        }

        private void RegisterPresentationTransient(GameObject root, Material material)
        {
            if (root == null)
                return;

            CleanupPresentationTransients();
            presentationTransients.Add(new PresentationTransient(root, material));
            while (presentationTransients.Count > MaxPresentationTransients)
                DestroyPresentationTransient(presentationTransients[0]);
        }

        private void DestroyPresentationTransient(GameObject root, Material material)
        {
            for (var i = presentationTransients.Count - 1; i >= 0; i -= 1)
            {
                var transient = presentationTransients[i];
                if (transient != null && transient.Root == root)
                    presentationTransients.RemoveAt(i);
            }

            if (material != null)
                Destroy(material);
            if (root != null)
                Destroy(root);
        }

        private void DestroyPresentationTransient(PresentationTransient transient)
        {
            if (transient == null)
                return;

            presentationTransients.Remove(transient);
            if (transient.Material != null)
                Destroy(transient.Material);
            if (transient.Root != null)
                Destroy(transient.Root);
        }

        private void ClearPresentationTransients()
        {
            for (var i = presentationTransients.Count - 1; i >= 0; i -= 1)
                DestroyPresentationTransient(presentationTransients[i]);
            presentationTransients.Clear();
        }

        private void CleanupPresentationTransients()
        {
            for (var i = presentationTransients.Count - 1; i >= 0; i -= 1)
            {
                var transient = presentationTransients[i];
                if (transient == null)
                {
                    presentationTransients.RemoveAt(i);
                    continue;
                }

                if (transient.Root == null)
                {
                    if (transient.Material != null)
                        Destroy(transient.Material);
                    presentationTransients.RemoveAt(i);
                }
            }
        }

        private void PlayDamageText(int player, int signedValue, DuelPresentationWeight weight)
        {
            var local = new Vector3(player == 0 ? -24f : 24f, 2.2f, player == 0 ? -18f : 18f);
            var position = worldAnchor.TransformPoint(local);
            var color = signedValue < 0 ? new Color(1f, 0.24f, 0.18f, 1f) : new Color(0.36f, 1f, 0.50f, 1f);
            var prefix = signedValue < 0 ? "-" : "+";
            PlayFloatingText(position, prefix + Mathf.Abs(signedValue), color, weight);
            PlayPulseAt(position, color, weight, signedValue < 0 ? 1.7f : 1.2f);
        }

        private void PlayPhaseText(int player, DuelPhase phase)
        {
            var local = new Vector3(player == 0 ? -13f : 13f, 2.0f, player == 0 ? -8f : 8f);
            PlayFloatingText(worldAnchor.TransformPoint(local), GetPhasePresentationName(phase), new Color(0.72f, 0.92f, 1f, 1f), DuelPresentationWeight.Light);
        }

        private Vector3 ResolveAttackTargetPoint(DuelPresentationEvent evt)
        {
            if (evt != null && evt.TargetCard != null)
                return ResolveCardWorldPoint(evt.TargetCard);

            var controller = evt == null ? 0 : evt.Controller;
            var local = controller == 0 ? new Vector3(0f, TableCardY, 31f) : new Vector3(0f, TableCardY, -31f);
            return worldAnchor == null ? local : worldAnchor.TransformPoint(local);
        }

        private Vector3 ResolveCardWorldPoint(GameCard card)
        {
            if (TryGetCardWorldPoint(card, out var position))
                return position;
            if (card != null && card.p != null)
                return ResolveGpsWorldPoint(card.p, card);
            return worldAnchor == null ? Vector3.zero : worldAnchor.TransformPoint(new Vector3(0f, TableCardY, 0f));
        }

        private bool TryGetCardWorldPoint(GameCard card, out Vector3 position)
        {
            position = default;
            if (card == null)
                return false;

            if (cardProxies.TryGetValue(card, out var proxy) && proxy != null && proxy.Root != null && proxy.Root.activeInHierarchy)
            {
                if (TryCollectWorldBounds(proxy.Root, out var bounds))
                {
                    position = bounds.center;
                    return true;
                }
            }

            if (card.p == null || worldAnchor == null)
                return false;

            position = ResolveGpsWorldPoint(card.p, card);
            return true;
        }

        private Vector3 ResolveGpsWorldPoint(GPS gps, GameCard card)
        {
            if (gps == null)
                return ResolveCardWorldPoint(card);

            var local = ResolveQuestGpsLocalPosition(gps, card);
            return worldAnchor == null ? local : worldAnchor.TransformPoint(local);
        }

        private static Vector3 ResolveQuestGpsLocalPosition(GPS gps, GameCard card)
        {
            if (gps == null)
                return Vector3.zero;

            var position = ScaleQuestBoardPosition(GameCard.GetCardPosition(gps, card, card == null ? null : card.overlayParent));
            if ((gps.location & (uint)CardLocation.Hand) > 0)
            {
                return ResolveQuestHandLocalPosition(gps);
            }

            if ((gps.location & (uint)(CardLocation.Deck | CardLocation.Extra)) > 0)
            {
                position.y = PileCardY;
                return position;
            }

            if ((gps.location & (uint)(CardLocation.Grave | CardLocation.Removed)) > 0)
            {
                position.y = TableCardY + Mathf.Clamp((int)gps.sequence, 0, 8) * 0.035f;
                return position;
            }

            if ((gps.location & (uint)CardLocation.Overlay) > 0)
            {
                position.y = TableCardY + 0.05f + Mathf.Clamp(gps.position, 0, 8) * 0.025f;
                return position;
            }

            position.y = TableCardY;
            return position;
        }

        private static Color ResolveSummonColor(GameCard card)
        {
            var data = card == null ? null : card.GetData();
            if (data == null)
                return new Color(0.55f, 0.95f, 1f, 0.82f);
            if (data.HasType(CardType.Fusion))
                return new Color(0.82f, 0.40f, 1f, 0.84f);
            if (data.HasType(CardType.Synchro))
                return new Color(0.88f, 1f, 0.92f, 0.86f);
            if (data.HasType(CardType.Xyz))
                return new Color(0.95f, 0.86f, 0.30f, 0.86f);
            if (data.HasType(CardType.Link))
                return new Color(0.24f, 0.62f, 1f, 0.86f);
            if (data.HasType(CardType.Ritual))
                return new Color(0.28f, 0.54f, 1f, 0.86f);
            return new Color(0.52f, 1f, 0.70f, 0.82f);
        }

        private static float ResolveWeightScale(DuelPresentationWeight weight)
        {
            switch (weight)
            {
                case DuelPresentationWeight.Finisher:
                    return 2.0f;
                case DuelPresentationWeight.Heavy:
                    return 1.55f;
                case DuelPresentationWeight.Medium:
                    return 1.18f;
                default:
                    return 0.86f;
            }
        }

        private static float ResolveWeightDuration(DuelPresentationWeight weight)
        {
            switch (weight)
            {
                case DuelPresentationWeight.Finisher:
                    return 0.95f;
                case DuelPresentationWeight.Heavy:
                    return 0.72f;
                case DuelPresentationWeight.Medium:
                    return 0.52f;
                default:
                    return 0.36f;
            }
        }

        private static string GetPhasePresentationName(DuelPhase phase)
        {
            switch (phase)
            {
                case DuelPhase.Draw:
                    return "DRAW";
                case DuelPhase.Standby:
                    return "STANDBY";
                case DuelPhase.Main1:
                    return "MAIN 1";
                case DuelPhase.BattleStart:
                case DuelPhase.BattleStep:
                case DuelPhase.Battle:
                    return "BATTLE";
                case DuelPhase.Main2:
                    return "MAIN 2";
                case DuelPhase.End:
                    return "END";
                default:
                    return phase.ToString().ToUpperInvariant();
            }
        }

        private static void SendPresentationHaptic(DuelPresentationWeight weight, int controller)
        {
            var amplitude = 0.12f;
            var duration = 0.035f;
            switch (weight)
            {
                case DuelPresentationWeight.Finisher:
                    amplitude = 0.62f;
                    duration = 0.16f;
                    break;
                case DuelPresentationWeight.Heavy:
                    amplitude = 0.42f;
                    duration = 0.10f;
                    break;
                case DuelPresentationWeight.Medium:
                    amplitude = 0.24f;
                    duration = 0.06f;
                    break;
            }

            SendHapticImpulse(XRNode.RightHand, amplitude, duration);
            if (weight >= DuelPresentationWeight.Heavy)
                SendHapticImpulse(XRNode.LeftHand, amplitude * 0.75f, duration);
        }

        private static void SendHapticImpulse(XRNode node, float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
                device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, duration));
        }

        private static bool IsQuestVisibleCard(GameCard card)
        {
            if (card == null || card.p == null)
                return false;

            var location = card.p.location;
            if (location == 0)
                return false;
            if ((location & (uint)CardLocation.Search) > 0)
                return false;
            if ((location & (uint)CardLocation.Unknown) > 0)
                return false;
            if ((location & (uint)CardLocation.Deck) > 0)
                return false;
            if ((location & (uint)CardLocation.Extra) > 0)
                return false;

            return (location & (uint)(CardLocation.Hand | CardLocation.Onfield | CardLocation.Grave | CardLocation.Removed | CardLocation.Overlay)) > 0;
        }

        private QuestCardProxy EnsureCardProxy(GameCard card)
        {
            if (cardProxies.TryGetValue(card, out var proxy) && proxy.Root != null)
                return proxy;

            proxy = QuestCardProxy.Create(
                card,
                proxyRoot,
                GetCardSideMaterial(),
                GetCardBackMaterial(),
                GetPlaceholderFaceMaterial(),
                GetHighlightMaterial(),
                GetActionHighlightMaterial(),
                GetTargetHighlightMaterial(),
                GetHandAccentMaterial());
            cardProxies[card] = proxy;
            return proxy;
        }

        private void UpdateCardProxy(QuestCardProxy proxy, GameCard card)
        {
            if (proxy == null || proxy.Root == null || card == null || card.p == null)
                return;

            if (!proxy.Root.activeSelf)
                proxy.Root.SetActive(true);

            proxy.Card = card;
            proxy.Hit.Card = card;
            var cardMono = proxy.Root.GetComponent<GameCardMono>();
            if (cardMono != null)
                cardMono.cookieCard = card;

            var interactionState = ResolveQuestCardInteractionState(card);
            var localPosition = ResolveQuestCardLocalPosition(card);
            localPosition = ApplyQuestCardInteractionLift(localPosition, card, interactionState, card == hoveredCard);
            var localRotation = ResolveQuestCardLocalRotation(card);
            var localScale = ResolveQuestCardLocalScale(card, interactionState, card == hoveredCard);
            proxy.Transform.localPosition = localPosition;
            proxy.Transform.localRotation = localRotation;
            proxy.Transform.localScale = localScale;

            var knownFace = ShouldShowKnownFace(card);
            proxy.Front.SetActive(knownFace);
            proxy.Back.SetActive(!knownFace);
            UpdateInteractionHintProxy(proxy, card, interactionState);
            UpdateHandAccentProxy(proxy, card, interactionState);

            if (knownFace)
                EnsureProxyFaceTexture(proxy, card.GetData().Id);

            UpdatePortraitProxy(proxy, card);
            UpdatePowerLabel(proxy, card);
        }

        private void UpdateInteractionHintProxy(QuestCardProxy proxy, GameCard card, QuestCardInteractionState state)
        {
            if (proxy == null || card == null)
                return;

            var selectable = state == QuestCardInteractionState.SelectionTarget;
            var actionable = state == QuestCardInteractionState.Actionable;
            SetInteractionObject(proxy.Highlight, false, 0f, 0f);
            SetActionMarkerObject(proxy.ActionHighlight, actionable);
            SetTargetMarkerObject(proxy.TargetHighlight, selectable);

            if (proxy.InteractionLabelRoot == null || proxy.InteractionLabelText == null)
                return;

            var showLabel = selectable || actionable;
            if (proxy.InteractionLabelRoot.activeSelf != showLabel)
                proxy.InteractionLabelRoot.SetActive(showLabel);
            if (!showLabel)
                return;

            proxy.InteractionLabelText.text = selectable
                ? "\u9009\u62e9\u76ee\u6807"
                : GetQuestActionHintLabel(card);
            proxy.InteractionLabelText.color = selectable
                ? new Color(0.35f, 1f, 0.82f, 1f)
                : new Color(1f, 0.82f, 0.28f, 1f);
            FaceTextToCamera(proxy.InteractionLabelRoot.transform);
        }

        private static string GetQuestActionHintLabel(GameCard card)
        {
            if (card == null || card.buttons == null || card.buttons.Count == 0)
                return "\u53ef\u64cd\u4f5c";

            var hasActivate = false;
            var hasSummon = false;
            var hasSpecialSummon = false;
            var hasBattle = false;
            var hasSet = false;
            var hasPosition = false;
            foreach (var button in card.buttons)
            {
                switch (button.type)
                {
                    case MDPro3.UI.ButtonType.Activate:
                        hasActivate = true;
                        break;
                    case MDPro3.UI.ButtonType.Summon:
                    case MDPro3.UI.ButtonType.PenSummon:
                        hasSummon = true;
                        break;
                    case MDPro3.UI.ButtonType.SpSummon:
                        hasSpecialSummon = true;
                        break;
                    case MDPro3.UI.ButtonType.Battle:
                        hasBattle = true;
                        break;
                    case MDPro3.UI.ButtonType.SetSpell:
                    case MDPro3.UI.ButtonType.SetMonster:
                    case MDPro3.UI.ButtonType.SetPendulum:
                        hasSet = true;
                        break;
                    case MDPro3.UI.ButtonType.ToAttackPosition:
                    case MDPro3.UI.ButtonType.ToDefensePosition:
                        hasPosition = true;
                        break;
                }
            }

            if (hasBattle)
                return "\u53ef\u653b\u51fb";
            if (hasActivate)
                return "\u53ef\u53d1\u52a8";
            if (hasSpecialSummon)
                return "\u53ef\u7279\u53ec";
            if (hasSummon)
                return "\u53ef\u53ec\u5524";
            if (hasSet)
                return "\u53ef\u653e\u7f6e";
            if (hasPosition)
                return "\u53ef\u8f6c\u5411";
            return "\u53ef\u64cd\u4f5c";
        }

        private void UpdateHandAccentProxy(QuestCardProxy proxy, GameCard card, QuestCardInteractionState state)
        {
            if (proxy == null || card == null || card.p == null)
                return;

            var isPlayerHand = card.p.controller == 0 && (card.p.location & (uint)CardLocation.Hand) > 0;
            SetHandAccentObject(
                proxy.HandAccent,
                isPlayerHand,
                state != QuestCardInteractionState.None || card == hoveredCard,
                ResolveQuestHandNormalizedEdge(card.p));
        }

        private static QuestCardInteractionState ResolveQuestCardInteractionState(GameCard card)
        {
            if (IsQuestFieldSelectionTarget(card))
                return QuestCardInteractionState.SelectionTarget;
            if (IsQuestActionableCard(card))
                return QuestCardInteractionState.Actionable;
            return QuestCardInteractionState.None;
        }

        private static Vector3 ApplyQuestCardInteractionLift(
            Vector3 localPosition,
            GameCard card,
            QuestCardInteractionState state,
            bool hovered)
        {
            var lift = 0f;
            var bob = 0f;
            var speed = 0f;
            switch (state)
            {
                case QuestCardInteractionState.SelectionTarget:
                    lift = SelectionTargetCardLift;
                    bob = SelectionTargetCardBob;
                    speed = 3.3f;
                    break;
                case QuestCardInteractionState.Actionable:
                    lift = ActionableCardLift;
                    bob = ActionableCardBob;
                    speed = 2.8f;
                    break;
            }

            if (hovered)
                lift += HoveredCardLift;

            if (lift <= 0f)
                return localPosition;

            var phase = Mathf.Abs(card == null ? 0 : card.md5 % 997) * 0.031f;
            var pulse = bob <= 0f ? 0f : (Mathf.Sin(Time.unscaledTime * speed + phase) + 1f) * 0.5f * bob;
            localPosition.y += lift + pulse;
            return localPosition;
        }

        private static void SetInteractionObject(GameObject target, bool active, float baseExpand, float pulseExpand)
        {
            if (target == null)
                return;

            if (target.activeSelf != active)
                target.SetActive(active);
            if (!active)
                return;

            var pulse = (Mathf.Sin(Time.unscaledTime * 5.5f) + 1f) * 0.5f;
            var expand = baseExpand + pulse * pulseExpand;
            target.transform.localScale = new Vector3(CardWidth + expand, 0.012f, CardHeight + expand);
        }

        private static void SetActionMarkerObject(GameObject target, bool active)
        {
            if (target == null)
                return;

            if (target.activeSelf != active)
                target.SetActive(active);
            if (!active)
                return;

            var pulse = (Mathf.Sin(Time.unscaledTime * 5.5f) + 1f) * 0.5f;
            target.transform.localPosition = new Vector3(0f, CardThickness + 0.022f, ActionMarkerZ);
            target.transform.localScale = new Vector3(
                ActionMarkerBaseWidth + pulse * 0.34f,
                0.014f,
                ActionMarkerBaseDepth + pulse * 0.055f);
        }

        private static void SetTargetMarkerObject(GameObject target, bool active)
        {
            if (target == null)
                return;

            if (target.activeSelf != active)
                target.SetActive(active);
            if (!active)
                return;

            var pulse = (Mathf.Sin(Time.unscaledTime * 5.9f) + 1f) * 0.5f;
            target.transform.localPosition = new Vector3(0f, CardThickness + 0.028f, ActionMarkerZ);
            target.transform.localScale = new Vector3(
                CardWidth * 0.76f + pulse * 0.42f,
                0.016f,
                ActionMarkerBaseDepth * 1.18f + pulse * 0.07f);
        }

        private static void SetHandAccentObject(GameObject target, bool active, bool emphasized, float normalizedEdge)
        {
            if (target == null)
                return;

            if (target.activeSelf != active)
                target.SetActive(active);
            if (!active)
                return;

            var pulse = (Mathf.Sin(Time.unscaledTime * (emphasized ? 6.6f : 3.6f) + normalizedEdge * 2.4f) + 1f) * 0.5f;
            var emphasis = emphasized ? 1f : 0f;
            var width = HandAccentBaseWidth + pulse * (0.22f + emphasis * 0.35f);
            var depth = HandAccentBaseDepth + pulse * (0.035f + emphasis * 0.055f);
            target.transform.localPosition = new Vector3(0f, CardThickness + 0.018f, HandAccentZ);
            target.transform.localScale = new Vector3(width, 0.01f, depth);
        }

        private static bool IsQuestActionableCard(GameCard card)
        {
            if (card == null || card.p == null)
                return false;
            if (card.buttons == null || card.buttons.Count == 0)
                return false;
            if ((card.p.location & (uint)(CardLocation.Hand | CardLocation.Onfield | CardLocation.Grave | CardLocation.Removed)) == 0)
                return false;
            return true;
        }

        private void UpdatePowerLabel(QuestCardProxy proxy, GameCard card)
        {
            if (proxy == null || proxy.PowerLabelRoot == null || proxy.PowerLabelText == null)
                return;

            if (!ShouldShowPowerLabel(card))
            {
                if (proxy.PowerLabelRoot.activeSelf)
                    proxy.PowerLabelRoot.SetActive(false);
                return;
            }

            var data = card.GetData();
            if (!proxy.PowerLabelRoot.activeSelf)
                proxy.PowerLabelRoot.SetActive(true);

            proxy.PowerLabelText.text = FormatPowerLabel(card, data);
            proxy.PowerLabelText.color = Color.white;
            FaceTextToCamera(proxy.PowerLabelRoot.transform);
        }

        private static bool ShouldShowPowerLabel(GameCard card)
        {
            if (card == null || card.p == null || card.GetData() == null)
                return false;
            if (!card.GetData().HasType(CardType.Monster))
                return false;
            if ((card.p.location & (uint)CardLocation.MonsterZone) == 0)
                return false;
            if ((card.p.location & (uint)CardLocation.Overlay) > 0)
                return false;
            if ((card.p.position & (uint)CardPosition.FaceUp) == 0)
                return false;
            return true;
        }

        private static Color ResolvePowerLabelColor(int current, int original)
        {
            if (current < 0 || original < 0)
                return Color.white;
            if (current > original)
                return new Color(0.32f, 1f, 0.58f, 1f);
            if (current < original)
                return new Color(1f, 0.34f, 0.28f, 1f);
            return Color.white;
        }

        private static string FormatPowerLabel(GameCard card, Card data)
        {
            if (data == null)
                return string.Empty;

            var extras = GetMonsterGradeLabel(data) + " " + GetMonsterGradeValue(data);
            if (data.HasType(CardType.Xyz))
            {
                var materialCount = 0;
                try
                {
                    materialCount = Program.instance?.ocgcore == null ? 0 : Program.instance.ocgcore.GCS_GetOverlays(card).Count;
                }
                catch
                {
                    materialCount = 0;
                }
                extras += "  \u7d20\u6750 " + materialCount;
            }
            if (data.HasType(CardType.Tuner))
                extras += "  \u8c03\u6574";

            var grade = ColorizePowerLine(string.Empty, extras, new Color(1f, 0.84f, 0.30f, 1f), 0);
            var attackActive = card != null && card.p != null && ((card.p.position & (uint)CardPosition.Attack) > 0 || data.HasType(CardType.Link));
            var defenseActive = !data.HasType(CardType.Link) && !attackActive;
            var stance = data.HasType(CardType.Link)
                ? "LINK"
                : attackActive ? "\u653b\u51fb\u8868\u793a" : "\u5b88\u5907\u8868\u793a";
            var attack = ColorizePowerLine("ATK", data.GetAttackString(), ResolvePowerLabelColor(data.Attack, data.rAttack), attackActive ? 20 : 15);
            if (data.HasType(CardType.Link))
                return grade + "  " + ColorizePowerLine(string.Empty, stance, new Color(0.72f, 0.92f, 1f, 1f), 0) + "\n" + attack;

            return grade
                + "  " + ColorizePowerLine(string.Empty, stance, new Color(0.72f, 0.92f, 1f, 1f), 0)
                + "\n"
                + attack
                + "   "
                + ColorizePowerLine("DEF", data.GetDefenseString(), ResolvePowerLabelColor(data.Defense, data.rDefense), defenseActive ? 20 : 15);
        }

        private static string GetMonsterGradeLabel(Card data)
        {
            if (data == null)
                return string.Empty;

            switch (data.GetLevelType())
            {
                case Card.LevelType.Rank:
                    return "\u9636\u7ea7";
                case Card.LevelType.Link:
                    return "LINK";
                default:
                    return "\u2605";
            }
        }

        private static string GetMonsterGradeValue(Card data)
        {
            if (data == null)
                return string.Empty;

            if (data.HasType(CardType.Link))
                return data.GetLinkCount().ToString();
            return Mathf.Max(0, data.Level).ToString();
        }

        private static string ColorizePowerLine(string label, string value, Color color, int size = 0)
        {
            var content = string.IsNullOrEmpty(label) ? value : label + " " + value;
            if (size > 0)
                content = "<size=" + size + ">" + content + "</size>";
            return "<mark=#061016D8><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + content + "</color></mark>";
        }

        private static bool IsQuestFieldSelectionTarget(GameCard card)
        {
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (card == null || core == null || core.places == null)
                return false;

            foreach (var place in core.places)
                if (place != null && place.cardSelecting && place.cookieCard == card)
                    return true;

            return false;
        }

        private static Vector3 ResolveQuestCardLocalPosition(GameCard card)
        {
            var position = ScaleQuestBoardPosition(GameCard.GetCardPosition(card.p, card, card.overlayParent));
            if ((card.p.location & (uint)CardLocation.Hand) > 0)
            {
                return ResolveQuestHandLocalPosition(card.p);
            }

            if ((card.p.location & (uint)(CardLocation.Grave | CardLocation.Removed)) > 0)
            {
                position.y = TableCardY + Mathf.Clamp((int)card.p.sequence, 0, 8) * 0.035f;
                return position;
            }

            if ((card.p.location & (uint)CardLocation.Overlay) > 0)
            {
                position.y = TableCardY + 0.05f + Mathf.Clamp(card.p.position, 0, 8) * 0.025f;
                return position;
            }

            position.y = TableCardY;
            return position;
        }

        private static Quaternion ResolveQuestCardLocalRotation(GameCard card)
        {
            var euler = GameCard.GetCardRotation(card.p, card.GetData().Id);
            if ((card.p.location & (uint)CardLocation.Hand) > 0)
            {
                var offset = ResolveQuestHandSlotOffset(card.p);
                var normalizedEdge = ResolveQuestHandNormalizedEdge(card.p);
                var yaw = Mathf.Clamp(offset * HandCardFanYaw, -HandCardMaxFanYaw, HandCardMaxFanYaw);
                var roll = Mathf.Clamp(-offset * HandCardFanRoll, -9f, 9f);
                if (card.p.controller == 0)
                {
                    yaw = -yaw;
                }
                else
                {
                    roll = -roll;
                    euler.x += normalizedEdge * 0.8f;
                }

                euler.x -= normalizedEdge * 1.2f;
                euler.y += yaw;
                return Quaternion.Euler(euler.x, euler.y, roll);
            }
            return Quaternion.Euler(euler.x, euler.y, 0f);
        }

        private static Vector3 ResolveQuestCardLocalScale(GameCard card, QuestCardInteractionState state, bool hovered)
        {
            var scale = GameCard.GetCardScale(card.p);
            if ((card.p.location & (uint)CardLocation.Hand) == 0)
                return scale;

            var normalizedEdge = ResolveQuestHandNormalizedEdge(card.p);
            var handScale = card.p.controller == 0
                ? Mathf.Lerp(HandCardCenterScale, HandCardEdgeScale, normalizedEdge * normalizedEdge)
                : Mathf.Lerp(0.94f, 0.88f, normalizedEdge);

            if (state != QuestCardInteractionState.None)
                handScale += 0.035f;
            if (hovered)
                handScale += HandCardHoverScale;

            return scale * handScale;
        }

        private static Vector3 ResolveQuestHandLocalPosition(GPS gps)
        {
            var offset = ResolveQuestHandSlotOffset(gps);
            var count = ResolveQuestHandCount(gps);
            var crowding = Mathf.InverseLerp(7f, 13f, count);
            var spacing = Mathf.Lerp(HandCardBaseSpacing, HandCardMinSpacing, crowding);
            var x = offset * spacing;
            if (gps.controller != 0)
                x = -x;

            var edge = Mathf.Abs(offset);
            var normalizedEdge = ResolveQuestHandNormalizedEdge(gps);
            var arc = normalizedEdge * normalizedEdge;
            var z = gps.controller == 0
                ? PlayerHandBaseZ + edge * HandCardFanDepth + arc * 0.62f
                : OpponentHandBaseZ - edge * HandCardFanDepth - arc * 0.52f;
            var phase = (float)gps.sequence * 0.47f + (gps.controller == 0 ? 0f : 1.3f);
            var floatWave = Mathf.Sin(Time.unscaledTime * (1.35f + normalizedEdge * 0.45f) + phase) * HandCardFloatAmplitude;
            var y = HandCardY + HandCardCenterLift * (1f - arc) - HandCardEdgeDrop * arc + floatWave;
            return new Vector3(x, y, z);
        }

        private static float ResolveQuestHandSlotOffset(GPS gps)
        {
            if (gps == null)
                return 0f;

            var count = ResolveQuestHandCount(gps);
            return (int)gps.sequence - (count - 1) * 0.5f;
        }

        private static float ResolveQuestHandNormalizedEdge(GPS gps)
        {
            if (gps == null)
                return 0f;

            var count = ResolveQuestHandCount(gps);
            var maxEdge = Mathf.Max((count - 1) * 0.5f, 1f);
            return Mathf.Clamp01(Mathf.Abs(ResolveQuestHandSlotOffset(gps)) / maxEdge);
        }

        private static int ResolveQuestHandCount(GPS gps)
        {
            if (gps == null)
                return 1;

            var fallback = Mathf.Max(1, (int)gps.sequence + 1);
            var core = Program.instance == null ? null : Program.instance.ocgcore;
            if (core == null)
                return fallback;

            try
            {
                var count = gps.controller == 0 ? core.GetMyHandCount() : core.GetOpHandCount();
                return Mathf.Max(fallback, count);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ShouldShowKnownFace(GameCard card)
        {
            if (card == null || card.p == null || card.GetData() == null || card.GetData().Id <= 0)
                return false;

            if ((card.p.position & (uint)CardPosition.FaceUp) > 0)
                return true;
            if (card.p.controller == 0 && (card.p.location & (uint)CardLocation.Hand) > 0)
                return true;
            if ((card.p.location & (uint)(CardLocation.Grave | CardLocation.Removed)) > 0)
                return true;

            return false;
        }

        private void EnsureProxyFaceTexture(QuestCardProxy proxy, int code)
        {
            if (proxy == null || code <= 0 || proxy.LoadedFaceCode == code || proxy.LoadingFaceCode == code)
                return;

            proxy.LoadingFaceCode = code;
            StartCoroutine(LoadProxyFaceTexture(proxy, code));
        }

        private IEnumerator LoadProxyFaceTexture(QuestCardProxy proxy, int code)
        {
            var task = TextureManager.LoadQuestFieldCardTextureAsync(code, true);
            while (!task.IsCompleted)
                yield return null;

            if (proxy == null || proxy.FrontRenderer == null || proxy.LoadingFaceCode != code)
                yield break;

            var texture = task.Result;
            if (texture != null)
            {
                var material = new Material(GetPlaceholderFaceMaterial()) { name = "QuestCardFace_" + code };
                ApplyTexture(material, texture);
                proxy.FrontRenderer.sharedMaterial = material;
                proxy.LoadedFaceCode = code;
            }

            proxy.LoadingFaceCode = 0;
        }

        private void UpdatePortraitProxy(QuestCardProxy proxy, GameCard card)
        {
            if (proxy == null || proxy.Portrait == null || card == null || card.p == null || card.GetData() == null)
                return;

            var showPortrait = ShouldShowPortrait(card);
            if (proxy.Portrait.activeSelf != showPortrait)
                proxy.Portrait.SetActive(showPortrait);
            if (!showPortrait)
                return;

            EnsurePortraitTexture(proxy, card.GetData().Id);
            FacePortraitToCamera(proxy.Portrait.transform);
        }

        private static bool ShouldShowPortrait(GameCard card)
        {
            if (card == null || card.p == null || card.GetData() == null)
                return false;
            if (!card.GetData().HasType(CardType.Monster))
                return false;
            if ((card.p.location & (uint)CardLocation.MonsterZone) == 0)
                return false;
            if ((card.p.location & (uint)CardLocation.Overlay) > 0)
                return false;
            if ((card.p.position & (uint)CardPosition.FaceUp) == 0)
                return false;
            return card.GetData().Id > 0;
        }

        private void EnsurePortraitTexture(QuestCardProxy proxy, int code)
        {
            if (proxy == null || code <= 0 || proxy.LoadedPortraitCode == code || proxy.LoadingPortraitCode == code)
                return;

            proxy.LoadingPortraitCode = code;
            StartCoroutine(LoadPortraitTexture(proxy, code));
        }

        private IEnumerator LoadPortraitTexture(QuestCardProxy proxy, int code)
        {
            var task = TextureManager.LoadUiPortraitAsync(code, true);
            while (!task.IsCompleted)
                yield return null;

            if (proxy == null || proxy.PortraitRenderer == null || proxy.LoadingPortraitCode != code)
                yield break;

            var texture = task.Result;
            if (texture != null)
            {
                var material = new Material(GetPortraitMaterial()) { name = "QuestMonsterPortrait_" + code };
                ApplyTexture(material, texture);
                ConfigureTransparentMaterial(material);
                proxy.PortraitRenderer.sharedMaterial = material;
                proxy.LoadedPortraitCode = code;

                var aspect = texture.height <= 0 ? 0.72f : (float)texture.width / texture.height;
                var width = Mathf.Clamp(PortraitHeight * aspect, 2.4f, PortraitMaxWidth);
                proxy.Portrait.transform.localScale = new Vector3(width, PortraitHeight, 1f);
                proxy.Portrait.transform.localPosition = new Vector3(0f, PortraitHeight * 0.5f + CardThickness + 0.15f, 0f);
            }

            proxy.LoadingPortraitCode = 0;
        }

        private void FacePortraitToCamera(Transform target)
        {
            if (target == null || xrCamera == null)
                return;

            var forward = target.position - xrCamera.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            target.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void RemoveStaleCardProxies()
        {
            staleCards.Clear();
            foreach (var pair in cardProxies)
            {
                if (!visibleCards.Contains(pair.Key))
                    staleCards.Add(pair.Key);
            }

            foreach (var card in staleCards)
            {
                if (cardProxies.TryGetValue(card, out var proxy) && proxy.Root != null)
                    Destroy(proxy.Root);
                cardProxies.Remove(card);
            }
        }

        private void UpdatePileProxies(OcgCore core)
        {
            UpdatePileProxy(core, 0, CardLocation.Deck, "\u4e3b\u5361\u7ec4");
            UpdatePileProxy(core, 1, CardLocation.Deck, "\u4e3b\u5361\u7ec4");
            UpdatePileProxy(core, 0, CardLocation.Extra, "\u989d\u5916");
            UpdatePileProxy(core, 1, CardLocation.Extra, "\u989d\u5916");
        }

        private void UpdatePileProxy(OcgCore core, uint controller, CardLocation location, string label)
        {
            if (core == null)
                return;

            var count = core.GetLocationCardCount(location, controller);
            var key = controller + "_" + location;
            if (!pileProxies.TryGetValue(key, out var pile) || pile.Root == null)
            {
                pile = QuestPileProxy.Create(key, pileRoot, GetCardSideMaterial(), GetCardBackMaterial(), GetPileFaceMaterial());
                pileProxies[key] = pile;
            }

            pile.Root.SetActive(true);
            pile.SetVisibleCount(count);

            var gps = new GPS
            {
                controller = controller,
                location = (uint)location,
                sequence = 0,
                position = (int)CardPosition.FaceDownAttack
            };
            var position = ScaleQuestBoardPosition(GameCard.GetCardPosition(gps));
            position.y = PileCardY;
            var rotation = GameCard.GetCardRotation(gps);
            pile.Transform.localPosition = position;
            pile.Transform.localRotation = Quaternion.Euler(0f, rotation.y, 0f);
            pile.Label.text = (controller == 0 ? "\u6211\u65b9 " : "\u5bf9\u65b9 ") + label + "\n" + count + " \u5f20";
            pile.Hit.Controller = controller;
            pile.Hit.Location = location;
            FaceTextToCamera(pile.Label.transform);
        }

        private static Vector3 ScaleQuestBoardPosition(Vector3 position)
        {
            position.x *= QuestBoardScaleX;
            position.z *= QuestBoardScaleZ;
            return position;
        }

        private void FaceTextToCamera(Transform target)
        {
            if (target == null || xrCamera == null)
                return;

            var forward = target.position - xrCamera.transform.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return;

            target.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        private void SuppressLegacyDuelContainer(Transform legacyDuelContainer)
        {
            if (legacyDuelContainer == null)
            {
                cachedLegacyRenderers.Clear();
                cachedLegacyColliders.Clear();
                cachedLegacyCanvases.Clear();
                lastSuppressedLegacyDuelContainer = null;
                return;
            }

            var hiddenThisFrame = 0;
            var disabledThisFrame = 0;
            var now = Time.unscaledTime;
            if (legacyDuelContainer != lastSuppressedLegacyDuelContainer || now >= nextLegacySuppressionScanTime)
            {
                cachedLegacyRenderers.Clear();
                cachedLegacyColliders.Clear();
                cachedLegacyCanvases.Clear();
                lastSuppressedLegacyDuelContainer = legacyDuelContainer;
                nextLegacySuppressionScanTime = now + LegacySuppressionRescanInterval;

                foreach (var renderer in legacyDuelContainer.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null || renderer.GetComponentInParent<QuestCardProxyHit>() != null)
                        continue;

                    cachedLegacyRenderers.Add(renderer);
                    if (SuppressLegacyRenderer(renderer))
                    {
                        hiddenLegacyRendererCount += 1;
                        hiddenThisFrame += 1;
                    }
                }

                foreach (var collider in legacyDuelContainer.GetComponentsInChildren<Collider>(true))
                {
                    if (collider == null || collider.GetComponentInParent<QuestCardProxyHit>() != null)
                        continue;

                    cachedLegacyColliders.Add(collider);
                    if (SuppressLegacyCollider(collider))
                    {
                        disabledLegacyColliderCount += 1;
                        disabledThisFrame += 1;
                    }
                }

                foreach (var canvas in legacyDuelContainer.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas == null)
                        continue;

                    cachedLegacyCanvases.Add(canvas);
                    canvas.enabled = false;
                }
            }
            else
            {
                for (var i = cachedLegacyRenderers.Count - 1; i >= 0; i--)
                {
                    var renderer = cachedLegacyRenderers[i];
                    if (renderer == null)
                    {
                        cachedLegacyRenderers.RemoveAt(i);
                        continue;
                    }

                    SuppressLegacyRenderer(renderer);
                }

                for (var i = cachedLegacyColliders.Count - 1; i >= 0; i--)
                {
                    var collider = cachedLegacyColliders[i];
                    if (collider == null)
                    {
                        cachedLegacyColliders.RemoveAt(i);
                        continue;
                    }

                    SuppressLegacyCollider(collider);
                }

                for (var i = cachedLegacyCanvases.Count - 1; i >= 0; i--)
                {
                    var canvas = cachedLegacyCanvases[i];
                    if (canvas == null)
                    {
                        cachedLegacyCanvases.RemoveAt(i);
                        continue;
                    }

                    canvas.enabled = false;
                }
            }

            if (!legacySuppressionLogged && (hiddenThisFrame > 0 || disabledThisFrame > 0))
            {
                legacySuppressionLogged = true;
                Debug.LogFormat(
                    "Quest native duel presenter isolated legacy MDPro duel container. RenderersOff={0}, CollidersDisabled={1}",
                    hiddenLegacyRendererCount,
                    disabledLegacyColliderCount);
            }
        }

        private static bool SuppressLegacyRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            var changed = !renderer.forceRenderingOff || renderer.enabled;
            renderer.forceRenderingOff = true;
            renderer.enabled = false;
            return changed;
        }

        private static bool SuppressLegacyCollider(Collider collider)
        {
            if (collider == null)
                return false;

            var changed = collider.enabled;
            collider.enabled = false;
            return changed;
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var names = new List<string>();
            var current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }

        private void LogDiagnostics(OcgCore core, Transform legacyDuelContainer)
        {
            if (!QuestVerboseProxyDiagnostics)
                return;

            if (Time.unscaledTime - lastDiagnosticsTime < ProxyDiagnosticsInterval)
                return;

            lastDiagnosticsTime = Time.unscaledTime;
            var legacyVisible = 0;
            if (legacyDuelContainer != null)
            {
                foreach (var renderer in legacyDuelContainer.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer != null && renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy)
                        legacyVisible += 1;
                }
            }

            Debug.LogFormat(
                "Quest native duel diagnostics. Cards={0}, Proxies={1}, Piles={2}, LegacyVisibleRenderers={3}, Message={4}, Popup={5}",
                core == null || core.cards == null ? 0 : core.cards.Count,
                cardProxies.Count,
                pileProxies.Count,
                legacyVisible,
                core == null ? "<none>" : core.currentMessage.ToString(),
                core != null && core.currentPopup != null);
        }

        private void CaptureDebugFrameIfUseful()
        {
            if (!QuestAutoDebugCapture)
                return;

            if (automaticDebugCaptureCount >= MaxAutomaticDebugCaptures)
                return;
            if (Time.unscaledTime < 5f)
                return;
            if (Time.unscaledTime - lastDebugCaptureTime < 7f)
                return;

            lastDebugCaptureTime = Time.unscaledTime;
            automaticDebugCaptureCount += 1;

            try
            {
                var directory = Path.Combine(Application.persistentDataPath, "QuestDebug");
                Directory.CreateDirectory(directory);
                var fileName = "quest-debug-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + automaticDebugCaptureCount + ".png";
                var relativePath = Path.Combine("QuestDebug", fileName).Replace('\\', '/');
                ScreenCapture.CaptureScreenshot(relativePath);
                Debug.Log("Quest debug screenshot requested: " + Path.Combine(directory, fileName));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Quest debug screenshot request failed: " + ex.Message);
            }
        }

        private static bool TryCollectWorldBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
                return false;

            var initialized = false;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(false))
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (initialized)
                return true;

            foreach (var collider in root.GetComponentsInChildren<Collider>(false))
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;

                if (!initialized)
                {
                    bounds = collider.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return initialized;
        }

        private Material GetCardSideMaterial()
        {
            if (cardSideMaterial != null)
                return cardSideMaterial;

            cardSideMaterial = CreateMaterial("QuestCardSideMaterial", new Color(0.035f, 0.038f, 0.043f, 1f), false);
            return cardSideMaterial;
        }

        private Material GetCardBackMaterial()
        {
            if (cardBackMaterial != null)
                return cardBackMaterial;

            cardBackMaterial = CreateMaterial("QuestCardBackMaterial", new Color(0.05f, 0.09f, 0.16f, 1f), false);
            ApplyTexture(cardBackMaterial, LoadPreferredCardBackTexture() ?? CreateQuestFallbackCardBackTexture());
            return cardBackMaterial;
        }

        private Material GetPlaceholderFaceMaterial()
        {
            if (placeholderFaceMaterial != null)
                return placeholderFaceMaterial;

            placeholderFaceMaterial = CreateMaterial("QuestCardFacePlaceholderMaterial", new Color(0.18f, 0.16f, 0.11f, 1f), false);
            return placeholderFaceMaterial;
        }

        private Material GetPileFaceMaterial()
        {
            if (pileFaceMaterial != null)
                return pileFaceMaterial;

            pileFaceMaterial = CreateMaterial("QuestPileFaceMaterial", new Color(0.02f, 0.26f, 0.30f, 1f), false);
            return pileFaceMaterial;
        }

        private Material GetPortraitMaterial()
        {
            if (portraitMaterial != null)
                return portraitMaterial;

            portraitMaterial = CreateMaterial("QuestMonsterPortraitMaterial", Color.white, true);
            ConfigureTransparentMaterial(portraitMaterial);
            return portraitMaterial;
        }

        private Material GetHighlightMaterial()
        {
            if (highlightMaterial != null)
                return highlightMaterial;

            highlightMaterial = CreateMaterial("QuestCardActionHighlightMaterial", new Color(0.15f, 1f, 0.62f, 0.36f), true);
            return highlightMaterial;
        }

        private Material GetActionHighlightMaterial()
        {
            if (actionHighlightMaterial != null)
                return actionHighlightMaterial;

            actionHighlightMaterial = CreateMaterial("QuestCardPlayableHighlightMaterial", new Color(1f, 0.72f, 0.12f, 0.70f), true);
            return actionHighlightMaterial;
        }

        private Material GetTargetHighlightMaterial()
        {
            if (targetHighlightMaterial != null)
                return targetHighlightMaterial;

            targetHighlightMaterial = CreateMaterial("QuestCardTargetHighlightMaterial", new Color(0.10f, 0.88f, 1f, 0.46f), true);
            return targetHighlightMaterial;
        }

        private Material GetHandAccentMaterial()
        {
            if (handAccentMaterial != null)
                return handAccentMaterial;

            handAccentMaterial = CreateMaterial("QuestHandAccentMaterial", new Color(0.22f, 0.95f, 1f, 0.28f), true);
            return handAccentMaterial;
        }

        private static Material CreateMaterial(string name, Color color, bool transparent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name };
            SetMaterialColor(material, color);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);

            if (transparent)
            {
                ConfigureTransparentMaterial(material);
            }
            else
            {
                if (material.HasProperty("_ZWrite"))
                    material.SetFloat("_ZWrite", 1f);
                material.renderQueue = (int)RenderQueue.Geometry;
            }

            return material;
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            if (material == null)
                return;

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

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;

            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void ApplyTexture(Material material, Texture texture)
        {
            if (material == null || texture == null)
                return;

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            SetMaterialColor(material, Color.white);
        }

        private static Texture2D LoadPreferredCardBackTexture()
        {
            if (preferredCardBackTexture != null)
                return preferredCardBackTexture;

            foreach (var path in GetPreferredCardBackPaths())
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                try
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        name = "QuestYGOPro2CardBack",
                        filterMode = FilterMode.Trilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 8
                    };
                    if (texture.LoadImage(File.ReadAllBytes(path), false))
                    {
                        preferredCardBackTexture = texture;
                        Debug.Log("Quest card back loaded from: " + path);
                        return preferredCardBackTexture;
                    }

                    Destroy(texture);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Quest card back load failed: " + path + " / " + ex.Message);
                }
            }

            return null;
        }

        private static IEnumerable<string> GetPreferredCardBackPaths()
        {
            yield return Path.Combine(Program.expansionsPath, PreferredCardBackRelativePath);
            yield return Path.Combine("Expansions", PreferredCardBackRelativePath);
        }

        private static Texture2D CreateQuestFallbackCardBackTexture()
        {
            if (fallbackCardBackTexture != null)
                return fallbackCardBackTexture;

            const int width = 512;
            const int height = 720;
            fallbackCardBackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "QuestFallbackCardBack",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var border = Mathf.RoundToInt(width * 0.055f);
            for (var y = 0; y < height; y += 1)
            {
                var v = y / (float)(height - 1);
                for (var x = 0; x < width; x += 1)
                {
                    var u = x / (float)(width - 1);
                    var inner = x >= border && x < width - border && y >= border && y < height - border;
                    var ring = Mathf.Sin((u * 10.5f + v * 13.5f) * Mathf.PI) * 0.5f + 0.5f;
                    var glow = Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 1.8f) * Mathf.Clamp01(1f - Mathf.Abs(v - 0.5f) * 1.4f);
                    Color color;
                    if (!inner)
                    {
                        color = Color.Lerp(new Color(0.74f, 0.48f, 0.13f, 1f), new Color(1f, 0.84f, 0.35f, 1f), ring * 0.35f + glow * 0.25f);
                    }
                    else
                    {
                        var baseColor = Color.Lerp(new Color(0.025f, 0.08f, 0.20f, 1f), new Color(0.02f, 0.20f, 0.34f, 1f), v);
                        var line = Mathf.Pow(ring, 10f) * 0.18f;
                        color = Color.Lerp(baseColor, new Color(0.12f, 0.72f, 0.90f, 1f), line + glow * 0.20f);
                    }

                    fallbackCardBackTexture.SetPixel(x, y, color);
                }
            }

            fallbackCardBackTexture.Apply(false, true);
            return fallbackCardBackTexture;
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

        private sealed class QuestCardProxy
        {
            public GameObject Root;
            public Transform Transform;
            public GameObject Front;
            public GameObject Back;
            public GameObject Highlight;
            public GameObject ActionHighlight;
            public GameObject TargetHighlight;
            public GameObject HandAccent;
            public GameObject Portrait;
            public GameObject PowerLabelRoot;
            public GameObject InteractionLabelRoot;
            public MeshRenderer FrontRenderer;
            public MeshRenderer PortraitRenderer;
            public TextMeshPro PowerLabelText;
            public TextMeshPro InteractionLabelText;
            public QuestCardProxyHit Hit;
            public GameCard Card;
            public int LoadedFaceCode;
            public int LoadingFaceCode;
            public int LoadedPortraitCode;
            public int LoadingPortraitCode;

            public static QuestCardProxy Create(
                GameCard card,
                Transform parent,
                Material sideMaterial,
                Material backMaterial,
                Material faceMaterial,
                Material highlightMaterial,
                Material actionHighlightMaterial,
                Material targetHighlightMaterial,
                Material handAccentMaterial)
            {
                var root = new GameObject("QuestCardProxy_" + (card == null ? "null" : card.md5.ToString()));
                SetQuestOverlayLayer(root);
                root.transform.SetParent(parent, false);
                var hit = root.AddComponent<QuestCardProxyHit>();
                hit.Card = card;
                root.AddComponent<GameCardMono>().cookieCard = card;
                var collider = root.AddComponent<BoxCollider>();
                collider.size = new Vector3(CardWidth, CardThickness * 2.5f, CardHeight);
                collider.center = new Vector3(0f, CardThickness * 0.5f, 0f);

                var side = GameObject.CreatePrimitive(PrimitiveType.Cube);
                side.name = "QuestCardProxySide";
                SetQuestOverlayLayer(side);
                UnityEngine.Object.Destroy(side.GetComponent<Collider>());
                side.transform.SetParent(root.transform, false);
                side.transform.localPosition = new Vector3(0f, CardThickness * 0.5f, 0f);
                side.transform.localScale = new Vector3(CardWidth, CardThickness, CardHeight);
                ConfigureRenderer(side.GetComponent<MeshRenderer>(), sideMaterial);

                var front = CreateCardQuad("QuestCardProxyFront", root.transform, CardThickness + 0.006f, faceMaterial);
                var back = CreateCardQuad("QuestCardProxyBack", root.transform, CardThickness + 0.012f, backMaterial);
                var portrait = CreatePortraitQuad("QuestCardProxyPortrait", root.transform, faceMaterial);
                var powerLabel = CreatePowerLabel(root.transform);
                var interactionLabel = CreateInteractionLabel(root.transform);
                var highlight = CreateHighlightCube("QuestCardProxySelectionHighlight", root.transform, CardThickness + 0.018f, highlightMaterial);
                var actionHighlight = CreateHighlightCube("QuestCardProxyPlayableHighlight", root.transform, CardThickness + 0.020f, actionHighlightMaterial);
                var targetHighlight = CreateHighlightCube("QuestCardProxyTargetHighlight", root.transform, CardThickness + 0.024f, targetHighlightMaterial);
                var handAccent = CreateHighlightCube("QuestCardProxyHandAccent", root.transform, CardThickness + 0.018f, handAccentMaterial);

                return new QuestCardProxy
                {
                    Root = root,
                    Transform = root.transform,
                    Front = front,
                    Back = back,
                    Highlight = highlight,
                    ActionHighlight = actionHighlight,
                    TargetHighlight = targetHighlight,
                    HandAccent = handAccent,
                    Portrait = portrait,
                    PowerLabelRoot = powerLabel,
                    InteractionLabelRoot = interactionLabel,
                    FrontRenderer = front.GetComponent<MeshRenderer>(),
                    PortraitRenderer = portrait.GetComponent<MeshRenderer>(),
                    PowerLabelText = powerLabel.GetComponentInChildren<TextMeshPro>(true),
                    InteractionLabelText = interactionLabel.GetComponentInChildren<TextMeshPro>(true),
                    Hit = hit,
                    Card = card
                };
            }

            private static GameObject CreateHighlightCube(string name, Transform parent, float y, Material material)
            {
                var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                highlight.name = name;
                SetQuestOverlayLayer(highlight);
                UnityEngine.Object.Destroy(highlight.GetComponent<Collider>());
                highlight.transform.SetParent(parent, false);
                highlight.transform.localPosition = new Vector3(0f, y, 0f);
                highlight.transform.localScale = new Vector3(CardWidth + 0.26f, 0.012f, CardHeight + 0.26f);
                ConfigureRenderer(highlight.GetComponent<MeshRenderer>(), material);
                highlight.SetActive(false);
                return highlight;
            }

            private static GameObject CreateCardQuad(string name, Transform parent, float y, Material material)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = name;
                SetQuestOverlayLayer(quad);
                UnityEngine.Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(parent, false);
                quad.transform.localPosition = new Vector3(0f, y, 0f);
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(CardWidth, CardHeight, 1f);
                ConfigureRenderer(quad.GetComponent<MeshRenderer>(), material);
                return quad;
            }

            private static GameObject CreatePortraitQuad(string name, Transform parent, Material material)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = name;
                SetQuestOverlayLayer(quad);
                UnityEngine.Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(parent, false);
                quad.transform.localPosition = new Vector3(0f, PortraitHeight * 0.5f + CardThickness + 0.15f, 0f);
                quad.transform.localRotation = Quaternion.identity;
                quad.transform.localScale = new Vector3(PortraitHeight * 0.72f, PortraitHeight, 1f);
                ConfigureRenderer(quad.GetComponent<MeshRenderer>(), material);
                quad.SetActive(false);
                return quad;
            }

            private static GameObject CreatePowerLabel(Transform parent)
            {
                var labelRoot = new GameObject("QuestCardProxyPowerLabel");
                SetQuestOverlayLayer(labelRoot);
                labelRoot.transform.SetParent(parent, false);
                labelRoot.transform.localPosition = new Vector3(0f, PowerLabelY, PowerLabelZ);
                labelRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                labelRoot.transform.localScale = Vector3.one * PowerLabelScale;

                var textObject = new GameObject("QuestCardProxyPowerText");
                SetQuestOverlayLayer(textObject);
                textObject.transform.SetParent(labelRoot.transform, false);
                textObject.transform.localPosition = Vector3.zero;
                textObject.transform.localRotation = Quaternion.identity;
                textObject.transform.localScale = Vector3.one;

                var text = textObject.AddComponent<TextMeshPro>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 20f;
                text.fontStyle = FontStyles.Bold;
                text.richText = true;
                text.enableWordWrapping = false;
                text.text = string.Empty;
                text.color = Color.white;
                text.outlineWidth = 0.28f;
                text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
                text.margin = new Vector4(0.6f, 0.3f, 0.6f, 0.3f);

                labelRoot.SetActive(false);
                return labelRoot;
            }

            private static GameObject CreateInteractionLabel(Transform parent)
            {
                var labelRoot = new GameObject("QuestCardProxyInteractionLabel");
                SetQuestOverlayLayer(labelRoot);
                labelRoot.transform.SetParent(parent, false);
                labelRoot.transform.localPosition = new Vector3(0f, InteractionLabelY, InteractionLabelZ);
                labelRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                labelRoot.transform.localScale = Vector3.one * InteractionLabelScale;

                var textObject = new GameObject("QuestCardProxyInteractionText");
                SetQuestOverlayLayer(textObject);
                textObject.transform.SetParent(labelRoot.transform, false);
                textObject.transform.localPosition = Vector3.zero;
                textObject.transform.localRotation = Quaternion.identity;
                textObject.transform.localScale = Vector3.one;

                var text = textObject.AddComponent<TextMeshPro>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = 13.2f;
                text.fontStyle = FontStyles.Bold;
                text.richText = true;
                text.enableWordWrapping = false;
                text.text = string.Empty;
                text.color = Color.white;
                text.outlineWidth = 0.22f;
                text.outlineColor = new Color(0f, 0f, 0f, 0.95f);
                text.margin = new Vector4(0.3f, 0.1f, 0.3f, 0.1f);

                labelRoot.SetActive(false);
                return labelRoot;
            }

            public static void ConfigureRenderer(Renderer renderer, Material material)
            {
                if (renderer == null)
                    return;

                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private sealed class PresentationTransient
        {
            public readonly GameObject Root;
            public readonly Material Material;

            public PresentationTransient(GameObject root, Material material)
            {
                Root = root;
                Material = material;
            }
        }

        private sealed class QuestPileProxy
        {
            public GameObject Root;
            public Transform Transform;
            public TextMeshPro Label;
            public QuestPileProxyHit Hit;
            private List<GameObject> layers = new List<GameObject>();

            public static QuestPileProxy Create(
                string key,
                Transform parent,
                Material sideMaterial,
                Material backMaterial,
                Material faceMaterial)
            {
                var root = new GameObject("QuestPileProxy_" + key);
                SetQuestOverlayLayer(root);
                root.transform.SetParent(parent, false);
                var hit = root.AddComponent<QuestPileProxyHit>();
                var collider = root.AddComponent<BoxCollider>();
                collider.size = new Vector3(CardWidth, 0.75f, CardHeight);
                collider.center = new Vector3(0f, 0.35f, 0f);

                var pileLayers = new List<GameObject>();
                for (var index = 0; index < 5; index += 1)
                    pileLayers.Add(CreatePileLayer(root.transform, index, sideMaterial, backMaterial));

                var labelObject = new GameObject("QuestPileProxyLabel");
                SetQuestOverlayLayer(labelObject);
                labelObject.transform.SetParent(root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.92f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.84f;
                var label = labelObject.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 2.6f;
                label.fontStyle = FontStyles.Bold;
                label.color = new Color(0.88f, 1f, 1f, 1f);
                label.outlineWidth = 0.18f;
                label.outlineColor = new Color(0f, 0f, 0f, 0.95f);
                label.enableWordWrapping = false;
                label.text = string.Empty;

                return new QuestPileProxy
                {
                    Root = root,
                    Transform = root.transform,
                    Label = label,
                    Hit = hit,
                    layers = pileLayers
                };
            }

            public void SetVisibleCount(int count)
            {
                var visibleLayers = Mathf.Clamp(count, 0, layers.Count);
                for (var index = 0; index < layers.Count; index += 1)
                    if (layers[index] != null && layers[index].activeSelf != index < visibleLayers)
                        layers[index].SetActive(index < visibleLayers);
            }

            private static GameObject CreatePileLayer(Transform parent, int index, Material sideMaterial, Material backMaterial)
            {
                var layer = new GameObject("QuestPileLayer_" + index);
                SetQuestOverlayLayer(layer);
                layer.transform.SetParent(parent, false);
                layer.transform.localPosition = new Vector3(0f, index * 0.035f, 0f);

                var side = GameObject.CreatePrimitive(PrimitiveType.Cube);
                side.name = "QuestPileSide";
                SetQuestOverlayLayer(side);
                UnityEngine.Object.Destroy(side.GetComponent<Collider>());
                side.transform.SetParent(layer.transform, false);
                side.transform.localPosition = new Vector3(0f, CardThickness * 0.5f, 0f);
                side.transform.localScale = new Vector3(CardWidth, CardThickness, CardHeight);
                QuestCardProxy.ConfigureRenderer(side.GetComponent<MeshRenderer>(), sideMaterial);

                var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
                back.name = "QuestPileBack";
                SetQuestOverlayLayer(back);
                UnityEngine.Object.Destroy(back.GetComponent<Collider>());
                back.transform.SetParent(layer.transform, false);
                back.transform.localPosition = new Vector3(0f, CardThickness + 0.006f, 0f);
                back.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                back.transform.localScale = new Vector3(CardWidth, CardHeight, 1f);
                QuestCardProxy.ConfigureRenderer(back.GetComponent<MeshRenderer>(), backMaterial);
                return layer;
            }
        }
    }

    public sealed class QuestCardProxyHit : MonoBehaviour
    {
        public GameCard Card;
    }

    public sealed class QuestPileProxyHit : MonoBehaviour
    {
        public uint Controller;
        public CardLocation Location;
    }
}
