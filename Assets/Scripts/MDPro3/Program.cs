using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MDPro3.YGOSharp;
using UnityEngine.EventSystems;
using UnityEngine.AddressableAssets;
using MDPro3.Net;
using MDPro3.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MDPro3
{
    public class Program : MonoBehaviour
    {
        public bool controlBool;
        public bool controlBool2;

        [Header("Public References")]
        public Transform container_3D;
        public Transform container_2D;
        public CardRenderer cardRenderer;

        [Header("Manager")]
        public CameraManager camera_;
        public UIManager ui_;
        public BackgroundManager background_;
        public AudioManager audio_;
        public TextureManager texture_;
        public MessageManager message_;
        public TimelineManager timeline_;
        public NewsManager news_;

        [Header("Servants")]
        public Menu menu;
        public Solo solo;
        public Online online;
        public SelectPuzzle puzzle;
        public SelectReplay replay;
        public MonsterCutin cutin;
        public MateView mate;
        public SelectDeck selectDeck;
        public Setting setting;
        public Appearance appearance;
        public SelectCharacter character;
        public OcgCore ocgcore;
        public Room room;
        public EditDeck editDeck;
        public DeckEditor deckEditor;
        public OnlineDeckViewer onlineDeckViewer;

        [Header("SidePanels")]

        [HideInInspector]
        public Servant currentServant;
        [HideInInspector]
        public Servant currentSubServant;
        [HideInInspector]
        public int depth;

        #region Const
        public static bool Running = true;
        public const string artPath = "Picture/Art/";
        public const string altArtPath = "Picture/Art2/";
        public const string cardPicPath = "Picture/CardGenerated/";
        public const string closeupPath = "Picture/Closeup/";
        public const string dataPath = "Data/";
        public const string localesPath = "Data/locales/";
        public const string configPath = "Data/config.conf";
        public const string lflistPath = "Data/lflist.conf";
        public const string deckPath = "Deck/";
        public const string expansionsPath = "Expansions/";
        public const string puzzlePath = "Puzzle/";
        public const string replayPath = "Replay/";
        public const string diyPath = "Picture/DIY/";
        public const string slash = "/";
        public const string ydkExpansion = ".ydk";
        public const string pngExpansion = ".png";
        public const string jpgExpansion = ".jpg";
        public const string yrpExpansion = ".yrp";
        public const string yrp3dExpansion = ".yrp3d";
        private const string QuestDebugAutoDeckName = "QuestDebugAuto";
        private static readonly int[] QuestDebugAutoOpeningHandCodes =
        {
            7084129,
            43722862,
            89739383,
            47222536,
            48680970
        };
        #endregion

        #region Initializement

        public static Program instance;
        public static Items items;

        List<Manager> managers = new List<Manager>();
        List<Servant> servants = new List<Servant>();

        void Initialize()
        {
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            Config.Initialize(configPath);

            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (items != null)
                InitializeRest();
            else
            {
                items = Resources.Load<Items>("Items");
                if (items != null)
                {
                    InitializeRest();
                    return;
                }

                var handle = Addressables.LoadAssetAsync<Items>("Items");
                handle.Completed += (result) =>
                {
                    items = result.Result;
                    if (items == null)
                        items = Resources.Load<Items>("Items");
                    InitializeRest();
                };
            }
        }

        void InitializeRest()
        {
            ZipHelper.Initialize();
            items.Initialize();
            BanlistManager.Initialize();
            InitializeAllManagers();
            InitializeAllServants();
            QuestRuntimeDebugSettings.Initialize();
            ReadParams();
            StartCoroutine(ResourceSmokeTestAsync());
#if !UNITY_EDITOR && UNITY_ANDROID
            if (QuestRuntimeDebugSettings.AutoEnterSolo)
                StartCoroutine(QuestDebugAutoSoloAsync());
#endif

            //VoiceHelper.ExportAllCardsNotFound();
        }

        public static bool exitOnReturn = false;
        void ReadParams()
        {
            var args = Environment.GetCommandLineArgs();
            //args = new string[2]
            //{
            //    //"-r",
            //    //"TURN023"

            //    //"-s",
            //    //"6ace for win!"

            //    "-d",
            //    "LLÌúÊÞ",

            //    //"-n",
            //    //"³à×ÓÄÎÂä",

            //    //"-h",
            //    //"mygo.superpre.pro",
            //    //"-p",
            //    //"888",
            //    //"-w",
            //    //"M#1008611",
            //    //"-j"
            //};

            string nick = null;
            string host = null;
            string port = null;
            string password = null;
            string deck = null;
            string replay = null;
            string puzzle = null;
            var join = false;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-n" && args.Length > i + 1)
                {
                    nick = args[++i];
                    Config.Set("DuelPlayerName0", nick);
                    Config.Save();
                }

                if (args[i].ToLower() == "-h" && args.Length > i + 1) 
                    host = args[++i];
                if (args[i].ToLower() == "-p" && args.Length > i + 1) 
                    port = args[++i];
                if (args[i].ToLower() == "-w" && args.Length > i + 1)
                    password = args[++i];
                if (args[i].ToLower() == "-d" && args.Length > i + 1)
                    deck = args[++i];
                if (args[i].ToLower() == "-r" && args.Length > i + 1)
                    replay = args[++i];
                if (args[i].ToLower() == "-s" && args.Length > i + 1)
                    puzzle = args[++i];

                if (args[i].ToLower() == "-j")
                {
                    join = true;
                    Config.Set("DeckInUse", deck);
                    Config.Save();
                }
                if (args[i].ToLower() == "--codex-auto-solo")
                    Debug.LogWarning("Ignoring deprecated --codex-auto-solo. Use the Solo menu manually.");
            }


            if (join)
            {
                online.KF_OnlineGame(nick, host, port, password);
                exitOnReturn = true;
            }
            else if (deck != null)
            {
                Config.Set("DeckInUse", deck);
                editDeck.SwitchCondition(EditDeck.Condition.EditDeck);
                ShiftToServant(editDeck);
                exitOnReturn = true;
            }
            else if (replay != null)
            {
                this.replay.KF_Replay(replay);
                exitOnReturn = true;
            }
            else if (puzzle != null)
            {
                this.puzzle.StartPuzzle(puzzlePath + puzzle);
                exitOnReturn = true;
            }
        }

        IEnumerator ResourceSmokeTestAsync()
        {
            yield return null;

            const int testCode = 89631139;
            var language = MDPro3.Utility.Language.GetConfig();
            var cardDatabasePath = localesPath + language + "/cards.cdb";
            if (!File.Exists(cardDatabasePath))
                cardDatabasePath = localesPath + "zh-CN/cards.cdb";
            var stringsPath = localesPath + language + "/strings.conf";
            if (!File.Exists(stringsPath))
                stringsPath = localesPath + "zh-CN/strings.conf";
            var artPath = Program.artPath + testCode + jpgExpansion;

            var card = CardsManager.Get(testCode, true);
            Debug.LogFormat(
                "MDPro3 resource check: cwd='{0}', persistent='{1}', db='{2}' exists={3}, strings='{4}' exists={5}, art='{6}' exists={7}, cards={8}, card[{9}] id={10} name='{11}'",
                Environment.CurrentDirectory,
                Application.persistentDataPath,
                cardDatabasePath,
                File.Exists(cardDatabasePath),
                stringsPath,
                File.Exists(stringsPath),
                artPath,
                File.Exists(artPath),
                CardsManager._cards.Count,
                testCode,
                card.Id,
                card.Name);

            while (TextureManager.container == null)
                yield return null;

            var artTask = TextureManager.LoadArtAsync(testCode, true);
            while (!artTask.IsCompleted)
                yield return null;
            var art = artTask.Result;
            Debug.LogFormat(
                "MDPro3 resource check: art[{0}] loaded={1} size={2}x{3} foundArt={4}",
                testCode,
                art != null,
                art == null ? 0 : art.width,
                art == null ? 0 : art.height,
                TextureManager.lastCardFoundArt);

            var cardTask = TextureManager.LoadCardAsync(testCode, true);
            while (!cardTask.IsCompleted)
                yield return null;
            var cardTexture = cardTask.Result;
            Debug.LogFormat(
                "MDPro3 resource check: renderedCard[{0}] loaded={1} size={2}x{3} renderOk={4}",
                testCode,
                cardTexture != null,
                cardTexture == null ? 0 : cardTexture.width,
                cardTexture == null ? 0 : cardTexture.height,
                TextureManager.lastCardRenderSucceed);
        }
        IEnumerator QuestDebugAutoSoloAsync()
        {
            Debug.Log("Quest debug auto solo requested.");
            var readyDeadline = Time.realtimeSinceStartup + 45f;
            while (Time.realtimeSinceStartup < readyDeadline)
            {
                if (exitOnReturn)
                {
                    Debug.LogWarning("Quest debug auto solo skipped: Program.exitOnReturn is active.");
                    yield break;
                }

                if (solo != null && menu != null && TextureManager.container != null)
                    break;

                yield return null;
            }

            if (solo == null)
            {
                Debug.LogWarning("Quest debug auto solo skipped: Solo servant is missing.");
                yield break;
            }

            solo.SwitchCondition(Solo.Condition.ForSolo);
            if (currentServant != solo)
                ShiftToServant(solo);

            var soloDeadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < soloDeadline && solo.lastSoloItem == null)
                yield return null;

            if (solo.lastSoloItem == null)
            {
                Debug.LogWarning("Quest debug auto solo skipped: AI list did not select a default item.");
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.75f);
            if (currentServant != solo)
                ShiftToServant(solo);

            var previousDeckName = ApplyQuestDebugAutoSoloSetup();
            Debug.LogFormat("Quest debug auto solo launching AI index={0}.", solo.lastSoloItem.index);
            solo.OnPlay();
            StartCoroutine(QuestDebugAutoAdvanceRoomAsync(previousDeckName));
        }

        private string ApplyQuestDebugAutoSoloSetup()
        {
            var previousDeckName = Config.Get("DeckInUse", string.Empty);
            var deckReady = EnsureQuestDebugAutoDeck();
            if (deckReady)
                Config.Set("DeckInUse", QuestDebugAutoDeckName);

            if (solo.toggleLockHand != null)
                solo.toggleLockHand.SetToggleOn(true);
            if (solo.toggleNoCheck != null)
                solo.toggleNoCheck.SetToggleOn(false);
            if (solo.toggleNoShuffle != null)
                solo.toggleNoShuffle.SetToggleOn(true);
            if (solo.inputLP != null)
                solo.inputLP.text = "8000";
            if (solo.inputHand != null)
                solo.inputHand.text = "5";
            if (solo.inputDraw != null)
                solo.inputDraw.text = "1";

            Debug.LogFormat(
                "Quest debug auto solo setup: deck={0}, previousDeck={1}, deckReady={2}, lockHand={3}, noCheck={4}, noShuffle={5}, hand={6}, draw={7}.",
                QuestDebugAutoDeckName,
                string.IsNullOrEmpty(previousDeckName) ? "<empty>" : previousDeckName,
                deckReady,
                solo.toggleLockHand != null && solo.toggleLockHand.isOn,
                solo.toggleNoCheck != null && solo.toggleNoCheck.isOn,
                solo.toggleNoShuffle != null && solo.toggleNoShuffle.isOn,
                solo.inputHand == null ? "<missing>" : solo.inputHand.text,
                solo.inputDraw == null ? "<missing>" : solo.inputDraw.text);
            Debug.LogFormat(
                "Quest debug auto fixed opening hand: noShuffle={0}, expected=[{1}]",
                solo.toggleNoShuffle != null && solo.toggleNoShuffle.isOn,
                BuildQuestDebugOpeningHandLog());
            return previousDeckName;
        }

        private IEnumerator QuestDebugAutoAdvanceRoomAsync(string previousDeckName)
        {
            var deadline = Time.realtimeSinceStartup + 60f;
            var readySent = false;
            var startSent = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (ocgcore != null && currentServant == ocgcore)
                {
                    RestoreQuestDebugPreviousDeck(previousDeckName, "duel-started");
                    yield break;
                }

                if (room != null && Room.fromSolo && Room.players != null)
                {
                    var selfIndex = Room.selfType;
                    var selfReady = selfIndex >= 0
                        && selfIndex < Room.players.Length
                        && Room.players[selfIndex] != null
                        && Room.players[selfIndex].ready;

                    if (!readySent && !selfReady && selfIndex >= 0 && selfIndex < Room.players.Length && Room.players[selfIndex] != null)
                    {
                        room.OnReady();
                        readySent = true;
                        Debug.LogFormat("Quest debug auto room ready sent. self={0}, host={1}", selfIndex, Room.isHost);
                    }
                    else if (selfReady)
                    {
                        readySent = true;
                    }

                    var opponentIndex = selfIndex == 0 ? 1 : 0;
                    var opponentReady = opponentIndex >= 0
                        && opponentIndex < Room.players.Length
                        && Room.players[opponentIndex] != null
                        && Room.players[opponentIndex].ready;
                    if (Room.isHost && readySent && opponentReady && !startSent)
                    {
                        room.OnStart();
                        startSent = true;
                        Debug.LogFormat("Quest debug auto room start sent. self={0}, opponent={1}", selfIndex, opponentIndex);
                    }
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }

            RestoreQuestDebugPreviousDeck(previousDeckName, "timeout");
            Debug.LogWarning("Quest debug auto room advance timed out before duel start.");
        }

        private static void RestoreQuestDebugPreviousDeck(string previousDeckName, string reason)
        {
            Config.Set("DeckInUse", previousDeckName ?? string.Empty);
            Debug.LogFormat(
                "Quest debug auto deck restored. reason={0}, deck={1}",
                reason,
                string.IsNullOrEmpty(previousDeckName) ? "<empty>" : previousDeckName);
        }

        private static bool EnsureQuestDebugAutoDeck()
        {
            try
            {
                Directory.CreateDirectory(deckPath);
                var path = deckPath + QuestDebugAutoDeckName + ydkExpansion;
                var content = BuildQuestDebugAutoDeckYdk();
                if (!File.Exists(path) || File.ReadAllText(path) != content)
                    File.WriteAllText(path, content);

                Debug.LogFormat("Quest debug auto deck ready: {0}", Path.GetFullPath(path));
                return File.Exists(path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Quest debug auto deck setup failed: " + ex.Message);
                return false;
            }
        }

        private static string BuildQuestDebugOpeningHandLog()
        {
            var parts = new List<string>();
            foreach (var code in QuestDebugAutoOpeningHandCodes)
            {
                var card = CardsManager.Get(code, true);
                var name = card == null || string.IsNullOrWhiteSpace(card.Name) ? code.ToString() : card.Name;
                parts.Add(code + ":" + name);
            }
            return string.Join(", ", parts);
        }

        private static string BuildQuestDebugAutoDeckYdk()
        {
            return string.Join(
                Environment.NewLine,
                new[]
                {
                    "#created by MDPro3 Quest debug automation",
                    "#main",
                    "7084129",
                    "43722862",
                    "89739383",
                    "47222536",
                    "48680970",
                    "46986414",
                    "46986414",
                    "46986414",
                    "30603688",
                    "30603688",
                    "71007216",
                    "71007216",
                    "70117860",
                    "70117860",
                    "14824019",
                    "14824019",
                    "14824019",
                    "23434538",
                    "23434538",
                    "23434538",
                    "14558127",
                    "14558127",
                    "1784686",
                    "2314238",
                    "23314220",
                    "23314220",
                    "70368879",
                    "70368879",
                    "70368879",
                    "41735184",
                    "41735184",
                    "73616671",
                    "73616671",
                    "67775894",
                    "67775894",
                    "7922915",
                    "7922915",
                    "40605147",
                    "40605147",
                    "83764718",
                    "#extra",
                    "41721210",
                    "41721210",
                    "50954680",
                    "50954680",
                    "58074177",
                    "90036274",
                    "14577226",
                    "14577226",
                    "16691074",
                    "22110647",
                    "80117527",
                    "71384012",
                    "71384012",
                    "71384012",
                    "1482001",
                    "!side",
                    "#pickup",
                    "#0",
                    "#0",
                    "#0",
                    "#case",
                    "#1080001",
                    "#protector",
                    "#1070001",
                    "#field",
                    "#1090001",
                    "#grave",
                    "#1100001",
                    "#stand",
                    "#1110001",
                    "#mate",
                    "#1000001"
                }) + Environment.NewLine;
        }
        public void InitializeForDataChange()
        {
            ZipHelper.Initialize();
            BanlistManager.Initialize();
            StringHelper.Initialize();
            CardsManager.Initialize();
        }

        private void InitializeAllManagers()
        {
            managers.Add(texture_);
            managers.Add(ui_);
            managers.Add(camera_);
            managers.Add(audio_);
            managers.Add(timeline_);
            managers.Add(background_);
            managers.Add(message_);

            foreach (Manager manager in managers)
                manager.Initialize();
        }
        private void InitializeAllServants()
        {
            servants.Add(setting);
            servants.Add(menu);
            servants.Add(solo);
            servants.Add(online);
            servants.Add(puzzle);
            servants.Add(replay);
            servants.Add(cutin);
            servants.Add(mate);
            servants.Add(selectDeck);
            servants.Add(appearance);
            servants.Add(character);
            servants.Add(ocgcore);
            servants.Add(room);
            servants.Add(editDeck);
            servants.Add(deckEditor);
            servants.Add(onlineDeckViewer);
            foreach (Servant servant in servants)
                servant.Initialize();
        }

        #endregion

        #region MonoBehaviors

        public const string tempFolder = "TempFolder/";
        public const string rootWindows64 = "StandaloneWindows64/";
        public const string rootAndroid = "Android/";
        public static string root = rootWindows64;

        void Awake()
        {
#if UNITY_ANDROID
            root = rootAndroid;
#endif
            instance = this;
            Initialize();
        }

        public float timeScale
        {
            get 
            { 
                return m_timeScale;
            }
            set 
            {
                m_timeScale = value;
                Time.timeScale = value;
            }
        }
        float m_timeScale = 1f;
#if UNITY_EDITOR
        public float timeScaleForEdit = 1;
#endif

        void Update()
        {
            TcpHelper.PerFrameFunction();
            foreach (Manager manager in managers) 
                manager.PerFrameFunction();
            foreach (Servant servant in servants) 
                servant.PerFrameFunction();

#if UNITY_EDITOR
            timeScale = timeScaleForEdit;
#endif
        }

        public void UnloadUnusedAssets()
        {
            if (gc == null)
            {
                gc = UnloadUnusedAssetsAsync();
                StartCoroutine(gc);
            }
        }
        IEnumerator gc;
        IEnumerator UnloadUnusedAssetsAsync()
        {
            var unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone)
                yield return null;
            gc = null;
        }

        public static bool noAccess = false;

        #endregion

        #region Tools
        public static int TimePassed()
        {
            return (int)(Time.time * 1000f);
        }

        public void ShiftToServant(Servant servant)
        {
            currentServant = servant;
            foreach (var ser in servants)
                if (ser != servant)
                    ser.Hide(servant.depth);
            foreach (var ser in servants)
                if (ser == servant)
                    ser.Show(depth);
            depth = servant.depth;
        }
        public void ShowSubServant(Servant servant)
        {
            if (currentSubServant == null)
            {
                servant.Show(0);
                currentSubServant = servant;
            }
            else
            {
                currentSubServant.Hide(servant.depth);
                servant.Show(currentSubServant.depth);
                currentSubServant = servant;
            }
        }

        public void ExitCurrentServant()
        {
            if (currentSubServant != null)
                currentSubServant.OnReturn();
            else
            {
                if(currentServant == null)
                {
                    foreach(var servant in  servants)
                        if (servant.showing)
                        {
                            currentServant = servant;
                            break;
                        }
                }
                if (currentServant == null)
                {
                    foreach (var servant in servants)
                        if (servant.showing)
                        {
                            currentServant = servant;
                            break;
                        }
                    if(currentServant == null)
                        currentServant = online;
                }
                currentServant.OnReturn();
            }
        }

        public void ExitDuel()
        {
            currentSubServant.OnReturn();
            currentServant.OnReturn();
        }

        #endregion

        #region System
        private void OnApplicationQuit()
        {
            Running = false;
            Config.Save();
            ClearCache();
            YgoServer.StopServer();
            ZipHelper.Dispose();
            try
            {
                TcpHelper.tcpClient.Close();
            }
            catch { }
            TcpHelper.tcpClient = null;
            MyCard.CloseAthleticWatchListWebSocket();
        }

        private void ClearCache()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject cacheDir = currentActivity.Call<AndroidJavaObject>("getCacheDir");
            string cachePath = cacheDir.Call<string>("getAbsolutePath");
            Tools.ClearDirectoryRecursively(new DirectoryInfo(cachePath));
#else
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
#endif
        }

        public static void GameQuit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        #endregion
    }
}
