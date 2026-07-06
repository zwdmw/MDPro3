using MDPro3.Net;
using MDPro3.UI;
using MDPro3.YGOSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using MDPro3.Utility;
using MDPro3.UI.PropertyOverrider;

namespace MDPro3
{
    public class Solo : Servant
    {
        public class BotInfo
        {
            public string name;
            public string command;
            public string desc;
            public string[] flags;
            public int main0;
        }
        public enum Condition
        {
            ForSolo,
            ForRoom
        }

        [Header("Solo")]
        public SelectionToggle toggleLockHand;
        public SelectionToggle toggleNoCheck;
        public SelectionToggle toggleNoShuffle;
        public TMP_InputField inputPort;
        public TMP_InputField inputLP;
        public TMP_InputField inputHand;
        public TMP_InputField inputDraw;

        [HideInInspector] public SelectionToggle_Solo lastSoloItem;
        public SuperScrollView superScrollView;
        private List<string[]> tasks = new List<string[]>();
        public static string port;
        private IList<BotInfo> bots = new List<BotInfo>();
        public static Condition condition;
        const string windbotDialogsPath = "Data/Windbot/Dialogs/";

        #region Servant
        public override void Initialize()
        {
            showLine = false;
            SwitchCondition(Condition.ForSolo);
            base.Initialize();

            var btnDeck = Manager.GetElement<SelectionButton>("ButtonDeck");
            btnDeck.SetButtonText(Config.Get("DeckInUse", "@ui"));
            btnDeck.gameObject.SetActive(false);
            Load();

            StartCoroutine(Program.instance.texture_.SetCommonShopButtonMaterial(Manager.GetElement<Image>("ButtonOut"), false));
            StartCoroutine(Program.instance.texture_.SetCommonShopButtonMaterial(Manager.GetElement<Image>("ButtonHover"), false));
        }
        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            if (condition == Condition.ForSolo)
            {
                YgoServer.StopServer();
            }
        }
        public override void SelectLastSelectable()
        {
            if (lastSoloItem != null)
                EventSystem.current.SetSelectedGameObject(lastSoloItem.gameObject);
        }
        protected override bool NeedResponseInput()
        {
            if (inputPort.isFocused)
                return false;
            if (inputLP.isFocused)
                return false;
            if(inputHand.isFocused)
                return false;
            if(inputDraw.isFocused)
                return false;

            return base.NeedResponseInput();
        }
        #endregion

