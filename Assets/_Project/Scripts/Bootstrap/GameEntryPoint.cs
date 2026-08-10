using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SuccuPet.Application.Pets;
using SuccuPet.Core.Pets;
using SuccuPet.Infrastructure.Persistence.Pets;

namespace SuccuPet.Bootstrap
{
    public sealed class GameEntryPoint : MonoBehaviour
    {
        [Header("Environment")]
        [SerializeField]
        private EnvironmentConfig environmentConfig;

        [Header("Default Pet")]
        [SerializeField]
        private string defaultPetId = "player-pet-001";

        [SerializeField]
        private string defaultPetDisplayName = "Succu";

        [Header("Persistence")]
        [Min(5f)]
        [SerializeField]
        private float autosaveIntervalSeconds = 30f;

        private static GameEntryPoint instance;

        private PetSession petSession;
        private float autosaveTimer;

        public static GameEntryPoint Instance => instance;

        public EnvironmentConfig EnvironmentConfig =>
            environmentConfig;

        public PetSession PetSession =>
            petSession;

        public bool IsReady =>
            petSession != null &&
            petSession.IsInitialized;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            DontDestroyOnLoad(gameObject);

            if (environmentConfig == null)
            {
                Debug.LogError(
                    "EnvironmentConfig is not assigned " +
                    "to GameEntryPoint.",
                    this);

                enabled = false;
                return;
            }

            ConfigureApplication();
            ComposeDependencies();
            InitializeGame();

            SceneManager.activeSceneChanged +=
                HandleActiveSceneChanged;
        }

        private void ConfigureApplication()
        {
            QualitySettings.vSyncCount = 0;

            UnityEngine.Application.targetFrameRate = 60;

            Screen.sleepTimeout =
                SleepTimeout.SystemSetting;
        }

        private void ComposeDependencies()
        {
            IPetStateRepository repository =
                new JsonFilePetStateRepository(
                    "pet-state-v1.json");

            LoadOrCreatePetStateUseCase loadUseCase =
                new LoadOrCreatePetStateUseCase(
                    repository,
                    PetDecayPolicy.Default);

            PerformPetCareActionUseCase careUseCase =
                new PerformPetCareActionUseCase(
                    PetDecayPolicy.Default,
                    repository);

            UpdatePetStateUseCase updateUseCase =
                new UpdatePetStateUseCase(
                    PetDecayPolicy.Default,
                    repository);

            petSession = new PetSession(
                loadUseCase,
                careUseCase,
                updateUseCase);
        }

        private void InitializeGame()
        {
            DateTime utcNow = DateTime.UtcNow;

            LoadPetStateResult result =
                petSession.Initialize(
                    defaultPetId,
                    defaultPetDisplayName,
                    utcNow);

            if (!environmentConfig.EnableDebugLogs)
            {
                return;
            }

            PetState petState =
                petSession.CurrentPetState;

            Debug.Log(
                $"SuccuPet started in " +
                $"{environmentConfig.Environment} environment.");

            Debug.Log(
                $"Pet session ready | " +
                $"Created: {result.WasCreated} | " +
                $"Offline hours: " +
                $"{result.DecayResult.AppliedHours:0.00} | " +
                $"Pet ID: {petState.Profile.PetId} | " +
                $"Name: {petState.Profile.DisplayName} | " +
                $"Level: {petState.Stats.Level} | " +
                $"XP: {petState.Stats.CurrentExperience}");
        }

        public PerformPetCareActionResult PerformCareAction(
            PetCareActionType actionType)
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "Game is not ready.");
            }

            PerformPetCareActionResult result =
                petSession.PerformCareAction(
                    actionType,
                    DateTime.UtcNow);

            PetCareActionResult careResult =
                result.CareResult;

            if (!careResult.IsSuccessful)
            {
                if (environmentConfig.EnableDebugLogs)
                {
                    Debug.LogWarning(
                        $"Pet action rejected | " +
                        $"Action: {careResult.ActionType} | " +
                        $"Need: {careResult.TargetNeed} | " +
                        $"Value: {careResult.PreviousNeedValue:0.0} | " +
                        $"Reason: {careResult.Message}");
                }

                return result;
            }

            if (environmentConfig.EnableDebugLogs)
            {
                Debug.Log(
                    $"Pet action completed | " +
                    $"Action: {careResult.ActionType} | " +
                    $"Need: {careResult.TargetNeed} | " +
                    $"Value: {careResult.PreviousNeedValue:0.0} -> " +
                    $"{careResult.CurrentNeedValue:0.0} | " +
                    $"XP Earned: {careResult.ExperienceEarned} | " +
                    $"Affection Earned: " +
                    $"{careResult.AffectionEarned:0.0}");
            }

            return result;
        }

        public SetPetSleepingResult SetPetSleeping(
            bool shouldSleep)
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "Game is not ready.");
            }

            SetPetSleepingResult result =
                petSession.SetSleeping(
                    shouldSleep,
                    DateTime.UtcNow);

            if (environmentConfig.EnableDebugLogs &&
                result.StateChanged)
            {
                Debug.Log(
                    result.IsSleeping
                        ? "Pet started sleeping."
                        : "Pet woke up.");
            }

            return result;
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            autosaveTimer += Time.unscaledDeltaTime;

            if (autosaveTimer < autosaveIntervalSeconds)
            {
                return;
            }

            autosaveTimer = 0f;
            SimulateAndSave();
        }

        private void OnApplicationPause(bool isPaused)
        {
            SimulateAndSave();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SimulateAndSave();
        }

        private void HandleActiveSceneChanged(
            Scene previousScene,
            Scene currentScene)
        {
            SimulateAndSave();
        }

        private void OnApplicationQuit()
        {
            SimulateAndSave();
        }

        private void SimulateAndSave()
        {
            if (!IsReady)
            {
                return;
            }

            try
            {
                petSession.SimulateAndSave(DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Could not save pet state: " +
                    $"{exception.Message}",
                    this);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.activeSceneChanged -=
                    HandleActiveSceneChanged;

                SimulateAndSave();
                instance = null;
            }
        }
    }
}
