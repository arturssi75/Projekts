// ==================== KONFIGURĀCIJA UN GLOBĀLIE MAINĪGIE ====================
const API_BASE = 'http://localhost:5000/api';

let currentEditableId = null;
let currentSection = 'cargo';
let currentUser = null;
let sharedFormModal = null;

const apiClient = axios.create({
    baseURL: API_BASE,
});

apiClient.interceptors.request.use(config => {
    const token = localStorage.getItem('authToken');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
}, error => {
    console.error("Axios pieprasījuma veidošanas kļūda:", error);
    showToast("Kļūda sazinoties ar serveri (pieprasījums).", "danger");
    return Promise.reject(error);
});

apiClient.interceptors.response.use(response => {
    return response;
}, error => {
    if (error.response) {
        if (error.response.status === 401) {
            console.warn("Saņemta 401 (Neautorizēts) atbilde. Lietotājs tiek izlogots.");
            logout();
        } else if (error.response.status === 403) {
            showToast('Jums nav tiesību veikt šo darbību vai piekļūt šim resursam.', 'danger');
        } else {
            const responseData = error.response.data;
            let errorMessage = `Servera kļūda: ${error.response.status}`;
            if (responseData) {
                if (responseData.message) errorMessage = responseData.message;
                else if (responseData.title) errorMessage = responseData.title;
                else if (responseData.errors && typeof responseData.errors === 'object') errorMessage = Object.values(responseData.errors).flat().join(' \n');
                else if (Array.isArray(responseData.errors)) errorMessage = responseData.errors.join(' \n');
                else if (typeof responseData === 'string') errorMessage = responseData;
            }
            showToast(errorMessage, 'danger');
        }
    } else {
        console.error("Tīkla vai cita veida kļūda API pieprasījumā (bez servera atbildes):", error);
        showToast('Neizdevās sazināties ar serveri. Lūdzu, pārbaudiet savienojumu.', 'danger');
    }
    return Promise.reject(error);
});

// ==================== AUTENTIFIKĀCIJAS FUNKCIJAS ====================
function isAuthenticated() {
    return localStorage.getItem('authToken') !== null;
}

function getUserData() {
    const userDataString = localStorage.getItem('userData');
    if (userDataString) {
        try {
            const parsedData = JSON.parse(userDataString);
            console.log('Original parsedData from localStorage:', parsedData);

            if (parsedData && typeof parsedData === 'object') {
                // Pārbauda un normalizē lomas
                if (parsedData.roles && typeof parsedData.roles === 'object' &&
                    parsedData.roles.$values !== undefined && Array.isArray(parsedData.roles.$values)) {
                    console.log("getUserData: 'roles' lauks tiek iegūts no .$values struktūras.", parsedData.roles.$values);
                    parsedData.roles = parsedData.roles.$values;
                } else if (!parsedData.roles || !Array.isArray(parsedData.roles)) {
                    console.warn("getUserData: Lietotāja datiem no localStorage trūkst 'roles' masīva, tas nav masīvs, vai nav .$values. Iestata kā tukšu masīvu.", parsedData);
                    parsedData.roles = [];
                }

                // Normalizē citus laukus, ja nepieciešams
                if (typeof parsedData.username !== 'string') parsedData.username = 'Nezināms lietotājs';
                if (typeof parsedData.firstName !== 'string') parsedData.firstName = '';
                if (typeof parsedData.lastName !== 'string') parsedData.lastName = '';

                console.log('Processed currentUser.roles in getUserData after potential modifications:', parsedData.roles);
                return parsedData;
            } else {
                console.error("getUserData: Parsētie lietotāja dati no localStorage nav objekts:", parsedData);
                logout(); // Izlogo, ja dati nav korekti
                return null;
            }
        } catch (e) {
            console.error("getUserData: Kļūda, parsējot userData no localStorage:", e);
            logout(); // Izlogo kļūdas gadījumā
            return null;
        }
    }
    return null; // Nav lietotāja datu localStorage
}

function logout() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('userData');
    currentUser = null;
    // Pārliecināmies, ka pārvirzām tikai tad, ja neesam jau login/register lapā
    const currentPage = window.location.pathname.toLowerCase();
    if (!currentPage.endsWith('login.html') && !currentPage.endsWith('register.html')) {
        window.location.href = 'login.html';
    } else {
        updateUIBasedOnAuthState(); // Atjaunojam UI pat ja paliekam login/register lapā
    }
}

// ==================== UI PALĪGFUNKCIJAS ====================
function showToast(message, type = 'success') {
    const toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        console.error("Toast container nav atrasts!");
        alert(message); // Fallback
        return;
    }
    const toastId = `toast-${Date.now()}`;
    const toastHTML = `
        <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Aizvērt"></button>
            </div>
        </div>`;
    toastContainer.insertAdjacentHTML('beforeend', toastHTML);
    const toastElement = document.getElementById(toastId);
    // Pārbauda vai Bootstrap un Toast ir pieejami globāli
    if (window.bootstrap && window.bootstrap.Toast) {
        const bootstrapToast = new window.bootstrap.Toast(toastElement, { delay: 5000 });
        bootstrapToast.show();
        // Nodrošina, ka elements tiek noņemts no DOM pēc aizvēršanas
        toastElement.addEventListener('hidden.bs.toast', () => toastElement.remove());
    } else {
        console.error("Bootstrap Toast nav pieejams.");
        toastElement.remove(); // Noņemam, ja nevar parādīt
        alert(message); // Fallback
    }
}

function toggleLoading(show) {
    const loader = document.getElementById('loading-overlay');
    if (loader) loader.style.display = show ? 'flex' : 'none';
}

// Funkcija datu iegūšanai no API atbildes, apstrādājot .NET $values struktūru
function extractData(response) {
    // Pārbauda vai atbilde ir veiksmīga un satur datus
    if (response && response.status >= 200 && response.status < 300 && response.data) {
        // Pārbauda vai dati ir objekts ar $values masīvu (tipiski .NET kolekcijām ar ReferenceHandler.Preserve)
        if (typeof response.data === 'object' && response.data !== null && response.data.$values !== undefined && Array.isArray(response.data.$values)) {
            console.log("extractData: Dati iegūti no .$values struktūras (kolekcija ar ReferenceHandler.Preserve)");
            return response.data.$values; // Atgriež masīvu no $values
        }
        // Ja nav $values struktūra, atgriež visus datus
        return response.data;
    }
    // Ja atbilde nav veiksmīga vai nesatur datus
    console.warn("Neizdevās iegūt datus no API atbildes vai atbilde nebija veiksmīga:", response);
    return null; // Atgriež null, ja dati nav iegūstami
}


function showSection(sectionId) {
    console.log(`[SHOW_SECTION_START] Mēģina parādīt sadaļu: ${sectionId}`);
    const currentPath = window.location.pathname.toLowerCase();
    // Pārbauda vai lapa ir Index.html vai tās saknes ceļš
    const isIndexPage = currentPath.endsWith('index.html') || currentPath === '/' || currentPath.startsWith('/project/'); // Pielāgojiet, ja bāzes ceļš ir /project/ vai līdzīgs

    // Ja nav autentificējies un atrodas Index lapā, izlogo
    if (!isAuthenticated() && isIndexPage) {
        console.log("[SHOW_SECTION] Nav autentificējies uz Index lapas, izlogo.");
        logout();
        return;
    }

    // Iegūst lietotāja lomas vai tukšu masīvu, ja nav definētas
    const rolesToCheck = (currentUser && currentUser.roles && Array.isArray(currentUser.roles)) ? currentUser.roles : [];
    console.log(`[SHOW_SECTION] Pārbauda piekļuvi: sectionId='${sectionId}', rolesToCheck=[${rolesToCheck.join(',')}]`);
    // Pārbauda vai lietotājam ir tiesības piekļūt sadaļai
    const userCanAccess = canUserAccessSection(sectionId, rolesToCheck);
    console.log(`[SHOW_SECTION] canUserAccessSection atgrieza: ${userCanAccess} priekš sadaļas '${sectionId}'`);

    // Ja lietotājam nav tiesību
    if (!userCanAccess) {
        showToast('Jums nav tiesību piekļūt šai sadaļai.', 'danger');
        // Iegūst noklusējuma sadaļu lietotājam
        const defaultSection = getDefaultSectionForUser(rolesToCheck);
        console.log(`[SHOW_SECTION] Nav tiesību '${sectionId}', mēģina parādīt noklusējuma sadaļu: '${defaultSection}'`);

        // Pārbauda, lai izvairītos no bezgalīga cikla, ja nav piekļuves pat noklusējuma sadaļai
        if (sectionId === defaultSection && !canUserAccessSection(defaultSection, rolesToCheck)) {
             console.error(`[SHOW_SECTION] Cikls! Nevar piekļūt ne pieprasītajai ('${sectionId}'), ne noklusējuma ('${defaultSection}') sadaļai. Parāda tukšu lapu.`);
             // Paslēpj visas satura sadaļas
             document.querySelectorAll('.content-section').forEach(section => section.style.display = 'none');
             // Noņem aktīvo klasi no visām navigācijas saitēm
             document.querySelectorAll('.navbar-nav .nav-link.active').forEach(link => link.classList.remove('active'));
             // Varētu parādīt kādu "error" sadaļu, ja tāda ir definēta
             return;
        }
        // Ja noklusējuma sadaļa atšķiras vai tai ir piekļuve, mēģina to parādīt
        if (sectionId !== defaultSection) {
            showSection(defaultSection); // Rekursīvs izsaukums ar noklusējuma sadaļu
        } else {
            // Ja sectionId IR defaultSection, bet canUserAccessSection atgrieza false,
            // tad kaut kas ir ļoti nogājis greizi ar tiesību loģiku priekš defaultSection.
            // Šajā gadījumā, lai izvairītos no bezgalīga cikla, vienkārši neko nedarām vai parādam kļūdu.
            console.error(`[SHOW_SECTION] Kļūda: Noklusējuma sadaļai '${defaultSection}' nav piekļuves, lai gan tai vajadzētu būt.`);
            // Varētu paslēpt visas sadaļas vai parādīt kļūdas ziņojumu
        }
        return; // Pārtrauc funkcijas izpildi, jo nav tiesību
    }

    // Ja ir tiesības:
    // Paslēpj visas satura sadaļas
    document.querySelectorAll('.content-section').forEach(section => section.style.display = 'none');
    // Noņem aktīvo klasi no visām navigācijas saitēm
    document.querySelectorAll('.navbar-nav .nav-link.active').forEach(link => link.classList.remove('active'));

    // Atrod un parāda aktīvo sadaļu
    const activeSectionElement = document.getElementById(`${sectionId}-section`);
    if (activeSectionElement) {
        activeSectionElement.style.display = 'block'; // Parāda sadaļu
        currentSection = sectionId; // Atjauno pašreizējo sadaļu
        // Atrod un pievieno aktīvo klasi atbilstošajai navigācijas saitei
        const activeNavLink = document.querySelector(`.navbar-nav .nav-link[onclick="showSection('${sectionId}')"]`);
        if (activeNavLink) activeNavLink.classList.add('active');
        console.log(`[SHOW_SECTION_END] Veiksmīgi parādīta sadaļa '${sectionId}'`);
        // Ielādē datus parādītajai sadaļai
        loadDataForSection(sectionId);
    } else {
        // Ja sadaļas elements nav atrasts
        console.error(`[SHOW_SECTION] Sadaļas elements ar ID '${sectionId}-section' nav atrasts.`);
        // Mēģina parādīt noklusējuma sadaļu kā fallback
        const fallbackSection = getDefaultSectionForUser(rolesToCheck);
        console.log(`[SHOW_SECTION] Mēģina parādīt fallback sadaļu: '${fallbackSection}'`);
        if (document.getElementById(`${fallbackSection}-section`)) {
            showSection(fallbackSection); // Izsauc showSection ar fallback sadaļu
        } else {
            // Ja arī fallback sadaļa nav atrasta
            console.error(`[SHOW_SECTION] Fallback sadaļa '${fallbackSection}' arī nav atrasta.`);
            // Šeit varētu parādīt kļūdas ziņojumu lietotājam
        }
    }
}

