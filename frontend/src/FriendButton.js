import React from 'react';

const FriendButton = ({
    targetUsername,
    currentUsername,
    relationship = null,
    friendsList = [],
    incomingRequests = [],
    outgoingRequests = [],
    onSend,
    onRespond
}) => {
    if (!targetUsername) return null;
    if (currentUsername && targetUsername.toLowerCase() === currentUsername.toLowerCase()) return null;

    const relationshipStatus = (relationship?.status || relationship?.Status || '').toLowerCase();
    const relationshipId = relationship?.friendshipId || relationship?.FriendshipId || relationship?.FriendshipID || null;

    if (relationshipStatus === 'friend') {
        return <button className="button" disabled style={{ opacity: 0.7 }}>Friends</button>;
    }

    if (relationshipStatus === 'incoming') {
        return (
            <div style={{ display: 'flex', gap: 8 }}>
                <button className="button button-primary" onClick={() => onRespond && onRespond(relationshipId, true)}>Accept</button>
                <button className="button button-secondary" onClick={() => onRespond && onRespond(relationshipId, false)}>Decline</button>
            </div>
        );
    }

    if (relationshipStatus === 'outgoing') {
        return <button className="button" disabled style={{ opacity: 0.7 }}>Pending</button>;
    }

    const isFriend = (friendsList || []).some(f => ((f.username || f.Username) || '').toLowerCase() === (targetUsername || '').toLowerCase());
    if (isFriend) {
        return <button className="button" disabled style={{ opacity: 0.7 }}>Friends</button>;
    }

    const getOtherUsername = (r) => (r.requesterUsername || r.RequesterUsername || r.requesterusername || r.Requesterusername || '');
    const hasOutgoing = (outgoingRequests || []).some(r => getOtherUsername(r).toLowerCase() === (targetUsername || '').toLowerCase());
    const incoming = (incomingRequests || []).find(r => (r.requesterUsername || r.RequesterUsername || '').toLowerCase() === (targetUsername || '').toLowerCase());

    if (incoming) {
        const id = incoming.friendshipId || incoming.FriendshipId || incoming.FriendshipID || incoming.FriendshipId;
        return (
            <div style={{ display: 'flex', gap: 8 }}>
                <button className="button button-primary" onClick={() => onRespond(id, true)}>Accept</button>
                <button className="button button-secondary" onClick={() => onRespond(id, false)}>Decline</button>
            </div>
        );
    }

    if (hasOutgoing) {
        return <button className="button" disabled style={{ opacity: 0.7 }}>Pending</button>;
    }

    return <button className="button button-primary" onClick={() => onSend && onSend(targetUsername)}>Add Friend</button>;
};

export default FriendButton;
