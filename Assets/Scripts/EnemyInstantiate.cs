using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
[System.Serializable]
public class HostileEntity // I would have called this "Enemy" but needs to be inclusive of bosses lol
{
    public Transform entityParentPath; // E.g., "Default" folder under enemies object in a scene
    public GameObject entityModel; // The model we need to instantiate which will be a prefab

    public HostileEntity(Transform entityParentPath, GameObject entityModel)
    {
        this.entityParentPath = entityParentPath; 
        this.entityModel = entityModel; 
    }
}

public class EnemyInstantiate : MonoBehaviour
{
    [SerializeField] private Transform defaultEnemyParentPath;
    [SerializeField] private GameObject defaultEnemyModel;
    [SerializeField] private Camera playerCamera; // We need this for any UI elements attached to enemies later when we instantiate them

    private HostileEntity[] enemyTypes;

    void Start()
    {
        HostileEntity[] enemyTypes = { new HostileEntity(defaultEnemyParentPath, defaultEnemyModel)};
        for (int i = 0; i < enemyTypes.Length; i++) // Will not iterate much because there shouldn't be too many enemy types per scene
        {
            Transform enemyHolder = enemyTypes[i].entityParentPath;
            GameObject enemyModel = enemyTypes[i].entityModel;
            for (int i2 = 0; i2 < enemyHolder.transform.childCount; i2++) // Find folder designated to that enemy type, also (i2) == index 2 because I can't use int "i" again lol
            {
                Transform enemyContainer = enemyHolder.transform.GetChild(i2);

                Transform[] enemyWaypoints = { enemyContainer.GetChild(0), enemyContainer.GetChild(1), enemyContainer.GetChild(2), enemyContainer.GetChild(3) };
                GameObject instantiatedEnemy = Instantiate(enemyModel);
                instantiatedEnemy.transform.parent = enemyContainer;
                instantiatedEnemy.transform.localPosition = Vector3.zero;
                EnemyHandler enemyHandler = instantiatedEnemy.GetComponent<EnemyHandler>();
                enemyHandler.waypoints = enemyWaypoints;
                UiLookAtCamera uiLookAtCamera = instantiatedEnemy.GetComponentInChildren<UiLookAtCamera>();
                uiLookAtCamera.cameraObj = playerCamera;
            }
        }
    }

    void SpawnEnemies()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