function loadDataForSection(sectionId) {
    // *** LABOJUMS ***
    // Kartējam daudzskaitļa sectionId uz vienskaitļa menedžera nosaukuma bāzi
    let managerBaseName = sectionId;
    const pluralToSingularMap = {
        'devices': 'Device',
        'clients': 'Client',
        'routes': 'Route',
        'vehicles': 'Vehicle',
        'dispatchers': 'Dispatcher',
        'cargos': 'Cargo' // Pievienots arī kravām, ja nu kas
    };

    if (pluralToSingularMap[sectionId]) {
        managerBaseName = pluralToSingularMap[sectionId];
    } else {
        // Ja nav mapē, pieņemam, ka sectionId jau ir pareizs (vai tā ir īpaša sadaļa)
        // un izmantojam capitalizeFirstLetter kā iepriekš
        managerBaseName = capitalizeFirstLetter(sectionId);
    }

    // Konstruējam menedžera nosaukumu (piem., 'DeviceManager', 'ClientManager')
    const managerName = managerBaseName + 'Manager';
    // *** LABOJUMA BEIGAS ***

    // Pārbaudām vai menedžeris eksistē un tam ir .load() metode
    if (window[managerName] && typeof window[managerName].load === 'function') {
        console.log(`[LOAD_DATA] Izsauc ${managerName}.load() priekš sadaļas '${sectionId}'`);
        window[managerName].load();
    } else if (sectionId === 'map' && MapManager && typeof MapManager.loadDevices === 'function') {
        // Īpašs gadījums kartei
        console.log(`[LOAD_DATA] Izsauc MapManager.loadDevices() priekš sadaļas 'map'`);
        MapManager.loadDevices();
        // Pēc nelielas pauzes pārliecinās, ka karte tiek pareizi attēlota
        if (MapManager.map) setTimeout(() => { if(MapManager.map) MapManager.map.invalidateSize(); }, 100);
    } else if (sectionId === 'admin-users') {
        // Sadaļa, kurai nav paredzēta datu ielāde šādā veidā
        console.log("Lietotāju pārvaldības sadaļa (loadDataForSection) vēl nav implementēta.");
    } else if (!['login', 'register'].includes(sectionId)) {
        // Ja nav atrasts neviens no iepriekšējiem un nav login/register sadaļa
        console.warn(`Nav definēta ielādes funkcija vai menedžeris ('${managerName}') priekš sadaļas: ${sectionId}`);
    }
}


function capitalizeFirstLetter(string) {
    if (!string || typeof string !== 'string') return '';
    return string.charAt(0).toUpperCase() + string.slice(1);
}

function updateUIBasedOnAuthState() {
    const userInfoContainer = document.getElementById('userInfoContainer');
    const logoutButtonContainer = document.getElementById('logoutButtonContainer');
    const userInfoElement = document.getElementById('userInfo');
    const currentPath = window.location.pathname.toLowerCase();
    const isAuthPage = currentPath.endsWith('login.html') || currentPath.endsWith('register.html');

    if (isAuthenticated() && currentUser) {
        // Pārbauda vai lietotājam ir derīgas lomas
        const userHasValidRoles = currentUser.roles && Array.isArray(currentUser.roles);
        // Sagatavo lomu sarakstu attēlošanai
        const rolesForDisplay = userHasValidRoles && currentUser.roles.length > 0 ? currentUser.roles.join(', ') : 'Lomas nav definētas';

        // Attēlo lietotāja informāciju
        if (userInfoElement) userInfoElement.textContent = `Sveiki, ${currentUser.firstName || currentUser.username}! (${rolesForDisplay})`;
        if (userInfoContainer) userInfoContainer.style.display = 'flex'; // Parāda lietotāja info konteineri
        if (logoutButtonContainer) logoutButtonContainer.style.display = 'list-item'; // Parāda izlogošanās pogu

        // Nosaka lietotāja lomas
        const isAdmin = userHasValidRoles && currentUser.roles.includes('Admin');
        const isDispatcher = userHasValidRoles && currentUser.roles.includes('Dispatcher');
        // const isClient = userHasValidRoles && currentUser.roles.includes('Client'); // Klienta loma varētu tikt izmantota specifiskākām tiesībām

        // Pielāgo navigācijas elementu redzamību atkarībā no lomām
        toggleNavElement('nav-cargo', true); // Kravas redz visi autentificētie
        toggleNavElement('nav-map', true);   // Karte redz visi autentificētie
        toggleNavElement('nav-clients', isAdmin || isDispatcher); // Klientus redz Admin un Dispatcher
        toggleNavElement('nav-routes', isAdmin || isDispatcher); // Maršrutus redz Admin un Dispatcher
        toggleNavElement('nav-vehicles', isAdmin || isDispatcher); // Transportlīdzekļus redz Admin un Dispatcher
        toggleNavElement('nav-dispatchers', isAdmin); // Sūtītājus redz tikai Admin
        toggleNavElement('nav-devices', isAdmin || isDispatcher); // Ierīces redz Admin un Dispatcher
        toggleNavElement('nav-admin-users', isAdmin); // Lietotāju pārvaldību redz tikai Admin

        // Pielāgo "Jauns..." pogu redzamību
        toggleButtonVisibility('btn-new-cargo', isAdmin || isDispatcher);
        toggleButtonVisibility('btn-new-client', isAdmin || isDispatcher);
        toggleButtonVisibility('btn-new-route', isAdmin || isDispatcher);
        toggleButtonVisibility('btn-new-vehicle', isAdmin || isDispatcher);
        toggleButtonVisibility('btn-new-dispatcher', isAdmin);
        toggleButtonVisibility('btn-new-device', isAdmin || isDispatcher);

    } else { // Nav autentificējies vai currentUser nav korekts
        // Paslēpj lietotāja informāciju un izlogošanās pogu
        if (userInfoContainer) userInfoContainer.style.display = 'none';
        if (logoutButtonContainer) logoutButtonContainer.style.display = 'none';

        // Ja esam Index.html (nevis login/register) un neesam autentificējušies, slēpjam visas navigācijas sadaļas un pogas
        if (!isAuthPage) {
            document.querySelectorAll('.navbar-nav .nav-item[id^="nav-"]').forEach(item => item.style.display = 'none');
            document.querySelectorAll('button[id^="btn-new-"]').forEach(btn => btn.style.display = 'none');
        }
    }
}

// Palīgfunkcija navigācijas elementa redzamības pārslēgšanai
function toggleNavElement(elementId, show) {
    const element = document.getElementById(elementId);
    if (element) element.style.display = show ? 'list-item' : 'none';
}

// Palīgfunkcija pogas redzamības pārslēgšanai
function toggleButtonVisibility(buttonId, show) {
    const button = document.getElementById(buttonId);
    if (button) button.style.display = show ? 'inline-block' : 'none';
}


function canUserAccessSection(sectionId, rolesArray) {
    console.log(`[CAN_ACCESS_START] Izsaukta ar section: '${sectionId}', lomām: [${rolesArray ? rolesArray.join(',') : 'NAV LOMU'}]`);
    const currentPath = window.location.pathname.toLowerCase();
    // Pārbauda vai lapa ir Index.html vai tās saknes ceļš
    const isIndexPage = currentPath.endsWith('index.html') || currentPath === '/' || currentPath.startsWith('/project/'); // Pielāgojiet, ja bāzes ceļš ir /project/ vai līdzīgs

    // Ja neesam Index.html (piem., esam login.html vai register.html), atļaujam tikai tās sadaļas
    if (!isIndexPage) {
        const allowed = ['login', 'register'].includes(sectionId);
        console.log(`[CAN_ACCESS] Nav Index lapa (Ceļš: ${currentPath}). Sadaļa: '${sectionId}', Atļauts: ${allowed}`);
        return allowed;
    }

    // Ja esam Index.html, bet neesam autentificējušies, liedzam visu
    if (!isAuthenticated()) {
        console.log("[CAN_ACCESS] Index lapa, bet nav autentificējies. Piekļuve liegta.");
        return false;
    }

    // Ja nav lomu masīva vai tas nav masīvs (currentUser varētu būt null, ja getUserData neizdodas)
    if (!Array.isArray(rolesArray)) {
        console.warn("[CAN_ACCESS] rolesArray nav derīgs masīvs. Liedz piekļuvi.", rolesArray);
        return false;
    }

    // Ja lomu masīvs ir tukšs (lietotājam nav piešķirtas lomas)
    if (rolesArray.length === 0) {
        console.warn(`[CAN_ACCESS] Lietotājam ir tukšs lomu masīvs, mēģina piekļūt: ${sectionId}`);
        // Atļauj piekļuvi tikai noteiktām sadaļām, ja lomu nav (piem., kravas un karte)
        const accessForEmptyRoles = ['cargo', 'map'].includes(sectionId); // Pielāgojiet, ja nepieciešams
        console.log(`[CAN_ACCESS] Piešķir piekļuvi tukšām lomām: ${accessForEmptyRoles}`);
        return accessForEmptyRoles;
    }

    // Nosaka lietotāja lomas
    const isAdmin = rolesArray.includes('Admin');
    const isDispatcher = rolesArray.includes('Dispatcher');
    const isClient = rolesArray.includes('Client'); // Klienta loma pagaidām netiek aktīvi izmantota tiesību noteikšanai
    let hasAccess = false;

    // Nosaka piekļuvi atkarībā no sadaļas ID un lietotāja lomām
    switch (sectionId) {
        case 'cargo':       hasAccess = true; break; // Kravas redz visi autentificētie
        case 'map':         hasAccess = true; break; // Karte redz visi autentificētie
        case 'clients':     hasAccess = isAdmin || isDispatcher; break; // Klientus redz Admin un Dispatcher
        case 'routes':      hasAccess = isAdmin || isDispatcher; break; // Maršrutus redz Admin un Dispatcher
        case 'vehicles':    hasAccess = isAdmin || isDispatcher; break; // Transportlīdzekļus redz Admin un Dispatcher
        case 'devices':     hasAccess = isAdmin || isDispatcher; break; // Ierīces redz Admin un Dispatcher
        case 'dispatchers': hasAccess = isAdmin; break; // Sūtītājus redz tikai Admin
        case 'admin-users': hasAccess = isAdmin; break; // Lietotāju pārvaldību redz tikai Admin
        default:
            console.warn(`[CAN_ACCESS] Nezināma sadaļa: ${sectionId}`);
            hasAccess = false; // Nezināmām sadaļām piekļuve liegta
    }
    console.log(`[CAN_ACCESS_END] Gala lēmums priekš '${sectionId}': ${hasAccess} (isAdmin:${isAdmin}, isDispatcher:${isDispatcher}, isClient:${isClient})`);
    return hasAccess;
}

function getDefaultSectionForUser(rolesArray) {
    // Pārliecināmies, ka rolesArray ir masīvs
    if (!Array.isArray(rolesArray)) {
        rolesArray = []; // Ja nav masīvs, uzskatām par tukšu lomu sarakstu
    }

    // Noklusējuma sadaļa ir 'cargo', ja lietotājam ir kāda no pamata lomām vai nav lomu vispār
    if (rolesArray.includes('Admin') || rolesArray.includes('Dispatcher') || rolesArray.includes('Client') || rolesArray.length === 0) {
        return 'cargo';
    }
    // Ja kāda iemesla dēļ nav nevienas no augstāk minētajām lomām, bet masīvs nav tukšs
    // (maz ticams scenārijs ar pašreizējo lomu struktūru), atgriežam 'cargo' kā drošu noklusējumu.
    return 'cargo';
}

// ==================== ENTĪTIJU MENEDŽERI ====================

