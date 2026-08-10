using System;
using UnityEngine;
using SuccuPet.Core.Pets;
using SuccuPet.Application.Pets;
using SuccuPet.Infrastructure.Persistence.Pets;

namespace SuccuPet.Bootstrap
{
    public sealed class GameEntryPoint : MonoBehaviour
    {
        [SerializeField]
        private EnvironmentConfig environmentConfig;

        private static GameEntryPoint instance;

        public static GameEntryPoint Instance => instance;

        public EnvironmentConfig EnvironmentConfig => environmentConfig;

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
                    "EnvironmentConfig is not assigned to GameEntryPoint.",
                    this);

                enabled = false;
                return;
            }

            ConfigureApplication();
            InitializeGame();
        }

       private void ConfigureApplication()
{
    QualitySettings.vSyncCount = 0;
    UnityEngine.Application.targetFrameRate = 60;
    Screen.sleepTimeout = SleepTimeout.SystemSetting;
}

        private void InitializeGame()
        {
            if (!environmentConfig.EnableDebugLogs)
            {
                return;
            }

            Debug.Log(
                $"SuccuPet started in " +
                $"{environmentConfig.Environment} environment.");

            if (environmentConfig.Environment == AppEnvironment.Mock)
            {
                RunPetDomainSmokeTest();
            }
        }

    private void RunPetDomainSmokeTest()
{
    DateTime utcNow = DateTime.UtcNow;

    JsonFilePetStateRepository repository =
        new JsonFilePetStateRepository(
            "succupet-smoke-pet-state.json");

    PetState initialState = PetState.CreateNew(
        petId: "mock-pet-001",
        displayName: "Mock Pet",
        utcNow: utcNow.AddHours(-2d));

    repository.Save(initialState);

    LoadOrCreatePetStateUseCase loadUseCase =
        new LoadOrCreatePetStateUseCase(
            repository,
            PetDecayPolicy.Default);

    LoadPetStateResult loadResult =
        loadUseCase.Execute(
            petId: "mock-pet-001",
            displayName: "Mock Pet",
            utcNow: utcNow);

    PerformPetCareActionUseCase careUseCase =
        new PerformPetCareActionUseCase(
            PetDecayPolicy.Default,
            repository);

    PerformPetCareActionResult careResult =
        careUseCase.Execute(
            loadResult.PetState,
            PetCareActionType.Feed,
            utcNow);

    bool loadedAgain =
        repository.TryLoad(out PetState verifiedState);

    Debug.Log(
        $"Pet save check | " +
        $"Loaded: {loadedAgain} | " +
        $"Created: {loadResult.WasCreated} | " +
        $"Offline hours: " +
        $"{loadResult.DecayResult.AppliedHours:0.00} | " +
        $"Fullness: {verifiedState.Needs.Fullness:0.0} | " +
        $"XP: {verifiedState.Stats.CurrentExperience} | " +
        $"Affection: {verifiedState.Stats.Affection:0.0} | " +
        $"File: {repository.FilePath}");
}

    }
}