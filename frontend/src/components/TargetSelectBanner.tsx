import { AvailableAction } from '../types/game';

interface TargetSelectBannerProps {
  pendingAction: AvailableAction;
  selectedTargets: string[];
  chosenAmount: number;
  onAmountChange: (amount: number) => void;
  onConfirm: () => void;
  onCancel: () => void;
}

export function TargetSelectBanner({
  pendingAction,
  selectedTargets,
  chosenAmount,
  onAmountChange,
  onConfirm,
  onCancel,
}: TargetSelectBannerProps) {
  return (
    <div className="target-select-banner">
      <span>Select target for: <strong>{pendingAction.label}</strong></span>
      <span className="target-count">({selectedTargets.length}/{pendingAction.requiresChoice?.max ?? 1} selected)</span>
      {pendingAction.requiresChoice?.chooseAmount && pendingAction.amountMax != null && (
        <div className="amount-slider">
          <label>Amount: <strong>{chosenAmount}</strong></label>
          <input
            type="range"
            min={pendingAction.requiresChoice.amountMin ?? 1}
            max={pendingAction.amountMax}
            value={chosenAmount}
            onChange={e => onAmountChange(Number(e.target.value))}
          />
        </div>
      )}
      <button
        className="confirm-target-btn"
        disabled={selectedTargets.length < (pendingAction.requiresChoice?.min ?? 1)}
        onClick={onConfirm}
      >
        Confirm
      </button>
      <button className="cancel-target-btn" onClick={onCancel}>
        Cancel
      </button>
    </div>
  );
}
