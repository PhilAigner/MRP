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