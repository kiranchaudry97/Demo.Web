const API_BASE = '/api';
const API_KEY = 'demo-api-key-12345';
let winkelmandje = [];
let klanten = [];
let boeken = [];
let orders = [];

const headers = {
    'Content-Type': 'application/json',
    'X-API-Key': API_KEY
};

// Initialisatie
document.addEventListener('DOMContentLoaded', () => {
    loadKlanten();
    loadBoeken();
    loadOrders();
    setupForms();
});

function setupForms() {
    document.getElementById('klant-form').addEventListener('submit', submitKlantForm);
    document.getElementById('klant-modal-form').addEventListener('submit', submitKlantModalForm);
    document.getElementById('boek-modal-form').addEventListener('submit', submitBoekModalForm);
}

// API Calls
async function apiCall(endpoint, method = 'GET', body = null) {
    const options = {
        method,
        headers: headers
    };
    
    if (body) {
        options.body = JSON.stringify(body);
    }
    
    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Er is een fout opgetreden');
        }
        
        return await response.json();
    } catch (error) {
        showError(error.message);
        throw error;
    }
}

// Klanten
async function loadKlanten() {
    try {
        klanten = await apiCall('/klanten');
        displayKlanten();
        updateKlantDropdown();
        document.getElementById('klanten-loading').style.display = 'none';
        document.getElementById('klanten-tabel').style.display = 'table';
    } catch (error) {
        console.error('Fout bij laden klanten:', error);
    }
}

function displayKlanten() {
    const tbody = document.getElementById('klanten-body');
    tbody.innerHTML = klanten.map(klant => `
        <tr>
            <td>${klant.naam}</td>
            <td>${klant.email}</td>
            <td>
                <button class="btn btn-bewerken" onclick="bewerkenKlant(${klant.id})">Bewerken</button>
                <button class="btn btn-verwijderen" onclick="verwijderenKlant(${klant.id})">Verwijderen</button>
            </td>
        </tr>
    `).join('');
}

function updateKlantDropdown() {
    const select = document.getElementById('order-klant');
    select.innerHTML = '<option value="">Selecteer een klant</option>' +
        klanten.map(k => `<option value="${k.id}">${k.naam}</option>`).join('');
}

async function submitKlantForm(e) {
    e.preventDefault();
    
    const id = document.getElementById('klant-id').value;
    const klantData = {
        naam: document.getElementById('klant-naam').value,
        email: document.getElementById('klant-email').value,
        telefoon: document.getElementById('klant-telefoon').value,
        adres: document.getElementById('klant-adres').value
    };
    
    try {
        if (id) {
            await apiCall(`/klanten/${id}`, 'PUT', klantData);
            showSuccess('Klant succesvol bijgewerkt');
        } else {
            await apiCall('/klanten', 'POST', klantData);
            showSuccess('Klant succesvol toegevoegd');
        }
        
        resetKlantForm();
        loadKlanten();
    } catch (error) {
        console.error('Fout bij opslaan klant:', error);
    }
}

function bewerkenKlant(id) {
    const klant = klanten.find(k => k.id === id);
    if (klant) {
        document.getElementById('modal-klant-id').value = klant.id;
        document.getElementById('modal-klant-naam').value = klant.naam;
        document.getElementById('modal-klant-email').value = klant.email;
        document.getElementById('modal-klant-telefoon').value = klant.telefoon;
        document.getElementById('modal-klant-adres').value = klant.adres;
        document.getElementById('klant-modal-title').textContent = 'Klant Bewerken';
        document.getElementById('klant-modal').style.display = 'block';
    }
}

async function verwijderenKlant(id) {
    if (confirm('Weet u zeker dat u deze klant wilt verwijderen?')) {
        try {
            await apiCall(`/klanten/${id}`, 'DELETE');
            showSuccess('Klant succesvol verwijderd');
            loadKlanten();
        } catch (error) {
            console.error('Fout bij verwijderen klant:', error);
        }
    }
}

function resetKlantForm() {
    document.getElementById('klant-form').reset();
    document.getElementById('klant-id').value = '';
}

