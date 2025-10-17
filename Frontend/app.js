// API Base URL
const API_BASE = 'http://localhost:8081/api';

// Storage for user/media IDs
let savedData = {
    userId: '',
    lastViewedUserId: '',
    mediaId: '',
    ratingId: ''
};

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    loadSavedData();
    updateSavedDataDisplay();
});

// Save data to localStorage
function saveData() {
    localStorage.setItem('mrp-saved-data', JSON.stringify(savedData));
    updateSavedDataDisplay();
}

// Load data from localStorage
function loadSavedData() {
    const stored = localStorage.getItem('mrp-saved-data');
    if (stored) {
        savedData = JSON.parse(stored);
    }
}

// Update saved data display
function updateSavedDataDisplay() {
    const display = document.getElementById('savedDataDisplay');
    if (!savedData.userId && !savedData.lastViewedUserId && !savedData.mediaId && !savedData.ratingId) {
        display.innerHTML = '<p style="color: #856404;">No saved data yet. Create a user or media entry first!</p>';
        return;
    }
    
    let html = '';
    if (savedData.userId) {
        html += `<div class="saved-item"><span>?? Registered User ID:</span> <code>${savedData.userId}</code></div>`;
    }
    if (savedData.lastViewedUserId && savedData.lastViewedUserId !== savedData.userId) {
        html += `<div class="saved-item"><span>??? Last Viewed User ID:</span> <code>${savedData.lastViewedUserId}</code></div>`;
    }
    if (savedData.mediaId) {
        html += `<div class="saved-item"><span>?? Last Media ID:</span> <code>${savedData.mediaId}</code></div>`;
    }
    if (savedData.ratingId) {
        html += `<div class="saved-item"><span>? Last Rating ID:</span> <code>${savedData.ratingId}</code></div>`;
    }
    display.innerHTML = html;
}

// Show section
function showSection(sectionId) {
    // Hide all sections
    document.querySelectorAll('.section').forEach(section => {
        section.classList.remove('active');
    });
    
    // Remove active class from all buttons
    document.querySelectorAll('.nav-button').forEach(button => {
        button.classList.remove('active');
    });
    
    // Show selected section
    document.getElementById(sectionId).classList.add('active');
    
    // Add active class to clicked button
    event.target.classList.add('active');
}

// Display response
function displayResponse(elementId, status, data, isSuccess = true) {
    const element = document.getElementById(elementId);
    element.classList.remove('hidden');
    
    const statusClass = isSuccess ? 'status-success' : 'status-error';
    const statusText = isSuccess ? '? Success' : '? Error';
    
    element.innerHTML = `
        <div class="status-badge ${statusClass}">${statusText} - Status: ${status}</div>
        <h3>Response:</h3>
        <div class="response-content">${JSON.stringify(data, null, 2)}</div>
    `;
}

// API Call Helper
async function apiCall(endpoint, method = 'GET', body = null) {
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json'
        }
    };
    
    if (body) {
        options.body = JSON.stringify(body);
    }
    
    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        const data = await response.json();
        return { status: response.status, data, ok: response.ok };
    } catch (error) {
        return { status: 0, data: { error: error.message }, ok: false };
    }
}

// ============= USER FUNCTIONS =============

async function registerUser() {
    const username = document.getElementById('reg-username').value;
    const password = document.getElementById('reg-password').value;
    
    if (!username || !password) {
        alert('Please fill in all fields');
        return;
    }
    
    const result = await apiCall('/users/register', 'POST', { username, password });
    displayResponse('register-response', result.status, result.data, result.ok);
    
    if (result.ok && result.data.uuid) {
        savedData.userId = result.data.uuid;
        saveData();
    }
}

async function loginUser() {
    const username = document.getElementById('login-username').value;
    const password = document.getElementById('login-password').value;
    
    if (!username || !password) {
        alert('Please fill in all fields');
        return;
    }
    
    const result = await apiCall('/users/login', 'POST', { username, password });
    displayResponse('login-response', result.status, result.data, result.ok);
    
    // Save user ID after successful login
    if (result.ok && result.data.uuid) {
        savedData.userId = result.data.uuid;
        savedData.lastViewedUserId = result.data.uuid;
        saveData();
    }
}

