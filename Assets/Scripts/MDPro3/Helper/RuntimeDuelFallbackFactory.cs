using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using YgomGame.Duel;
using YgomSystem.ElementSystem;

namespace MDPro3
{
    public static class RuntimeDuelFallbackFactory
    {
        static Material lineMaterial;
        static Material deckMaterial;
        static Material deckBackMaterial;
        static Material effectMaterialTemplate;
        static Texture2D preferredDeckBackTexture;
        const string FallbackDeckName = "FallbackDuelDeckAppearance";
        const string PreferredCardBackRelativePath = "texture/duel/opponent.jpg";

        public static GameObject CreateAttackLine()
        {
            var root = new GameObject("FallbackAttackLine");
            var elements = new List<ElementObject>();
            CreateLine(root.transform, "arrowlimeRollover", new Color(0.2f, 0.85f, 1f, 0.95f), elements);
            CreateLine(root.transform, "arrowRollover", new Color(1f, 1f, 1f, 0.9f), elements);
            var manager = root.AddComponent<ElementObjectManager>();
            manager.serializedElements = elements.ToArray();
            root.SetActive(false);
            return root;
        }

        public static GameObject CreateSimpleLine(string name, Color color)
        {
            var root = new GameObject(name);
            CreateLine(root.transform, "Line", color, null);
            root.SetActive(false);
            return root;
        }

        public static GameObject CreateDice(string name)
        {
            var root = new GameObject(name + "Root");
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = name;
            Object.Destroy(child.GetComponent<Collider>());
            child.transform.SetParent(root.transform, false);
            child.transform.localScale = Vector3.one * 0.5f;
            child.AddComponent<PlayableDirector>();
            return root;
        }

        public static GameObject CreateDeckAppearance()
        {
            var root = new GameObject(FallbackDeckName);
            var rootElements = new List<ElementObject>();

            var shuffleTop = new GameObject("CardShuffleTop");
            shuffleTop.transform.SetParent(root.transform, false);
            shuffleTop.AddComponent<Animator>();
            rootElements.Add(AddElement(shuffleTop, "CardShuffleTop"));

            var cardElements = new List<ElementObject>();
            for (var i = 1; i <= 4; i++)
            {
                CreateDeckLayer(shuffleTop.transform, i, cardElements);
            }

            var childManager = shuffleTop.AddComponent<ElementObjectManager>();
            childManager.serializedElements = cardElements.ToArray();

            var rootManager = root.AddComponent<ElementObjectManager>();
            rootManager.serializedElements = rootElements.ToArray();
            return root;
        }

        public static GameObject CreateChainSpot()
        {
            var root = new GameObject("FallbackChainSpot");
            root.AddComponent<PlayableDirector>();

            var elements = new List<ElementObject>();
            var wrap = CreateSpriteObject(root.transform, "ChainWrapSet", Vector3.zero, 2.2f, GetChainWrapSprite(), new Color(0.1f, 0.55f, 1f, 0.62f));
            elements.Add(AddElement(wrap, "ChainWrapSet"));

            var oneDigit = CreateSpriteObject(root.transform, "DummyNum01", new Vector3(0f, 0.02f, -0.02f), 0.75f, GetChainNumberSprite(1), Color.white);
            elements.Add(AddElement(oneDigit, "DummyNum01"));

            var tens = CreateSpriteObject(root.transform, "DummyNum02_01", new Vector3(-0.28f, 0.02f, -0.02f), 0.62f, GetChainNumberSprite(1), Color.white);
            elements.Add(AddElement(tens, "DummyNum02_01"));

            var ones = CreateSpriteObject(root.transform, "DummyNum02_02", new Vector3(0.28f, 0.02f, -0.02f), 0.62f, GetChainNumberSprite(2), Color.white);
            elements.Add(AddElement(ones, "DummyNum02_02"));

            tens.SetActive(false);
            ones.SetActive(false);

            var manager = root.AddComponent<ElementObjectManager>();
            manager.serializedElements = elements.ToArray();
            root.AddComponent<DuelChainSpot>();
            return root;
        }

