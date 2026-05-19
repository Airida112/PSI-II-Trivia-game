// ...existing code...
import React, { useState } from 'react';
import './Editor.css';
import LiquidChrome from './LiquidChrome';
import Navbar from './Navbar';

const DifficultyLevel = {
    Easy: "Easy",
    Medium: "Medium",
    Hard: "Hard"
};

// map enum to int
const CategoryMap = {
    Geography: 0,
    History: 1,
    Science: 2,
    Sports: 3,
    Literature: 4,
};
const CategoryMapReverse = Object.fromEntries(Object.entries(CategoryMap).map(([k, v]) => [v, k]));

const DifficultyMap = {
    Easy: 1,
    Medium: 2,
    Hard: 3,
};
const DifficultyMapReverse = Object.fromEntries(Object.entries(DifficultyMap).map(([k, v]) => [v, k]));

const Editor = ({ onHome, onEditor, onLogout, fetchGlobalLeaderboard, onProfileClick }) => {
    const [idInput, setIdInput] = useState('');
    const [current, setCurrent] = useState({
        id: '',
        timeLimit: 20,
        questionText: '',
        answerOptions: ['', '', '', ''],
        correctAnswerIndex: 0,
        category: "Null",
        difficulty: "Null",
    });
    const [message, setMessage] = useState('');

    const parseId = (val) => {
        const n = Number(val);
        return Number.isInteger(n) && n > 0 ? n : null;
    };

    const ensureFourOptions = (opts) => {
        const arr = Array.isArray(opts) ? [...opts] : [];
        while (arr.length < 4) arr.push('');
        return arr.slice(0, 4);
    };

    const API_BASE = '/api/Editor';

    const apiGetQuestion = async (id) => {
        const url = `${API_BASE}/getquestion/${id}`;
        try {
            const res = await fetch(url);
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error(`GET ${url} failed: ${res.status} ${res.statusText}`, text);
                setMessage(text || `GET failed: ${res.status}`);
                return null;
            }
            return await res.json();
        } catch (err) {
            console.error(`GET ${url} network error:`, err);
            setMessage('Network error while getting question');
            return null;
        }
    };

    const apiCreateQuestion = async (payload) => {
        const url = `${API_BASE}/addquestion`;
        try {
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error(`POST ${url} failed: ${res.status} ${res.statusText}`, text);
                setMessage(text || `CREATE failed: ${res.status}`);
                return null;
            }

            const contentType = res.headers.get('content-type') || '';
            if (res.status === 204) {
                return {};
            }
            if (contentType.includes('application/json')) {
                try {
                    return await res.json();
                } catch (err) {
                    console.warn(`POST ${url} returned invalid JSON`, err);
                    return {};
                }
            }
            const text = await res.text().catch(() => '');
            if (!text) return {};
            try {
                return JSON.parse(text);
            } catch {
                return { _raw: text };
            }
        } catch (err) {
            console.error(`POST ${url} network error:`, err);
            setMessage('Network error while creating question');
            return null;
        }
    };

    const apiDeleteQuestion = async (id) => {
        const url = `${API_BASE}/deletequestion/${id}`;
        try {
            const res = await fetch(url, { method: 'DELETE' });
            if (!res.ok) {
                const text = await res.text().catch(() => '');
                console.error(`DELETE ${url} failed: ${res.status} ${res.statusText}`, text);
                setMessage(text || `DELETE failed: ${res.status}`);
                return false;
            }
            return true;
        } catch (err) {
            console.error(`DELETE ${url} network error:`, err);
            setMessage('Network error while deleting question');
            return false;
        }
    };


    const handleGet = async () => {
        const id = parseId(idInput);
        if (!id) { setMessage('Provide a numeric id'); return; }

        setMessage('Fetching...');
        const remote = await apiGetQuestion(id);
        if (remote) {
            setCurrent({ ...remote, answerOptions: ensureFourOptions(remote.answerOptions) });
            setMessage(`Loaded question ${remote.id}`);
        } else {
            clearFields();
        }
    };

    const handleNew = async () => {
        if (!current) { setMessage('No data to create'); return; }

        // DTO matching TriviaQuestionDTO.cs
        const payload = {
            QuestionText: String(current.questionText || ''),
            Answer1: String((current.answerOptions && current.answerOptions[0]) || ''),
            Answer2: String((current.answerOptions && current.answerOptions[1]) || ''),
            Answer3: String((current.answerOptions && current.answerOptions[2]) || ''),
            Answer4: String((current.answerOptions && current.answerOptions[3]) || ''),
            CorrectAnswerIndex: Number(current.correctAnswerIndex) || 0,
            Category: CategoryMap[current.category] ?? 0,
            Difficulty: DifficultyMap[current.difficulty] ?? 1,
            TimeLimit: Number(current.timeLimit) || 30,
        };

        setMessage('Creating on server...');
        const created = await apiCreateQuestion(payload);
        if (created === null) {
            return;
        }

        const resp = { ...payload, ...created };

        clearFields();

        if (resp.id) setIdInput(String(resp.id));
        setMessage(`Created question${resp.id ? ` ${resp.id}` : ''}`);
    };

    const handleDelete = async () => {
        const id = parseId(idInput);
        if (!id) { setMessage('Provide a numeric id'); return; }

        setMessage('Deleting...');
        const ok = await apiDeleteQuestion(id);
        if (ok) {
            clearFields();
            setIdInput('');
            setMessage(`Deleted question ${id}`);
        }
    };

    const clearFields = () => {
        setCurrent({
            id: '',
            timeLimit: 20,
            questionText: '',
            answerOptions: ['', '', '', ''],
            correctAnswerIndex: 0,
            category: "Null",
            difficulty: "Null",
        });
    };

    const updateCurrentField = (field, value) => {
        setCurrent(prev => ({ ...prev, [field]: value }));
    };

    const updateOption = (index, value) => {
        setCurrent(prev => {
            const opts = ensureFourOptions(prev?.answerOptions);
            opts[index] = value;
            return { ...prev, answerOptions: opts };
        });
    };

    return (
        <>
            <Navbar
                onProfileClick={onProfileClick}
                onHome={onHome}
                onEditor={onEditor}
                onFetchGlobalLeaderboard={fetchGlobalLeaderboard}
                onLogout={onLogout}
            />

            <div className="liquid-chrome-background">
                <LiquidChrome
                    baseColor={[0.3, 0.4, 0.5]}
                    speed={0.2}
                    amplitude={0.3}
                    frequencyX={2.5}
                    frequencyY={2.5}
                    interactive={false}
                />
            </div>

            <div className="editor-page">
                <div className="editor-card">
                    <h2 className="editor-title">Question Editor</h2>

                    <div className="controls-row">
                        <div className="id-group">
                            <label className="label-small">Id</label>
                            <input className="input-id" value={idInput} onChange={(e) => setIdInput(e.target.value)} />
                        </div>

                        <div className="inline-buttons">
                            <button type="button" className="btn" onClick={handleGet}>Get</button>
                            <button type="button" className="btn" onClick={handleNew}>New</button>
                            <button type="button" className="btn danger" onClick={handleDelete}>Delete</button>
                        </div>
                    </div>

                    <div className="form-field">
                        <label className="label-small">Time Limit (s)</label>
                        <input className="input-full" type="number" value={current?.timeLimit ?? ''} min={5} onChange={(e) => updateCurrentField('timeLimit', e.target.value)} />
                    </div>

                    <div className="form-field">
                        <label className="label-small">Question Text</label>
                        <textarea className="question-textarea" value={current?.questionText ?? ''} onChange={(e) => updateCurrentField('questionText', e.target.value)} rows={4} />
                    </div>

                    <div className="form-field">
                        <label className="label-small">Answer Options (always 4)</label>
                        <div className="options-list">
                            {Array.from({ length: 4 }).map((_, idx) => {
                                const opt = (current?.answerOptions && current.answerOptions[idx]) ?? '';
                                return (
                                    <div key={idx} className="option-row">
                                        <input className="input-option" value={opt} onChange={(e) => updateOption(idx, e.target.value)} />
                                        <label className="radio-label">
                                            <input type="radio" name="correct" checked={Number(current?.correctAnswerIndex) === idx} onChange={() => updateCurrentField('correctAnswerIndex', idx)} />
                                        </label>
                                    </div>
                                );
                            })}
                        </div>
                    </div>

                    <div className="two-columns">
                        <div className="form-field">
                            <label className="label-small">Category</label>
                            <select className="input-full" value={current?.category ?? "Null"} onChange={(e) => updateCurrentField('category', e.target.value)}>

                                {Object.keys(CategoryMap).map(v => <option key={v} value={v}>{v}</option>)}
                            </select>
                        </div>

                        <div className="form-field">
                            <label className="label-small">Difficulty</label>
                            <select className="input-full" value={current?.difficulty ?? DifficultyLevel.Easy} onChange={(e) => updateCurrentField('difficulty', e.target.value)}>
                                {Object.keys(DifficultyMap).map(v => <option key={v} value={v}>{v}</option>)}
                            </select>
                        </div>
                    </div>

                    <div className="message-row">{message}</div>
                </div>
            </div>
        </>
    );
};

export default Editor;