// --- CargoManager ---
const CargoManager = {
    API_URL: `/Cargo`,
    formModalElement: document.getElementById('formModal'),
    formModal: null, // Tiks inicializēts DOMContentLoaded
    formContentDiv: document.getElementById('modalContent'),
    modalTitle: document.getElementById('modalTitle'),
    allClients: [], allRoutes: [], allDispatchers: [], allDevices: [],
    load: async function() { await this.loadCargos(); },
    async loadCargos() {
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_URL);
            const cargosVM = extractData(response);
            if (cargosVM && Array.isArray(cargosVM)) {
                this.renderTable(cargosVM);
            } else {
                console.error("[FRONTEND] CargoManager.loadCargos: Saņemtie dati kravām nav masīvs.", cargosVM);
                const container = document.getElementById('cargo-list');
                if(container) container.innerHTML = '<p class="text-danger">Nevar ielādēt kravas.</p>';
            }
        } catch (error) {
            // Kļūda jau tiek apstrādāta apiClient interceptorā un parādīts toast
            console.error("[FRONTEND] CargoManager.loadCargos: Kļūda ielādējot kravas:", error);
            const container = document.getElementById('cargo-list');
            if(container) container.innerHTML = '<p class="text-danger">Kļūda ielādējot kravas.</p>';
        } finally {
            toggleLoading(false);
        }
    },
    renderTable(cargosVM) {
        const container = document.getElementById('cargo-list');
        if (!container) { console.error("Element 'cargo-list' not found for CargoManager.renderTable"); return; }
        let tableHTML = `<div class="table-responsive"><table class="table table-hover"><thead><tr>
            <th>ID</th><th>Statuss</th><th>Sūtītājs</th><th>Saņēmējs</th><th>Maršruts</th><th>Ierīces</th><th>Darbības</th>
            </tr></thead><tbody>`;
        cargosVM.forEach(cargo => {
            // Pārbauda vai cargo.devices ir masīvs pirms map izsaukšanas
            const deviceText = Array.isArray(cargo.devices)
                ? cargo.devices.map(d => `${d.type}(ID:${d.deviceId})`).join(', ')
                : 'Nav';
            tableHTML += `<tr>
                <td>${cargo.cargoId}</td>
                <td><span class="badge ${this.getStatusClass(cargo.status)}">${cargo.status}</span></td>
                <td>${cargo.senderName || 'N/A'}</td>
                <td>${cargo.clientName || 'N/A'}</td>
                <td>${cargo.routeDescription || 'N/A'}</td>
                <td>${deviceText}</td>
                <td class="action-buttons">`;
            // Pārbauda lietotāja tiesības pirms pogu pievienošanas
            if (currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher'))) {
                tableHTML += `<button class="btn btn-sm btn-warning me-1" onclick="CargoManager.showForm(${cargo.cargoId})"><i class="bi bi-pencil"></i></button>
                              <button class="btn btn-sm btn-danger" onclick="CargoManager.delete(${cargo.cargoId})"><i class="bi bi-trash"></i></button>`;
            }
            tableHTML += `</td></tr>`;
        });
        tableHTML += `</tbody></table></div>`;
        container.innerHTML = tableHTML;
    },
    getStatusClass(statusString) {
        const status = String(statusString); // Pārvērš par string drošības pēc
        switch (status) {
            case 'Pending': return 'bg-secondary';
            case 'InTransit': return 'bg-primary';
            case 'Delivered': return 'bg-success';
            case 'Cancelled': return 'bg-danger';
            case 'RouteAssigned': return 'bg-info';
            default: return 'bg-light text-dark'; // Noklusējuma stils
        }
    },
    async showForm(cargoId = null) {
        // Pārbauda tiesības
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        currentEditableId = cargoId; // Saglabā rediģējamās kravas ID
        this.modalTitle.textContent = cargoId ? 'Rediģēt kravu' : 'Pievienot jaunu kravu';
        this.formContentDiv.innerHTML = '<p class="text-center p-3">Ielādē formas datus...</p>';
        if (this.formModal) this.formModal.show(); else console.error("CargoManager.formModal nav inicializēts");

        toggleLoading(true); // Parāda ielādes indikatoru
        try {
            // Paralēli ielādē visus nepieciešamos datus formai
            const [clientsRes, routesRes, dispatchersRes, devicesRes] = await Promise.all([
                apiClient.get(`/Client`),
                apiClient.get(`/Route`),
                apiClient.get(`/Dispatcher`),
                apiClient.get(`/Device`)
            ]);
            // Iegūst datus no atbildēm vai tukšu masīvu, ja neizdodas
            this.allClients = extractData(clientsRes) || [];
            this.allRoutes = extractData(routesRes) || [];
            this.allDispatchers = extractData(dispatchersRes) || [];
            this.allDevices = extractData(devicesRes) || [];

            let cargoDataForForm = {}; // Objekts kravas datiem
            let selectedDeviceIds = []; // Masīvs izvēlēto ierīču ID
            if (cargoId) { // Ja rediģējam esošu kravu
                const cargoRes = await apiClient.get(`${this.API_URL}/${cargoId}`);
                cargoDataForForm = extractData(cargoRes);
                // Ja kravai ir piesaistītas ierīces, iegūst to ID
                if (cargoDataForForm && cargoDataForForm.devices && Array.isArray(cargoDataForForm.devices)) {
                    selectedDeviceIds = cargoDataForForm.devices.map(d => d.deviceId);
                }
            }
            // Renderē formu ar ielādētajiem datiem
            this.renderForm(cargoDataForForm, selectedDeviceIds);
        } catch (error) {
            console.error("Kļūda ielādējot datus kravas formai:", error);
            this.formContentDiv.innerHTML = '<p class="text-danger text-center p-3">Nevar ielādēt formas datus.</p>';
        } finally {
            toggleLoading(false); // Paslēpj ielādes indikatoru
        }
    },
    renderForm(cargoVM = {}, selectedDeviceIds = []) {
        // Veido opcijas statusa izvēlnei
        const statusOptions = ['Pending', 'RouteAssigned', 'InTransit', 'Delivered', 'Cancelled']
            .map(s => `<option value="${s}" ${cargoVM.status === s ? 'selected' : ''}>${s}</option>`).join('');
        // Veido opcijas klientu izvēlnei
        const clientOptions = this.allClients.map(c => `<option value="${c.clientId}" ${cargoVM.clientId === c.clientId ? 'selected' : ''}>${c.name}</option>`).join('');
        // Veido opcijas maršrutu izvēlnei
        const routeOptions = this.allRoutes.map(r => `<option value="${r.routeId}" ${cargoVM.routeId === r.routeId ? 'selected' : ''}>${r.startPoint} → ${r.endPoint}</option>`).join('');
        // Veido opcijas sūtītāju izvēlnei
        const dispatcherOptions = this.allDispatchers.map(d => `<option value="${d.senderId}" ${cargoVM.senderId === d.senderId ? 'selected' : ''}>${d.name}</option>`).join('');
        // Veido checkboxus ierīču izvēlei
        const deviceCheckboxes = this.allDevices.length > 0 ? this.allDevices.map(d => `
            <div class="form-check">
                <input class="form-check-input" type="checkbox" name="selectedDevices" value="${d.deviceId}" id="device-chk-${d.deviceId}" ${selectedDeviceIds.includes(d.deviceId) ? 'checked' : ''}>
                <label class="form-check-label" for="device-chk-${d.deviceId}">${d.type} (ID: ${d.deviceId})</label>
            </div>`).join('') : '<p class="text-muted">Nav pieejamu ierīču.</p>';

        // Ievieto formas HTML modālajā logā
        this.formContentDiv.innerHTML = `
            <form id="cargoFormInternal">
                <div class="mb-3"><label for="statusCargoForm" class="form-label">Statuss</label><select class="form-select" id="statusCargoForm">${statusOptions}</select></div>
                <div class="mb-3"><label for="senderIdCargoForm" class="form-label">Sūtītājs</label><select class="form-select" id="senderIdCargoForm" required><option value="">Izvēlieties...</option>${dispatcherOptions}</select></div>
                <div class="mb-3"><label for="clientIdCargoForm" class="form-label">Saņēmējs</label><select class="form-select" id="clientIdCargoForm" required><option value="">Izvēlieties...</option>${clientOptions}</select></div>
                <div class="mb-3"><label for="routeIdCargoForm" class="form-label">Maršruts</label><select class="form-select" id="routeIdCargoForm" required><option value="">Izvēlieties...</option>${routeOptions}</select></div>
                <div class="mb-3"><label class="form-label">Ierīces</label><div class="form-control-scroll p-2 border rounded" style="max-height: 150px; overflow-y: auto;">${deviceCheckboxes}</div></div>
                <button type="submit" class="btn btn-primary">Saglabāt</button>
            </form>`;
        // Pievieno 'submit' notikuma klausītāju formai
        document.getElementById('cargoFormInternal').addEventListener('submit', (e) => { e.preventDefault(); this.saveCargo(); });
    },
    async saveCargo() {
        // Nolasa datus no formas
        const cargoDto = {
            status: document.getElementById('statusCargoForm').value,
            senderId: parseInt(document.getElementById('senderIdCargoForm').value),
            clientId: parseInt(document.getElementById('clientIdCargoForm').value),
            routeId: parseInt(document.getElementById('routeIdCargoForm').value),
            // Iegūst atlasīto ierīču ID masīvu
            deviceIds: Array.from(document.querySelectorAll('#cargoFormInternal input[name="selectedDevices"]:checked')).map(cb => parseInt(cb.value))
        };

        // Pārbauda vai obligātie lauki ir aizpildīti
        if (!cargoDto.senderId || !cargoDto.clientId || !cargoDto.routeId) {
            showToast('Lūdzu, aizpildiet visus obligātos laukus (Sūtītājs, Saņēmējs, Maršruts).', 'warning');
            return;
        }

        toggleLoading(true); // Parāda ielādes indikatoru
        try {
            if (currentEditableId) { // Ja rediģējam
                cargoDto.cargoId = currentEditableId;
                await apiClient.put(`${this.API_URL}/${currentEditableId}`, cargoDto);
                showToast('Krava veiksmīgi atjaunināta!');
            } else { // Ja pievienojam jaunu
                await apiClient.post(this.API_URL, cargoDto);
                showToast('Jauna krava veiksmīgi pievienota!');
            }
            if (this.formModal) this.formModal.hide(); // Aizver modālo logu
            this.loadCargos(); // Pārlādē kravu sarakstu
        } catch (error) {
            console.error("Kļūda saglabājot kravu:", error);
            // Kļūdas ziņojums jau tiek parādīts no apiClient interceptora
        } finally {
            toggleLoading(false); // Paslēpj ielādes indikatoru
        }
    },
    async delete(cargoId) {
        // Pārbauda tiesības
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        // Prasa apstiprinājumu
        if (confirm('Vai tiešām vēlaties dzēst šo kravu?')) {
            toggleLoading(true);
            try {
                await apiClient.delete(`${this.API_URL}/${cargoId}`);
                showToast('Krava veiksmīgi dzēsta!');
                this.loadCargos(); // Pārlādē sarakstu
            } catch (error) {
                console.error("Kļūda dzēšot kravu:", error);
                // Kļūdas ziņojums no interceptora
            } finally {
                toggleLoading(false);
            }
        }
    }
};


