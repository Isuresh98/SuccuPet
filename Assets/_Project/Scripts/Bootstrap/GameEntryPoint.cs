using UnityEngine;

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
            UnityEngine.Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
        }
    }
}