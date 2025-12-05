
-- zum ausführen:
-- docker exec -i pg17 psql -U app < create_database.sql


-- Datenbank erstellen (falls noch nicht vorhanden)
CREATE DATABASE mrp_db;

-- Verbindung zur Datenbank herstellen
\c mrp_db;

-- Tabelle für Users
CREATE TABLE users (
    uuid UUID PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    created TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabelle für Profiles
CREATE TABLE profiles (
    uuid UUID PRIMARY KEY,
    user_uuid UUID NOT NULL UNIQUE,
    number_of_logins INT NOT NULL DEFAULT 0,
    number_of_ratings_given INT NOT NULL DEFAULT 0,
    number_of_media_added INT NOT NULL DEFAULT 0,
    number_of_reviews_written INT NOT NULL DEFAULT 0,
    favorite_genre VARCHAR(255) NOT NULL DEFAULT '',
    favorite_media_type VARCHAR(255) NOT NULL DEFAULT '',
    sobriquet VARCHAR(255) NOT NULL DEFAULT '',
    about_me TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE
);

-- Tabelle für Media Entries
CREATE TABLE media_entries (
    uuid UUID PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    media_type VARCHAR(50) NOT NULL,
    release_year INT NOT NULL,
    age_restriction VARCHAR(50) NOT NULL,
    genre VARCHAR(255) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_uuid UUID NOT NULL,
    FOREIGN KEY (created_by_uuid) REFERENCES users(uuid) ON DELETE CASCADE
);

-- Tabelle für Ratings
CREATE TABLE ratings (
    uuid UUID PRIMARY KEY,
    media_entry_uuid UUID NOT NULL,
    user_uuid UUID NOT NULL,
    stars INT NOT NULL CHECK (stars >= 1 AND stars <= 5),
    comment TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    public_visible BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (media_entry_uuid) REFERENCES media_entries(uuid) ON DELETE CASCADE,
    FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE,
    UNIQUE(media_entry_uuid, user_uuid)
);

-- Tabelle für Rating Likes (Many-to-Many Beziehung)
CREATE TABLE rating_likes (
    rating_uuid UUID NOT NULL,
    user_uuid UUID NOT NULL,
    PRIMARY KEY (rating_uuid, user_uuid),
    FOREIGN KEY (rating_uuid) REFERENCES ratings(uuid) ON DELETE CASCADE,
    FOREIGN KEY (user_uuid) REFERENCES users(uuid) ON DELETE CASCADE
);

-- Indizes für bessere Performance
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_profiles_user_uuid ON profiles(user_uuid);
CREATE INDEX idx_media_entries_title ON media_entries(title);
CREATE INDEX idx_media_entries_created_by ON media_entries(created_by_uuid);
CREATE INDEX idx_media_entries_media_type ON media_entries(media_type);
CREATE INDEX idx_media_entries_genre ON media_entries(genre);
CREATE INDEX idx_ratings_media_entry ON ratings(media_entry_uuid);
CREATE INDEX idx_ratings_user ON ratings(user_uuid);
CREATE INDEX idx_ratings_stars ON ratings(stars);
CREATE INDEX idx_rating_likes_rating ON rating_likes(rating_uuid);
CREATE INDEX idx_rating_likes_user ON rating_likes(user_uuid);

-- View für durchschnittliche Bewertungen pro Media Entry
CREATE VIEW media_average_scores AS
SELECT 
    media_entry_uuid,
    AVG(stars) as average_score,
    COUNT(*) as rating_count
FROM ratings
GROUP BY media_entry_uuid;