# Margins First-Store Owner Playable-Loop Checklist v0.1

Test the Windows x64 development build produced from implementation commit
`96d7b6c0039f55cd3934a6ecf84322966f3550dd`. Record the first failed step and
stop if state conservation is visibly wrong. `FSV-003` remains open until this
checklist is completed.

1. **WASD and mouse look:** move in every direction and confirm mouse input changes body yaw and camera pitch without losing control.
2. **`Tab` HUD switching:** enter and leave the development HUD; confirm cursor/input ownership switches and the HUD cannot trigger world interactions.
3. **Delivery opening and exact product removal:** target the sealed container, open it with `E`, then target each configured product and confirm each `E` removes exactly that product and decrements its shown remainder once.
4. **Exact-target pickup:** aim at one loose product, press `E`, and confirm only that physical unit becomes held.
5. **Mouse-wheel product rotation:** rotate the held product in both directions and confirm each wheel step changes one quarter turn.
6. **Exact-target shelf stocking:** target a compatible snap point and stock with `E`; confirm the chosen point and held orientation are used, then confirm an invalid shelf keeps the same single held unit.
7. **Fixture place, move, rotate, cancel, remove, and re-place:** use explicit fixture handles; confirm valid/invalid previews, quarter-turn rotation, exact cancel restoration, `Backspace` removal, and later re-placement.
8. **Staged checkout scan, correction, and completion:** begin at the checkout, scan the presented sequence with `E`, remove the most recent correctable scan with `Q`, rescan, complete once, and confirm replay does not consume stock or revenue again.
9. **Cleaning:** target the cleaning task and apply one `E` progress unit at a time; confirm named progress, completion, and already-complete feedback.
10. **Opening, closing, and result:** use the world operating control through preparation, open, closing, result finalization, and acknowledgement; confirm the first actionable blocker and the displayed gross, COGS, expense, contribution, unit, and transaction totals.
11. **`F5` save, exit, relaunch, `F9` load, and comparison:** save only while not holding a product and without an incomplete checkout; exit cleanly, relaunch once, load, and compare fixture layout, delivery, loose/held/shelf inventory, physical units, ledger, cleaning, operating result, and player position/yaw/pitch. Confirm prompts, previews, HUD mode, and objective text were not restored.

Do not merge PRs #17-#20 until failures are assigned to an owning branch and all
blocker or major defects are corrected and rerun.
