using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExploBullet : MonoBehaviour
{
    [SerializeField] private GameObject shooter;
    [SerializeField] private float destroyTime = 2f;
    [SerializeField] private float damage = 15;
    [SerializeField] private float bulletForce = 50f;
    [SerializeField] PhotonView pv;
    [SerializeField] private float splashRange;

    private Rigidbody2D bulletRigidBody;

    private Transform firePoint;


    private void Start() {
        pv = GetComponent<PhotonView>();
        bulletRigidBody = this.GetComponent<Rigidbody2D>();
        bulletRigidBody.AddForce(this.transform.up * bulletForce, ForceMode2D.Impulse); //Adding force to the bullet, making it move
    }

    private void Awake() {
        StartCoroutine("DestroyByTime");
    }

    IEnumerator DestroyByTime() {
        yield return new WaitForSeconds(destroyTime);
        this.GetComponent<PhotonView>().RPC("destroyBullet", RpcTarget.AllBuffered);
    }

    //Collisions
    private void OnTriggerEnter2D(Collider2D collision) {
        bool hit = false;
        Box destroyable = collision.GetComponent<Box>();
        Barrel barrel = collision.GetComponent<Barrel>();
        Player playerBody = collision.GetComponent<Player>();
        Wall wall = collision.GetComponent<Wall>();

        if (destroyable != null) {
            destroyable.TakeDamage(damage);
            hit = true;
        }

        if (barrel != null) {
            barrel.TakeDamage(damage);
            hit = true;
        }

        if ((playerBody != null) && (!collision.gameObject.GetPhotonView().IsMine)) {
            playerBody.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.AllBuffered, damage);
            hit = true;
        }

        if (hit) {
            explode();
            StopCoroutine("DestroyByTime");
            this.GetComponent<PhotonView>().RPC("destroyBullet", RpcTarget.AllBuffered);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        //Wall wall = collision.gameObject.GetComponent<Wall>();
        if (collision.gameObject.tag == "wall") {
            explode();
            StopCoroutine("DestroyByTime");
            this.GetComponent<PhotonView>().RPC("destroyBullet", RpcTarget.AllBuffered);
        }
    }

    public void explode() {
        //Debug.Log("inside explode");
        var hitColliders = Physics2D.OverlapCircleAll(transform.position, splashRange);
        foreach (var hitCollider in hitColliders) {
            Player player = hitCollider.GetComponent<Player>();
            if (player) {
                player.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.AllBuffered, 100.00f);
                Debug.Log("Ammo exploded and dealt dmg to a player");
            }
        }
    }

    [PunRPC]
    public void destroyBullet() {
        Destroy(this.gameObject);
    }
}
