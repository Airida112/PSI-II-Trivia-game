import React, { useState, useEffect } from 'react';
import { User, Trophy, Target, Calendar, TrendingUp, Award, ArrowLeft, Settings } from 'lucide-react';
import './Profile.css';
import LiquidChrome from './LiquidChrome';

const Profile = ({ username, onBack }) => {
    const [userData, setUserData] = useState(null);
    const [currentUser, setCurrentUser] = useState(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        const fetchUserData = async () => {
            try {
                const response = await fetch(`/api/leaderboard/rank/${username}`);
                if (response.ok) {
                    const data = await response.json();
                    setUserData(data);
                } else {
                    console.error('Failed to fetch user data');
                }
            } catch (error) {
                console.error('Error fetching user data:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchUserData();
    }, [username]);

    // Fetch current user for clan operations
    useEffect(() => {
        const fetchCurrentUser = async () => {
            if (!username) return;
            try {
                const res = await fetch(`/api/clan/getuser/${encodeURIComponent(username)}`);
                if (res.ok) {
                    const u = await res.json();
                    setCurrentUser(u);
                } else {
                    console.error('Failed to fetch current user for clan info');
                }
            } catch (err) {
                console.error('Network error loading current user:', err);
            }
        };
        fetchCurrentUser();
    }, [username]);

    if (loading) {
        return (
            <>
                <div className="liquid-chrome-background">
                    <LiquidChrome
                        baseColor={[0.4, 0.5, 0.9]}
                        speed={0.5}
                        amplitude={0.6}
                        frequencyX={3}
                        frequencyY={3}
                        interactive={false}
                    />
                </div>
                <div className="container">
                    <div className="loading">Loading profile...</div>
                </div>
            </>
        );
    }

    // If userData failed to load, render the normal dashboard with safe defaults
    const safeUserData = userData ?? {
        username: username || 'Unknown',
        gamesPlayed: 0,
        totalPoints: 0,
        elo: 0,
        rank: 0,
        totalPlayers: 0
    };

    const stats = [
        { label: "Games Played", value: safeUserData.gamesPlayed, icon: Target, color: "#3b82f6" },
        { label: "Total Points", value: safeUserData.totalPoints?.toLocaleString?.() ?? String(safeUserData.totalPoints || 0), icon: Trophy, color: "#f59e0b" },
        { label: "Current ELO", value: safeUserData.elo, icon: TrendingUp, color: "#10b981" },
        { label: "Global Rank", value: `#${safeUserData.rank}`, icon: Award, color: "#667eea" }
    ];

    return (
        <>
            <div className="liquid-chrome-background">
                <LiquidChrome
                    baseColor={[0.4, 0.5, 0.9]}
                    speed={0.5}
                    amplitude={0.6}
                    frequencyX={3}
                    frequencyY={3}
                    interactive={false}
                />
            </div>
            <div className="container" style={{ paddingTop: '20px' }}>
                <div className="card" style={{ maxWidth: '800px' }}>
                    {/* Header */}
                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '30px' }}>
                        <button
                            onClick={onBack}
                            className="button button-secondary"
                            style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
                        >
                            <ArrowLeft className="icon" />
                            Back
                        </button>
                    </div>

                    {/* Profile Card */}
                    <div style={{
                        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                        padding: '30px',
                        borderRadius: '12px',
                        marginBottom: '24px',
                    
                        color: 'white'
                    }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
                            {/* Avatar */}
                            <div style={{
                                width: '80px',
                                height: '80px',
                                background: 'rgba(255, 255, 255, 0.2)',
                                borderRadius: '50%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                border: '3px solid rgba(255, 255, 255, 0.3)'
                            }}>
                                <User style={{ width: '40px', height: '40px' }} />
                            </div>

                            {/* User Info */}
                            <div style={{ flex: 1 }}>
                                <h1 style={{ margin: '0 0 8px 0', fontSize: '32px' }}>{safeUserData.username}</h1>
                                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', opacity: 0.9 }}>
                                    <Calendar style={{ width: '16px', height: '16px' }} />
                                    <span>Total Players: {safeUserData.totalPlayers}</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Stats Grid */}
                    <div style={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
                        gap: '16px',
                        marginBottom: '24px'
                    }}>
                        {stats.map((stat, index) => {
                            const Icon = stat.icon;
                            return (
                                <div
                                    key={index}
                                    style={{
                                        background: '#f7fafc',
                                        padding: '20px',
                                        borderRadius: '12px',
                                        border: '2px solid #e2e8f0',
                                        transition: 'all 0.2s'
                                    }}
                                    onMouseEnter={(e) => {
                                        e.currentTarget.style.borderColor = stat.color;
                                        e.currentTarget.style.transform = 'translateY(-2px)';
                                    }}
                                    onMouseLeave={(e) => {
                                        e.currentTarget.style.borderColor = '#e2e8f0';
                                        e.currentTarget.style.transform = 'translateY(0)';
                                    }}
                                >
                                    <Icon style={{ width: '32px', height: '32px', color: stat.color, marginBottom: '12px' }} />
                                    <div style={{ fontSize: '28px', fontWeight: 'bold', color: '#1a202c', marginBottom: '4px' }}>
                                        {stat.value}
                                    </div>
                                    <div style={{ fontSize: '14px', color: '#718096' }}>
                                        {stat.label}
                                    </div>
                                </div>
                            );
                        })}
                    </div>

                    {/* Performance Details */}
                    <div style={{
                        background: '#f7fafc',
                        padding: '24px',
                        borderRadius: '12px',
                        border: '2px solid #e2e8f0'
                    }}>
                        <h3 style={{ margin: '0 0 20px 0', color: '#1a202c', fontSize: '20px' }}>
                            Performance Details
                        </h3>
                        <div style={{
                            display: 'grid',
                            gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                            gap: '12px'
                        }}>
                            <div style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                padding: '12px',
                                background: 'white',
                                borderRadius: '8px'
                            }}>
                                <span style={{ color: '#718096' }}>Average Score</span>
                                <span style={{ fontWeight: 'bold', color: '#1a202c' }}>
                                    {safeUserData.gamesPlayed > 0
                                        ? Math.round((safeUserData.totalPoints || 0) / safeUserData.gamesPlayed)
                                        : 0
                                    }
                                </span>
                            </div>
                            <div style={{
                                display: 'flex',
                                justifyContent: 'space-between',
                                padding: '12px',
                                background: 'white',
                                borderRadius: '8px'
                            }}>
                                <span style={{ color: '#718096' }}>Rank Position</span>
                                <span style={{ fontWeight: 'bold', color: '#667eea' }}>
                                    Top {safeUserData.totalPlayers > 0
                                        ? Math.round((safeUserData.rank / safeUserData.totalPlayers) * 100)
                                        : 0
                                    }%
                                </span>
                            </div>
                        </div>
                    </div>
                    {/* Clan Section */}
                    <div style={{
                        background: '#f7fafc',
                        padding: '24px',
                        borderRadius: '12px',
                        border: '2px solid #e2e8f0',
                        marginTop: '16px'
                    }}>
                        <h3 style={{ margin: '0 0 12px 0', color: '#1a202c', fontSize: '20px' }}>Join a clan</h3>
                        <ClanSection username={username} />

                    </div>
                    {/* Clan Admin */}
                    <div style={{
                        background: '#f7fafc',
                        padding: '16px',
                        borderRadius: '12px',
                        border: '2px solid #e2e8f0',
                        marginTop: '12px'
                    }}>
                        <h4 style={{ margin: '0 0 8px 0', color: '#1a202c' }}>Clan Admin</h4>
                        <ClanAdmin userId={currentUser?.id} />
                    </div>
                </div>
            </div>
        </>
    );
};

