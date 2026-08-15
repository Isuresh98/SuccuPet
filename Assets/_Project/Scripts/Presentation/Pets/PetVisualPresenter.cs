using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetVisualPresenter : MonoBehaviour
    {
        private enum VisualState
        {
            Calm,
            Happy,
            Hungry,
            Tired,
            Sad,
            Dirty,
            Sleeping,
            Sick,
            Coma
        }

        [Header("UI")]
        [SerializeField]
        private Image petImage;

        [SerializeField]
        private TMP_Text petStateText;

        [Header("Optional State Sprites")]
        [SerializeField]
        private Sprite normalSprite;

        [SerializeField]
        private Sprite happySprite;

        [SerializeField]
        private Sprite needsCareSprite;

        [SerializeField]
        private Sprite sleepingSprite;

        [SerializeField]
        private Sprite sickSprite;

        [Header("State Colors")]
        [SerializeField]
        private Color normalColor = Color.white;

        [SerializeField]
        private Color happyColor =
            new Color(0.55f, 1f, 0.65f, 1f);

        [SerializeField]
        private Color warningColor =
            new Color(1f, 0.72f, 0.32f, 1f);

        [SerializeField]
        private Color sleepingColor =
            new Color(0.55f, 0.75f, 1f, 1f);

        [SerializeField]
        private Color sickColor =
            new Color(1f, 0.45f, 0.45f, 1f);

        [SerializeField]
        private Color comaColor =
            new Color(0.45f, 0.45f, 0.5f, 1f);

        private PetSession petSession;

        private void Start()
        {
            GameEntryPoint entryPoint = GameEntryPoint.Instance;

            if (entryPoint == null || !entryPoint.IsReady)
            {
                Debug.LogError(
                    "GameEntryPoint is not ready for pet visuals.",
                    this);

                enabled = false;
                return;
            }

            petSession = entryPoint.PetSession;
            petSession.StateChanged += Refresh;

            Refresh(petSession.CurrentPetState);
        }

        private void Refresh(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            VisualState visualState =
                ResolveVisualState(petState);

            ApplyVisualState(visualState);
        }

        private static VisualState ResolveVisualState(
            PetState petState)
        {
            if (petState.IsInComa)
            {
                return VisualState.Coma;
            }

            if (petState.IsSleeping)
            {
                return VisualState.Sleeping;
            }

            if (petState.Health.Status ==
                    PetHealthStatus.Critical ||
                petState.Health.Status ==
                    PetHealthStatus.Fatigued)
            {
                return VisualState.Sick;
            }

            PetNeeds needs = petState.Needs;

            float lowestNeed = Mathf.Min(
                needs.Vitality,
                needs.Rest,
                needs.Mood,
                needs.Allure);

            if (lowestNeed < 30f)
            {
                if (Mathf.Approximately(
                        lowestNeed,
                        needs.Vitality))
                {
                    return VisualState.Hungry;
                }

                if (Mathf.Approximately(
                        lowestNeed,
                        needs.Rest))
                {
                    return VisualState.Tired;
                }

                if (Mathf.Approximately(
                        lowestNeed,
                        needs.Mood))
                {
                    return VisualState.Sad;
                }

                return VisualState.Dirty;
            }

            if (needs.Vitality >= 80f &&
                needs.Rest >= 80f &&
                needs.Mood >= 80f &&
                needs.Allure >= 80f)
            {
                return VisualState.Happy;
            }

            return VisualState.Calm;
        }

        private void ApplyVisualState(
            VisualState visualState)
        {
            Sprite targetSprite = normalSprite;
            Color targetColor = normalColor;
            string stateLabel = "CALM";

            switch (visualState)
            {
                case VisualState.Happy:
                    targetSprite = GetSpriteOrFallback(
                        happySprite);
                    targetColor = happyColor;
                    stateLabel = "HAPPY";
                    break;

                case VisualState.Hungry:
                    targetSprite = GetSpriteOrFallback(
                        needsCareSprite);
                    targetColor = warningColor;
                    stateLabel = "HUNGRY";
                    break;

                case VisualState.Tired:
                    targetSprite = GetSpriteOrFallback(
                        needsCareSprite);
                    targetColor = warningColor;
                    stateLabel = "TIRED";
                    break;

                case VisualState.Sad:
                    targetSprite = GetSpriteOrFallback(
                        needsCareSprite);
                    targetColor = warningColor;
                    stateLabel = "SAD";
                    break;

                case VisualState.Dirty:
                    targetSprite = GetSpriteOrFallback(
                        needsCareSprite);
                    targetColor = warningColor;
                    stateLabel = "NEEDS A BATH";
                    break;

                case VisualState.Sleeping:
                    targetSprite = GetSpriteOrFallback(
                        sleepingSprite);
                    targetColor = sleepingColor;
                    stateLabel = "SLEEPING";
                    break;

                case VisualState.Sick:
                    targetSprite = GetSpriteOrFallback(
                        sickSprite);
                    targetColor = sickColor;
                    stateLabel = "SICK";
                    break;

                case VisualState.Coma:
                    targetSprite = GetSpriteOrFallback(
                        sickSprite);
                    targetColor = comaColor;
                    stateLabel = "COMA - RECOVERING";
                    break;
            }

            if (petImage != null)
            {
                if (targetSprite != null)
                {
                    petImage.sprite = targetSprite;
                }

                petImage.color = targetColor;
                petImage.preserveAspect = true;
            }

            if (petStateText != null)
            {
                petStateText.text = stateLabel;
            }
        }

        private Sprite GetSpriteOrFallback(Sprite sprite)
        {
            return sprite != null
                ? sprite
                : normalSprite;
        }

        private void OnDestroy()
        {
            if (petSession != null)
            {
                petSession.StateChanged -= Refresh;
            }
        }
    }
}