async function getProfile() {
    let userId = document.getElementById('profile-userid').value || savedData.userId;
    
    if (!userId) {
        alert('Please enter a User ID or register a user first');
        return;
    }
    
    const result = await apiCall(`/users/profile?userid=${userId}`, 'GET');
    displayResponse('profile-response', result.status, result.data, result.ok);
    
    // Save the last viewed user ID
    if (result.ok && result.data.user) {
        savedData.lastViewedUserId = result.data.user;
        saveData();
    }
}

function showUpdateProfile() {
    document.getElementById('update-profile-form').classList.toggle('hidden');
}

function useSavedUserId() {
    const userId = savedData.lastViewedUserId || savedData.userId;
    if (userId) {
        document.getElementById('profile-userid').value = userId;
    } else {
        alert('No saved User ID found. Please register a user first.');
    }
}

async function updateProfile() {
    let userId = document.getElementById('profile-userid').value || savedData.lastViewedUserId || savedData.userId;
    const sobriquet = document.getElementById('profile-sobriquet').value;
    const aboutMe = document.getElementById('profile-aboutme').value;
    
    if (!userId) {
        alert('Please enter a User ID or load a profile first');
        return;
    }
    
    // API expects 'user' field, not 'userid'
    const result = await apiCall('/users/profile', 'PUT', {
        user: userId,  // Changed from 'userid' to 'user'
        sobriquet,
        aboutMe
    });
    
    displayResponse('profile-response', result.status, result.data, result.ok);
    
    // Reload profile after update
    if (result.ok) {
        setTimeout(() => getProfile(), 500);
    }
}

// ============= MEDIA FUNCTIONS =============

async function createMedia() {
    const title = document.getElementById('media-title').value;
    const description = document.getElementById('media-description').value;
    const mediaType = document.getElementById('media-type').value;
    const releaseYear = parseInt(document.getElementById('media-year').value);
    const ageRestriction = document.getElementById('media-fsk').value;
    const genre = document.getElementById('media-genre').value;
    let createdBy = document.getElementById('media-creator').value || savedData.userId;
    
    if (!title || !createdBy) {
        alert('Please fill in at least Title and Creator ID (or register a user first)');
        return;
    }
    
    const result = await apiCall('/media', 'POST', {
        title,
        description,
        mediaType,
        releaseYear,
        ageRestriction,
        genre,
        createdBy
    });
    
    displayResponse('media-create-response', result.status, result.data, result.ok);
    
    if (result.ok && result.data.uuid) {
        savedData.mediaId = result.data.uuid;
        saveData();
    }
}

async function listMedia() {
    const result = await apiCall('/media', 'GET');
    displayResponse('media-list-response', result.status, result.data, result.ok);
}

async function filterMedia() {
    const params = new URLSearchParams();
    
    const title = document.getElementById('filter-title').value;
    const genre = document.getElementById('filter-genre').value;
    const mediaType = document.getElementById('filter-mediatype').value;
    const fsk = document.getElementById('filter-fsk').value;
    const minYear = document.getElementById('filter-minyear').value;
    const maxYear = document.getElementById('filter-maxyear').value;
    const sortBy = document.getElementById('filter-sortby').value;
    const sortOrder = document.getElementById('filter-sortorder').value;
    
    if (title) params.append('titleContains', title);
    if (genre) params.append('genre', genre);
    if (mediaType) params.append('mediaType', mediaType);
    if (fsk) params.append('ageRestriction', fsk);
    if (minYear) params.append('minYear', minYear);
    if (maxYear) params.append('maxYear', maxYear);
    if (sortBy) {
        params.append('sortBy', sortBy);
        params.append('sortOrder', sortOrder);
    }
    
    const queryString = params.toString();
    const endpoint = queryString ? `/media?${queryString}` : '/media';
    
    const result = await apiCall(endpoint, 'GET');
    displayResponse('media-filter-response', result.status, result.data, result.ok);
}

function clearFilters() {
    document.getElementById('filter-title').value = '';
    document.getElementById('filter-genre').value = '';
    document.getElementById('filter-mediatype').value = '';
    document.getElementById('filter-fsk').value = '';
    document.getElementById('filter-minyear').value = '';
    document.getElementById('filter-maxyear').value = '';
    document.getElementById('filter-sortby').value = '';
    document.getElementById('filter-sortorder').value = 'asc';
}

