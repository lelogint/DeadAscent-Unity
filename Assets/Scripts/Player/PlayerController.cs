using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    [Header("Player")]

    [SerializeField] private Transform playerCollider;
    [SerializeField] private Transform cameraObject;
    [SerializeField] private Transform playerModel;
    [SerializeField] private Transform itemHolder;
    [SerializeField] private CinemachineCamera mainCamera;
    [SerializeField] private CinemachineCamera getItemCamera;
    [SerializeField] private Transform itemPos; // The holder for the get item cutscene
    [SerializeField] private Rigidbody rigidBody;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private GameObject swordMesh; // Sword mesh
    [SerializeField] private Collider[] swordColliders; // Colliders used in attacks
    [SerializeField] private LayerMask collisionLayer;

    public Dictionary<string, bool> obtainedItems = new Dictionary<string, bool>() // Booleans with string dictionary keys so we can access the boolean (item is enabled true or false) through the item name
    {
        { "Sword", false},
        { "Bash", false},
        { "Dash", true},
        { "WallJump", true},
        { "DoubleJump", false},
        { "SwordUpgrade", false},
        { "HeartCrystal", false },
        { "Key1", false },
        { "Key2", false },
    }; // Create a dictionary for items collected

    // Other scripts related to the player
    private UiHandler uiHandler;
    private SoundHandler playerSoundHandler;
    private EffectHandler effectHandler;
    public AnimationHandler animationHandler;

    private RaycastHit currentInteractable;
    private bool crouchHeld = false;
    private float maxHealth = 100f;
    private float health = 100f;
    private int slashNum = 1; // Slash direction, alternates (1,2)
    private int coinNum = 0;
    private bool shopNavigateDebounce = false;

    // Player constants;
    private const float rotSpeed = 15f;
    private const float accel = 27f;
    private const float decel = 30f;
    private const float maxSpeed = 9.5f;
    private const float jumpPower = 12f;
    private const float dashSpeed = 19.5f;
    private const float sprintSpeed = 18.5f;
    private const float gravity = 30f;
    private const float gravityMax = 35f;
    private const float wallJumpPower = 11.5f;

    private const float playerHeight = 1.25f; // For ground casting
    private string currentState = "Spawn"; // Our current state - state list: "Normal", "WallJump", "Bash", "GetItem", "Dead", "Spawn", "Shop", "Hit"

    private float soulCount = 0f;
    private float raycastDebounce = 0f;
    private float wallJumpTimer = 0f;
    private float attackStateTimer = 0f;
    private float dashGroundTimer = 0f;
    private float hitTimer = 0f;
    private float modelGroundOffset = 1.12f;
    private float deadTimer = 2f;

    private bool inputBufferAttack = false; // Buffer inputs, if slash is inputted gives some leeway for the input if too early
    private bool canDoubleJump = false; // Debounce for double jump
    private bool canDash = false; // Dash debounce
    private bool isGrounded = false;

    private Vector3 inputDir; // Input direction
    private Vector3 groundNormal; // Ground normal detected from raycasting
    private Vector3 plrVelocity = Vector3.zero; // Player velocity (axis are always local to player), goes as follows (x = left-right, y = up-down, z = forward-back)
    private GameObject instantiatedItemHeld;

    void Start()
    {
        (int money, Dictionary<string, bool> loadedItems, bool validLoad) = UserData.LoadData();
        if (validLoad == true)
        {
            coinNum = money;
            obtainedItems = loadedItems;
        }
        mainCamera.Priority = 100;
        getItemCamera.Priority = 0;
        rigidBody = GetComponent<Rigidbody>();
        effectHandler = GetComponent<EffectHandler>();
        uiHandler = GetComponent<UiHandler>();
        playerSoundHandler = GetComponent<SoundHandler>();
        animationHandler = GetComponent<AnimationHandler>();
        uiHandler.UpdateCoinCount(coinNum);
        rigidBody.freezeRotation = true;
        MountSwordToBack();
        effectHandler.VisibleUpgrades(obtainedItems); // Make all of our already obtained upgrades visible
        UpdateHealth();
        (int resIndex, int fpsCapIndex, bool isFullscreen, bool validSettings) = UserData.LoadSettings();
        if(validSettings == true)
        {
            uiHandler.ResolutionSettings(uiHandler.resolutionOptions[resIndex]);
            uiHandler.resolutionIndex = resIndex;
            uiHandler.FpsSettings(uiHandler.fpsOptions[fpsCapIndex]);
            uiHandler.fpsCurrentIndex = fpsCapIndex;
            uiHandler.FullscreenSettings(isFullscreen);
        }

        foreach (Transform child in itemHolder) // Loop through our collectable item holder
        {
            if (obtainedItems.ContainsKey(child.tag)) // Check if the child object's tag is the same as the name of a collectable item
            {
                if (obtainedItems[child.tag] == true) // Check if the player has that specific item
                {
                    Destroy(child.GetChild(0).gameObject); // Destroy the collectable if the player already has it
                }
            }
        }

        Application.quitting += OnQuit;
    }

    void OnQuit()
    {
        UserData.SaveUserSettings(uiHandler.resolutionIndex, uiHandler.fpsCurrentIndex, uiHandler.fullScreen);
        UserData.SaveUserData(coinNum, obtainedItems);
    }

    void Awake()
    {
        //Application.targetFrameRate = 60;
    }

    public void MountSwordToBack() // Place sword on player's back
    {
        effectHandler.MountSwordToBack(swordMesh);
    }

    public void DeathEffect()
    {
        playerSoundHandler.PlaySound("HitGround", 2f);
    }

    public void AnimationStepEvent() // Stepping sounds 
    {
        if (isGrounded == true)
        {
            playerSoundHandler.RandomSound("Step", 1, 4, 3f);
        }
    }

    public void UpdateHealth()
    {
        if (obtainedItems["HeartCrystal"] == true)
        {
            maxHealth = 150f;
            health = 150f;
            uiHandler.UpdateHealth(health, maxHealth);
        }
    }

    public void GotItemPauseGame() // Public method for pausing our animator on the cutscene part specifically when the player holds the item up
    {
        playerAnimator.speed = 0f;
    }

    private void GotItemExit() // Exit our get item cutscene, resume player and if the item has a cosmetic effect, enable it.
    {
        if (instantiatedItemHeld)
        {
            if (instantiatedItemHeld.tag == "Sword")
            {
                effectHandler.ShowSword(obtainedItems);
            }
            else if (instantiatedItemHeld.tag == "DoubleJump")
            {
                effectHandler.ShowWingBoots();
            }
            else if (instantiatedItemHeld.tag == "Bash")
            {
                effectHandler.ShowBash();
            }
            else if (instantiatedItemHeld.tag == "SwordUpgrade")
            {
                effectHandler.ShowSword(obtainedItems);
            }
            else if (instantiatedItemHeld.tag == "Key1" || instantiatedItemHeld.tag == "Key2")
            {
                UserData.SaveUserSettings(uiHandler.resolutionIndex, uiHandler.fpsCurrentIndex, uiHandler.fullScreen);
                SceneManager.LoadScene("HubWorld");
            }
            else if (instantiatedItemHeld.tag == "HeartCrystal")
            {
                UpdateHealth();
            }
            Destroy(instantiatedItemHeld); // Destroy the held item from the cutscene
        }
        uiHandler.UiItemClose();
        mainCamera.enabled = true;
        playerAnimator.speed = 1f;
        SetState("Normal");
    }

    public void HealthChange(float num, string type, Vector3 knockbackPos, float knockbackStrength = 13f) // A public method so enemy can have access to our health when dealing damage
    {
        if (currentState != "GetItem" && currentState != "Dead" && hitTimer <= 0f)
        {
            switch (type) // There is different ways for setting player health, we can directly set it or we can add a value to it by using "Set" or "Add" strings
            {
                case "Set":
                    health = math.clamp(num, 0, 100); // Clamp health when setting it to stop invalid input of setting health e.g., 20000 health shouldn't be possible
                    break;
                case "Add":
                    health = math.clamp(health += num, 0, 100); // Clamp health to stop it going out of boundaries
                    break;
            }
            uiHandler.HealthUi(health, maxHealth);
            if (health + num < health) // If player is taking damage specifically, not healing
            {
                SetState("Hit");
                knockbackPos.y = playerCollider.transform.position.y; // Put enemy hit position and player on same plane Y
                playerCollider.transform.rotation = Quaternion.LookRotation(knockbackPos - playerCollider.transform.position); // Rotate player to face the knockback dealt
                plrVelocity = new Vector3(0, 0, -knockbackStrength);
                animationHandler.RunAnims(-knockbackStrength);
                playerSoundHandler.RandomSound("Hit", 1, 2, 2f);
                effectHandler.PlayerHit();
                hitTimer = 3f; // Invincibility and lock control timer
            }
            if (health == 0) // Player dies
            {
                // uiHandler.OnDeath();
                SetState("Dead");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (obtainedItems.ContainsKey(other.tag) && currentState == "Normal") // Check if the tag is an ability as well with the same name
        {
            GetItemScene(other);
            obtainedItems[other.tag] = true; // Toggle the item with associated tag to true, meaning we have that item
            UserData.SaveUserData(coinNum, obtainedItems);
        }
        else if (other.CompareTag("Coin"))
        {
            coinNum++;
            playerSoundHandler.PlaySound("CoinGet", 0.5f);
            uiHandler.UpdateCoinCount(coinNum);
            effectHandler.CoinEffect(other.transform.position);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Goal"))
        {
            other.transform.Find("Chest").GetComponent<Animator>().SetTrigger("Open");
            SetState("Empty");
            plrVelocity = Vector3.zero;
            uiHandler.OnWin();
        }
        else if (other.CompareTag("Enemy"))
        {
            if (currentState == "Bash")
            {
                EnemyHandler enemyScript = other.GetComponent<EnemyHandler>(); // Get enemy script
                bool enemyHit = enemyScript.HealthChanger(-35, true, playerCollider.forward * 20f + Vector3.up * 3f, 1.75f); // Use enemy public method to give damage  
                if (enemyHit == true)
                {
                    SetState("Normal");
                    animationHandler.SetAnimation("Bash", "Base", 0);
                    animationHandler.SetAnimation("Bash", "Attack", 0);
                    playerSoundHandler.RandomSound("SlashHit", 1, 3, 2f);
                    effectHandler.BashHit();
                }
            }
        }
        else if (other.CompareTag("Level1"))
        {
            SceneManager.LoadScene("Level1");
            UserData.SaveUserSettings(uiHandler.resolutionIndex, uiHandler.fpsCurrentIndex, uiHandler.fullScreen);
        }
        else if (other.CompareTag("Level2"))
        {
            SceneManager.LoadScene("Level2");
            UserData.SaveUserSettings(uiHandler.resolutionIndex, uiHandler.fpsCurrentIndex, uiHandler.fullScreen);
        }
        else if (other.CompareTag("Hazard"))
        {
            HealthChange(-15f, "Add", other.transform.position, 8f);
        }
        else if (other.CompareTag("Button"))
        {
            other.transform.GetComponent<Animation>().Play();
            other.enabled = false;
            other.transform.GetComponent<PlayAnimation>().AnimPlay();
        }
    }

    private void GetItemScene(Collider other) // Handle the cutscene when we find an item
    {
        instantiatedItemHeld = effectHandler.InstantiateHeldItemModel(other.tag, itemPos); // Use the player effect script to instantiate the item with associated tag
        mainCamera.enabled = false; // Disable normal game camera
        uiHandler.UiItemOpen(other.tag);
        plrVelocity = Vector3.zero; // Remove all velocity
        Destroy(other.gameObject); // Destroy the collectable
        playerSoundHandler.PlaySound("GetItem", 2.5f);
        animationHandler.SetAnimation("GetItem", "Base", 0);
        animationHandler.SetAnimation("Empty", "Attack", 0);
        SetState("GetItem");
    }

    private void LeaveGround() // When the player leaves the ground - (grounded - airborne)
    {
        isGrounded = false;
        canDash = true;
        canDoubleJump = true;
    }

    private void LeaveAirborne() // When player lands on the ground - (airborne - grounded)
    {
        isGrounded = true;
        playerSoundHandler.RandomSound("Landed", 1, 2, 1f);
    }

    public void SetState(string to) // Set player's state
    {
        if (currentState == "Bash") // If our state is leaving the bash sprint state specifically
        {
            effectHandler.DashEffectDisable(); // Toggle off bash effects
        }

        currentState = to;
    }

    private void SetCharacterModelTransform() // Align Character Model with Collider plus a slight rotation for leaning effect 
    {
        float rotAmount;
        if (inputDir.magnitude > 0.1f)
        {
            rotAmount = math.clamp(Vector3.SignedAngle(inputDir, playerCollider.forward, Vector3.up) / 8f, -0.25f, 0.25f);
        }
        else
        {
            rotAmount = 0f;
        }
        playerModel.position = playerCollider.position - (playerCollider.up * modelGroundOffset);
        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, playerCollider.rotation * quaternion.Euler(0, 0, rotAmount), 30f * Time.deltaTime); // Make player lean into turn by adding rotation on local z axis, CFrame.Angles() = quaternion.Euler()) Logan (my) cross port Lua converted from = part.CFrame * CFrame.Angles(0,0,math.clamp(velocityOnX, clampLow, clampHigh))
    }

    public void OnSprint(InputAction.CallbackContext context) // Sprint action from InputActions binded controls
    {
        if (context.performed == true && uiHandler.IsMenuOpen() == false)
        {
            if (currentState == "Normal" && attackStateTimer == 0 && obtainedItems["Bash"] == true)
            {
                if (isGrounded == true)
                {
                    //    playerSoundHandler.RandomSound("Dash", 1, 2, 0.35f);
                    effectHandler.DashEffectEnable();
                    inputDir = playerCollider.transform.forward;
                    SetState("Bash");
                }
            }
        }
        else
        {
            if (currentState == "Bash")
            {
                SetState("Normal");
            }
        }
    }

    public void OnInteract(InputAction.CallbackContext context) // When we interact with NPCs or Shops
    {
        if (context.performed == true && uiHandler.IsMenuOpen() == false)
        {
            if (currentState == "Shop")
            {
                currentState = "Normal";
                uiHandler.ShopClose();
            }
            if (currentInteractable.transform != null && uiHandler.isShopOpen == false)
            {
                InteractType interactType = currentInteractable.transform.GetComponent<InteractType>();
                string uiType = interactType.type;
                if (isGrounded == true && uiType == "Shop")
                {
                    SetState("Shop");

                    uiHandler.ShopOpen();
                    plrVelocity = Vector3.zero;
                    animationHandler.RunAnims(0f);
                }else if(uiType == "Door")
                {
                    if (obtainedItems.ContainsKey(interactType.doorRequirement) == true)
                    {
                        if (obtainedItems[interactType.doorRequirement] == true)
                        {
                            interactType.doorAnimation.Play();
                        }
                    }
                }
            }
        }
    }

    public void OnDash(InputAction.CallbackContext context) // Dash action from InputActions binded controls 
    {
        if (context.performed == true && uiHandler.IsMenuOpen() == false)
        {
            if (currentState == "Normal" && attackStateTimer == 0 && obtainedItems["Dash"] == true) // Check if player is in normal state and can dash
            {
                if (canDash == true)
                {
                    canDash = false;
                    playerSoundHandler.RandomSound("Dash", 1, 2, 0.35f);
                    animationHandler.SetAnimation("AirDash", "Base", 0.025f);
                    effectHandler.DashEffectEmit();
                    plrVelocity = Vector3.zero;
                    plrVelocity.z = dashSpeed;
                    inputDir = playerCollider.transform.forward;
                    dashGroundTimer = 5f;
                    raycastDebounce = 0f;
                }
            }
        }
    }

    public void OnJump(InputAction.CallbackContext context) // Jump action from InputActions binded controls
    {
        if (context.performed == true && uiHandler.IsMenuOpen() == false)
        {
            if (currentState == "Normal") // If player is in normal state
            {
                if (isGrounded == true)
                {
                    LeaveGround();
                    if (plrVelocity.z > 15f)
                    {

                        plrVelocity += new Vector3(0, 0, 5.5f);
                        animationHandler.SetAnimation("DashJump", "Base", 0);
                    }
                    else
                    {
                        animationHandler.SetAnimation("Jump", "Base", 0);
                    }
                    plrVelocity = new Vector3(plrVelocity.x, jumpPower, plrVelocity.z);
                    raycastDebounce = 4f;
                    playerSoundHandler.PlaySound("Jump1", 1f);
                }
                else
                {
                    bool wallJumped = CheckWallJump();
                    if (wallJumped == false && obtainedItems["DoubleJump"] == true && canDoubleJump == true)
                    {
                        canDoubleJump = false;

                        playerSoundHandler.PlaySound("DoubleJump", 1f);
                        animationHandler.SetAnimation("DoubleJump", "Base", 0);
                        plrVelocity = new Vector3(plrVelocity.x, jumpPower, plrVelocity.z);
                        raycastDebounce = 5f;
                    }
                }
            }
            else if (currentState == "Bash") // If player is the bash sprint state
            {
                if (Input.GetButtonDown("Jump"))
                {
                    SetState("Normal");
                    LeaveGround();

                    raycastDebounce = 5f;
                    plrVelocity = new Vector3(plrVelocity.x, jumpPower, plrVelocity.z);
                    plrVelocity += new Vector3(0, 0, 5.5f);
                    animationHandler.SetAnimation("DashJump", "Base", 0);
                    playerSoundHandler.PlaySound("Jump1", 1f);
                }
            }
            else if (currentState == "GetItem") // If we are in the get item cutscene and the player presses jump, exit cutscene.
            {
                if (playerAnimator.speed == 0)
                {
                    GotItemExit();
                }
            }
            else if (currentState == "Shop")
            {
                int returnedMoney = uiHandler.ShopPurchase(coinNum);
                if (returnedMoney != coinNum)
                {
                    coinNum = returnedMoney;
                    playerSoundHandler.PlaySound("CoinGet", 1f);
                    uiHandler.UpdateCoinCount(coinNum);
                    UserData.SaveUserData(coinNum, obtainedItems);
                }
                else
                {
                    playerSoundHandler.PlaySound("UiBack", 1f);
                }
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context) // Attack action from InputActions binded controls
    {
        if (context.performed == true && uiHandler.IsMenuOpen() == false)
        {
            if (currentState == "Normal")
            {
                Attack(4f, 10f, false);
            }
        }
    }

    public void OnCrouch(InputAction.CallbackContext context) // Attack action from InputActions binded controls
    {
        if (context.performed == true && uiHandler.IsMenuOpen() == false)
        {
            if (currentState == "Normal")
            {
                crouchHeld = true;
                if (plrVelocity.z < 1)
                {

                }
            }
        }
        else
        {
            crouchHeld = false;
        }
    }

    private void Update() // Runs every frame
    {
        RaycastHit hit;
        float rayDist = playerHeight + 0.05f;
        rayDist = (isGrounded == true) ? rayDist + 0.5f : rayDist;
        bool raycastCollided = Physics.Raycast(playerCollider.position, Vector3.down, out hit, rayDist, collisionLayer); // Raycast at ground

        inputDir = Vector3.zero; // Reset input direction
        EssentialTimers();
        switch (currentState) // Handle player states
        {
            case "Normal":
                GetInputDirection();
                RotatePlayerToInput(rotSpeed * Time.deltaTime);
                if (inputBufferAttack == true && attackStateTimer < 1) // If attack is buffered and we are able to attack, do attack
                {
                    Attack(4f, 10f, true);
                    inputBufferAttack = false;
                }
                if (raycastCollided && raycastDebounce < 1) // If our raycast has hit the ground, we are not airborne anymore
                {
                    groundNormal = hit.normal;
                    if (isGrounded == false)
                    {
                        LeaveAirborne();
                    }
                    playerCollider.position = (hit.point) + ((Vector3.up) * (playerHeight - 0.1f)); // Offset player from ground ONLY if grounded
                    animationHandler.RunAnims(plrVelocity.z);
                    if (crouchHeld == true)
                    {

                    }
                }
                else
                {
                    groundNormal = Vector3.up;
                    if (animationHandler.currentAnimation["Base"] != "Jump" && animationHandler.currentAnimation["Base"] != "DoubleJump" && animationHandler.currentAnimation["Base"] != "DashJump" && animationHandler.currentAnimation["Base"] != "AirAttack1" && animationHandler.currentAnimation["Base"] != "AirAttack2" && animationHandler.currentAnimation["Base"] != "AirDash")
                    {
                        animationHandler.SetAnimation("Fall", "Base");
                    }
                    if (isGrounded == true) // If our raycast hasn't hit the ground, we are not grounded;
                    {
                        LeaveGround();
                    }
                }
                break;
            case "WallJump":
                wallJumpTimer = math.clamp(wallJumpTimer - (10f * Time.deltaTime), 0, 5);
                if (wallJumpTimer < 1) // If our wall jump state timer runs out, put player back into default state
                {
                    SetState("Normal");
                }
                break;
            case "Bash":
                GetInputDirection();
                RotatePlayerToInput((rotSpeed / 2.5f) * Time.deltaTime);
                if (raycastCollided && raycastDebounce < 1 && Vector3.Dot(inputDir, playerCollider.transform.forward) > -0.9f && inputDir.magnitude > 0.1f)
                {
                    groundNormal = hit.normal;
                    playerCollider.position = (hit.point) + ((Vector3.up) * (playerHeight - 0.1f)); // Offset player from ground
                    animationHandler.RunAnims(plrVelocity.z, true);
                }
                else
                {
                    SetState("Normal");
                }
                break;
            case "Hit":
                if (hitTimer > 1)
                {
                    if (raycastCollided && raycastDebounce < 1) // If our raycast has hit the ground, we are not airborne anymore
                    {
                        groundNormal = hit.normal;
                        playerCollider.position = (hit.point) + ((Vector3.up) * (playerHeight - 0.1f)); // Offset player from ground ONLY if grounded
                        animationHandler.RunAnims(plrVelocity.z);
                    }
                    else
                    {
                        groundNormal = Vector3.up;
                        if (animationHandler.currentAnimation["Base"] != "Jump" && animationHandler.currentAnimation["Base"] != "DoubleJump" && animationHandler.currentAnimation["Base"] != "DashJump" && animationHandler.currentAnimation["Base"] != "AirAttack1" && animationHandler.currentAnimation["Base"] != "AirAttack2" && animationHandler.currentAnimation["Base"] != "AirDash")
                        {
                            animationHandler.SetAnimation("Fall", "Base");
                        }
                    }
                }
                else
                {
                    SetState("Normal");
                }
                break;
            case "Dead":
                deadTimer -= Time.deltaTime;
                if(deadTimer < 0)
                {
                    int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
                    SceneManager.LoadScene(currentSceneIndex);
                }
                if (raycastCollided && raycastDebounce < 1) // If our raycast has hit the ground, we are not airborne anymore
                {
                    groundNormal = hit.normal;
                    playerCollider.position = (hit.point) + ((Vector3.up) * (playerHeight - 0.1f)); // Offset player from ground ONLY if grounded
                    if (animationHandler.currentAnimation["Base"] != "Dead")
                    {
                        animationHandler.SetAnimation("Dead", "Base", 0);
                        animationHandler.SetAnimation("Empty", "Attack", 0);
                    }
                }
                else
                {
                    groundNormal = Vector3.up;
                    if (animationHandler.currentAnimation["Base"] != "Jump" && animationHandler.currentAnimation["Base"] != "DoubleJump" && animationHandler.currentAnimation["Base"] != "DashJump" && animationHandler.currentAnimation["Base"] != "AirAttack1" && animationHandler.currentAnimation["Base"] != "AirAttack2" && animationHandler.currentAnimation["Base"] != "AirDash")
                    {
                        animationHandler.SetAnimation("Fall", "Base");
                    }
                }
                break;
            case "GetItem":
                itemPos.rotation = Quaternion.Slerp(itemPos.rotation, itemPos.rotation * quaternion.Euler(0, 0.1f, 0), 30f * Time.deltaTime);
                break;
            case "Shop":
                float[] inputAxis = Inputs();
                float inputX = inputAxis[0];
                if ((inputX >= 0.1f || inputX <= -0.1f)) // If player has moved after not moving for a frame (will be used for cycling shop menu)
                {
                    if (shopNavigateDebounce == false)
                    {
                        uiHandler.NavigateShop(inputX);
                        shopNavigateDebounce = true;
                    }
                }
                else
                {
                    shopNavigateDebounce = false;
                }
                break;
            default:
                break;
        }
        if (isGrounded == true && dashGroundTimer <= 0 && canDash == false)
        {
            canDash = true;
        }
        RaycastHit hitInteract;
        bool canInteract = Physics.Raycast(playerCollider.position - playerCollider.forward * 1.5f, playerCollider.forward, out hitInteract, 3f, LayerMask.GetMask("Interactable")); // Raycast at ground
        if (canInteract && currentState != "Shop")
        {
            // If the target changed
            if (currentInteractable.transform != hitInteract.transform)
            {
                // Hide old one (if any)
                if (currentInteractable.transform != null)
                {
                    Transform oldUI = currentInteractable.transform.Find("UiHolder");
                    if (oldUI != null) oldUI.gameObject.SetActive(false);
                }

                // Set new target
                currentInteractable = hitInteract;

                // Show new one
                Transform newUI = currentInteractable.transform.Find("UiHolder");
                if (newUI != null) newUI.gameObject.SetActive(true);
            }
        }
        // CASE 2: If we’re no longer looking at an interactable
        else
        {
            if (currentInteractable.transform != null)
            {
                Transform oldUI = currentInteractable.transform.Find("UiHolder");
                if (oldUI != null) oldUI.gameObject.SetActive(false);
                currentInteractable = new RaycastHit();
            }
        }
        SetCharacterModelTransform();
    }

    private void EssentialTimers() // Run timers that are essential and aren't entirely state related - so are ran outside of our state switch statement
    {
        if (raycastDebounce != 0)
        {
            raycastDebounce = math.clamp(raycastDebounce - (13f * Time.deltaTime), 0, 5); // Clamp at 0
        }
        if (dashGroundTimer != 0)
        {
            dashGroundTimer = math.clamp(dashGroundTimer - (13f * Time.deltaTime), 0, 5); // Clamp at 0
        }
        if (attackStateTimer != 0)
        {
            attackStateTimer = math.clamp(attackStateTimer - (10f * Time.deltaTime), 0, 5); // Clamp at 0
        }
        if (hitTimer != 0)
        {
            hitTimer = math.clamp(hitTimer - (10f * Time.deltaTime), 0, 5);
        }
    }

    private void FixedUpdate() // Used for controls
    {
        switch (currentState) // Switch statement handles all player states.
        {
            case "Normal":
                if (isGrounded == true)
                {
                    plrVelocity.z = PlayerMovement(plrVelocity.z, true);
                    plrVelocity.y = 0;
                }
                else if (isGrounded == false)
                {
                    plrVelocity.z = PlayerMovement(plrVelocity.z, true);
                    plrVelocity.y = math.clamp(plrVelocity.y - gravity * Time.fixedDeltaTime, -gravityMax, 50f);
                }
                break;
            case "WallJump":
                plrVelocity.z = PlayerMovement(plrVelocity.z, false, 15f);
                plrVelocity.y = math.clamp(plrVelocity.y - gravity * Time.fixedDeltaTime, -gravityMax, 50f);
                break;
            case "Bash":
                plrVelocity.z = sprintSpeed;
                plrVelocity.y = 0;
                if (plrVelocity.z < 6)
                {
                    SetState("Normal");
                }
                break;
            case "Hit":
                if (isGrounded == true)
                {
                    plrVelocity.z = PlayerMovement(plrVelocity.z, true);
                    plrVelocity.y = 0;
                }
                else if (isGrounded == false)
                {
                    plrVelocity.z = PlayerMovement(plrVelocity.z, true);
                    plrVelocity.y = math.clamp(plrVelocity.y - gravity * Time.fixedDeltaTime, -gravityMax, 50f);
                }
                break;
            case "Dead":
                if (isGrounded == true)
                {
                    plrVelocity.z = PlayerMovement(plrVelocity.z, true);
                    plrVelocity.y = 0;
                }
                else if (isGrounded == false)
                {
                    plrVelocity.z = PlayerMovement(plrVelocity.z, true);
                    plrVelocity.y = math.clamp(plrVelocity.y - gravity * Time.fixedDeltaTime, -gravityMax, 50f);
                }
                break;
            default:
                break;
        }
        plrVelocity.x = Decelerate(plrVelocity.x, decel * Time.fixedDeltaTime); // Decelerate velocity on horizontal x axis 
        Vector3 globalisedVelocity = playerCollider.TransformDirection(plrVelocity); // Globalise the velocity, vector3 from local space to world space
        rigidBody.linearVelocity = globalisedVelocity; // Set our linear velocity to allow movement via rigidbody
        rigidBody.angularVelocity = Vector3.zero; // Make sure player doesn't gain any rotational velocity
    }

    void Attack(float timer, float push, bool attackBuffered) // Attack input and input buffering with attacks.
    {
        if (obtainedItems["Sword"] == true)
        {
            if (attackStateTimer < 1)
            {
                SlashFunction(timer, push); // If attack doesn't need to be buffered to be used, attack straight away
            }
            else if (attackBuffered == false && attackStateTimer < 2.5f) // If our input buffer timer is high enough and the player hasn't already input buffered, buffer the attack input
            {
                inputBufferAttack = true;
            }
        }
    }
    private IEnumerator CheckHitboxCoroutine(Collider collider)
    {
        float checkInterval = 0.05f; // Interval between hitbox checks
        float checkDuration = 0.2f; // Total time checked in the hitbox e.g., attack forwards and if an enemy enters the hitbox .05 seconds after, it still hits.
        float timer = 0f; // Current timer

        while (timer < checkDuration) // Loop while not at full duration
        {
            Collider[] colliderOverlap = Physics.OverlapBox(collider.bounds.center, collider.bounds.extents, collider.transform.rotation, LayerMask.GetMask("Enemy")); //Find objects within bounds of hitbox
            bool slashSoundDebounce = false; // Prevent sound overlay - sound playing twice
            bool recoilSoundDebounce = false;
            foreach (Collider c in colliderOverlap) // Loop through parts in bounds
            {
                if (c.tag == "Hazard" || c.tag == "Enemy")
                {
                    timer = checkDuration;
                    if (collider == swordColliders[2] || collider == swordColliders[3]) // Check if the player is using either of the downward slash colliders so we can pogo slash
                    {
                        if (c.tag == "Enemy")
                        {
                            bool enemyHit;
                            float damage = (obtainedItems["SwordUpgrade"] == false) ? -25f : -25f * 1.25f;
                            EnemyHandler enemyScript = c.GetComponent<EnemyHandler>(); // Get enemy script
                            enemyHit = enemyScript.HealthChanger(damage, false, playerCollider.forward * 2.5f + playerCollider.up * -5); // Use enemy public method to give damage
                            if (enemyHit == true)
                            {
                                if (c.transform.parent.tag == "EnemyDoor")
                                {
                                    c.transform.parent.GetComponent<OpenGate>().CheckChildren();
                                }
                                if (slashSoundDebounce == false)
                                {
                                    playerSoundHandler.RandomSound("SlashHit", 1, 3, 0.75f);
                                    slashSoundDebounce = true;
                                }
                                soulCount = Mathf.Clamp(soulCount + 5, 0, 100);
                                plrVelocity = new Vector3(plrVelocity.x, jumpPower / 1.4f, (plrVelocity.z / 2.2f)); // Push player up!!! Pogo yippeeee
                                dashGroundTimer = 0;
                                canDash = true;
                                canDoubleJump = true;
                            }
                        }
                        else
                        {
                            if (recoilSoundDebounce == false)
                            {
                                playerSoundHandler.RandomSound("SwordRecoil", 1, 3, 0.87f);
                                recoilSoundDebounce = true;
                            }
                            plrVelocity = new Vector3(plrVelocity.x, jumpPower / 1.4f, (plrVelocity.z / 2.2f)); // Push player up!!! Pogo yippeeee
                            effectHandler.SwordRecoilSpark(2);
                            dashGroundTimer = 0;
                            canDash = true;
                            canDoubleJump = true;
                        }
                    }
                    else
                    {
                        if (c.tag == "Enemy")
                        {
                            bool enemyHit;
                            float damage = (obtainedItems["SwordUpgrade"] == false) ? -25f : -25f * 1.25f;
                            EnemyHandler enemyScript = c.GetComponent<EnemyHandler>(); // Get enemy script
                            enemyHit = enemyScript.HealthChanger(damage, false, playerCollider.forward * 4f); // Use enemy public method to give damage
                            if (enemyHit == true)
                            {
                                if (c.transform.parent.transform.parent.tag == "EnemyDoor")
                                {
                                    c.transform.parent.transform.parent.GetComponent<OpenGate>().CheckChildren();
                                }
                                if (slashSoundDebounce == false)
                                {
                                    playerSoundHandler.RandomSound("SlashHit", 1, 3, 0.75f);
                                    slashSoundDebounce = true;
                                }
                                soulCount = Mathf.Clamp(soulCount + 5, 0, 100);
                                plrVelocity = new Vector3(plrVelocity.x, plrVelocity.y, (plrVelocity.z / 2.2f));
                            }
                        }
                        else
                        {
                            if (recoilSoundDebounce == false)
                            {
                                playerSoundHandler.RandomSound("SwordRecoil", 1, 3, 0.87f);
                                recoilSoundDebounce = true;
                            }
                            plrVelocity = new Vector3(plrVelocity.x, plrVelocity.y, -8f);
                            effectHandler.SwordRecoilSpark(1);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(checkInterval); // Pause coroutine for set number - our while loop doesn't iterate instantly to prevent scanning hitbox all the time (more performant!!)
            timer += checkInterval;
        }
    }

    private void SlashFunction(float timer, float push) // Handles slash attacks
    {
        slashNum = (slashNum > 1) ? slashNum = 1 : slashNum = 2; // Set out slash count - if slash number one go to slash two
        slashNum = (crouchHeld == true && isGrounded == false) ? slashNum + 2 : slashNum;

        attackStateTimer = timer;
        effectHandler.InstantiateSlashEffect(slashNum, playerCollider);
        effectHandler.MountSwordToHand(swordMesh);
        StartCoroutine(CheckHitboxCoroutine(swordColliders[slashNum - 1]));

        if (slashNum == 1)
        {
            animationHandler.SetAnimation("Attack1", "Attack", 0.025f);
        }
        else if (slashNum == 2)
        {
            animationHandler.SetAnimation("Attack2", "Attack", 0.025f);
        }
        else if (slashNum == 3)
        {
            animationHandler.SetAnimation("AirAttack1", "Attack", 0.025f);
            //  animationHandler.SetAnimation("AirAttack2", "Base", 0.025f);
            slashNum = 1;
        }
        else if (slashNum == 4)
        {
            animationHandler.SetAnimation("AirAttack2", "Attack", 0.025f);
            //  animationHandler.SetAnimation("AirAttack1", "Base", 0.025f);
            slashNum = 2;
        }

        playerSoundHandler.SwingSounds(slashNum);
    }

    private bool CheckWallJump() // Check if player can walljump
    {
        if (obtainedItems["WallJump"] == true) // If player has obtained the walljump
        {
            RaycastHit wallHit;
            bool wallRaycastCollided = Physics.Raycast(playerCollider.position - new Vector3(0, 1, 0), playerCollider.forward, out wallHit, 1.2f, collisionLayer); // Raycast infront of player to detect wall
            if (wallRaycastCollided)
            {
                float dotProduct = Vector3.Dot(wallHit.normal, Vector3.up);
                float dotMove = Vector3.Dot(wallHit.normal, inputDir);
                if (dotProduct > -0.025f && dotProduct < 0.025f && dotMove <= -0.27f) // Check if walls dot product is close enough to 0 because 0 means the vectors are perpendicular - essentially checking how close wall is to 90 degrees
                {
                    plrVelocity = new Vector3(0, wallJumpPower, wallJumpPower / 1.3f);
                    Vector3 look = wallHit.normal;
                    look.y = 0;
                    playerSoundHandler.RandomSound("Landed", 1, 2, 1f);
                    //playerCollider.position = wallHit.point + wallHit.normal + new Vector3(0,1,0);
                    playerCollider.forward = look;
                    dashGroundTimer = 0;
                    canDash = true;
                    canDoubleJump = true;
                    animationHandler.SetAnimation("WallJump", "Base", 0);
                    SetState("WallJump");
                    wallJumpTimer = 4f;
                    return true;
                }
            }
        }
        return false;

    }

    private void GetInputDirection() // Localise player movement to camera
    {
        float[] returnedInputs = Inputs();
        float inputDirX = returnedInputs[0];
        float inputDirY = returnedInputs[1];
        Vector3 cameraLook = cameraObject.forward;
        Vector3 cameraRight = cameraObject.right;
        cameraLook.y = 0; // We don't want movement up y axis!
        cameraRight.y = 0;
        inputDir = (cameraLook * inputDirY) + (cameraRight * inputDirX); //Multiply camera local axis by our inputs to get the movedirection.
    }

    private void RotatePlayerToInput(float slerpSpeed) // Spherically interpolate player to the desired rotation
    {
        if (inputDir.magnitude > 0.1f)
        {
            playerCollider.forward = Vector3.Slerp(playerCollider.forward, inputDir.normalized, slerpSpeed);
        }
    }

    private float PlayerMovement(float velocityOnAxis, bool canMove, float adjustDecel = decel) // Handles both player acceleration on a singular axis and deceleration when nothing is pressed
    {
        if (inputDir.magnitude > 0.1f)
        {
            if (velocityOnAxis < maxSpeed && canMove == true)
            {
                velocityOnAxis += accel * Time.fixedDeltaTime;
            }
            else
            {
                velocityOnAxis = Decelerate(velocityOnAxis, adjustDecel * Time.deltaTime); // Decelerate if player can't move or speed is greater than the speed cap
            }
        }
        else
        {
            velocityOnAxis = Decelerate(velocityOnAxis, adjustDecel * Time.deltaTime); // If there is no input decelerate player
        }
        return velocityOnAxis; // Return the velocity from the specific axis that has been decelerated
    }

    float Decelerate(float velocityOnAxis, float decelRate) // Self explanatory - decelerates a specific vector passed as an argument like vector3.x
    {
        if (velocityOnAxis < 0) // if we are decelerating up to zero like velocity = -4, we want to add to get closer to 0.
        {
            float velocityMinusDecel = velocityOnAxis + decelRate;
            if (velocityMinusDecel > 0) // If velocity - deceleration is less than 0 we set it to zero to prevent decelerating past 0
            {
                return 0;
            }
            return velocityMinusDecel; // Otherwise decelerated velocity will be returned
        }
        else if (velocityOnAxis > 0) // if we are decelerating down to zero e.g., velocity = 4, we want to subtract to get closer to 0.
        {
            float velocityMinusDecel = velocityOnAxis - decelRate;
            if (velocityMinusDecel < 0) // If velocity + deceleration is greater than 0 we set it to zero to prevent decelerating past 0
            {
                return 0;
            }
            return velocityMinusDecel; // Otherwise, decelerated velocity will be returned
        }
        else
        {
            return velocityOnAxis;
        }
    }

    Vector3 MoveDirection(Vector3 direction) // Get our true move direction by projecting our movement vector onto ground/slope via the raycasted slope normal
    {
        Vector3 alignedToSlope = Vector3.ProjectOnPlane(direction, groundNormal);
        return alignedToSlope;
    }

    float[] Inputs() // Retrieve player's directional inputs
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float[] inputs = { horizontal, vertical };
        return inputs;
    }
}