function openNieuweKlantModal() {
    document.getElementById('klant-modal-title').textContent = 'Nieuwe Klant';
    document.getElementById('klant-modal-form').reset();
    document.getElementById('modal-klant-id').value = '';
    document.getElementById('klant-modal').style.display = 'block';
}

function closeKlantModal() {
    document.getElementById('klant-modal').style.display = 'none';
}

async function submitKlantModalForm(e) {
    e.preventDefault();
    
    const id = document.getElementById('modal-klant-id').value;
    const klantData = {
        naam: document.getElementById('modal-klant-naam').value,
        email: document.getElementById('modal-klant-email').value,
        telefoon: document.getElementById('modal-klant-telefoon').value,
        adres: document.getElementById('modal-klant-adres').value
    };
    
    try {
        if (id) {
            await apiCall(`/klanten/${id}`, 'PUT', klantData);
            showSuccess('Klant succesvol bijgewerkt');
        } else {
            await apiCall('/klanten', 'POST', klantData);
            showSuccess('Klant succesvol toegevoegd');
        }
        
        closeKlantModal();
        loadKlanten();
    } catch (error) {
        console.error('Fout bij opslaan klant:', error);
    }
}

// Boeken
async function loadBoeken() {
    try {
        boeken = await apiCall('/boeken');
        displayBoeken();
        updateBoekDropdown();
        document.getElementById('boeken-loading').style.display = 'none';
        document.getElementById('boeken-tabel').style.display = 'table';
    } catch (error) {
        console.error('Fout bij laden boeken:', error);
    }
}

function displayBoeken() {
    const tbody = document.getElementById('boeken-body');
    tbody.innerHTML = boeken.map(boek => {
        const voorraadClass = boek.voorraadAantal < 15 ? 'voorraad-laag' : '';
        return `
        <tr>
            <td>${boek.titel}</td>
            <td>${boek.auteur}</td>
            <td>EUR ${boek.prijs.toFixed(2)}</td>
            <td class="${voorraadClass}">
                ${boek.voorraadAantal} 
                ${boek.voorraadAantal < 15 ? '<span class="voorraad-info">(Laag!)</span>' : ''}
            </td>
            <td><small>${boek.isbn}</small></td>
            <td>
                <button class="btn btn-bewerken" onclick="bewerkenBoek(${boek.id})">Bewerken</button>
                <button class="btn btn-verwijderen" onclick="verwijderenBoek(${boek.id})">Verwijderen</button>
            </td>
        </tr>
    `}).join('');
}

function updateBoekDropdown() {
    const select = document.getElementById('order-boek');
    select.innerHTML = '<option value="">Selecteer een boek</option>' +
        boeken.map(b => `
            <option value="${b.id}">
                ${b.titel} - EUR ${b.prijs.toFixed(2)} (Voorraad: ${b.voorraadAantal})
            </option>
        `).join('');
}

function bewerkenBoek(id) {
    const boek = boeken.find(b => b.id === id);
    if (boek) {
        document.getElementById('modal-boek-id').value = boek.id;
        document.getElementById('modal-boek-titel').value = boek.titel;
        document.getElementById('modal-boek-auteur').value = boek.auteur;
        document.getElementById('modal-boek-prijs').value = boek.prijs;
        document.getElementById('modal-boek-voorraad').value = boek.voorraadAantal;
        document.getElementById('modal-boek-isbn').value = boek.isbn;
        document.getElementById('boek-modal-title').textContent = 'Boek Bewerken';
        document.getElementById('boek-modal').style.display = 'block';
    }
}

async function verwijderenBoek(id) {
    if (confirm('Weet u zeker dat u dit boek wilt verwijderen?')) {
        try {
            await apiCall(`/boeken/${id}`, 'DELETE');
            showSuccess('Boek succesvol verwijderd');
            loadBoeken();
        } catch (error) {
            console.error('Fout bij verwijderen boek:', error);
        }
    }
}

