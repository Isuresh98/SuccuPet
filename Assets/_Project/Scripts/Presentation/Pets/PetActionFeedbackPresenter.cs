using System.Collections;
using UnityEngine;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetActionFeedbackPresenter : MonoBehaviour
    {
        [Header("Pet Transform")]
        [SerializeField]
        private RectTransform petTransform;

        [Header("Animation")]
        [Min(0.1f)]
        [SerializeField]
        private float animationDuration = 0.4f;

        [Min(0f)]
        [SerializeField]
        private float feedPulseAmount = 0.18f;

        [Min(0f)]
        [SerializeField]
        private float playRotationAmount = 10f;

        [Min(0f)]
        [SerializeField]
        private float batheJumpAmount = 18f;

        [Min(0f)]
        [SerializeField]
        private float rejectedShakeAmount = 8f;

        private PetSession petSession;
        private Coroutine feedbackCoroutine;

        private Vector3 defaultScale;
        private Quaternion defaultRotation;
        private Vector2 defaultPosition;

        private bool hasCapturedDefaults;


        private void Start()
        {
            GameEntryPoint entryPoint = GameEntryPoint.Instance;

            if (entryPoint == null || !entryPoint.IsReady)
            {
                Debug.LogError(
                    "GameEntryPoint is not ready for action feedback.",
                    this);

                enabled = false;
                return;
            }

            if (petTransform == null)
            {
                Debug.LogError(
                    "Pet Transform is not assigned.",
                    this);

                enabled = false;
                return;
            }

           CaptureTransformDefaults();

            petSession = entryPoint.PetSession;
            petSession.CareActionPerformed +=
                HandleCareActionPerformed;
        }

        private void Awake()
{
    CaptureTransformDefaults();
}

private void CaptureTransformDefaults()
{
    if (hasCapturedDefaults ||
        petTransform == null)
    {
        return;
    }

    defaultScale = petTransform.localScale;

    if (defaultScale.sqrMagnitude < 0.0001f)
    {
        defaultScale = Vector3.one;
        petTransform.localScale = defaultScale;
    }

    defaultRotation = petTransform.localRotation;
    defaultPosition = petTransform.anchoredPosition;
    hasCapturedDefaults = true;
}


        private void HandleCareActionPerformed(
            PerformPetCareActionResult result)
        {
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }

            feedbackCoroutine = StartCoroutine(
                AnimateAction(
                    result.CareResult.ActionType,
                    result.CareResult.IsSuccessful));
        }

        private IEnumerator AnimateAction(
            PetCareActionType actionType,
            bool isSuccessful)
        {
            ResetTransform();

            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                float progress = Mathf.Clamp01(
                    elapsed / animationDuration);

                if (!isSuccessful)
                {
                    AnimateRejectedAction(progress);
                }
                else
                {
                    switch (actionType)
                    {
                        case PetCareActionType.Feed:
                            AnimateFeed(progress);
                            break;

                        case PetCareActionType.Play:
                            AnimatePlay(progress);
                            break;

                        case PetCareActionType.Bathe:
                            AnimateBathe(progress);
                            break;
                    }
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ResetTransform();
            feedbackCoroutine = null;
        }

        private void AnimateFeed(float progress)
        {
            float pulse = Mathf.Sin(progress * Mathf.PI);

            petTransform.localScale =
                defaultScale *
                (1f + pulse * feedPulseAmount);
        }

        private void AnimatePlay(float progress)
        {
            float rotation =
                Mathf.Sin(progress * Mathf.PI * 4f) *
                playRotationAmount *
                (1f - progress);

            petTransform.localRotation =
                defaultRotation *
                Quaternion.Euler(0f, 0f, rotation);
        }

        private void AnimateBathe(float progress)
        {
            float height =
                Mathf.Sin(progress * Mathf.PI) *
                batheJumpAmount;

            petTransform.anchoredPosition =
                defaultPosition +
                Vector2.up * height;
        }

        private void AnimateRejectedAction(float progress)
        {
            float shake =
                Mathf.Sin(progress * Mathf.PI * 6f) *
                rejectedShakeAmount *
                (1f - progress);

            petTransform.anchoredPosition =
                defaultPosition +
                Vector2.right * shake;
        }

       private void ResetTransform()
{
    if (petTransform == null ||
        !hasCapturedDefaults)
    {
        return;
    }

    petTransform.localScale = defaultScale;
    petTransform.localRotation = defaultRotation;
    petTransform.anchoredPosition = defaultPosition;
}

       private void OnDisable()
{
    if (feedbackCoroutine != null)
    {
        StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = null;
    }

    if (hasCapturedDefaults)
    {
        ResetTransform();
    }
}

        private void OnDestroy()
        {
            if (petSession != null)
            {
                petSession.CareActionPerformed -=
                    HandleCareActionPerformed;
            }
        }
    }
}