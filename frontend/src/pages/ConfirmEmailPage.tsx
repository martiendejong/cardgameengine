import { useEffect, useState } from 'react';

interface ConfirmEmailPageProps {
  userId: string;
  token: string;
  onConfirm: (userId: string, token: string) => Promise<unknown>;
  onDone: () => void;
}

export function ConfirmEmailPage({ userId, token, onConfirm, onDone }: ConfirmEmailPageProps) {
  const [status, setStatus] = useState<'checking' | 'ok' | 'error'>('checking');
  const [message, setMessage] = useState('Confirming your email...');

  useEffect(() => {
    let cancelled = false;
    onConfirm(userId, token)
      .then((result) => {
        if (cancelled) return;
        setStatus('ok');
        setMessage((result as { message?: string })?.message ?? 'Email confirmed. You can now log in.');
      })
      .catch((err: any) => {
        if (cancelled) return;
        setStatus('error');
        setMessage(err.message ?? 'This confirmation link is invalid or has expired.');
      });
    return () => { cancelled = true; };
  }, [userId, token, onConfirm]);

  return (
    <div className="lobby-page">
      <div className="lobby-card">
        <h1 className="lobby-title">Town Wars</h1>
        {status === 'checking' && <p className="lobby-subtitle">{message}</p>}
        {status === 'ok' && <div className="lobby-note">{message}</div>}
        {status === 'error' && <div className="error-box">{message}</div>}
        {status !== 'checking' && (
          <button className="start-btn" onClick={onDone} style={{ marginTop: 16 }}>
            Go to login
          </button>
        )}
      </div>
    </div>
  );
}
