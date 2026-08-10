using System;
using UnityEngine;
using SuccuPet.Core.Pets;

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
            Application.targetFrameRate = 60;
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

            PetDecayResult result = PetNeedsDecayService.Apply(
                petState,
                utcNow,
                PetDecayPolicy.Default);

            Debug.Log(
                $"Pet domain check | " +
                $"Hours: {result.AppliedHours:0.00} | " +
                $"Fullness: {petState.Needs.Fullness:0.0} | " +
                $"Energy: {petState.Needs.Energy:0.0} | " +
                $"Happiness: {petState.Needs.Happiness:0.0} | " +
                $"Hygiene: {petState.Needs.Hygiene:0.0}");
        }
    }
}