// --- ClientManager ---
// Saglabājam tavu esošo ClientManager struktūru, pielāgojot API izsaukumus un lomu pārbaudes
const ClientManager = {
    API_URL: `/Client`,
    formModalElement: document.getElementById('formModal'),
    formModal: null,
    formContentDiv: document.getElementById('modalContent'),
    modalTitle: document.getElementById('modalTitle'),
    load: async function() { await this.loadClients(); },
    async loadClients() {
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_URL);
            const clientsVM = extractData(response);
            if (clientsVM && Array.isArray(clientsVM)) {
                this.renderTable(clientsVM);
            } else {
                console.error("ClientManager.loadClients: Saņemtie dati nav masīvs.", clientsVM);
                document.getElementById('clients-list').innerHTML = '<p class="text-danger">Nevar ielādēt klientus.</p>';
            }
        } catch (error) {
            console.error("ClientManager.loadClients: Kļūda:", error);
            document.getElementById('clients-list').innerHTML = '<p class="text-danger">Kļūda ielādējot klientus.</p>';
        } finally {
            toggleLoading(false);
        }
    },
    renderTable(clientsVM) {
        const container = document.getElementById('clients-list');
        if (!container) { console.error("Element 'clients-list' not found"); return; }
        let html = `<div class="table-responsive"><table class="table table-hover"><thead><tr><th>ID</th><th>Nosaukums</th><th>Darbības</th></tr></thead><tbody>`;
        clientsVM.forEach(client => {
            html += `<tr><td>${client.clientId}</td><td>${client.name}</td><td class="action-buttons">`;
            if (currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher'))) {
                html += `<button class="btn btn-sm btn-warning me-1" onclick="ClientManager.showForm(${client.clientId})"><i class="bi bi-pencil"></i></button>
                         <button class="btn btn-sm btn-danger" onclick="ClientManager.delete(${client.clientId})"><i class="bi bi-trash"></i></button>`;
            }
            html += `</td></tr>`;
        });
        html += `</tbody></table></div>`;
        container.innerHTML = html;
    },
    async showForm(id = null) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        currentEditableId = id;
        this.modalTitle.textContent = id ? 'Rediģēt Klientu' : 'Jauns Klients';
        this.formContentDiv.innerHTML = '<p class="text-center p-3">Ielādē...</p>';
        if (this.formModal) this.formModal.show();

        let clientData = {};
        if (id) {
            toggleLoading(true);
            try {
                const response = await apiClient.get(`${this.API_URL}/${id}`);
                clientData = extractData(response);
            } catch (e) {
                console.error(e); showToast('Kļūda ielādējot klienta datus.', 'danger');
                this.formContentDiv.innerHTML = '<p class="text-danger text-center p-3">Kļūda ielādējot datus.</p>';
                toggleLoading(false); return;
            } finally { toggleLoading(false); }
        }
        this.renderForm(clientData);
    },
    renderForm(clientVM = {}) {
        this.formContentDiv.innerHTML = `
            <form id="clientFormInternal">
                <div class="mb-3">
                    <label for="clientNameForm" class="form-label">Nosaukums</label>
                    <input type="text" class="form-control" id="clientNameForm" value="${clientVM.name || ''}" required>
                </div>
                <button type="submit" class="btn btn-primary">Saglabāt</button>
            </form>`;
        document.getElementById('clientFormInternal').addEventListener('submit', (e) => { e.preventDefault(); this.saveClient(); });
    },
    async saveClient() {
        const clientDto = {
            name: document.getElementById('clientNameForm').value
        };
        if (!clientDto.name) {
             showToast('Lūdzu, ievadiet klienta nosaukumu.', 'warning');
             return;
        }
        toggleLoading(true);
        try {
            if (currentEditableId) {
                clientDto.clientId = currentEditableId;
                await apiClient.put(`${this.API_URL}/${currentEditableId}`, clientDto);
                showToast('Klients veiksmīgi atjaunināts!');
            } else {
                await apiClient.post(this.API_URL, clientDto);
                showToast('Jauns klients veiksmīgi pievienots!');
            }
            if (this.formModal) this.formModal.hide();
            this.loadClients();
        } catch (error) { console.error("Kļūda saglabājot klientu:", error); }
        finally { toggleLoading(false); }
    },
    async delete(id) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        if (confirm('Vai tiešām vēlaties dzēst šo klientu?')) {
            toggleLoading(true);
            try {
                await apiClient.delete(`${this.API_URL}/${id}`);
                showToast('Klients veiksmīgi dzēsts!');
                this.loadClients();
            } catch (error) { console.error("Kļūda dzēšot klientu:", error); }
            finally { toggleLoading(false); }
        }
    }
};

// --- RouteManager ---
// Saglabājam tavu esošo struktūru, pielāgojam API izsaukumus un lomas
const RouteManager = {
    API_URL: `/Route`, // Izmantojam API_BASE no globālās konfigurācijas
    formModalElement: document.getElementById('formModal'),
    formModal: null,
    formContentDiv: document.getElementById('modalContent'),
    modalTitle: document.getElementById('modalTitle'),
    load: async function() { await this.loadRoutes(); },
    async loadRoutes() {
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_URL);
            const routesVM = extractData(response);
            if (routesVM && Array.isArray(routesVM)) {
                this.renderTable(routesVM);
            } else {
                console.error("RouteManager.loadRoutes: Saņemtie dati nav masīvs.", routesVM);
                document.getElementById('routes-list').innerHTML = '<p class="text-danger">Nevar ielādēt maršrutus.</p>';
            }
        } catch (error) {
            console.error("RouteManager.loadRoutes: Kļūda:", error);
            document.getElementById('routes-list').innerHTML = '<p class="text-danger">Kļūda ielādējot maršrutus.</p>';
        } finally {
            toggleLoading(false);
        }
    },
    renderTable(routesVM) {
        const container = document.getElementById('routes-list');
        if (!container) { console.error("Element 'routes-list' not found"); return; }
        let html = `<div class="table-responsive"><table class="table table-hover"><thead><tr>
            <th>ID</th><th>Sākums</th><th>Beigas</th><th>Pieturas</th><th>Paredz. laiks</th><th>Darbības</th>
            </tr></thead><tbody>`;
        routesVM.forEach(route => {
            // Drošāka wayPoints apstrāde, pārbaudot vai tas ir masīvs
            const wayPointsText = Array.isArray(route.wayPoints) ? route.wayPoints.join(', ') : 'Nav';
            html += `<tr>
                <td>${route.routeId}</td><td>${route.startPoint}</td><td>${route.endPoint}</td>
                <td>${wayPointsText}</td>
                <td>${route.estimatedTime ? new Date(route.estimatedTime).toLocaleString('lv-LV') : 'N/A'}</td>
                <td class="action-buttons">`;
            if (currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher'))) {
                html += `<button class="btn btn-sm btn-warning me-1" onclick="RouteManager.showForm(${route.routeId})"><i class="bi bi-pencil"></i></button>
                         <button class="btn btn-sm btn-danger" onclick="RouteManager.delete(${route.routeId})"><i class="bi bi-trash"></i></button>`;
            }
            html += `</td></tr>`;
        });
        html += `</tbody></table></div>`;
        container.innerHTML = html;
    },
    async showForm(id = null) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        currentEditableId = id;
        this.modalTitle.textContent = id ? 'Rediģēt Maršrutu' : 'Jauns Maršruts';
        this.formContentDiv.innerHTML = '<p class="text-center p-3">Ielādē...</p>';
        if (this.formModal) this.formModal.show();

        let routeData = {};
        if (id) {
            toggleLoading(true);
            try {
                const response = await apiClient.get(`${this.API_URL}/${id}`);
                routeData = extractData(response);
            } catch (e) { console.error(e); showToast('Kļūda ielādējot maršruta datus.', 'danger'); this.formContentDiv.innerHTML = '<p class="text-danger text-center p-3">Kļūda.</p>'; toggleLoading(false); return;}
            finally { toggleLoading(false); }
        }
        this.renderForm(routeData);
    },
    renderForm(routeVM = {}) {
        // Pārveido datumu pareizā formātā datetime-local ievadei
        const estimatedTimeValue = routeVM.estimatedTime ? new Date(new Date(routeVM.estimatedTime).getTime() - (new Date().getTimezoneOffset() * 60000)).toISOString().slice(0, 16) : '';
        // Drošāka wayPoints apstrāde
        const wayPointsValue = Array.isArray(routeVM.wayPoints) ? routeVM.wayPoints.join(', ') : '';
        this.formContentDiv.innerHTML = `
            <form id="routeFormInternal">
                <div class="mb-3"><label for="startPointRouteForm" class="form-label">Sākuma punkts</label><input type="text" class="form-control" id="startPointRouteForm" value="${routeVM.startPoint || ''}" required></div>
                <div class="mb-3"><label for="endPointRouteForm" class="form-label">Beigu punkts</label><input type="text" class="form-control" id="endPointRouteForm" value="${routeVM.endPoint || ''}" required></div>
                <div class="mb-3"><label for="wayPointsRouteForm" class="form-label">Pieturas punkti (atdalīt ar komatu)</label><input type="text" class="form-control" id="wayPointsRouteForm" value="${wayPointsValue}"></div>
                <div class="mb-3"><label for="estimatedTimeRouteForm" class="form-label">Paredzamais laiks</label><input type="datetime-local" class="form-control" id="estimatedTimeRouteForm" value="${estimatedTimeValue}" required></div>
                <button type="submit" class="btn btn-primary">Saglabāt</button>
            </form>`;
        document.getElementById('routeFormInternal').addEventListener('submit', (e) => { e.preventDefault(); this.saveRoute(); });
    },
    async saveRoute() {
        // Pārveido datumu uz ISO string UTC formātā pirms sūtīšanas
        const localDate = document.getElementById('estimatedTimeRouteForm').value;
        let estimatedTimeISO = null;
        if (localDate) {
            try {
                 estimatedTimeISO = new Date(localDate).toISOString();
            } catch (e) {
                 console.error("Nederīgs datuma formāts:", localDate, e);
                 showToast("Lūdzu, ievadiet derīgu datumu un laiku.", "warning");
                 return;
            }
        } else {
             showToast("Lūdzu, ievadiet paredzamo laiku.", "warning");
             return;
        }

        const routeDto = {
            startPoint: document.getElementById('startPointRouteForm').value,
            endPoint: document.getElementById('endPointRouteForm').value,
            // Sadala pieturas punktus, noņem tukšumus un filtrē tukšus ierakstus
            wayPoints: document.getElementById('wayPointsRouteForm').value.split(',').map(p => p.trim()).filter(p => p),
            estimatedTime: estimatedTimeISO
        };

        if (!routeDto.startPoint || !routeDto.endPoint) {
            showToast('Lūdzu, aizpildiet sākuma un beigu punktus.', 'warning');
            return;
        }

        toggleLoading(true);
        try {
            if (currentEditableId) {
                routeDto.routeId = currentEditableId;
                await apiClient.put(`${this.API_URL}/${currentEditableId}`, routeDto);
                showToast('Maršruts veiksmīgi atjaunināts!');
            } else {
                await apiClient.post(this.API_URL, routeDto);
                showToast('Jauns maršruts veiksmīgi pievienots!');
            }
            if (this.formModal) this.formModal.hide();
            this.loadRoutes();
        } catch (error) { console.error("Kļūda saglabājot maršrutu:", error); }
        finally { toggleLoading(false); }
    },
    async delete(id) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        if (confirm('Vai tiešām vēlaties dzēst šo maršrutu?')) {
            toggleLoading(true);
            try {
                await apiClient.delete(`${this.API_URL}/${id}`);
                showToast('Maršruts veiksmīgi dzēsts!');
                this.loadRoutes();
            } catch (error) { console.error("Kļūda dzēšot maršrutu:", error); }
            finally { toggleLoading(false); }
        }
    }
};


// ==================== TRANSPORTLĪDZEKĻU PĀRVALDĪBA (VehicleManager) ====================

