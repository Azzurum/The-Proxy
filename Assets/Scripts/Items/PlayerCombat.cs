using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public InventoryManager inventoryManager;

    private float repulsorCooldown = 0f;
    private Texture2D generatedCrosshair;

    void Start()
    {
        if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
        CreateCrosshair();
    }

    private void CreateCrosshair()
    {
        generatedCrosshair = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int i = 0; i < 32; i++)
            for (int j = 0; j < 32; j++)
                generatedCrosshair.SetPixel(i, j, transparent);

        Color red = Color.red;
        for (int i = 10; i < 22; i++)
        {
            generatedCrosshair.SetPixel(16, i, red);
            generatedCrosshair.SetPixel(15, i, red);
            generatedCrosshair.SetPixel(17, i, red);

            generatedCrosshair.SetPixel(i, 16, red);
            generatedCrosshair.SetPixel(i, 15, red);
            generatedCrosshair.SetPixel(i, 17, red);
        }
        generatedCrosshair.Apply();
    }

    void Update()
    {
        // Manage passive cooldowns
        if (repulsorCooldown > 0) repulsorCooldown -= Time.deltaTime;

        UpdateCursor();

        // 2. Aim and Fire (Left or Right Click)
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            MetRigManager rigManager = FindAnyObjectByType<MetRigManager>();
            if (rigManager != null && rigManager.isRigOpen) return;

            FireActiveWeapon();
        }
    }

    private void UpdateCursor()
    {
        if (HotbarManager.Instance == null || HotbarManager.Instance.currentEquippedIndex == -1)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        var quickSlot = HotbarManager.Instance.quickSlots[HotbarManager.Instance.currentEquippedIndex];
        ItemData activeItem = (quickSlot != null && quickSlot.containedItem != null) ? quickSlot.containedItem.itemData : null;

        bool isWeapon = activeItem != null && (activeItem.itemID == "STUN-ARC" || activeItem.itemID == "WEP-REPULSE");

        if (isWeapon)
        {
            Cursor.SetCursor(generatedCrosshair, new Vector2(16, 16), CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private void FireActiveWeapon()
    {
        if (HotbarManager.Instance == null || HotbarManager.Instance.currentEquippedIndex == -1) return;

        var quickSlot = HotbarManager.Instance.quickSlots[HotbarManager.Instance.currentEquippedIndex];
        ItemData activeItem = (quickSlot != null && quickSlot.containedItem != null) ? quickSlot.containedItem.itemData : null;
        
        if (activeItem == null) return; // Nothing in hands

        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDirection = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        float stunRange = 5f;
        float repulseRange = 3f;
        float range = activeItem.itemID == "WEP-REPULSE" ? repulseRange : stunRange;

        Collider2D hitCollider = null;
        bool hitProxy = false;

        // Weapon Aim Logic
        if (activeItem.itemID == "STUN-ARC" || activeItem.itemID == "WEP-REPULSE")
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, aimDirection, range);
            foreach (var hit in hits)
            {
                if (hit.collider.CompareTag("Proxy"))
                {
                    hitCollider = hit.collider;
                    hitProxy = true;
                    break;
                }
            }

            // Fallback: generous circle cast for easier aiming
            if (!hitProxy)
            {
                hits = Physics2D.CircleCastAll(transform.position, 1.5f, aimDirection, range);
                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Proxy"))
                    {
                        hitCollider = hit.collider;
                        hitProxy = true;
                        break;
                    }
                }
            }
            
            Debug.Log($"<color=yellow>[DEBUG]</color> Firing {activeItem.itemID}. Aim Dir: {aimDirection}, Hit Proxy: {hitProxy}");
        }

        // Execute the specific weapon logic
        if (activeItem.itemID == "STUN-ARC")
        {
            ExecuteStunner(hitProxy, hitCollider);
        }
        else if (activeItem.itemID == "WEP-REPULSE")
        {
            ExecuteRepulsor(hitProxy, hitCollider);
        }
    }

    private void ExecuteStunner(bool hitProxy, Collider2D hitCollider)
    {
        if (inventoryManager.TryConsumeBatteries(1))
        {
            if (hitProxy)
            {
                Debug.Log("<color=cyan>ARC-PULSE HIT:</color> The Proxy is stunned!");
                hitCollider.GetComponent<ProxyAI>().ApplyStun();
            }
            else
            {
                Debug.Log("<color=red>ARC-PULSE MISS:</color> You fired at the wall and wasted a battery!");
            }
        }
        else
        {
            Debug.Log("STUNNER CLICK: Empty! No Aether-Core Batteries in M.E.T. Buffer.");
        }
    }

    private void ExecuteRepulsor(bool hitProxy, Collider2D hitCollider)
    {
        if (repulsorCooldown <= 0f)
        {
            repulsorCooldown = 10f; // Reset pneumatics

            if (hitProxy)
            {
                Debug.Log("<color=cyan>REPULSOR HIT:</color> The Proxy is knocked back!");
                hitCollider.GetComponent<ProxyAI>().ApplyRepulsor(transform.position, 7f);
            }
            else
            {
                Debug.Log("<color=red>REPULSOR MISS:</color> You punched the air!");
            }
        }
        else
        {
            Debug.Log($"REPULSOR JAMMED: Pneumatics recharging... ({repulsorCooldown:F1}s)");
        }
    }
}