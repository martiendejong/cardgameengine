# Agent Progress

## 2026-08-25 — task 648
Plan: move the target-select-banner out of GameBoard's scrollable .middle-zone
and render it as a sibling of <ActionPanel> in GamePage's flex column, so it
sits fixed at the bottom of the screen above the end-turn bar without scrolling
away (mirrors how ActionPanel is already pinned). Lift pendingAction/
selectedTargets/chosenAmount state from GameBoard up to GamePage.
