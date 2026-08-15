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
                    PetDecayPolicy.Default,
                    PetGrowthPolicy.Default);

            PerformPetCareActionUseCase careUseCase =
                new PerformPetCareActionUseCase(
                    PetDecayPolicy.Default,
                    PetGrowthPolicy.Default,
                    repository);

            UpdatePetStateUseCase updateUseCase =
                new UpdatePetStateUseCase(
                    PetDecayPolicy.Default,
                    PetGrowthPolicy.Default,
                    repository);

            SelectStarterEggUseCase starterEggUseCase =
                new SelectStarterEggUseCase(repository);

            CompletePetHatchingUseCase hatchingUseCase =
                new CompletePetHatchingUseCase(repository);

            EvolvePetUseCase evolveUseCase =
                new EvolvePetUseCase(
                    PetDecayPolicy.Default,
                    PetGrowthPolicy.Default,
                    repository);

            RegisterPetTrainingUseCase trainingUseCase =
                new RegisterPetTrainingUseCase(
                    PetDecayPolicy.Default,
                    PetGrowthPolicy.Default,
                    repository);

            petSession = new PetSession(
                loadUseCase,
                careUseCase,
                updateUseCase,
                starterEggUseCase,
                hatchingUseCase,
                evolveUseCase,
                trainingUseCase);
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
                $"XP: {petState.Stats.CurrentExperience} | " +
                $"Health: {petState.Health.Value} | " +
                $"Coma: {petState.IsInComa} | " +
                $"Stage: {petState.Growth.Stage} | " +
                $"Variant: {petState.Growth.Variant} | " +
                $"Lineage: " +
                $"{(petState.Origin.HasSelectedLineage ? petState.Origin.LineageId : "Not selected")}");
        }

        public StarterEggSelectionResult SelectStarterEgg(
            string lineageId)
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "Game is not ready.");
            }

            StarterEggSelectionResult result =
                petSession.SelectStarterEgg(
                    lineageId,
                    DateTime.UtcNow);

            if (environmentConfig.EnableDebugLogs)
            {
                if (result.IsSuccessful)
                {
                    Debug.Log(
                        $"Starter egg selected | " +
                        $"Lineage: {result.Lineage.Id} | " +
                        $"Color rarity: {result.ColorRarity}",
                        this);
                }
                else
                {
                    Debug.LogWarning(
                        $"Starter egg rejected | " +
                        $"Reason: {result.Message}",
                        this);
                }
            }

            return result;
        }

        public PetEvolutionResult CompletePetHatching()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "Game is not ready.");
            }

            PetEvolutionResult result =
                petSession.CompleteHatching(DateTime.UtcNow);

            LogEvolutionResult(result);
            return result;
        }

        public PetEvolutionResult TryEvolvePet()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "Game is not ready.");
            }

            PetEvolutionResult result =
                petSession.TryEvolve(DateTime.UtcNow);

            LogEvolutionResult(result);
            return result;
        }

        public PetTrainingResult RegisterTeenTrainingSession()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "Game is not ready.");
            }

            PetTrainingResult result =
                petSession.RegisterTeenTraining(DateTime.UtcNow);

            if (environmentConfig.EnableDebugLogs)
            {
                if (result.IsSuccessful)
                {
                    Debug.Log(
                        $"Teen training completed | " +
                        $"Sessions: {result.CurrentSessions} | " +
                        $"Growth earned: {result.GrowthPointsEarned}",
                        this);
                }
                else
                {
                    Debug.LogWarning(
                        $"Teen training rejected | " +
                        $"Reason: {result.Message}",
                        this);
                }
            }

            return result;
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
                    $"Growth: " +
                    $"{result.GrowthResult.PreviousGrowthPoints} -> " +
                    $"{result.GrowthResult.CurrentGrowthPoints} | " +
                    $"Affection Earned: " +
                    $"{careResult.AffectionEarned:0.0}");
            }

            return result;
        }

        private void LogEvolutionResult(PetEvolutionResult result)
        {
            if (!environmentConfig.EnableDebugLogs)
            {
                return;
            }

            if (result.IsSuccessful)
            {
                Debug.Log(
                    $"Pet evolution completed | " +
                    $"Gate: {result.Gate} | " +
                    $"Stage: {result.PreviousStage} -> " +
                    $"{result.CurrentStage} | " +
                    $"Variant: {result.CurrentVariant}",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    $"Pet evolution rejected | " +
                    $"Reason: {result.Message}",
                    this);
            }
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
                PetDecayResult result =
                    petSession.SimulateAndSave(DateTime.UtcNow);

                if (!environmentConfig.EnableDebugLogs)
                {
                    return;
                }

                if (result.EnteredComa)
                {
                    Debug.LogWarning(
                        "Pet entered a care coma. " +
                        "All four needs must remain above halfway " +
                        "for the recovery window.",
                        this);
                }
                else if (result.RecoveredFromComa)
                {
                    Debug.Log(
                        "Pet recovered from the care coma.",
                        this);
                }
                else if (result.HealthChanged)
                {
                    Debug.Log(
                        $"Hidden Health updated | " +
                        $"{result.PreviousHealth} -> " +
                        $"{result.CurrentHealth} | " +
                        $"Evaluations: {result.HealthEvaluations}",
                        this);
                }
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
