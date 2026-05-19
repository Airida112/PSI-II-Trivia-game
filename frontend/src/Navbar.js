import React from 'react';
import { Trophy, LogOut, User, Home, Edit } from 'lucide-react';
import './Navbar.css';

const Navbar = ({ onProfileClick, onFetchGlobalLeaderboard, onLogout, onHome, onEditor }) => {
    return (
        <div className="navbar">
            <button
                onClick={onProfileClick}
                style={{
                    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                    border: 'none',
                    width: '40px',
                    height: '40px',
                    borderRadius: '50%',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    cursor: 'pointer',
                    transition: 'transform 0.2s'
                }}
                onMouseEnter={(e) => e.currentTarget.style.transform = 'scale(1.1)'}
                onMouseLeave={(e) => e.currentTarget.style.transform = 'scale(1)'}
                type="button"
            >
                <User style={{ width: '20px', height: '20px', color: 'white' }} />
            </button>

            <div className="navbar-buttons">
                <button onClick={onHome} className="navbar-button navbar-home" type="button">
                    <Home className="icon" />
                    Home
                </button>
                <button onClick={onEditor} className="navbar-button navbar-editor" type="button">
                    <Edit className="icon" />
                    Editor
                </button>
                <button onClick={onFetchGlobalLeaderboard} className="navbar-button navbar-leaderboard" type="button">
                    <Trophy className="icon" />
                    Leaderboard
                </button>
                <button onClick={onLogout} className="navbar-button navbar-logout" type="button">
                    <LogOut className="icon" />
                    Logout
                </button>
            </div>
        </div>
    );
};

export default Navbar;