const VehicleManager = {
    API_URL: `/Vehicle`,
    formModal: null,
    formContentDiv: document.getElementById('modalContent'),
    modalTitle: document.getElementById('modalTitle'),
    load: async function() { await this.loadVehicles(); },
    async loadVehicles() {
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_URL);
            const vehiclesVM = extractData(response); // Sagaida VehicleViewModel[]
            if (vehiclesVM && Array.isArray(vehiclesVM)) {
                this.renderTable(vehiclesVM);
            } else {
                console.error("VehicleManager.loadVehicles: Saņemtie dati nav masīvs.", vehiclesVM);
                document.getElementById('vehicles-list').innerHTML = '<p class="text-danger">Nevar ielādēt transportlīdzekļus.</p>';
            }
        } catch (error) {
            console.error("VehicleManager.loadVehicles: Kļūda:", error);
            document.getElementById('vehicles-list').innerHTML = '<p class="text-danger">Kļūda ielādējot transportlīdzekļus.</p>';
        } finally {
            toggleLoading(false);
        }
    },
    renderTable(vehiclesVM) {
        const container = document.getElementById('vehicles-list');
        if (!container) { console.error("Element 'vehicles-list' not found"); return; }
        let html = `<div class="table-responsive"><table class="table table-hover"><thead><tr>
            <th>ID</th><th>Valsts numurs</th><th>Vadītāja vārds</th><th>Darbības</th>
            </tr></thead><tbody>`;
        vehiclesVM.forEach(vehicle => {
            html += `<tr>
                <td>${vehicle.vehicleId}</td>
                <td>${vehicle.licensePlate || 'N/A'}</td>
                <td>${vehicle.driverName || 'N/A'}</td>
                <td class="action-buttons">`;
            if (currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher'))) {
                html += `<button class="btn btn-sm btn-warning me-1" onclick="VehicleManager.showForm(${vehicle.vehicleId})"><i class="bi bi-pencil"></i></button>
                         <button class="btn btn-sm btn-danger" onclick="VehicleManager.delete(${vehicle.vehicleId})"><i class="bi bi-trash"></i></button>`;
            }
            html += `</td></tr>`;
        });
        html += `</tbody></table></div>`;
        container.innerHTML = html;
    },
    async showForm(id = null) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        currentEditableId = id;
        this.modalTitle.textContent = id ? 'Rediģēt Transportlīdzekli' : 'Jauns Transportlīdzeklis';
        this.formContentDiv.innerHTML = '<p class="text-center p-3">Ielādē...</p>';
        if (this.formModal) this.formModal.show(); else console.error("VehicleManager.formModal nav inicializēts.");

        let vehicleData = {};
        if (id) {
            toggleLoading(true);
            try {
                const response = await apiClient.get(`${this.API_URL}/${id}`);
                vehicleData = extractData(response);
            } catch (e) {
                console.error(e); showToast('Kļūda ielādējot transportlīdzekļa datus.', 'danger');
                this.formContentDiv.innerHTML = '<p class="text-danger text-center p-3">Kļūda.</p>';
                toggleLoading(false); return;
            } finally { toggleLoading(false); }
        }
        this.renderForm(vehicleData);
    },
    renderForm(vehicleVM = {}) {
        this.formContentDiv.innerHTML = `
            <form id="vehicleFormInternal">
                <div class="mb-3">
                    <label for="licensePlateVehicleForm" class="form-label">Valsts numurs</label>
                    <input type="text" class="form-control" id="licensePlateVehicleForm" value="${vehicleVM.licensePlate || ''}" required>
                </div>
                <div class="mb-3">
                    <label for="driverNameVehicleForm" class="form-label">Vadītāja vārds</label>
                    <input type="text" class="form-control" id="driverNameVehicleForm" value="${vehicleVM.driverName || ''}" required>
                </div>
                <button type="submit" class="btn btn-primary">Saglabāt</button>
            </form>`;
        document.getElementById('vehicleFormInternal').addEventListener('submit', (e) => { e.preventDefault(); this.saveVehicle(); });
    },
    async saveVehicle() {
        const vehicleDto = {
            licensePlate: document.getElementById('licensePlateVehicleForm').value,
            driverName: document.getElementById('driverNameVehicleForm').value
        };
        if (!vehicleDto.licensePlate || !vehicleDto.driverName) {
            showToast('Lūdzu, aizpildiet abus laukus.', 'warning');
            return;
        }
        toggleLoading(true);
        try {
            if (currentEditableId) {
                vehicleDto.vehicleId = currentEditableId;
                await apiClient.put(`${this.API_URL}/${currentEditableId}`, vehicleDto);
                showToast('Transportlīdzeklis veiksmīgi atjaunināts!');
            } else {
                await apiClient.post(this.API_URL, vehicleDto);
                showToast('Jauns transportlīdzeklis veiksmīgi pievienots!');
            }
            if (this.formModal) this.formModal.hide();
            this.loadVehicles();
        } catch (error) { console.error("Kļūda saglabājot transportlīdzekli:", error); }
        finally { toggleLoading(false); }
    },
    async delete(id) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        if (confirm('Vai tiešām vēlaties dzēst šo transportlīdzekli?')) {
            toggleLoading(true);
            try {
                await apiClient.delete(`${this.API_URL}/${id}`);
                showToast('Transportlīdzeklis veiksmīgi dzēsts!');
                this.loadVehicles();
            } catch (error) { console.error("Kļūda dzēšot transportlīdzekli:", error); }
            finally { toggleLoading(false); }
        }
    }
};

