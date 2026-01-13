// Zoekfunctionaliteit voor Klanten
async function searchKlanten() {
    const searchInput = document.getElementById('klanten-search-input');
    const query = searchInput.value.trim();
    
    if (!query) {
        showError('Voer een zoekterm in');
        return;
    }
    
    try {
        document.getElementById('klanten-loading').style.display = 'block';
        document.getElementById('klanten-tabel').style.display = 'none';
        
        const results = await apiCall(`/klanten/search?query=${encodeURIComponent(query)}`);
        
        klanten = results; // Update global klanten array
        displayKlanten();
        
        const resultsDiv = document.getElementById('klanten-search-results');
        resultsDiv.textContent = `${results.length} resultaten gevonden voor "${query}"`;
        
        document.getElementById('klanten-loading').style.display = 'none';
        document.getElementById('klanten-tabel').style.display = 'table';
        
    } catch (error) {
        console.error('Fout bij zoeken klanten:', error);
    }
}

// Clear klanten search
function clearKlantenSearch() {
    document.getElementById('klanten-search-input').value = '';
    document.getElementById('klanten-search-results').textContent = '';
    loadKlanten();
}

// Zoekfunctionaliteit voor Boeken
async function searchBoeken() {
    const searchInput = document.getElementById('boeken-search-input');
    const query = searchInput.value.trim();
    
    if (!query) {
        showError('Voer een zoekterm in');
        return;
    }
    
    try {
        document.getElementById('boeken-loading').style.display = 'block';
        document.getElementById('boeken-tabel').style.display = 'none';
        
        const results = await apiCall(`/boeken/search?query=${encodeURIComponent(query)}`);
        
        boeken = results; // Update global boeken array
        displayBoeken();
        updateBoekDropdown();
        
        const resultsDiv = document.getElementById('boeken-search-results');
        resultsDiv.textContent = `${results.length} resultaten gevonden voor "${query}"`;
        
        document.getElementById('boeken-loading').style.display = 'none';
        document.getElementById('boeken-tabel').style.display = 'table';
        
    } catch (error) {
        console.error('Fout bij zoeken boeken:', error);
    }
}

// Clear boeken search
function clearBoekenSearch() {
    document.getElementById('boeken-search-input').value = '';
    document.getElementById('boeken-search-results').textContent = '';
    loadBoeken();
}

// Enter key support
document.addEventListener('DOMContentLoaded', () => {
    // Klanten zoeken met Enter
    const klantenSearchInput = document.getElementById('klanten-search-input');
    if (klantenSearchInput) {
        klantenSearchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchKlanten();
            }
        });
    }
    
    // Boeken zoeken met Enter
    const boekenSearchInput = document.getElementById('boeken-search-input');
    if (boekenSearchInput) {
        boekenSearchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                searchBoeken();
            }
        });
    }
});
