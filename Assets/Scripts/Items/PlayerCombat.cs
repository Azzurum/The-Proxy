using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Manages player aiming, weapon cooldowns, and firing logic for equipped tools.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("Audio SFX")]
    [Tooltip("Audio source for playing combat sounds.")]
    public AudioSource audioSource;
    [Tooltip("Sound played when the Arc-Pulse Stunner is fired.")]
    public AudioClip sfxStunnerFire;
    [Tooltip("Sound played when the K-80 Repulsor is fired.")]
    public AudioClip sfxRepulsorFire;
    [Tooltip("Sound played when attempting to fire an empty weapon.")]
    public AudioClip sfxWeaponEmpty;
    [Tooltip("Sound played when a shot successfully connects with an enemy.")]
    public AudioClip sfxEnemyHit;

    private float _repulsorCooldown = 0f;
    private float _stunnerCooldown = 0f;
    private Texture2D _generatedCrosshair;
    private MetRigManager _metRigManager;
    
    private readonly RaycastHit2D[] _raycastHits = new RaycastHit2D[5];
    private ContactFilter2D _enemyFilter;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        _metRigManager = FindAnyObjectByType<MetRigManager>();
        
        _enemyFilter = ContactFilter2D.noFilter;
        _enemyFilter.useTriggers = true;

        CreateCrosshair();
    }

    private void CreateCrosshair()
    {
        _generatedCrosshair = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color transparent = new Color(0, 0, 0, 0);
        for (int i = 0; i < 32; i++)
            for (int j = 0; j < 32; j++)
                _generatedCrosshair.SetPixel(i, j, transparent);

        Color red = Color.red;
        for (int i = 10; i < 22; i++)
        {
            _generatedCrosshair.SetPixel(16, i, red);
            _generatedCrosshair.SetPixel(15, i, red);
            _generatedCrosshair.SetPixel(17, i, red);

            _generatedCrosshair.SetPixel(i, 16, red);
            _generatedCrosshair.SetPixel(i, 15, red);
            _generatedCrosshair.SetPixel(i, 17, red);
        }
        _generatedCrosshair.Apply();
    }

    private void Update()
    {
        if (_repulsorCooldown > 0) _repulsorCooldown -= Time.deltaTime;
        if (_stunnerCooldown > 0) _stunnerCooldown -= Time.deltaTime;

        bool isRigOpen = _metRigManager != null && _metRigManager.isRigOpen;

        if (isRigOpen) Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        else UpdateCursor();

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (isRigOpen) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

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
            Cursor.SetCursor(_generatedCrosshair, new Vector2(16, 16), CursorMode.Auto);
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
        
        if (activeItem == null) return; 

        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aimDirection = (mouseWorldPosition - (Vector2)transform.position).normalized;
        
        float range = activeItem.itemID == "WEP-REPULSE" ? 3f : 5f;

        Collider2D hitCollider = null;
        bool hitProxy = false;

        if (activeItem.itemID == "STUN-ARC" || activeItem.itemID == "WEP-REPULSE")
        {
            int hitCount = Physics2D.Raycast(transform.position, aimDirection, _enemyFilter, _raycastHits, range);
            for (int i = 0; i < hitCount; i++)
            {
                if (_raycastHits[i].collider.TryGetComponent<ProxyAI>(out var proxy))
                {
                    hitCollider = _raycastHits[i].collider;
                    hitProxy = true;
                    break;
                }
            }

            if (!hitProxy)
            {
                hitCount = Physics2D.CircleCast(transform.position, 1.5f, aimDirection, _enemyFilter, _raycastHits, range);
                for (int i = 0; i < hitCount; i++)
                {
                    if (_raycastHits[i].collider.TryGetComponent<ProxyAI>(out var proxy))
                    {
                        hitCollider = _raycastHits[i].collider;
                        hitProxy = true;
                        break;
                    }
                }
            }
        }

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
        if (_stunnerCooldown > 0f) return;

        if (InventoryManager.Instance != null && InventoryManager.Instance.TryConsumeBatteries(1))
        {
            if (audioSource != null) audioSource.PlayOneShot(sfxStunnerFire != null ? sfxStunnerFire : ProceduralAudioGen.GeneratePew());

            _stunnerCooldown = 2.0f; 
            
            if (hitProxy && hitCollider.TryGetComponent<ProxyAI>(out var proxy))
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxEnemyHit != null ? sfxEnemyHit : ProceduralAudioGen.GenerateStaticGlitch(0.15f));
                proxy.ApplyStun();
            }
        }
        else
        {
            if (audioSource != null) audioSource.PlayOneShot(sfxWeaponEmpty != null ? sfxWeaponEmpty : ProceduralAudioGen.GenerateClick());
        }
    }

    private void ExecuteRepulsor(bool hitProxy, Collider2D hitCollider)
    {
        if (_repulsorCooldown <= 0f)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.TryConsumeBatteries(1))
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxRepulsorFire != null ? sfxRepulsorFire : ProceduralAudioGen.GeneratePneumaticBlast());

                _repulsorCooldown = 10f; 

                if (hitProxy && hitCollider.TryGetComponent<ProxyAI>(out var proxy))
                {
                    if (audioSource != null) audioSource.PlayOneShot(sfxEnemyHit != null ? sfxEnemyHit : ProceduralAudioGen.GenerateStaticGlitch(0.15f));
                    proxy.ApplyRepulsor(transform.position, 7f);
                }
            }
            else
            {
                if (audioSource != null) audioSource.PlayOneShot(sfxWeaponEmpty != null ? sfxWeaponEmpty : ProceduralAudioGen.GenerateClick());
            }
        }
        else
        {
            if (audioSource != null) audioSource.PlayOneShot(sfxWeaponEmpty != null ? sfxWeaponEmpty : ProceduralAudioGen.GenerateClick());
        }
    }
}