function openNieuwBoekModal() {
    document.getElementById('boek-modal-title').textContent = 'Nieuw Boek';
    document.getElementById('boek-modal-form').reset();
    document.getElementById('modal-boek-id').value = '';
    document.getElementById('boek-modal').style.display = 'block';
}

function closeBoekModal() {
    document.getElementById('boek-modal').style.display = 'none';
}

async function submitBoekModalForm(e) {
    e.preventDefault();
    
    const id = document.getElementById('modal-boek-id').value;
    const boekData = {
        titel: document.getElementById('modal-boek-titel').value,
        auteur: document.getElementById('modal-boek-auteur').value,
        prijs: parseFloat(document.getElementById('modal-boek-prijs').value),
        voorraadAantal: parseInt(document.getElementById('modal-boek-voorraad').value),
        isbn: document.getElementById('modal-boek-isbn').value
    };
    
    try {
        if (id) {
            await apiCall(`/boeken/${id}`, 'PUT', boekData);
            showSuccess('Boek succesvol bijgewerkt');
        } else {
            await apiCall('/boeken', 'POST', boekData);
            showSuccess('Boek succesvol toegevoegd');
        }
        
        closeBoekModal();
        loadBoeken();
    } catch (error) {
        console.error('Fout bij opslaan boek:', error);
    }
}

// Winkelmandje
function toevoegenAanWinkelmandje() {
    const boekId = parseInt(document.getElementById('order-boek').value);
    const aantal = parseInt(document.getElementById('order-aantal').value);
    
    if (!boekId || aantal < 1) {
        showError('Selecteer een boek en voer een geldig aantal in');
        return;
    }
    
    const boek = boeken.find(b => b.id === boekId);
    if (!boek) {
        showError('Boek niet gevonden');
        return;
    }
    
    if (aantal > boek.voorraadAantal) {
        showError(`Onvoldoende voorraad. Beschikbaar: ${boek.voorraadAantal}`);
        return;
    }
    
    const bestaandItem = winkelmandje.find(item => item.boekId === boekId);
    if (bestaandItem) {
        bestaandItem.aantal += aantal;
    } else {
        winkelmandje.push({
            boekId: boekId,
            titel: boek.titel,
            prijs: boek.prijs,
            aantal: aantal
        });
    }
    
    displayWinkelmandje();
    document.getElementById('order-aantal').value = 1;
    showSuccess(`${boek.titel} toegevoegd aan winkelmandje`);
}

function displayWinkelmandje() {
    const container = document.getElementById('winkelmandje-items');
    
    if (winkelmandje.length === 0) {
        container.innerHTML = '<p style="color: #999; text-align: center;">Geen items in winkelmandje</p>';
        document.getElementById('totaal-bedrag').textContent = 'Totaal: EUR 0.00';
        return;
    }
    
    container.innerHTML = winkelmandje.map((item, index) => `
        <div class="cart-item">
            <div>
                <strong>${item.titel}</strong><br>
                <small>EUR ${item.prijs.toFixed(2)} × ${item.aantal} = EUR ${(item.prijs * item.aantal).toFixed(2)}</small>
            </div>
            <button class="btn btn-verwijderen" onclick="verwijderenUitWinkelmandje(${index})">×</button>
        </div>
    `).join('');
    
    const totaal = winkelmandje.reduce((sum, item) => sum + (item.prijs * item.aantal), 0);
    document.getElementById('totaal-bedrag').textContent = `Totaal: EUR ${totaal.toFixed(2)}`;
}

function verwijderenUitWinkelmandje(index) {
    winkelmandje.splice(index, 1);
    displayWinkelmandje();
}

// Orders
async function loadOrders() {
    try {
        orders = await apiCall('/orders');
        displayOrders();
        document.getElementById('bestellingen-loading').style.display = 'none';
        document.getElementById('bestellingen-tabel').style.display = 'table';
    } catch (error) {
        console.error('Fout bij laden orders:', error);
    }
}