// ==================== DISPEČERU PĀRVALDĪBA (DispatcherManager) ====================
const DispatcherManager = {
    API_URL: `/Dispatcher`,
    formModal: null,
    formContentDiv: document.getElementById('modalContent'),
    modalTitle: document.getElementById('modalTitle'),
    load: async function() { await this.loadDispatchers(); },
    async loadDispatchers() {
        // Tikai Admin var redzēt visus dispečerus
        if (!(currentUser && Array.isArray(currentUser.roles) && currentUser.roles.includes('Admin'))) {
            document.getElementById('dispatchers-list').innerHTML = '<p class="text-muted">Jums nav tiesību skatīt šo sadaļu.</p>';
            return;
        }
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_URL);
            const dispatchersVM = extractData(response);
            if (dispatchersVM && Array.isArray(dispatchersVM)) {
                this.renderTable(dispatchersVM);
            } else {
                console.error("DispatcherManager.loadDispatchers: Saņemtie dati nav masīvs.", dispatchersVM);
                document.getElementById('dispatchers-list').innerHTML = '<p class="text-danger">Nevar ielādēt sūtītājus.</p>';
            }
        } catch (error) {
            console.error("DispatcherManager.loadDispatchers: Kļūda:", error);
            document.getElementById('dispatchers-list').innerHTML = '<p class="text-danger">Kļūda ielādējot sūtītājus.</p>';
        } finally {
            toggleLoading(false);
        }
    },
    renderTable(dispatchersVM) {
        const container = document.getElementById('dispatchers-list');
        if (!container) { console.error("Element 'dispatchers-list' not found"); return; }
        let html = `<div class="table-responsive"><table class="table table-hover"><thead><tr>
            <th>ID</th><th>Nosaukums</th><th>E-pasts</th><th>Tālrunis</th><th>Darbības</th>
            </tr></thead><tbody>`;
        dispatchersVM.forEach(dispatcher => {
            html += `<tr>
                <td>${dispatcher.senderId}</td>
                <td>${dispatcher.name || 'N/A'}</td>
                <td>${dispatcher.email || 'N/A'}</td>
                <td>${dispatcher.phone || 'N/A'}</td>
                <td class="action-buttons">`;
            if (currentUser && Array.isArray(currentUser.roles) && currentUser.roles.includes('Admin')) {
                html += `<button class="btn btn-sm btn-warning me-1" onclick="DispatcherManager.showForm(${dispatcher.senderId})"><i class="bi bi-pencil"></i></button>
                         <button class="btn btn-sm btn-danger" onclick="DispatcherManager.delete(${dispatcher.senderId})"><i class="bi bi-trash"></i></button>`;
            }
            html += `</td></tr>`;
        });
        html += `</tbody></table></div>`;
        container.innerHTML = html;
    },
    async showForm(id = null) {
        if (!(currentUser && Array.isArray(currentUser.roles) && currentUser.roles.includes('Admin'))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        currentEditableId = id;
        this.modalTitle.textContent = id ? 'Rediģēt Sūtītāju' : 'Jauns Sūtītājs';
        this.formContentDiv.innerHTML = '<p class="text-center p-3">Ielādē...</p>';
        if (this.formModal) this.formModal.show();

        let dispatcherData = {};
        if (id) {
            toggleLoading(true);
            try {
                const response = await apiClient.get(`${this.API_URL}/${id}`);
                dispatcherData = extractData(response);
            } catch (e) {
                console.error(e); showToast('Kļūda ielādējot sūtītāja datus.', 'danger');
                this.formContentDiv.innerHTML = '<p class="text-danger text-center p-3">Kļūda.</p>';
                toggleLoading(false); return;
            } finally { toggleLoading(false); }
        }
        this.renderForm(dispatcherData);
    },
    renderForm(dispatcherVM = {}) {
        this.formContentDiv.innerHTML = `
            <form id="dispatcherFormInternal">
                <div class="mb-3">
                    <label for="dispatcherNameForm" class="form-label">Nosaukums</label>
                    <input type="text" class="form-control" id="dispatcherNameForm" value="${dispatcherVM.name || ''}" required>
                </div>
                <div class="mb-3">
                    <label for="dispatcherEmailForm" class="form-label">E-pasts</label>
                    <input type="email" class="form-control" id="dispatcherEmailForm" value="${dispatcherVM.email || ''}">
                </div>
                <div class="mb-3">
                    <label for="dispatcherPhoneForm" class="form-label">Tālrunis</label>
                    <input type="tel" class="form-control" id="dispatcherPhoneForm" value="${dispatcherVM.phone || ''}">
                </div>
                <button type="submit" class="btn btn-primary">Saglabāt</button>
            </form>`;
        document.getElementById('dispatcherFormInternal').addEventListener('submit', (e) => { e.preventDefault(); this.saveDispatcher(); });
    },
    async saveDispatcher() {
        const dispatcherDto = {
            name: document.getElementById('dispatcherNameForm').value,
            email: document.getElementById('dispatcherEmailForm').value,
            phone: document.getElementById('dispatcherPhoneForm').value
        };
        if (!dispatcherDto.name) {
             showToast('Lūdzu, ievadiet sūtītāja nosaukumu.', 'warning');
             return;
        }
        toggleLoading(true);
        try {
            if (currentEditableId) {
                dispatcherDto.senderId = currentEditableId;
                await apiClient.put(`${this.API_URL}/${currentEditableId}`, dispatcherDto);
                showToast('Sūtītājs veiksmīgi atjaunināts!');
            } else {
                await apiClient.post(this.API_URL, dispatcherDto);
                showToast('Jauns sūtītājs veiksmīgi pievienots!');
            }
            if (this.formModal) this.formModal.hide();
            this.loadDispatchers();
        } catch (error) { console.error("Kļūda saglabājot sūtītāju:", error); }
        finally { toggleLoading(false); }
    },
    async delete(id) {
        if (!(currentUser && Array.isArray(currentUser.roles) && currentUser.roles.includes('Admin'))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        if (confirm('Vai tiešām vēlaties dzēst šo sūtītāju?')) {
            toggleLoading(true);
            try {
                await apiClient.delete(`${this.API_URL}/${id}`);
                showToast('Sūtītājs veiksmīgi dzēsts!');
                this.loadDispatchers();
            } catch (error) { console.error("Kļūda dzēšot sūtītāju:", error); }
            finally { toggleLoading(false); }
        }
    }
};

// ==================== IERĪČU PĀRVALDĪBA (DeviceManager) ====================
const DeviceManager = {
    API_URL: `/Device`,
    formModal: null,
    formContentDiv: document.getElementById('modalContent'),
    modalTitle: document.getElementById('modalTitle'),
    load: async function() { await this.loadDevices(); },
    async loadDevices() {
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_URL);
            const devicesVM = extractData(response);
            if (devicesVM && Array.isArray(devicesVM)) {
                this.renderTable(devicesVM);
            } else {
                console.error("DeviceManager.loadDevices: Saņemtie dati nav masīvs.", devicesVM);
                document.getElementById('devices-list').innerHTML = '<p class="text-danger">Nevar ielādēt ierīces.</p>';
            }
        } catch (error) {
            console.error("DeviceManager.loadDevices: Kļūda:", error);
            document.getElementById('devices-list').innerHTML = '<p class="text-danger">Kļūda ielādējot ierīces.</p>';
        } finally {
            toggleLoading(false);
        }
    },
    renderTable(devicesVM) {
        const container = document.getElementById('devices-list');
        if (!container) { console.error("Element 'devices-list' not found"); return; }
        let html = `<div class="table-responsive"><table class="table table-hover"><thead><tr>
            <th>ID</th><th>Tips</th><th>Platums</th><th>Garums</th><th>Pēd. atjaun.</th><th>Krava ID</th><th>Darbības</th>
            </tr></thead><tbody>`;
        devicesVM.forEach(device => {
            html += `<tr>
                <td>${device.deviceId}</td>
                <td>${device.type}</td>
                <td>${typeof device.latitude === 'number' ? device.latitude.toFixed(6) : 'N/A'}</td>
                <td>${typeof device.longitude === 'number' ? device.longitude.toFixed(6) : 'N/A'}</td>
                <td>${device.lastUpdate ? new Date(device.lastUpdate).toLocaleString('lv-LV') : 'N/A'}</td>
                <td>${device.cargoId || 'Nav piesaistīta'}</td>
                <td class="action-buttons">`;
            if (currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher'))) {
                html += `<button class="btn btn-sm btn-warning me-1" onclick="DeviceManager.showForm(${device.deviceId})"><i class="bi bi-pencil"></i></button>
                         <button class="btn btn-sm btn-danger" onclick="DeviceManager.delete(${device.deviceId})"><i class="bi bi-trash"></i></button>`;
            }
            html += `</td></tr>`;
        });
        html += `</tbody></table></div>`;
        container.innerHTML = html;
    },
    async showForm(id = null) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        currentEditableId = id;
        this.modalTitle.textContent = id ? 'Rediģēt Ierīci' : 'Jauna Ierīce';
        this.formContentDiv.innerHTML = '<p class="text-center p-3">Ielādē...</p>';
        if (this.formModal) this.formModal.show();

        let deviceData = {};
        if (id) {
            toggleLoading(true);
            try {
                const response = await apiClient.get(`${this.API_URL}/${id}`);
                deviceData = extractData(response);
            } catch (e) {
                console.error(e); showToast('Kļūda ielādējot ierīces datus.', 'danger');
                this.formContentDiv.innerHTML = '<p class="text-danger text-center p-3">Kļūda.</p>';
                toggleLoading(false); return;
            } finally { toggleLoading(false); }
        }
        this.renderForm(deviceData);
    },
    renderForm(deviceVM = {}) {
        const deviceTypeOptions = ['GPS', 'RFID', 'Sensor']
            .map(type => `<option value="${type}" ${deviceVM.type === type ? 'selected' : ''}>${type}</option>`).join('');

        this.formContentDiv.innerHTML = `
            <form id="deviceFormInternal">
                <div class="mb-3">
                    <label for="deviceTypeForm" class="form-label">Tips</label>
                    <select class="form-select" id="deviceTypeForm" required>${deviceTypeOptions}</select>
                </div>
                <div class="mb-3">
                    <label for="latitudeDeviceForm" class="form-label">Platums (Latitude)</label>
                    <input type="number" step="0.000001" class="form-control" id="latitudeDeviceForm" value="${deviceVM.latitude || '0.0'}" required>
                </div>
                <div class="mb-3">
                    <label for="longitudeDeviceForm" class="form-label">Garums (Longitude)</label>
                    <input type="number" step="0.000001" class="form-control" id="longitudeDeviceForm" value="${deviceVM.longitude || '0.0'}" required>
                </div>
                <div class="mb-3">
                    <label for="cargoIdDeviceForm" class="form-label">Kravas ID (ja piesaistīta, atstāt tukšu, ja nē)</label>
                    <input type="number" class="form-control" id="cargoIdDeviceForm" value="${deviceVM.cargoId || ''}">
                </div>
                 <div class="mb-3">
                     <label class="form-label">Izvēlēties atrašanās vietu kartē:</label>
                     <div id="device-form-map-container" style="height: 300px; width: 100%;"></div>
                     <small class="form-text text-muted">Noklikšķiniet kartē, lai atjauninātu koordinātes.</small>
                </div>
                <button type="submit" class="btn btn-primary">Saglabāt</button>
            </form>`;

        // Inicializē kartes izvēli modālajā logā tikai tad, ja MapManager un tā metodes ir pieejamas
        if (window.MapManager && MapManager.MapPicker && typeof MapManager.MapPicker.initMap === 'function') {
             // Neliela aizkave, lai nodrošinātu, ka modālais logs ir pilnībā redzams pirms kartes inicializācijas
             setTimeout(() => {
                MapManager.MapPicker.initMap('device-form-map-container', parseFloat(deviceVM.latitude) || 56.946, parseFloat(deviceVM.longitude) || 24.105); // Rīgas koordinātes kā noklusējums
            }, 200);
        } else {
            console.warn("MapManager vai MapPicker nav pieejams ierīces formas kartes inicializācijai.");
            // Varētu paslēpt kartes konteineri vai parādīt paziņojumu
            const mapContainer = document.getElementById('device-form-map-container');
            if(mapContainer) mapContainer.innerHTML = '<p class="text-warning">Kartes izvēle nav pieejama.</p>';
        }

        document.getElementById('deviceFormInternal').addEventListener('submit', (e) => { e.preventDefault(); this.saveDevice(); });
    },
    async saveDevice() {
        const deviceDto = {
            type: document.getElementById('deviceTypeForm').value,
            latitude: parseFloat(document.getElementById('latitudeDeviceForm').value),
            longitude: parseFloat(document.getElementById('longitudeDeviceForm').value),
            // Pārveido tukšu string par null, citādi par skaitli
            cargoId: document.getElementById('cargoIdDeviceForm').value ? parseInt(document.getElementById('cargoIdDeviceForm').value) : null
        };

        if (isNaN(deviceDto.latitude) || isNaN(deviceDto.longitude)) {
            showToast('Lūdzu, ievadiet derīgas skaitliskas vērtības koordinātēm.', 'warning');
            return;
        }
        // Pārbauda vai cargoId ir skaitlis, ja tas nav null
        if (deviceDto.cargoId !== null && isNaN(deviceDto.cargoId)) {
             showToast('Lūdzu, ievadiet derīgu skaitlisku vērtību Kravas ID vai atstājiet to tukšu.', 'warning');
             return;
        }

        toggleLoading(true);
        try {
            if (currentEditableId) {
                deviceDto.deviceId = currentEditableId;
                await apiClient.put(`${this.API_URL}/${currentEditableId}`, deviceDto);
                showToast('Ierīce veiksmīgi atjaunināta!');
            } else {
                await apiClient.post(this.API_URL, deviceDto);
                showToast('Jauna ierīce veiksmīgi pievienota!');
            }
            if (this.formModal) this.formModal.hide();
            this.loadDevices();
        } catch (error) { console.error("Kļūda saglabājot ierīci:", error); }
        finally { toggleLoading(false); }
    },
    async delete(id) {
        if (!(currentUser && Array.isArray(currentUser.roles) && (currentUser.roles.includes('Admin') || currentUser.roles.includes('Dispatcher')))) {
            showToast('Jums nav tiesību veikt šo darbību.', 'danger'); return;
        }
        if (confirm('Vai tiešām vēlaties dzēst šo ierīci?')) {
            toggleLoading(true);
            try {
                await apiClient.delete(`${this.API_URL}/${id}`);
                showToast('Ierīce veiksmīgi dzēsta!');
                this.loadDevices();
            } catch (error) { console.error("Kļūda dzēšot ierīci:", error); }
            finally { toggleLoading(false); }
        }
    }
};


