// API Base URL
const API_BASE = 'http://localhost:8080/api';

// Storage for user/media IDs and token
let savedData = {
    userId: '',
    lastViewedUserId: '',
    mediaId: '',
    ratingId: '',
    token: '',  // Add token storage
    username: '' // Store username for display
};

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    loadSavedData();
    updateSavedDataDisplay();
    updateAuthStatus();
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
    if (!savedData.userId && !savedData.lastViewedUserId && !savedData.mediaId && !savedData.ratingId && !savedData.token) {
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
    if (savedData.token) {
        html += `<div class="saved-item"><span>?? Auth Token:</span> <code style="color: green;">${savedData.token.substring(0, 20)}...</code></div>`;
    }
    if (savedData.username) {
        html += `<div class="saved-item"><span>?? Logged in as:</span> <code style="color: blue;">${savedData.username}</code></div>`;
    }
    display.innerHTML = html;
    
    // Update auth status
    updateAuthStatus();
}

// Update authentication status in UI
function updateAuthStatus() {
    const authStatusElement = document.getElementById('authStatus');
    if (!authStatusElement) return;
    
    if (savedData.token) {
        authStatusElement.innerHTML = `
            <div class="auth-status logged-in">
                <span>?? Logged in${savedData.username ? ' as ' + savedData.username : ''}</span>
                <button onclick="logoutUser()" class="logout-btn">Logout</button>
            </div>
        `;
    } else {
        authStatusElement.innerHTML = `
            <div class="auth-status logged-out">
                <span>? Not logged in</span>
            </div>
        `;
    }
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
async function apiCall(endpoint, method = 'GET', body = null, requiresAuth = false) {
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json'
        }
    };
    
    // Add Authorization header if token exists and auth is required
    if (requiresAuth && savedData.token) {
        options.headers['Authorization'] = `Bearer ${savedData.token}`;
    }
    
    if (body) {
        options.body = JSON.stringify(body);
    }
    
    try {
        console.log(`API Call: ${method} ${API_BASE}${endpoint}`);
        if (body) console.log('Request body:', body);
        
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        let data;
        
        try {
            // Only try to parse JSON if there's content
            const contentType = response.headers.get("content-type");
            if (contentType && contentType.includes("application/json") && response.status !== 204) {
                try {
                    data = await response.json();
                } catch (e) {
                    console.error("Error parsing JSON response:", e);
                    data = { error: "Could not parse server response" };
                }
            } else if (response.status === 204) {
                // No content
                data = { message: "Success - No Content" };
            } else {
                // Handle text response or other content types
                const text = await response.text();
                if (text && text.trim()) {
                    try {
                        data = JSON.parse(text);
                    } catch (e) {
                        data = { message: text || "No response body" };
                    }
                } else {
                    data = { message: "No response body" };
                }
            }
        } catch (parseError) {
            console.error("Error handling response:", parseError);
            data = { error: "Failed to process response" };
        }
        
        console.log(`Response: ${response.status}`, data);
        return { 
            status: response.status, 
            data, 
            ok: response.ok,
            headers: Object.fromEntries(response.headers.entries())
        };
    } catch (error) {
        console.error("API call error:", error);
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
    
    // Save user ID and token after successful login
    if (result.ok && result.data.token) {
        savedData.token = result.data.token;
        savedData.username = username;
        
        // Extract user ID from response if available, otherwise try to get from username
        const user = await apiCall(`/users/profile?userid=${savedData.userId}`, 'GET', null, true);
        if (user.ok && user.data.user) {
            savedData.userId = user.data.user;
            savedData.lastViewedUserId = user.data.user;
        }
        saveData();
        updateAuthStatus();
        alert('Login successful! Token saved for authenticated requests.');
    }
}

// Logout function
function logoutUser() {
    if (confirm('Are you sure you want to log out?')) {
        // Clear authentication data
        savedData.token = '';
        savedData.username = '';
        saveData();
        updateAuthStatus();
        alert('Logged out successfully');
    }
}

