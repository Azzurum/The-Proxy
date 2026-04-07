<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>M.E.T. Rig - Command Interface v7.0 (Final Architecture)</title>
    <link href="https://fonts.googleapis.com/css2?family=Share+Tech+Mono&display=swap" rel="stylesheet">
    <style>
        :root {
            /* Dynamic UI Variables governed by Threat Level */
            --ui-color: #00ffcc;
            --ui-bg: rgba(0, 255, 204, 0.05);
            --ui-border: rgba(0, 255, 204, 0.3);
            --ui-glow: rgba(0, 255, 204, 0.5);
            --ui-text-dim: rgba(0, 255, 204, 0.7);
            
            /* Core Structural Variables */
            --dash-height: 140px;
            --inspector-height: 75px;
            --bottom-total: calc(var(--dash-height) + var(--inspector-height));
            --visor-width: 420px;
            --tray-width: 380px;
        }

        * { box-sizing: border-box; user-select: none; margin: 0; padding: 0; }
        body { background-color: #010101; color: #fff; font-family: 'Share Tech Mono', monospace; overflow: hidden; display: flex; justify-content: center; align-items: center; height: 100vh; }

        /* The Game World Container */
        .game-window { position: relative; width: 1600px; height: 900px; background: #05080a; overflow: hidden; border: 1px solid #222; }
        
        /* Environmental Effects */
        .threat-vignette { position: absolute; top:0; left:0; width:100%; height:100%; box-shadow: inset 0 0 0px rgba(150,0,0,0); pointer-events: none; transition: box-shadow 0.5s ease-out, background 0.5s; z-index: 5; }
        .scanlines { position: absolute; top:0; left:0; width:100%; height:100%; background: repeating-linear-gradient(to bottom, transparent 0px, transparent 2px, rgba(0,0,0,0.15) 3px, rgba(0,0,0,0.15) 4px); pointer-events: none; z-index: 99;}

        /* Player Reference */
        .player-sprite { position: absolute; top: 38%; left: 50%; transform: translate(-50%, -50%); width: 48px; height: 48px; background: #444; border: 2px solid #222; z-index: 1; box-shadow: 0 10px 30px rgba(0,0,0,0.8); }

        /* Master Controls (Top) */
        .controls { position: absolute; top: 15px; left: 50%; transform: translateX(-50%); display: flex; gap: 15px; z-index: 10000; align-items: center; background: rgba(0,0,0,0.8); padding: 10px 20px; border: 1px solid #333; border-radius: 4px; backdrop-filter: blur(5px); }
        .btn { background: rgba(255, 255, 255, 0.1); color: #fff; border: 1px solid #fff; padding: 10px 20px; font-family: inherit; font-size: 16px; cursor: pointer; text-transform: uppercase; font-weight: bold; transition: all 0.2s; letter-spacing: 1px;}
        .btn:hover, .btn.active { background: #fff; color: #000; box-shadow: 0 0 15px #fff;}
        .threat-group { display: flex; align-items: center; gap: 10px; border-left: 1px solid #444; padding-left: 15px; color: #aaa; font-size: 12px;}
        input[type=range] { width: 150px; accent-color: red; cursor: pointer; }

        /* =========================================
           MAIN VISORS (LEFT & RIGHT)
           ========================================= */
        .visor-panel { 
            position: absolute; 
            top: 0; 
            bottom: var(--bottom-total); /* Stops exactly above the Inspector Bar */
            width: var(--visor-width); 
            background: rgba(5, 5, 8, 0.95); 
            backdrop-filter: blur(12px); 
            border: 2px solid var(--ui-border); 
            border-top: none; 
            border-bottom: none;
            z-index: 40; 
            display: flex; 
            flex-direction: column; 
            transition: transform 0.4s cubic-bezier(0.1, 0.8, 0.1, 1), border-color 0.3s; 
            box-shadow: 0 0 50px rgba(0,0,0,0.9); 
            padding: 25px; 
        }
        
        .visor-left { left: 0; transform: translateX(-100%); border-left: none; }
        .visor-right { right: 0; transform: translateX(100%); border-right: none; }
        .is-open .visor-left, .is-open .visor-right { transform: translateX(0); }

        .panel-header { font-size: 18px; color: var(--ui-color); border-bottom: 2px solid var(--ui-border); padding-bottom: 15px; margin-bottom: 20px; text-align: center; letter-spacing: 3px; transition: color 0.3s, border-color 0.3s; font-weight: bold; flex-shrink: 0; }
        
        /* Grid Systems - Maximized & Perfectly Symmetrical */
        .grid-wrapper { flex-grow: 1; display: flex; justify-content: center; align-items: stretch; overflow: hidden; }
        .grid-container { 
            display: grid; 
            grid-template-columns: repeat(5, 1fr); 
            grid-template-rows: repeat(10, 1fr); 
            gap: 6px; 
            aspect-ratio: 1 / 2; /* Forces perfect squares */
            max-height: 100%; 
            max-width: 100%;
        }
        .grid-cell { background: var(--ui-bg); border: 1px dashed var(--ui-border); display: flex; align-items: center; justify-content: center; position: relative; transition: all 0.2s; }
        .grid-cell:hover { border-style: solid; background: rgba(255,255,255,0.15); z-index: 50; box-shadow: 0 0 15px var(--ui-glow); }
        
        /* Draggable Items */
        .item { width: 88%; height: 88%; background: var(--ui-color); color: #000; font-weight: bold; font-size: 14px; display: flex; align-items: center; justify-content: center; cursor: grab; z-index: 60; transition: background 0.3s, box-shadow 0.3s; box-shadow: inset 0 0 10px rgba(255,255,255,0.5); border-radius: 2px; }
        .item.ext-item { background: #9bb; box-shadow: inset 0 0 10px rgba(0,0,0,0.5); }
        .item:active { cursor: grabbing; transform: scale(1.1); box-shadow: 0 0 20px var(--ui-color); background: #fff; }
        .item.dragging { opacity: 0; }

        /* =========================================
           DIEGETIC HARDWARE LATCHES (FOLDERS)
           ========================================= */
        .hardware-latch { position: absolute; top: 20%; width: 44px; height: 200px; background: rgba(5,5,8,0.95); border: 2px solid var(--ui-border); display: flex; align-items: center; justify-content: center; cursor: pointer; color: var(--ui-color); font-size: 14px; font-weight: bold; letter-spacing: 4px; writing-mode: vertical-rl; text-orientation: mixed; transition: all 0.2s; box-shadow: 0 0 20px rgba(0,0,0,0.8); z-index: 45; }
        .hardware-latch:hover { background: var(--ui-color); color: #000; box-shadow: 0 0 20px var(--ui-color); }
        .hardware-latch.active { background: var(--ui-color); color: #000; }
        .latch-right { right: -44px; border-left: none; border-radius: 0 8px 8px 0; }
        .latch-left { left: -44px; border-right: none; border-radius: 8px 0 0 8px; transform: rotate(180deg); }

        /* =========================================
           SLIDE-OUT SUB-PANELS (MAP & EXT-NODE)
           ========================================= */
        .sub-panel { 
            position: absolute; 
            top: 40px; 
            bottom: calc(var(--bottom-total) + 40px); 
            width: var(--tray-width); 
            background: rgba(10, 10, 14, 0.95); 
            border: 2px solid var(--ui-border); 
            z-index: 30; 
            display: flex; 
            flex-direction: column; 
            padding: 25px; 
            transition: transform 0.5s cubic-bezier(0.2, 0.8, 0.2, 1), box-shadow 0.3s; 
            box-shadow: inset 0 0 50px rgba(0,0,0,0.8); 
        }
        
        .ext-node-panel { left: calc(var(--visor-width) - 5px); transform: translateX(-120%); border-left: none; border-radius: 0 10px 10px 0; border-color: #9bb; }
        .ext-open .ext-node-panel { transform: translateX(0); box-shadow: 15px 0 30px rgba(0,0,0,0.8), inset 0 0 50px rgba(0,0,0,0.8); }

        .map-panel { right: calc(var(--visor-width) - 5px); transform: translateX(120%); border-right: none; border-radius: 10px 0 0 10px; }
        .map-open .map-panel { transform: translateX(0); box-shadow: -15px 0 30px rgba(0,0,0,0.8), inset 0 0 50px rgba(0,0,0,0.8); }

        /* External Grid (5x5) Perfectly Square */
        .grid-ext-wrapper { width: 100%; display: flex; justify-content: center; margin-bottom: 20px; }
        .grid-ext { display: grid; grid-template-columns: repeat(5, 1fr); grid-template-rows: repeat(5, 1fr); gap: 6px; aspect-ratio: 1/1; width: 100%; max-height: 100%; }

        /* The Map System */
        .map-container { flex-grow: 1; border: 1px solid var(--ui-border); background: linear-gradient(rgba(0, 255, 204, 0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(0, 255, 204, 0.05) 1px, transparent 1px); background-size: 20px 20px; position: relative; overflow: hidden; box-shadow: inset 0 0 30px rgba(0,0,0,0.8); }
        .map-room { position: absolute; border: 1px solid var(--ui-border); background: rgba(0, 255, 204, 0.02); display: flex; align-items: center; justify-content: center; font-size: 10px; color: var(--ui-text-dim); text-align: center; }
        .blip-player { position: absolute; width: 10px; height: 10px; background: #fff; border-radius: 50%; top: 40%; left: 50%; transform: translate(-50%, -50%); box-shadow: 0 0 10px #fff, 0 0 20px var(--ui-color); animation: pulseMap 2s infinite; }
        .blip-proxy { position: absolute; width: 14px; height: 14px; background: #ff0033; border-radius: 50%; top: 90%; left: 80%; transform: translate(-50%, -50%); box-shadow: 0 0 20px #ff0033; opacity: 0; transition: top 0.5s, left 0.5s, opacity 0.5s; z-index: 5;}
        @keyframes pulseMap { 0% { transform: translate(-50%, -50%) scale(1); opacity: 1; } 100% { transform: translate(-50%, -50%) scale(3); opacity: 0; } }

        /* =========================================
           BOTTOM UI GROUP (INSPECTOR BAR + DASHBOARD)
           ========================================= */
        .bottom-ui-group { 
            position: absolute; 
            bottom: 0; left: 0; right: 0; 
            height: var(--bottom-total); 
            z-index: 50; 
            display: flex; 
            flex-direction: column; 
            transform: translateY(100%); 
            transition: transform 0.4s cubic-bezier(0.1, 0.8, 0.1, 1); 
        }
        .is-open .bottom-ui-group { transform: translateY(0); }

        /* NEW: Horizontal Inspector Bar */
        .inspector-bar { 
            height: var(--inspector-height); 
            background: rgba(10, 10, 15, 0.98); 
            border-top: 2px solid var(--ui-border); 
            border-bottom: 1px solid var(--ui-border); 
            display: flex; 
            align-items: center; 
            padding: 0 30px; 
            gap: 30px; 
            transition: border-color 0.3s;
            box-shadow: 0 -10px 30px rgba(0,0,0,0.5);
        }
        .inspect-icon { width: 45px; height: 45px; border: 1px solid var(--ui-border); background: var(--ui-bg); display: flex; align-items: center; justify-content: center; font-size: 18px; color: var(--ui-color); box-shadow: inset 0 0 10px var(--ui-glow); flex-shrink: 0;}
        .inspect-title-group { display: flex; flex-direction: column; justify-content: center; min-width: 250px; }
        .inspect-title { font-size: 20px; font-weight: bold; color: #fff; text-transform: uppercase; letter-spacing: 2px;}
        .inspect-sub { font-size: 12px; color: var(--ui-text-dim); font-weight: bold;}
        .inspect-desc { font-size: 14px; color: #ddd; line-height: 1.4; border-left: 2px solid var(--ui-color); padding-left: 20px; flex-grow: 1; }

        /* Bottom Dashboard (Tactical & Biometrics) */
        .bottom-dashboard { 
            height: var(--dash-height); 
            background: rgba(5, 5, 8, 0.98); 
            display: flex; 
            align-items: stretch; 
            padding: 15px 30px; 
            gap: 25px; 
            backdrop-filter: blur(20px); 
        }

        .hud-block { border: 1px solid var(--ui-border); background: rgba(0,0,0,0.6); padding: 12px 20px; display: flex; flex-direction: column; transition: border-color 0.3s; position: relative; }
        .hud-label { font-size: 11px; color: var(--ui-text-dim); margin-bottom: 8px; letter-spacing: 2px; font-weight: bold; transition: color 0.3s; text-transform: uppercase;}

        /* Tactical Loadout (Hotbar + Trash) */
        .tactical-block { flex: 0 0 400px; display: flex; flex-direction: row; gap: 15px; background: transparent; border: none; padding: 0; }
        .hotbar-area { flex: 1; border: 1px solid var(--ui-border); background: rgba(0,0,0,0.6); padding: 10px 15px; display: flex; flex-direction: column; }
        .hotbar-slots { display: flex; gap: 15px; justify-content: space-between; flex-grow: 1; align-items: center; }
        .hotbar-slot { width: 65px; height: 65px; background: var(--ui-bg); border: 1px solid var(--ui-border); position: relative; display: flex; align-items: center; justify-content: center; }
        .hotbar-slot::before { content: '0' attr(data-key); position: absolute; top: 2px; left: 4px; font-size: 12px; color: var(--ui-text-dim); font-weight:bold;}
        
        .trash-area { flex: 0 0 90px; border: 1px dashed #ff0033; background: rgba(255,0,51,0.05); display: flex; flex-direction: column; align-items: center; justify-content: center; color: #ff0033; transition: all 0.2s; cursor: pointer;}
        .trash-area.drag-over { background: rgba(255,0,51,0.3); box-shadow: inset 0 0 20px rgba(255,0,51,0.8); border-style: solid; color: #fff; }

        /* Biometrics */
        .bio-block { flex: 0 0 360px; display: flex; flex-direction: row; gap: 20px; align-items: center; justify-content: center; }
        .face-container { width: 75px; height: 85px; background: #000; border: 1px solid #444; position: relative; filter: grayscale(1) contrast(1.2); transition: filter 0.3s; box-shadow: inset 0 0 15px rgba(0,0,0,0.9); border-radius: 4px; overflow: hidden; flex-shrink: 0;}
        .face-head { position: absolute; bottom: -5%; left: 10%; width: 80%; height: 80%; background: #ccc; border-radius: 20px 20px 10px 10px; }
        .face-eye { position: absolute; top: 35%; width: 14px; height: 8px; background: #111; border-radius: 50%; transition: all 0.1s; }
        .eye-l { left: 14px; } .eye-r { right: 14px; }
        .face-mouth { position: absolute; bottom: 20%; left: 50%; transform: translateX(-50%); width: 22px; height: 4px; background: #111; border-radius: 2px; transition: all 0.1s; }

        .ekg-wrapper { flex-grow: 1; height: 100%; display: flex; flex-direction: column; justify-content: space-between; }
        .ekg-top { display: flex; justify-content: space-between; align-items: flex-start; }
        .bpm-number { font-size: 38px; font-weight: bold; color: var(--ui-color); line-height: 0.8; transition: color 0.3s; }
        .ekg-graph { width: 100%; height: 40px; margin-top: auto; }
        .ekg-graph path { stroke: var(--ui-color); stroke-width: 2; fill: none; stroke-linecap: round; stroke-linejoin: round; stroke-dasharray: 100; stroke-dashoffset: 100; animation: drawEKG 2s linear infinite; transition: stroke 0.3s; }
        @keyframes drawEKG { 0% { stroke-dashoffset: 100; } 100% { stroke-dashoffset: 0; } }

        /* MOTHER-v4 Log */
        .log-block { flex: 1; display: flex; flex-direction: column; justify-content: flex-start; }
        .mother-text { font-size: 15px; color: #bbb; line-height: 1.6; }
        .mother-text strong { color: var(--ui-color); transition: color 0.3s; }

        /* General Effects */
        .signal-spike { position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: rgba(255, 255, 255, 0.8); z-index: 999; opacity: 0; pointer-events: none; mix-blend-mode: overlay; }
        .ui-jitter { animation: uiJitter 0.1s infinite; }
        @keyframes uiJitter { 0% { transform: translate(1px, 1px); } 50% { transform: translate(-1px, -2px); } 100% { transform: translate(-2px, 1px); } }

    </style>
</head>
<body>
    <div class="game-window" id="gameWindow">
        <div class="threat-vignette" id="vignette"></div>
        <div class="signal-spike" id="signalSpike"></div>
        <div class="scanlines"></div>
        <div class="player-sprite"></div>
        
        <div class="controls">
            <button class="btn" id="toggleBtn">SYSTEM BOOT // M.E.T. RIG</button>
            <div class="threat-group">
                <label>Proxy Proximity:</label>
                <input type="range" id="threatSlider" min="0" max="100" value="0">
            </div>
        </div>

        <div class="visor-panel visor-left" id="visorLeft">
            <div class="panel-header">LOCAL M.E.T. BUFFER [LEFT]</div>
            <div class="grid-wrapper">
                <div class="grid-container" id="gridLeft"></div>
            </div>
            <div class="hardware-latch latch-right" id="latchExt">EXT-NODE</div>
        </div>

        <div class="sub-panel ext-node-panel" id="extPanel">
            <div class="panel-header" style="color: #9bb; border-color: #9bb;">CONTAINER DETECTED</div>
            <div class="grid-ext-wrapper">
                <div class="grid-ext" id="gridExt"></div>
            </div>
            <div class="inspect-desc" style="border-left-color: #9bb; color: #999; font-size: 12px; padding:0;">> Foreign matter detected.<br>> Assimilation authorized.</div>
        </div>

        <div class="visor-panel visor-right" id="visorRight">
            <div class="panel-header">LOCAL M.E.T. BUFFER [RIGHT]</div>
            <div class="grid-wrapper">
                <div class="grid-container" id="gridRight"></div>
            </div>
            <div class="hardware-latch latch-left" id="latchMap">NAV-LINK</div>
        </div>

        <div class="sub-panel map-panel" id="mapPanel">
            <div class="panel-header">WAYFARER NAV-LINK</div>
            <div class="map-container">
                <div class="map-room" style="top: 10%; left: 10%; width: 30%; height: 30%;">MEDBAY</div>
                <div class="map-room" style="top: 10%; left: 50%; width: 40%; height: 20%;">CREW QUARTERS</div>
                <div class="map-room" style="top: 40%; left: 30%; width: 40%; height: 40%;">MAIN HALLWAY</div>
                <div class="map-room" style="top: 70%; left: 60%; width: 30%; height: 20%;">AIRLOCK C</div>
                
                <div class="blip-player"></div>
                <div class="blip-proxy" id="proxyBlip"></div>
            </div>
        </div>

        <div class="bottom-ui-group" id="bottomGroup">
            
            <div class="inspector-bar" id="inspector">
                <div class="inspect-icon" id="insIcon">?</div>
                <div class="inspect-title-group">
                    <div class="inspect-title" id="insName">AWAITING I/O</div>
                    <div class="inspect-sub" id="insSub">MASS: N/A | STATUS: UNKNOWN</div>
                </div>
                <div class="inspect-desc" id="insDesc">Select a digitized matter node to view quantum properties and MOTHER-v4 structural analysis.</div>
            </div>

            <div class="bottom-dashboard" id="dashboard">
                
                <div class="tactical-block">
                    <div class="hotbar-area">
                        <div class="hud-label">ACTIVE MATERIEL</div>
                        <div class="hotbar-slots">
                            <div class="hotbar-slot" data-key="1" id="hb1"></div>
                            <div class="hotbar-slot" data-key="2" id="hb2"></div>
                            <div class="hotbar-slot" data-key="3" id="hb3"></div>
                        </div>
                    </div>
                    <div class="trash-area" id="trashZone">
                        <div style="font-size: 26px; font-weight: bold;">[ X ]</div>
                        <div style="font-size: 12px; margin-top:5px; letter-spacing: 2px; font-weight:bold;">PURGE</div>
                    </div>
                </div>

                <div class="hud-block bio-block">
                    <div class="face-container" id="kaelenFace">
                        <div class="face-head"></div>
                        <div class="face-eye eye-l" id="eyeL"></div>
                        <div class="face-eye eye-r" id="eyeR"></div>
                        <div class="face-mouth" id="mouth"></div>
                    </div>
                    <div class="ekg-wrapper">
                        <div class="ekg-top">
                            <div class="hud-label">HOST BIOMETRICS</div>
                            <div style="text-align: right;">
                                <span class="bpm-number" id="bpmText">62</span>
                                <span style="font-size: 14px; color: var(--ui-text-dim); font-weight:bold;">BPM</span>
                            </div>
                        </div>
                        <svg class="ekg-graph" viewBox="0 0 100 30" preserveAspectRatio="none">
                            <path id="ekgPath" d="M0,15 L10,15 L15,5 L20,25 L25,15 L100,15"></path>
                        </svg>
                    </div>
                </div>

                <div class="hud-block log-block">
                    <div class="hud-label">SYMBIOTIC LINK // MOTHER-V4</div>
                    <div class="mother-text" id="motherLog">
                        > Establishing neural handshake...<br>
                        > Environment stable. <strong>Awaiting directives, Custodian.</strong>
                    </div>
                </div>

            </div>
        </div>
    </div>

    <script>
        const ui = {
            window: document.getElementById('gameWindow'),
            toggleBtn: document.getElementById('toggleBtn'),
            threatSlider: document.getElementById('threatSlider'),
            spike: document.getElementById('signalSpike'),
            vignette: document.getElementById('vignette'),
            trash: document.getElementById('trashZone'),
            dash: document.getElementById('dashboard'),
            bottomGroup: document.getElementById('bottomGroup'),
            proxyBlip: document.getElementById('proxyBlip')
        };

        const hardware = {
            latchExt: document.getElementById('latchExt'),
            latchMap: document.getElementById('latchMap'),
        };

        const inspector = {
            icon: document.getElementById('insIcon'),
            name: document.getElementById('insName'),
            sub: document.getElementById('insSub'),
            desc: document.getElementById('insDesc')
        };

        const bio = {
            face: document.getElementById('kaelenFace'),
            eyeL: document.getElementById('eyeL'),
            eyeR: document.getElementById('eyeR'),
            mouth: document.getElementById('mouth'),
            ekg: document.getElementById('ekgPath'),
            bpm: document.getElementById('bpmText'),
            log: document.getElementById('motherLog')
        };

        const root = document.documentElement; 
        let draggedItem = null;
        let audioCtx;
        let nextBeatTime = 0;
        let heartbeatRunning = false;
        
        let isVisorOpen = false;
        let isExtOpen = false;
        let isMapOpen = false;

        function initAudio() {
            if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        }

        // ================= MASTER TOGGLE =================

        ui.toggleBtn.addEventListener('click', () => {
            initAudio();
            isVisorOpen = !isVisorOpen;
            ui.window.classList.toggle('is-open');
            ui.toggleBtn.classList.toggle('active');
            ui.toggleBtn.innerText = isVisorOpen ? "SYSTEM OFFLINE // DISENGAGE" : "SYSTEM BOOT // M.E.T. RIG";
            
            if (isVisorOpen) {
                playDigitalBoot();
                scheduleHeartbeat();
            } else {
                if(isExtOpen) toggleExtNode();
                if(isMapOpen) toggleMap();
            }
        });

        // ================= DIEGETIC LATCHES =================

        hardware.latchExt.addEventListener('click', toggleExtNode);
        hardware.latchMap.addEventListener('click', toggleMap);

        function toggleExtNode() {
            if(!isVisorOpen) return;
            isExtOpen = !isExtOpen;
            ui.window.classList.toggle('ext-open');
            hardware.latchExt.classList.toggle('active');
            if(isExtOpen) playMechanicalTray();
        }

        function toggleMap() {
            if(!isVisorOpen) return;
            isMapOpen = !isMapOpen;
            ui.window.classList.toggle('map-open');
            hardware.latchMap.classList.toggle('active');
            if(isMapOpen) playMechanicalTray();
        }

        // ================= INVENTORY SYSTEM =================

        function setupGrid(containerId, count) {
            const container = document.getElementById(containerId);
            for (let i = 0; i < count; i++) {
                const cell = document.createElement('div');
                cell.className = 'grid-cell';
                cell.addEventListener('dragover', e => e.preventDefault());
                cell.addEventListener('drop', handleDrop);
                container.appendChild(cell);
            }
            return container;
        }
        
        const gridLeft = setupGrid('gridLeft', 50);
        const gridRight = setupGrid('gridRight', 50);
        const gridExt = setupGrid('gridExt', 25);
        
        ['hb1', 'hb2', 'hb3'].forEach(id => {
            const el = document.getElementById(id);
            el.addEventListener('dragover', e => e.preventDefault());
            el.addEventListener('drop', handleDrop);
        });

        ui.trash.addEventListener('dragover', e => { e.preventDefault(); ui.trash.classList.add('drag-over'); });
        ui.trash.addEventListener('dragleave', () => { ui.trash.classList.remove('drag-over'); });
        ui.trash.addEventListener('drop', e => {
            e.preventDefault(); 
            ui.trash.classList.remove('drag-over');
            if (draggedItem) { 
                draggedItem.remove(); 
                triggerSpike(true); 
                updateInspector(null);
            }
        });

        const itemDB = {
            'BATT': { name: 'Aether-Core', mass: '1.2kg', state: 'Volatile', desc: 'Unstable corporate power supply. Radiation leaking from casing.', ext: false },
            'KEY':  { name: 'Sec-Pass V4', mass: '0.1kg', state: 'Encrypted', desc: 'Quantum-encrypted physical master key for lower decks.', ext: false },
            'HEAL': { name: 'Coagulant', mass: '0.4kg', state: 'Sterile', desc: 'Emergency bio-foam injector. Designed to keep assets working.', ext: false },
            'SCRP': { name: 'Scrap Plating', mass: '4.5kg', state: 'Inert', desc: 'Torn bulkhead plating. Heavy, but useful for makeshift armor.', ext: true },
            'DATA': { name: 'Lore Drive', mass: '0.1kg', state: 'Corrupted', desc: 'Audio logs from the previous Custodian. They sound frantic.', ext: true }
        };

        function spawnItem(id, pos, targetContainer) {
            const data = itemDB[id];
            const target = targetContainer.children[pos];
            const item = document.createElement('div');
            item.className = `item ${data.ext ? 'ext-item' : ''}`;
            item.draggable = true;
            item.innerText = id.substring(0,3); 
            item.dataset.id = id;

            item.addEventListener('dragstart', () => {
                draggedItem = item;
                setTimeout(() => item.classList.add('dragging'), 0);
                updateInspector(item);
            });
            item.addEventListener('dragend', () => { 
                if(draggedItem) draggedItem.classList.remove('dragging'); 
                draggedItem = null; 
            });
            item.addEventListener('mousedown', () => updateInspector(item));
            if (target) target.appendChild(item);
        }

        spawnItem('BATT', 12, gridLeft);
        spawnItem('KEY', 34, gridRight);
        spawnItem('HEAL', 0, document.getElementById('hb1').parentElement);
        document.getElementById('hb1').appendChild(document.getElementById('hb1').parentElement.querySelector('.item'));
        
        spawnItem('SCRP', 2, gridExt);
        spawnItem('DATA', 14, gridExt);

        function handleDrop(e) {
            e.preventDefault();
            const t = e.currentTarget;
            if (t.children.length === 0 || (t.children.length === 1 && t.children[0].tagName === 'STYLE')) {
                t.appendChild(draggedItem);
                if(!t.closest('#gridExt')) draggedItem.classList.remove('ext-item');
                triggerSpike(false);
            }
        }

        function updateInspector(itemEl) {
            if (!itemEl) {
                inspector.icon.innerText = '?';
                inspector.name.innerText = 'AWAITING I/O';
                inspector.sub.innerText = 'MASS: N/A | STATUS: UNKNOWN';
                inspector.desc.innerHTML = 'Select a digitized matter node to view quantum properties and MOTHER-v4 structural analysis.';
                return;
            }
            const data = itemDB[itemEl.dataset.id];
            inspector.icon.innerText = itemEl.innerText;
            inspector.name.innerText = data.name;
            inspector.sub.innerText = `MASS: ${data.mass} | STATUS: ${data.state}`;
            inspector.desc.innerHTML = `<strong>ANALYSIS:</strong> ${data.desc}`;
        }

        // ================= DYNAMIC THREAT SYSTEM =================

        function triggerSpike(isPurge) {
            ui.spike.style.background = isPurge ? "rgba(255, 0, 50, 0.4)" : "rgba(255, 255, 255, 0.2)";
            ui.spike.style.opacity = '1';
            ui.spike.style.transition = 'none';
            setTimeout(() => {
                ui.spike.style.transition = 'opacity 0.5s ease-out';
                ui.spike.style.opacity = '0';
            }, 50);
            playDigitalZip();
        }

        ui.threatSlider.addEventListener('input', (e) => {
            const threat = e.target.value; 
            
            ui.vignette.style.boxShadow = `inset 0 0 ${threat * 5}px rgba(150, 0, 0, ${threat/100})`;
            
            if (threat > 10) {
                ui.proxyBlip.style.opacity = (threat / 100);
                const topProg = 90 - (threat * 0.5); 
                const leftProg = 80 - (threat * 0.3); 
                ui.proxyBlip.style.top = `${topProg}%`;
                ui.proxyBlip.style.left = `${leftProg}%`;
            } else {
                ui.proxyBlip.style.opacity = 0;
            }

            if (threat < 40) {
                root.style.setProperty('--ui-color', '#00ffcc');
                root.style.setProperty('--ui-bg', 'rgba(0, 255, 204, 0.05)');
                root.style.setProperty('--ui-border', 'rgba(0, 255, 204, 0.3)');
                ui.bottomGroup.classList.remove('ui-jitter');
                updateFace(1, 'none', '4px', '2px');
                bio.bpm.innerText = Math.floor(60 + (threat * 0.5));
                bio.ekg.style.animationDuration = '2s';
                bio.log.innerHTML = `> Connection stable. All systems nominal.<br>> <strong>Awaiting directives, Custodian.</strong>`;
            } 
            else if (threat >= 40 && threat < 80) {
                root.style.setProperty('--ui-color', '#ffaa00');
                root.style.setProperty('--ui-bg', 'rgba(255, 170, 0, 0.05)');
                root.style.setProperty('--ui-border', 'rgba(255, 170, 0, 0.4)');
                ui.bottomGroup.classList.remove('ui-jitter');
                updateFace(0.5, 'translate(2px, 0)', '6px', '2px'); 
                bio.bpm.innerText = Math.floor(80 + (threat * 0.8));
                bio.ekg.style.animationDuration = '1s';
                bio.log.innerHTML = `> Proximity alert. Massive anomaly detected on Nav-Link.<br>> <strong>Minimize digitization noise immediately.</strong>`;
            } 
            else {
                root.style.setProperty('--ui-color', '#ff0033');
                root.style.setProperty('--ui-bg', 'rgba(255, 0, 51, 0.08)');
                root.style.setProperty('--ui-border', 'rgba(255, 0, 51, 0.6)');
                ui.bottomGroup.classList.add('ui-jitter');
                updateFace(0, `translate(${Math.random()*4-2}px, 0)`, '12px', '50%'); 
                bio.bpm.innerText = Math.floor(120 + (threat * 1.5));
                bio.ekg.style.animationDuration = '0.4s';
                bio.log.innerHTML = `> <strong>CRITICAL: ENTITY IS IN VISUAL RANGE.</strong><br>> Host survival probability: 4.2%`;
            }
        });

        function updateFace(grayscale, eyeTransform, mouthHeight, mouthRadius) {
            bio.face.style.filter = `grayscale(${grayscale}) ${grayscale === 0 ? 'sepia(1) hue-rotate(-50deg)' : ''}`;
            bio.eyeL.style.transform = eyeTransform;
            bio.eyeR.style.transform = eyeTransform;
            bio.mouth.style.height = mouthHeight;
            bio.mouth.style.borderRadius = mouthRadius;
        }

        // ================= AUDIO GENERATORS =================

        function scheduleHeartbeat() {
            if (!isVisorOpen) return;
            const threat = ui.threatSlider.value;
            const interval = 1.0 - (threat / 100 * 0.7); 

            if (audioCtx && audioCtx.currentTime >= nextBeatTime) {
                playThump(threat);
                nextBeatTime = audioCtx.currentTime + interval;
                bio.bpm.style.transform = 'scale(1.15)';
                setTimeout(() => bio.bpm.style.transform = 'scale(1)', 100);
            }
            requestAnimationFrame(scheduleHeartbeat);
        }

        function playThump(threatLevel) {
            if (!audioCtx) return;
            const osc = audioCtx.createOscillator(); const gain = audioCtx.createGain();
            osc.type = 'sine'; osc.frequency.setValueAtTime(40 + (threatLevel * 0.3), audioCtx.currentTime);
            osc.frequency.exponentialRampToValueAtTime(10, audioCtx.currentTime + 0.1);
            gain.gain.setValueAtTime(0.4 + (threatLevel / 200), audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.1);
            osc.connect(gain); gain.connect(audioCtx.destination); osc.start(); osc.stop(audioCtx.currentTime + 0.1);
        }

        function playDigitalBoot() {
            if (!audioCtx) return;
            const osc = audioCtx.createOscillator(); const gain = audioCtx.createGain();
            osc.type = 'square'; osc.frequency.setValueAtTime(150, audioCtx.currentTime); osc.frequency.exponentialRampToValueAtTime(600, audioCtx.currentTime + 0.1);
            gain.gain.setValueAtTime(0.05, audioCtx.currentTime); gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.1);
            osc.connect(gain); gain.connect(audioCtx.destination); osc.start(); osc.stop(audioCtx.currentTime + 0.1);
        }

        function playMechanicalTray() {
            if (!audioCtx) return;
            const osc = audioCtx.createOscillator(); const gain = audioCtx.createGain();
            osc.type = 'sawtooth'; osc.frequency.setValueAtTime(50, audioCtx.currentTime); osc.frequency.linearRampToValueAtTime(30, audioCtx.currentTime + 0.2);
            gain.gain.setValueAtTime(0.1, audioCtx.currentTime); gain.gain.linearRampToValueAtTime(0.001, audioCtx.currentTime + 0.2);
            osc.connect(gain); gain.connect(audioCtx.destination); osc.start(); osc.stop(audioCtx.currentTime + 0.2);
        }

        function playDigitalZip() {
            if (!audioCtx) return;
            const osc = audioCtx.createOscillator(); const gain = audioCtx.createGain();
            osc.type = 'sawtooth'; osc.frequency.setValueAtTime(800, audioCtx.currentTime); osc.frequency.exponentialRampToValueAtTime(100, audioCtx.currentTime + 0.05);
            gain.gain.setValueAtTime(0.05, audioCtx.currentTime); gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.05);
            osc.connect(gain); gain.connect(audioCtx.destination); osc.start(); osc.stop(audioCtx.currentTime + 0.05);
        }
    </script>
</body>
</html>