// ==================== KARTES PĀRVALDĪBA (MapManager) ====================
const MapManager = {
    API_DEVICES_URL: `/Map/GetDevices`, // MapController galapunkts, kas atgriež MapDeviceViewModel[]
    // API_DEVICE_ROUTE_URL: `/Route/ForCargo`, // Piemērs, ja būtu specifisks maršruta galapunkts kravai
    API_DEVICE_HISTORY_URL: `/Device`,
    map: null,             // Galvenās kartes Leaflet instance
    deviceMarkers: [],     // Masīvs ar visiem Leaflet marķieriem kartē
    routeLayers: {},       // Objekts, lai glabātu maršrutu slāņus (key: deviceId, value: L.polyline)
    selectedDeviceId: null,// Pašreiz izvēlētās ierīces ID sānjoslā
    allMapDevices: [],     // Masīvs, kurā glabāt visus no API ielādētos ierīču datus (MapDeviceViewModel)
    updateInterval: null,  // Intervāla ID automātiskai atjaunināšanai
    UPDATE_FREQUENCY: 30000, // Atjaunināšanas biežums milisekundēs (piem., 30 sekundes)

    // Objekts kartes izvēles funkcionalitātei modālajos logos (piem., ierīces formas)
    MapPicker: {
        map: null,            // Leaflet kartes instance modālajam logam
        marker: null,         // Marķieris izvēlētajai atrašanās vietai modālajā logā
        mapElementId: null,   // ID HTML elementam, kurā karte tiek inicializēta

        // Inicializē karti norādītajā HTML elementā ar ID `elementId`
        initMap(elementId, initialLat = 56.946, initialLng = 24.105) { // Noklusējuma koordinātes Rīgai
            this.mapElementId = elementId;
            const mapElement = document.getElementById(elementId);

            if (!mapElement) {
                console.error(`[MapPicker] Kartes elements ar ID "${elementId}" nav atrasts.`);
                return;
            }
            // Ja karte jau ir inicializēta šajā elementā, vispirms to iznīcina
            if (this.map !== null) {
                this.destroyMap();
            }
            try {
                this.map = L.map(elementId).setView([initialLat, initialLng], 13); // Sākotnējais skats un zoom
                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                }).addTo(this.map);
                this.map.on('click', this.onMapClick.bind(this)); // Piesaista klikšķa notikumu
                // Ja ir norādītas sākotnējās koordinātes (nevis 0,0), pievieno marķieri
                if (initialLat !== 0 || initialLng !== 0) {
                     this.addMarker(initialLat, initialLng);
                }
                // Pārliecinās, ka karte pareizi attēlojas modālajā logā
                setTimeout(() => { if (this.map) this.map.invalidateSize(); }, 250); // Nedaudz lielāka aizkave
            } catch(e) {
                console.error(`[MapPicker] Kļūda inicializējot Leaflet karti elementā '${elementId}':`, e);
                mapElement.innerHTML = '<p class="text-danger">Nevarēja ielādēt karti.</p>';
            }
        },

        // Apstrādā kartes klikšķa notikumu modālajā logā
        onMapClick(e) {
            if (!this.map) return; // Pārbaude vai karte eksistē
            const lat = e.latlng.lat.toFixed(6);
            const lng = e.latlng.lng.toFixed(6);
            // Atjaunina formas laukus ar izvēlētajām koordinātēm
            // Pieņem, ka formas laukiem ir ID 'latitudeDeviceForm' un 'longitudeDeviceForm'
            const latInput = document.getElementById('latitudeDeviceForm');
            const lngInput = document.getElementById('longitudeDeviceForm');
            if (latInput) latInput.value = lat;
            if (lngInput) lngInput.value = lng;
            this.addMarker(e.latlng.lat, e.latlng.lng); // Pievieno/pārvieto marķieri
        },

        // Pievieno marķieri kartē vai pārvieto esošo
        addMarker(lat, lng) {
            if (this.map === null) return; // Pārbaude vai karte eksistē
            // Noņem iepriekšējo marķieri, ja tas eksistē un ir kartē
            if (this.marker !== null && this.map.hasLayer(this.marker)) {
                 this.map.removeLayer(this.marker);
            }
            // Pievieno jaunu marķieri
            this.marker = L.marker([lat, lng]).addTo(this.map);
        },

        // Iznīcina kartes instanci un notīra resursus
        destroyMap() {
            if (this.map !== null) {
                this.map.off(); // Noņem visus notikumu klausītājus
                this.map.remove(); // Noņem karti no DOM
                this.map = null;
                this.marker = null;
                this.mapElementId = null;
                console.log("[MapPicker] Kartes instance modālajā logā iznīcināta.");
            }
        }
    },

    // Inicializē galveno karti
    initMap() {
        const mapElement = document.getElementById('map');
        if (mapElement) {
             // Pārbauda vai karte jau nav inicializēta
            if (this.map) {
                 console.log("Galvenā karte jau inicializēta. Tikai pārlādē datus.");
                 this.loadDevices(); // Ja karte jau ir, tikai ielādē datus
                 return;
            }
            try {
                this.map = L.map(mapElement).setView([56.946, 24.105], 7); // Rīgas centrs, zoom 7
                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                }).addTo(this.map);
                // Klikšķis uz kartes paslēpj visus maršrutus
                this.map.on('click', () => this.hideAllRoutes());
                console.log("Galvenā karte veiksmīgi inicializēta.");
                // Pēc inicializācijas ielādē datus
                this.loadDevices();
                // Sāk automātisko atjaunināšanu
                this.startAutoUpdate();
            } catch(e) {
                console.error("Kļūda inicializējot galveno Leaflet karti:", e);
                mapElement.innerHTML = '<p class="text-danger">Nevarēja ielādēt karti.</p>';
            }
        } else {
            console.warn("Kartes elements 'map' nav atrasts initMap laikā.");
        }
    },

    // Sāk automātisko datu atjaunināšanu
    startAutoUpdate() {
        // Pārtrauc iepriekšējo intervālu, ja tāds ir
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
        }
        // Sāk jaunu intervālu
        this.updateInterval = setInterval(() => {
            console.log("Automātiskā kartes datu atjaunināšana...");
            this.loadDevices();
        }, this.UPDATE_FREQUENCY);
        console.log(`Kartes automātiskā atjaunināšana sākta (ik pēc ${this.UPDATE_FREQUENCY / 1000} sekundēm).`);
    },

    // Pārtrauc automātisko datu atjaunināšanu
    stopAutoUpdate() {
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
            console.log("Kartes automātiskā atjaunināšana pārtraukta.");
        }
    },

    // Ielādē ierīču datus no API un atjaunina karti un sarakstu
    async loadDevices() {
        // Pārbauda vai karte ir inicializēta
        if (!this.map) {
             console.warn("Mēģinājums ielādēt ierīces pirms kartes inicializācijas.");
             return;
        }
        toggleLoading(true);
        try {
            const response = await apiClient.get(this.API_DEVICES_URL); // Izmanto MapController galapunktu
            const devicesVM = extractData(response); // Sagaida MapDeviceViewModel[]
            if (devicesVM && Array.isArray(devicesVM)) {
                // Filtrē ierīces, kurām ir derīgas koordinātes (nav null vai 0,0)
                this.allMapDevices = devicesVM.filter(d =>
                    typeof d.latitude === 'number' && typeof d.longitude === 'number' &&
                    (d.latitude !== 0 || d.longitude !== 0)
                );
                this.updateDeviceList(this.allMapDevices);
                this.updateDeviceMarkers(this.allMapDevices);
                this.updateLastUpdatedTime();
            } else {
                console.warn("[FRONTEND] MapManager.loadDevices: Nav ierīču ar koordinātēm vai saņemti nepareizi dati.", devicesVM);
                this.allMapDevices = []; // Notīra iepriekšējos datus
                const container = document.getElementById('mapDeviceList');
                if (container) container.innerHTML = '<li class="list-group-item text-center p-3 text-muted">Nav ierīču ar atrašanās vietas datiem.</li>';
                 this.updateDeviceMarkers([]); // Notīra marķierus no kartes
            }
        } catch (error) {
            console.error("[FRONTEND] MapManager.loadDevices: Kļūda:", error);
            const container = document.getElementById('mapDeviceList');
            if (container) container.innerHTML = '<li class="list-group-item text-center p-3 text-danger">Nevar ielādēt ierīces kartei.</li>';
        } finally {
            toggleLoading(false);
        }
    },

    // Atjaunina ierīču sarakstu sānjoslā
    updateDeviceList(devices) {
        const deviceListElement = document.getElementById('mapDeviceList');
        if (!deviceListElement) { console.error("Elements 'mapDeviceList' nav atrasts."); return; }
        deviceListElement.innerHTML = ''; 
        if (!Array.isArray(devices) || devices.length === 0) {
            deviceListElement.innerHTML = '<li class="list-group-item text-center p-3 text-muted">Nav atrasta neviena ierīce.</li>';
            return;
        }
        devices.forEach(deviceVM => {
            const isActive = this.isDeviceActive(deviceVM.lastUpdate);
            const listItem = document.createElement('li');
            listItem.className = `device-list-item list-group-item list-group-item-action d-flex justify-content-between align-items-center p-2 ${this.selectedDeviceId === deviceVM.deviceId ? 'active' : ''}`;
            listItem.style.cursor = 'pointer';
            listItem.innerHTML = `
                <div class="flex-grow-1 me-2" style="overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                    <span class="status-badge ${isActive ? 'status-active' : 'status-inactive'} me-1"></span>
                    <span class="fw-medium">ID: ${deviceVM.deviceId}</span> <small>(${deviceVM.type})</small>
                    <div class="small text-muted">Atj.: ${this.formatLastUpdate(deviceVM.lastUpdate)}</div>
                    ${deviceVM.cargoId ? `<div class="small text-muted">Krava: #${deviceVM.cargoId}</div>` : ''}
                </div>
                <div>
                    <button class="btn btn-sm btn-outline-info py-0 px-1 me-1 flex-shrink-0" onclick="MapManager.showDeviceHistoryRoute(${deviceVM.deviceId}, event)" title="Rādīt vēsturi">
                        <i class="fas fa-route"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-primary py-0 px-1 flex-shrink-0" onclick="MapManager.flyToDevice(${deviceVM.deviceId}, event)" title="Rādīt kartē">
                        <i class="fas fa-map-marker-alt"></i>
                    </button>
                </div>`;
            listItem.addEventListener('click', (e) => {
                if (e.target.tagName !== 'BUTTON' && !e.target.closest('button')) {
                    this.showDeviceDetails(deviceVM);
                }
            });
            deviceListElement.appendChild(listItem);
        });
    },  

    // Filtrē ierīču sarakstu un marķierus kartē, pamatojoties uz ievadi filtrā
    filterDeviceList() {
        const filterText = document.getElementById('mapDeviceFilter').value.toLowerCase();
        // Filtrē no pilnā `allMapDevices` saraksta
        const filteredDevices = this.allMapDevices.filter(device => {
            const deviceIdMatch = device.deviceId.toString().includes(filterText);
            const typeMatch = device.type.toLowerCase().includes(filterText);
            const cargoIdMatch = device.cargoId ? device.cargoId.toString().includes(filterText) : false;
            return deviceIdMatch || typeMatch || cargoIdMatch;
        });
        // Atjauno sarakstu un marķierus ar filtrētajiem rezultātiem
        this.updateDeviceList(filteredDevices);
        this.updateDeviceMarkers(filteredDevices);
    },

    // Atjaunina marķierus kartē
    updateDeviceMarkers(devices) {
        if (!this.map) return; // Pārbauda vai karte ir inicializēta
        // Noņem visus iepriekšējos marķierus no kartes
        this.deviceMarkers.forEach(marker => {
            if (this.map.hasLayer(marker)) this.map.removeLayer(marker);
        });
        this.deviceMarkers = []; // Notīra marķieru masīvu

        // Pievieno jaunus marķierus katrai ierīcei no filtrētā saraksta
        devices.forEach(deviceVM => { // deviceVM šeit ir MapDeviceViewModel
            const isActive = this.isDeviceActive(deviceVM.lastUpdate);
            // Pievieno 'selected-map-marker' klasi, ja šī ir atlasītā ierīce
            const markerClass = `custom-marker ${isActive ? 'active-marker' : 'inactive-marker'} ${this.selectedDeviceId === deviceVM.deviceId ? 'selected-map-marker' : ''}`;
            // Izveido pielāgotu ikonu
            const deviceIcon = L.divIcon({
                className: markerClass,
                html: `<div class="marker-pin"></div><div class="marker-label">${deviceVM.deviceId}</div>`,
                iconSize: [30, 42],
                iconAnchor: [15, 42] // Punkts, kur ikona "pieskaras" kartei
            });

            try {
                // Izveido marķieri ar pielāgoto ikonu un datiem
                const marker = L.marker([deviceVM.latitude, deviceVM.longitude], {
                    icon: deviceIcon,
                    deviceId: deviceVM.deviceId, // Saglabājam ID, lai vieglāk atrastu
                    deviceData: deviceVM // Saglabājam visus datus
                }).addTo(this.map);

                // Pievieno klikšķa notikumu marķierim
                marker.on('click', (e) => {
                    // Izsauc detaļu parādīšanu ar saglabātajiem datiem
                    this.showDeviceDetails(e.target.options.deviceData);
                });
                // Pievieno marķieri kopējam sarakstam
                this.deviceMarkers.push(marker);
            } catch(e) {
                 console.error(`Kļūda pievienojot marķieri ierīcei ${deviceVM.deviceId}:`, e, deviceVM);
            }
        });
    },

    // Parāda izvēlētās ierīces detaļas apakšējā panelī
    showDeviceDetails(deviceVM) { // deviceVM šeit ir MapDeviceViewModel
        if (!deviceVM || typeof deviceVM.deviceId === 'undefined') {
             console.warn("showDeviceDetails saņēma nederīgus datus:", deviceVM);
             return;
        }
        this.selectedDeviceId = deviceVM.deviceId; // Atjauno atlasītās ierīces ID

        // Atjauno detaļu paneļa laukus
        document.getElementById('map-detail-deviceId').textContent = deviceVM.deviceId;
        document.getElementById('map-detail-lastUpdate').textContent = this.formatLastUpdate(deviceVM.lastUpdate);
        // Pārbauda vai koordinātes ir skaitļi pirms toFixed izsaukšanas
        const latText = typeof deviceVM.latitude === 'number' ? deviceVM.latitude.toFixed(4) : 'N/A';
        const lonText = typeof deviceVM.longitude === 'number' ? deviceVM.longitude.toFixed(4) : 'N/A';
        document.getElementById('map-detail-location').textContent = `${latText}, ${lonText}`;

        // Atjauno saraksta un marķieru izskatu, lai izceltu atlasīto
        // Izmanto filterDeviceList, lai ņemtu vērā arī esošo filtru
        this.filterDeviceList();

        // Atrod atlasīto marķieri
        const selectedMarker = this.deviceMarkers.find(m => m.options.deviceId === deviceVM.deviceId);
        // Ja marķieris atrasts un karte eksistē
        if (selectedMarker && this.map) {
            // "Aizlido" uz marķieri ar animāciju
            this.map.flyTo(selectedMarker.getLatLng(), this.map.getZoom() < 13 ? 13 : this.map.getZoom() , { animate: true, duration: 0.5 });
            // Šeit varētu atvērt popup, ja nepieciešams
            // selectedMarker.bindPopup(`<b>Ierīce ${deviceVM.deviceId}</b><br>${deviceVM.type}`).openPopup();
        }
    },

    // Centrē karti uz ierīci (izsauc no saraksta pogas)
    flyToDevice(deviceId, event) {
        event?.stopPropagation(); // Aptur notikuma burbuļošanu, lai neizsauktu showDeviceDetails no saraksta elementa
        const deviceData = this.allMapDevices.find(d => d.deviceId === deviceId);
        if (deviceData) {
            this.showDeviceDetails(deviceData); // Tas arī centrēs karti un atjaunos UI
        } else {
            showToast(`Ierīce ar ID ${deviceId} nav atrasta sarakstā.`, 'warning');
        }
    },

    // JAUNS: Ielādē un parāda ierīces vēstures maršrutu
    async showDeviceHistoryRoute(deviceId, event) {
        event?.stopPropagation(); // Lai neizsauktu showDeviceDetails
        console.log(`[MapManager] Mēģina ielādēt vēsturi ierīcei ID: ${deviceId}`);
        toggleLoading(true);
        // JAUNS: Nolasa datuma filtru vērtības
        const startDateInput = document.getElementById('historyStartDate');
        const endDateInput = document.getElementById('historyEndDate');
        const startDate = startDateInput ? startDateInput.value : null;
        const endDate = endDateInput ? endDateInput.value : null;

        let apiUrl = `${this.API_DEVICE_HISTORY_URL}/${deviceId}/history`;
        const params = new URLSearchParams();
        if (startDate) {
            params.append('startDate', startDate);
        }
        if (endDate) {
            params.append('endDate', endDate);
        }
        if (params.toString()) {
            apiUrl += `?${params.toString()}`;
        }
        try {
            // TODO: Pievienot datuma filtru funkcionalitāti, ja nepieciešams
            // const startDate = document.getElementById('historyStartDate')?.value;
            // const endDate = document.getElementById('historyEndDate')?.value;
            // let url = `${this.API_DEVICE_HISTORY_URL}/${deviceId}/history`;
            // if (startDate && endDate) {
            //     url += `?startDate=${startDate}&endDate=${endDate}`;
            // }
            const response = await apiClient.get(`${this.API_DEVICE_HISTORY_URL}/${deviceId}/history`);
            const historyPointsDto = extractData(response); // Sagaida List<DeviceHistoryPointDto>

            if (historyPointsDto && Array.isArray(historyPointsDto) && historyPointsDto.length > 0) {
                const latLngs = historyPointsDto.map(p => L.latLng(p.latitude, p.longitude));
                this.displayHistoryRouteOnMap(latLngs, deviceId, 'green'); 
                showToast(`Ierīces ${deviceId} vēstures maršruts ielādēts.`, 'success');
            } else {
                this.hideAllHistoryRoutes(deviceId); // Paslēpj esošo vēstures maršrutu, ja jauns nav atrasts
                showToast(`Ierīcei ${deviceId} nav atrasti vēstures dati.`, 'info');
            }
        } catch (error) {
            console.error(`Kļūda ielādējot ierīces ${deviceId} vēsturi:`, error);
            showToast(`Kļūda ielādējot ierīces ${deviceId} vēsturi.`, 'danger');
        } finally {
            toggleLoading(false);
        }
    },

    // Parāda maršrutu (līniju) kartē, ja ir pieejamas koordinātes
    displayRouteOnMap(routePoints, layerId, color = 'green', layerGroup = this.routeLayers) {
        if (!this.map) return;
        this.hideSpecificLayer(layerId, layerGroup); // Paslēpj iepriekšējo slāni šim ID šajā grupā

        if (routePoints && Array.isArray(routePoints) && routePoints.length > 1) {
            try {
                const routeLayer = L.polyline(routePoints, { color: color, weight: 3, opacity: 0.7 }).addTo(this.map);
                layerGroup[layerId] = routeLayer; // Saglabā slāni
                this.map.fitBounds(routeLayer.getBounds().pad(0.1));
            } catch (e) {
                 console.error(`Kļūda attēlojot maršrutu ${layerId}:`, e, routePoints);
                 showToast(`Kļūda attēlojot maršrutu ${layerId}.`, 'danger');
            }
        } else {
            console.warn(`displayRouteOnMap saņēma nepareizus datus vai nepietiekami punktu maršrutam ${layerId}.`);
        }
    },

    // JAUNS: Specifiski vēstures maršruta attēlošanai
    displayHistoryRouteOnMap(historyLatLngs, deviceId, color = 'green') {
        this.displayRouteOnMap(historyLatLngs, deviceId, color, this.historyRouteLayers);
    },

    hideSpecificLayer(layerId, layerGroup) {
        if (layerGroup[layerId] && this.map.hasLayer(layerGroup[layerId])) {
            this.map.removeLayer(layerGroup[layerId]);
        }
        delete layerGroup[layerId];
    },

    // Paslēpj visus maršrutus kartē, izņemot norādīto (ja tāds ir)
    hideAllRoutes(exceptDeviceId = null) {
        if (!this.map) return; // Pārbauda vai karte ir inicializēta
        // Iterē cauri visiem saglabātajiem maršrutu slāņiem
        for (const devId in this.routeLayers) {
            // Ja slāņa ID nav tas, kuru jāpatur
            if (String(devId) !== String(exceptDeviceId)) {
                // Ja slānis ir kartē, noņem to
                if (this.map.hasLayer(this.routeLayers[devId])) {
                    this.map.removeLayer(this.routeLayers[devId]);
                }
                // Dzēš slāni no uzskaites
                delete this.routeLayers[devId];
            }
        }
    },
    
    // Paslēpj visus vēstures maršrutus
    hideAllHistoryRoutes(exceptLayerId = null) {
        if (!this.map) return;
        for (const layerId in this.historyRouteLayers) {
             if (String(layerId) !== String(exceptLayerId)) {
                this.hideSpecificLayer(layerId, this.historyRouteLayers);
            }
        }
    },

    // Pārbauda, vai ierīce ir bijusi aktīva nesen (piem., pēdējās 24h)
    isDeviceActive(lastUpdate) {
        if (!lastUpdate) return false; // Ja nav datuma, nav aktīva
        try {
            const updateTime = new Date(lastUpdate);
            // Izveido datumu pirms 24 stundām
            const twentyFourHoursAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);
            // Salīdzina vai atjauninājuma laiks ir jaunāks par 24h slieksni
            return updateTime > twentyFourHoursAgo;
        } catch (e) {
            console.error("Kļūda pārbaudot ierīces aktivitāti:", lastUpdate, e);
            return false; // Kļūdas gadījumā uzskata par neaktīvu
        }
    },

    // Formatē datuma/laika zīmogu lasāmā formātā (LV lokalizācija)
    formatLastUpdate(timestamp) {
        if (!timestamp) return 'Nav datu';
        try {
            // Izmanto lv-LV lokalizāciju un pielāgotu formātu
            return new Date(timestamp).toLocaleString('lv-LV', {
                year: 'numeric', month: '2-digit', day: '2-digit',
                hour: '2-digit', minute: '2-digit'//, second: '2-digit' // Var pievienot sekundes, ja nepieciešams
            });
        } catch (e) {
            console.error("Kļūda formatējot datumu:", timestamp, e);
            return 'Nederīgs datums';
        }
    },

    // Atjaunina laiku, kad pēdējo reizi tika ielādēti kartes dati
    updateLastUpdatedTime() {
        const lastUpdatedElement = document.getElementById('lastMapUpdated');
        if (lastUpdatedElement) {
            // Izmanto lv-LV lokalizāciju laika attēlošanai
            lastUpdatedElement.textContent = `Dati atjaunināti: ${new Date().toLocaleTimeString('lv-LV')}`;
        }
    }
};


// ==================== INICIALIZĀCIJA UN NOTIKUMU KLAUSĪTĀJI ====================
document.addEventListener('DOMContentLoaded', () => {
    currentUser = getUserData(); // Iegūst lietotāja datus no localStorage
    console.log("DOMContentLoaded - currentUser pēc getUserData():", currentUser);

    const currentPath = window.location.pathname.toLowerCase();
    const isAuthPage = currentPath.endsWith('login.html') || currentPath.endsWith('register.html');

    if (!isAuthenticated()) { // Ja lietotājs NAV autentificējies
        if (!isAuthPage) { // Un NAV login/register lapā
            console.log("DOMContentLoaded: Nav autentificējies un nav auth lapā. Pārvirza uz login.html");
            window.location.href = 'login.html'; // Pārvirza uz login lapu
            return; // Pārtrauc tālāku izpildi, jo notiks pārvirzīšana
        }
        // Ja ir auth lapā un nav autentificējies, vienkārši atjauno UI (slēpj nevajadzīgos elementus)
        updateUIBasedOnAuthState();
    } else { // Ja lietotājs IR autentificējies
        if (isAuthPage) { // Bet atrodas login/register lapā
            console.log("DOMContentLoaded: Ir autentificējies, bet atrodas auth lapā. Pārvirza uz Index.html");
            window.location.href = 'Index.html'; // Pārvirza uz galveno lapu
            return; // Pārtrauc tālāku izpildi
        }

        // Šajā brīdī esam Index.html un esam autentificējušies
        console.log("DOMContentLoaded: Pieteicies lietotājs:", currentUser);

        // Pārbauda vai lietotāja dati ir korekti (īpaši lomas)
        if (!currentUser || typeof currentUser !== 'object' || !Array.isArray(currentUser.roles)) {
            console.error("DOMContentLoaded: Lietotāja dati nav korekti ielādēti vai 'roles' nav derīgs masīvs. Lietotājs tiek izlogots.", currentUser);
            logout(); // Izlogo, ja dati nav kārtībā
            return;
        }

        // Atjauno UI atbilstoši lietotāja lomām
        updateUIBasedOnAuthState();
        // Nosaka un parāda sākotnējo sadaļu
        const initialSection = getDefaultSectionForUser(currentUser.roles);
        console.log(`DOMContentLoaded: Mēģina parādīt sākotnējo sadaļu: ${initialSection}`);
        showSection(initialSection);

        // Pārējā inicializācija (modālie logi, pogas, karte)
        const formModalElement = document.getElementById('formModal');
        if (formModalElement && window.bootstrap && window.bootstrap.Modal) {
            try {
                sharedFormModal = new bootstrap.Modal(formModalElement);
                // Piešķir kopīgo modālo logu visiem menedžeriem
                [CargoManager, ClientManager, RouteManager, VehicleManager, DispatcherManager, DeviceManager].forEach(manager => {
                    if (manager && typeof manager === 'object' ) {
                         manager.formModal = sharedFormModal;
                    }
                });
                // Pievieno notikumu klausītāju modālā loga aizvēršanai, lai iznīcinātu karti tajā
                if (MapManager && MapManager.MapPicker && typeof MapManager.MapPicker.destroyMap === 'function') {
                    formModalElement.addEventListener('hidden.bs.modal', () => MapManager.MapPicker.destroyMap());
                }
            } catch(e) {
                console.error("Kļūda inicializējot Bootstrap modālo logu:", e);
            }
        } else {
             console.error("Modālais logs 'formModal' nav atrasts vai Bootstrap Modal nav pieejams.");
        }

        // Pievieno notikumu klausītāju izlogošanās pogai
        const logoutButton = document.getElementById('logoutButton');
        if (logoutButton) {
            logoutButton.addEventListener('click', (e) => { e.preventDefault(); logout(); });
        }

        // Pievieno notikumu klausītāju kartes atsvaidzināšanas pogai
        const refreshMapBtn = document.getElementById('refreshMapBtn');
        if (refreshMapBtn && MapManager && typeof MapManager.loadDevices === 'function') {
            refreshMapBtn.addEventListener('click', () => MapManager.loadDevices());
        }

        // Pievieno notikumu klausītāju kartes ierīču filtram
        const filterInput = document.getElementById('mapDeviceFilter');
        if (filterInput && MapManager && typeof MapManager.filterDeviceList === 'function') {
            filterInput.addEventListener('input', () => MapManager.filterDeviceList());
        }

        // Inicializē galveno karti, ja tās elements pastāv
        if (document.getElementById('map') && MapManager && typeof MapManager.initMap === 'function') {
            MapManager.initMap();
        }
    }
});

// Pievieno menedžeru objektus un galvenās funkcijas globālajam scope,
// lai tiem varētu piekļūt no HTML (piem., onclick)
window.showSection = showSection;
window.CargoManager = CargoManager;
window.ClientManager = ClientManager;
window.RouteManager = RouteManager;
window.VehicleManager = VehicleManager;
window.DispatcherManager = DispatcherManager;
window.DeviceManager = DeviceManager;
window.MapManager = MapManager;
window.isAuthenticated = isAuthenticated;
window.toggleLoading = toggleLoading;
window.showToast = showToast;
window.API_BASE = API_BASE; // Var noderēt atkļūdošanai konsolē
window.logout = logout; // Pievienojam arī logout globāli