        public static GameObject CreateSummonSynchroTimeline(string sourcePath)
        {
            var root = new GameObject("Fallback" + PathSafeName(sourcePath));
            root.AddComponent<PlayableDirector>();

            var rootElements = new List<ElementObject>();

            var anchor = new GameObject("FallbackSynchroAnchor");
            anchor.transform.SetParent(root.transform, false);
            var autoScaleTarget = new GameObject("FallbackSynchroAutoScaleTarget");
            autoScaleTarget.transform.SetParent(anchor.transform, false);

            var postSynchro = new GameObject("SummonSynchroPostSynchro");
            postSynchro.transform.SetParent(root.transform, false);
            rootElements.Add(AddElement(postSynchro, "SummonSynchroPostSynchro"));

            var postElements = new List<ElementObject>();
            var dummy = new GameObject("DummyCardSynchro");
            dummy.transform.SetParent(postSynchro.transform, false);
            postElements.Add(AddElement(dummy, "DummyCardSynchro"));

            var dummyElements = new List<ElementObject>();
            CreateDummyCardRenderer(dummy.transform, "DummyCardModel_side", new Vector3(0f, 0f, 0f), Quaternion.identity, dummyElements);
            CreateDummyCardRenderer(dummy.transform, "DummyCardModel_back", new Vector3(0f, -0.011f, 0f), Quaternion.Euler(90f, 0f, 180f), dummyElements);
            CreateDummyCardRenderer(dummy.transform, "DummyCardModel_front", new Vector3(0f, 0.011f, 0f), Quaternion.Euler(90f, 0f, 0f), dummyElements);
            var dummyManager = dummy.AddComponent<ElementObjectManager>();
            dummyManager.serializedElements = dummyElements.ToArray();

            var addRenderer = CreateDummyCardRenderer(postSynchro.transform, "DummyCardLinkAdd", new Vector3(0f, 0.015f, 0.35f), Quaternion.Euler(90f, 0f, 0f), postElements);
            addRenderer.transform.localScale = new Vector3(2.55f, 3.6f, 1f);

            var postManager = postSynchro.AddComponent<ElementObjectManager>();
            postManager.serializedElements = postElements.ToArray();

            for (var i = 1; i < 12; i++)
            {
                var suffix = i > 9 ? i.ToString() : "0" + i.ToString();
                rootElements.Add(AddElement(CreateMarker(root.transform, "NumberNonTuner" + suffix), "NumberNonTuner" + suffix));
                rootElements.Add(AddElement(CreateMarker(root.transform, "SynchroStarLevel" + suffix), "SynchroStarLevel" + suffix));
                rootElements.Add(AddElement(CreateMarker(root.transform, "NumberTuner" + suffix), "NumberTuner" + suffix));
            }

            rootElements.Add(AddElement(CreateMarker(root.transform, "SynchroCircle01"), "SynchroCircle01"));
            rootElements.Add(AddElement(CreateMarker(root.transform, "SynchroCircle02"), "SynchroCircle02"));
            rootElements.Add(AddElement(CreateMarker(root.transform, "SynchroCircle03"), "SynchroCircle03"));

            var rootManager = root.AddComponent<ElementObjectManager>();
            rootManager.serializedElements = rootElements.ToArray();
            return root;
        }

        public static GameObject CreateDuelBackground()
        {
            var root = new GameObject("FallbackDuelBackground");
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "FallbackBackdrop";
            Object.Destroy(backdrop.GetComponent<Collider>());
            backdrop.transform.SetParent(root.transform, false);
            backdrop.transform.localPosition = new Vector3(0f, 18f, 52f);
            backdrop.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            backdrop.transform.localScale = new Vector3(120f, 54f, 1f);
            backdrop.GetComponent<Renderer>().material = GetDeckMaterial();
            return root;
        }

        public static GameObject CreateDuelText(string sourcePath)
        {
            var root = new GameObject("Fallback" + PathSafeName(sourcePath));
            root.AddComponent<PlayableDirector>();
            return root;
        }

        public static GameObject CreateSimpleEffect(string sourcePath)
        {
            if (!ShouldShowFallbackEffect(sourcePath))
                return new GameObject("Fallback" + PathSafeName(sourcePath));

            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Fallback" + PathSafeName(sourcePath);
            Object.Destroy(root.GetComponent<Collider>());
            root.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            root.transform.localScale = GetEffectScale(sourcePath);

            var renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateEffectMaterial(sourcePath);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return root;
        }

