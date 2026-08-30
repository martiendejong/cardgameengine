import { useState, useEffect } from 'react';
import { LobbyPage } from './pages/LobbyPage';
import { GamePage } from './pages/GamePage';
import { CampaignPage } from './pages/CampaignPage';
import { DecksPage } from './pages/DecksPage';
import { AuthPage } from './pages/AuthPage';
import { ConfirmEmailPage } from './pages/ConfirmEmailPage';
import { useAuth } from './hooks/useAuth';
import { BASE } from './config';
import './App.css';

interface Session {
  matchId: string;
  seat: string; // player id for a fixed seat, '' for hotseat (omniscient)
}

const GAME_ID = 'town-tcg';

function App() {
  const auth = useAuth();
  const [session, setSession] = useState<Session | null>(null);
  const [view, setView] = useState<'loading' | 'lobby' | 'campaign' | 'decks'>('loading');
  const [confirmParams, setConfirmParams] = useState<{ userId: string; token: string } | null>(null);
  const [authError, setAuthError] = useState('');

  // Email-confirmation links and OAuth error redirects land here as query params —
  // handled before the login gate below, since a just-registered user isn't logged in yet.
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const userId = params.get('userId');
    const token = params.get('token');
    if (userId && token) {
      setConfirmParams({ userId, token });
      return;
    }
    const err = params.get('authError');
    if (err) {
      setAuthError('Sign-in failed. Please try again.');
      window.history.replaceState(null, '', window.location.pathname);
    }
  }, []);

  useEffect(() => {
    if (auth.loading || !auth.user?.authenticated || confirmParams) return;

    const params = new URLSearchParams(window.location.search);
    const match = params.get('match');
    const player = params.get('player');
    if (match) {
      setSession({ matchId: match, seat: player ?? '' });
      return;
    }
    if (params.get('campaign')) {
      setView('campaign');
      return;
    }
    fetch(`${BASE}api/campaign?gameId=${GAME_ID}`, { credentials: 'include' })
      .then(r => r.json())
      .then(data => setView(data.multiplayerUnlocked ? 'lobby' : 'campaign'))
      .catch(() => setView('campaign'));
  }, [auth.loading, auth.user?.authenticated, confirmParams]);

  function handleMatchCreated(matchId: string, seat: string) {
    if (seat) {
      const url = new URL(window.location.href);
      url.searchParams.set('match', matchId);
      url.searchParams.set('player', seat);
      window.history.replaceState(null, '', url.toString());
    }
    setSession({ matchId, seat });
  }

  function handleMissionStarted(matchId: string, seat: string) {
    const url = new URL(window.location.href);
    url.searchParams.set('match', matchId);
    url.searchParams.set('player', seat);
    url.searchParams.set('campaign', '1');
    window.history.replaceState(null, '', url.toString());
    setSession({ matchId, seat });
  }

  function handleLeave() {
    const params = new URLSearchParams(window.location.search);
    const backToCampaign = params.get('campaign') !== null;
    window.history.replaceState(
      null, '',
      window.location.pathname + (backToCampaign ? '?campaign=1' : ''),
    );
    setSession(null);
    setView(backToCampaign ? 'campaign' : 'lobby');
  }

  function openCampaign() {
    window.history.replaceState(null, '', window.location.pathname + '?campaign=1');
    setView('campaign');
  }

  function openDecks() {
    window.history.replaceState(null, '', window.location.pathname);
    setView('decks');
  }

  function openLobby() {
    window.history.replaceState(null, '', window.location.pathname);
    setView('lobby');
  }

  async function handleLogout() {
    await auth.logout();
    setSession(null);
    setView('loading');
    window.history.replaceState(null, '', window.location.pathname);
  }

  if (confirmParams) {
    return (
      <ConfirmEmailPage
        userId={confirmParams.userId}
        token={confirmParams.token}
        onConfirm={auth.confirmEmail}
        onDone={() => {
          setConfirmParams(null);
          window.history.replaceState(null, '', window.location.pathname);
        }}
      />
    );
  }

  if (auth.loading) {
    return <div className="lobby-page"><div className="lobby-card"><p>Loading...</p></div></div>;
  }

  if (!auth.user?.authenticated) {
    return (
      <>
        {authError && <div className="error-toast">{authError}</div>}
        <AuthPage
          providers={auth.providers}
          onRegister={auth.register}
          onLogin={auth.login}
          onLoginWithGoogle={auth.loginWithGoogle}
          onLoginWithFacebook={auth.loginWithFacebook}
        />
      </>
    );
  }

  if (session) {
    return (
      <GamePage
        matchId={session.matchId}
        seat={session.seat}
        onLeave={handleLeave}
      />
    );
  }

  const accountBar = (
    <div className="account-bar">
      <span>Logged in as {auth.user.displayName ?? auth.user.email}</span>
      <button className="back-btn" onClick={handleLogout}>Log out</button>
    </div>
  );

  if (view === 'loading') {
    return <div className="lobby-page"><div className="lobby-card"><p>Loading...</p></div></div>;
  }

  if (view === 'campaign') {
    return (
      <div className="app-shell">
        {accountBar}
        <CampaignPage onMissionStarted={handleMissionStarted} onOpenLobby={openLobby} />
      </div>
    );
  }

  if (view === 'decks') {
    return (
      <div className="app-shell">
        {accountBar}
        <DecksPage onOpenLobby={openLobby} />
      </div>
    );
  }

  return (
    <div className="app-shell">
      {accountBar}
      <LobbyPage
        onMatchCreated={handleMatchCreated}
        onOpenCampaign={openCampaign}
        onOpenDecks={openDecks}
        canUseAdminMode={auth.user.isAdmin === true}
      />
    </div>
  );
}

export default App;
