using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SuccuPet.Application.Pets;
using SuccuPet.Bootstrap;
using SuccuPet.Core.Pets;

namespace SuccuPet.Presentation.Pets
{
    public sealed class PetAudioPresenter : MonoBehaviour
    {
        private const string MasterVolumeKey =
            "SuccuPet.Audio.MasterVolume";

        private const string MutedKey =
            "SuccuPet.Audio.Muted";

        [Header("Audio Sources (created automatically if empty)")]
        [SerializeField]
        private AudioSource musicSource;

        [SerializeField]
        private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField]
        private AudioClip backgroundMusic;

        [Range(0f, 1f)]
        [SerializeField]
        private float musicVolume = 0.22f;

        [Header("Interface")]
        [SerializeField]
        private AudioClip uiClickClip;

        [SerializeField]
        private AudioClip rejectedClip;

        [SerializeField]
        private AudioClip successClip;

        [Header("Care Actions")]
        [SerializeField]
        private AudioClip feedClip;

        [SerializeField]
        private AudioClip playClip;

        [SerializeField]
        private AudioClip batheClip;

        [SerializeField]
        private AudioClip sleepClip;

        [SerializeField]
        private AudioClip wakeClip;

        [Header("Lifecycle")]
        [SerializeField]
        private AudioClip eggSelectedClip;

        [SerializeField]
        private AudioClip hatchClip;

        [SerializeField]
        private AudioClip evolutionClip;

        [SerializeField]
        private AudioClip comaClip;

        [SerializeField]
        private AudioClip recoveryClip;

        [SerializeField]
        private AudioClip gameOverClip;

        [SerializeField]
        private AudioClip newPetClip;

        [Header("Polish")]
        [Range(0f, 1f)]
        [SerializeField]
        private float sfxVolume = 0.85f;

        [Range(0.8f, 1f)]
        [SerializeField]
        private float minimumPitch = 0.96f;

        [Range(1f, 1.2f)]
        [SerializeField]
        private float maximumPitch = 1.04f;

        [SerializeField]
        private bool automaticallyWireButtonClicks = true;

        private readonly List<Button> wiredButtons =
            new List<Button>();

        private PetSession petSession;
        private bool isBound;
        private bool hasSnapshot;
        private bool previousSleeping;
        private bool previousComa;
        private bool previousDead;
        private bool previousHasLineage;
        private float masterVolume = 1f;
        private bool isMuted;

        private void Awake()
        {
            EnsureAudioSources();
            LoadAudioPreferences();
            ApplyAudioSettings();
            StartBackgroundMusic();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            TryBind();
            WireAllButtons();
        }

        private void Update()
        {
            if (!isBound)
            {
                TryBind();
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            UnbindSession();
            UnwireAllButtons();
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }

        private void LoadAudioPreferences()
        {
            masterVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, 1f));

            isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
        }

        private void ApplyAudioSettings()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
                musicSource.mute = isMuted;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = masterVolume;
                sfxSource.mute = isMuted;
            }
        }

        private void StartBackgroundMusic()
        {
            if (musicSource == null || backgroundMusic == null)
            {
                return;
            }

            if (musicSource.clip == backgroundMusic && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        private void TryBind()
        {
            GameEntryPoint entryPoint = GameEntryPoint.Instance;

            if (entryPoint == null || !entryPoint.IsReady)
            {
                return;
            }

            if (petSession == entryPoint.PetSession && isBound)
            {
                return;
            }

            UnbindSession();
            petSession = entryPoint.PetSession;
            petSession.StateChanged += HandleStateChanged;
            petSession.CareActionPerformed += HandleCareActionPerformed;
            petSession.PetEvolved += HandlePetEvolved;
            petSession.TrainingPerformed += HandleTrainingPerformed;
            petSession.PetDied += HandlePetDied;
            isBound = true;

            CaptureSnapshot(petSession.CurrentPetState);

            if (previousDead)
            {
                PlayClip(gameOverClip);
            }
        }

        private void UnbindSession()
        {
            if (petSession != null && isBound)
            {
                petSession.StateChanged -= HandleStateChanged;
                petSession.CareActionPerformed -= HandleCareActionPerformed;
                petSession.PetEvolved -= HandlePetEvolved;
                petSession.TrainingPerformed -= HandleTrainingPerformed;
                petSession.PetDied -= HandlePetDied;
            }

            petSession = null;
            isBound = false;
            hasSnapshot = false;
        }

        private void HandleCareActionPerformed(
            PerformPetCareActionResult result)
        {
            if (!result.IsSuccessful)
            {
                PlayClip(rejectedClip);
                return;
            }

            switch (result.CareResult.ActionType)
            {
                case PetCareActionType.Feed:
                    PlayClip(feedClip != null ? feedClip : successClip);
                    break;

                case PetCareActionType.Play:
                    PlayClip(playClip != null ? playClip : successClip);
                    break;

                case PetCareActionType.Bathe:
                    PlayClip(batheClip != null ? batheClip : successClip);
                    break;

                default:
                    PlayClip(successClip);
                    break;
            }
        }

        private void HandlePetEvolved(PetEvolutionResult result)
        {
            if (!result.IsSuccessful)
            {
                PlayClip(rejectedClip);
                return;
            }

            PlayClip(
                result.Gate == PetEvolutionGate.Hatching
                    ? hatchClip
                    : evolutionClip);
        }

        private void HandleTrainingPerformed(PetTrainingResult result)
        {
            PlayClip(result.IsSuccessful ? successClip : rejectedClip);
        }

        private void HandlePetDied(PetState petState)
        {
            previousDead = true;
            PlayClip(gameOverClip);
        }

        private void HandleStateChanged(PetState petState)
        {
            if (petState == null)
            {
                return;
            }

            if (!hasSnapshot)
            {
                CaptureSnapshot(petState);
                return;
            }

            bool currentHasLineage =
                petState.Origin.HasSelectedLineage;

            bool startedNewPet =
                previousHasLineage && !currentHasLineage;

            if (startedNewPet)
            {
                PlayClip(newPetClip);
            }
            else
            {
                if (!previousHasLineage && currentHasLineage)
                {
                    PlayClip(eggSelectedClip);
                }

                if (previousSleeping != petState.IsSleeping)
                {
                    PlayClip(
                        petState.IsSleeping
                            ? sleepClip
                            : wakeClip);
                }

                if (!previousComa && petState.IsInComa)
                {
                    PlayClip(comaClip);
                }
                else if (previousComa &&
                         !petState.IsInComa &&
                         !petState.IsDead)
                {
                    PlayClip(recoveryClip);
                }
            }

            CaptureSnapshot(petState);
        }

        private void CaptureSnapshot(PetState petState)
        {
            if (petState == null)
            {
                hasSnapshot = false;
                return;
            }

            previousSleeping = petState.IsSleeping;
            previousComa = petState.IsInComa;
            previousDead = petState.IsDead;
            previousHasLineage = petState.Origin.HasSelectedLineage;
            hasSnapshot = true;
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null || sfxSource == null || isMuted)
            {
                return;
            }

            sfxSource.pitch = Random.Range(minimumPitch, maximumPitch);
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void PlayUiClick()
        {
            PlayClip(uiClickClip);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        public void SetMuted(bool shouldMute)
        {
            isMuted = shouldMute;
            PlayerPrefs.SetInt(MutedKey, isMuted ? 1 : 0);
            PlayerPrefs.Save();
            ApplyAudioSettings();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            WireAllButtons();
        }

        private void WireAllButtons()
        {
            if (!automaticallyWireButtonClicks)
            {
                return;
            }

            UnwireAllButtons();

            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int index = 0; index < buttons.Length; index++)
            {
                Button button = buttons[index];

                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveListener(PlayUiClick);
                button.onClick.AddListener(PlayUiClick);
                wiredButtons.Add(button);
            }
        }

        private void UnwireAllButtons()
        {
            for (int index = 0; index < wiredButtons.Count; index++)
            {
                Button button = wiredButtons[index];

                if (button != null)
                {
                    button.onClick.RemoveListener(PlayUiClick);
                }
            }

            wiredButtons.Clear();
        }

        private void OnValidate()
        {
            minimumPitch = Mathf.Clamp(minimumPitch, 0.8f, 1f);
            maximumPitch = Mathf.Clamp(maximumPitch, 1f, 1.2f);

            if (maximumPitch < minimumPitch)
            {
                maximumPitch = minimumPitch;
            }

            ApplyAudioSettings();
        }
        
    }
}