        #region Solo
        public void SwitchCondition(Condition condition)
        {
            Solo.condition = condition;
            switch (condition)
            {
                case Condition.ForSolo:
                    returnServant = Program.instance.menu;
                    depth = 1;
                    toggleNoCheck.gameObject.SetActive(true);
                    toggleNoShuffle.gameObject.SetActive(true);
                    inputPort.gameObject.SetActive(true);
                    inputLP.gameObject.SetActive(true);
                    inputHand.gameObject.SetActive(true);
                    inputDraw.gameObject.SetActive(true);
                    break;
                case Condition.ForRoom:
                    returnServant = Program.instance.room;
                    depth = 3;
                    toggleNoCheck.gameObject.SetActive(false);
                    toggleNoShuffle.gameObject.SetActive(false);
                    inputPort.gameObject.SetActive(false);
                    inputLP.gameObject.SetActive(false);
                    inputHand.gameObject.SetActive(false);
                    inputDraw.gameObject.SetActive(false);
                    break;
            }
        }
        public void Load()
        {
            ReadBots(Program.localesPath + Language.GetConfig() + "/bot.conf");
            Print();
        }
        private void ReadBots(string confPath)
        {
            bots.Clear();
            StreamReader reader = new StreamReader(new FileStream(confPath, FileMode.Open, FileAccess.Read));
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line.Length > 0 && line[0] == '!')
                {
                    BotInfo newBot = new BotInfo();
                    newBot.name = line.TrimStart('!');
                    newBot.command = reader.ReadLine().Trim();
                    newBot.desc = reader.ReadLine().Trim();
                    line = reader.ReadLine().Trim();
                    newBot.flags = line.Split(' ');

                    newBot.main0 = 5990062;
                    try
                    {
                        string deckName = "";
                        deckName = newBot.command.Split(new string[] { "Deck=", " Dialog=" }, StringSplitOptions.RemoveEmptyEntries)[1].Replace("'", "").Replace(" ", "");
                        if(File.Exists("Data/Windbot/Decks/AI_" + deckName + Program.ydkExpansion)) 
                        {
                            var aiDeck = new Deck("Data/Windbot/Decks/AI_" + deckName + Program.ydkExpansion);
                            if(aiDeck.Main.Count > 0)
                                newBot.main0 = aiDeck.Main[0];
                        }
                    }
                    catch { }

                    bots.Add(newBot);
                }
            }
        }
        public void Print()
        {
            superScrollView?.Clear();
            tasks.Clear();
            transform.GetChild(0).gameObject.SetActive(true);

            for (int i = 0; i < bots.Count; i++)
            {
                string[] task = new string[]
                {
                    i.ToString(),
                };
                tasks.Add(task);
            }
            var handle = Addressables.LoadAssetAsync<GameObject>("ItemSolo");
            handle.Completed += (result) =>
            {
                var itemHeight = PropertyOverrider.NeedMobileLayout() ? 180f : 150f;
                float topPadding = PropertyOverrider.NeedMobileLayout() ? 148f : 134f;
                float space = itemHeight - (PropertyOverrider.NeedMobileLayout() ? 152f : 122f);
                float bottomPadding = (PropertyOverrider.NeedMobileLayout() ? 64f : 54f) - space;
                superScrollView = new SuperScrollView(
                    1,
                    700,
                    itemHeight,
                    topPadding,
                    bottomPadding,
                    result.Result,
                    ItemOnListRefresh,
                    Manager.GetElement<ScrollRect>("ScrollRect"));
                superScrollView.Print(tasks);
                StartCoroutine(SelectZero());
            };
        }
        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Solo>();
            handler.index = int.Parse(task[0]);
            handler.botInfo = bots[handler.index];
            handler.Refresh();
        }
        private IEnumerator SelectZero()
        {
            while (superScrollView == null || superScrollView.items.Count == 0 || superScrollView.items[0].gameObject == null)
                yield return null;
            var item0 = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Solo>();
            while (item0 == null || !item0.refreshed)
                yield return null;
            item0.SetToggleOn();
            Debug.LogFormat("Solo.SelectZero selected AI index={0}", item0.index);
            yield return new WaitForSecondsRealtime(2f);
            if(!showing)
                transform.GetChild(0).gameObject.SetActive(false);
        }
        public void OnSelectAIDeck()
        {
            Program.instance.selectDeck.SwitchCondition(SelectDeck.Condition.ForSolo);
            Program.instance.ShiftToServant(Program.instance.selectDeck);
        }
        string GetWindBotCommand(int aiCode, bool diyDeck)
        {
            BotInfo bot = bots[aiCode];
            string aiCommand = bot.command;
            if (diyDeck)
            {
                string selectedDeck = GetAIDeck();
                if (!File.Exists(Program.deckPath + selectedDeck + Program.ydkExpansion))
                {
                    MessageManager.Cast(InterString.Get("请先为AI选择有效的卡组。"));
                    return string.Empty;
                }
                aiCommand += " DeckFile=\"" + selectedDeck + "\"";
            }
            Match match = Regex.Match(aiCommand, "Random=(\\w+)");
            if (match.Success)
            {
                string randomFlag = match.Groups[1].Value;
                string command = GetRandomBot(randomFlag);
                if (command != string.Empty)
                    aiCommand = command;
            }
            return aiCommand;
        }
        public void OnPlay()
        {

            if (lastSoloItem == null)
            {
                Debug.LogWarning("Solo.OnPlay ignored: no AI item is ready yet.");
                MessageManager.Cast(InterString.Get("AI列表尚未加载完成，请稍后再试。"));
                return;
            }

            Debug.LogFormat("Solo.OnPlay submit AI index={0}", lastSoloItem.index);
            lastSoloItem.PublicSubmit();
        }
        public void SelectLastSoloItem()
        {
            UserInput.NextSelectionIsAxis = true;
            SelectLastSelectable();
        }
        public void StartAIForSolo(int aiCode, bool diyDeck)
        {
            Debug.LogFormat("Solo.StartAIForSolo aiCode={0}, diyDeck={1}", aiCode, diyDeck);
            string aiCommand = GetWindBotCommand(aiCode, diyDeck);
            if (!string.IsNullOrEmpty(aiCommand))
                Launch(aiCommand, toggleLockHand.isOn, toggleNoCheck.isOn, toggleNoShuffle.isOn);
            else
                Debug.LogWarning("Solo.StartAIForSolo canceled: empty AI command.");
        }
        public void StartAIForRoom(int aiCode, bool diyDeck)
        {
            string aiCommand = GetWindBotCommand(aiCode, diyDeck);
            if (!string.IsNullOrEmpty(aiCommand))
            {
                StartWindBot(aiCommand, TcpHelper.joinedAddress, TcpHelper.joinedPort, TcpHelper.joinedPassword, toggleLockHand.isOn, 600);
                Program.instance.ShiftToServant(Program.instance.room);
            }
        }
        private string GetRandomBot(string flag)
        {
            IList<BotInfo> foundBots = new List<BotInfo>();
            foreach (var bot in bots)
            {
                if (Array.IndexOf(bot.flags, flag) >= 0) foundBots.Add(bot);
            }
            if (foundBots.Count > 0)
            {
                System.Random rand = new System.Random();
                BotInfo bot = foundBots[rand.Next(foundBots.Count)];
                return bot.command;
            }
            return "";
        }
        private string GetFallbackWindbotDialog()
        {
            var config = Language.GetConfig();
            var suffix = "EN";
            if (config == "ge-DE")
                suffix = "DE";
            else if (config == "es-ES")
                suffix = "ES";
            else if (config == "fr-FR")
                suffix = "FR";
            else if (config == "it-IT")
                suffix = "IT";
            else if (config == "ja-JP")
                suffix = "JP";
            else if (config == "ko-KR")
                suffix = "KR";
            else if (config == "pt-PT")
                suffix = "PT";
            else if (config == "zh-CN")
                suffix = "zh-CN";
            else if (config == "zh-TW")
                suffix = "zh-TW";

            var candidate = "Universal." + suffix;
            if (File.Exists(windbotDialogsPath + candidate + ".json"))
                return candidate;
            return "Universal.EN";
        }
        public void StartWindBot(string command, string ip, string port, string password, bool lockHand, int delay)
        {
            Debug.LogFormat("Solo.StartWindBot ip={0}, port={1}, delay={2}, lockHand={3}, command={4}", ip, port, delay, lockHand, command);
            command = command.Replace("'", "\"");
            if (lockHand)
                command += " Hand=1";
            command += " Host=" + ip;
            command += " Port=" + port;
            command += " HostInfo=" + password;

            var args = Tools.SplitWithPreservedQuotes(command);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("Dialog="))
                {
                    var path = args[i][7..];
                    if(!File.Exists(windbotDialogsPath + path + ".json"))
                    {
                        args[i] = "Dialog=" + GetFallbackWindbotDialog();
                    }
                    break;
                }
            }

            (new Thread(() =>
            {
                try
                {
                    Thread.Sleep(delay);
                    WindBot.Program.Main(args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            })).Start();
        }
        public void Launch(string command, bool lockHand, bool noCheck, bool noShuffle)
        {
            port = inputPort.text;
            if (string.IsNullOrEmpty(port) || !int.TryParse(port, out var portNumber) || portNumber <= 0 || portNumber > 65535)
            {
                port = "7911";
                inputPort.text = port;
                portNumber = 7911;
            }
            Debug.LogFormat("Solo.Launch port={0}, lockHand={1}, noCheck={2}, noShuffle={3}, command={4}", port, lockHand, noCheck, noShuffle, command);

            string lp = inputLP.text;
            if (string.IsNullOrEmpty(lp) /*|| lp == "0"*/)
                lp = "8000";
            string hand = inputHand.text;
            if (string.IsNullOrEmpty(hand) /*|| hand == "0"*/)
                hand = "5";
            string draw = inputDraw.text;
            if (string.IsNullOrEmpty(draw) /*|| draw == "0"*/)
                draw = "5";
            string args = port + " -1 5 0 F " + (noCheck ? "T " : "F ") + (noShuffle ? "T " : "F ") + lp + " " + hand + " " + draw + " 0 0";
            if (TcpHelper.IsPortAvailable(portNumber))
            {
                Debug.LogFormat("Solo.Launch starting local server args={0}", args);
                YgoServer.StartServer(args);
                Room.fromSolo = true;
                if (lockHand)
                    Room.soloLockHand = true;
                else
                    Room.soloLockHand = false;
                Room.fromLocalHost = false;

                TcpHelper.LinkStart("127.0.0.1", Config.Get("DuelPlayerName0", "@ui"), port, string.Empty, true, () => StartWindBot(command, "127.0.0.1", port, string.Empty, lockHand, 0));
            }
            else
            {
                Debug.LogWarningFormat("Solo.Launch port unavailable: {0}", port);
                MessageManager.messageFromSubString = InterString.Get("端口被占用， 请尝试修改端口后再尝试。端口号应大于0，小于65535。");
            }
        }

        public void SetAIDeck(string deckName)
        {
            var btn = Manager.GetElement<SelectionButton>("ButtonDeck");
            btn.SetButtonText(deckName);
        }
        public string GetAIDeck()
        {
            var btn = Manager.GetElement<SelectionButton>("ButtonDeck");
            return btn.GetButtonText();
        }
        #endregion
    }
}

