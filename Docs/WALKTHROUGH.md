# English video walkthrough

Start the recording with the running game. The first minute demonstrates the three-slot bag, two lamp sockets and how an unmounted lens is carried forward.

| Time | Show | Suggested narration |
| --- | --- | --- |
| 0:00–0:15 | Show Dock Lantern, the empty bag and catalogue unlock hints. | “A storm has darkened three coastal beacons. The catalogue shows all the lenses and tells me which beacon unlocks each one.” |
| 0:15–0:25 | Collect **Saltglass**, **Copper Eye** and **Seafoam Wheel**. Point to the bag at **3 / 3**, the empty sockets and the **0 / 5** beam. | “Collecting puts a lens in my three-slot bag. The beam stays at zero until I mount lenses in the lamp.” |
| 0:25–0:42 | Mount Saltglass and Seafoam Wheel for **6 / 5**. Unmount the wheel, then mount Copper Eye for **5 / 5**. | “The first pair is one over target. Unmounting the wheel keeps it in my bag. Saltglass and Copper Eye match the target.” |
| 0:42–0:55 | Light Dock Lantern. Point to **Seafoam Wheel** remaining in the **1 / 3** bag and to the spent pair. | “Lighting a beacon spends only the two mounted lenses. The wheel is still in my bag for the next stop.” |
| 0:55–1:15 | Collect Twinwake Prism and mount it with the wheel. Show the **8 / 8** beam and light Bell Buoy. | “The prism gains two power with an even-power partner. The wheel and prism make eight together.” |
| 1:15–1:35 | Collect Storm Fresnel and Fog Pearl, mount them for **10 / 10** and light Northlight. | “The Fresnel supplies six, the pearl supplies one, and its even-partner bonus adds three. The coast is alight.” |
| 1:35–2:00 | Stop Play. Show `GameSession.cs`. | “The session is plain C#. It owns the bag and lamp sockets, checks exact targets and consumes the mounted pair.” |
| 2:00–2:20 | Show `Station.cs`, its `UIDocument`, and `Data/Coast.asset`. | “The Station connects the authored coast to the session and UI Toolkit. The coast asset lists the lenses and beacon targets.” |
| 2:20–3:00 | In a **separate disposable copy**, duplicate a lens in `Data/Lenses`. Give it a unique ID, name, icon and power. Assign an existing effect or create an Even Partner Bonus asset, then append the lens to the copy's Coast asset. Enter Play to show its card. | “A lens is a ScriptableObject. I can add one with an existing effect without editing the inventory or UI. New effect types implement LensEffect.” |

The base game contains eight lenses. Run the content tests in the disposable copy when changing a route or adding a lens. Azulon requests a shareable English video and a public repository URL for submission.
