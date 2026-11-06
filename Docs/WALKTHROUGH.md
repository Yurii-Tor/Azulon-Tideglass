# English video walkthrough

Record the actual Unity Game view or a running build together with the Project window and Inspector. The following route fits a roughly three-minute recording.

| Time | Show | Suggested narration |
| --- | --- | --- |
| 0:00–0:25 | Start the Station scene. Point out the empty inventory and the full catalogue. | “I'm the keeper of three coastal beacons. The catalogue shows every lens along the route, including salvage I can’t reach yet. My inventory contains only the lenses I’ve collected.” |
| 0:25–0:55 | Collect **Saltglass** and **Copper Eye**, mount both from the inventory, then ignite **Dock Lantern**. | “This signal needs five. Saltglass supplies two and the Copper Eye supplies three. Lighting the harbor lamp opens Shatter Reef. I keep both lenses.” |
| 0:55–1:25 | Collect **Seafoam Wheel** and **Twinwake Prism**. Fit them at **Bell Buoy**, then ignite. | “The prism starts at two. When its partner has even base power it gains another two. Paired with the four-power wheel, it reaches eight. I can see the result before committing.” |
| 1:25–1:45 | Collect **Storm Fresnel**. Pair it with the same **Twinwake Prism**. Ignite **Northlight**. | “Here I can reuse the prism with the newly unlocked Fresnel. Six plus two and the resonance bonus give ten. The last signal is back on.” |
| 1:45–2:10 | Stop Play. Show `Station.cs`, `GameSession.cs`, and `Data/Coast.asset`. | “The scene connects a coast asset to a plain C# session and the UI. The session handles ownership, socket limits and ignition. The coast asset sets the route and the available lenses.” |
| 2:10–2:55 | Duplicate a lens in **Data/Lenses**. Change its ID to **signal-seed**, its display name to **Signal Seed**, and **Unlock After** to zero. Create an **Even partner bonus** asset, set its bonus to four, assign it to the new lens, and append that lens to the coast. Start Play and show the new card and its effect hint. | “This new lens uses an existing behavior with a different bonus. New rule types implement the two members of LensEffect. There’s no item switch to extend in the inventory or the view.” |

Make the demonstration edits on a disposable branch or revert them before submission. Azulon requests a public GitHub or GitLab repository and a shareable link to the video, recorded in English.