const ClanAdmin = ({ userId }) => {
    const API_BASE = '/api/clan';
    const [createName, setCreateName] = useState('');
    const [renameId, setRenameId] = useState('');
    const [renameName, setRenameName] = useState('');
    const [deleteId, setDeleteId] = useState('');
    const [msg, setMsg] = useState('');

    const handleCreate = async () => {
        if (!createName) return setMsg('Provide a name');
        if (!userId) return setMsg('User ID not available');
        try {
            const res = await fetch(`${API_BASE}/create/?clanName=${encodeURIComponent(createName)}&userId=${userId}`, {
                method: 'POST',
            });
            if (!res.ok) {
                const t = await res.text().catch(() => '');
                console.error('Create failed', res.status, t);
                setMsg(t || `Create failed: ${res.status}`);
                return;
            }
            setMsg('Clan created');
            setCreateName('');
        } catch (err) {
            console.error('Network error create clan', err);
            setMsg('Network error');
        }
    };

    const handleRename = async () => {
        const id = Number(renameId);
        if (!id || !renameName) return setMsg('Provide id and new name');
        if (!userId) return setMsg('User ID not available');
        try {
            const res = await fetch(`${API_BASE}/rename?clanId=${id}&newName=${encodeURIComponent(renameName)}&userId=${userId}`,
            {
                method: 'PATCH'
            });
            if (!res.ok) {
                const t = await res.text().catch(() => '');
                console.error('Rename failed', res.status, t);
                setMsg(t || `Rename failed: ${res.status}`);
                return;
            }
            setMsg('Clan renamed');
            setRenameId(''); setRenameName('');
        } catch (err) {
            console.error('Network error rename clan', err);
            setMsg('Network error');
        }
    };

    const handleDelete = async () => {
        const id = Number(deleteId);
        if (!id) return setMsg('Provide id');
        if (!userId) return setMsg('User ID not available');
        try {
            const res = await fetch(`${API_BASE}/delete?clanId=${id}&userId=${userId}`, { method: 'DELETE' });
            if (!res.ok) {
                const t = await res.text().catch(() => '');
                console.error('Delete failed', res.status, t);
                setMsg(t || `Delete failed: ${res.status}`);
                return;
            }
            setMsg('Clan deleted');
            setDeleteId('');
        } catch (err) {
            console.error('Network error delete clan', err);
            setMsg('Network error');
        }
    };

    return (
        <div style={{ display: 'grid', gap: 8 }}>
            <div style={{ display: 'flex', gap: 8 }}>
                <input
                    value={createName}
                    onChange={(e) => setCreateName(e.target.value)}
                    placeholder="New clan name"
                    style={{ flex: 1, padding: 8, borderRadius: 6, border: 'none', outline: 'none', background: 'white' }}
                />
                <button
                    type="button"
                    className="button"
                    onClick={handleCreate}
                    style={{ background: '#10b981', color: 'white', border: 'none' }}
                >
                    Create
                </button>
            </div>

            <div style={{ display: 'flex', gap: 8 }}>
                <input
                    value={renameId}
                    onChange={(e) => setRenameId(e.target.value)}
                    placeholder="Clan id"
                    style={{ width: 100, padding: 8, borderRadius: 6, border: 'none', outline: 'none', background: 'white' }}
                />
                <input
                    value={renameName}
                    onChange={(e) => setRenameName(e.target.value)}
                    placeholder="New name"
                    style={{ flex: 1, padding: 8, borderRadius: 6, border: 'none', outline: 'none', background: 'white' }}
                />
                <button
                    type="button"
                    className="button"
                    onClick={handleRename}
                    style={{ background: '#f58d16ff', color: 'white', border: 'none' }}
                >
                    Rename
                </button>
            </div>

            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input
                    value={deleteId}
                    onChange={(e) => setDeleteId(e.target.value)}
                    placeholder="Clan id"
                    style={{ width: 140, padding: 8, borderRadius: 6, border: 'none', outline: 'none', background: 'white' }}
                />
                <button
                    type="button"
                    className="button button-danger"
                    onClick={handleDelete}
                    style={{ background: '#ef4444', color: 'white', border: 'none' }}
                >
                    Delete
                </button>
            </div>

            <div style={{ color: '#333', minHeight: 18 }}>{msg}</div>
        </div>
    );
};

