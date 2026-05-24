class GameConnection {
    constructor() {
        this.connection = null;
        this.listeners = new Map();
    }

    async connect(url) {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.min.js';
        document.head.appendChild(script);

        return new Promise((resolve) => {
            script.onload = () => {
                const token = localStorage.getItem('token');
                this.connection = new window.signalR.HubConnectionBuilder()
                    .withUrl(url, {
                        accessTokenFactory: () => token || ''
                    })
                    .withAutomaticReconnect()
                    .build();

                this.connection.start()
                    .then(() => resolve(true))
                    .catch(err => {
                        console.error('Connection error:', err);
                        resolve(false);
                    });
            };
        });
    }

    on(event, callback) {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, []);
            this.connection?.on(event, (...args) => {
                this.listeners.get(event).forEach(cb => cb(...args));
            });
        }
        this.listeners.get(event).push(callback);
    }

    off(event, callback) {
        const callbacks = this.listeners.get(event);
        if (callbacks) {
            const index = callbacks.indexOf(callback);
            if (index > -1) callbacks.splice(index, 1);
        }
    }

    async invoke(method, ...args) {
        return this.connection?.invoke(method, ...args);
    }

    async disconnect() {
        if (!this.connection) return;

        try {
            await this.connection.stop();
        } catch (err) {
            console.error('Error stopping connection:', err);
        } finally {
            this.listeners.clear();
            this.connection = null;
        }
    }
}

export default GameConnection
