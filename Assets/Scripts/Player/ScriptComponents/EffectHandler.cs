using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.VFX;

public class EffectHandler : MonoBehaviour
{
    [Header("Sword Related Effects")]
    // Sword slashes
    [SerializeField] private VisualEffect swordMeshParticle1;
    [SerializeField] private VisualEffect swordMeshParticle2;
    [SerializeField] private VisualEffect swordMeshParticle3;
    [SerializeField] private VisualEffect swordMeshParticle4;

    // Other
    [SerializeField] private ParticleSystem dashWind1;
    [SerializeField] private ParticleSystem dashWind2;
    [SerializeField] private ParticleSystem dashArc1;
    [SerializeField] private ParticleSystem dashArc2;
    [SerializeField] private ParticleSystem playerHit1;
    [SerializeField] private ParticleSystem playerHit2;
    [SerializeField] private ParticleSystem recoilSpark1;
    [SerializeField] private ParticleSystem recoilSpark2;

    [SerializeField] private Light skullFlameLight;
    [SerializeField] private ParticleSystem skullFlame;
    [SerializeField] private ParticleSystem skullFlame2;
    [SerializeField] private ParticleSystem getCoin;

    [SerializeField] private GameObject handSwordHolder;
    [SerializeField] private GameObject backSwordHolder;

    // Enable meshes associated with upgrades
    [SerializeField] private MeshRenderer swordVis;
    [SerializeField] private GameObject upgradeSwordVis;
    [SerializeField] private SkinnedMeshRenderer wingedBootsVis;
    [SerializeField] private SkinnedMeshRenderer bashShoulderVis;

    [SerializeField] private GameObject[] heldItemModels; //Array of gameObjects containing the models that appear during the "get item" cutscene
    [SerializeField] private Dictionary<string, GameObject> taggedHeldItems = new Dictionary<string, GameObject>() { }; // Create a dictionary with string table keys for the gameobjects

    void Start()
    {
        taggedHeldItems.Add("Sword", heldItemModels[0]);
        taggedHeldItems.Add("DoubleJump", heldItemModels[1]);
        taggedHeldItems.Add("WallJump", heldItemModels[2]);
        taggedHeldItems.Add("Bash", heldItemModels[3]);
        taggedHeldItems.Add("SwordUpgrade", heldItemModels[4]);
        taggedHeldItems.Add("Key1", heldItemModels[5]);
        taggedHeldItems.Add("Key2", heldItemModels[6]);
        taggedHeldItems.Add("HeartCrystal", heldItemModels[7]);
    }

    public void VisibleUpgrades(Dictionary<string, bool> obtainedItems)
    {
        ShowSword(obtainedItems);
        if (obtainedItems["Bash"] == true)
        {
            ShowBash(true);
        }
        else
        {
            ShowBash(false);
        }
        if (obtainedItems["DoubleJump"] == true)
        {
            ShowWingBoots(true);
        }
        else
        {
            ShowWingBoots(false);
        }
    }

    public void ShowSword(Dictionary<string, bool> obtainedItems) // Enable sword if player has the sword item and/or sword upgrade
    {
        if (obtainedItems["Sword"] == true)
        {
            if (obtainedItems["SwordUpgrade"] == true)
            {
                swordVis.enabled = false;
                upgradeSwordVis.SetActive(true);
            }
            else
            {
                swordVis.enabled = true;
                upgradeSwordVis.SetActive(false);
            }
        }
        else
        {
            swordVis.enabled = false;
            upgradeSwordVis.SetActive(false);
        }
    }

    public void CoinEffect(Vector3 coinPos)
    {
        getCoin.gameObject.transform.position = coinPos;
        getCoin.Emit(1);
    }

    public void HideSword() // Hide sword from being equipped
    {
        swordVis.enabled = false;
    }
    public void ShowBash(bool choice = true)
    {
        bashShoulderVis.enabled = choice;
    }
    public void ShowWingBoots(bool choice = true) // Show the winged boots on player
    {
        wingedBootsVis.enabled = choice;
    }
    public void MountSwordToBack(GameObject swordMesh) // Sheath the sword on player's back
    {
        swordMesh.transform.parent = backSwordHolder.transform.parent;
        swordMesh.transform.position = backSwordHolder.transform.position;
        swordMesh.transform.rotation = backSwordHolder.transform.rotation;
    }

    public void MountSwordToHand(GameObject swordMesh) // Parent sword to player's hand
    {
        swordMesh.transform.parent = handSwordHolder.transform.parent;
        swordMesh.transform.position = handSwordHolder.transform.position;
        swordMesh.transform.rotation = handSwordHolder.transform.rotation;
    }

    public bool IsSwordOnBack(GameObject swordMesh)
    {
        bool onBack = (swordMesh.transform.parent == backSwordHolder.transform) ? true : false;
        return onBack;
    }

    public void InstantiateSlashEffect(int slashNum, Transform playerCollider) // Instantiate the slash effect, different direction depending on our slash number
    {
        if (slashNum == 1)
        {
            VisualEffect newSlashEffect = Instantiate(swordMeshParticle1, playerCollider.transform);
            newSlashEffect.Play();
            Destroy(newSlashEffect.gameObject, 0.9f);
        }
        else if (slashNum == 2)
        {
            VisualEffect newSlashEffect = Instantiate(swordMeshParticle2, playerCollider.transform);
            newSlashEffect.Play();
            Destroy(newSlashEffect.gameObject, 0.9f);
        }
        else if (slashNum == 3)
        {
            VisualEffect newSlashEffect = Instantiate(swordMeshParticle4, playerCollider.transform);
            newSlashEffect.Play();
            Destroy(newSlashEffect.gameObject, 0.9f);
        }
        else if (slashNum == 4)
        {
            VisualEffect newSlashEffect = Instantiate(swordMeshParticle3, playerCollider.transform);
            newSlashEffect.Play();
            Destroy(newSlashEffect.gameObject, 0.9f);
        }
    }

    public GameObject InstantiateHeldItemModel(string tag, Transform itemPos) // Instantiate the held item during the "get item" cutscene
    {
        GameObject newModel = Instantiate(taggedHeldItems[tag], itemPos);
        newModel.transform.parent = itemPos;
        return newModel;
    }

    public void SoulEffectEnable()
    {
        skullFlame.Play();
        skullFlame2.Play();
        skullFlameLight.enabled = true;
    }

    public void SoulEffectDisable()
    {
        skullFlame.Stop();
        skullFlame2.Stop();
        skullFlameLight.enabled = false;
    }

    public void DashEffectEnable() // Enable our bash sprint effect
    {
        dashWind1.Play();
        dashWind2.Play();
        //   dashArc1.Play();
        //  dashArc2.Play();
    }

    public void BashHit()
    {
        dashArc1.Emit(4);
        dashArc2.Emit(4);
    }

    public void PlayerHit()
    {
        playerHit1.Emit(1);
        playerHit2.Emit(1);
    }

    public void SwordRecoilSpark(int type = 1)
    {
        if (type == 1)
        {
            recoilSpark1.Emit(8);
        }
        else
        {
            recoilSpark2.Emit(8);
        }
    }
    public void DashEffectEmit() // Emit the dash effect
    {
        //    dashWind2.Emit(2);
        //    skullFlame.Emit(3);
        dashWind1.Emit(3);
        dashWind2.Emit(3);
    }

    public void DashEffectDisable() // Disable our bash sprint effect
    {
        dashWind1.Stop();
        dashWind2.Stop();
        //   dashArc1.Stop();
        //   dashArc2.Stop();
    }
}
