# Local verification

## Automated checks

Open `Tideglass` in Unity **6000.3.28f1**. Run **Window → General → Test Runner**. In a clean checkout, expect **12 EditMode** tests and **1 PlayMode** test.

| Suite | What a passing run covers |
| --- | --- |
| GameSessionTests (9) | Ownership, tier gating, full sockets, invalid combinations, paired bonuses, ordering, retention and completion. |
| CoastContentTests (3) | Imported textures, reachable catalogue entries, a playable route through the actual asset data, the starting scene and UI styles. |
| StationPlayModeTests (1) | Loads Station, activates the actual collect, mount and ignition controls, then checks the next beacon and newly unlocked salvage. |

On Windows, close the Editor instance using this project, open PowerShell in the project root and run the following. The script writes each suite's NUnit XML and Editor log to `TestResults/` and stops if any suite fails, skips tests or returns no results.

```powershell
.\Tools\Verify.ps1
```

If your Hub install is in a custom folder, pass the path explicitly.

```powershell
.\Tools\Verify.ps1 -Unity 'D:\Unity\6000.3.28f1\Editor\Unity.exe'
```

## Manual review

1. Start with an empty inventory. All six catalogue entries should expose their name, art, description and base power. Three are initially collectible.
2. Collect Saltglass and Seafoam Wheel. Mount both. The beam should read six against a target of five, and ignition should stay unavailable. Remove the wheel and replace it with a collected Copper Eye. The beam should now show five and the beacon should be ready.
3. Light the first beacon. Both initial lenses remain owned, the sockets clear and Shatter Reef salvage becomes collectible. Mount Seafoam Wheel with Twinwake Prism and check the even-partner bonus.
4. Finish the three-beacon run. Verify the final signal, the completed route and the retained collection. Restart Play Mode and verify a new session.
5. Check a 1280 × 720 Game view and a narrow window. Confirm that text and controls are reachable, that focus and activation work on a keyboard, and that the device's pointer can perform the full route.
6. Add a lens by following the README. Confirm that the catalogue displays the new icon, power, unlock threshold and effect hint.
7. Build the Station scene for the installed desktop target and repeat the first beacon in the actual player. Review both Editor and player logs.

## Verification performed before handoff

The nine pure C# tests ran successfully against Unity's NUnit fork on a .NET host. All five runtime and test assemblies compiled against the corresponding Unity 6000.3.28f1 managed APIs and the Unity Test Framework. Separate static checks resolved the project asset GUIDs, checked all six PNG icons and enumerated the valid lens pairs from the serialized coast data.

The PowerShell runner also passed seven success and failure scenarios using simulated Editor processes on Linux.

The Unity Editor import, on-screen layout, Play Mode execution, platform build and Windows PowerShell invocation still need a local run. The walkthrough in this folder is a recording guide. Supply the video recorded in English and the public repository URL for the actual submission.
