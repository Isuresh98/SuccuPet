using System;
using UnityEngine;
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

        private static GameEntryPoint instance;

        private PetSession petSession;

        public static GameEntryPoint Instance => instance;

        public EnvironmentConfig EnvironmentConfig =>
            environmentConfig;

        public PetSession PetSession => petSession;

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

            petSession = new PetSession(
                loadUseCase,
                careUseCase);
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

            if (environmentConfig.EnableDebugLogs)
            {
                PetCareActionResult careResult =
                    result.CareResult;

                Debug.Log(
                    $"Pet action completed | " +
                    $"Action: {careResult.ActionType} | " +
                    $"Need: {careResult.TargetNeed} | " +
                    $"Value: " +
                    $"{careResult.PreviousNeedValue:0.0} " +
                    $"-> {careResult.CurrentNeedValue:0.0} | " +
                    $"XP: " +
                    $"{petSession.CurrentPetState.Stats.CurrentExperience} | " +
                    $"Affection: " +
                    $"{petSession.CurrentPetState.Stats.Affection:0.0}");
            }

            return result;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}