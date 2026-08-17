using UnityEngine;

public class SlimeAnimEvents : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public EnemyHandler enemyHandler;
    public Collider hitbox;
    
    public void Attack()
    {
        enemyHandler.Hitbox(hitbox);
    }
}