async function updateMedia() {
    const uuid = document.getElementById('update-media-id').value;
    const title = document.getElementById('update-media-title').value;
    const description = document.getElementById('update-media-description').value;
    const genre = document.getElementById('update-media-genre').value;
    
    if (!uuid) {
        alert('Please enter Media ID');
        return;
    }
    
    const body = { uuid };
    if (title) body.title = title;
    if (description) body.description = description;
    if (genre) body.genre = genre;
    
    const result = await apiCall('/media', 'PUT', body);
    displayResponse('media-update-response', result.status, result.data, result.ok);
}

async function deleteMedia() {
    const id = document.getElementById('delete-media-id').value;
    
    if (!id) {
        alert('Please enter Media ID');
        return;
    }
    
    if (!confirm('Are you sure you want to delete this media entry?')) {
        return;
    }
    
    const result = await apiCall(`/media?id=${id}`, 'DELETE');
    displayResponse('media-delete-response', result.status, result.data, result.ok);
}

// ============= RATING FUNCTIONS =============

async function createRating() {
    const mediaEntry = document.getElementById('rating-media-id').value;
    let user = document.getElementById('rating-user-id').value || savedData.userId;
    const stars = parseInt(document.getElementById('rating-stars').value);
    const comment = document.getElementById('rating-comment').value;
    const publicVisible = document.getElementById('rating-public').checked;
    
    if (!mediaEntry || !user || !stars) {
        alert('Please fill in Media ID, User ID, and Stars');
        return;
    }
    
    if (stars < 1 || stars > 5) {
        alert('Stars must be between 1 and 5');
        return;
    }
    
    const result = await apiCall('/ratings', 'POST', {
        mediaEntry,
        user,
        stars,
        comment,
        publicVisible
    });
    
    displayResponse('rating-create-response', result.status, result.data, result.ok);
    
    if (result.ok && result.data.uuid) {
        savedData.ratingId = result.data.uuid;
        saveData();
    }
}

async function listRatings() {
    const creator = document.getElementById('rating-list-creator').value;
    const media = document.getElementById('rating-list-media').value;
    
    let endpoint = '/ratings';
    if (creator) {
        endpoint += `?creator=${creator}`;
    } else if (media) {
        endpoint += `?media=${media}`;
    }
    
    const result = await apiCall(endpoint, 'GET');
    displayResponse('rating-list-response', result.status, result.data, result.ok);
}

async function updateRating() {
    const uuid = document.getElementById('update-rating-id').value;
    const stars = parseInt(document.getElementById('update-rating-stars').value);
    const comment = document.getElementById('update-rating-comment').value;
    
    if (!uuid) {
        alert('Please enter Rating ID');
        return;
    }
    
    const body = { uuid };
    if (stars) body.stars = stars;
    if (comment) body.comment = comment;
    
    const result = await apiCall('/ratings', 'PUT', body);
    displayResponse('rating-update-response', result.status, result.data, result.ok);
}

async function deleteRating() {
    const id = document.getElementById('delete-rating-id').value;
    
    if (!id) {
        alert('Please enter Rating ID');
        return;
    }
    
    if (!confirm('Are you sure you want to delete this rating?')) {
        return;
    }
    
    const result = await apiCall(`/ratings?id=${id}`, 'DELETE');
    displayResponse('rating-delete-response', result.status, result.data, result.ok);
}

async function approveRating() {
    const ratingId = document.getElementById('approve-rating-id').value || savedData.ratingId;
    let approverId = document.getElementById('approve-approver-id').value || savedData.userId;
    
    if (!ratingId) {
        alert('Please enter a Rating ID');
        return;
    }
    
    if (!approverId) {
        alert('Please enter an Approver User ID (media entry owner)');
        return;
    }
    
    const result = await apiCall(`/ratings/approve?id=${ratingId}&approverId=${approverId}`, 'PATCH');
    displayResponse('rating-approve-response', result.status, result.data, result.ok);
    
    if (result.ok) {
        // Optionally reload the rating details or show success message
        alert('Rating successfully approved and made publicly visible!');
    }
}

