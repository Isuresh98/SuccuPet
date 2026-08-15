# SuccuPet — Design and Technical Notes

## 1. Objective

The objective was to create a small but complete real-time creature-care game
that is easy to understand during a short reviewer session. The player must
make repeated care decisions, receive immediate feedback, understand the risk
of neglect, and be able to recover or restart without leaving the application.

The implementation prioritises a reliable end-to-end loop over unrelated
content. The submitted flow includes onboarding, real-time care, progression,
persistence, failure, restart, loading feedback, audio feedback, and an
Editor-only QA tool.

## 2. Core design decisions

### Four readable needs

The pet exposes four needs at all times:

- **Fullness** — restored by Feed.
- **Energy** — connected to Sleep/Wake.
- **Happiness** — restored by Play.
- **Hygiene** — restored by Bathe/Clean.

The values decay over time so the game remains active without requiring a
scripted sequence. Near-full actions are rejected to communicate that care
should be intentional and to prevent repeated reward farming.

### Hidden long-term health

The visible needs describe the immediate condition, while hidden Health
represents long-term care quality. Periodic evaluations improve Health during
consistent care and reduce it during sustained neglect. Reaching zero causes a
care coma rather than instant death, giving the player a recovery opportunity.
If recovery conditions are not maintained, the pet can die and the current run
ends permanently.

This creates three understandable risk levels:

1. Normal care
2. Recoverable coma
3. Permanent Game Over

### Starter choice and lifecycle

The player sees eight egg options. Free starter eggs can be selected; cafe
exclusive options communicate future product scope but remain locked in this
prototype. Confirmation prevents accidental permanent selection. Hatching
changes the growth state from Egg to Bat (Baby).

Growth Points earned from care and training drive the lifecycle:

- Egg → Bat after hatching
- Bat → Teen at the first growth gate
- Teen → Adult at the second growth gate

Health and Teen training contribute to Default/Special evolution outcomes.
This connects daily care quality to long-term progression without requiring a
large content system.

### Failure and restart

Death activates a clear Game Over overlay and reports survived time. Starting a
new pet resets the domain save first and then returns presentation state to egg
selection. This ordering prevents UI from displaying the previous pet while a
new save is being created.

## 3. Technical structure

### Core

Pure pet rules and state objects live in the Core layer. Examples include
`PetState`, `PetNeeds`, `PetHealth`, care policies, decay policies, egg
selection rules, and growth/evolution services. Core logic does not depend on
scene objects.

### Application

Use cases coordinate domain operations and persistence. `PetSession` owns the
active state and publishes events such as state changes, care actions,
evolution, training, and death.

### Infrastructure

`JsonFilePetStateRepository` stores versioned save data. Mapping is separated
from the domain so old schemas can be migrated without putting serialization
concerns into gameplay objects. Writes use a temporary file before replacement
to reduce the chance of corrupting the primary save.

### Bootstrap

`GameEntryPoint` creates repositories and use cases, initializes the session,
controls autosave, handles application lifecycle callbacks, and exposes safe
entry points to presentation components.

### Presentation

Presenters subscribe to `PetSession` events and update UI, pet visuals,
animations, tutorial panels, loading state, audio, growth status, and Game Over
presentation. UI is not the source of truth.

### Editor tooling

`SuccuPetQaConsoleWindow` is isolated under an Editor folder. It edits test
saves, applies state presets, invokes live actions, simulates offline time, and
tracks final QA. It is excluded automatically from Web player builds.

## 4. Persistence and elapsed time

The game stores a schema-versioned JSON snapshot through
`Application.persistentDataPath`. Timestamps allow elapsed time to be simulated
when the app resumes. The repository preserves identity, needs, health,
sleep/coma/death state, starter origin, colour seed/rarity, lifecycle progress,
XP, Affection, and Coins.

Persistence is local by design for this prototype. In Web builds, the save is
tied to the current browser and site storage.

## 5. UX and polish

- A full-screen loading overlay appears immediately and remains until
  `GameEntryPoint.IsReady`.
- The loading overlay shows progress, status, a slow-start message, and a smooth
  fade-out.
- A central audio presenter maps domain events to Feed, Play, Bathe, Sleep,
  Wake, hatching, evolution, coma, recovery, rejection, and Game Over SFX.
- UI button click sounds are wired automatically.
- Background music and volume/mute preferences are supported.
- Buttons, status text, stat bars, sprite changes, and overlays provide
  redundant visual feedback.

## 6. Testing strategy

Testing combined normal user flows with deterministic presets:

- Fresh save → egg selection → hatching → care
- Successful and rejected actions
- Sleep persistence
- Offline decay
- Evolution gates and training
- Coma and recovery
- Dead save → Game Over → new pet → egg selection
- Save/reload in Editor and Web build
- Startup loading and audio feedback

The QA Console makes otherwise slow health, coma, death, and evolution tests
repeatable within an assessment timeframe.

## 7. Scope decisions and known limitation

The optional tutorial's exact interrupted-step persistence is a known
limitation: closing mid-tutorial can restart the tutorial from the beginning.
Completing or skipping it prevents it from blocking the main game. Time was
prioritised toward the required care loop, save/load, progression, failure,
restart, browser build, loading feedback, and audio polish.

The following systems were intentionally excluded:

- Authentication and cloud accounts
- Backend/admin services
- Real cafe unlock integration
- Shop and monetisation
- Breeding and large pet collections
- Complete School/Gym activities
- Cloud save synchronisation

## 8. AI-assisted workflow disclosure

AI-assisted tools supported brief analysis, architecture discussion,
boilerplate drafting, debugging suggestions, and documentation review. The
candidate selected the final approach, integrated it into Unity, assigned scene
references and assets, tested the implemented flows, and remains responsible
for explaining and maintaining the submitted solution.

## 9. Improvements with more time

- Persist the exact optional tutorial step in the main versioned save schema.
- Add automated Edit Mode tests for decay, migration, coma, and evolution.
- Replace the Teen training test hook with a complete activity screen.
- Add accessibility settings for reduced motion, text scale, music, and SFX.
- Add a branded Web template for the engine-download phase before the Unity
  scene becomes available.
- Add cloud sync only after authentication and conflict rules are defined.

