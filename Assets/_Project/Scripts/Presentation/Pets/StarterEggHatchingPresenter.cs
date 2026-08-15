using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class StarterEggHatchingPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class LineageHatchView
        {
            [SerializeField]
            private string lineageId;

            [SerializeField]
            private Sprite eggSprite;

            [SerializeField]
            private Sprite petSprite;

            public string LineageId => lineageId;
            public Sprite EggSprite => eggSprite;
            public Sprite PetSprite => petSprite;
        }

        [Header("Next Screen")]
        [SerializeField]
        private GameObject petCarePanel;

        [Header("Hatching Text")]
        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text petNameText;

        [SerializeField]
        private TMP_Text welcomeText;

        [Header("Hatching Visuals")]
        [SerializeField]
        private Image eggImage;

        [SerializeField]
        private Image petImage;

        [SerializeField]
        private Image glowImage;

        [SerializeField]
        private Image flashImage;

        [SerializeField]
        private RectTransform eggTransform;

        [SerializeField]
        private RectTransform petTransform;

        [SerializeField]
        private CanvasGroup petCanvasGroup;

        [SerializeField]
        private CanvasGroup resultCanvasGroup;

        [Header("Continue")]
        [SerializeField]
        private Button continueButton;

        [Header("Eight Lineage Images")]
        [SerializeField]
        private LineageHatchView[] lineageViews;

        [Header("Animation Timing")]
        [Min(0f)]
        [SerializeField]
        private float introDelay = 0.35f;

        [Min(0.1f)]
        [SerializeField]
        private float shakeDuration = 1.8f;

        [Min(0.1f)]
        [SerializeField]
        private float flashDuration = 0.30f;

        [Min(0.1f)]
        [SerializeField]
        private float revealDuration = 0.65f;

        [Header("Shake Settings")]
        [Min(0f)]
        [SerializeField]
        private float maximumShakeDistance = 24f;

        [Min(0f)]
        [SerializeField]
        private float shakeSpeed = 42f;

        [Min(0f)]
        [SerializeField]
        private float maximumEggRotation = 12f;

        private Coroutine hatchCoroutine;
        private Vector2 eggRestPosition;
        private bool canContinue;

        private void Awake()
        {
            if (eggTransform == null && eggImage != null)
            {
                eggTransform = eggImage.rectTransform;
            }

            if (petTransform == null && petImage != null)
            {
                petTransform = petImage.rectTransform;
            }

            if (eggTransform != null)
            {
                eggRestPosition = eggTransform.anchoredPosition;
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(
                    HandleContinueClicked);
            }
        }

        public bool Play(string lineageId)
        {
            if (string.IsNullOrWhiteSpace(lineageId))
            {
                Debug.LogError(
                    "Cannot hatch an egg without a lineage ID.",
                    this);
                return false;
            }

            LineageHatchView hatchView =
                FindLineageView(lineageId);

            if (hatchView == null)
            {
                Debug.LogError(
                    $"No hatching images are assigned for lineage: " +
                    $"{lineageId}",
                    this);
                return false;
            }

            if (hatchView.EggSprite == null ||
                hatchView.PetSprite == null)
            {
                Debug.LogError(
                    $"Egg or pet sprite is missing for lineage: " +
                    $"{lineageId}",
                    this);
                return false;
            }

            string displayName = lineageId;

            if (PetLineageCatalog.TryGet(
                    lineageId,
                    out PetLineageDefinition lineage))
            {
                displayName = lineage.DisplayName;
            }

            gameObject.SetActive(true);

            if (petCarePanel != null)
            {
                petCarePanel.SetActive(false);
            }

            if (hatchCoroutine != null)
            {
                StopCoroutine(hatchCoroutine);
            }

            PrepareSequence(hatchView, displayName);

            hatchCoroutine =
                StartCoroutine(HatchSequence());

            return true;
        }

        private void PrepareSequence(
            LineageHatchView hatchView,
            string displayName)
        {
            canContinue = false;

            SetText(
                titleText,
                "A NEW LIFE IS AWAKENING...");

            SetText(
                petNameText,
                displayName.ToUpperInvariant());

            SetText(
                welcomeText,
                $"{displayName} has hatched!\n" +
                "Take good care of your new companion.");

            if (eggImage != null)
            {
                eggImage.sprite = hatchView.EggSprite;
                eggImage.preserveAspect = true;
                eggImage.gameObject.SetActive(true);
            }

            if (petImage != null)
            {
                petImage.sprite = hatchView.PetSprite;
                petImage.preserveAspect = true;
                petImage.gameObject.SetActive(true);
            }

            if (eggTransform != null)
            {
                eggTransform.anchoredPosition = eggRestPosition;
                eggTransform.localRotation = Quaternion.identity;
                eggTransform.localScale = Vector3.one;
            }

            if (petTransform != null)
            {
                petTransform.localRotation = Quaternion.identity;
                petTransform.localScale = Vector3.one * 0.20f;
            }

            SetCanvasGroupAlpha(petCanvasGroup, 0f);
            SetCanvasGroupAlpha(resultCanvasGroup, 0f);
            SetImageAlpha(glowImage, 0f);
            SetImageAlpha(flashImage, 0f);

            if (continueButton != null)
            {
                continueButton.interactable = false;
                continueButton.gameObject.SetActive(false);
            }
        }

        private IEnumerator HatchSequence()
        {
            if (introDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    introDelay);
            }

            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / shakeDuration);

                float strength =
                    Mathf.SmoothStep(0.15f, 1f, progress);

                float wave =
                    Mathf.Sin(elapsed * shakeSpeed);

                if (eggTransform != null)
                {
                    float horizontalOffset =
                        wave * maximumShakeDistance * strength;

                    float verticalOffset =
                        Mathf.Abs(Mathf.Sin(
                            elapsed * shakeSpeed * 0.5f)) *
                        maximumShakeDistance * 0.12f * strength;

                    eggTransform.anchoredPosition =
                        eggRestPosition +
                        new Vector2(
                            horizontalOffset,
                            verticalOffset);

                    eggTransform.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            wave * maximumEggRotation * strength);
                }

                float glowPulse =
                    0.72f +
                    Mathf.Sin(elapsed * 9f) * 0.12f;

                SetImageAlpha(
                    glowImage,
                    Mathf.Lerp(0.12f, glowPulse, progress));

                if (glowImage != null)
                {
                    float glowScale =
                        Mathf.Lerp(0.85f, 1.12f, progress);

                    glowImage.rectTransform.localScale =
                        Vector3.one * glowScale;
                }

                yield return null;
            }

            ResetEggTransform();

            float halfFlashDuration = flashDuration * 0.5f;

            yield return AnimateImageAlpha(
                flashImage,
                GetImageAlpha(flashImage),
                1f,
                halfFlashDuration);

            if (eggImage != null)
            {
                eggImage.gameObject.SetActive(false);
            }

            SetText(
                titleText,
                "YOUR PET HAS HATCHED!");

            elapsed = 0f;

            while (elapsed < revealDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / revealDuration);

                float eased = Mathf.SmoothStep(
                    0f,
                    1f,
                    progress);

                SetCanvasGroupAlpha(
                    petCanvasGroup,
                    eased);

                SetCanvasGroupAlpha(
                    resultCanvasGroup,
                    Mathf.InverseLerp(
                        0.30f,
                        1f,
                        progress));

                if (petTransform != null)
                {
                    float revealScale;

                    if (progress < 0.78f)
                    {
                        revealScale = Mathf.Lerp(
                            0.20f,
                            1.08f,
                            Mathf.SmoothStep(
                                0f,
                                1f,
                                progress / 0.78f));
                    }
                    else
                    {
                        revealScale = Mathf.Lerp(
                            1.08f,
                            1f,
                            Mathf.InverseLerp(
                                0.78f,
                                1f,
                                progress));
                    }

                    petTransform.localScale =
                        Vector3.one * revealScale;
                }

                SetImageAlpha(
                    flashImage,
                    1f - eased);

                SetImageAlpha(
                    glowImage,
                    Mathf.Lerp(0.80f, 0.32f, eased));

                yield return null;
            }

            SetCanvasGroupAlpha(petCanvasGroup, 1f);
            SetCanvasGroupAlpha(resultCanvasGroup, 1f);
            SetImageAlpha(flashImage, 0f);

            if (petTransform != null)
            {
                petTransform.localScale = Vector3.one;
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = true;
            }

            canContinue = true;
            hatchCoroutine = null;
        }

        private IEnumerator AnimateImageAlpha(
            Image target,
            float from,
            float to,
            float duration)
        {
            if (target == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                SetImageAlpha(target, to);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / duration);

                SetImageAlpha(
                    target,
                    Mathf.Lerp(from, to, progress));

                yield return null;
            }

            SetImageAlpha(target, to);
        }

        private LineageHatchView FindLineageView(
            string lineageId)
        {
            if (lineageViews == null)
            {
                return null;
            }

            for (int index = 0;
                index < lineageViews.Length;
                index++)
            {
                LineageHatchView view =
                    lineageViews[index];

                if (view != null &&
                    string.Equals(
                        view.LineageId,
                        lineageId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return view;
                }
            }

            return null;
        }

        private void HandleContinueClicked()
        {
            if (!canContinue)
            {
                return;
            }

            canContinue = false;

            if (continueButton != null)
            {
                continueButton.interactable = false;
            }

            GameEntryPoint entryPoint =
                GameEntryPoint.Instance;

            if (entryPoint == null || !entryPoint.IsReady)
            {
                ShowHatchingSaveError(
                    "The game is not ready. Please restart and try again.");
                return;
            }

            PetEvolutionResult result =
                entryPoint.CompletePetHatching();

            if (!result.IsSuccessful)
            {
                ShowHatchingSaveError(result.Message);
                return;
            }

            if (petCarePanel != null)
            {
                petCarePanel.SetActive(true);
            }

            gameObject.SetActive(false);
        }

        private void ShowHatchingSaveError(string message)
        {
            canContinue = true;

            SetText(titleText, "COULD NOT CONTINUE");
            SetText(welcomeText, message);

            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }

        private void ResetEggTransform()
        {
            if (eggTransform == null)
            {
                return;
            }

            eggTransform.anchoredPosition = eggRestPosition;
            eggTransform.localRotation = Quaternion.identity;
            eggTransform.localScale = Vector3.one;
        }

        private static void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetCanvasGroupAlpha(
            CanvasGroup target,
            float alpha)
        {
            if (target == null)
            {
                return;
            }

            target.alpha = Mathf.Clamp01(alpha);
            target.interactable = alpha >= 0.99f;
            target.blocksRaycasts = alpha >= 0.99f;
        }

        private static float GetImageAlpha(Image target)
        {
            return target != null
                ? target.color.a
                : 0f;
        }

        private static void SetImageAlpha(
            Image target,
            float alpha)
        {
            if (target == null)
            {
                return;
            }

            Color color = target.color;
            color.a = Mathf.Clamp01(alpha);
            target.color = color;
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(
                    HandleContinueClicked);
            }
        }
    }
}
