using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 50;
    [SerializeField] PhotonView view;
    [SerializeField] TMP_Text playerName;

    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private GameObject playerCamera;

    [SerializeField] private int health = 1000;
    private GameObject weapon;
    private GameObject head;
    private Collider2D playerCollider;
    //private GameObject AK;
    //private GameObject pistol;
    private GameObject firePoint;

    private GameObject sceneCamera;

    public Vector2 inputPosition;
    public Vector2 mousePosition;


    // Start is called before the first frame update
    void Start()
    {
        view = GetComponent<PhotonView>();
        if (view.IsMine) {
            sceneCamera = GameObject.Find("Main Camera");
            weapon = GameObject.Find("Weapon");
            head = GameObject.Find("Head");
            //AK = GameObject.Find("AK");
            //pistol = GameObject.Find("Pistol");
            //pistol.SetActive(false);
            firePoint = GameObject.Find("FirePoint");

            sceneCamera.SetActive(false);
            playerCamera.SetActive(true);
        }
        Debug.Log(view.Owner.NickName);
        playerName.text = view.Owner.NickName;
    }

    private void Update()
    {
        inputPosition.x = Input.GetAxis("Horizontal");
        inputPosition.y = Input.GetAxis("Vertical");

        Camera playerCam = playerCamera.GetComponent<Camera>();
        mousePosition = playerCam.ScreenToWorldPoint(Input.mousePosition); //Getting the coordinates of mouse cursor as world's point

        if(head != null) {
            headPosition.x = head.transform.position.x;
            headPosition.y = head.transform.position.y;
        }
        if(firePoint != null) {
            firePointPosition.x = firePoint.transform.position.x;
            firePointPosition.y = firePoint.transform.position.y;
        }
        if ((head != null) && (firePoint != null))
        {
            firePointHeadDistance = Vector2.Distance(headPosition, firePointPosition);
            mouseHeadDistance = Vector2.Distance(headPosition, mousePosition);
        /*if (pickUpAllowed && Input.GetKeyDown(KeyCode.E)) {
            Debug.Log(weaponIcon.name);
            if(weaponIcon.name.Contains("smallPistol")) {
                Debug.Log("Pisztolet znaleziony");
                AK.SetActive(false);
                pistol.SetActive(true);
            } else if(weaponIcon.name.Contains("smallAK")) {
                AK.SetActive(true);
                pistol.SetActive(false);
                Debug.Log("Akacz znaleziony");
            }
            Destroy(weaponIcon);*/
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (view.IsMine)
        {
            //Character movement
            playerRigidbody.MovePosition(playerRigidbody.position + inputPosition * speed * Time.fixedDeltaTime);

            //Character rotation
            float lookDirX = 0.0f;
            float lookDirY = 0.0f;

            //Precise aiming rotation (long range)
            if ((mouseHeadDistance-4.0f) > (firePointHeadDistance))
            {
                Debug.Log("Firepoint");
                lookDirX = mousePosition.x - firePoint.transform.position.x;
                lookDirY = mousePosition.y - firePoint.transform.position.y;
                float currentAngle = playerRigidbody.rotation;
                float angle = Mathf.Atan2(lookDirY, lookDirX) * Mathf.Rad2Deg - 90; //95 degrees - offset, which should be changed after creating final player model
                head.transform.rotation = Quaternion.Euler(0, 0, angle); //Rotation of the weapon, it should point to the local cursor

                //More elaborate way to smoothe the angle is written below. Should be used at a later time

                /*float angleDiff = angle - currentAngle;
                angleDiff = Mathf.Repeat(angleDiff + 180f, 360f) - 180f;
                angle = currentAngle + angleDiff;
                float smoothedAngle = Mathf.Lerp(currentAngle, angle, 0.2f);*/
            }
            //From hibs aiming rotation (close range)
            else if(mouseHeadDistance != firePointHeadDistance)
            {
                lookDirX = mousePosition.x - head.transform.position.x;
                lookDirY = mousePosition.y - head.transform.position.y;
                float currentAngle = playerRigidbody.rotation;
                float angle = Mathf.Atan2(lookDirY, lookDirX) * Mathf.Rad2Deg - 85; //87 degrees - offset, which should be changed after creating final player model
                head.transform.rotation = Quaternion.Euler(0, 0, angle); //Rotation of the weapon, it should point to the local cursor

                //More elaborate way to smoothe the angle is written below. Should be used at a later time

                /*float angleDiff = angle - currentAngle;
                angleDiff = Mathf.Repeat(angleDiff + 180f, 360f) - 180f;
                angle = currentAngle + angleDiff;
                float smoothedAngle = Mathf.Lerp(currentAngle, angle, 0.2f);*/
            }

        }
    }

    [PunRPC]
    public void TakeDamage(int damage)
    {
        health -= damage;

        /*if (health <= 0)
        {
            float lookDirX = mousePosition.x - AK.transform.position.x;
            float lookDirY = mousePosition.y - AK.transform.position.y;
            float currentAngle = playerRigidbody.rotation;
            float angle = Mathf.Atan2(lookDirY, lookDirX) * Mathf.Rad2Deg - 95f; //95 degrees - offset, which should be changed after creating final player model

            if (AK.activeInHierarchy) {
                AK.transform.rotation = Quaternion.Euler(0, 0, angle); //Rotation of the ak, it should point to the local cursor
            } else if (pistol.activeInHierarchy) {
                pistol.transform.rotation = Quaternion.Euler(0, 0, angle); //Rotation of the pistol, it should point to the local cursor
            }

            //More elaborate way to smoothe the angle is written below. Should be used at a later time

            /*float angleDiff = angle - currentAngle;
            angleDiff = Mathf.Repeat(angleDiff + 180f, 360f) - 180f;
            angle = currentAngle + angleDiff;
            float smoothedAngle = Mathf.Lerp(currentAngle, angle, 0.2f);*/
        //}
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.tag.Equals("WeaponIcon")) {
            pickUpAllowed = true;
            weaponIcon = collision.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.tag.Equals("WeaponIcon")) {
            pickUpAllowed = false;
        }
    }
}
