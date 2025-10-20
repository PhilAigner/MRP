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
        html += `<div class="saved-item"><span>Registered User ID:</span> <code>${savedData.userId}</code></div>`;
    }
    if (savedData.lastViewedUserId && savedData.lastViewedUserId !== savedData.userId) {
        html += `<div class="saved-item"><span>Last Viewed User ID:</span> <code>${savedData.lastViewedUserId}</code></div>`;
    }
    if (savedData.mediaId) {
        html += `<div class="saved-item"><span>Last Media ID:</span> <code>${savedData.mediaId}</code></div>`;
    }
    if (savedData.ratingId) {
        html += `<div class="saved-item"><span>Last Rating ID:</span> <code>${savedData.ratingId}</code></div>`;
    }
    if (savedData.token) {
        html += `<div class="saved-item"><span>Auth Token:</span> <code style="color: green;">${savedData.token.substring(0, 20)}...</code></div>`;
    }
    if (savedData.username) {
        html += `<div class="saved-item"><span>Logged in as:</span> <code style="color: blue;">${savedData.username}</code></div>`;
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
        let data = {};
        
        try {
            // Only try to parse JSON if there's content
            const contentType = response.headers.get("content-type");
            
            if (response.status === 204) {
                // No content
                data = { message: "Success - No Content" };
            } 
            else if (contentType && contentType.includes("application/json")) {
                const text = await response.text();
                if (text && text.trim()) {
                    try {
                        data = JSON.parse(text);
                    } catch (e) {
                        console.error("JSON parse error:", e);
                        data = { error: "Invalid JSON response", rawText: text };
                    }
                } else {
                    data = { message: "Empty JSON response" };
                }
            } 
            else {
                // Non-JSON response
                const text = await response.text();
                data = { message: text || "No response body" };
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
    
    if (result.ok && result.data.user.uuid) {
        savedData.userId = result.data.user.uuid;
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
    
    try {
        console.log('Attempting to login with username:', username);
        const result = await apiCall('/users/login', 'POST', { username, password });
        displayResponse('login-response', result.status, result.data, result.ok);
        
        // Save user ID and token after successful login
        if (result.ok && result.data.token) {
            console.log('Login successful, token received');
            savedData.token = result.data.token;
            savedData.username = username;
            
            // Check if the user ID is directly available in the login response
            if (result.data.uuid) {
                console.log('User ID found in login response:', result.data.uuid);
                savedData.userId = result.data.uuid;
                savedData.lastViewedUserId = result.data.uuid;
                saveData();
                updateAuthStatus();
                alert('Login successful! Token and user ID saved.');
                return;
            }
            
            // If not available in login response, try to get user ID from the profile endpoint
            console.log('Fetching user profile to get user ID');
            
            // First try with username since we don't have userId yet
            const userByName = await apiCall(`/users?username=${encodeURIComponent(username)}`, 'GET', null, true);
            console.log('User lookup by username response:', userByName);
            
            if (userByName.ok && userByName.data && Array.isArray(userByName.data) && userByName.data.length > 0) {
                const userId = userByName.data[0].uuid;
                console.log('Found user ID from username lookup:', userId);
                savedData.userId = userId;
                savedData.lastViewedUserId = userId;
                saveData();
                updateAuthStatus();
                alert('Login successful! Token and user ID saved.');
                return;
            }
            
            // If that fails too, make a direct profile call
            const profile = await apiCall('/users/profile', 'GET', null, true);
            console.log('Profile response:', profile);
            
            if (profile.ok && profile.data && profile.data.user) {
                console.log('User ID found in profile:', profile.data.user);
                savedData.userId = profile.data.user;
                savedData.lastViewedUserId = profile.data.user;
            }
            
            saveData();
            updateAuthStatus();
            alert('Login successful! Token saved for authenticated requests.');
        }
    } catch (error) {
        console.error('Error during login:', error);
        displayResponse('login-response', 500, { error: error.message }, false);
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
    
    // Use the correct endpoint format: /api/users/{userId}/profile
    const result = await apiCall(`/users/${userId}/profile`, 'GET', null, true);
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
    
    // API expects userId in the path: /api/users/{userId}/profile
    const result = await apiCall(`/users/${userId}/profile`, 'PUT', {
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
    
    try {
        console.log('Creating media entry with data:', {
            title, description, mediaType, releaseYear, ageRestriction, genre, createdBy
        });
        
        const result = await apiCall('/media', 'POST', {
            title,
            description,
            mediaType,
            releaseYear,
            ageRestriction,
            genre,
            createdBy
        }, true);
        
        console.log('Create media response:', result);
        displayResponse('media-create-response', result.status, result.data, result.ok);
        
        if (result.ok && result.data && result.data.uuid) {
            savedData.mediaId = result.data.uuid;
            saveData();
            console.log('Saved new media ID:', savedData.mediaId);
        }
    } catch (error) {
        console.error('Error creating media:', error);
        displayResponse('media-create-response', 500, { error: error.message }, false);
    }
}

// Function to create 5 test media entries
async function createTestMediaEntries() {
    try {
        // Check if user is logged in
        if (!savedData.token) {
            alert('Please login first to create test media entries');
            return;
        }

        // Get value from the Creator input field or use saved ID
        let creatorId = document.getElementById('media-creator').value || savedData.userId;
        
        // Show immediate feedback
        const responseElement = document.getElementById('media-create-response');
        responseElement.classList.remove('hidden');
        
        // If no creator ID provided, try to get the user ID
        if (!creatorId) {
            responseElement.innerHTML = `
                <div class="status-badge status-success">⏳ Working...</div>
                <h3>Getting user information...</h3>
                <div class="response-content">Retrieving user ID before creating entries...</div>
            `;
            
            console.log("No creator ID found. Trying to get user profile...");
            
            try {
                // Try to get user profile
                const profileResponse = await fetch(`${API_BASE}/users/profile`, {
                    method: 'GET',
                    headers: {
                        'Authorization': `Bearer ${savedData.token}`
                    }
                });
                
                const profileData = await profileResponse.json();
                console.log("Profile response:", profileData);
                
                if (profileResponse.ok && profileData.user) {
                    creatorId = profileData.user;
                    savedData.userId = creatorId; // Also save it for future use
                    saveData();
                    console.log("Retrieved user ID from profile:", creatorId);
                } else if (savedData.username) {
                    // Try to look up by username if we have one
                    const userResponse = await fetch(`${API_BASE}/users?username=${encodeURIComponent(savedData.username)}`, {
                        headers: {
                            'Authorization': `Bearer ${savedData.token}`
                        }
                    });
                    
                    const userData = await userResponse.json();
                    console.log("User lookup response:", userData);
                    
                    if (userResponse.ok && Array.isArray(userData) && userData.length > 0) {
                        creatorId = userData[0].uuid;
                        savedData.userId = creatorId; // Also save it
                        saveData();
                        console.log("Retrieved user ID from username lookup:", creatorId);
                    }
                }
            } catch (profileError) {
                console.error("Error getting user profile:", profileError);
            }
            
            // If we still don't have a creator ID, show an error
            if (!creatorId) {
                responseElement.innerHTML = `
                    <div class="status-badge status-error">❌ Error</div>
                    <h3>Failed to create test media entries</h3>
                    <div class="response-content">Error: Could not determine user ID. Please enter a Creator ID manually or log in again.</div>
                `;
                alert("Could not determine user ID. Please enter a Creator ID in the form or log in again.");
                return;
            }
        }

        // Now we should have a creator ID, so proceed with creating test entries
        responseElement.innerHTML = `
            <div class="status-badge status-success">⏳ Working...</div>
            <h3>Creating test media entries</h3>
            <div class="response-content">Using creator ID: ${creatorId}</div>
        `;

        // Create simple test data
        const now = Date.now();
        const testEntries = [
            { title: `Action Movie ${now}-1`, description: "Test action movie", mediaType: "Movie", releaseYear: 2022, ageRestriction: "FSK16", genre: "Action", createdBy: creatorId },
            { title: `Comedy ${now}-2`, description: "Test comedy", mediaType: "Movie", releaseYear: 2023, ageRestriction: "FSK12", genre: "Comedy", createdBy: creatorId },
            { title: `Documentary ${now}-3`, description: "Test documentary", mediaType: "Documentary", releaseYear: 2021, ageRestriction: "FSK0", genre: "Science", createdBy: creatorId },
            { title: `Game ${now}-4`, description: "Test game", mediaType: "Game", releaseYear: 2024, ageRestriction: "FSK12", genre: "Fantasy", createdBy: creatorId },
            { title: `Horror ${now}-5`, description: "Test horror", mediaType: "Movie", releaseYear: 2023, ageRestriction: "FSK18", genre: "Horror", createdBy: creatorId }
        ];

        console.log("Creating test entries with creator ID:", creatorId);
        
        // Create each media entry sequentially
        const results = [];
        for (let i = 0; i < testEntries.length; i++) {
            const entry = testEntries[i];
            
            // Update progress display
            responseElement.innerHTML = `
                <div class="status-badge status-success">⏳ Working...</div>
                <h3>Creating test entry ${i+1} of 5</h3>
                <div class="response-content">Creating: "${entry.title}"</div>
            `;
            
            console.log(`Creating entry ${i+1}:`, entry);
            
            // Make API call
            try {
                const response = await fetch(`${API_BASE}/media`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${savedData.token}`
                    },
                    body: JSON.stringify(entry)
                });
                
                // Process response
                let data;
                const text = await response.text();
                try {
                    if (text && text.trim()) {
                        data = JSON.parse(text);
                    } else {
                        data = { message: "Empty response" };
                    }
                } catch (e) {
                    console.error("Error parsing response:", e);
                    data = { error: "Could not parse response: " + text.substring(0, 100) };
                }
                
                console.log(`Media ${i+1} response:`, { status: response.status, data });
                
                results.push({
                    success: response.ok,
                    data: data,
                    title: entry.title,
                    status: response.status
                });
            } catch (error) {
                console.error(`Error with entry ${i+1}:`, error);
                results.push({
                    success: false,
                    data: { error: error.message },
                    title: entry.title,
                    status: 0
                });
            }
            
            // Small delay between requests
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        
        // Count successes and collect IDs
        const successes = results.filter(r => r.success);
        const ids = successes.map(r => r.data && r.data.uuid).filter(id => id);
        
        console.log("Creation results:", {
            total: results.length,
            successes: successes.length,
            ids: ids
        });
        
        // Update the UI based on results
        if (successes.length === testEntries.length) {
            responseElement.innerHTML = `
                <div class="status-badge status-success">✅ Success</div>
                <h3>Created 5 Test Media Entries</h3>
                <div class="response-content">All entries created successfully!</div>
            `;
        } else {
            const errorList = results.filter(r => !r.success)
                .map(r => `<li>${r.title}: ${r.status} - ${r.data && r.data.error ? r.data.error : 'Unknown error'}</li>`)
                .join('');
                
            responseElement.innerHTML = `
                <div class="status-badge status-error">⚠️ Partial Success</div>
                <h3>Created ${successes.length} of 5 entries</h3>
                <div class="response-content">
                    <p>Errors:</p>
                    <ul>${errorList}</ul>
                </div>
            `;
        }
        
        // Save the last successful ID if any
        if (ids.length > 0) {
            savedData.mediaId = ids[ids.length - 1];
            saveData();
        }
        
    } catch (error) {
        console.error("Failed to create test media entries:", error);
        const responseElement = document.getElementById('media-create-response');
        responseElement.classList.remove('hidden');
        responseElement.innerHTML = `
            <div class="status-badge status-error">❌ Error</div>
            <h3>Failed to create test media entries</h3>
            <div class="response-content">Error: ${error.message}</div>
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
    
    try {
        // Log the data we're sending
        console.log("Creating rating with data:", {
            mediaEntry,
            user,
            stars,
            comment
            // Note: publicVisible is not sent as it's now only set via the approve endpoint
        });
        
        // Check if we should use the shorthand endpoint (when the user is rating their own media)
        if (user === savedData.userId) {
            console.log("Using shorthand endpoint for rating");
            
            // Use the shorthand endpoint which takes fewer parameters
            const result = await apiCall(`/media/${mediaEntry}/rate`, 'POST', {
                stars,
                comment
                // publicVisible removed as it can only be set via approval
            }, true);
            
            displayResponse('rating-create-response', result.status, result.data, result.ok);
            
            if (result.ok && result.data && result.data.uuid) {
                savedData.ratingId = result.data.uuid;
                saveData();
                alert('Rating created successfully!');
            } else if (!result.ok) {
                console.error("Error response:", result);
                alert(`Failed to create rating: ${result.data && result.data.error ? result.data.error : 'Unknown error'}`);
            }
        } else {
            console.log("Using standard endpoint for rating");
            
            // For standard endpoint, ensure we're sending exactly what the backend expects
            // First, check if the media ID and user ID are valid GUIDs
            if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(mediaEntry)) {
                alert('Please enter a valid Media ID (must be a GUID)');
                return;
            }
            
            if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(user)) {
                alert('Please enter a valid User ID (must be a GUID)');
                return;
            }
            
            // Use standard endpoint with all required fields
            const result = await apiCall('/ratings', 'POST', {
                // Include only the fields that the C# RatingDto expects
                mediaEntry,
                user,
                stars,
                comment: comment || undefined  // Only send if not empty
                // publicVisible removed as it can only be set via approval
            }, true);
            
            displayResponse('rating-create-response', result.status, result.data, result.ok);
            
            if (result.ok && result.data && result.data.uuid) {
                savedData.ratingId = result.data.uuid;
                saveData();
                alert('Rating created successfully!');
            } else if (!result.ok) {
                console.error("Error response:", result);
                alert(`Failed to create rating: ${result.data && result.data.error ? result.data.error : 'Unknown error'}`);
            }
        }
    } catch (error) {
        console.error("Error creating rating:", error);
        displayResponse('rating-create-response', 500, { error: `Error: ${error.message}` }, false);
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
    // publicVisible removed as it can only be set via approval
    
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
    
    if (!ratingId) {
        alert('Please enter a Rating ID');
        return;
    }
    
    if (!savedData.token) {
        alert('Please login first to approve ratings');
        return;
    }
    
    console.log(`Attempting to approve rating: ${ratingId}`);
    try {
        // Use POST method with the /ratings/{ratingId}/approve endpoint
        const result = await apiCall(`/ratings/${ratingId}/approve`, 'POST', null, true);
        
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


