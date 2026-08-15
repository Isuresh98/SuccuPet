using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Bootstrap;

namespace SuccuPet.Presentation.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class StartupLoadingPresenter : MonoBehaviour
    {
        [Header("Loading View")]
        [SerializeField]
        private GameObject loadingRoot;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private Slider progressSlider;

        [SerializeField]
        private TMP_Text statusText;

        [SerializeField]
        private RectTransform spinner;

        [Header("Timing")]
        [Min(0f)]
        [SerializeField]
        private float minimumVisibleSeconds = 1.2f;

        [Min(0.05f)]
        [SerializeField]
        private float fadeDurationSeconds = 0.3f;

        [Min(1f)]
        [SerializeField]
        private float slowStartupWarningSeconds = 15f;

        [Min(0f)]
        [SerializeField]
        private float readyHoldSeconds = 0.15f;

        [Header("Motion")]
        [SerializeField]
        private float spinnerDegreesPerSecond = -180f;

        [Header("Messages")]
        [SerializeField]
        private string loadingMessage = "Preparing your pet...";

        [SerializeField]
        private string readyMessage = "Ready!";

        [SerializeField]
        private string slowStartupMessage =
            "Still loading... Please wait a moment.";

        private Coroutine loadingRoutine;
        private bool isShowing;

        private void Reset()
        {
            loadingRoot = gameObject;
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (loadingRoot == null)
            {
                loadingRoot = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            ShowImmediately();
        }

        private void OnEnable()
        {
            if (loadingRoutine != null)
            {
                StopCoroutine(loadingRoutine);
            }

            loadingRoutine = StartCoroutine(RunStartupLoading());
        }

        private void OnDisable()
        {
            if (loadingRoutine != null)
            {
                StopCoroutine(loadingRoutine);
                loadingRoutine = null;
            }

            isShowing = false;
        }

        private void Update()
        {
            if (!isShowing || spinner == null)
            {
                return;
            }

            spinner.Rotate(
                0f,
                0f,
                spinnerDegreesPerSecond * Time.unscaledDeltaTime);
        }

        private IEnumerator RunStartupLoading()
        {
            float startedAt = Time.realtimeSinceStartup;
            bool slowWarningShown = false;

            SetStatus(loadingMessage);
            SetProgress(0.08f);

            while (!IsGameReady())
            {
                float elapsed =
                    Time.realtimeSinceStartup - startedAt;

                float simulatedProgress = Mathf.Lerp(
                    0.08f,
                    0.90f,
                    1f - Mathf.Exp(-elapsed * 0.7f));

                SetProgress(simulatedProgress);

                if (!slowWarningShown &&
                    elapsed >= slowStartupWarningSeconds)
                {
                    slowWarningShown = true;
                    SetStatus(slowStartupMessage);

                    Debug.LogWarning(
                        "SuccuPet startup is taking longer than expected. " +
                        "The loading screen will remain visible until " +
                        "GameEntryPoint is ready.",
                        this);
                }

                yield return null;
            }

            while (Time.realtimeSinceStartup - startedAt <
                   minimumVisibleSeconds)
            {
                float elapsed =
                    Time.realtimeSinceStartup - startedAt;

                float normalized = minimumVisibleSeconds <= 0f
                    ? 1f
                    : elapsed / minimumVisibleSeconds;

                SetProgress(Mathf.Lerp(0.90f, 0.98f, normalized));
                yield return null;
            }

            SetProgress(1f);
            SetStatus(readyMessage);

            if (readyHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(readyHoldSeconds);
            }

            yield return FadeOut();

            isShowing = false;
            loadingRoutine = null;

            if (loadingRoot != null)
            {
                loadingRoot.SetActive(false);
            }
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (fadeDurationSeconds <= 0f)
            {
                canvasGroup.alpha = 0f;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < fadeDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(
                    elapsed / fadeDurationSeconds);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        private void ShowImmediately()
        {
            if (loadingRoot != null && !loadingRoot.activeSelf)
            {
                loadingRoot.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            isShowing = true;
            SetStatus(loadingMessage);
            SetProgress(0.08f);
        }

        private static bool IsGameReady()
        {
            GameEntryPoint entryPoint = GameEntryPoint.Instance;
            return entryPoint != null && entryPoint.IsReady;
        }

        private void SetProgress(float normalizedValue)
        {
            if (progressSlider != null)
            {
                progressSlider.SetValueWithoutNotify(
                    Mathf.Clamp01(normalizedValue));
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void OnValidate()
        {
            minimumVisibleSeconds = Mathf.Max(
                0f,
                minimumVisibleSeconds);

            fadeDurationSeconds = Mathf.Max(
                0.05f,
                fadeDurationSeconds);

            slowStartupWarningSeconds = Mathf.Max(
                1f,
                slowStartupWarningSeconds);

            readyHoldSeconds = Mathf.Max(
                0f,
                readyHoldSeconds);
        }
    }
}