        public static GameObject CreateSummonLandingHandEffect()
        {
            var root = new GameObject("FallbackSummonLandingHand");
            CreateSummonLandingBranch(root.transform, "AttackLanding", new Color(1f, 0.72f, 0.18f, 0.42f));
            CreateSummonLandingBranch(root.transform, "DefenseLanding", new Color(0.28f, 0.72f, 1f, 0.34f));
            return root;
        }

        public static GameObject CreateDummyCardTimeline(string sourcePath)
        {
            var root = new GameObject("Fallback" + PathSafeName(sourcePath));
            root.AddComponent<PlayableDirector>();

            var rootElements = new List<ElementObject>();
            var dummy = new GameObject("DummyCard01");
            dummy.transform.SetParent(root.transform, false);
            dummy.transform.localScale = Vector3.one;
            rootElements.Add(AddElement(dummy, "DummyCard01"));

            var dummyElements = new List<ElementObject>();
            CreateDummyCardRenderer(dummy.transform, "DummyCardModel_side", new Vector3(0f, 0f, 0f), Quaternion.identity, dummyElements);
            CreateDummyCardRenderer(dummy.transform, "DummyCardModel_back", new Vector3(0f, -0.011f, 0f), Quaternion.Euler(90f, 0f, 180f), dummyElements);
            CreateDummyCardRenderer(dummy.transform, "DummyCardModel_front", new Vector3(0f, 0.011f, 0f), Quaternion.Euler(90f, 0f, 0f), dummyElements);

            var dummyManager = dummy.AddComponent<ElementObjectManager>();
            dummyManager.serializedElements = dummyElements.ToArray();

            var rootManager = root.AddComponent<ElementObjectManager>();
            rootManager.serializedElements = rootElements.ToArray();
            return root;
        }

        static void CreateSummonLandingBranch(Transform parent, string name, Color color)
        {
            var branch = new GameObject(name);
            branch.transform.SetParent(parent, false);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = name + "Pulse";
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(branch.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            ring.transform.localScale = new Vector3(2.8f, 0.018f, 2.8f);

            var renderer = ring.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateTransparentColorMaterial("FallbackSummonLandingMat", color);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        static LineRenderer CreateLine(Transform parent, string label, Color color, List<ElementObject> elements)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 9;
            line.useWorldSpace = true;
            line.startWidth = 0.12f;
            line.endWidth = 0.12f;
            line.numCapVertices = 4;
            line.material = GetLineMaterial(color);
            if (elements != null)
                elements.Add(AddElement(go, label));
            return line;
        }

        static Renderer CreateDummyCardRenderer(
            Transform parent,
            string label,
            Vector3 localPosition,
            Quaternion localRotation,
            List<ElementObject> elements)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = label;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = localRotation;
            quad.transform.localScale = new Vector3(2.4f, 3.4f, 1f);
            var renderer = quad.GetComponent<Renderer>();
            renderer.material = GetDeckBackMaterial();
            elements.Add(AddElement(quad, label));
            return renderer;
        }

