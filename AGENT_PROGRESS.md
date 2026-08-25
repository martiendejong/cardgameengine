# Agent Progress

## 2026-08-25 — task 648
Done: moved the target-select-banner out of GameBoard's scrollable .middle-zone
into a new TargetSelectBanner component rendered as a sibling of <ActionPanel>
in GamePage's flex column, so it sits fixed at the bottom of the screen above
the end-turn bar without scrolling away (mirrors how ActionPanel is already
pinned). Lifted pendingAction/selectedTargets/chosenAmount state and the
related handlers from GameBoard up to GamePage. PR #2.
Verified: build clean (tsc -b && vite build); live Playwright run against the
real backend+frontend dev servers — triggered a real target-selection action,
confirmed the banner is not inside .board-main, sits above .action-panel in
DOM/visual order, stays fully visible after scrolling the board, wraps
correctly at a 390px mobile width, and Confirm/Cancel still work.
Left: nothing.
