using UnityEngine;

namespace SuccuPet.Bootstrap
{
    [CreateAssetMenu(
        fileName = "EnvironmentConfig",
        menuName = "SuccuPet/Configuration/Environment Config")]
    public sealed class EnvironmentConfig : ScriptableObject
    {
        [Header("Environment")]
        [SerializeField]
        private AppEnvironment environment = AppEnvironment.Mock;

        [Header("Server")]
        [SerializeField]
        private string apiBaseUrl = string.Empty;

        [SerializeField]
        private bool useMockServices = true;

        [Header("Development")]
        [SerializeField]
        private bool enableDebugLogs = true;

        public AppEnvironment Environment => environment;
        public string ApiBaseUrl => apiBaseUrl;
        public bool UseMockServices => useMockServices;
        public bool EnableDebugLogs => enableDebugLogs;
    }
}