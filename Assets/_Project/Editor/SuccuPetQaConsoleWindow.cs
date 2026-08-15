// Place this file at: Assets/_Project/Editor/SuccuPetQaConsoleWindow.cs
// This tool is Editor-only and does not become part of the WebGL build.

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SuccuPet.EditorTools
{
    public sealed class SuccuPetQaConsoleWindow : EditorWindow
    {
        [Serializable]
        private sealed class PetSaveRecord
        {
            public int schemaVersion = 6;
            public string petId = "player-pet-001";
            public string displayName = "Succu";
            public string createdAtUtc;
            public string lastNeedsUpdateUtc;

            public bool isSleeping;
            public long sleepStartedUtcTicks;
            public long lastSimulationUtcTicks;

            public int health = 100;
            public double healthEvaluationProgressMinutes;
            public bool isInComa;
            public long comaStartedUtcTicks;
            public double comaRecoveryProgressHours;
            public bool isDead;
            public long diedAtUtcTicks;

            public bool hasSelectedStarterEgg;
            public string lineageId;
            public int acquisitionType;
            public int colorSeed;
            public int colorRarity;
            public long acquiredAtUtcTicks;

            public int growthStage;
            public int evolutionVariant;
            public int growthPoints;
            public int teenTrainingSessions;
            public long stageStartedAtUtcTicks;

            public float fullness = 100f;
            public float energy = 100f;
            public float happiness = 100f;
            public float hygiene = 100f;

            public int level = 1;
            public int currentExperience;
            public float affection;
            public int coins;
        }

        private const string SaveFileName = "pet-state-v1.json";
        private const string ChecklistKeyPrefix = "SuccuPet.QA.Check.";

        private static readonly string[] Tabs =
        {
            "Dashboard",
            "Manual Data",
            "Presets",
            "Checklist"
        };

        private static readonly string[] ChecklistItems =
        {
            "Unity Console has zero red errors",
            "Egg selection -> confirmation -> hatching -> pet screen",
            "Feed, Play, Bathe and Sleep actions work",
            "Full-stat action is rejected with visible feedback",
            "Needs decay while time passes",
            "Sleep state survives a restart",
            "Coma -> recovery/death flow works",
            "Game Over -> Start New Pet -> egg selection works",
            "Tutorial can be completed or skipped",
            "Save data survives stop/play and browser restart"
        };

        private PetSaveRecord data;
        private Vector2 scrollPosition;
        private int selectedTab;
        private float offlineHours = 1f;
        private string statusMessage = "Ready";
        private MessageType statusType = MessageType.Info;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;

        private string SavePath =>
            Path.Combine(
                UnityEngine.Application.persistentDataPath,
                SaveFileName);

        [MenuItem("Tools/SuccuPet/QA Console %#q")]
        public static void Open()
        {
            SuccuPetQaConsoleWindow window =
                GetWindow<SuccuPetQaConsoleWindow>();

            window.titleContent = new GUIContent("SuccuPet QA");
            window.minSize = new Vector2(540f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadFromDisk(showStatus: false);
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();

            selectedTab = GUILayout.Toolbar(
                selectedTab,
                Tabs,
                GUILayout.Height(28f));

            EditorGUILayout.Space(6f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0:
                    DrawDashboard();
                    break;
                case 1:
                    DrawManualData();
                    break;
                case 2:
                    DrawPresets();
                    break;
                case 3:
                    DrawChecklist();
                    break;
            }

            EditorGUILayout.EndScrollView();
            DrawFooter();
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (sectionStyle == null)
            {
                sectionStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13
                };
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                "SUCCUPET  /  QA CONSOLE",
                titleStyle,
                GUILayout.Height(30f));

            Color previousColor = GUI.color;
            GUI.color = EditorApplication.isPlaying
                ? new Color(0.45f, 1f, 0.60f)
                : new Color(1f, 0.82f, 0.40f);

            GUILayout.Label(
                EditorApplication.isPlaying ? "● PLAY MODE" : "● EDIT MODE",
                EditorStyles.boldLabel,
                GUILayout.Width(110f));

            GUI.color = previousColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "Fast save editing, runtime actions and final submission checks.",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawDashboard()
        {
            DrawSection("SAVE STATUS");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawKeyValue("File", File.Exists(SavePath) ? "Found" : "Not created");
            DrawKeyValue("Path", SavePath);
            DrawKeyValue("Schema", data.schemaVersion.ToString());
            DrawKeyValue("Pet", data.displayName + "  (" + data.petId + ")");
            DrawKeyValue("Stage", GetGrowthStageName(data.growthStage));
            DrawKeyValue("Condition", GetConditionLabel());
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);
            DrawSection("CURRENT SNAPSHOT");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawProgress("Fullness", data.fullness, new Color(0.95f, 0.55f, 0.45f));
            DrawProgress("Energy", data.energy, new Color(0.35f, 0.75f, 1f));
            DrawProgress("Happiness", data.happiness, new Color(1f, 0.72f, 0.25f));
            DrawProgress("Hygiene", data.hygiene, new Color(0.55f, 0.90f, 0.82f));
            DrawKeyValue("Hidden Health", data.health.ToString());
            DrawKeyValue("Level / XP", data.level + " / " + data.currentExperience);
            DrawKeyValue("Growth / Training", data.growthPoints + " / " + data.teenTrainingSessions);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6f);
            DrawSection("SAVE COMMANDS");
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reload From Disk", GUILayout.Height(32f)))
            {
                LoadFromDisk(showStatus: true);
            }

            if (GUILayout.Button("Save Data", GUILayout.Height(32f)))
            {
                SaveToDisk(createBackup: true);
            }

            EditorGUILayout.EndHorizontal();

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.45f, 0.85f, 0.65f);

            if (GUILayout.Button(
                    EditorApplication.isPlaying
                        ? "SAVE & RESTART PLAY MODE"
                        : "SAVE & ENTER PLAY MODE",
                    GUILayout.Height(38f)))
            {
                SaveAndStartPlayMode();
            }

            GUI.backgroundColor = previousBackground;

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Reveal Save File"))
            {
                RevealSaveFile();
            }

            if (GUILayout.Button("Open Save Folder"))
            {
                EditorUtility.RevealInFinder(
                    UnityEngine.Application.persistentDataPath);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            DrawRuntimeActions();
        }

        private void DrawRuntimeActions()
        {
            DrawSection("LIVE ACTION TESTS");

            bool runtimeReady = IsRuntimeReady();
            EditorGUILayout.HelpBox(
                runtimeReady
                    ? "GameEntryPoint is ready. These buttons use the real game flow."
                    : "Enter Play Mode and wait for GameEntryPoint to become ready.",
                runtimeReady ? MessageType.Info : MessageType.Warning);

            using (new EditorGUI.DisabledScope(!runtimeReady))
            {
                EditorGUILayout.BeginHorizontal();
                RuntimeButton("FEED", () => InvokeCareAction("Feed"));
                RuntimeButton("PLAY", () => InvokeCareAction("Play"));
                RuntimeButton("BATHE", () => InvokeCareAction("Bathe"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                RuntimeButton("SLEEP", () => InvokeEntryPoint("SetPetSleeping", true));
                RuntimeButton("WAKE", () => InvokeEntryPoint("SetPetSleeping", false));
                RuntimeButton("SIMULATE + SAVE", InvokeRuntimeSave);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                RuntimeButton("TRY EVOLVE", () => InvokeEntryPoint("TryEvolvePet"));
                RuntimeButton("ADD TRAINING", () => InvokeEntryPoint("RegisterTeenTrainingSession"));
                RuntimeButton("START NEW PET", () => InvokeEntryPoint("StartNewPet"));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawManualData()
        {
            DrawSection("IDENTITY");
            data.petId = EditorGUILayout.TextField("Pet ID", data.petId);
            data.displayName = EditorGUILayout.TextField("Display Name", data.displayName);
            data.createdAtUtc = EditorGUILayout.TextField("Created UTC", data.createdAtUtc);

            EditorGUILayout.Space(8f);
            DrawSection("NEEDS (0 - 100)");
            data.fullness = EditorGUILayout.Slider("Fullness", data.fullness, 0f, 100f);
            data.energy = EditorGUILayout.Slider("Energy", data.energy, 0f, 100f);
            data.happiness = EditorGUILayout.Slider("Happiness", data.happiness, 0f, 100f);
            data.hygiene = EditorGUILayout.Slider("Hygiene", data.hygiene, 0f, 100f);

            EditorGUILayout.Space(8f);
            DrawSection("HEALTH & SURVIVAL");
            data.health = EditorGUILayout.IntSlider("Hidden Health", data.health, 0, 100);
            data.healthEvaluationProgressMinutes = EditorGUILayout.DoubleField(
                "Health Eval Minutes",
                data.healthEvaluationProgressMinutes);
            data.isInComa = EditorGUILayout.Toggle("In Coma", data.isInComa);
            data.comaRecoveryProgressHours = EditorGUILayout.DoubleField(
                "Coma Recovery Hours",
                data.comaRecoveryProgressHours);
            data.isDead = EditorGUILayout.Toggle("Dead", data.isDead);
            data.isSleeping = EditorGUILayout.Toggle("Sleeping", data.isSleeping);

            EditorGUILayout.Space(8f);
            DrawSection("ORIGIN & GROWTH");
            data.hasSelectedStarterEgg = EditorGUILayout.Toggle(
                "Starter Egg Selected",
                data.hasSelectedStarterEgg);
            data.lineageId = EditorGUILayout.TextField("Lineage ID", data.lineageId);
            data.acquisitionType = EditorGUILayout.IntPopup(
                "Acquisition",
                data.acquisitionType,
                new[] { "None", "Starter Egg", "Gold Shop", "Breeding", "Cafe Claim", "Legacy" },
                new[] { 0, 1, 2, 3, 4, 5 });
            data.colorRarity = EditorGUILayout.IntPopup(
                "Color Rarity",
                data.colorRarity,
                new[] { "Common", "Uncommon", "Rare" },
                new[] { 0, 1, 2 });
            data.colorSeed = EditorGUILayout.IntField("Color Seed", data.colorSeed);
            data.growthStage = EditorGUILayout.IntPopup(
                "Growth Stage",
                data.growthStage,
                new[] { "Egg", "Bat (Baby)", "Teen", "Adult" },
                new[] { 0, 1, 2, 3 });
            data.evolutionVariant = EditorGUILayout.IntPopup(
                "Variant",
                data.evolutionVariant,
                new[] { "None", "Default", "Special" },
                new[] { 0, 1, 2 });
            data.growthPoints = Mathf.Max(
                0,
                EditorGUILayout.IntField("Growth Points", data.growthPoints));
            data.teenTrainingSessions = Mathf.Max(
                0,
                EditorGUILayout.IntField("Training Sessions", data.teenTrainingSessions));

            EditorGUILayout.Space(8f);
            DrawSection("PROGRESSION");
            data.level = Mathf.Max(1, EditorGUILayout.IntField("Level", data.level));
            data.currentExperience = Mathf.Max(
                0,
                EditorGUILayout.IntField("Current XP", data.currentExperience));
            data.affection = Mathf.Max(
                0f,
                EditorGUILayout.FloatField("Affection", data.affection));
            data.coins = Mathf.Max(0, EditorGUILayout.IntField("Coins", data.coins));

            EditorGUILayout.Space(10f);
            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.45f, 0.85f, 0.65f);

            if (GUILayout.Button("SAVE MANUAL DATA", GUILayout.Height(38f)))
            {
                NormalizeStateBeforeSave();
                SaveToDisk(createBackup: true);
            }

            GUI.backgroundColor = previousBackground;
        }

        private void DrawPresets()
        {
            EditorGUILayout.HelpBox(
                "A preset edits the JSON save. Use Save & Enter/Restart Play Mode to load it into the game.",
                MessageType.Info);

            DrawSection("CORE FLOW PRESETS");
            DrawPresetButton(
                "NEW PLAYER / EGG SELECTION",
                "Fresh save, no starter lineage selected.",
                ApplyNewPlayerPreset);
            DrawPresetButton(
                "HEALTHY BAT",
                "Normal playable pet with balanced needs.",
                ApplyHealthyBatPreset);
            DrawPresetButton(
                "LOW NEEDS",
                "All four needs at 10 for decay and warning tests.",
                ApplyLowNeedsPreset);
            DrawPresetButton(
                "SLEEPING PET",
                "Sleeping state persistence test.",
                ApplySleepingPreset);

            EditorGUILayout.Space(8f);
            DrawSection("HEALTH / GAME OVER PRESETS");
            DrawPresetButton(
                "COMA",
                "Health 0, coma active, pet not dead.",
                ApplyComaPreset);
            DrawPresetButton(
                "COMA READY TO RECOVER",
                "Needs healthy and the 24-hour recovery progress near completion.",
                ApplyComaRecoveryPreset);
            DrawPresetButton(
                "DEAD / GAME OVER",
                "Loads directly into the Game Over flow.",
                ApplyDeadPreset);

            EditorGUILayout.Space(8f);
            DrawSection("EVOLUTION PRESETS");
            DrawPresetButton(
                "BAT READY TO EVOLVE",
                "Bat stage, 100 growth, high health.",
                ApplyBatReadyPreset);
            DrawPresetButton(
                "TEEN READY FOR SPECIAL ADULT",
                "Special Teen, 250 growth, 5 training sessions.",
                ApplyTeenReadyPreset);

            EditorGUILayout.Space(8f);
            DrawSection("OFFLINE TIME SIMULATION");
            offlineHours = EditorGUILayout.Slider(
                "Hours Ago",
                offlineHours,
                0.25f,
                72f);

            if (GUILayout.Button("SET LAST UPDATE " + offlineHours.ToString("0.##") + " HOURS AGO"))
            {
                DateTime simulatedTime = DateTime.UtcNow.AddHours(-offlineHours);
                data.lastSimulationUtcTicks = simulatedTime.Ticks;
                data.lastNeedsUpdateUtc = simulatedTime.ToString("O");
                SaveToDisk(createBackup: true);
                SetStatus("Offline time applied. Enter Play Mode to run decay.", MessageType.Info);
            }

            EditorGUILayout.Space(8f);
            DrawSection("TUTORIAL TEST DATA");

            if (GUILayout.Button("CLEAR FIRST-CARE PLAYERPREFS"))
            {
                ClearFirstCarePlayerPrefs();
            }
        }

        private void DrawChecklist()
        {
            int completed = 0;

            for (int index = 0; index < ChecklistItems.Length; index++)
            {
                if (EditorPrefs.GetBool(ChecklistKeyPrefix + index, false))
                {
                    completed++;
                }
            }

            EditorGUILayout.LabelField(
                "FINAL QA  " + completed + " / " + ChecklistItems.Length,
                titleStyle);

            Rect progressRect = GUILayoutUtility.GetRect(18f, 18f);
            EditorGUI.ProgressBar(
                progressRect,
                completed / (float)ChecklistItems.Length,
                completed == ChecklistItems.Length ? "READY TO BUILD" : "TESTING");

            EditorGUILayout.Space(10f);

            for (int index = 0; index < ChecklistItems.Length; index++)
            {
                string key = ChecklistKeyPrefix + index;
                bool previous = EditorPrefs.GetBool(key, false);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                bool current = EditorGUILayout.ToggleLeft(
                    (index + 1) + ".  " + ChecklistItems[index],
                    previous,
                    EditorStyles.boldLabel);
                EditorGUILayout.EndVertical();

                if (current != previous)
                {
                    EditorPrefs.SetBool(key, current);
                }
            }

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("RESET CHECKLIST"))
            {
                for (int index = 0; index < ChecklistItems.Length; index++)
                {
                    EditorPrefs.DeleteKey(ChecklistKeyPrefix + index);
                }
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(statusMessage, statusType);
        }

        private void DrawSection(string title)
        {
            EditorGUILayout.LabelField(title, sectionStyle);
        }

        private static void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, EditorStyles.boldLabel, GUILayout.Width(135f));
            EditorGUILayout.SelectableLabel(value ?? "-", GUILayout.Height(18f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawProgress(string label, float value, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(75f));
            Rect rect = GUILayoutUtility.GetRect(18f, 18f);
            Color previous = GUI.color;
            GUI.color = color;
            EditorGUI.ProgressBar(rect, Mathf.Clamp01(value / 100f), value.ToString("0.0"));
            GUI.color = previous;
            EditorGUILayout.EndHorizontal();
        }

        private static void RuntimeButton(string label, Action action)
        {
            if (GUILayout.Button(label, GUILayout.Height(28f)))
            {
                action();
            }
        }

        private void DrawPresetButton(string title, string description, Action action)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("APPLY PRESET"))
            {
                action();
                NormalizeStateBeforeSave();
                SaveToDisk(createBackup: true);
                SetStatus(title + " preset saved.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void LoadFromDisk(bool showStatus)
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    data = CreateFreshRecord();

                    if (showStatus)
                    {
                        SetStatus("No save found. Fresh editable data created in the window.", MessageType.Warning);
                    }

                    return;
                }

                string json = File.ReadAllText(SavePath);
                PetSaveRecord loaded = JsonUtility.FromJson<PetSaveRecord>(json);
                data = loaded ?? CreateFreshRecord();

                if (data.schemaVersion < 6)
                {
                    data.schemaVersion = 6;
                }

                if (showStatus)
                {
                    SetStatus("Save data loaded from disk.", MessageType.Info);
                }
            }
            catch (Exception exception)
            {
                data = CreateFreshRecord();
                SetStatus("Could not load save: " + exception.Message, MessageType.Error);
            }
        }

        private void SaveToDisk(bool createBackup)
        {
            try
            {
                NormalizeStateBeforeSave();
                Directory.CreateDirectory(
                    UnityEngine.Application.persistentDataPath);

                if (createBackup && File.Exists(SavePath))
                {
                    File.Copy(SavePath, SavePath + ".qa-backup", overwrite: true);
                }

                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                AssetDatabase.Refresh();
                SetStatus("Save written successfully. A QA backup was kept.", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus("Could not write save: " + exception.Message, MessageType.Error);
            }
        }

        private void NormalizeStateBeforeSave()
        {
            data.schemaVersion = 6;
            data.petId = string.IsNullOrWhiteSpace(data.petId)
                ? "player-pet-001"
                : data.petId.Trim();
            data.displayName = string.IsNullOrWhiteSpace(data.displayName)
                ? "Succu"
                : data.displayName.Trim();

            data.fullness = Mathf.Clamp(data.fullness, 0f, 100f);
            data.energy = Mathf.Clamp(data.energy, 0f, 100f);
            data.happiness = Mathf.Clamp(data.happiness, 0f, 100f);
            data.hygiene = Mathf.Clamp(data.hygiene, 0f, 100f);
            data.health = Mathf.Clamp(data.health, 0, 100);

            if (data.lastSimulationUtcTicks <= 0)
            {
                data.lastSimulationUtcTicks = DateTime.UtcNow.Ticks;
            }

            DateTime lastSimulation = new DateTime(
                data.lastSimulationUtcTicks,
                DateTimeKind.Utc);
            data.lastNeedsUpdateUtc = lastSimulation.ToString("O");

            if (string.IsNullOrWhiteSpace(data.createdAtUtc))
            {
                data.createdAtUtc = DateTime.UtcNow.ToString("O");
            }

            if (data.isDead)
            {
                data.isInComa = false;
                data.isSleeping = false;
                data.sleepStartedUtcTicks = 0;
                data.comaStartedUtcTicks = 0;
                data.comaRecoveryProgressHours = 0d;
                data.health = 0;

                if (data.diedAtUtcTicks <= 0)
                {
                    data.diedAtUtcTicks = DateTime.UtcNow.Ticks;
                }
            }
            else
            {
                data.diedAtUtcTicks = 0;
            }

            if (data.isInComa)
            {
                data.isSleeping = false;
                data.sleepStartedUtcTicks = 0;
                data.health = 0;

                if (data.comaStartedUtcTicks <= 0)
                {
                    data.comaStartedUtcTicks = DateTime.UtcNow.Ticks;
                }
            }
            else
            {
                data.comaStartedUtcTicks = 0;
            }

            if (data.isSleeping && data.sleepStartedUtcTicks <= 0)
            {
                data.sleepStartedUtcTicks = DateTime.UtcNow.Ticks;
            }

            if (!data.isSleeping)
            {
                data.sleepStartedUtcTicks = 0;
            }

            if (!data.hasSelectedStarterEgg)
            {
                data.lineageId = string.Empty;
                data.acquisitionType = 0;
                data.acquiredAtUtcTicks = 0;
                data.growthStage = 0;
                data.evolutionVariant = 0;
            }
            else
            {
                EnsureSelectedLineage();
            }
        }

        private PetSaveRecord CreateFreshRecord()
        {
            DateTime utcNow = DateTime.UtcNow;

            return new PetSaveRecord
            {
                schemaVersion = 6,
                petId = "player-pet-001",
                displayName = "Succu",
                createdAtUtc = utcNow.ToString("O"),
                lastNeedsUpdateUtc = utcNow.ToString("O"),
                lastSimulationUtcTicks = utcNow.Ticks,
                health = 100,
                fullness = 100f,
                energy = 100f,
                happiness = 100f,
                hygiene = 100f,
                level = 1
            };
        }

        private void ApplyNewPlayerPreset()
        {
            data = CreateFreshRecord();
        }

        private void ApplyHealthyBatPreset()
        {
            EnsurePlayableBase();
            data.health = 100;
            data.fullness = 80f;
            data.energy = 80f;
            data.happiness = 80f;
            data.hygiene = 80f;
            data.growthStage = 1;
            data.evolutionVariant = 0;
        }

        private void ApplyLowNeedsPreset()
        {
            EnsurePlayableBase();
            data.health = 40;
            data.fullness = 10f;
            data.energy = 10f;
            data.happiness = 10f;
            data.hygiene = 10f;
        }

        private void ApplySleepingPreset()
        {
            EnsurePlayableBase();
            data.isSleeping = true;
            data.sleepStartedUtcTicks = DateTime.UtcNow.Ticks;
        }

        private void ApplyComaPreset()
        {
            EnsurePlayableBase();
            data.health = 0;
            data.isInComa = true;
            data.comaStartedUtcTicks = DateTime.UtcNow.Ticks;
            data.comaRecoveryProgressHours = 0d;
        }

        private void ApplyComaRecoveryPreset()
        {
            ApplyComaPreset();
            data.fullness = 90f;
            data.energy = 90f;
            data.happiness = 90f;
            data.hygiene = 90f;
            data.comaRecoveryProgressHours = 23.95d;
        }

        private void ApplyDeadPreset()
        {
            EnsurePlayableBase();
            data.health = 0;
            data.isDead = true;
            data.diedAtUtcTicks = DateTime.UtcNow.Ticks;
        }

        private void ApplyBatReadyPreset()
        {
            EnsurePlayableBase();
            data.growthStage = 1;
            data.evolutionVariant = 0;
            data.growthPoints = 100;
            data.health = 100;
        }

        private void ApplyTeenReadyPreset()
        {
            EnsurePlayableBase();
            data.growthStage = 2;
            data.evolutionVariant = 2;
            data.growthPoints = 250;
            data.teenTrainingSessions = 5;
            data.health = 100;
        }

        private void EnsurePlayableBase()
        {
            data.isDead = false;
            data.diedAtUtcTicks = 0;
            data.isInComa = false;
            data.comaStartedUtcTicks = 0;
            data.comaRecoveryProgressHours = 0d;
            data.isSleeping = false;
            data.sleepStartedUtcTicks = 0;
            data.hasSelectedStarterEgg = true;
            EnsureSelectedLineage();

            if (data.growthStage == 0)
            {
                data.growthStage = 1;
            }
        }

        private void EnsureSelectedLineage()
        {
            data.hasSelectedStarterEgg = true;

            if (string.IsNullOrWhiteSpace(data.lineageId))
            {
                data.lineageId = "free-succubus-violet";
            }

            data.acquisitionType = data.acquisitionType == 0
                ? 1
                : data.acquisitionType;

            if (data.acquiredAtUtcTicks <= 0)
            {
                data.acquiredAtUtcTicks = DateTime.UtcNow.Ticks;
            }

            if (data.stageStartedAtUtcTicks <= 0)
            {
                data.stageStartedAtUtcTicks = DateTime.UtcNow.Ticks;
            }
        }

        private void SaveAndStartPlayMode()
        {
            SaveToDisk(createBackup: true);

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += WaitForEditModeThenPlay;
            }
            else if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
            }
        }

        private static void WaitForEditModeThenPlay()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += WaitForEditModeThenPlay;
                return;
            }

            EditorApplication.isPlaying = true;
        }

        private void RevealSaveFile()
        {
            if (File.Exists(SavePath))
            {
                EditorUtility.RevealInFinder(SavePath);
            }
            else
            {
                SetStatus("Save file does not exist yet.", MessageType.Warning);
            }
        }

        private bool IsRuntimeReady()
        {
            if (!EditorApplication.isPlaying)
            {
                return false;
            }

            object entryPoint = GetEntryPointInstance();

            if (entryPoint == null)
            {
                return false;
            }

            PropertyInfo readyProperty = entryPoint.GetType().GetProperty("IsReady");
            return readyProperty != null && (bool)readyProperty.GetValue(entryPoint);
        }

        private object GetEntryPointInstance()
        {
            Type type = FindType("SuccuPet.Bootstrap.GameEntryPoint");
            PropertyInfo instanceProperty = type?.GetProperty(
                "Instance",
                BindingFlags.Public | BindingFlags.Static);
            return instanceProperty?.GetValue(null);
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(fullName, throwOnError: false);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private void InvokeCareAction(string enumValue)
        {
            try
            {
                object entryPoint = GetEntryPointInstance();
                Type actionType = FindType("SuccuPet.Core.Pets.PetCareActionType");

                if (entryPoint == null || actionType == null)
                {
                    throw new InvalidOperationException("Runtime types are not ready.");
                }

                object action = Enum.Parse(actionType, enumValue);
                InvokeEntryPoint("PerformCareAction", action);
            }
            catch (Exception exception)
            {
                SetStatus("Runtime action failed: " + GetRootMessage(exception), MessageType.Error);
            }
        }

        private void InvokeEntryPoint(string methodName, params object[] arguments)
        {
            try
            {
                object entryPoint = GetEntryPointInstance();

                if (entryPoint == null)
                {
                    throw new InvalidOperationException("GameEntryPoint was not found.");
                }

                MethodInfo method = FindCompatibleMethod(
                    entryPoint.GetType(),
                    methodName,
                    arguments);

                if (method == null)
                {
                    throw new MissingMethodException(methodName);
                }

                method.Invoke(entryPoint, arguments);
                SetStatus(methodName + " executed. Check Game view and Console.", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus(methodName + " failed: " + GetRootMessage(exception), MessageType.Error);
            }
        }

        private void InvokeRuntimeSave()
        {
            try
            {
                object entryPoint = GetEntryPointInstance();

                if (entryPoint == null)
                {
                    throw new InvalidOperationException("GameEntryPoint was not found.");
                }

                MethodInfo method = entryPoint.GetType().GetMethod(
                    "SimulateAndSave",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (method == null)
                {
                    throw new MissingMethodException("SimulateAndSave");
                }

                method.Invoke(entryPoint, null);
                LoadFromDisk(showStatus: false);
                SetStatus("Runtime state simulated and saved.", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus("Runtime save failed: " + GetRootMessage(exception), MessageType.Error);
            }
        }

        private static MethodInfo FindCompatibleMethod(
            Type type,
            string methodName,
            object[] arguments)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

            for (int methodIndex = 0; methodIndex < methods.Length; methodIndex++)
            {
                MethodInfo method = methods[methodIndex];

                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != arguments.Length)
                {
                    continue;
                }

                bool compatible = true;

                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    object argument = arguments[parameterIndex];

                    if (argument != null &&
                        !parameters[parameterIndex].ParameterType.IsInstanceOfType(argument))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible)
                {
                    return method;
                }
            }

            return null;
        }

        private void ClearFirstCarePlayerPrefs()
        {
            string[] likelyKeys =
            {
                "SuccuPet.FirstCare.Pending",
                "SuccuPet.FirstCare.Completed",
                "SuccuPet.FirstCare.Step",
                "SuccuPet.FirstCare.PetId",
                "FirstCareTutorialPending",
                "FirstCareTutorialCompleted",
                "FirstCareTutorialStep"
            };

            for (int index = 0; index < likelyKeys.Length; index++)
            {
                PlayerPrefs.DeleteKey(likelyKeys[index]);
            }

            PlayerPrefs.Save();
            SetStatus(
                "Known first-care keys were cleared. If your script uses different key names, use its constants.",
                MessageType.Info);
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                LoadFromDisk(showStatus: false);
                Repaint();
            }
        }

        private string GetConditionLabel()
        {
            if (data.isDead)
            {
                return "DEAD";
            }

            if (data.isInComa)
            {
                return "COMA";
            }

            if (data.isSleeping)
            {
                return "SLEEPING";
            }

            if (!data.hasSelectedStarterEgg)
            {
                return "AWAITING EGG SELECTION";
            }

            return "ACTIVE";
        }

        private static string GetGrowthStageName(int stage)
        {
            switch (stage)
            {
                case 0:
                    return "Egg";
                case 1:
                    return "Bat (Baby)";
                case 2:
                    return "Teen";
                case 3:
                    return "Adult";
                default:
                    return "Unknown (" + stage + ")";
            }
        }

        private static string GetRootMessage(Exception exception)
        {
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception.Message;
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            Repaint();
        }
    }
}