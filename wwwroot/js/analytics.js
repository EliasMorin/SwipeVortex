document.addEventListener('DOMContentLoaded', function() {
    let data = null;
    const loadingDiv = document.createElement('div');
    loadingDiv.className = 'text-center p-4';
    loadingDiv.textContent = 'Chargement des données Instagram...';
    document.querySelector('.container').appendChild(loadingDiv);

    // Récupérer les différentes données en parallèle
    Promise.all([
        fetch('/api/InstagramAnalysis/stats').then(response => {
            if (!response.ok) throw new Error(`Erreur HTTP! statut: ${response.status}`);
            return response.json();
        }),
        fetch('/api/InstagramAnalysis/top-posts').then(response => {
            if (!response.ok) throw new Error(`Erreur HTTP! statut: ${response.status}`);
            return response.json();
        }),
        fetch('/api/InstagramAnalysis/category-scores').then(response => {
            if (!response.ok) throw new Error(`Erreur HTTP! statut: ${response.status}`);
            return response.json();
        }),
        fetch('/api/InstagramAnalysis/users').then(response => {
            if (!response.ok) throw new Error(`Erreur HTTP! statut: ${response.status}`);
            return response.json();
        })
    ])
    .then(([statsData, topPostsData, categoriesData, usersData]) => {
        // Structurer les données récupérées
        data = {
            stats: statsData.stats,
            topPosts: topPostsData,
            categories: categoriesData,
            topUsers: usersData
        };
        
        loadingDiv.remove();
        renderDashboard(data);
    })
    .catch(error => {
        loadingDiv.remove();
        showError(error.message);
        console.error('Erreur de chargement:', error);
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

    function renderDashboard(data) {
        if (!data || !data.stats) {
            showError('Aucune donnée Instagram disponible');
            return;
        }

        // Mettre à jour les cartes de statistiques
        document.getElementById('totalPosts').textContent = data.stats.totalPostsProcessed || 0;
        
        // Calcul du score moyen des catégories
        document.getElementById('averageScore').textContent = 
            data.categories && data.categories.length > 0 ? 
            (data.categories.reduce((acc, cat) => acc + (cat.averageScore || 0), 0) / data.categories.length).toFixed(2) : 
            '-';

        // Calcul de l'engagement total
        document.getElementById('totalEngagement').textContent = 
            data.topPosts && data.topPosts.length > 0 ? 
            data.topPosts.reduce((total, topPost) => {
                // Vérifier si medias existe et n'est pas undefined
                if (topPost.medias && Array.isArray(topPost.medias)) {
                    return total + topPost.medias.reduce((acc, post) => 
                        acc + (post.likesCount || 0) + (post.commentsCount || 0), 0);
                }
                return total;
            }, 0).toLocaleString() : 
            '0';

        // Remplir le tableau des top posts
        renderTopPostsTable(data.topPosts || []);

        // Rendu des graphiques
        renderCategoryChart(data.categories || []);
        renderFollowersChart(data.topUsers || []);
        
        // Aplatir les médias des top posts pour l'engagement
        const allMediaPosts = data.topPosts ? 
            data.topPosts.flatMap(tp => tp.medias || []) : 
            [];
        renderEngagementChart(allMediaPosts);
    }

    function renderTopPostsTable(topPosts) {
        const tbody = document.getElementById('topPostsTable');
        tbody.innerHTML = topPosts
            .flatMap(topPost => {
                // Vérifier si medias existe et n'est pas undefined
                if (!topPost.medias || !Array.isArray(topPost.medias)) {
                    return [];
                }
                return topPost.medias
                    .sort((a, b) => (b.finalScore || 0) - (a.finalScore || 0))
                    .slice(0, 10)
                    .map((post, index) => `
                        <tr>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${index + 1}</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                                @${post.user && post.user.userName ? post.user.userName : 'Inconnu'}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${(post.finalScore || 0).toFixed(2)}</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${((post.finalScore || 0) * 100).toFixed(2)}%</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">${(post.penetrationRate || 0).toFixed(2)}%</td>
                        </tr>
                    `);
            })
            .join('');
    }

    function renderCategoryChart(categories) {
        const ctx = document.getElementById('categoryChart').getContext('2d');
        new Chart(ctx, {
            type: 'pie',
            data: {
                labels: categories.map(cat => cat.categoryName),
                datasets: [{
                    data: categories.map(cat => cat.averageScore),
                    backgroundColor: ['#FF6384', '#36A2EB', '#FFCE56', '#4BC0C0', '#9966FF']
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
    }

    function renderFollowersChart(users) {
        const ctx = document.getElementById('followersChart').getContext('2d');
        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: users.map(user => user.userName),
                datasets: [{
                    label: 'Followers',
                    data: users.map(user => user.followerCount),
                    backgroundColor: '#36A2EB'
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: value => value.toLocaleString()
                        }
                    }
                }
            }
        });
    }

    function renderEngagementChart(posts) {
        const ctx = document.getElementById('engagementChart').getContext('2d');
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: posts.map(post => post.userName || 'Utilisateur'),
                datasets: [
                    {
                        label: 'Score Moyen',
                        data: posts.map(post => (post.averageFinalScore || 0) * 100),
                        borderColor: '#8884d8',
                        fill: false
                    },
                    {
                        label: 'Taux de Pénétration',
                        data: posts.map(post => (post.penetrationRate || 0) * 100),
                        borderColor: '#82ca9d',
                        fill: false
                    },
                    {
                        label: 'Likes',
                        data: posts.map(post => post.likesCount || 0),
                        borderColor: '#ffc658',
                        fill: false
                    }
                ]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    }
});