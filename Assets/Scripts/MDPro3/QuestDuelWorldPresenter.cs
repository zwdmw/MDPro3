using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MDPro3.YGOSharp;
using MDPro3.YGOSharp.OCGWrapper.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace MDPro3
{
    public sealed class QuestDuelWorldPresenter : MonoBehaviour
    {
        private const int FallbackQuestOverlayLayer = 24;
        private const float CardWidth = 5.05f;
        private const float CardHeight = 7.12f;
        private const float CardThickness = 0.14f;
        private const float TableCardY = 0.52f;
        private const float HandCardY = 7.0f;
        private const float PlayerHandMaxNearZ = -34f;
        private const float PileCardY = 0.5f;
        private const float PortraitHeight = 26f;
        private const float PortraitMaxWidth = 18f;
        private const float PowerLabelY = CardThickness + 0.78f;
        private const float PowerLabelZ = -3.92f;
        private const float PowerLabelScale = 0.23f;
        private const float QuestBoardScaleX = 1.38f;
        private const float QuestBoardScaleZ = 1.34f;
        private const float ProxyDiagnosticsInterval = 3f;
        private const int MaxAutomaticDebugCaptures = 6;

        private Camera xrCamera;
        private Transform worldAnchor;
        private Transform proxyRoot;
        private Transform pileRoot;
        private Material cardBackMaterial;
        private Material cardSideMaterial;
        private Material placeholderFaceMaterial;
        private Material highlightMaterial;
        private Material pileFaceMaterial;
        private Material portraitMaterial;
        private static Texture2D fallbackCardBackTexture;
        private readonly Dictionary<GameCard, QuestCardProxy> cardProxies = new Dictionary<GameCard, QuestCardProxy>();
        private readonly Dictionary<string, QuestPileProxy> pileProxies = new Dictionary<string, QuestPileProxy>();
        private readonly List<GameCard> visibleCards = new List<GameCard>();
        private readonly List<GameCard> staleCards = new List<GameCard>();
        private float lastDiagnosticsTime;
        private float lastDebugCaptureTime;
        private int automaticDebugCaptureCount;
        private int hiddenLegacyRendererCount;
        private int disabledLegacyColliderCount;
        private bool legacySuppressionLogged;

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

            proxy = QuestCardProxy.Create(card, proxyRoot, GetCardSideMaterial(), GetCardBackMaterial(), GetPlaceholderFaceMaterial(), GetHighlightMaterial());
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

            var localPosition = ResolveQuestCardLocalPosition(card);
            var localRotation = ResolveQuestCardLocalRotation(card);
            var localScale = GameCard.GetCardScale(card.p);
            proxy.Transform.localPosition = localPosition;
            proxy.Transform.localRotation = localRotation;
            proxy.Transform.localScale = localScale;

            var knownFace = ShouldShowKnownFace(card);
            proxy.Front.SetActive(knownFace);
            proxy.Back.SetActive(!knownFace);
            proxy.Highlight.SetActive(IsQuestFieldSelectionTarget(card));

            if (knownFace)
                EnsureProxyFaceTexture(proxy, card.GetData().Id);

            UpdatePortraitProxy(proxy, card);
            UpdatePowerLabel(proxy, card);
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

            proxy.PowerLabelText.text = FormatPowerLabel(data);
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

        private static string FormatPowerLabel(Card data)
        {
            if (data == null)
                return string.Empty;

            var grade = ColorizePowerLine(GetMonsterGradeLabel(data), GetMonsterGradeValue(data), new Color(1f, 0.84f, 0.30f, 1f));
            var attack = ColorizePowerLine("ATK", data.GetAttackString(), ResolvePowerLabelColor(data.Attack, data.rAttack));
            if (data.HasType(CardType.Link))
                return grade + "\n" + attack;

            return grade + "\n" + attack + "\n" + ColorizePowerLine("DEF", data.GetDefenseString(), ResolvePowerLabelColor(data.Defense, data.rDefense));
        }

        private static string GetMonsterGradeLabel(Card data)
        {
            if (data == null)
                return string.Empty;

            switch (data.GetLevelType())
            {
                case Card.LevelType.Rank:
                    return "RANK";
                case Card.LevelType.Link:
                    return "LINK";
                default:
                    return "LV";
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

        private static string ColorizePowerLine(string label, string value, Color color)
        {
            return "<mark=#061016D8><color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + label + " " + value + "</color></mark>";
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
                if (card.p.controller == 0)
                    position.z = Mathf.Max(position.z, PlayerHandMaxNearZ);
                position.y = HandCardY;
                return position;
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
            return Quaternion.Euler(euler.x, euler.y, 0f);
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
            UpdatePileProxy(core, 0, CardLocation.Deck, "Deck");
            UpdatePileProxy(core, 1, CardLocation.Deck, "Deck");
            UpdatePileProxy(core, 0, CardLocation.Extra, "Extra");
            UpdatePileProxy(core, 1, CardLocation.Extra, "Extra");
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

            pile.Root.SetActive(count > 0);
            if (count <= 0)
                return;

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
            pile.Label.text = label + "\n" + count.ToString();
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
                return;

            var hiddenThisFrame = 0;
            foreach (var renderer in legacyDuelContainer.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.GetComponentInParent<QuestCardProxyHit>() != null)
                    continue;

                if (!renderer.forceRenderingOff)
                {
                    renderer.forceRenderingOff = true;
                    hiddenLegacyRendererCount += 1;
                    hiddenThisFrame += 1;
                }
                if (renderer.enabled)
                    renderer.enabled = false;
            }

            var disabledThisFrame = 0;
            foreach (var collider in legacyDuelContainer.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null || collider.GetComponentInParent<QuestCardProxyHit>() != null)
                    continue;

                if (collider.enabled)
                {
                    collider.enabled = false;
                    disabledLegacyColliderCount += 1;
                    disabledThisFrame += 1;
                }
            }

            foreach (var canvas in legacyDuelContainer.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas != null && canvas.enabled)
                    canvas.enabled = false;
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
            ApplyTexture(cardBackMaterial, CreateQuestFallbackCardBackTexture());
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
            public GameObject Portrait;
            public GameObject PowerLabelRoot;
            public MeshRenderer FrontRenderer;
            public MeshRenderer PortraitRenderer;
            public TextMeshPro PowerLabelText;
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
                Material highlightMaterial)
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
                var highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
                highlight.name = "QuestCardProxyHighlight";
                SetQuestOverlayLayer(highlight);
                UnityEngine.Object.Destroy(highlight.GetComponent<Collider>());
                highlight.transform.SetParent(root.transform, false);
                highlight.transform.localPosition = new Vector3(0f, CardThickness + 0.018f, 0f);
                highlight.transform.localScale = new Vector3(CardWidth + 0.26f, 0.012f, CardHeight + 0.26f);
                ConfigureRenderer(highlight.GetComponent<MeshRenderer>(), highlightMaterial);
                highlight.SetActive(false);

                return new QuestCardProxy
                {
                    Root = root,
                    Transform = root.transform,
                    Front = front,
                    Back = back,
                    Highlight = highlight,
                    Portrait = portrait,
                    PowerLabelRoot = powerLabel,
                    FrontRenderer = front.GetComponent<MeshRenderer>(),
                    PortraitRenderer = portrait.GetComponent<MeshRenderer>(),
                    PowerLabelText = powerLabel.GetComponentInChildren<TextMeshPro>(true),
                    Hit = hit,
                    Card = card
                };
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
                text.fontSize = 13.5f;
                text.fontStyle = FontStyles.Bold;
                text.richText = true;
                text.enableWordWrapping = false;
                text.text = string.Empty;
                text.color = Color.white;
                text.outlineWidth = 0.22f;
                text.outlineColor = new Color(0f, 0f, 0f, 0.92f);
                text.margin = new Vector4(0.4f, 0.2f, 0.4f, 0.2f);

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

        private sealed class QuestPileProxy
        {
            public GameObject Root;
            public Transform Transform;
            public TextMeshPro Label;
            public QuestPileProxyHit Hit;

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

                for (var index = 0; index < 5; index += 1)
                    CreatePileLayer(root.transform, index, sideMaterial, backMaterial);

                var labelObject = new GameObject("QuestPileProxyLabel");
                SetQuestOverlayLayer(labelObject);
                labelObject.transform.SetParent(root.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 0.92f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.76f;
                var label = labelObject.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 2.2f;
                label.color = Color.white;
                label.enableWordWrapping = false;
                label.text = string.Empty;

                return new QuestPileProxy
                {
                    Root = root,
                    Transform = root.transform,
                    Label = label,
                    Hit = hit
                };
            }

            private static void CreatePileLayer(Transform parent, int index, Material sideMaterial, Material backMaterial)
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
