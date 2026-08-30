import { useState } from 'react';
import { AuthProviders } from '../hooks/useAuth';

interface AuthPageProps {
  providers: AuthProviders;
  onRegister: (email: string, password: string, displayName: string) => Promise<unknown>;
  onLogin: (email: string, password: string) => Promise<unknown>;
  onLoginWithGoogle: () => void;
  onLoginWithFacebook: () => void;
}

export function AuthPage({ providers, onRegister, onLogin, onLoginWithGoogle, onLoginWithFacebook }: AuthPageProps) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');
  const [busy, setBusy] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setInfo('');
    setBusy(true);
    try {
      if (mode === 'register') {
        const result = await onRegister(email, password, displayName);
        setInfo((result as { message?: string })?.message ?? 'Check your email to confirm your account.');
        setMode('login');
      } else {
        await onLogin(email, password);
      }
    } catch (err: any) {
      setError(err.message ?? 'Something went wrong');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="lobby-page">
      <div className="lobby-card">
        <h1 className="lobby-title">Town Wars</h1>
        <p className="lobby-subtitle">
          {mode === 'login' ? 'Log in to play' : 'Create an account'}
        </p>

        {error && <div className="error-box">{error}</div>}
        {info && <div className="lobby-note">{info}</div>}

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Email</label>
            <input
              type="email"
              className="player-name-input"
              value={email}
              onChange={e => setEmail(e.target.value)}
              required
              autoComplete="email"
            />
          </div>

          {mode === 'register' && (
            <div className="form-group">
              <label>Display name (optional)</label>
              <input
                type="text"
                className="player-name-input"
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                placeholder="Shown to other players"
              />
            </div>
          )}

          <div className="form-group">
            <label>Password</label>
            <input
              type="password"
              className="player-name-input"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              minLength={8}
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            />
          </div>

          <button className="start-btn" type="submit" disabled={busy}>
            {busy ? 'Please wait...' : mode === 'login' ? 'Log In' : 'Register'}
          </button>
        </form>

        {(providers.google || providers.facebook) && (
          <div className="mode-select" style={{ marginTop: 16 }}>
            {providers.google && (
              <button className="back-btn" onClick={onLoginWithGoogle}>
                Continue with Google
              </button>
            )}
            {providers.facebook && (
              <button className="back-btn" onClick={onLoginWithFacebook}>
                Continue with Facebook
              </button>
            )}
          </div>
        )}

        <button
          className="back-btn"
          style={{ marginTop: 16 }}
          onClick={() => { setMode(mode === 'login' ? 'register' : 'login'); setError(''); setInfo(''); }}
        >
          {mode === 'login' ? "Don't have an account? Register" : 'Already have an account? Log in'}
        </button>
      </div>
    </div>
  );
}
