-- Structure de la base de données pour DashGen

CREATE DATABASE IF NOT EXISTS dashboard_db;
USE dashboard_db;

-- Table des utilisateurs
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    name VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Table des tableaux de bord
CREATE TABLE IF NOT EXISTS dashboards (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
);

-- Table des widgets
CREATE TABLE IF NOT EXISTS widgets (
    id INT AUTO_INCREMENT PRIMARY KEY,
    dashboard_id INT NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'chart-bar', 'chart-line', etc.
    title VARCHAR(255),
    x INT DEFAULT 0,
    y INT DEFAULT 0,
    width INT DEFAULT 4,
    height INT DEFAULT 3,
    data_config TEXT, -- JSON configuration for the widget
    FOREIGN KEY (dashboard_id) REFERENCES dashboards(id) ON DELETE CASCADE
);

-- Données de test
INSERT INTO users (email, password, name) VALUES ('admin@dashgen.com', 'admin123', 'Admin User');
