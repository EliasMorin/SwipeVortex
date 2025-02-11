document.addEventListener('DOMContentLoaded', function () {
    let data = null;
    const loadingDiv = document.createElement('div');
    loadingDiv.className = 'text-center p-4';
    loadingDiv.textContent = 'Chargement des données...';
    document.querySelector('.container').appendChild(loadingDiv);

    fetch('/api/happn/stats')
        .then(response => {
            console.log('Response status:', response.status);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(jsonData => {
            console.log('Données API complètes :', jsonData);
            
            data = jsonData || {};  // Ensure data is at least an empty object
            loadingDiv.remove();
            
            // Log les données spécifiques pour vérification
            console.log('Encounters:', data.Encounters);
            console.log('TotalLikes:', data.TotalLikes);
            console.log('TotalMatches:', data.TotalMatches);
            console.log('TotalMessagesSent:', data.TotalMessagesSent);
            console.log('TotalConversations:', data.TotalConversations);
            console.log('TotalCrushes:', data.TotalCrushes);
            
            updateDashboard(data);
        })
        .catch(error => {
            console.error('Erreur complète :', error);
            loadingDiv.remove();
            showError(error.message);
        });
});

function showError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded relative m-4';
    errorDiv.innerHTML = `
        <strong class="font-bold">Erreur de chargement :</strong>
        <span class="block sm:inline">${message}</span>
    `;
    document.querySelector('.container').appendChild(errorDiv);
}

function updateDashboard(data = {}) {
    console.log('Mise à jour du dashboard avec :', data);

    // Utilisez encounters (minuscules) au lieu de Encounters
    const encounters = data.encounters || [];

    // Update statistics cards
    try {
        document.getElementById('totalRecommendations').textContent = data.totalLikes ?? '0';
        document.getElementById('totalConversations').textContent = data.totalConversations ?? '0';
        document.getElementById('totalCrushes').textContent = data.totalCrushes ?? '0';
        document.getElementById('activeConversations').textContent = data.totalMessagesSent ?? '0';
    } catch (error) {
        console.error('Erreur lors de la mise à jour des statistiques :', error);
    }

    // Render charts
    try {
        renderAgeDistribution(prepareAgeDistribution(encounters));
        renderCityDistribution(prepareCityDistribution(encounters));
        renderActivityTimeline(prepareActivityTimeline(encounters));
        renderMessagesByDay(prepareMessagesByDay(encounters));
    } catch (error) {
        console.error('Erreur lors du rendu des graphiques :', error);
    }

    // Update recommendations table
    try {
        updateRecommendationsTable(encounters);
    } catch (error) {
        console.error('Erreur lors de la mise à jour du tableau des recommandations :', error);
    }
}

// Fonctions de préparation des données avec des logs
function prepareAgeDistribution(encounters = []) {
    console.log('Préparation de la distribution des âges avec :', encounters);
    const ageDistribution = encounters.reduce((acc, enc) => {
        if (enc && enc.age) {
            acc[enc.age] = (acc[enc.age] || 0) + 1;
        }
        return acc;
    }, {});
    console.log('Distribution des âges :', ageDistribution);
    return ageDistribution;
}

function prepareCityDistribution(encounters = []) {
    console.log('Préparation de la distribution des villes avec :', encounters);
    const cityDistribution = encounters.reduce((acc, enc) => {
        if (enc && enc.residenceCity && enc.residenceCity !== 'Unknown') {
            acc[enc.residenceCity] = (acc[enc.residenceCity] || 0) + 1;
        }
        return acc;
    }, {});
    console.log('Distribution des villes :', cityDistribution);
    return cityDistribution;
}

function prepareActivityTimeline(encounters = []) {
    console.log('Préparation de la timeline d\'activité avec :', encounters);
    const activityTimeline = encounters.reduce((acc, enc) => {
        if (enc && enc.date) {
            const hour = new Date(enc.date).getHours();
            acc[hour] = (acc[hour] || 0) + 1;
        }
        return acc;
    }, {});
    console.log('Timeline d\'activité :', activityTimeline);
    return activityTimeline;
}

function prepareMessagesByDay(encounters = []) {
    console.log('Préparation des messages par jour avec :', encounters);
    const messagesByDay = {};
    const today = new Date();
    
    // Initialize last 7 days
    for (let i = 6; i >= 0; i--) {
        const date = new Date();
        date.setDate(date.getDate() - i);
        messagesByDay[date.toISOString().split('T')[0]] = 0;
    }

    // Count messages for each day
    encounters.forEach(enc => {
        if (enc && enc.date) {
            const dateKey = new Date(enc.date).toISOString().split('T')[0];
            if (messagesByDay.hasOwnProperty(dateKey)) {
                messagesByDay[dateKey]++;
            }
        }
    });

    console.log('Messages par jour :', messagesByDay);
    return messagesByDay;
}

function updateRecommendationsTable(recommendations = []) {
    console.log('Mise à jour du tableau des recommandations avec :', recommendations);
    const tbody = document.getElementById('recommendationsTable');
    if (!tbody) return;

    try {
        tbody.innerHTML = recommendations
            .sort((a, b) => new Date(b.date) - new Date(a.date))
            .slice(0, 10)
            .map(rec => `
                <tr class="hover:bg-gray-50">
                    <td class="px-6 py-4 whitespace-nowrap">
                        ${rec?.firstName || '-'}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                        ${rec?.age || '-'}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                        ${rec?.residenceCity || '-'}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap">
                        ${rec?.date ? new Date(rec.date).toLocaleDateString('fr-FR', {
                            day: 'numeric',
                            month: 'short',
                            hour: '2-digit',
                            minute: '2-digit'
                        }) : '-'}
                    </td>
                </tr>
            `).join('');
    } catch (error) {
        console.error('Erreur lors de la mise à jour du tableau des recommandations :', error);
        tbody.innerHTML = `
            <tr>
                <td colspan="4" class="px-6 py-4 text-center text-red-500">
                    Erreur lors du chargement des recommandations
                </td>
            </tr>
        `;
    }
}

// Fonctions de rendu des graphiques avec gestion des erreurs
function renderAgeDistribution(ageData = {}) {
    console.log('Rendu de la distribution des âges avec :', ageData);
    const canvas = document.getElementById('ageDistributionChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    try {
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: Object.keys(ageData),
                datasets: [{
                    label: 'Nombre de profils',
                    data: Object.values(ageData),
                    backgroundColor: '#FF6B6B',
                    borderColor: '#FF5252',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Erreur lors du rendu du graphique des âges :', error);
    }
}

function renderCityDistribution(cityData = {}) {
    console.log('Rendu de la distribution des villes avec :', cityData);
    const canvas = document.getElementById('cityDistributionChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    try {
        new Chart(ctx, {
            type: 'pie',
            data: {
                labels: Object.keys(cityData),
                datasets: [{
                    data: Object.values(cityData),
                    backgroundColor: [
                        '#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4',
                        '#FFEEAD', '#D4A5A5', '#9DC8C8', '#58C9B9',
                        '#52B3D9', '#C8F7C5'
                    ]
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'right'
                    }
                }
            }
        });
    } catch (error) {
        console.error('Erreur lors du rendu du graphique des villes :', error);
    }
}

function renderActivityTimeline(timelineData = {}) {
    console.log('Rendu de la timeline d\'activité avec :', timelineData);
    const canvas = document.getElementById('activityTimelineChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    try {
        const hours = Array.from({ length: 24 }, (_, i) => i);
        const data = hours.map(hour => timelineData[hour] || 0);

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: hours.map(h => `${h}h`),
                datasets: [{
                    label: 'Activité',
                    data: data,
                    borderColor: '#4ECDC4',
                    backgroundColor: 'rgba(78, 205, 196, 0.1)',
                    fill: true
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Erreur lors du rendu du graphique de la timeline :', error);
    }
}

function renderMessagesByDay(messageData = {}) {
    console.log('Rendu des messages par jour avec :', messageData);
    const canvas = document.getElementById('messagesByDayChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    try {
        const days = ['Dimanche', 'Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi'];
        const today = new Date();
        const labels = Array.from({ length: 7 }, (_, i) => {
            const d = new Date();
            d.setDate(today.getDate() - i);
            return days[d.getDay()];
        }).reverse();

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Messages',
                    data: Object.values(messageData),
                    backgroundColor: '#45B7D1',
                    borderColor: '#3C9FB3',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Erreur lors du rendu du graphique des messages par jour :', error);
    }
}