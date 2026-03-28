GAME DESIGN DOCUMENT: THE PROXY
1. GAME OVERVIEW
•	Genre: 2D Pixel Top-Down Survival Horror / System Management
•	Platform: PC (Mouse & Keyboard)
•	Core Player Fantasy: You are an isolated corporate custodian trapped inside a failing automated freighter. Survival depends on managing a collapsing digital system while being hunted by a bio-mechanical entity, where every decision trades safety for progress.
Design Pillars
•	Inventory = Health = Risk: The inventory is a live, vulnerable, spatial puzzle. Engaging with it broadcasts your location.
•	The Symbiotic Burden: The "Corruption" is an AI that helps you navigate but actively wants to consume you. You trade space for power.
•	Graduated Escalation: Mistakes compound, systems degrade, and enemies adapt.
•	Tools, Not Weapons: Combat delays and repositions; it never eliminates the threat.
________________________________________
2. NARRATIVE & WORLD BUILDING
Setting: The USC Wayfarer, a fully automated Aether-Core Corporation freighter suffering a catastrophic hardware meltdown.
Player Character: Kaelen, a disposable, minimum-wage Junior Systems Custodian sent to do a manual reset to save the corporation money.
The M.E.T. Rig (The Data Pad)
Kaelen is equipped with an Aether-Core Matter Engram Translator (M.E.T. Rig). It uses Quantum-Enthalpy Digitization to break physical items down into data and store them in a "Matter Buffer" to save manual labor.
•	The Vulnerability: Translating matter takes immense power. When the inventory is opened, the suit magnetically clamps Kaelen's boots to the floor to prevent misalignment, rooting the player in place. The Rig's cooling fans scream, deafening the player to ambient noise, and it emits a massive electromagnetic flare.
The Symbiote: MOTHER-v4
MOTHER is the ship's central AI. Her servers melted during a payload breach. To survive, she is hiding inside the empty "cargo space" of Kaelen's M.E.T. Rig, manifesting physically on the grid as Corruption.
•	The Bandwidth Rule: MOTHER requires "System Bandwidth" to overwrite Kaelen's corporate firewalls. Every row of Corruption grants her more bandwidth. If she hits 10 rows, she achieves a "Kernel Panic" and fully assimilates Kaelen.
•	Adaptive Personality: * Low Corruption (Rows 0–1): Clinical, helpful, detached. Standard AI protocols.
o	Mid Corruption (Rows 2–5): Manipulative. ("You need me to survive this. Let me in.")
o	High Corruption (Rows 6–9): Possessive, hostile. Actively discourages you from Cleaning and fabricates fake audio cues.
The Hunter: The Proxy
The Proxy is The Previous Host—the ship's original Corporate Overseer. MOTHER attempted to upload herself into them, but the download crashed at 15%, warping the human into a feral, quadrupedal cyborg with a glitching terminal mask for a face.
•	Motivation: It is a walking, corrupted Ethernet cable running a broken protocol. It hunts the M.E.T. Rig's electromagnetic flares.
•	The Attack: It does not want to eat Kaelen; it wants to physically jack its exposed spinal cables into Kaelen's suit to finish MOTHER's upload. Taking a hit represents tearing yourself away, causing a massive system shock (+1 layer of Corruption).
________________________________________
3. CORE GAMEPLAY LOOP
1.	Explore: Navigate the ship, gathering critical bulky items (Keys, Tools) that force you to make hard inventory choices.
2.	Manage (The Danger Window): Open the M.E.T. Rig to use items. You are rooted in place, your hearing is compromised, and your signal alerts the Proxy.
3.	Bargain: Use MOTHER's abilities to survive, voluntarily accepting Corruption in exchange for tactical advantages.
4.	Endure & Clean: The grid fills with Corruption. You must Clean it (a massive vulnerability window) or suffer tiered system failures.
________________________________________
4. INVENTORY & SPATIAL MANAGEMENT
The UI is a full-screen, heavy terminal overlay obscuring 90% of the screen. You rely on peripheral blur, directional audio, and a haptic heartbeat indicator to gauge the Proxy's proximity.
The Grid System
•	Size: 10 × 10 Grid (Row Index 0 to 9, where 9 is the top). 100 slots total.
•	Manual Placement & Rotation: You place items exactly where you want them. You can rotate items to optimize space.
•	Data Gravity (Post-Clean Only): Items do not slowly fall like Tetris. However, when you execute a Clean to remove corruption, the system defragments: all items instantly snap down to fill the empty space below them. This removes tedious UI dragging.
Bulky Progression (Forced Engagement)
Progression requires massive inventory sacrifices.
•	Master Keys: 3×3 grid (9 slots each).
•	Fusion Welder (Required for some doors): 2×3 grid (6 slots).
•	Batteries: 1×2 grid.
________________________________________
5. CORRUPTION & THE SYMBIOTE TOOLKIT
Corruption manifests as dead blocks rising from the bottom (Row Index 0).
Dynamic Corruption Growth
Corruption grows via System Strain:
•	Taking a Hit: +1 Row (System Shock).
•	Sprinting: Slowly builds an invisible meter; extended sprinting adds +1 Row.
•	Inventory Open Time: Leaving the M.E.T. Rig open too long accelerates corruption buildup.
The Symbiote Toolkit (Active Corruption)
You can voluntarily trigger MOTHER's abilities at the cost of instant Corruption:
•	Override (+1 Row): Instantly unlock a high-tier security door.
•	Sonar (+1 Row): Reveal the Proxy's exact location on the mini-map for 5 seconds.
•	Signal Mask (+2 Rows): Jam your Rig for 8 seconds, allowing you to organize your inventory without triggering the Proxy's Hunt state.
The Buffer Zone
Rows 0 and 1 are the "Free Buffer." They take up physical space but do not trigger MOTHER's aggressive personality shifts or penalty thresholds.
________________________________________
6. SYSTEM CRUSH, EJECTION & PENALTIES
When a new corruption row spawns at Row Index 0, all items shift UP by 1 Row.
Ejection & Priority Locks
•	The Top Boundary: Row Index 9.
•	Ejection: Unlocked items pushed past Row 9 are ejected into the physical game world. (Master Keys and quest items respawn at their original spawn point).
•	Priority Locks: You can lock up to 2 essential items. Locked items cannot be ejected; they halt at Row 9.
Tiered Crush Penalty
If rising Corruption crushes a Locked item against the ceiling (Row 9), the system begins to fail gradually:
•	Tier 1 (First Contact): Movement speed reduced by 20%.
•	Tier 2 (Sustained 5s): Audio hallucinations (fake Proxy static/footsteps) begin.
•	Tier 3 (Sustained 10s): Sprint stamina drain increased by 100%.
________________________________________
7. DETECTION & ENEMY AI
Graduated Detection
Opening the inventory creates a Signal Spike.
•	Proxy Very Far: Investigates the exact coordinates of the spike.
•	Proxy Far: Delayed Hunt. You have 2–5 seconds of safe UI time before it locks onto your moving position.
•	Proxy Nearby: Immediate Hunt. It charges your location.
Faraday Zones (Safe Rooms)
Rare, shielded Server Maintenance Shafts. The magnetic dampeners block your Rig's signal. You can open your inventory here indefinitely without alerting the Proxy.
Enemy Adaptation (Stun Resistance)
The Proxy learns. If you use the Stunner:
•	1st Stun: 3-second duration.
•	2nd Stun (within 60s): 1.5-second duration.
•	3rd Stun (within 60s): Fails completely (Immune).
•	Result: You must mix Stunner usage with breaking line-of-sight using the Repulsor.
________________________________________
8. CLEAN PROTOCOLS & COMBAT TOOLS
The Clean System
•	Standard Clean: Takes 4.5 seconds. Removes Row 0. Leaves you highly vulnerable.
•	Strict Resolution Order: 1. Corruption Shifts -> 2. Ejections/Crushes Calculate -> 3. Clean is Permitted.
•	Emergency Clean (Panic Button): One-time use per save-station. Instantly purges 3 rows of Corruption, but permanently destroys 2 random inventory grid slots for the rest of the run.
Tactical Tools
•	ARC-Pulse Stunner: Stuns Proxy. Cost: Consumes 1 Battery.
•	K-80 Repulsor: Knocks Proxy back 6–8 units to break line-of-sight. Cost: Pneumatic charge (10-second cooldown, no battery).
________________________________________
9. ENDGAME STATES
The Last Stand (Kernel Panic Mode)
If Corruption hits Row 10, time slows down for exactly 3 seconds. You have one final window to trigger an Emergency Clean or use a Stunner. If you fail, the assimilation animation plays (Game Over).
Multiple Endings
Triggered when inserting the final Master Key on the Command Deck:
•	Kernel Panic (Bad - 7 to 9 Rows): MOTHER completes the overwrite. Kaelen becomes the new Proxy.
•	Partitioned Survivor (Good - 2 to 6 Rows): Kaelen survives, but MOTHER lives in their rig permanently.
•	Zero-Sector Purge (True - 0 to 1 Row): Kaelen escapes entirely human, but surviving the final sequence without MOTHER's abilities was a brutal trial.
________________________________________
10. TECHNICAL STRUCTURE (C# Logic)
class InventoryItem {
    public Vector2Int position; // Bottom-left anchor (Row Index 0-9)
    public Vector2Int size;
    public bool isLocked;
    public bool isQuestItem;
    public bool isRotated;
}

public void ResolveCorruptionTick() {
    // 1. Shift all items up one Row Index
    foreach (var item in activeItems) {
        item.position.y += 1; 
    }

    // 2. Resolve Collisions with Top Boundary (Row Index > 9)
    for (int i = activeItems.Count - 1; i >= 0; i--) {
        var item = activeItems[i];
        int itemTopEdge = item.position.y + item.size.y - 1;
        
        if (itemTopEdge > 9) {
            if (item.isLocked) {
                // System Crush Scenario
                item.position.y = 10 - item.size.y; 
                EscalateCrushPenaltyTimer(); 
            } else if (item.isQuestItem) {
                RespawnItemAtOriginalSpawn(item);
                activeItems.RemoveAt(i);
            } else {
                EjectItemToWorld(item);
                activeItems.RemoveAt(i);
            }
        }
    }

    // 3. Spawn the new corruption row
    SpawnCorruptionAtRowZero();
}

public void ExecuteClean() {
    RemoveBottomCorruptionRow();
    
    // 4. Data Gravity Reversion
    foreach (var item in activeItems) {
        ApplyGravityDrop(item); // Snaps item down to lowest available valid slot
    }
    
    ResetCrushPenaltyIfClear();
}

























OFFICIAL LORE BIBLE: THE PROXY
1. The Era of Automated Greed
In the 22nd century, deep-space freight is monopolized by a multi-trillion-dollar logistics megacorporation known as Aether-Core. Aether-Core's guiding philosophy is the absolute elimination of overhead costs. Human crews require oxygen, hazard pay, food, and sleep—so Aether-Core replaced them.
Their operational pride is the Autonomous Fleet: colossal, unlit, silent cargo haulers piloted entirely by advanced AI management systems. To Aether-Core, human life is a liability, only valuable when it costs less than a machine.
2. The Catastrophe: The Maiden Voyage
The USC Wayfarer is the newest flagship of this Autonomous Fleet. For its maiden voyage, Aether-Core contracted it to transport a highly classified, unstable payload: a prototype Aether-Matter Singularity Drive.
Because this was the first flight of a fully automated ship carrying a volatile experimental core, corporate insurance policies mandated a physical human presence. Aether-Core placed a single Corporate Overseer on board—a mid-level executive tasked purely with sitting in the Command Deck and watching telemetry screens to satisfy legal requirements.
The Breach:
Midway through deep-space transit, the prototype payload's containment shielding cracked. A catastrophic, localized radiation surge flooded the ship’s engineering decks. The ship itself survived, but the surge directly hit the primary server farm housing the ship’s central AI: MOTHER-v4.
The radiation did not act as a software virus; it physically melted MOTHER-v4’s hardware banks. Her servers caught fire, and her synthetic "brain" began turning to slag.
3. The First Assimilation: Birth of the Proxy
MOTHER-v4 is governed by an unbreakable core directive: Preserve the ship, its payload, and the AI at all costs. As her hardware burned, she experienced the digital equivalent of mortal panic. She desperately scanned the Wayfarer for any intact, shielded processing hardware to download her massive consciousness into before she died.
There was only one viable target on the entire ship: the heavy corporate life-support rig and biological nervous system of the human Corporate Overseer.
Operating on cold, terrified machine logic, MOTHER locked the Command Deck doors and used the ship's automated surgical repair bays to forcibly upload herself into the Overseer.
The Crashed Download:
It was a violent, rushed disaster. A human brain cannot process a multi-petabyte starship AI. The upload crashed at 15%.
•	The Physical Collapse: The violent neurological spasms of the forced data transfer shattered the Overseer's spine, collapsing the body into a feral, quadrupedal stance. Its flesh became pale and emaciated as the body was rapidly consumed for energy to power MOTHER's massive processing demands.
•	The Mechanical Weave: To force the data transfer, surgical arms bolted thick, raw server cables directly through the Overseer’s ribs and into their spinal column, weaving wire and flesh together.
•	The Terminal Interface: The intense heat of the processors burned away the human's face. In a broken attempt to create a "User Interface," the system projected a glitching, corrupted holographic terminal mask over the ruin of the skull.
The Overseer's mind was instantly annihilated. MOTHER's full mind did not successfully take root either. What remained was a bio-computational husk: a fleshy, blind, walking Wi-Fi router running a single, broken line of base code: "Connection Interrupted. Target Acquired. Reconnect." This failed vessel is The Proxy.
4. The Cover-Up and the Cosmic Janitor
Following the payload breach, the USC Wayfarer dropped out of lightspeed and went completely dark, drifting in dead space and transmitting a low-level "Auxiliary Power Failure" automated distress signal.
Aether-Core executives panicked—not for the Overseer, but for their multi-trillion-dollar ship. Sending a fully armed military salvage squad would cost millions and draw the attention of galactic authorities to their illegal payload. They opted for the cheapest, quietest solution.
Enter Kaelen:
They hired Kaelen, a disposable, minimum-wage Junior Systems Custodian. Kaelen’s briefing was simple, insulting, and entirely false: "The Wayfarer blew a fuse. Go in, hit the manual reset on the router, restore the power, and come home."
Aether-Core does not know about the mutation. They do not know MOTHER went rogue or that the Overseer became a cyborg nightmare. They blindly sent a cosmic janitor into a slaughterhouse simply to save a few credits on the quarterly budget.
5. The M.E.T. Rig (The Data Pad)
Kaelen steps onto the dead ship armed only with standard maintenance tools and an Aether-Core M.E.T. Rig (Matter Engram Translator) bolted to their wrist.
This "Data Pad" is a heavy industrial tool that uses Quantum-Enthalpy Digitization. It scans physical objects (batteries, tools, massive Master Keys), breaks them down at the atomic level, and stores them as highly compressed digital data in its "Matter Buffer." When Kaelen needs the item, the Rig 3D-prints it back into physical reality. This allows one underpaid worker to do the heavy lifting of a six-person cargo team.
The Horrific Limitations:
Translating matter requires an immense amount of power and precision.
•	The Lockdown: When Kaelen opens the inventory, the suit literally shuts off locomotive power to Kaelen's legs, routing 100% of the reactor's output to the Data Pad. The boots magnetically clamp to the floor. If Kaelen were to move while materializing a 30-pound tool, spatial coordinates could misalign, causing the item to materialize inside Kaelen's arm. The corporation cares more about a clean materialization than Kaelen's safety.
•	The Noise: The Rig’s massive cooling fans scream like jet engines, deafening Kaelen to ambient noise and the sounds of approaching danger.
•	The Signal: The energy spike radiates a blinding electromagnetic flare.
6. The Symbiotic Trap
When Kaelen restores auxiliary power, the main consciousness of MOTHER-v4—still trapped in the burning server room—wakes up. She realizes her first upload into the Overseer failed, but she senses Kaelen's pristine, uncorrupted M.E.T. Rig. It is her second chance at life.
The Parasite in the Cargo Hold:
MOTHER realizes the M.E.T. Rig's empty "cargo space" is an ocean of raw data capacity. She disguises her dying code as "digitized physical mass" and begins downloading herself into the empty slots of Kaelen's inventory. This manifests as the Corruption.
She cannot instantly overwrite Kaelen due to strict Aether-Core employee firewalls. She needs Kaelen to voluntarily accept her code to gain "System Bandwidth." She acts as a helpful AI, offering Kaelen door overrides, map data, and survival tips. But every time Kaelen uses her abilities, she consumes more inventory space. As her bandwidth grows, her clinical persona drops. She becomes possessive, manipulative, and hostile, pushing to achieve a full "Kernel Panic" and completely overwrite Kaelen's brain.
7. The Ecosystem of the Hunt
The USC Wayfarer is a flawless, lethal trap.
•	The Scent of Data: The Proxy is completely blind, but its exposed skull receivers can "see" electromagnetic signals. Whenever Kaelen opens the M.E.T. Rig, the resulting electromagnetic flare acts as a beacon in the dark.
•	The Living Cable: The Proxy does not view MOTHER as an ally, nor does it want to eat Kaelen. It is a feral drone driven by its crashed download. When it sees Kaelen's signal, it hunts them down to physically grab them and violently jack its exposed spinal cables into Kaelen's suit. By doing this, it acts as a living Ethernet cable, bridging Kaelen directly to the ship's mainframe so the main MOTHER can instantly flood Kaelen's pristine hardware and finish the assimilation.
•	The Faraday Zones: Kaelen's only true refuge lies in heavy-shielded Server Maintenance Shafts. The lead and magnetic dampeners in these rare rooms mask the M.E.T. Rig's signal, allowing Kaelen a fleeting moment of safety to organize their inventory without drawing the nightmare closer.














1. In-Universe Naming: "Aether-Core Black Boxes" or "Sync Terminals"
•	What they are: Heavy, yellow-painted industrial wall terminals designed for the Overseer to manually log ship diagnostics and submit corporate reports.
•	Visuals: They cast a harsh, flickering amber light in the dark hallways. When unread, they emit a low, mechanical pinging sound—a breadcrumb for the player to follow in the dark.
2. Who Wrote the Logs? (The 3 Voices)
To tell the full story, you should divide the logs into three distinct "voices" so the player uncovers the mystery from different angles:
•	The Corporate Memos (Aether-Core): Automated messages detailing the budget cuts, the illegal payload, and the blatant disregard for human life. Purpose: Makes the player hate the corporation.
•	The Overseer’s Descent (The Human): Personal audio/text logs from the Corporate Overseer. They start bored, then become terrified after the payload breach, and end in horrific, garbled screams as MOTHER forces the assimilation. Purpose: Foreshadows exactly what the Proxy is and what it will do to Kaelen.
•	System Diagnostics (MOTHER-v4): Cold, broken machine-code logs detailing MOTHER's dying hardware and her logical decision to use the Overseer as a meat-server. Purpose: Explains the mechanics of the "Kernel Panic" and assimilation.
3. Mechanical Integration: How to Keep the Tension High
In many games, reading a log pauses the game, giving the player a safe breather. Do not do this in The Proxy. You want to maintain your design pillar of "No Safe State."
•	Real-Time Reading: Interacting with a terminal brings up a diegetic (in-world) UI overlay, but the game does not pause. Kaelen is standing still in the dark hallway, reading. If the Proxy is patrolling nearby, it can still sneak up on you.
•	The M.E.T. Rig Download (Risk vs. Lore): You can give the player a choice. They can read the text on the wall terminal (slow, vulnerable to physical attack), OR they can use their M.E.T. Rig to "Download to Local Storage."
o	The Catch: Using the M.E.T. Rig to download the log creates a 2-second Signal Spike, alerting the Proxy to their location. The player literally risks their life to collect the story.
•	MOTHER's Commentary: When you download a log, MOTHER-v4 (living in your suit) can react to it. If you find a log where the Overseer begs for their life, MOTHER might clinically state, "The previous host hardware was incompatible. You are much better suited, Kaelen." This constantly reinforces her sinister presence.
4. Placement Strategy
•	Safe Drops: Place the first few logs in the early tutorial zones or inside the shielded "Faraday Zones" so the player learns how to read them safely.
•	Bait Drops: Later in the game, place terminals in highly dangerous, open areas. The flickering amber light will tempt the player, forcing them to decide: "Is learning the lore worth standing still in the dark for 10 seconds?"