async function getProfile() {
    let userId = document.getElementById('profile-userid').value || savedData.userId;
    
    if (!userId) {
        alert('Please enter a User ID or register a user first');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to view profiles');
        return;
    }
    
    const result = await apiCall(`/users/profile?userid=${userId}`, 'GET', null, true);
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
    
    if (!savedData.token) {
        alert('Please login first to update profiles');
        return;
    }
    
    // API expects 'user' field, not 'userid'
    const result = await apiCall('/users/profile', 'PUT', {
        user: userId,
        sobriquet,
        aboutMe
    }, true);
    
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
    
    if (!savedData.token) {
        alert('Please login first to create media entries');
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
    }, true);
    
    displayResponse('media-create-response', result.status, result.data, result.ok);
    
    if (result.ok && result.data.uuid) {
        savedData.mediaId = result.data.uuid;
        saveData();
    }
}

// Function to create 5 test media entries
async function createTestMediaEntries() {
    if (!savedData.token) {
        alert('Please login first to create test media entries');
        return;
    }

    if (!savedData.userId) {
        alert('User ID not found. Please register or login first');
        return;
    }

    const testMedia = [
        {
            title: "Test Movie 1",
            description: "A test action movie with explosions and thrills",
            mediaType: "Movie",
            releaseYear: 2022,
            ageRestriction: "FSK16",
            genre: "Action"
        },
        {
            title: "Test Series 1",
            description: "A comedy series about a group of friends",
            mediaType: "Series",
            releaseYear: 2023,
            ageRestriction: "FSK12",
            genre: "Comedy"
        },
        {
            title: "Test Documentary",
            description: "Educational documentary about space exploration",
            mediaType: "Documentary",
            releaseYear: 2021,
            ageRestriction: "FSK0",
            genre: "Science"
        },
        {
            title: "Test Game",
            description: "An adventure game in a fantasy world",
            mediaType: "Game",
            releaseYear: 2024,
            ageRestriction: "FSK12",
            genre: "Adventure"
        },
        {
            title: "Test Horror Movie",
            description: "A scary movie with ghosts and suspense",
            mediaType: "Movie",
            releaseYear: 2023,
            ageRestriction: "FSK18",
            genre: "Horror"
        }
    ];

    const responseElement = document.getElementById('media-create-response');
    responseElement.classList.remove('hidden');
    responseElement.innerHTML = '<p>Creating 5 test media entries...</p>';

    const createdIds = [];
    let successCount = 0;
    let errorMessages = '';
    
    for (let i = 0; i < testMedia.length; i++) {
        const media = testMedia[i];
        try {
            console.log(`Creating test media ${i+1}/5:`, {
                ...media,
                createdBy: savedData.userId
            });
            
            const result = await apiCall('/media', 'POST', {
                ...media,
                createdBy: savedData.userId
            }, true);
            
            console.log(`Response for media ${i+1}:`, result);
            
            if (result.ok && result.data.uuid) {
                createdIds.push(result.data.uuid);
                successCount++;
                // Save the last one as current media ID
                if (i === testMedia.length - 1) {
                    savedData.mediaId = result.data.uuid;
                    saveData();
                }
            } else {
                errorMessages += `<p>Error creating media entry ${i+1}: ${result.data.error || 'Unknown error'}</p>`;
            }
            
            // Update progress in the UI
            responseElement.innerHTML = `<p>Creating test media entries: ${i + 1}/${testMedia.length} completed</p>`;
            
        } catch (error) {
            console.error(`Error creating media ${i+1}:`, error);
            errorMessages += `<p>Error creating media entry ${i+1}: ${error.message}</p>`;
        }
        
        // Add a small delay between requests to avoid overwhelming the server
        await new Promise(resolve => setTimeout(resolve, 500));
    }

    if (successCount === testMedia.length) {
        responseElement.innerHTML = `
            <div class="status-badge status-success">? Success</div>
            <h3>Created 5 Test Media Entries:</h3>
            <div class="response-content">Created ${createdIds.length} media entries successfully!
IDs: ${JSON.stringify(createdIds, null, 2)}</div>
        `;
    } else {
        responseElement.innerHTML = `
            <div class="status-badge ${successCount > 0 ? 'status-success' : 'status-error'}">
                ${successCount > 0 ? '? Partial Success' : '? Error'}
            </div>
            <h3>Created ${successCount} out of 5 test media entries</h3>
            <div class="response-content">
                ${successCount > 0 ? `Successfully created IDs: ${JSON.stringify(createdIds, null, 2)}<br><br>` : ''}
                ${errorMessages}
            </div>
        `;
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
    
    if (!savedData.token) {
        alert('Please login first to update media entries');
        return;
    }
    
    const body = { uuid };
    if (title) body.title = title;
    if (description) body.description = description;
    if (genre) body.genre = genre;
    
    const result = await apiCall('/media', 'PUT', body, true);
    displayResponse('media-update-response', result.status, result.data, result.ok);
}

async function deleteMedia() {
    const id = document.getElementById('delete-media-id').value;
    
    if (!id) {
        alert('Please enter Media ID');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to delete media entries');
        return;
    }
    
    if (!confirm('Are you sure you want to delete this media entry?')) {
        return;
    }
    
    // Updated endpoint to match Postman collection
    const result = await apiCall(`/media/${id}`, 'DELETE', null, true);
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
    
    if (!savedData.token) {
        alert('Please login first to create ratings');
        return;
    }
    
    // Check if we should use the shorthand endpoint (when the user is rating their own entry)
    if (user === savedData.userId) {
        console.log("Using shorthand endpoint for rating own media");
        // Use shorthand endpoint
        try {
            const result = await apiCall(`/media/${mediaEntry}/rate`, 'POST', {
                stars,
                comment,
                publicVisible
            }, true);
            
            displayResponse('rating-create-response', result.status, result.data, result.ok);
            
            if (result.ok && result.data.uuid) {
                savedData.ratingId = result.data.uuid;
                saveData();
                alert('Rating created successfully using shorthand endpoint!');
            }
        } catch (error) {
            console.error("Error using shorthand endpoint:", error);
            displayResponse('rating-create-response', 500, { error: `Error: ${error.message}` }, false);
        }
    } else {
        console.log("Using standard endpoint for rating");
        // Use standard endpoint
        try {
            const result = await apiCall('/ratings', 'POST', {
                mediaEntry,
                user,
                stars,
                comment,
                publicVisible
            }, true);
            
            displayResponse('rating-create-response', result.status, result.data, result.ok);
            
            if (result.ok && result.data.uuid) {
                savedData.ratingId = result.data.uuid;
                saveData();
            }
        } catch (error) {
            console.error("Error using standard endpoint:", error);
            displayResponse('rating-create-response', 500, { error: `Error: ${error.message}` }, false);
        }
    }
}

async function listRatings() {
    const creator = document.getElementById('rating-list-creator').value;
    const media = document.getElementById('rating-list-media').value;
    const ratingId = document.getElementById('rating-get-id').value;
    
    let endpoint = '/ratings';
    
    // If a specific rating ID is provided, get that single rating
    if (ratingId) {
        endpoint = `/ratings/${ratingId}`;
    } else if (creator) {
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
    const publicVisible = document.getElementById('update-rating-public').checked;
    
    if (!uuid) {
        alert('Please enter Rating ID');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to update ratings');
        return;
    }
    
    const body = { uuid };
    if (!isNaN(stars) && stars >= 1 && stars <= 5) body.stars = stars;
    if (comment !== undefined) body.comment = comment;
    if (publicVisible !== undefined) body.publicVisible = publicVisible;
    
    console.log(`Attempting to update rating: ${uuid}`, body);
    
    try {
        // Updated endpoint to match Postman collection
        const result = await apiCall(`/ratings/${uuid}`, 'PUT', body, true);
        displayResponse('rating-update-response', result.status, result.data, result.ok);
        
        if (result.ok) {
            alert('Rating successfully updated!');
        } else {
            alert(`Failed to update rating: ${result.data.error || 'Unknown error'}`);
        }
    } catch (error) {
        console.error("Error updating rating:", error);
        displayResponse('rating-update-response', 500, { error: `Error: ${error.message}` }, false);
    }
}

async function deleteRating() {
    const id = document.getElementById('delete-rating-id').value;
    
    if (!id) {
        alert('Please enter Rating ID');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to delete ratings');
        return;
    }
    
    if (!confirm('Are you sure you want to delete this rating?')) {
        return;
    }
    
    console.log(`Attempting to delete rating: ${id}`);
    
    try {
        // Updated endpoint to match Postman collection
        const result = await apiCall(`/ratings/${id}`, 'DELETE', null, true);
        displayResponse('rating-delete-response', result.status, result.data, result.ok);
        
        if (result.ok) {
            alert('Rating successfully deleted!');
            // Clear the rating ID from saved data if it was the one we just deleted
            if (savedData.ratingId === id) {
                savedData.ratingId = '';
                saveData();
            }
        } else {
            alert(`Failed to delete rating: ${result.data.error || 'Unknown error'}`);
        }
    } catch (error) {
        console.error("Error deleting rating:", error);
        displayResponse('rating-delete-response', 500, { error: `Error: ${error.message}` }, false);
    }
}

async function approveRating() {
    const ratingId = document.getElementById('approve-rating-id').value || savedData.ratingId;
    const approverId = document.getElementById('approve-approver-id').value || savedData.userId;
    
    if (!ratingId) {
        alert('Please enter a Rating ID');
        return;
    }
    
    if (!approverId) {
        alert('Please enter an Approver User ID (media entry owner)');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to approve ratings');
        return;
    }
    
    console.log(`Attempting to approve rating: ${ratingId} by approver: ${approverId}`);
    try {
        // The backend expects the media owner to be the one approving
        // Let's try both endpoints to see which one works
        let result;
        
        try {
            // First try the path parameter version
            result = await apiCall(`/ratings/${ratingId}/approve`, 'PATCH', null, true);
        } catch (error) {
            console.log("First approval attempt failed, trying with query parameters");
            // If that fails, try the query parameter version
            result = await apiCall(`/ratings/approve?id=${ratingId}&approverId=${approverId}`, 'PATCH', null, true);
        }
        
        displayResponse('rating-approve-response', result.status, result.data, result.ok);
        
        if (result.ok) {
            alert('Rating successfully approved and made publicly visible!');
        } else {
            alert(`Failed to approve rating: ${result.data.error || 'Unknown error'}`);
        }
    } catch (error) {
        console.error("Error approving rating:", error);
        displayResponse('rating-approve-response', 500, { error: `Error: ${error.message}` }, false);
    }
}

async function likeRating() {
    const ratingId = document.getElementById('like-rating-id').value || savedData.ratingId;
    
    if (!ratingId) {
        alert('Please enter a Rating ID');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to like ratings');
        return;
    }
    
    console.log(`Attempting to like rating: ${ratingId}`);
    try {
        // Updated endpoint to match Postman collection
        const result = await apiCall(`/ratings/${ratingId}/like`, 'POST', null, true);
        displayResponse('rating-like-response', result.status, result.data, result.ok);
        
        if (result.ok) {
            alert('Rating liked successfully! ??');
        } else {
            alert(`Failed to like rating: ${result.data.error || 'Unknown error'}`);
        }
    } catch (error) {
        console.error("Error liking rating:", error);
        displayResponse('rating-like-response', 500, { error: `Error: ${error.message}` }, false);
    }
}

async function unlikeRating() {
    const ratingId = document.getElementById('like-rating-id').value || savedData.ratingId;
    
    if (!ratingId) {
        alert('Please enter a Rating ID');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to unlike ratings');
        return;
    }
    
    console.log(`Attempting to unlike rating: ${ratingId}`);
    try {
        // Updated endpoint to match Postman collection
        const result = await apiCall(`/ratings/${ratingId}/like`, 'DELETE', null, true);
        displayResponse('rating-like-response', result.status, result.data, result.ok);
        
        if (result.ok) {
            alert('Like removed successfully! ??');
        } else {
            alert(`Failed to unlike rating: ${result.data.error || 'Unknown error'}`);
        }
    } catch (error) {
        console.error("Error unliking rating:", error);
        displayResponse('rating-like-response', 500, { error: `Error: ${error.message}` }, false);
    }
}


