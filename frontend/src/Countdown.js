import React, { useState, useEffect } from 'react';
import './Countdown.css';

const Countdown = ({ onComplete }) => {
    const [count, setCount] = useState(3);

    useEffect(() => {
        if (count === 0) {
            setTimeout(() => {
                onComplete();
            }, 500);
            return;
        }

        const timer = setTimeout(() => {
            setCount(count - 1);
        }, 1000);

        return () => clearTimeout(timer);
    }, [count, onComplete]);

    return (
        <div className="countdown-overlay">
            <div className={`countdown-number ${count === 0 ? 'countdown-go' : ''}`}>
                {count === 0 ? 'GO!' : count}
            </div>
        </div>
    );
};

export default Countdown;