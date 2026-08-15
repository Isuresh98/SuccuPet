# SuccuPet — Final QA and Submission Checklist

Use a checked copy of this file as the final release record.

## Editor health

- [ ] Unity opens with the version in `ProjectVersion.txt`.
- [ ] Console has zero red errors.
- [ ] Bootstrap scene and all modified prefabs/scenes are saved.
- [ ] No `Missing Script` components exist.
- [ ] Required Inspector references are assigned.
- [ ] Startup LoadingPanel is active and is the Canvas's last sibling.
- [ ] `PetAudioPresenter` has the intended clips assigned.

## Fresh-player flow

- [ ] Delete/reset the save and press Play.
- [ ] Loading screen appears immediately and blocks underlying input.
- [ ] Loading reaches `Ready!` and fades smoothly.
- [ ] Eight starter eggs are visible.
- [ ] Locked and eligible egg states are clear.
- [ ] Confirmation Back and Confirm buttons work.
- [ ] Hatching flow completes and opens Pet Care.
- [ ] Tutorial starts immediately after hatching.
- [ ] Tutorial can be completed or skipped.

## Core care loop

- [ ] Fullness, Energy, Happiness, and Hygiene are always readable.
- [ ] Needs decrease with elapsed time.
- [ ] Feed restores Fullness.
- [ ] Play restores Happiness.
- [ ] Bathe/Clean restores Hygiene.
- [ ] Sleep and Wake change state correctly.
- [ ] Sleeping appearance/status is visible.
- [ ] Near-full care actions are rejected with feedback.
- [ ] Cooldown prevents button spam.
- [ ] Successful actions award XP, Affection, and Growth.
- [ ] Buttons and feedback remain readable at mobile resolution.

## Progression

- [ ] Hatching changes Egg to Bat (Baby).
- [ ] Bat reaches the first evolution threshold.
- [ ] Bat → Teen evolution works.
- [ ] Teen training increments correctly.
- [ ] Teen reaches the second evolution threshold.
- [ ] Teen → Adult evolution works.
- [ ] Default/Special variant rules behave as designed.
- [ ] Stage, growth, training, and variant persist after restart.

## Health, death, and restart

- [ ] Low-needs preset produces expected warnings/health behaviour.
- [ ] Coma state is clearly visible and disables inappropriate actions.
- [ ] Healthy recovery conditions progress correctly.
- [ ] Recovery exits coma correctly.
- [ ] Dead preset opens Game Over.
- [ ] Game Over shows survived time.
- [ ] `Start New Pet` clears the failed run.
- [ ] Restart returns to starter egg selection without a blank screen.

## Persistence

- [ ] Care changes survive Editor Stop/Play.
- [ ] Sleeping state survives restart.
- [ ] Starter lineage survives restart.
- [ ] Growth and progression survive restart.
- [ ] Offline elapsed time is simulated.
- [ ] Invalid/old save migration does not create red errors.
- [ ] Browser refresh preserves the Web save.
- [ ] Clearing browser site data produces a fresh save.

## Audio

- [ ] UI click sound is subtle and consistent.
- [ ] Feed, Play, Bathe, Sleep, and Wake sounds trigger once.
- [ ] Rejected action sound is distinguishable but not harsh.
- [ ] Hatching and evolution sounds trigger once.
- [ ] Coma, recovery, and Game Over sounds are appropriate.
- [ ] Background music is lower than SFX.
- [ ] No clip distorts or clips at maximum expected volume.
- [ ] Audio starts after browser interaction if autoplay is initially restricted.

## Web release build

- [ ] Bootstrap is included and first in the Build Profile Scene List.
- [ ] Development Build is disabled.
- [ ] Compression is Gzip.
- [ ] Decompression Fallback is enabled.
- [ ] Code Optimization is Runtime Speed.
- [ ] Strip Engine Code is enabled.
- [ ] Build completes without errors.
- [ ] Build launches through a local server or hosted URL.
- [ ] Browser Console has no critical errors.
- [ ] Loading, gameplay, audio, save, Game Over, and restart work in the build.
- [ ] Layout is checked on desktop and a tall mobile viewport.

## Repository

- [ ] `.gitignore` exists before the final add/commit.
- [ ] `Assets` and all Unity `.meta` files are committed.
- [ ] `Packages` and `ProjectSettings` are committed.
- [ ] `Library`, `Temp`, `Logs`, `obj`, `UserSettings`, and local builds are absent.
- [ ] README placeholders are replaced.
- [ ] Third-party asset names, URLs, and licences are complete.
- [ ] Commit history contains clear milestone messages.
- [ ] Repository opens from a clean clone.
- [ ] Private repository reviewer access is confirmed, or the repository is public.
- [ ] `v1.0-assessment` tag/release is created.

## Final delivery

- [ ] Playable Web URL opens without authentication problems.
- [ ] GitHub URL opens for the reviewer.
- [ ] Source/build ZIP opens successfully if attached.
- [ ] README, design notes, QA checklist, and third-party notices are included.
- [ ] Known tutorial limitation is disclosed.
- [ ] Candidate can explain architecture, decay, save, coma, death, and restart.
- [ ] Submission email contains both links and brief test instructions.

