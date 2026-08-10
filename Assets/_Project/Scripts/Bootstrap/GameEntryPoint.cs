using System;
using UnityEngine;
using SuccuPet.Core.Pets;
using SuccuPet.Application.Pets;

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

    PetState petState = PetState.CreateNew(
        petId: "mock-pet-001",
        displayName: "Mock Pet",
        utcNow: utcNow.AddHours(-2d));

    PerformPetCareActionUseCase useCase =
        new PerformPetCareActionUseCase(
            PetDecayPolicy.Default);

    PerformPetCareActionResult result =
        useCase.Execute(
            petState,
            PetCareActionType.Feed,
            utcNow);

    PetCareActionResult careResult =
        result.CareResult;

    Debug.Log(
        $"Pet care check | " +
        $"Decay hours: {result.DecayResult.AppliedHours:0.00} | " +
        $"Action: {careResult.ActionType} | " +
        $"Fullness: {careResult.PreviousNeedValue:0.0} " +
        $"-> {careResult.CurrentNeedValue:0.0} | " +
        $"XP: {petState.Stats.CurrentExperience} | " +
        $"Affection: {petState.Stats.Affection:0.0}");
}
    }
}