        static void CreateDeckLayer(Transform parent, int index, List<ElementObject> elements)
        {
            var suffix = index.ToString("00");
            var layer = new GameObject("FallbackDeckCard" + suffix);
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = new Vector3(0f, (index - 1) * 0.035f, 0f);

            var side = GameObject.CreatePrimitive(PrimitiveType.Cube);
            side.name = "CardModel" + suffix + "_side";
            Object.Destroy(side.GetComponent<Collider>());
            side.transform.SetParent(layer.transform, false);
            side.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            side.transform.localRotation = Quaternion.identity;
            side.transform.localScale = new Vector3(2.45f, 0.024f, 3.45f);
            var sideRenderer = side.GetComponent<MeshRenderer>();
            if (sideRenderer != null)
            {
                sideRenderer.sharedMaterial = GetDeckMaterial();
                sideRenderer.shadowCastingMode = ShadowCastingMode.Off;
                sideRenderer.receiveShadows = false;
            }
            elements.Add(AddElement(side, side.name));

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "CardModel" + suffix + "_back";
            Object.Destroy(back.GetComponent<Collider>());
            back.transform.SetParent(layer.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            back.transform.localRotation = Quaternion.Euler(90f, 0f, 180f);
            back.transform.localScale = new Vector3(2.4f, 3.4f, 1f);
            var backRenderer = back.GetComponent<MeshRenderer>();
            if (backRenderer != null)
            {
                backRenderer.sharedMaterial = GetDeckBackMaterial();
                backRenderer.shadowCastingMode = ShadowCastingMode.Off;
                backRenderer.receiveShadows = false;
            }
            elements.Add(AddElement(back, back.name));
        }

        public static bool TrySetFallbackDeckCount(ElementObjectManager manager, int count)
        {
            if (!IsFallbackDeck(manager))
                return false;

            var shuffleTop = manager.GetElement<Transform>("CardShuffleTop");
            if (shuffleTop == null)
                return false;

            if (count <= 0)
            {
                shuffleTop.localScale = Vector3.zero;
                return true;
            }

            shuffleTop.localScale = Vector3.one;
            return true;
        }

        public static bool IsFallbackDeck(ElementObjectManager manager)
        {
            if (manager == null)
                return false;

            var rootName = manager.gameObject.name;
            return !string.IsNullOrEmpty(rootName) && rootName.StartsWith(FallbackDeckName);
        }

        static GameObject CreateMarker(Transform parent, string name)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            return marker;
        }

        static GameObject CreateSpriteObject(Transform parent, string name, Vector3 localPosition, float scale, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = Vector3.one * scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = "ChainSpot";
            renderer.sortingOrder = name == "ChainWrapSet" ? 0 : 1;
            return go;
        }

        static Sprite GetChainWrapSprite()
        {
            return TextureManager.container != null ? TextureManager.container.chainNumSet0 : null;
        }

        static Sprite GetChainNumberSprite(int number)
        {
            if (TextureManager.container == null)
                return null;

            switch (number)
            {
                case 0: return TextureManager.container.chainCircleNum0;
                case 1: return TextureManager.container.chainCircleNum1;
                case 2: return TextureManager.container.chainCircleNum2;
                case 3: return TextureManager.container.chainCircleNum3;
                case 4: return TextureManager.container.chainCircleNum4;
                case 5: return TextureManager.container.chainCircleNum5;
                case 6: return TextureManager.container.chainCircleNum6;
                case 7: return TextureManager.container.chainCircleNum7;
                case 8: return TextureManager.container.chainCircleNum8;
                case 9: return TextureManager.container.chainCircleNum9;
                default: return TextureManager.container.typeNone;
            }
        }