const ClanSection = ({ username }) => {
    const [clan, setClan] = useState(null);
    const [members, setMembers] = useState([]);
    const [joinName, setJoinName] = useState('');
    const [loading, setLoading] = useState(false);
    const [currentUser, setCurrentUser] = useState(null);

    const API_BASE = '/api/clan';

    // load current user and their clan if exists
    useEffect(() => {
        const load = async () => {
            if (!username) return;
            setLoading(true);
            try {
                const uRes = await fetch(`${API_BASE}/getuser/${encodeURIComponent(username)}`);
                if (!uRes.ok) {
                    console.error('Failed to fetch current user for clan info');
                    setLoading(false);
                    return;
                }
                const u = await uRes.json();
                setCurrentUser(u);

                const clanId = u.clanId ?? null;
                if (!clanId) {
                    setClan(null);
                    setMembers([]);
                    setLoading(false);
                    return;
                }

                const cRes = await fetch(`${API_BASE}/getclan/${clanId}`);
                if (cRes.ok) {
                    const c = await cRes.json();
                    setClan(c);
                } else {
                    console.error('Failed to fetch clan');
                }

                const mRes = await fetch(`${API_BASE}/getmembers/${clanId}`);
                if (mRes.ok) {
                    const m = await mRes.json();
                    try {
                        const detailed = await Promise.all((m || []).map(async (mem) => {
                            const userId = mem.id ?? mem.userId ?? mem;
                            try {
                                const uRes2 = await fetch(`${API_BASE}/getuser/${encodeURIComponent(userId)}`);
                                if (uRes2.ok) return await uRes2.json();
                                return mem;
                            } catch (err) {
                                console.error('Error fetching user', userId, err);
                                return mem;
                            }
                        }));
                        setMembers(detailed);
                    } catch (err) {
                        console.error('Error resolving member details', err);
                        setMembers(m || []);
                    }
                } else {
                    console.error('Failed to fetch clan members');
                }
            } catch (err) {
                console.error('Network error loading clan:', err);
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [username]);

    // Join by clan name: look up clanId then call join endpoint
    const handleJoinByName = async () => {
        if (!joinName || !currentUser) return;
        setLoading(true);
        try {
            // lookup clan by name
            const lookupRes = await fetch(`${API_BASE}/getclanbyname/${encodeURIComponent(joinName)}`);
            if (!lookupRes.ok) {
                const text = await lookupRes.text().catch(() => '');
                console.error('Lookup clan by name failed:', lookupRes.status, text);
                setLoading(false);
                return;
            }
            const found = await lookupRes.json().catch(() => null);
            const clanId = found && (found.id ?? found.Id);
            if (!clanId) {
                console.error('Clan not found by name');
                setLoading(false);
                return;
            }

            const userId = currentUser.id || currentUser.userId;
            const joinRes = await fetch(`${API_BASE}/join/${clanId}?userId=${userId}`, {
                method: 'POST'
            });
            if (!joinRes.ok) {
                const text = await joinRes.text().catch(() => '');
                console.error('Join clan failed:', joinRes.status, text);
                setLoading(false);
                return;
            }

            // refresh clan info
            const cRes = await fetch(`${API_BASE}/getclan/${clanId}`);
            if (cRes.ok) setClan(await cRes.json());
            const mRes = await fetch(`${API_BASE}/getmembers/${clanId}`);
            if (mRes.ok) {
                const m = await mRes.json();
                try {
                    const detailed = await Promise.all((m || []).map(async (mem) => {
                        const userId2 = mem.id ?? mem.userId ?? mem;
                        try {
                            const uRes2 = await fetch(`${API_BASE}/getuser/${encodeURIComponent(userId2)}`);
                            if (uRes2.ok) return await uRes2.json();
                            return mem;
                        } catch (err) {
                            console.error('Error fetching user', userId2, err);
                            return mem;
                        }
                    }));
                    setMembers(detailed);
                } catch (err) {
                    console.error('Error resolving member details', err);
                    setMembers(m || []);
                }
            }

            setJoinName('');
        } catch (err) {
            console.error('Network error joining clan by name:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleLeave = async () => {
        if (!clan || !currentUser) return;
        setLoading(true);
        try {
            const userId = currentUser.id || currentUser.userId || currentUser.username;
            const res = await fetch(`${API_BASE}/leave/${clan.id}?userId=${encodeURIComponent(userId)}`, {
                method: 'DELETE'
            });
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error('Leave clan failed:', res.status, text);
                setLoading(false);
                return;
            }
            setClan(null);
            setMembers([]);
        } catch (err) {
            console.error('Network error leaving clan:', err);
        } finally {
            setLoading(false);
        }
    };

    if (loading) return <div>Loading...</div>;

    // Kick a member (POST with query params)
    const handleKick = async (kickeeId) => {
        if (!currentUser || !kickeeId) return;
        if (kickeeId === (currentUser.id || currentUser.userId)) return; // don't kick self
        setLoading(true);
        try {
            const adminId = currentUser.id || currentUser.userId;
            const res = await fetch(`${API_BASE}/kick?kickeeId=${encodeURIComponent(kickeeId)}&adminId=${encodeURIComponent(adminId)}`, {
                method: 'POST'
            });
            if (!res.ok) {
                const t = await res.text().catch(() => '');
                console.error('Kick failed', res.status, t);
                setLoading(false);
                return;
            }
            // refresh members
            if (clan && clan.id) {
                const mRes = await fetch(`${API_BASE}/getmembers/${clan.id}`);
                if (mRes.ok) {
                    const m = await mRes.json();
                    try {
                        const detailed = await Promise.all((m || []).map(async (mem) => {
                            const userId = mem.id ?? mem.userId ?? mem;
                            try {
                                const uRes2 = await fetch(`${API_BASE}/getuser/${encodeURIComponent(userId)}`);
                                if (uRes2.ok) return await uRes2.json();
                                return mem;
                            } catch (err) {
                                console.error('Error fetching user', userId, err);
                                return mem;
                            }
                        }));
                        setMembers(detailed);
                    } catch (err) {
                        console.error('Error resolving member details', err);
                        setMembers(m || []);
                    }
                }
            }
        } catch (err) {
            console.error('Network error kicking user:', err);
        } finally {
            setLoading(false);
        }
    };

    if (clan) {
        return (
            <div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                        <div style={{ fontWeight: '600', fontSize: '18px' }}>{clan.name}</div>
                        <div style={{ color: '#718096' }}>{clan.memberCount} members</div>
                    </div>
                    <div>
                        <button type="button" className="button button-danger" onClick={handleLeave}>Leave</button>
                    </div>
                </div>

                <div style={{ marginTop: '12px' }}>
                    <div style={{ fontWeight: 600, marginBottom: '8px' }}>Members</div>
                    <ul style={{ margin: 0, paddingLeft: '0' }}>
                        {members.map((m) => {
                            const id = m.id || m.userId || m;
                            const name = m.username || m.userName || id;
                            return (
                                <li key={id} style={{ listStyle: 'none', padding: '6px 8px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', borderRadius: 6 }}>
                                    <span>{name}</span>
                                    <div>
                                        {id !== (currentUser.id || currentUser.userId) && (
                                            <button type="button" class="button" onClick={() => handleKick(id)} style={{ background: '#ef4444', color: 'white', border: 'none', padding: '6px 10px', borderRadius: 6 }}>Kick</button>
                                        )}
                                    </div>
                                </li>
                            );
                        })}
                    </ul>
                </div>
            </div>
        );
    }

    return (
        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
            <input value={joinName} onChange={(e) => setJoinName(e.target.value)} placeholder="Clan name" style={{ flex: 1, padding: '8px', borderRadius: '8px', border: '1px solid #e5e7eb' }} />
            <button type="button" className="button button-primary" onClick={handleJoinByName}>Join</button>
        </div>
    );
};


export default Profile;