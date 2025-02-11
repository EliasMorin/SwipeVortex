document.addEventListener('DOMContentLoaded', function() {
    let data = null;
    const loadingDiv = document.createElement('div');
    loadingDiv.className = 'text-center p-4';
    loadingDiv.textContent = 'Chargement des données...';
    document.querySelector('.container').appendChild(loadingDiv);

    fetch('/api/bumble/stats')
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return response.json();
        })
        .then(jsonData => {
            data = jsonData;
            loadingDiv.remove();
            updateDashboard(data);
        })
        .catch(error => {
            loadingDiv.remove();
            showError(error.message);
            console.error('Error:', error);
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

function updateDashboard(data) {
    if (!data) {
        showError('Données non disponibles');
        return;
    }

    // Update statistics cards avec vérification des valeurs
    document.getElementById('totalLikes').textContent = data.totalLikes || 0;
    document.getElementById('totalMatches').textContent = data.totalMatches || 0;
    document.getElementById('totalMessages').textContent = data.totalMessagesSent || 0;
    document.getElementById('matchRate').textContent = 
        `${(((data.totalMatches || 0) / (data.totalLikes || 1)) * 100).toFixed(1)}%`;

    // Vérifier si encounters existe et n'est pas vide
    if (data.encounters && Array.isArray(data.encounters) && data.encounters.length > 0) {
        renderAgeDistribution(data.encounters);
        renderActivityTime(data.encounters);
        updateEncountersTable(data.encounters);
    } else {
        console.log('Aucune rencontre à afficher');
    }
}

function renderAgeDistribution(encounters) {
    if (!encounters || !Array.isArray(encounters)) {
        console.error('Données de rencontres invalides');
        return;
    }

    const ageGroups = encounters.reduce((acc, enc) => {
        if (enc && typeof enc.age === 'number') {
            acc[enc.age] = (acc[enc.age] || 0) + 1;
        }
        return acc;
    }, {});

    const ctx = document.getElementById('ageDistributionChart');
    if (!ctx) {
        console.error('Canvas non trouvé');
        return;
    }

    new Chart(ctx.getContext('2d'), {
        type: 'bar',
        data: {
            labels: Object.keys(ageGroups).sort((a, b) => a - b),
            datasets: [{
                label: 'Nombre de profils',
                data: Object.values(ageGroups),
                backgroundColor: '#4F46E5',
                borderColor: '#4338CA',
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
}

function renderActivityTime(encounters) {
    if (!encounters || !Array.isArray(encounters)) {
        console.error('Données de rencontres invalides');
        return;
    }

    const hourlyActivity = encounters.reduce((acc, enc) => {
        if (enc && enc.date) {
            const hour = new Date(enc.date).getHours();
            if (!isNaN(hour)) {
                acc[hour] = (acc[hour] || 0) + 1;
            }
        }
        return acc;
    }, {});

    const hours = Array.from({length: 24}, (_, i) => i);
    const data = hours.map(hour => hourlyActivity[hour] || 0);

    const ctx = document.getElementById('activityTimeChart');
    if (!ctx) {
        console.error('Canvas non trouvé');
        return;
    }

    new Chart(ctx.getContext('2d'), {
        type: 'line',
        data: {
            labels: hours.map(h => `${h}h`),
            datasets: [{
                label: 'Activité',
                data: data,
                borderColor: '#10B981',
                backgroundColor: 'rgba(16, 185, 129, 0.1)',
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
}

function updateEncountersTable(encounters) {
    if (!encounters || !Array.isArray(encounters)) {
        console.error('Données de rencontres invalides');
        return;
    }

    const tbody = document.getElementById('encountersTable');
    if (!tbody) {
        console.error('Table non trouvée');
        return;
    }

    tbody.innerHTML = encounters
        .sort((a, b) => new Date(b.date) - new Date(a.date))
        .slice(0, 10)
        .map(encounter => `
            <tr>
                <td class="px-6 py-4 whitespace-nowrap">${encounter.name || 'N/A'}</td>
                <td class="px-6 py-4 whitespace-nowrap">${encounter.age || 'N/A'}</td>
                <td class="px-6 py-4 whitespace-nowrap">
                    ${encounter.isMatch ? 
                        '<span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">Oui</span>' : 
                        '<span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800">Non</span>'}
                </td>
                <td class="px-6 py-4 whitespace-nowrap">${new Date(encounter.date).toLocaleDateString('fr-FR')}</td>
            </tr>
        `).join('');
}