function displayOrders() {
    const tbody = document.getElementById('bestellingen-body');
    tbody.innerHTML = orders.map(order => {
        const klant = klanten.find(k => k.id === order.klant?.id);
        return `
        <tr>
            <td>${order.id}</td>
            <td>${klant ? klant.naam : 'Onbekend'}</td>
            <td>
                ${order.aantalItems} item(s)
            </td>
            <td>${new Date(order.orderDatum).toLocaleString('nl-NL')}</td>
            <td>EUR ${order.totaalBedrag.toFixed(2)}</td>
            <td>
                <button class="btn btn-primary" onclick="toonOrderDetails(${order.id})">Details</button>
            </td>
        </tr>
    `}).join('');
}

async function plaatsOrder() {
    const klantId = parseInt(document.getElementById('order-klant').value);
    
    if (!klantId) {
        showError('Selecteer een klant');
        return;
    }
    
    if (winkelmandje.length === 0) {
        showError('Winkelmandje is leeg');
        return;
    }
    
    const orderData = {
        klantId: klantId,
        items: winkelmandje.map(item => ({
            boekId: item.boekId,
            aantal: item.aantal
        }))
    };
    
    try {
        const result = await apiCall('/orders', 'POST', orderData);
        showSuccess(`
            Order succesvol geplaatst!<br>
            <strong>Order nummer:</strong> ${result.orderNummer}<br>
            <strong>Totaal:</strong> EUR ${result.totaalBedrag.toFixed(2)}<br>
            <strong>Salesforce ID:</strong> ${result.salesforceId}<br>
            <strong>SAP Status:</strong> ${result.sapStatus}
        `);
        
        winkelmandje = [];
        displayWinkelmandje();
        loadOrders();
        loadBoeken();
    } catch (error) {
        console.error('Fout bij plaatsen order:', error);
    }
}

async function toonOrderDetails(orderId) {
    try {
        const order = await apiCall(`/orders/${orderId}`);
        
        const content = `
            <div style="line-height: 1.8;">
                <p><strong>Order Nummer:</strong> ${order.orderNummer}</p>
                <p><strong>Datum:</strong> ${new Date(order.orderDatum).toLocaleString('nl-NL')}</p>
                <p><strong>Status:</strong> <span class="status-badge status-nieuw">${order.status}</span></p>
                <p><strong>Klant:</strong> ${order.klant.naam} (${order.klant.email})</p>
                <p><strong>Salesforce ID:</strong> ${order.salesforceId || 'N/A'}</p>
                <p><strong>SAP Status:</strong> ${order.sapStatus || 'N/A'}</p>
                
                <h3 style="margin-top: 20px; margin-bottom: 10px;">Bestelde Items:</h3>
                <table style="width: 100%; margin-top: 10px;">
                    <thead>
                        <tr>
                            <th>Boek</th>
                            <th>Aantal</th>
                            <th>Prijs</th>
                            <th>Subtotaal</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${order.items.map(item => `
                            <tr>
                                <td>${item.titel}</td>
                                <td>${item.aantal}</td>
                                <td>EUR ${item.prijs.toFixed(2)}</td>
                                <td>EUR ${item.subtotaal.toFixed(2)}</td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
                
                <div class="totaal-bedrag" style="margin-top: 15px;">
                    Totaal: EUR ${order.totaalBedrag.toFixed(2)}
                </div>
            </div>
        `;
        
        document.getElementById('order-details-content').innerHTML = content;
        document.getElementById('order-details-modal').style.display = 'block';
    } catch (error) {
        console.error('Fout bij laden order details:', error);
    }
}

function closeOrderDetailsModal() {
    document.getElementById('order-details-modal').style.display = 'none';
}

// Utility functies
function showSuccess(message) {
    const container = document.getElementById('message-container');
    container.innerHTML = `<div class="success">${message}</div>`;
    setTimeout(() => {
        container.innerHTML = '';
    }, 5000);
}

function showError(message) {
    const container = document.getElementById('message-container');
    container.innerHTML = `<div class="error">${message}</div>`;
    setTimeout(() => {
        container.innerHTML = '';
    }, 5000);
}

// Close modals wanneer buiten geklikt wordt
window.onclick = function(event) {
    if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
    }
}
