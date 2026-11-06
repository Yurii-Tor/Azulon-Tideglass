# Local verification

## Automated checks

Open the project in Unity **6000.3.21f1**. Run **Window → General → Test Runner**. In a clean checkout, expect **14 EditMode** tests and **1 PlayMode** test.

| Suite | What a passing run covers |
| --- | --- |
| GameSessionTests (9) | Ownership, tier gating, full sockets, invalid combinations, paired bonuses, ordering, retention and completion. |
| CoastContentTests (5) | Imported textures, reachable catalogue entries, a playable route through the authored assets, the starting scene, UI styles and diagnostics for missing lens references or duplicate IDs. |
| StationPlayModeTests (1) | Loads Station and restores all three beacons through the actual UI controls. It checks an incorrect pair, the prism bonus, newly unlocked salvage and inventory retention after completion. |

On Windows, close the Editor instance using this project, open PowerShell in the project root and run the following. The script writes each suite's NUnit XML and Editor log to `TestResults/` and stops if any suite fails, skips tests or returns no results.

```powershell
.\Tools\Verify.ps1
```

The runner uses the editor version in `ProjectSettings/ProjectVersion.txt`. If your Hub install is in a custom folder, pass the path explicitly.

```powershell
.\Tools\Verify.ps1 -Unity 'D:\Unity\6000.3.21f1\Editor\Unity.exe'
```

## Manual review

1. Start with an empty inventory. All six catalogue entries should expose their name, art, description and base power. Three are initially collectible.
2. Collect Saltglass and Seafoam Wheel. Mount both. The beam should read six against a target of five, and ignition should stay unavailable. Remove the wheel and replace it with a collected Copper Eye. The beam should now show five and the beacon should be ready.
3. Light the first beacon. Both initial lenses remain owned, the sockets clear and Shatter Reef salvage becomes collectible. Mount Seafoam Wheel with Twinwake Prism and check the even-partner bonus.
4. Finish the three-beacon run. Verify the final signal, the completed route and the retained collection. Restart Play Mode and verify a new session.
5. Check a 1280 × 720 Game view and a narrow window. Confirm that text and controls are reachable, that focus and activation work on a keyboard, and that the device's pointer can perform the full route.
6. Add a lens by following the README. Confirm that the catalogue displays the new icon, power, unlock threshold and effect hint.
7. Build the Station scene for the installed desktop target and repeat the first beacon in the actual player. Review both Editor and player logs.

## Verified on Windows

On 2026-11-06, Unity **6000.3.21f1** imported the project and produced these results:

- `Tools/Verify.ps1`: 14 EditMode and 1 PlayMode test passed.
- `-buildWindows64Player`: Windows x64 standalone build succeeded.
- `-runTests -testPlatform StandaloneWindows64`: the full route passed in a standalone Windows test player.

The route test sends UI Toolkit submit events to the running scene's buttons. Use the manual checklist for physical keyboard and mouse input and for visual checks at different window sizes. The walkthrough in this folder is a recording guide; the submission also requires a shareable video recorded in English.
