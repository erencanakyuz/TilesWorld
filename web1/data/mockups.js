/**
 * Mockup Data - HTML templates for all UI component previews
 */

window.MockupData = {
    
    // Phone mockup wrapper styles (will be injected dynamically)
    phoneStyles: `
        .phone-mockup {
            width: 400px;
            height: 800px;
            background: #000;
            border-radius: 35px;
            padding: 25px;
            margin: 0 auto;
            position: relative;
            overflow: hidden;
            box-shadow: 0 0 50px rgba(0,0,0,0.3);
        }

        .phone-mockup::before {
            content: '';
            position: absolute;
            top: 12px;
            left: 50%;
            transform: translateX(-50%);
            width: 200px;
            height: 25px;
            background: #000;
            border-radius: 15px;
            z-index: 10;
        }

        @media (max-width: 768px) {
            .phone-mockup {
                width: 350px;
                height: 700px;
            }
        }
    `,

    // Component mockups
    components: {
        
        'world-tour': `
            <div class="phone-mockup">
                <div class="world-tour-bg">
                    <div class="world-header">
                        <div class="header-controls">
                            <div class="nav-btn-world back-btn">←</div>
                            <div class="world-title">🌍 World Tour</div>
                            <div class="nav-btn-world settings-btn">⚙️</div>
                        </div>
                        <div class="tour-progress">
                            <div class="progress-info">
                                <span class="visited">5/12 Cities</span>
                                <span class="completion">42% Complete</span>
                            </div>
                            <div class="progress-bar-world">
                                <div class="progress-fill-world" style="width: 42%"></div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="globe-container">
                        <div class="globe-wrapper">
                            <div class="globe">
                                <div class="globe-surface">
                                    <div class="location-pin unlocked active" style="top: 25%; left: 52%;" data-location="vienna">
                                        <div class="pin-icon">🎼</div>
                                        <div class="pin-pulse"></div>
                                    </div>
                                    <div class="location-pin completed" style="top: 30%; left: 48%;" data-location="paris">
                                        <div class="pin-icon">🗼</div>
                                        <div class="completion-star">⭐</div>
                                    </div>
                                    <div class="location-pin completed" style="top: 28%; left: 50%;" data-location="london">
                                        <div class="pin-icon">👑</div>
                                        <div class="completion-star">⭐</div>
                                    </div>
                                    <div class="location-pin locked" style="top: 45%; left: 35%;" data-location="newyork">
                                        <div class="pin-icon">🗽</div>
                                        <div class="lock-overlay">🔒</div>
                                    </div>
                                    <div class="location-pin locked" style="top: 55%; left: 85%;" data-location="tokyo">
                                        <div class="pin-icon">🏯</div>
                                        <div class="lock-overlay">🔒</div>
                                    </div>
                                    <div class="location-pin special" style="top: 70%; left: 15%;" data-location="rio">
                                        <div class="pin-icon">🎭</div>
                                        <div class="special-glow"></div>
                                    </div>
                                </div>
                                <div class="globe-grid">
                                    <div class="grid-line horizontal" style="top: 25%"></div>
                                    <div class="grid-line horizontal" style="top: 50%"></div>
                                    <div class="grid-line horizontal" style="top: 75%"></div>
                                    <div class="grid-line vertical" style="left: 25%"></div>
                                    <div class="grid-line vertical" style="left: 50%"></div>
                                    <div class="grid-line vertical" style="left: 75%"></div>
                                </div>
                            </div>
                        </div>
                        
                        <div class="globe-controls">
                            <div class="zoom-controls">
                                <div class="zoom-btn zoom-in">+</div>
                                <div class="zoom-btn zoom-out">-</div>
                            </div>
                            <div class="rotation-hint">
                                <div class="hint-icon">👆</div>
                                <div class="hint-text">Drag to rotate</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="location-details">
                        <div class="detail-card active">
                            <div class="location-header">
                                <div class="location-flag">🇦🇹</div>
                                <div class="location-info">
                                    <div class="location-name">Vienna</div>
                                    <div class="location-subtitle">Classical Music Capital</div>
                                </div>
                                <div class="difficulty-badge expert">Expert</div>
                            </div>
                            
                            <div class="location-content">
                                <div class="composer-showcase">
                                    <div class="composer-avatars">
                                        <div class="composer-avatar mozart">M</div>
                                        <div class="composer-avatar beethoven">B</div>
                                        <div class="composer-avatar schubert">S</div>
                                    </div>
                                    <div class="featured-text">Mozart, Beethoven & Schubert</div>
                                </div>
                                
                                <div class="songs-preview">
                                    <div class="song-item unlocked">
                                        <div class="song-icon">🎹</div>
                                        <div class="song-title">Eine kleine Nachtmusik</div>
                                        <div class="song-stars">⭐⭐⭐</div>
                                    </div>
                                    <div class="song-item current">
                                        <div class="song-icon">🎼</div>
                                        <div class="song-title">Symphony No. 9</div>
                                        <div class="song-progress">2/3 ⭐</div>
                                    </div>
                                </div>
                            </div>
                            
                            <div class="location-actions">
                                <div class="action-btn-world play-btn">
                                    <div class="btn-icon">▶️</div>
                                    <div class="btn-text">PLAY</div>
                                </div>
                                <div class="action-btn-world preview-btn">
                                    <div class="btn-icon">🎵</div>
                                    <div class="btn-text">PREVIEW</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="floating-compass">
                        <div class="compass-ring">
                            <div class="compass-needle"></div>
                            <div class="compass-directions">
                                <span class="direction n">N</span>
                                <span class="direction s">S</span>
                                <span class="direction e">E</span>
                                <span class="direction w">W</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `,

        'main-menu': `
            <div class="phone-mockup">
                <div class="main-menu-bg">
                    <div class="floating-particles">
                        <div class="particle"></div>
                        <div class="particle"></div>
                        <div class="particle"></div>
                        <div class="particle"></div>
                        <div class="particle"></div>
                    </div>
                    
                    <div class="daily-challenge-banner">🎯 New Daily Challenge!</div>
                    
                    <div class="menu-header">
                        <div class="game-logo">🎹</div>
                        <div class="game-title">PIANO RUSH</div>
                        <div class="game-subtitle">Rhythm Master</div>
                        <div class="version-badge">v2.1.0</div>
                    </div>
                    
                    <div class="menu-content">
                        <div class="main-buttons">
                            <button class="primary-btn">▶️ PLAY NOW</button>
                            <button class="secondary-btn">🛒 SHOP</button>
                            <button class="secondary-btn">🎵 MUSIC LIBRARY</button>
                        </div>
                        
                        <div class="quick-actions">
                            <div class="quick-action">⚙️</div>
                            <div class="quick-action" style="position: relative;">
                                🏆
                                <div class="notification-badge">!</div>
                            </div>
                            <div class="quick-action">👤</div>
                            <div class="quick-action" style="position: relative;">
                                🎯
                                <div class="notification-badge">3</div>
                            </div>
                        </div>
                        
                        <div class="stats-bar">
                            <div class="stat-item">
                                <div class="stat-icon">🪙</div>
                                <span>2,450</span>
                            </div>
                            <div class="stat-item">
                                <div class="stat-icon">💎</div>
                                <span>75</span>
                            </div>
                            <div class="stat-item">
                                <div class="stat-icon">⭐</div>
                                <span>Level 23</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="menu-footer">
                        <div class="social-links">
                            <div class="social-link">📘</div>
                            <div class="social-link">📷</div>
                            <div class="social-link">🐦</div>
                            <div class="social-link">🎮</div>
                        </div>
                        <div class="menu-info">
                            Daily Challenge Available<br>
                            Connected to Game Center
                        </div>
                    </div>
                </div>
            </div>
        `,

        'shop': `
            <div class="phone-mockup">
                <div class="shop-bg-new">
                    <div class="shop-header-new">
                        <div class="shop-nav">
                            <div class="nav-btn back-btn">←</div>
                            <div class="shop-title-new">🛒 Piano Shop</div>
                            <div class="nav-btn menu-btn">☰</div>
                        </div>
                        <div class="currency-display">
                            <div class="currency-new coins">
                                <div class="currency-icon">🪙</div>
                                <span>123,253</span>
                            </div>
                            <div class="currency-new gems">
                                <div class="currency-icon">💎</div>
                                <span>123,253</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="shop-world">
                        <div class="shop-character left-char">🎹</div>
                        <div class="shop-character right-char">🎵</div>
                        <div class="shop-items-display">
                            <div class="display-pedestal">
                                <div class="preview-item piano-skin"></div>
                            </div>
                            <div class="display-pedestal">
                                <div class="preview-item note-trail"></div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="upgrade-panels">
                        <div class="upgrade-card unlock-card">
                            <div class="card-header">
                                <div class="unlock-icon">🔓</div>
                                <div class="card-title">Unlock</div>
                                <div class="close-card">×</div>
                            </div>
                            <div class="card-content">
                                <div class="unlock-preview">
                                    <div class="theme-preview classical"></div>
                                    <div class="unlock-price">💎 2.5M</div>
                                </div>
                                <div class="unlock-info">
                                    <div class="unlock-name">Classical Theme</div>
                                    <div class="unlock-benefits">
                                        <div class="benefit">• Golden piano keys</div>
                                        <div class="benefit">• Classical music pack</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="renovation-popup">
                        <div class="renovation-header">
                            <div class="renovation-title">Renovate</div>
                            <div class="close-renovation">×</div>
                        </div>
                        <div class="renovation-subtitle">Upgrade your piano studio!</div>
                        <div class="before-after">
                            <div class="studio-preview before">
                                <div class="studio-icon basic">🎹</div>
                                <div class="studio-label">Basic Studio</div>
                            </div>
                            <div class="arrow">→</div>
                            <div class="studio-preview after">
                                <div class="studio-icon premium">🎼</div>
                                <div class="studio-label">Concert Hall</div>
                            </div>
                        </div>
                        <div class="upgrade-button-new">Upgrade</div>
                    </div>
                </div>
            </div>
        `,

        'powerups': `
            <div class="phone-mockup">
                <div class="powerup-bg-new">
                    <div class="powerup-header">
                        <div class="header-nav">
                            <div class="nav-btn-new back-btn">←</div>
                            <div class="powerup-title">🎵 Customization</div>
                            <div class="nav-btn-new preview-btn">👁️</div>
                        </div>
                        <div class="player-stats">
                            <div class="stat-bubble">
                                <div class="stat-icon-new">🎹</div>
                                <span>Master</span>
                            </div>
                            <div class="stat-bubble">
                                <div class="stat-icon-new">🔥</div>
                                <span>28 Streak</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="category-tabs">
                        <div class="category-tab active" data-category="instruments">🎹</div>
                        <div class="category-tab" data-category="tiles">💎</div>
                        <div class="category-tab" data-category="backgrounds">🌙</div>
                        <div class="category-tab" data-category="artists">🎼</div>
                        <div class="category-tab" data-category="effects">✨</div>
                    </div>
                    
                    <div class="feature-showcase">
                        <div class="showcase-container">
                            <div class="preview-stage">
                                <div class="demo-tiles">
                                    <div class="demo-tile active piano"></div>
                                    <div class="demo-tile guitar"></div>
                                    <div class="demo-tile violin"></div>
                                    <div class="demo-tile drum"></div>
                                </div>
                                <div class="sound-waves">
                                    <div class="wave"></div>
                                    <div class="wave"></div>
                                    <div class="wave"></div>
                                </div>
                            </div>
                            
                            <div class="feature-navigation">
                                <div class="nav-arrow left">‹</div>
                                <div class="feature-info">
                                    <div class="feature-name">Grand Piano</div>
                                    <div class="feature-description">Classic concert hall sound</div>
                                    <div class="feature-rarity legendary">Legendary</div>
                                </div>
                                <div class="nav-arrow right">›</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="features-grid">
                        <div class="feature-card unlocked equipped">
                            <div class="card-badge equipped-badge">Equipped</div>
                            <div class="feature-icon">🎹</div>
                            <div class="feature-title">Grand Piano</div>
                            <div class="unlock-method">Default</div>
                        </div>
                        
                        <div class="feature-card unlocked">
                            <div class="card-badge unlock-badge">Owned</div>
                            <div class="feature-icon">🎸</div>
                            <div class="feature-title">Electric Guitar</div>
                            <div class="unlock-method">Level 5</div>
                        </div>
                        
                        <div class="feature-card locked">
                            <div class="card-overlay">
                                <div class="lock-icon">🔒</div>
                                <div class="unlock-requirement">
                                    <div class="req-text">Unlock at</div>
                                    <div class="req-detail">Level 15</div>
                                </div>
                            </div>
                            <div class="feature-icon">🎻</div>
                            <div class="feature-title">Violin</div>
                            <div class="unlock-method">Level 15</div>
                        </div>
                        
                        <div class="feature-card premium">
                            <div class="card-badge premium-badge">Premium</div>
                            <div class="feature-icon">🥁</div>
                            <div class="feature-title">Jazz Drums</div>
                            <div class="unlock-method">💎 250</div>
                        </div>
                    </div>
                    
                    <div class="action-panel">
                        <div class="equip-button">EQUIP ⚡</div>
                        <div class="try-button">TRY 🎵</div>
                        <div class="unlock-button disabled">UNLOCK 💎 250</div>
                    </div>
                </div>
            </div>
        `,

        'leaderboard': `
            <div class="phone-mockup">
                <div class="leaderboard-bg-new">
                    <div class="leaderboard-header">
                        <div class="header-decoration">👑</div>
                        <div class="leaderboard-title">Leaderboard</div>
                        <div class="header-decoration">🏆</div>
                    </div>
                    
                    <div class="leaderboard-tabs-new">
                        <div class="tab-new active">Friends</div>
                        <div class="tab-new">Global</div>
                        <div class="tab-new">Weekly</div>
                    </div>
                    
                    <div class="leaderboard-content">
                        <div class="top-players">
                            <div class="top-player second">
                                <div class="player-avatar silver">S</div>
                                <div class="player-rank">#2</div>
                                <div class="player-name">Sarah</div>
                                <div class="player-score">42,150</div>
                            </div>
                            <div class="top-player first">
                                <div class="player-avatar gold">A</div>
                                <div class="crown">👑</div>
                                <div class="player-rank">#1</div>
                                <div class="player-name">Alex</div>
                                <div class="player-score">45,680</div>
                            </div>
                            <div class="top-player third">
                                <div class="player-avatar bronze">M</div>
                                <div class="player-rank">#3</div>
                                <div class="player-name">Mike</div>
                                <div class="player-score">38,920</div>
                            </div>
                        </div>
                        
                        <div class="leaderboard-list-new">
                            <div class="leaderboard-item-new your-rank">
                                <div class="rank-badge you">4</div>
                                <div class="avatar-new you">Y</div>
                                <div class="player-info-new">
                                    <div class="name-new">You</div>
                                    <div class="score-new">35,840</div>
                                </div>
                                <div class="rank-change up">+2</div>
                            </div>
                            <div class="leaderboard-item-new">
                                <div class="rank-badge">5</div>
                                <div class="avatar-new">L</div>
                                <div class="player-info-new">
                                    <div class="name-new">Lisa</div>
                                    <div class="score-new">34,520</div>
                                </div>
                                <div class="rank-change down">-1</div>
                            </div>
                        </div>
                    </div>
                    
                    <div class="leaderboard-actions">
                        <div class="action-btn challenge">⚔️ Challenge</div>
                        <div class="action-btn share">📤 Share</div>
                        <div class="action-btn refresh">🔄 Refresh</div>
                    </div>
                </div>
            </div>
        `,

        'hud': `
            <div class="phone-mockup">
                <div class="hud-bg">
                    <div class="hud-bar">
                        <div class="score">Score: 12,450</div>
                        <div class="combo-pill">Combo x15</div>
                        <div class="timer">2:30</div>
                    </div>
                    <div class="game-area">
                        <div class="tile-row">
                            <div class="tile active"></div>
                            <div class="tile"></div>
                            <div class="tile"></div>
                            <div class="tile"></div>
                        </div>
                        <div class="tile-row">
                            <div class="tile"></div>
                            <div class="tile active"></div>
                            <div class="tile"></div>
                            <div class="tile"></div>
                        </div>
                        <div class="tile-row">
                            <div class="tile"></div>
                            <div class="tile"></div>
                            <div class="tile active"></div>
                            <div class="tile"></div>
                        </div>
                    </div>
                    <div class="pause-btn">⏸️</div>
                </div>
            </div>
        `,

        'level-complete': `
            <div class="phone-mockup">
                <div class="modal-bg">
                    <div class="completion-card">
                        <div class="confetti">🎉</div>
                        <h2 class="completion-title">Level Complete!</h2>
                        <div class="stars">
                            <span class="star filled">⭐</span>
                            <span class="star filled">⭐</span>
                            <span class="star">⭐</span>
                        </div>
                        <div class="score-display">Final Score: 28,740</div>
                        <div class="completion-buttons">
                            <button class="pill-btn next-btn">Next Level</button>
                            <button class="pill-btn replay-btn">🔄 Replay</button>
                        </div>
                    </div>
                </div>
            </div>
        `,

        'settings': `
            <div class="phone-mockup">
                <div class="settings-overlay">
                    <div class="settings-panel">
                        <div class="settings-header">⚙️ Settings</div>
                        <div class="setting-item">
                            <span>Music</span>
                            <div class="toggle-switch active">
                                <div class="toggle-slider"></div>
                            </div>
                        </div>
                        <div class="setting-item">
                            <span>Sound Effects</span>
                            <div class="toggle-switch active">
                                <div class="toggle-slider"></div>
                            </div>
                        </div>
                        <div class="setting-item">
                            <span>Vibration</span>
                            <div class="toggle-switch">
                                <div class="toggle-slider"></div>
                            </div>
                        </div>
                        <div class="restore-btn">Restore Purchases</div>
                    </div>
                </div>
            </div>
        `,
        
        'achievement-gallery': `
            <div class="phone-mockup">
                <div class="achievement-gallery-bg">
                    <div class="achievement-header">
                        <div class="achievement-header-nav">
                            <div class="achievement-nav-btn">←</div>
                            <div class="achievement-title">🏆 Achievements</div>
                            <div class="achievement-nav-btn">🔍</div>
                        </div>
                        <div class="achievement-stats">
                            <div class="achievement-stat-bubble">
                                <div class="achievement-stat-icon">✓</div>
                                <span>24/48</span>
                            </div>
                            <div class="achievement-stat-bubble">
                                <div class="achievement-stat-icon">📊</div>
                                <span>50%</span>
                            </div>
                            <div class="achievement-stat-bubble">
                                <div class="achievement-stat-icon">⭐</div>
                                <span>3 Rare</span>
                            </div>
                        </div>
                    </div>

                    <div class="achievement-category-tabs">
                        <div class="achievement-category-tab active" data-category="all">🏆</div>
                        <div class="achievement-category-tab" data-category="gameplay">🎮</div>
                        <div class="achievement-category-tab" data-category="progress">📈</div>
                        <div class="achievement-category-tab" data-category="social">👥</div>
                    </div>

                    <div class="achievement-showcase">
                        <div class="achievement-showcase-container">
                            <div class="featured-achievement">
                                <div class="featured-achievement-icon legendary">👑</div>
                                <div class="featured-achievement-info">
                                    <div class="featured-title">Legendary Maestro</div>
                                    <div class="featured-description">Complete all World Tour locations</div>
                                    <div class="featured-progress">
                                        <div class="featured-progress-fill" style="width: 75%"></div>
                                    </div>
                                </div>
                                <div class="achievement-sparkles">
                                    <div class="sparkle"></div>
                                    <div class="sparkle"></div>
                                    <div class="sparkle"></div>
                                </div>
                            </div>
                            <div class="achievement-navigation">
                                <div class="achievement-nav-arrow">←</div>
                                <div class="achievement-info">
                                    <div class="achievement-name">Featured Achievement</div>
                                    <div class="achievement-category">Progress Category</div>
                                    <div class="achievement-rarity legendary">Legendary</div>
                                </div>
                                <div class="achievement-nav-arrow">→</div>
                            </div>
                        </div>
                    </div>

                    <div class="achievements-grid">
                        <!-- First Note -->
                        <div class="achievement-card unlocked">
                            <div class="achievement-card-badge unlocked">Done</div>
                            <div class="achievement-icon">🎵</div>
                            <div class="achievement-title-card">First Note</div>
                            <div class="achievement-description">Play your first piano tile</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 100%"></div>
                                </div>
                                <div class="achievement-progress-text">Completed</div>
                            </div>
                        </div>

                        <!-- Piano Virtuoso -->
                        <div class="achievement-card unlocked rare">
                            <div class="achievement-card-badge rare">Rare</div>
                            <div class="achievement-icon">🎹</div>
                            <div class="achievement-title-card">Piano Virtuoso</div>
                            <div class="achievement-description">Hit 1000 perfect notes</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 100%"></div>
                                </div>
                                <div class="achievement-progress-text">1000/1000</div>
                            </div>
                        </div>

                        <!-- Speed Demon -->
                        <div class="achievement-card unlocked">
                            <div class="achievement-card-badge unlocked">Done</div>
                            <div class="achievement-icon">⚡</div>
                            <div class="achievement-title-card">Speed Demon</div>
                            <div class="achievement-description">Play a song at 200% speed</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 100%"></div>
                                </div>
                                <div class="achievement-progress-text">Completed</div>
                            </div>
                        </div>

                        <!-- Combo Master -->
                        <div class="achievement-card unlocked">
                            <div class="achievement-card-badge unlocked">Done</div>
                            <div class="achievement-icon">🔥</div>
                            <div class="achievement-title-card">Combo Master</div>
                            <div class="achievement-description">Achieve 500 combo streak</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 100%"></div>
                                </div>
                                <div class="achievement-progress-text">500/500</div>
                            </div>
                        </div>

                        <!-- World Explorer -->
                        <div class="achievement-card unlocked">
                            <div class="achievement-card-badge unlocked">Done</div>
                            <div class="achievement-icon">🌍</div>
                            <div class="achievement-title-card">World Explorer</div>
                            <div class="achievement-description">Visit 5 different countries</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 100%"></div>
                                </div>
                                <div class="achievement-progress-text">5/5</div>
                            </div>
                        </div>

                        <!-- Locked Achievements -->
                        <div class="achievement-card locked">
                            <div class="achievement-card-badge locked">Locked</div>
                            <div class="achievement-icon">🎼</div>
                            <div class="achievement-title-card">Composer's Choice</div>
                            <div class="achievement-description">Complete 10 classical pieces</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 60%"></div>
                                </div>
                                <div class="achievement-progress-text">6/10</div>
                            </div>
                            <div class="achievement-card-overlay">
                                <div class="achievement-lock-icon">🔒</div>
                                <div class="achievement-unlock-requirement">
                                    <div class="achievement-req-text">Unlock by</div>
                                    <div class="achievement-req-detail">Play 4 more classical pieces</div>
                                </div>
                            </div>
                        </div>

                        <!-- Perfect Harmony -->
                        <div class="achievement-card locked">
                            <div class="achievement-card-badge locked">Locked</div>
                            <div class="achievement-icon">🏅</div>
                            <div class="achievement-title-card">Perfect Harmony</div>
                            <div class="achievement-description">Get 3 stars on 50 songs</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 32%"></div>
                                </div>
                                <div class="achievement-progress-text">16/50</div>
                            </div>
                            <div class="achievement-card-overlay">
                                <div class="achievement-lock-icon">🔒</div>
                                <div class="achievement-unlock-requirement">
                                    <div class="achievement-req-text">Unlock by</div>
                                    <div class="achievement-req-detail">Get 34 more 3-star ratings</div>
                                </div>
                            </div>
                        </div>

                        <!-- Precision Player -->
                        <div class="achievement-card locked">
                            <div class="achievement-card-badge locked">Locked</div>
                            <div class="achievement-icon">🎯</div>
                            <div class="achievement-title-card">Precision Player</div>
                            <div class="achievement-description">Hit 95% accuracy on Hard mode</div>
                            <div class="achievement-progress">
                                <div class="achievement-progress-bar">
                                    <div class="achievement-progress-fill" style="width: 80%"></div>
                                </div>
                                <div class="achievement-progress-text">76/95</div>
                            </div>
                            <div class="achievement-card-overlay">
                                <div class="achievement-lock-icon">🔒</div>
                                <div class="achievement-unlock-requirement">
                                    <div class="achievement-req-text">Unlock by</div>
                                    <div class="achievement-req-detail">Reach 95% accuracy</div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="achievement-action-panel">
                        <div class="achievement-view-button">
                            <span>👁️</span>
                            <span>View All</span>
                        </div>
                        <div class="achievement-share-button">
                            <span>📤</span>
                            <span>Share</span>
                        </div>
                    </div>
                </div>
            </div>
        `,
        
        'music-selection': `
            <div class="phone-mockup">
                <div class="music-selection-bg">
                    <div class="music-header">
                        <div class="music-header-nav">
                            <div class="music-nav-btn">←</div>
                            <div class="music-title">🎵 Music Library</div>
                            <div class="music-nav-btn">🔍</div>
                        </div>
                        <div class="music-player-stats">
                            <div class="music-stat-bubble">
                                <div class="music-stat-icon">🎶</div>
                                <span>247 Songs</span>
                            </div>
                            <div class="music-stat-bubble">
                                <div class="music-stat-icon">⏱️</div>
                                <span>12.5h</span>
                            </div>
                            <div class="music-stat-bubble">
                                <div class="music-stat-icon">🎼</div>
                                <span>8 Genres</span>
                            </div>
                        </div>
                    </div>

                    <div class="music-filter-tabs">
                        <div class="music-filter-tab active" data-genre="all">🎵</div>
                        <div class="music-filter-tab" data-genre="classical">🎼</div>
                        <div class="music-filter-tab" data-genre="jazz">🎷</div>
                        <div class="music-filter-tab" data-genre="modern">🎸</div>
                        <div class="music-filter-tab" data-genre="folk">🪕</div>
                        <div class="music-filter-tab" data-genre="electronic">🎹</div>
                    </div>

                    <div class="now-playing-showcase">
                        <div class="now-playing-container">
                            <div class="music-visualizer">
                                <div class="album-art">🎹</div>
                                <div class="music-info">
                                    <div class="track-title">Moonlight Sonata</div>
                                    <div class="track-artist">Ludwig van Beethoven</div>
                                    <div class="track-progress">
                                        <div class="track-progress-fill" style="width: 65%"></div>
                                    </div>
                                    <div class="track-time">2:45 / 4:12</div>
                                </div>
                                <div class="audio-bars">
                                    <div class="audio-bar"></div>
                                    <div class="audio-bar"></div>
                                    <div class="audio-bar"></div>
                                    <div class="audio-bar"></div>
                                    <div class="audio-bar"></div>
                                </div>
                            </div>
                            <div class="music-controls">
                                <div class="music-control-btn">⏮️</div>
                                <div class="music-control-btn play">▶️</div>
                                <div class="music-control-btn">⏭️</div>
                                <div class="music-info-center">
                                    <div class="current-song">Now Playing</div>
                                    <div class="current-artist">Classical Collection</div>
                                    <div class="difficulty-level medium">Medium</div>
                                </div>
                                <div class="music-control-btn">🔀</div>
                                <div class="music-control-btn">🔁</div>
                            </div>
                        </div>
                    </div>

                    <div class="music-library">
                        <!-- Canon in D -->
                        <div class="music-track playing">
                            <div class="track-badge featured">Featured</div>
                            <div class="track-art classical">🎼</div>
                            <div class="track-info">
                                <div class="track-name">Canon in D Major</div>
                                <div class="track-composer">Johann Pachelbel</div>
                                <div class="track-metadata">
                                    <span class="track-duration">3:24</span>
                                    <span class="track-bpm">64 BPM</span>
                                    <span class="track-key">D Major</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">⏸️</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                </div>
                            </div>
                        </div>

                        <!-- Für Elise -->
                        <div class="music-track">
                            <div class="track-badge new">New</div>
                            <div class="track-art classical">🎹</div>
                            <div class="track-info">
                                <div class="track-name">Für Elise</div>
                                <div class="track-composer">Ludwig van Beethoven</div>
                                <div class="track-metadata">
                                    <span class="track-duration">2:56</span>
                                    <span class="track-bpm">120 BPM</span>
                                    <span class="track-key">A Minor</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">▶️</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                </div>
                            </div>
                        </div>

                        <!-- Take Five -->
                        <div class="music-track">
                            <div class="track-art jazz">🎷</div>
                            <div class="track-info">
                                <div class="track-name">Take Five</div>
                                <div class="track-composer">Dave Brubeck</div>
                                <div class="track-metadata">
                                    <span class="track-duration">5:24</span>
                                    <span class="track-bpm">176 BPM</span>
                                    <span class="track-key">Eb Minor</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">▶️</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                </div>
                            </div>
                        </div>

                        <!-- Clocks -->
                        <div class="music-track">
                            <div class="track-art modern">🎸</div>
                            <div class="track-info">
                                <div class="track-name">Clocks</div>
                                <div class="track-composer">Coldplay</div>
                                <div class="track-metadata">
                                    <span class="track-duration">5:07</span>
                                    <span class="track-bpm">130 BPM</span>
                                    <span class="track-key">Eb Major</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">▶️</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                </div>
                            </div>
                        </div>

                        <!-- Scarborough Fair -->
                        <div class="music-track">
                            <div class="track-art folk">🪕</div>
                            <div class="track-info">
                                <div class="track-name">Scarborough Fair</div>
                                <div class="track-composer">Traditional</div>
                                <div class="track-metadata">
                                    <span class="track-duration">3:10</span>
                                    <span class="track-bpm">90 BPM</span>
                                    <span class="track-key">D Minor</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">▶️</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                    <span class="track-star empty">⭐</span>
                                </div>
                            </div>
                        </div>

                        <!-- Locked Premium Track -->
                        <div class="music-track premium">
                            <div class="track-badge premium">Premium</div>
                            <div class="track-art classical">🎻</div>
                            <div class="track-info">
                                <div class="track-name">The Four Seasons</div>
                                <div class="track-composer">Antonio Vivaldi</div>
                                <div class="track-metadata">
                                    <span class="track-duration">43:06</span>
                                    <span class="track-bpm">Various</span>
                                    <span class="track-key">Various</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">💎</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                </div>
                            </div>
                        </div>

                        <!-- Locked Track -->
                        <div class="music-track locked">
                            <div class="track-badge locked">Locked</div>
                            <div class="track-art classical">🎼</div>
                            <div class="track-info">
                                <div class="track-name">Symphony No. 9</div>
                                <div class="track-composer">Ludwig van Beethoven</div>
                                <div class="track-metadata">
                                    <span class="track-duration">65:00</span>
                                    <span class="track-bpm">120 BPM</span>
                                    <span class="track-key">D Minor</span>
                                </div>
                            </div>
                            <div class="track-actions">
                                <div class="track-play-btn">🔒</div>
                                <div class="track-stars">
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                    <span class="track-star">⭐</span>
                                </div>
                            </div>
                            <div class="track-lock-overlay">
                                <div class="track-lock-icon">🔒</div>
                            </div>
                        </div>
                    </div>

                    <div class="music-action-panel">
                        <div class="shuffle-button">
                            <span>🔀</span>
                            <span>Shuffle All</span>
                        </div>
                        <div class="playlist-button">
                            <span>📁</span>
                            <span>Create Playlist</span>
                        </div>
                    </div>

                    <div class="floating-music-notes">
                        <div class="music-note">♪</div>
                        <div class="music-note">♫</div>
                        <div class="music-note">♬</div>
                    </div>
                </div>
            </div>
        `
    }
};

console.log('🎨 Mockup Data loaded');
