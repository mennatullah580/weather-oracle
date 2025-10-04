// script.js - Frontend logic for Weather Oracle

// IMPORTANT: Change this to your backend URL
const BACKEND_URL = 'https://localhost:7036';

// Create animated stars
const starsContainer = document.getElementById('stars');
for (let i = 0; i < 100; i++) {
    const star = document.createElement('div');
    star.className = 'star';
    star.style.left = Math.random() * 100 + '%';
    star.style.top = Math.random() * 100 + '%';
    star.style.animationDelay = Math.random() * 3 + 's';
    starsContainer.appendChild(star);
}

// Initialize Leaflet map
const map = L.map('map').setView([30, 0], 2);
L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
    attribution: '&copy; OpenStreetMap contributors &copy; CARTO',
    maxZoom: 19
}).addTo(map);

let marker;

// Map click handler - sets lat/lon when user clicks
map.on('click', function (e) {
    const lat = e.latlng.lat.toFixed(4);
    const lon = e.latlng.lng.toFixed(4);
    document.getElementById('lat').value = lat;
    document.getElementById('lon').value = lon;

    if (marker) {
        map.removeLayer(marker);
    }
    marker = L.marker([lat, lon]).addTo(map);
});

// Demo city chips - quick selection
document.querySelectorAll('.city-chip').forEach(chip => {
    chip.addEventListener('click', function () {
        const lat = this.dataset.lat;
        const lon = this.dataset.lon;
        document.getElementById('lat').value = lat;
        document.getElementById('lon').value = lon;

        if (marker) {
            map.removeLayer(marker);
        }
        marker = L.marker([lat, lon]).addTo(map);
        map.setView([lat, lon], 5);
    });
});

// Main analyze function - THIS IS WHERE THE BACKEND IS CALLED
document.getElementById('analyzeBtn').addEventListener('click', async function () {
    const lat = document.getElementById('lat').value;
    const lon = document.getElementById('lon').value;
    const month = document.getElementById('month').value;

    if (!lat || !lon) {
        alert('Please select a location or enter coordinates');
        return;
    }

    const loader = document.getElementById('loader');
    const results = document.getElementById('results');
    const btn = this;

    loader.classList.add('show');
    results.classList.remove('show');
    btn.disabled = true;

    try {
        // ? THIS IS THE BACKEND CALL ?
        const API_URL = `${BACKEND_URL}/api/likelihood?lat=${lat}&lon=${lon}&month=${month}`;

        console.log('Calling backend:', API_URL);

        const response = await fetch(API_URL);
        const data = await response.json();

        console.log('Backend response:', data);

        if (!response.ok) {
            throw new Error(data.error || 'API request failed');
        }

        // Update UI with backend data
        const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        document.getElementById('locationInfo').innerHTML = `
            ?? <strong>Location:</strong> ${lat}, ${lon}<br>
            ?? <strong>Month:</strong> ${monthNames[month - 1]}<br>
            ?? <strong>Data Source:</strong> NASA POWER (1981-2010)
        `;

        const heatProb = data.probabilities.heat_above_35C || 0;
        const rainProb = data.probabilities.rain_above_20mm || 0;

        document.getElementById('heatValue').textContent = (heatProb * 100).toFixed(1) + '%';
        document.getElementById('rainValue').textContent = (rainProb * 100).toFixed(1) + '%';

        // Animate the progress bars
        setTimeout(() => {
            document.getElementById('heatBar').style.width = (heatProb * 100) + '%';
            document.getElementById('rainBar').style.width = (rainProb * 100) + '%';
        }, 100);

        // Add interpretations
        document.getElementById('heatInterpret').textContent =
            heatProb > 0.5 ? 'Very high likelihood - expect extreme heat' :
                heatProb > 0.2 ? 'Moderate likelihood - possible heat waves' :
                    heatProb > 0.05 ? 'Low likelihood - occasional hot days' :
                        'Very low likelihood - rare extreme heat';

        document.getElementById('rainInterpret').textContent =
            rainProb > 0.5 ? 'Very high likelihood - expect heavy rainfall' :
                rainProb > 0.2 ? 'Moderate likelihood - prepare for wet weather' :
                    rainProb > 0.05 ? 'Low likelihood - occasional heavy rain' :
                        'Very low likelihood - typically dry';

        loader.classList.remove('show');
        results.classList.add('show');

    } catch (error) {
        console.error('Error calling backend:', error);
        alert('Error: ' + error.message + '\n\nMake sure your backend is running on ' + BACKEND_URL);
        loader.classList.remove('show');
    } finally {
        btn.disabled = false;
    }
});