import React, { useState, useEffect, useCallback } from 'react';
import { Users, Trophy, Clock, ArrowLeft, Play, LogIn, Plus, Settings, Filter } from 'lucide-react';
import Login from './Login';
import Editor from './Editor';
import './App.css';
import TextPressure from './TextPressure';
import Profile from './Profile';
import FriendButton from './FriendButton';
import Navbar from './Navbar';
import Countdown from './Countdown';
import Background from './Background';
import GameConnection from './services/GameConnection';

const hubUrl = process.env.NODE_ENV === "development"
    ? "https://localhost:5001/gamehub"
    : "/gamehub";

function TriviaGame({ username, onLogout }) {
    const [connection] = useState(() => new GameConnection());
    const [connected, setConnected] = useState(false);
    const [currentUserId, setCurrentUserId] = useState('');
    const [gameState, setGameState] = useState('menu');
    const [gameId, setGameId] = useState('');
    const [playerId, setPlayerId] = useState(null);
    const [players, setPlayers] = useState([]);
    const [currentQuestion, setCurrentQuestion] = useState(null);
    const [selectedAnswer, setSelectedAnswer] = useState(null);
    const [answerResult, setAnswerResult] = useState(null);
    const [leaderboard, setLeaderboard] = useState([]);
    const [timeLeft, setTimeLeft] = useState(0);
    const [showAnswer, setShowAnswer] = useState(false);
    const [showGlobalLeaderboard, setShowGlobalLeaderboard] = useState(false);
    const [globalLeaderboard, setGlobalLeaderboard] = useState([]);
    const [showProfile, setShowProfile] = useState(false);
    const [viewProfileUsername, setViewProfileUsername] = useState(null);
    const [showFriendsPanel, setShowFriendsPanel] = useState(false);
    const [friendRequests, setFriendRequests] = useState([]);
    const [friendsList, setFriendsList] = useState([]);
    const [outgoingRequests, setOutgoingRequests] = useState([]);
    const [incomingInvites, setIncomingInvites] = useState([]);
    const [allUsers, setAllUsers] = useState([]);
    const [playerSearchResults, setPlayerSearchResults] = useState([]);
    const [isHost, setIsHost] = useState(false);
    const [showCountdown, setShowCountdown] = useState(false)

    // Team mode state
    const [isTeamMode, setIsTeamMode] = useState(false);
    const [numberOfTeams, setNumberOfTeams] = useState(2);
    const [teams, setTeams] = useState([]);

    const [currentQuestionNumber, setCurrentQuestionNumber] = useState(0);
    const [totalQuestions, setTotalQuestions] = useState(10);

    const handleCountdownComplete = useCallback(() => {
        console.log('=== COUNTDOWN COMPLETE ===');
        console.log('Setting showCountdown to false, gameState to playing');
        setShowCountdown(false);
        setGameState('playing');
    }, []);

    // local editor toggle inside TriviaGame
    const [showEditorLocal, setShowEditorLocal] = useState(false);
    const openEditor = () => setShowEditorLocal(true);
    const closeEditor = () => setShowEditorLocal(false);

    // Lobby settings state
    const [gameSettings, setGameSettings] = useState({
        maxPlayers: 10,
        questionsPerGame: 10,
        defaultTimeLimit: 30,
        questionCategories: ['Science', 'History', 'Sports', 'Geography', 'Literature'],
        maxDifficulty: 'Hard'
    });
    const [selectedCategories, setSelectedCategories] = useState(['Science', 'History', 'Sports', 'Geography', 'Literature']);
    const [selectedDifficulty, setSelectedDifficulty] = useState('Hard');
    const [maxPlayers, setMaxPlayers] = useState(10);
    const [questionsPerGame, setQuestionsPerGame] = useState(10);
    const [availableCategories] = useState(['Science', 'History', 'Sports', 'Geography', 'Literature']);
    const [availableDifficulties] = useState(['Easy', 'Medium', 'Hard']);

    const handleLogoutClick = async () => {
        if (currentUserId) {
            await fetch('/api/friendship/presence/logout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ userId: currentUserId })
            }).catch(err => {
                console.error('Failed to report logout presence', err);
            });
        }
        await connection.disconnect();
        onLogout();
    };

    useEffect(() => {
        const initConnection = async () => {
            const success = await connection.connect(hubUrl);
            setConnected(success);
        };
        initConnection();

        return () => {
            connection.disconnect();
        };
    }, [connection]);

    useEffect(() => {
        const resolveCurrentUserId = async () => {
            if (!username) {
                setCurrentUserId('');
                return;
            }

            try {
                const res = await fetch(`/api/clan/getuser/${encodeURIComponent(username)}`);
                if (!res.ok) return;
                const user = await res.json();
                setCurrentUserId(user.id || user.userId || '');
            } catch (err) {
                console.error('Failed to resolve current user id', err);
            }
        };

        resolveCurrentUserId();
    }, [username]);

    useEffect(() => {
        if (!currentUserId) return undefined;

        const pingPresence = () => {
            fetch('/api/friendship/presence/ping', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ userId: currentUserId })
            }).catch(err => {
                console.error('Failed to ping presence', err);
            });
        };

        pingPresence();
        const intervalId = window.setInterval(pingPresence, 10000);

        const handlePageExit = () => {
            const payload = JSON.stringify({ userId: currentUserId });
            if (navigator.sendBeacon) {
                const blob = new Blob([payload], { type: 'application/json' });
                navigator.sendBeacon('/api/friendship/presence/logout', blob);
                return;
            }

            fetch('/api/friendship/presence/logout', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: payload,
                keepalive: true
            }).catch(() => { });
        };

        window.addEventListener('pagehide', handlePageExit);
        window.addEventListener('beforeunload', handlePageExit);

        return () => {
            window.clearInterval(intervalId);
            window.removeEventListener('pagehide', handlePageExit);
            window.removeEventListener('beforeunload', handlePageExit);
        };
    }, [currentUserId]);

    useEffect(() => {
        if (!currentUserId || gameState !== 'lobby') return;
        fetchFriendsForUser(currentUserId);
    }, [currentUserId, gameState]);

    useEffect(() => {
        if (!connected) return;

        const handleGameCreated = (data) => {
            console.log('Game created:', data);
            setGameId(data.gameId);
            setPlayerId(data.playerId);
            setIsHost(true);
            setGameState('lobby');

            if (data.settings) {
                setGameSettings(data.settings);
                setMaxPlayers(data.settings.maxPlayers);
                setQuestionsPerGame(data.settings.questionsPerGame);
                setSelectedCategories(data.settings.questionCategories || availableCategories);
                setSelectedDifficulty(data.settings.maxDifficulty || 'Hard');
                setIsTeamMode(data.settings.isTeamMode || false);
                setNumberOfTeams(data.settings.numberOfTeams || 2);
            }
            if (data.teams) {
                setTeams(data.teams);
            }
        };

        const handleJoinedGame = (data) => {
            console.log('Joined game:', data);
            setGameId(data.gameId);
            setPlayerId(data.playerId);
            setPlayers(data.players);
            setIsHost(false);
            setGameState('lobby');
        };

        const handlePlayerLeft = (data) => {
            console.log('Player left:', data);
            setPlayers(data.players);
        }

        const handlePlayerJoined = (data) => {
            console.log('Player joined:', data);
            setPlayers(data.players);
        };

        const handleTeamsUpdated = (data) => {
            console.log('Teams updated:', data);
            setTeams(data.teams || []);
        };

        const handleSettingsUpdated = (data) => {
            console.log('Settings updated:', data);
            setGameSettings(data.settings);
            setMaxPlayers(data.settings.maxPlayers);
            setQuestionsPerGame(data.settings.questionsPerGame);
            setSelectedCategories(data.settings.questionCategories);
            setSelectedDifficulty(data.settings.maxDifficulty);
            setIsTeamMode(data.settings.isTeamMode);
            setNumberOfTeams(data.settings.numberOfTeams);
            if (data.teams) {
                setTeams(data.teams);
            }
        };

        const handleGameStarted = () => {
            console.log('Game started');
            setShowCountdown(true);
            setShowAnswer(false);
            setTotalQuestions(questionsPerGame);
            setCurrentQuestionNumber(0);
        };

        const handleQuestionSent = (data) => {
            console.log('Question sent:', data);
            setCurrentQuestion(data);
            setSelectedAnswer(null);
            setAnswerResult(null);
            setShowAnswer(false);
            setTimeLeft(data.timeLimit);
            setCurrentQuestionNumber(prev => prev + 1);
            setGameState('playing');
        };

        const handleAnswerResult = (data) => {
            console.log('Answer result:', data);
            setAnswerResult(data);
        };

        const handleQuestionRevealed = (data) => {
            console.log('Question revealed:', data);
            setShowAnswer(true);
            const modifiedLeaderboard = data.leaderboard.map(p => {
                if (p.Name === 'admin') {
                    return { ...p, score: '', correctAnswers: '' };
                }
                return p;
            });
            setLeaderboard(modifiedLeaderboard);
            if (currentQuestionNumber === totalQuestions) {
                setGameState('results');
            }
        };

        const handleGameEnded = (data) => {
            console.log('Game ended:', data);
            const modifiedLeaderboard = data.leaderboard.map(p => {
                if (p.Name === 'admin') {
                    return { ...p, score: '', correctAnswers: '' };
                }
                return p;
            });
            setLeaderboard(modifiedLeaderboard);
            setGameState('results');
        };

        const handleError = (message) => {
            console.error('Error:', message);
            alert(message);
        };

        const handleLobbyClosed = (data) => {
            console.log('Lobby closed:', data);
            resetLobbyAndGameState();
            alert(data?.reason || 'Lobby closed');
        };

        connection.on('GameCreated', handleGameCreated);
        connection.on('JoinedGame', handleJoinedGame);
        connection.on('PlayerJoined', handlePlayerJoined);
        connection.on('PlayerLeft', handlePlayerLeft);
        connection.on('SettingsUpdated', handleSettingsUpdated);
        connection.on('TeamsUpdated', handleTeamsUpdated);
        connection.on('GameStarted', handleGameStarted);
        connection.on('QuestionSent', handleQuestionSent);
        connection.on('AnswerResult', handleAnswerResult);
        connection.on('QuestionRevealed', handleQuestionRevealed);
        connection.on('GameEnded', handleGameEnded);
        connection.on('Error', handleError);
        connection.on('LobbyClosed', handleLobbyClosed);
        connection.on('FriendRequestReceived', (data) => {
            console.log('Friend request received:', data);
            if (currentUserId) {
                fetchFriendRequestsForUser(currentUserId);
            }
        });
        connection.on('FriendRequestAccepted', (data) => {
            console.log('Friend request accepted:', data);
            if (currentUserId) {
                fetchFriendRequestsForUser(currentUserId);
                fetchFriendsForUser(currentUserId);
            }
        });
        connection.on('FriendRequestResponded', (data) => {
            console.log('Friend request responded:', data);
            if (currentUserId) {
                fetchOutgoingRequestsForUser(currentUserId);
                if (data.accepted) {
                    fetchFriendsForUser(currentUserId);
                }
            }
        });
        connection.on('FriendStatusChanged', (data) => {
            console.log('Friend status changed:', data);
            setFriendsList(prev => prev.map(friend => {
                if ((friend.userId || friend.UserId) !== data.userId) return friend;
                return {
                    ...friend,
                    status: data.status,
                    Status: data.status,
                    activeGameId: data.gameId,
                    ActiveGameId: data.gameId
                };
            }));
        });
        connection.on('GameInviteReceived', (data) => {
            console.log('Game invite received:', data);
            setIncomingInvites(prev => {
                const alreadyExists = prev.some(invite => invite.gameId === data.gameId && invite.inviterUsername === data.inviterUsername);
                return alreadyExists ? prev : [data, ...prev];
            });
        });

        return () => {
            connection.off('GameCreated', handleGameCreated);
            connection.off('JoinedGame', handleJoinedGame);
            connection.off('PlayerJoined', handlePlayerJoined);
            connection.off('PlayerLeft', handlePlayerLeft);
            connection.off('SettingsUpdated', handleSettingsUpdated);
            connection.off('TeamsUpdated', handleTeamsUpdated);
            connection.off('CountdownComplete', handleCountdownComplete);
            connection.off('GameStarted', handleGameStarted);
            connection.off('QuestionSent', handleQuestionSent);
            connection.off('AnswerResult', handleAnswerResult);
            connection.off('QuestionRevealed', handleQuestionRevealed);
            connection.off('GameEnded', handleGameEnded);
            connection.off('Error', handleError);
            connection.off('LobbyClosed', handleLobbyClosed);
            connection.off('FriendRequestReceived');
            connection.off('FriendRequestAccepted');
            connection.off('FriendRequestResponded');
            connection.off('FriendStatusChanged');
            connection.off('GameInviteReceived');
        };
    }, [connected, connection, availableCategories, currentUserId, handleCountdownComplete]);

    useEffect(() => {
        if (timeLeft > 0 && currentQuestion && !showAnswer) {
            const timer = setTimeout(() => setTimeLeft(timeLeft - 1), 1000);
            return () => clearTimeout(timer);
        }
    }, [timeLeft, currentQuestion, showAnswer]);

    const createGame = async () => {
        await connection.invoke('CreateGame', username);
    };

    const joinGame = async () => {
        if (!gameId.trim()) return;
        await connection.invoke('JoinGame', gameId.toUpperCase(), username);
    };

    const fetchFriendRequestsForUser = async (userId) => {
        try {
            const res = await fetch(`/api/friendship/requests/${encodeURIComponent(userId)}`);
            if (res.ok) setFriendRequests(await res.json());
        } catch (err) {
            console.error('Failed to fetch friend requests', err);
        }
    };

    const fetchOutgoingRequestsForUser = async (userId) => {
        try {
            const res = await fetch(`/api/friendship/outgoing/${encodeURIComponent(userId)}`);
            if (res.ok) setOutgoingRequests(await res.json());
        } catch (err) {
            console.error('Failed to fetch outgoing friend requests', err);
        }
    };

    const fetchFriendsForUser = async (userId) => {
        try {
            const res = await fetch(`/api/friendship/friends/${encodeURIComponent(userId)}`);
            if (res.ok) setFriendsList(await res.json());
        } catch (err) {
            console.error('Failed to fetch friends list', err);
        }
    };

    const openFriendsPanel = async () => {
        try {
            if (!username) return alert('Not logged in');
            const uRes = await fetch(`/api/clan/getuser/${encodeURIComponent(username)}`);
            if (!uRes.ok) return alert('Could not resolve current user id');
            const u = await uRes.json();
            const userId = u.id || u.userId || u;
            await fetchFriendRequestsForUser(userId);
            await fetchFriendsForUser(userId);

            try {
                const res = await fetch('/api/clan/users');
                if (res.ok) {
                    const data = await res.json();
                    const normalized = (data || []).map(u => ({ username: u.username || u.Username, id: u.id || u.Id }));
                    
                    const filtered = normalized.filter(u => (u.username || '').toLowerCase() !== (username || '').toLowerCase());
                    setAllUsers(filtered);
                    setPlayerSearchResults(filtered);

                    try { await fetchOutgoingRequestsForUser(userId); } catch (e) { /* ignore */ }
                    if ((normalized || []).length === 0) alert('No users returned from server');
                } else {
                    console.error('/api/clan/users returned', res.status);
                    alert('Failed to load users list from server');
                }
            } catch (err) {
                console.error('Failed to fetch users list', err);
            }
            setShowFriendsPanel(true);
        } catch (err) {
            console.error('Failed to open friends panel', err);
            alert('Failed to open friends panel');
        }
    };

    // removed fetchTopPlayers; fetching all users is done in openFriendsPanel

    // View another player's profile
    const viewProfile = (otherUsername) => {
        setViewProfileUsername(otherUsername);
        setShowProfile(true);
    };

    // Search for players by username (uses leaderboard/rank endpoint to check existence)
    const [playerSearchQuery, setPlayerSearchQuery] = useState('');

    useEffect(() => {
        const normalizedQuery = playerSearchQuery.trim().toLowerCase();

        if (!normalizedQuery) {
            setPlayerSearchResults(allUsers || []);
            return;
        }

        const matches = (allUsers || []).filter(p => (p.username || '').toLowerCase().includes(normalizedQuery));
        setPlayerSearchResults(matches.slice(0, 50));
    }, [allUsers, playerSearchQuery]);

    const sendFriendRequestByUsername = async (targetUsername) => {
        try {
            const uRes = await fetch(`/api/clan/getuser/${encodeURIComponent(username)}`);
            if (!uRes.ok) return alert('Could not resolve current user id');
            const u = await uRes.json();
            const requesterId = u.id || u.userId || u;
            const res = await fetch('/api/friendship/send', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ requesterId, addresseeUsername: targetUsername })
            });
            if (res.ok) {
                alert('Friend request sent');
                // refresh outgoing requests so UI shows Pending
                try { await fetchOutgoingRequestsForUser(requesterId); } catch (e) { }
            } else {
                const t = await res.text().catch(() => '');
                alert(t || 'Failed to send friend request');
            }
        } catch (err) {
            console.error('Error sending friend request', err);
            alert('Network error');
        }
    };

    const respondToFriendRequest = async (friendshipId, accept) => {
        try {
            if (!username) return;
            const uRes = await fetch(`/api/clan/getuser/${encodeURIComponent(username)}`);
            if (!uRes.ok) return;
            const u = await uRes.json();
            const userId = u.id || u.userId || u;
            const res = await fetch('/api/friendship/respond', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ addresseeId: userId, friendshipId, accept: !!accept })
            });
            if (res.ok) {
                await fetchFriendRequestsForUser(userId);
                await fetchFriendsForUser(userId);
            } else {
                const t = await res.text().catch(() => '');
                alert(t || 'Failed to respond to friend request');
            }
        } catch (err) {
            console.error('Error responding to friend request', err);
        }
    };

    const sendGameInvite = async (friendId) => {
        try {
            if (!gameId || !currentUserId) return alert('No active game');
            await connection.invoke('SendGameInvite', gameId, currentUserId, friendId);
            alert('Invite sent');
        } catch (err) {
            console.error('Error sending invite', err);
            alert(err?.message || 'Failed to send invite');
        }
    };

    const acceptGameInvite = async (invite) => {
        try {
            await connection.invoke('AcceptGameInvite', invite.gameId, username);
            setIncomingInvites(prev => prev.filter(i => i !== invite));
        } catch (err) {
            console.error('Error accepting invite', err);
            setIncomingInvites(prev => prev.filter(i => i !== invite));
            alert(err?.message || 'Could not join invited lobby');
        }
    };

    const declineGameInvite = (invite) => {
        setIncomingInvites(prev => prev.filter(i => i !== invite));
    };

    const assignPlayerToTeam = async (playerId, teamId) => {
        try {
            await connection.invoke('AssignPlayerToTeam', gameId, playerId, teamId);
        } catch (error) {
            console.error('Error assigning player to team:', error);
        }
    };

    const updateGameSettings = async () => {
        try {
            await connection.invoke(
                'UpdateGameSettings',
                gameId,
                maxPlayers,
                questionsPerGame,
                selectedCategories,
                selectedDifficulty,
                isTeamMode,
                numberOfTeams
            );
            alert('Settings updated successfully!');
        } catch (error) {
            console.error('Error updating settings:', error);
            alert('Failed to update settings');
        }
    };

    const startGame = async () => {
        try {
            // Update settings one last time before starting
            await connection.invoke(
                'UpdateGameSettings',
                gameId,
                maxPlayers,
                questionsPerGame,
                selectedCategories,
                selectedDifficulty,
                isTeamMode, 
                numberOfTeams
            );

            // Start the game
            await connection.invoke('StartGame', gameId, selectedCategories, selectedDifficulty);
        } catch (error) {
            console.error('Error starting game:', error);
        }
    };

    const submitAnswer = async (index) => {
        if (selectedAnswer !== null) return;
        setSelectedAnswer(index);
        try {
            await connection.invoke('SubmitAnswer', gameId, playerId, index);
        } catch (error) {
            console.error('Error submitting answer:', error);
            if (currentQuestionNumber === totalQuestions) {
                const blankLeaderboard = players.map(p => ({
                    Id: p.id,
                    Name: p.name,
                    score: p.name === 'admin' ? '' : 0,
                    correctAnswers: p.name === 'admin' ? '' : 0
                }));
                setLeaderboard(blankLeaderboard);
                setGameState('results');
            }
        }
    };

    const resetLobbyAndGameState = () => {
        setGameState('menu');
        setGameId('');
        setPlayerId(null);
        setPlayers([]);
        setCurrentQuestion(null);
        setLeaderboard([]);
        setIsHost(false);
        setSelectedAnswer(null);
        setAnswerResult(null);
        setShowAnswer(false);
        setSelectedCategories(['Science', 'History', 'Sports', 'Geography', 'Literature']);
        setSelectedDifficulty('Hard');
        setMaxPlayers(10);
        setQuestionsPerGame(10);
        setCurrentQuestionNumber(0);
        setTotalQuestions(10);
    };

    const leaveGame = async () => {
        if (gameId && playerId) {
            try {
                console.log(`Leaving game ${gameId} as player ${playerId}`);
                await connection.invoke('LeaveGame');
            } catch (error) {
                console.error('Error leaving game:', error);
            }
        }

        resetLobbyAndGameState();
    };

    const fetchGlobalLeaderboard = async () => {
        try {
            console.log('Fetching global leaderboard...');
            const response = await fetch('/api/leaderboard/global?top=100');
            console.log('Response status:', response.status);
            console.log('Response ok:', response.ok);

            if (response.ok) {
                const data = await response.json();
                console.log('Leaderboard data:', data);
                setGlobalLeaderboard(data);
                setShowGlobalLeaderboard(true);
            } else {
                const errorText = await response.text();
                console.error('Failed to fetch leaderboard:', response.status, errorText);
                alert(`Failed to load leaderboard: ${response.status} ${response.statusText}`);
            }
        } catch (error) {
            console.error('Error fetching leaderboard:', error);
            alert(`Error loading leaderboard: ${error.message}`);
        }
    };

    const handleCategoryToggle = (category) => {
        setSelectedCategories(prev => {
            if (prev.includes(category)) {
                if (prev.length === 1) return prev; // Don't allow removing all categories
                return prev.filter(c => c !== category);
            } else {
                return [...prev, category];
            }
        });
    };

    const renderIncomingInvites = () => {
        if (incomingInvites.length === 0) return null;

        return (
            <div className="invite-stack">
                {incomingInvites.map((invite, idx) => (
                    <div key={`${invite.gameId}-${invite.inviterUsername || idx}`} className="invite-toast">
                        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 10 }}>
                            <Users className="icon" />
                            <div>
                                <div style={{ fontWeight: 700, color: '#1a202c' }}>Game invitation</div>
                                <div style={{ color: '#718096', fontSize: '14px' }}>
                                    {invite.inviterUsername || 'A friend'} invited you to lobby {invite.gameId}
                                </div>
                            </div>
                        </div>
                        <div style={{ display: 'flex', gap: 10 }}>
                            <button onClick={() => acceptGameInvite(invite)} className="button button-primary" style={{ flex: 1 }}>
                                Join
                            </button>
                            <button onClick={() => declineGameInvite(invite)} className="button button-secondary" style={{ flex: 1 }}>
                                Decline
                            </button>
                        </div>
                    </div>
                ))}
            </div>
        );
    };

    if (showEditorLocal) {
        return (
            <Editor
                onHome={() => { setShowEditorLocal(false); setGameState('menu'); }}
                onEditor={() => setShowEditorLocal(false)}
                onLogout={handleLogoutClick}
                fetchGlobalLeaderboard={fetchGlobalLeaderboard}
                onProfileClick={() => setShowProfile(true)}
            />
        );
    }

    if (!connected) {
        return (
            <>
                <Background />
                <div className="container">
                    <div className="loading">Connecting to game server...</div>
                </div>
            </>
        );
    }

    if (showProfile) {
        return <Profile username={viewProfileUsername || username} currentUsername={username} onBack={() => { setShowProfile(false); setViewProfileUsername(null); }} />;
    }
    if (showGlobalLeaderboard) {
        return (
            <>
                <Background />
                <div className="container">
                    <div className="card" style={{ maxWidth: '700px' }}>
                        <div className="header">
                            <Trophy className="icon-large" />
                            <h2>Global Leaderboard</h2>
                            <p>Top players ranked by ELO rating</p>
                        </div>

                        <div className="results" style={{ maxHeight: '500px', overflowY: 'auto' }}>
                            {globalLeaderboard.length === 0 ? (
                                <div style={{ textAlign: 'center', padding: '40px', color: '#6b7280' }}>
                                    No players found
                                </div>
                            ) : (
                                globalLeaderboard.map((player, index) => {
                                    const medals = ['🥇', '🥈', '🥉'];
                                    return (
                                        <div
                                            key={player.username || index}
                                            className={`result-item ${index < 3 ? `rank-${index + 1}` : ''}`}
                                            style={{
                                                marginBottom: '10px',
                                                background: index < 3 ? 'linear-gradient(135deg, #f6f8fb 0%, #ffffff 100%)' : '#f9fafb'
                                            }}
                                        >
                                            <div className="result-left">
                                                <span style={{
                                                    fontWeight: 'bold',
                                                    minWidth: '30px',
                                                    color: '#667eea'
                                                }}>
                                                    #{index + 1}
                                                </span>
                                                {index < 3 && <span className="medal">{medals[index]}</span>}
                                                <div>
                                                    <div className="player-name">
                                                        {player.username}
                                                        {player.username === username && (
                                                            <span className="badge" style={{ marginLeft: '10px' }}>You</span>
                                                        )}
                                                    </div>
                                                    <div className="player-stats">
                                                        {player.gamesPlayed} games played • {player.totalPoints} total points
                                                    </div>
                                                </div>
                                            </div>
                                            <div className="final-score" style={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
                                                <span style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#667eea' }}>
                                                    {player.elo}
                                                </span>
                                                <span style={{ fontSize: '0.75rem', color: '#6b7280' }}>ELO</span>
                                            </div>
                                        </div>
                                    );
                                })
                            )}
                        </div>

                        <button
                            onClick={() => setShowGlobalLeaderboard(false)}
                            className="button button-primary"
                            style={{ marginTop: '20px' }}
                        >
                            <ArrowLeft className="icon" />
                            Back to Menu
                        </button>
                    </div>
                </div>
            </>
        );
    }

    if (showCountdown) {
        console.log('=== SHOWING COUNTDOWN ===');
        return (
            <>
                <Background />
                <Countdown onComplete={handleCountdownComplete} />
            </>
        );
    }

    if (gameState === 'menu') {
        if (showFriendsPanel) {
            return (
                <>
                    <Background />
                    {renderIncomingInvites()}
                    <div className="container">
                        <div className="card" style={{ maxWidth: '700px' }}>
                            <div className="header">
                                <Users className="icon-large" />
                                <h2>Friends & Requests</h2>
                                <p>Manage friend requests and invite friends to your game</p>
                            </div>

                                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 20 }}>
                                <div>
                                    <h3 style={{ marginBottom: 12 }}>Pending Requests</h3>
                                    <div className="global-leaderboard">
                                        {friendRequests.length === 0 ? (
                                            <div className="empty-leaderboard">No pending requests</div>
                                        ) : (
                                            friendRequests.map((req, idx) => (
                                                <div key={req.friendshipId || req.id || idx} className="leaderboard-row">
                                                    <div className="player-details">
                                                        <div className="player-username">{req.requesterUsername || req.requesterId}</div>
                                                    </div>
                                                    <div style={{ display: 'flex', gap: 8 }}>
                                                        <button className="button button-primary" onClick={() => respondToFriendRequest(req.friendshipId || req.id, true)}>Accept</button>
                                                        <button className="button button-secondary" onClick={() => respondToFriendRequest(req.friendshipId || req.id, false)}>Decline</button>
                                                    </div>
                                                </div>
                                            ))
                                        )}
                                    </div>
                                </div>

                                <div>
                                    <h3 style={{ marginBottom: 12 }}>Friends</h3>
                                    <div className="global-leaderboard">
                                        {friendsList.length === 0 ? (
                                            <div className="empty-leaderboard">No friends yet</div>
                                        ) : (
                                            friendsList.map((f, idx) => {
                                                const status = f.status ?? f.Status ?? 'Offline';
                                                return (
                                                    <div key={f.userId || idx} className="leaderboard-row">
                                                        <div className="player-details">
                                                            <div className="player-username">{f.username || f.userId}</div>
                                                            <div className="player-games">{status}</div>
                                                        </div>
                                                        <div>
                                                            {/* Invite removed from friends panel; invites are sent from the lobby card */}
                                                            <span style={{ color: '#9ca3af' }}>{status === 'Online' ? 'Online' : status === 'InGame' ? 'In Game' : 'Offline'}</span>
                                                        </div>
                                                    </div>
                                                );
                                            })
                                        )}
                                    </div>
                                </div>
                            </div>

                            <div style={{ marginTop: 20 }}>
                                <h3 style={{ marginBottom: 12 }}>Find players</h3>
                                <div style={{ display: 'flex', gap: 8, marginBottom: 12 }}>
                                    <input className="input" placeholder="Search username" value={playerSearchQuery} onChange={(e) => setPlayerSearchQuery(e.target.value)} />
                                </div>
                                {playerSearchResults && playerSearchResults.length > 0 && (
                                    <div style={{ marginTop: 8 }}>
                                        {playerSearchResults.map((p, idx) => (
                                            <div key={p.Username || p.username || idx} className="leaderboard-row" style={{ marginBottom: 8 }}>
                                                <div className="player-details">
                                                    <div className="player-username">{p.Username || p.username}</div>
                                                </div>
                                                <div style={{ display: 'flex', gap: 8 }}>
                                                    <button className="button button-secondary" onClick={() => viewProfile(p.Username || p.username)}>View Profile</button>
                                                    <FriendButton
                                                        targetUsername={p.Username || p.username}
                                                        currentUsername={username}
                                                        friendsList={friendsList}
                                                        incomingRequests={friendRequests}
                                                        outgoingRequests={outgoingRequests}
                                                        onSend={sendFriendRequestByUsername}
                                                        onRespond={respondToFriendRequest}
                                                    />
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>

                            <button
                                onClick={() => { setShowFriendsPanel(false); setPlayerSearchQuery(''); setPlayerSearchResults([]); }}
                                className="button button-primary"
                                style={{ marginTop: '20px' }}
                            >
                                <ArrowLeft className="icon" />
                                Back to Menu
                            </button>
                        </div>
                    </div>
                </>
            );
        }

        return (
            <>
                <Background />
                {renderIncomingInvites()}
                <div className="container">
                    <Navbar
                        onProfileClick={() => setShowProfile(true)}
                        onEditor={openEditor}
                        onFetchGlobalLeaderboard={fetchGlobalLeaderboard}
                        onLogout={handleLogoutClick}
                        onFriendsClick={openFriendsPanel}
                    />

                    <div className="card">
                        <div className="header">
                            <div style={{ position: 'relative', height: '80px', marginBottom: '55px' }}>
                                <TextPressure
                                    text="TRIVIA GAME"
                                    flex={true}
                                    alpha={false}
                                    stroke={false}
                                    width={true}
                                    weight={true}
                                    italic={true}
                                    textColor="#1a202c"
                                    strokeColor="#667eea"
                                    minFontSize={32}
                                />
                            </div>
                            <p>Test your knowledge and compete with friends!</p>
                        </div>

                        <div className="button-group">
                            <button onClick={createGame} className="button button-primary">
                                <Plus className="icon" />
                                Create New Game
                            </button>

                            <div className="join-group">
                                <input
                                    type="text"
                                    placeholder="GAME CODE"
                                    value={gameId}
                                    onChange={(e) => setGameId(e.target.value.toUpperCase())}
                                    className="input-small"
                                    maxLength={6}
                                />
                                <button onClick={joinGame} className="button button-secondary">
                                    <LogIn className="icon" />
                                    Join
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'lobby') {

        const getPlayerTeam = (playerId) => {
            if (!isTeamMode || !teams.length) return null;
            return teams.find(team => team.members.some(m => m.id === playerId));
        };

        const teamColors = {
            1: { bg: '#fee2e2', border: '#ef4444', text: '#991b1b' },
            2: { bg: '#dbeafe', border: '#3b82f6', text: '#1e40af' },
            3: { bg: '#d1fae5', border: '#10b981', text: '#065f46' },
            4: { bg: '#fef3c7', border: '#f59e0b', text: '#92400e' },
            5: { bg: '#e9d5ff', border: '#a855f7', text: '#6b21a8' },
            6: { bg: '#fed7aa', border: '#ea580c', text: '#9a3412' }
        };

        return (
            <>
                <Background />
                {renderIncomingInvites()}
                <div className="container" style={{ paddingTop: '100px' }}>
                    <div className="card" style={{ maxWidth: '1000px' }}>
                        <div className="header">
                            <h1>Game Lobby</h1>
                            <p>Game Code: <span className="game-code">{gameId}</span></p>
                        </div>

                        {/* Team Mode Display */}
                        {isTeamMode && teams.length > 0 && (
                            <div style={{
                                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                                padding: '16px',
                                borderRadius: '12px',
                                marginBottom: '20px',
                                color: 'white',
                                textAlign: 'center'
                            }}>
                                <h3 style={{ margin: '0 0 8px 0', fontSize: '18px' }}>
                                    🎮 Team Mode Active
                                </h3>
                                <p style={{ margin: 0, opacity: 0.9, fontSize: '14px' }}>
                                    Players compete in {numberOfTeams} teams
                                </p>
                            </div>
                        )}

                        <div style={{
                            display: 'grid',
                            gridTemplateColumns: isHost ? '1fr 2fr' : '1fr',
                            gap: '30px'
                        }}>
                            {/* Players/Teams Display */}
                            <div className="section">
                                {!isTeamMode ? (
                                    // Free-for-all mode
                                    <>
                                        <div className="section-header">
                                            <Users className="icon" />
                                            <h3>Players ({players.length}/{gameSettings.maxPlayers})</h3>
                                        </div>
                                        <div className="players-list">
                                            {players.map((player) => (
                                                <div key={player.id} className="player-item">
                                                    <span>{player.name}</span>
                                                    {player.id === playerId && <span className="badge">You</span>}
                                                </div>
                                            ))}
                                        </div>
                                    </>
                                ) : (
                                    // Team mode
                                    <>
                                        <div className="section-header">
                                            <Users className="icon" />
                                            <h3>Teams ({players.length}/{gameSettings.maxPlayers})</h3>
                                        </div>
                                        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                                            {teams.map((team) => {
                                                const colors = teamColors[team.id] || teamColors[1];
                                                return (
                                                    <div
                                                        key={team.id}
                                                        style={{
                                                            background: colors.bg,
                                                            border: `2px solid ${colors.border}`,
                                                            borderRadius: '12px',
                                                            padding: '16px'
                                                        }}
                                                    >
                                                        <div style={{
                                                            display: 'flex',
                                                            alignItems: 'center',
                                                            justifyContent: 'space-between',
                                                            marginBottom: '12px'
                                                        }}>
                                                            <h4 style={{
                                                                margin: 0,
                                                                color: colors.text,
                                                                fontSize: '16px',
                                                                fontWeight: 'bold'
                                                            }}>
                                                                {team.name}
                                                            </h4>
                                                            <span style={{
                                                                background: colors.border,
                                                                color: 'white',
                                                                padding: '4px 12px',
                                                                borderRadius: '12px',
                                                                fontSize: '12px',
                                                                fontWeight: 'bold'
                                                            }}>
                                                                {team.memberCount} {team.memberCount === 1 ? 'player' : 'players'}
                                                            </span>
                                                        </div>
                                                        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                                                            {team.members && team.members.length > 0 ? (
                                                                team.members.map((member) => (
                                                                    <div
                                                                        key={member.id}
                                                                        style={{
                                                                            background: 'white',
                                                                            padding: '8px 12px',
                                                                            borderRadius: '6px',
                                                                            display: 'flex',
                                                                            justifyContent: 'space-between',
                                                                            alignItems: 'center'
                                                                        }}
                                                                    >
                                                                        <span style={{ fontWeight: '500', color: '#1a202c' }}>
                                                                            {member.name}
                                                                        </span>
                                                                        {member.id === playerId && (
                                                                            <span style={{
                                                                                background: colors.border,
                                                                                color: 'white',
                                                                                padding: '2px 8px',
                                                                                borderRadius: '9999px',
                                                                                fontSize: '11px',
                                                                                fontWeight: '600'
                                                                            }}>
                                                                                You
                                                                            </span>
                                                                        )}
                                                                    </div>
                                                                ))
                                                            ) : (
                                                                <div style={{
                                                                    background: 'rgba(255,255,255,0.5)',
                                                                    padding: '12px',
                                                                    borderRadius: '6px',
                                                                    textAlign: 'center',
                                                                    color: colors.text,
                                                                    fontSize: '13px',
                                                                    fontStyle: 'italic'
                                                                }}>
                                                                    No players yet
                                                                </div>
                                                            )}
                                                        </div>
                                                        {/* Allow players to switch teams */}
                                                        {getPlayerTeam(playerId)?.id !== team.id && (
                                                            <button
                                                                onClick={() => assignPlayerToTeam(playerId, team.id)}
                                                                style={{
                                                                    marginTop: '8px',
                                                                    width: '100%',
                                                                    padding: '8px',
                                                                    background: colors.border,
                                                                    color: 'white',
                                                                    border: 'none',
                                                                    borderRadius: '6px',
                                                                    cursor: 'pointer',
                                                                    fontWeight: '600',
                                                                    fontSize: '13px'
                                                                }}
                                                            >
                                                                Join This Team
                                                            </button>
                                                        )}
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    </>
                                )}
                            </div>

                            {/* Settings Panel (Host Only) */}
                            {isHost && (
                                <div className="section">
                                    <div className="section-header">
                                        <Settings className="icon" />
                                        <h3>Game Settings</h3>
                                    </div>
                                    <div style={{
                                        background: '#dbeafe',
                                        padding: '12px',
                                        borderRadius: '6px',
                                        marginBottom: '15px',
                                        fontSize: '0.875rem',
                                        color: '#1e40af'
                                    }}>
                                        <strong>You are the host!</strong> Configure the game settings below and click "Start Game" when ready.
                                    </div>

                                    {/* Game Mode Toggle */}
                                    <div className="form-group">
                                        <label style={{
                                            display: 'block',
                                            marginBottom: '8px',
                                            fontWeight: '600',
                                            color: '#374151'
                                        }}>
                                            Game Mode:
                                        </label>
                                        <div style={{ display: 'flex', gap: '10px' }}>
                                            <button
                                                onClick={() => setIsTeamMode(false)}
                                                className={`button ${!isTeamMode ? 'button-primary' : 'button-secondary'}`}
                                                style={{ flex: 1 }}
                                            >
                                                Free-for-All
                                            </button>
                                            <button
                                                onClick={() => setIsTeamMode(true)}
                                                className={`button ${isTeamMode ? 'button-primary' : 'button-secondary'}`}
                                                style={{ flex: 1 }}
                                            >
                                                Team Mode
                                            </button>
                                        </div>
                                    </div>

                                    {/* Number of Teams (only show in team mode) */}
                                    {isTeamMode && (
                                        <div className="form-group">
                                            <label style={{
                                                display: 'block',
                                                marginBottom: '8px',
                                                fontWeight: '600',
                                                color: '#374151'
                                            }}>
                                                Number of Teams (2-6):
                                            </label>
                                            <input
                                                type="number"
                                                min="2"
                                                max="6"
                                                value={numberOfTeams}
                                                onChange={(e) => setNumberOfTeams(parseInt(e.target.value) || 2)}
                                                className="input"
                                            />
                                        </div>
                                    )}

                                    {/* Max Players */}
                                    <div className="form-group">
                                        <label style={{
                                            display: 'block',
                                            marginBottom: '8px',
                                            fontWeight: '600',
                                            color: '#374151'
                                        }}>
                                            Maximum Players (1-20):
                                        </label>
                                        <input
                                            type="number"
                                            min="1"
                                            max="20"
                                            value={maxPlayers}
                                            onChange={(e) => setMaxPlayers(parseInt(e.target.value) || 1)}
                                            className="input"
                                        />
                                    </div>

                                    {/* Questions Per Game */}
                                    <div className="form-group">
                                        <label style={{
                                            display: 'block',
                                            marginBottom: '8px',
                                            fontWeight: '600',
                                            color: '#374151'
                                        }}>
                                            Questions Per Game (5-50):
                                        </label>
                                        <input
                                            type="number"
                                            min="2"
                                            max="50"
                                            value={questionsPerGame}
                                            onChange={(e) => setQuestionsPerGame(parseInt(e.target.value) || 5)}
                                            className="input"
                                        />
                                    </div>

                                    {/* Difficulty */}
                                    <div className="form-group">
                                        <label style={{
                                            display: 'block',
                                            marginBottom: '8px',
                                            fontWeight: '600',
                                            color: '#374151'
                                        }}>
                                            Maximum Difficulty:
                                        </label>
                                        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                                            {availableDifficulties.map((difficulty) => (
                                                <button
                                                    key={difficulty}
                                                    onClick={() => setSelectedDifficulty(difficulty)}
                                                    className={`button ${selectedDifficulty === difficulty ? 'button-primary' : 'button-secondary'}`}
                                                    style={{ flex: '1', minWidth: '80px' }}
                                                >
                                                    {difficulty}
                                                </button>
                                            ))}
                                        </div>
                                    </div>

                                    {/* Categories */}
                                    <div className="form-group">
                                        <label style={{
                                            display: 'block',
                                            marginBottom: '8px',
                                            fontWeight: '600',
                                            color: '#374151'
                                        }}>
                                            <Filter className="icon" style={{ display: 'inline', marginRight: '5px' }} />
                                            Question Categories:
                                        </label>
                                        <div style={{ display: 'flex', gap: '8px', marginBottom: '10px' }}>
                                            <button
                                                onClick={() => setSelectedCategories([...availableCategories])}
                                                className="button button-secondary"
                                                style={{ flex: '1', fontSize: '0.875rem', padding: '8px 12px' }}
                                            >
                                                Select All
                                            </button>
                                            <button
                                                onClick={() => setSelectedCategories([])}
                                                className="button button-secondary"
                                                style={{ flex: '1', fontSize: '0.875rem', padding: '8px 12px' }}
                                            >
                                                Deselect All
                                            </button>
                                        </div>
                                        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap' }}>
                                            {availableCategories.map((category) => (
                                                <button
                                                    key={category}
                                                    onClick={() => handleCategoryToggle(category)}
                                                    className={`button ${selectedCategories.includes(category) ? 'button-primary' : 'button-secondary'}`}
                                                    style={{ flex: '1 1 calc(50% - 10px)', minWidth: '120px' }}
                                                >
                                                    {category}
                                                </button>
                                            ))}
                                        </div>
                                        <small style={{
                                            display: 'block',
                                            marginTop: '8px',
                                            color: '#6b7280',
                                            fontSize: '0.875rem'
                                        }}>
                                            Selected: {selectedCategories.length} / {availableCategories.length}
                                        </small>
                                    </div>

                                    {/* Action Buttons */}
                                    <div style={{ display: 'flex', gap: '10px', marginTop: '20px' }}>
                                        <button
                                            onClick={updateGameSettings}
                                            disabled={selectedCategories.length === 0}
                                            className="button button-secondary"
                                            style={{ flex: '1' }}
                                        >
                                            <Settings className="icon" />
                                            Save Settings
                                        </button>
                                        <button
                                            onClick={startGame}
                                            disabled={players.length < 1 || selectedCategories.length === 0}
                                            className="button button-success"
                                            style={{ flex: '1' }}
                                        >
                                            <Play className="icon" />
                                            Start Game
                                        </button>
                                    </div>
                                    {players.length < 1 && (
                                        <small style={{
                                            display: 'block',
                                            marginTop: '10px',
                                            color: '#dc2626',
                                            textAlign: 'center',
                                            fontWeight: '500'
                                        }}>
                                            ⚠️ Need at least 1 player in the lobby to start
                                        </small>
                                    )}
                                    {selectedCategories.length === 0 && (
                                        <small style={{
                                            display: 'block',
                                            marginTop: '10px',
                                            color: '#dc2626',
                                            textAlign: 'center',
                                            fontWeight: '500'
                                        }}>
                                            ⚠️ Please select at least one category!
                                        </small>
                                    )}
                                </div>
                            )}

                            {/* Waiting Message (Non-Host) */}
                            {!isHost && (
                                <div className="section" style={{ textAlign: 'center' }}>
                                    <h3 style={{ color: '#1a202c', marginBottom: '20px' }}>
                                        Waiting for host to start the game...
                                    </h3>
                                    <div style={{
                                        background: '#f7fafc',
                                        padding: '20px',
                                        borderRadius: '8px',
                                        marginTop: '20px'
                                    }}>
                                        <h4 style={{ marginBottom: '15px', color: '#667eea' }}>
                                            Current Settings:
                                        </h4>
                                        <div style={{
                                            display: 'flex',
                                            flexDirection: 'column',
                                            gap: '8px',
                                            textAlign: 'left'
                                        }}>
                                            <div style={{
                                                padding: '8px',
                                                background: 'white',
                                                borderRadius: '6px'
                                            }}>
                                                <strong>Game Mode:</strong> {isTeamMode ? `Team Mode (${numberOfTeams} teams)` : 'Free-for-All'}
                                            </div>
                                            <div style={{
                                                padding: '8px',
                                                background: 'white',
                                                borderRadius: '6px'
                                            }}>
                                                <strong>Max Players:</strong> {maxPlayers}
                                            </div>
                                            <div style={{
                                                padding: '8px',
                                                background: 'white',
                                                borderRadius: '6px'
                                            }}>
                                                <strong>Questions:</strong> {questionsPerGame}
                                            </div>
                                            <div style={{
                                                padding: '8px',
                                                background: 'white',
                                                borderRadius: '6px'
                                            }}>
                                                <strong>Difficulty:</strong> {selectedDifficulty}
                                            </div>
                                            <div style={{
                                                padding: '8px',
                                                background: 'white',
                                                borderRadius: '6px'
                                            }}>
                                                <strong>Categories:</strong> {selectedCategories.join(', ')}
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            )}
                        </div>

                        <div className="section" style={{ marginTop: '24px' }}>
                            <div className="section-header">
                                <Users className="icon" />
                                <h3>Friends</h3>
                            </div>
                            <div className="players-list">
                                {friendsList.length === 0 ? (
                                    <div className="player-item">
                                        <span>No friends available</span>
                                    </div>
                                ) : (
                                    friendsList.map((friend) => {
                                        const status = friend.status ?? friend.Status ?? 'Offline';
                                        const friendId = friend.userId || friend.UserId;
                                        const canInvite = status === 'Online';
                                        return (
                                            <div key={friendId} className="player-item" style={{ gap: 12 }}>
                                                <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                                                    <span>{friend.username || friend.Username}</span>
                                                    <span className={`friend-status friend-status-${status.toLowerCase()}`}>
                                                        {status === 'InGame' ? 'In Game' : status}
                                                    </span>
                                                </div>
                                                <button
                                                    onClick={() => sendGameInvite(friendId)}
                                                    disabled={!canInvite}
                                                    className={`button ${canInvite ? 'button-primary' : 'button-secondary'}`}
                                                    style={{ padding: '8px 14px', minWidth: 90 }}
                                                >
                                                    Invite
                                                </button>
                                            </div>
                                        );
                                    })
                                )}
                            </div>
                        </div>

                        <div style={{
                            marginTop: '20px',
                            textAlign: 'center'
                        }}>
                            <button
                                onClick={leaveGame}
                                className="button button-primary"
                            >
                                <ArrowLeft className="icon" />
                                Back to Menu
                            </button>
                        </div>
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'playing' && currentQuestion) {
        return (
            <>
                <Background />
                {renderIncomingInvites()}
                <div className="container">
                    <div className="card">
                        <div className="question-header">
                            <div className="question-info">
                                <span className="badge badge-purple">Question {currentQuestion.questionNumber}</span>
                                <span className="badge badge-blue">{currentQuestion.category}</span>
                                <span className={`badge badge-${currentQuestion.difficulty.toLowerCase()}`}>
                                    {currentQuestion.difficulty}
                                </span>
                            </div>
                            <div className="timer">
                                <Clock className="icon" />
                                <span>{showAnswer ? 'Time\'s up!' : `${timeLeft}s`}</span>
                            </div>
                        </div>

                        <h3 className="question-text">{currentQuestion.questionText}</h3>

                        <div className="options">
                            {currentQuestion.answerOptions.map((option, index) => {
                                let className = 'option';

                                if (showAnswer) {
                                    if (index === answerResult?.correctAnswer) {
                                        className += ' option-correct';
                                    } else if (index === selectedAnswer) {
                                        className += ' option-incorrect';
                                    }
                                } else if (selectedAnswer === index) {
                                    className += ' option-selected';
                                }

                                return (
                                    <button
                                        key={index}
                                        onClick={() => submitAnswer(index)}
                                        disabled={selectedAnswer !== null || showAnswer}
                                        className={className}
                                    >
                                        {option}
                                    </button>
                                );
                            })}
                        </div>

                        {answerResult && (
                            <div className={`result ${answerResult.result === 'Correct' ? 'result-correct' : 'result-incorrect'}`}>
                                {answerResult.result === 'Correct' ? `Correct! +${answerResult.earnedPoints} points` : 'Incorrect!'}
                            </div>
                        )}

                        {showAnswer && (
                            <div className="answer-section">
                                <div className="leaderboard">
                                    <h4>Leaderboard</h4>
                                    {leaderboard.slice(0, 5).map((player, index) => (
                                        <div key={player.id} className="leaderboard-item">
                                            <span>{index + 1}. {player.name}</span>
                                            <span className="score">{player.score} pts</span>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </>
        );
    }

    if (gameState === 'results') {
        return (
            <>
                <Background />
                {renderIncomingInvites()}
                <div className="container">
                    <div className="card">
                        <div className="header">
                            <Trophy className="icon-large" />
                            <h2>Game Over!</h2>
                        </div>

                        <div className="results">
                            {leaderboard.map((player, index) => {
                                const medals = ['🥇', '🥈', '🥉'];
                                return (
                                    <div key={player.id} className={`result-item ${index < 3 ? `rank-${index + 1}` : ''}`}>
                                        <div className="result-left">
                                            {index < 3 && <span className="medal">{medals[index]}</span>}
                                            <div>
                                                <div className="player-name">{player.name}</div>
                                                <div className="player-stats">{player.correctAnswers} correct answers</div>
                                            </div>
                                        </div>
                                        <div className="final-score">{player.score}</div>
                                    </div>
                                );
                            })}
                        </div>

                        <button
                            onClick={leaveGame}
                            className="button button-primary"
                        >
                            Back to Menu
                        </button>
                    </div>
                </div>
            </>
        );
    }    
    return null;
}

export default function App() {
    const [isLoggedIn, setIsLoggedIn] = useState(false);
    const [username, setUsername] = useState('');

    useEffect(() => {
        const token = localStorage.getItem('token');
        const savedUsername = localStorage.getItem('username');
        if (token && savedUsername) {
            setIsLoggedIn(true);
            setUsername(savedUsername);
        }
    }, []);

    const handleLoginSuccess = (user) => {
        setIsLoggedIn(true);
        setUsername(user);
    };

    const handleLogout = () => {
        localStorage.removeItem('token');
        localStorage.removeItem('username');
        setIsLoggedIn(false);
        setUsername('');
    };

    if (!isLoggedIn) {
        return <Login onLoginSuccess={handleLoginSuccess} />;
    }

    return <TriviaGame username={username} onLogout={handleLogout} />;
}
