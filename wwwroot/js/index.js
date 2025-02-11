// app.js
document.addEventListener('DOMContentLoaded', function() {
    // État global existant
    const state = {
        platforms: {
            instagram: { 
                running: localStorage.getItem('instagram_running') === 'true', 
                autolike: localStorage.getItem('instagram_autolike') === 'true', 
                automessage: localStorage.getItem('instagram_automessage') === 'true', 
                message: localStorage.getItem('instagram_message') || '' 
            },
            bumble: { 
                running: localStorage.getItem('bumble_running') === 'true',
                autolike: localStorage.getItem('bumble_autolike') === 'true', 
                automessage: localStorage.getItem('bumble_automessage') === 'true', 
                message: localStorage.getItem('bumble_message') || '' 
            },
            happn: { 
                running: localStorage.getItem('happn_running') === 'true',
                autolike: localStorage.getItem('happn_autolike') === 'true', 
                automessage: localStorage.getItem('happn_automessage') === 'true', 
                message: localStorage.getItem('happn_message') || '', 
                token: localStorage.getItem('happn_token') || '' 
            }
        },
        currentPlatform: null
    };

    const credentials = {
        'instagram-username': localStorage.getItem('instagram-username') || '',
        'instagram-password': localStorage.getItem('instagram-password') || '',
        'bumble-cookie': localStorage.getItem('bumble-cookie') || '',
        'happn-token': localStorage.getItem('happn-token') || '',
        'tinder-x-auth-token': localStorage.getItem('tinder-x-auth-token') || '',
        'tinder-user-id': localStorage.getItem('tinder-user-id') || ''
    };

    // Fonction pour charger les credentials sauvegardées
    function loadStoredCredentials() {
        Object.keys(credentials).forEach(fieldId => {
            const input = document.getElementById(fieldId);
            if (input) {
                input.value = credentials[fieldId];
            }
        });
    }

    // Fonction pour sauvegarder une credential
    function saveCredential(fieldId, value) {
        credentials[fieldId] = value;
        localStorage.setItem(fieldId, value);
        
        // If this is the Bumble cookie, ensure it's properly stored
        if (fieldId === 'bumble-cookie') {
            localStorage.setItem('bumble-cookie', value);
        }
    }

    // Fonction pour gérer l'édition d'une credential
    function handleCredentialEdit(event) {
        const button = event.currentTarget;
        const fieldId = button.dataset.field;
        const input = document.getElementById(fieldId);
        
        if (input.disabled) {
            // Activer l'édition
            input.disabled = false;
            input.focus();
            button.innerHTML = `
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
            `;
        } else {
            // Sauvegarder et désactiver l'édition
            input.disabled = true;
            saveCredential(fieldId, input.value);
            button.innerHTML = `
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                </svg>
            `;
        }
    }

    // Ajouter les écouteurs d'événements pour les boutons d'édition
    document.querySelectorAll('.edit-credential').forEach(button => {
        button.addEventListener('click', handleCredentialEdit);
    });

    // Gérer la sauvegarde lors de la perte de focus
    document.querySelectorAll('input[id]').forEach(input => {
        input.addEventListener('blur', (e) => {
            if (!e.target.disabled) {
                const fieldId = e.target.id;
                saveCredential(fieldId, e.target.value);
                e.target.disabled = true;
                
                // Réinitialiser l'icône du bouton d'édition associé
                const editButton = document.querySelector(`button[data-field="${fieldId}"]`);
                if (editButton) {
                    editButton.innerHTML = `
                        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                        </svg>
                    `;
                }
            }
        });
    });

    // Charger les credentials au démarrage
    loadStoredCredentials();

    state.platforms.instagram.hashtag = localStorage.getItem('instagram_hashtag') || '';

    // DOM Elements for the new hashtag modal
    const hashtagModal = document.getElementById('hashtagModal');
    const hashtagInput = document.getElementById('hashtagInput');
    const cancelHashtagBtn = document.getElementById('cancelHashtagBtn');
    const confirmHashtagBtn = document.getElementById('confirmHashtagBtn');

    // Éléments DOM pour la modal
    const messageModal = document.getElementById('messageModal');
    const autoMessageInput = document.getElementById('autoMessageInput');
    const cancelMessageBtn = document.getElementById('cancelMessageBtn');
    const confirmMessageBtn = document.getElementById('confirmMessageBtn');

     // Function to show hashtag modal
    function showHashtagModal(platform) {
        console.log("Showing hashtag modal for platform:", platform);
        state.currentPlatform = platform;
        hashtagInput.value = state.platforms[platform].hashtag || '';
        hashtagModal.classList.add('modal-show');
    }

    // Function to hide hashtag modal
    function hideHashtagModal() {
        hashtagModal.classList.remove('modal-show');
        hashtagInput.value = '';
    }

    // Fonction pour afficher la modal
    function showMessageModal(platform) {
        console.log("Showing modal for platform:", platform); // Debug log
        state.currentPlatform = platform;
        autoMessageInput.value = state.platforms[platform].message || '';
        messageModal.classList.add('modal-show');
    }

    // Fonction pour cacher la modal
    function hideMessageModal() {
        messageModal.classList.remove('modal-show');
        // Do NOT reset currentPlatform here
        autoMessageInput.value = '';
    }

    // DOM Elements
    const startButtons = document.querySelectorAll('.start-button');
    const statusIndicators = document.querySelectorAll('.status-indicator');
    const checkboxes = document.querySelectorAll('input[type="checkbox"]');

    // Constante pour la clé de stockage des logs
    const LOGS_STORAGE_KEY = 'dating_app_logs';
    const MAX_STORED_LOGS = 1000; // Limite le nombre de logs stockés

    // Fonction pour charger les logs existants
    function loadStoredLogs() {
        const storedLogs = localStorage.getItem(LOGS_STORAGE_KEY);
        if (storedLogs) {
            const logs = JSON.parse(storedLogs);
            const consoleOutput = document.getElementById('console-output');
            consoleOutput.innerHTML = ''; // Nettoyer d'abord
            logs.forEach(log => {
                const logEntry = createLogEntry(log.message, log.type);
                consoleOutput.appendChild(logEntry);
            });
            // Auto-scroll vers le bas après chargement
            consoleOutput.scrollTop = consoleOutput.scrollHeight;
        }
    }

    // Fonction pour créer un élément de log
    function createLogEntry(message, type = 'info') {
        const logEntry = document.createElement('div');
        logEntry.className = 'log-entry';
        
        switch(type) {
            case 'error':
                logEntry.className += ' text-red-400';
                break;
            case 'warning':
                logEntry.className += ' text-yellow-400';
                break;
            case 'info':
                logEntry.className += ' text-blue-400';
                break;
            case 'success':
                logEntry.className += ' text-green-400';
                break;
            default:
                logEntry.className += ' text-gray-400';
        }
        
        const timestamp = new Date().toISOString();
        logEntry.textContent = `[${timestamp}] > ${message}`;
        return logEntry;
    }

    // Update platform status indicator
    function updateStatusIndicator(platform, isRunning) {
        const indicator = document.querySelector(`.status-indicator[data-platform="${platform}"]`);
        if (indicator) {
            if (isRunning) {
                indicator.classList.remove('bg-gray-200');
                indicator.classList.add('bg-green-500');
            } else {
                indicator.classList.remove('bg-green-500');
                indicator.classList.add('bg-gray-200');
            }
            
            // Persist running state in localStorage
            localStorage.setItem(`${platform}_running`, isRunning);
        }
    }

    // Handle checkbox changes
    function handleCheckboxChange(event) {
        const platform = event.target.dataset.platform;
        const action = event.target.dataset.action;
        state.platforms[platform][action] = event.target.checked;
        
        // Persist checkbox state in localStorage
        localStorage.setItem(`${platform}_${action}`, event.target.checked);
        
        const switchDiv = event.target.nextElementSibling;
        if (event.target.checked) {
            switchDiv.classList.add('bg-blue-600');
            switchDiv.classList.remove('bg-gray-200');
        } else {
            switchDiv.classList.remove('bg-blue-600');
            switchDiv.classList.add('bg-gray-200');
            
            // If automessage is disabled, clear the saved message
            if (action === 'automessage') {
                state.platforms[platform].message = '';
                localStorage.removeItem(`${platform}_message`);
            }
        }
    }

    // Initialisation des écouteurs d'événements
    document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
        checkbox.addEventListener('change', handleCheckboxChange);
    });

    document.querySelectorAll('.start-button').forEach(button => {
        button.addEventListener('click', (e) => {
            const platform = e.target.dataset.platform;
            togglePlatform(platform);
        });
    });
    
    // Start/Stop platform operations
    // Ajout dans la fonction togglePlatform
    // Modify the togglePlatform function in app.js
    async function togglePlatform(platform) {
        console.log("Attempting to toggle platform:", platform);

        // Validate platform
        if (!platform) {
            console.error("Platform is null or undefined");
            alert("Please select a valid platform");
            return;
        }

        // Validate platform exists in state
        if (!state.platforms.hasOwnProperty(platform)) {
            console.error(`Platform ${platform} not found in state`);
            alert(`Unsupported platform: ${platform}`);
            return;
        }

        const platformState = state.platforms[platform];
        
        if (platformState.running) {
            platformState.running = false;
            updateStatusIndicator(platform, false);
            updateButtonState(platform);
            return;
        }

        // Validation initiale
        if (!platformState.autolike && !platformState.automessage) {
            alert('Please select at least one action (Auto Like or Auto Message)');
            return;
        }

        // Special handling for Instagram - require hashtag
        if (platform === 'instagram' && !platformState.hashtag) {
            showHashtagModal(platform);
            return;
        }

        if (platformState.automessage && !platformState.message) {
            showMessageModal(platform);
            return;
        }

        try {
            platformState.running = true;
            updateStatusIndicator(platform, true);
            updateButtonState(platform);

            let response;
            
            switch (platform) {
                case 'instagram':
                    const instagramUsername = localStorage.getItem('instagram-username');
                    const instagramPassword = localStorage.getItem('instagram-password');
                    
                    if (!instagramUsername || !instagramPassword) {
                        alert('Please set your Instagram credentials first');
                        return;
                    }

                    response = await fetch('/api/dating/instagram/hashtag', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            username: instagramUsername,
                            password: instagramPassword,
                            hashtag: platformState.hashtag,
                            autoLike: platformState.autolike,
                            autoMessage: platformState.automessage,
                            message: platformState.message
                        })
                    });
                    break;

                case 'bumble':
                    const bumbleCookie = localStorage.getItem('bumble-cookie');
                    if (!bumbleCookie) {
                        alert('Please set your Bumble cookie first');
                        return;
                    }
                    
                    response = await fetch('/api/dating/bumble/encounters', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            cookie: bumbleCookie,
                            automessage: platformState.automessage,
                            autolike: platformState.autolike,
                            message: platformState.message
                        })
                    });
                    break;

                case 'happn':
                    const happnToken = localStorage.getItem('happn-token');
                    if (!happnToken) {
                        alert('Please set your Happn token first');
                        platformState.running = false;
                        updateStatusIndicator(platform, false);
                        updateButtonState(platform);
                        return;
                    }
                    
                    response = await fetch('/api/dating/happn/encounters', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            token: happnToken,
                            autoLike: platformState.autolike,
                            autoMessage: platformState.automessage,
                            message: platformState.message
                        })
                    });
                    break;

                default:
                    throw new Error(`Platform ${platform} not supported`);
            }

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            console.log(`${platform} Success:`, data);

        } catch (error) {
            console.error(`Error in ${platform} operation:`, error);
            platformState.running = false;
            updateStatusIndicator(platform, false);
            updateButtonState(platform);
            
            // Afficher une erreur à l'utilisateur
            alert(`Une erreur est survenue avec ${platform}: ${error.message}`);
        }
    }

    // Event listeners for hashtag modal
    cancelHashtagBtn.addEventListener('click', hideHashtagModal);

    confirmHashtagBtn.addEventListener('click', () => {
        const hashtag = hashtagInput.value.trim();
        const currentPlatform = state.currentPlatform;

        if (hashtag && currentPlatform) {
            if (state.platforms.hasOwnProperty(currentPlatform)) {
                state.platforms[currentPlatform].hashtag = hashtag;
                localStorage.setItem(`${currentPlatform}_hashtag`, hashtag);
                hideHashtagModal();
                
                // Proceed with original platform toggle logic
                togglePlatform(currentPlatform);
            } else {
                console.error(`Platform ${currentPlatform} not found in state`);
                alert('Invalid platform selected');
            }
        } else {
            alert('Please enter a hashtag');
        }
    });

    // Prevent modal from closing when clicking outside for hashtag modal
    hashtagModal.addEventListener('click', (e) => {
        if (e.target === hashtagModal) {
            hideHashtagModal();
        }
    });

    // Gestionnaires d'événements pour la modal
    cancelMessageBtn.addEventListener('click', hideMessageModal);

    confirmMessageBtn.addEventListener('click', () => {
        const message = autoMessageInput.value.trim();
        const currentPlatform = state.currentPlatform; // Store platform before hiding modal

        if (message && currentPlatform) {
            if (state.platforms.hasOwnProperty(currentPlatform)) {
                state.platforms[currentPlatform].message = message;
                hideMessageModal();
                
                console.log("Calling togglePlatform with:", currentPlatform);
                togglePlatform(currentPlatform);
            } else {
                console.error(`Platform ${currentPlatform} not found in state`);
                alert('Invalid platform selected');
            }
        } else {
            alert('Please enter a message');
        }
    });

    // Empêcher la fermeture de la modal en cliquant à l'extérieur
    messageModal.addEventListener('click', (e) => {
        if (e.target === messageModal) {
            hideMessageModal();
        }
    });

    // Ajout de l'écouteur pour les boutons Stop
    document.querySelectorAll('.stop-button').forEach(button => {
        button.addEventListener('click', (e) => {
            const platform = e.target.dataset.platform;
            stopPlatform(platform);
        });
    });

    // Nouvelle fonction pour arrêter une plateforme
    async function stopPlatform(platform) {
        try {
            const response = await fetch('/api/dating/stop', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ platform: platform })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            // Correction ici : utiliser state.platforms au lieu de this.state.platforms
            const platformState = state.platforms[platform];
            platformState.running = false;
            updateStatusIndicator(platform, false);
            updateButtonState(platform);
        } catch (error) {
            console.error(`Error stopping ${platform}:`, error);
            alert(`Impossible d'arrêter ${platform}: ${error.message}`);
        }
    }

    // Update button state
    function updateButtonState(platform) {
        const button = document.querySelector(`button[data-platform="${platform}"]`);
        const platformState = state.platforms[platform];
        
        if (button) {
            button.disabled = platformState.running;
            button.textContent = platformState.running ? 'Running...' : 'Start';
            
            if (platformState.running) {
                button.classList.add('opacity-50', 'cursor-not-allowed');
            } else {
                button.classList.remove('opacity-50', 'cursor-not-allowed');
            }
        }
    }

    // Event Listeners
    document.querySelectorAll('input[type="checkbox"]').forEach(checkbox => {
        checkbox.addEventListener('change', handleCheckboxChange);
    });

    startButtons.forEach(button => {
        button.addEventListener('click', (e) => {
            const platform = e.target.dataset.platform;
            togglePlatform(platform);
        });
    });

    const connection = new signalR.HubConnectionBuilder()
    .withUrl("/loghub")
    .withAutomaticReconnect([0, 2000, 5000, 10000, 20000]) // Tentatives de reconnexion avec délais croissants
    .configureLogging(signalR.LogLevel.Information)
    .build();

    let isConnecting = false;

        // Fonction pour ajouter un nouveau log
        function addLog(message, type = 'info') {
            const consoleOutput = document.getElementById('console-output');
            const logEntry = createLogEntry(message, type);
            consoleOutput.appendChild(logEntry);
            
            // Récupérer et mettre à jour les logs stockés
            let storedLogs = JSON.parse(localStorage.getItem(LOGS_STORAGE_KEY) || '[]');
            storedLogs.push({
                message,
                type,
                timestamp: new Date().toISOString()
            });

            // Limiter le nombre de logs stockés
            if (storedLogs.length > MAX_STORED_LOGS) {
                storedLogs = storedLogs.slice(-MAX_STORED_LOGS);
            }
            
            // Sauvegarder dans le stockage local
            localStorage.setItem(LOGS_STORAGE_KEY, JSON.stringify(storedLogs));
            
            // Auto-scroll vers le bas
            consoleOutput.scrollTop = consoleOutput.scrollHeight;
        }

        // Configurer les gestionnaires d'événements SignalR
        connection.on("ReceiveLog", (message, logLevel) => {
            console.log("Log reçu:", message, logLevel); // Pour debug
            addLog(message, logLevel.toLowerCase());
        });

        // Démarrer la connexion
        async function startConnection() {
            try {
                if (isConnecting) {
                    console.log("Connection attempt already in progress");
                    return;
                }

                if (connection.state === signalR.HubConnectionState.Connected) {
                    console.log("Already connected");
                    return;
                }

                isConnecting = true;
                await connection.start();
                console.log("SignalR Connected.");
                addLog("Connected to server", "success");
                isConnecting = false;
            } catch (err) {
                console.log("SignalR Connection Error: ", err);
                addLog("Connection error: " + err.message, "error");
                isConnecting = false;
                
                // Attendre avant de retenter la connexion
                setTimeout(() => {
                    if (connection.state !== signalR.HubConnectionState.Connected) {
                        startConnection();
                    }
                }, 5000);
            }
        }

        // Gérer la déconnexion
        connection.onclose(async (error) => {
            addLog("Connection lost. Attempting to reconnect...", "warning");
            if (error) {
                addLog(`Connection closed due to error: ${error}`, "error");
            }
            
            // La reconnexion automatique sera gérée par withAutomaticReconnect
        });

        // Démarrer la connexion au chargement
        startConnection();

        // Function pour envoyer une requête au endpoint Bumble
        async function startBumbleEncounters() {
            try {
                const response = await fetch('/api/Dating/bumble/encounters', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    }
                });
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                
                const data = await response.json();
                console.log("Response:", data);
            } catch (error) {
                console.error("Error:", error);
                addLog("Error: " + error.message, "error");
            }
        } 

        function clearLogs() {
            document.getElementById('console-output').innerHTML = '';
            localStorage.removeItem(LOGS_STORAGE_KEY);
            addLog("Console cleared", "info");
        }

    // Gérer la déconnexion
    connection.onclose(async (error) => {
        addLog("Connection lost. Attempting to reconnect...", "warning");
        if (error) {
            addLog(`Connection closed due to error: ${error}`, "error");
        }
        
        // La reconnexion automatique sera gérée par withAutomaticReconnect
    });

    // Gérer les événements de reconnexion
    connection.onreconnecting((error) => {
        addLog("Attempting to reconnect to server...", "warning");
        if (error) {
            addLog(`Reconnection error: ${error}`, "error");
        }
    });

    connection.onreconnected((connectionId) => {
        addLog("Successfully reconnected to server.", "success");
    });    

    // Initialize app
    function initializeApp() {
        
        // Charger les credentials sauvegardées
        const happnToken = localStorage.getItem('happn-token');
        if (happnToken) {
            state.platforms.happn.token = happnToken;
        }

        Object.keys(state.platforms).forEach(platform => {
            const platformState = state.platforms[platform];
            
            // Restore running state
            updateStatusIndicator(platform, platformState.running);
            
            const instagramHashtag = localStorage.getItem('instagram_hashtag');
            if (instagramHashtag) {
                state.platforms.instagram.hashtag = instagramHashtag;
            }

            // Restore checkbox states
            ['autolike', 'automessage'].forEach(action => {
                const checkbox = document.querySelector(`input[data-platform="${platform}"][data-action="${action}"]`);
                if (checkbox) {
                    checkbox.checked = platformState[action];
                    const switchDiv = checkbox.nextElementSibling;
                    if (platformState[action]) {
                        switchDiv.classList.add('bg-blue-600');
                        switchDiv.classList.remove('bg-gray-200');
                    } else {
                        switchDiv.classList.remove('bg-blue-600');
                        switchDiv.classList.add('bg-gray-200');
                    }
                }
            });
            
            updateButtonState(platform);
        });

        loadStoredLogs();
        
        if (connection.state !== signalR.HubConnectionState.Connected) {
            startConnection();
        }
    }

    // Initialize the application
    initializeApp();
    
    // Ajouter l'écouteur d'événements pour le bouton clear
    document.querySelector('[onclick="clearLogs()"]')?.addEventListener('click', clearLogs);
});