        static Vector3 GetEffectScale(string sourcePath)
        {
            var normalized = (sourcePath ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
            if (normalized.Contains("fxp_hl_set") || normalized.Contains("fxp_hl_spsom"))
                return new Vector3(2.7f, 0.025f, 3.8f);
            if (normalized.Contains("trpmgc"))
                return new Vector3(8f, 0.035f, 7f);
            if (normalized.Contains("mst"))
                return new Vector3(8f, 0.035f, 8f);
            if (normalized.Contains("card"))
                return new Vector3(2.6f, 0.035f, 3.6f);
            return new Vector3(8f, 0.035f, 8f);
        }

        static bool ShouldShowFallbackEffect(string sourcePath)
        {
            var normalized = (sourcePath ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
            return normalized.Contains("eff_highlight/eff_duel_highlight")
                || normalized.Contains("hitghlight/fxp_hl_select/")
                || normalized.Contains("hitghlight/fxp_hl_set")
                || normalized.Contains("hitghlight/fxp_hl_spsom");
        }

        static Material CreateEffectMaterial(string sourcePath)
        {
            var normalized = (sourcePath ?? string.Empty).ToLowerInvariant();
            var color = normalized.Contains("push")
                ? new Color(0.25f, 1f, 0.45f, 0.46f)
                : normalized.Contains("highlight")
                    ? new Color(0.22f, 0.95f, 1f, 0.34f)
                    : new Color(1f, 0.9f, 0.14f, 0.44f);

            if (effectMaterialTemplate == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Standard");
                effectMaterialTemplate = new Material(shader);
                effectMaterialTemplate.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            var material = new Material(effectMaterialTemplate) { name = "FallbackEffectMaterial" };
            SetMaterialColor(material, color);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            return material;
        }

        static Material CreateTransparentColorMaterial(string name, Color color)
        {
            var material = CreateEffectMaterial(name);
            material.name = name;
            SetMaterialColor(material, color);
            return material;
        }

        static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
                return;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        static ElementObject AddElement(GameObject go, string label)
        {
            var element = go.AddComponent<ElementObject>();
            element.label = label;
            return element;
        }

        static string PathSafeName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "DuelText";

            var lastSlash = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            return lastSlash >= 0 && lastSlash + 1 < path.Length
                ? path.Substring(lastSlash + 1)
                : path;
        }

        static Material GetLineMaterial(Color color)
        {
            if (lineMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Unlit/Color");
                lineMaterial = new Material(shader);
                lineMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }

            var material = new Material(lineMaterial);
            material.color = color;
            return material;
        }

        static Material GetDeckMaterial()
        {
            if (deckMaterial != null)
                return deckMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            deckMaterial = new Material(shader);
            SetMaterialColor(deckMaterial, new Color(0.06f, 0.08f, 0.11f, 1f));
            deckMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return deckMaterial;
        }

        static Material GetDeckBackMaterial()
        {
            if (deckBackMaterial != null)
                return deckBackMaterial;

            if (TextureManager.cardMatNormal != null)
                deckBackMaterial = new Material(TextureManager.cardMatNormal);
            else
                deckBackMaterial = new Material(GetDeckMaterial());

            SetMaterialColor(deckBackMaterial, new Color(0.08f, 0.15f, 0.28f, 1f));
            var preferredTexture = LoadPreferredDeckBackTexture();
            if (preferredTexture != null)
            {
                deckBackMaterial.mainTexture = preferredTexture;
                if (deckBackMaterial.HasProperty("_BaseMap"))
                    deckBackMaterial.SetTexture("_BaseMap", preferredTexture);
                if (deckBackMaterial.HasProperty("_MainTex"))
                    deckBackMaterial.SetTexture("_MainTex", preferredTexture);
            }
            else
            {
                var sprite = TextureManager.container != null ? TextureManager.container.cardBackDefault : null;
                if (sprite != null && sprite.texture != null)
                {
                    deckBackMaterial.mainTexture = sprite.texture;
                    if (deckBackMaterial.HasProperty("_BaseMap"))
                        deckBackMaterial.SetTexture("_BaseMap", sprite.texture);
                    if (deckBackMaterial.HasProperty("_MainTex"))
                        deckBackMaterial.SetTexture("_MainTex", sprite.texture);
                }
            }
            if (deckBackMaterial.HasProperty("_Surface"))
                deckBackMaterial.SetFloat("_Surface", 0f);
            if (deckBackMaterial.HasProperty("_ZWrite"))
                deckBackMaterial.SetFloat("_ZWrite", 1f);
            if (deckBackMaterial.HasProperty("_Cull"))
                deckBackMaterial.SetFloat("_Cull", (float)CullMode.Off);
            deckBackMaterial.renderQueue = 3000;
            deckBackMaterial.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            deckBackMaterial.DisableKeyword("_ALPHABLEND_ON");
            deckBackMaterial.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return deckBackMaterial;
        }

        static Texture2D LoadPreferredDeckBackTexture()
        {
            if (preferredDeckBackTexture != null)
                return preferredDeckBackTexture;

            foreach (var path in GetPreferredDeckBackPaths())
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                try
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        name = "QuestFallbackYGOPro2CardBack",
                        filterMode = FilterMode.Trilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 8
                    };
                    if (texture.LoadImage(File.ReadAllBytes(path), false))
                    {
                        preferredDeckBackTexture = texture;
                        Debug.Log("Runtime duel fallback card back loaded from: " + path);
                        return preferredDeckBackTexture;
                    }

                    Object.Destroy(texture);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Runtime duel fallback card back load failed: " + path + " / " + ex.Message);
                }
            }

            return null;
        }

        static IEnumerable<string> GetPreferredDeckBackPaths()
        {
            yield return Path.Combine(Program.expansionsPath, PreferredCardBackRelativePath);
            yield return Path.Combine("Expansions", PreferredCardBackRelativePath);
        }
    }
}
