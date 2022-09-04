using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
using TMPro;

public class Player : MonoBehaviour
{
    [SerializeField] private float health = 1000;
    private float maxHealth;
    [SerializeField] Image healthbarImage;
    [SerializeField] GameObject ui;

    [SerializeField] private float speed = 50;
    [SerializeField] PhotonView view;
    [SerializeField] TMP_Text playerName;
    [SerializeField] TMP_Text ammoCountText1;
    [SerializeField] TMP_Text ammoCountText2;
    [SerializeField] TMP_Text ammoCountText3;

    [SerializeField] private Rigidbody2D playerRigidbody;
    [SerializeField] private GameObject playerCamera;

    [SerializeField] private GameObject akSymbolPrefab;
    [SerializeField] private GameObject pistolSymbolPrefab;
    [SerializeField] private GameObject shotgunSymbolPrefab;
    [SerializeField] private GameObject ak; // !!! Don't change, it has to be initialized by SerializeField !!!
    [SerializeField] private GameObject pistol; // !!! Don't change, it has to be initialized by SerializeField !!!
    [SerializeField] private GameObject shotgun; // !!! Don't change, it has to be initialized by SerializeField !!!
    private uint ammoCountNormal = 30;
    private uint ammoCountBouncy = 0;
    private uint ammoCountExplo = 0;
    private GameObject weaponSymbol;
    private GameObject ammoSymbol;
    private GameObject firePoint;
    private GameObject akFirePoint; 
    private GameObject pistolFirePoint;
    private GameObject head;
    private GameObject shotgunFirePoint;

    private GameObject sceneCamera;

    private Vector2 inputPosition;
    private Vector2 mousePosition;
    private Vector2 headPosition;
    private Vector2 firePointPosition;
    private float firePointHeadDistance;
    private float mouseHeadDistance;

    private int isHoldingAk = 0; //0 - holdingAk, 1 - holdingPistol, 2 - holdingShotgun
    private bool pickUpAllowed = false;

    //////////////////////////////// Shooting ////////////////////////////////
    
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject bouncyBulletPrefab;
    [SerializeField] private GameObject exploBulletPrefab;
    [SerializeField] static private float shotCooldown = 0.5f;
    float timeStamp = 0;
    [SerializeField] private int akMagazineSize = 30;
    [SerializeField] private int pistolMagazineSize = 12;
    [SerializeField] private int shotgunMagazineSize = 2;
    [SerializeField] private float reloadAkTime = 16f;
    [SerializeField] private float reloadPistolTime = 8f;
    [SerializeField] private float reloadShotgunTime = 12f;
    private int bulletsInWeaponMagazine;
    private bool inReload = false;

    /// UI ///
    [SerializeField] private Image normalAmmoBackground;
    [SerializeField] private Image bouncyAmmoBackground;
    [SerializeField] private Image exploAmmoBackground;

    Color greenColor = new Color(72, 224, 113, 255);
    Color yellowColor = new Color(242, 163, 46, 255);
    //////////


    enum bulletType {
        NORMAL,
        BOUNCY,
        EXPLO
    }

    bulletType ammoTypeUsed = Player.bulletType.NORMAL;

    // Start is called before the first frame update
    //nice way to get child object by name
    //GameObject firePoint = shooter.transform.Find("FirePoint").gameObject;
    void Start()
    {
        view = GetComponent<PhotonView>();
        maxHealth = health;
        if (view.IsMine) {
            sceneCamera = GameObject.Find("Main Camera");
            head = GameObject.Find("Head");
            akFirePoint = GameObject.Find("FirePoint"); // akFirePoint
            pistolFirePoint = GameObject.Find("PistolFirePoint");
            shotgunFirePoint = GameObject.Find("ShotgunFirePoint");

            if (isHoldingAk == 0) {
                pistol.SetActive(false);
                shotgun.SetActive(false);
                firePoint = akFirePoint;
                bulletsInWeaponMagazine = akMagazineSize;
            }

            sceneCamera.SetActive(false);
            playerCamera.SetActive(true);
        } else {
            Destroy(ui);
        }
        Debug.Log(view.Owner.NickName);
        playerName.text = view.Owner.NickName;

        normalAmmoBackground.color = Color.yellow; //delete later
    }

    private void Update()
    {
        if (!view.IsMine)
            return;

        ammoCountText1.text = ammoCountNormal.ToString();
        ammoCountText2.text = ammoCountBouncy.ToString();
        ammoCountText3.text = ammoCountExplo.ToString();

        //picking ammo types
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            ammoTypeUsed = bulletType.NORMAL;
            normalAmmoBackground.color = Color.yellow;
            bouncyAmmoBackground.color = Color.white;
            exploAmmoBackground.color = Color.white;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            ammoTypeUsed = bulletType.BOUNCY;
            normalAmmoBackground.color = Color.white;
            bouncyAmmoBackground.color = Color.green; 
            exploAmmoBackground.color = Color.white;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            ammoTypeUsed = bulletType.EXPLO;
            normalAmmoBackground.color = Color.white;
            bouncyAmmoBackground.color = Color.white; 
            exploAmmoBackground.color = Color.red;
        }
        //


        inputPosition.x = Input.GetAxis("Horizontal");
        inputPosition.y = Input.GetAxis("Vertical");

        Camera playerCam = playerCamera.GetComponent<Camera>();
        mousePosition = playerCam.ScreenToWorldPoint(Input.mousePosition); //Getting the coordinates of mouse cursor as world's point

        //Picking up items from the ground and dropping the weapon that the player is currently holding
        if (pickUpAllowed && Input.GetKeyDown(KeyCode.E)) {
            Debug.Log(weaponSymbol.name);
            equipWeapon(weaponSymbol.name);
            this.GetComponent<PhotonView>().RPC("equipWeapon", RpcTarget.OthersBuffered, weaponSymbol.name);
            this.GetComponent<PhotonView>().RPC("destroyWeaponSymbol", RpcTarget.All);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if(ak.activeSelf)
            {
                inReload = true;
                StartCoroutine("reloadAk");
            }
            else if (pistol.activeSelf)
            {
                inReload = true;
                StartCoroutine("reloadPistol");
            }
            else if (shotgun.activeSelf)
            {
                inReload = true;
                StartCoroutine("reloadShotgun");
            }
        }

        //Getting positions for the player rotation
        if (head != null) {
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
        }

        ammoCountText1.text = ammoCountNormal.ToString();
        ammoCountText2.text = ammoCountBouncy.ToString();
        ammoCountText3.text = ammoCountExplo.ToString();

        int availableBullets = 0;
        if (ammoTypeUsed == Player.bulletType.NORMAL)
        {
            availableBullets = (int)ammoCountNormal;
        }
        else if (ammoTypeUsed == Player.bulletType.BOUNCY)
        {
            availableBullets = (int)ammoCountBouncy;
        }
        else if (ammoTypeUsed == Player.bulletType.EXPLO)
        {
            availableBullets = (int)ammoCountExplo;
        }

        if(bulletsInWeaponMagazine == 0)
        {
            inReload = true;
        }

        // Shooting
        if (ak.activeInHierarchy && Input.GetButton("Fire1") && view.IsMine) {
            if ((timeStamp <= Time.time) && (bulletsInWeaponMagazine > 0))
            {
                if (!inReload) {
                    ShootAk();
                    Debug.Log(bulletsInWeaponMagazine);
                    timeStamp = Time.time + shotCooldown;
                }
                //bulletsInWeaponMagazine--;
            }
            else if((availableBullets > 0) && (bulletsInWeaponMagazine == 0))
            {
                inReload = true;
                StartCoroutine("reloadAk");
            }
            else
            {
                Debug.Log("Pociski niet");
            }

        }
        else if (pistol.activeInHierarchy && Input.GetButtonDown("Fire1") && view.IsMine) {
            if((timeStamp <= Time.time) && bulletsInWeaponMagazine > 0) {
                if (!inReload) {
                    ShootPistol();
                    Debug.Log(bulletsInWeaponMagazine);
                    timeStamp = Time.time + shotCooldown;
                }
                //bulletsInWeaponMagazine--;
            }
            else if(availableBullets > 0 && (bulletsInWeaponMagazine == 0))
            {
                inReload = true;
                StartCoroutine("reloadPistol");
            }
            else
            {
                Debug.Log("Pociski niet");
            }
        }
        else if(shotgun.activeInHierarchy && Input.GetButtonDown("Fire1") && view.IsMine){
            if((timeStamp <= Time.time) && (bulletsInWeaponMagazine > 0)) {
                if (!inReload) { 
                    ShootShotgun();
                    Debug.Log(bulletsInWeaponMagazine);
                    timeStamp = Time.time + shotCooldown;
                }
                //bulletsInWeaponMagazine--;
            }
            else if(availableBullets > 0 && (bulletsInWeaponMagazine == 0))
            {
                inReload = true;
                StartCoroutine("reloadShotgun");
            }
            else
            {
                Debug.Log("Pociski niet");
            }
        }
    }

    IEnumerator reloadAk()
    {
        Debug.Log("Startuje korutyne");
        yield return new WaitForSeconds(reloadAkTime);
        Debug.Log("Uzupelniam pociski");
        bulletsInWeaponMagazine = 30;
        //bulletsInWeaponMagazine = subtractLoadedAmmoByType(akMagazineSize);
        Debug.Log("Pociskow mam: " + bulletsInWeaponMagazine);
        inReload = false;
    }

    IEnumerator reloadPistol()
    {
        yield return new WaitForSeconds(reloadPistolTime);
        bulletsInWeaponMagazine = 12;
        //bulletsInWeaponMagazine = subtractLoadedAmmoByType(pistolMagazineSize);
        inReload = false;
    }

    IEnumerator reloadShotgun()
    {
        yield return new WaitForSeconds(reloadShotgunTime);
        bulletsInWeaponMagazine = 2;
        //bulletsInWeaponMagazine = subtractLoadedAmmoByType(shotgunMagazineSize);
        inReload = false;
    }

    private int subtractLoadedAmmoByType(int numberQuantity)
    {
        int loadedBullets = 0;
        if(ammoTypeUsed == Player.bulletType.NORMAL)
        {
            loadedBullets = (ammoCountNormal >= numberQuantity) ? numberQuantity : (int)ammoCountNormal;
            ammoTypeUsed -= loadedBullets;
        }
        else if(ammoTypeUsed == Player.bulletType.BOUNCY)
        {
            loadedBullets = (ammoCountBouncy >= numberQuantity) ? numberQuantity : (int)ammoCountBouncy;
            ammoTypeUsed -= loadedBullets;
        }
        else if(ammoTypeUsed == Player.bulletType.EXPLO)
        {
            loadedBullets = (ammoCountExplo >= numberQuantity) ? numberQuantity : (int)ammoCountExplo;
            ammoTypeUsed -= loadedBullets;
        }
        return loadedBullets;
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
            //From hips aiming rotation (close range)
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
    public void TakeDamage(float damage)
    {
        if (view.IsMine) {
            health -= damage;
            healthbarImage.fillAmount = health / maxHealth;
            Debug.Log("Health: " + health + " maxHealth: " + maxHealth + " divided: " + health / maxHealth);
        }
        if (health <= 0) {
            if(view.IsMine)
                PhotonNetwork.LoadLevel("Dead");
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.tag.Equals("WeaponSymbol")) {
            pickUpAllowed = true;
            weaponSymbol = collision.gameObject;
            /*if (weaponSymbol.name.Contains("pistol")){
                bulletsInWeaponMagazine = collision.gameObject.GetComponent<PistoSymbolScript>().pistolAmmoInMagazine;
            }
            else if (weaponSymbol.name.Contains("ak")){
                bulletsInWeaponMagazine = collision.gameObject.GetComponent<AkSymbolScript>().akAmmoInMagazine;
            }
            else if (weaponSymbol.name.Contains("shotgun")){
                bulletsInWeaponMagazine = collision.gameObject.GetComponent<ShotgunSymboScript>().shotgunAmmoInMagazine;
            }*/
        } else if (collision.gameObject.tag.Equals("AmmoSymbol1") || collision.gameObject.tag.Equals("AmmoSymbol2") || collision.gameObject.tag.Equals("AmmoSymbol3")) {
            ammoSymbol = collision.gameObject;
            Debug.Log(collision.gameObject.name);
            if(collision.gameObject.tag.Equals("AmmoSymbol1")) {
                ammoCountNormal += 30;
                Debug.Log(ammoCountNormal);
            } else if(collision.gameObject.tag.Equals("AmmoSymbol2")) {
                ammoCountBouncy += 30;
                Debug.Log(ammoCountBouncy);
            } else if(collision.gameObject.tag.Equals("AmmoSymbol3")) {
                ammoCountExplo += 30;
                Debug.Log(ammoCountExplo);
            }
            this.GetComponent<PhotonView>().RPC("destroyAmmoSymbol", RpcTarget.AllBuffered);
        }
    }

    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.tag.Equals("WeaponSymbol")) {
            pickUpAllowed = false;
        }
    }

    [PunRPC]
    public void equipWeapon(string weaponName) {
        if (weaponName.Contains("pistol")) {
            Debug.Log("Pisztolet znaleziony");
            ak.SetActive(false);
            pistol.SetActive(true);
            shotgun.SetActive(false);
            firePoint = pistolFirePoint;
            bulletsInWeaponMagazine = pistolMagazineSize;
            dropCurrentWeapon(isHoldingAk);
            isHoldingAk = 1;

        }
        else if (weaponName.Contains("ak")) {
            Debug.Log("Akacz znaleziony");
            ak.SetActive(true);
            pistol.SetActive(false);
            shotgun.SetActive(false);
            firePoint = akFirePoint;
            bulletsInWeaponMagazine = akMagazineSize;
            dropCurrentWeapon(isHoldingAk);
            isHoldingAk = 0;
        }
        else if (weaponName.Contains("shotgun"))
        {
            Debug.Log("Shotgun znaleziony");
            ak.SetActive(false);
            pistol.SetActive(false);
            shotgun.SetActive(true);
            firePoint = shotgunFirePoint;
            bulletsInWeaponMagazine = shotgunMagazineSize;
            dropCurrentWeapon(isHoldingAk);
            isHoldingAk = 2;


        }
    }

    public void dropCurrentWeapon(int wasHoldingAK) {
        if (view.IsMine) {
            if (wasHoldingAK == 0)
                PhotonNetwork.Instantiate(akSymbolPrefab.name, this.transform.position, this.transform.rotation);
            else if(wasHoldingAK == 1)
                PhotonNetwork.Instantiate(pistolSymbolPrefab.name, this.transform.position, this.transform.rotation);
            else if(wasHoldingAK == 2)
                PhotonNetwork.Instantiate(shotgunSymbolPrefab.name, this.transform.position, this.transform.rotation);
        }
    }

    //function realizing releasing the bullet from barell
    //[RPC] - obsolete
    [PunRPC]
    void ShootAk() {
        if(bulletsInWeaponMagazine != 0) {
            if(ammoTypeUsed == bulletType.NORMAL)
                PhotonNetwork.Instantiate(bulletPrefab.name, akFirePoint.transform.position, akFirePoint.transform.rotation); //Instantiation of a new bullet
            else if(ammoTypeUsed == bulletType.BOUNCY)
                PhotonNetwork.Instantiate(bouncyBulletPrefab.name, akFirePoint.transform.position, akFirePoint.transform.rotation); //Instantiation of a new bullet
            else if(ammoTypeUsed == bulletType.EXPLO)
                PhotonNetwork.Instantiate(exploBulletPrefab.name, akFirePoint.transform.position, akFirePoint.transform.rotation); //Instantiation of a new bullet
            bulletsInWeaponMagazine--;
        }
    }

    [PunRPC]
    void ShootPistol() {
        if(bulletsInWeaponMagazine != 0) {
            if (ammoTypeUsed == bulletType.NORMAL)
                PhotonNetwork.Instantiate(bulletPrefab.name, pistolFirePoint.transform.position, pistolFirePoint.transform.rotation); //Instantiation of a new bullet
            else if (ammoTypeUsed == bulletType.BOUNCY)
                PhotonNetwork.Instantiate(bouncyBulletPrefab.name, pistolFirePoint.transform.position, pistolFirePoint.transform.rotation); //Instantiation of a new bullet
            else if (ammoTypeUsed == bulletType.EXPLO)
                PhotonNetwork.Instantiate(exploBulletPrefab.name, pistolFirePoint.transform.position, pistolFirePoint.transform.rotation); //Instantiation of a new bullet
            bulletsInWeaponMagazine--;
        }
    }

    [PunRPC]
    void ShootShotgun() {
        if(bulletsInWeaponMagazine != 0) {
            int shotgunScatteringValueMiddle = Random.Range(0, 5);
            int shotgunScatteringValueRight = Random.Range(15, 40);
            int shotgunScatteringValueLeft = Random.Range(320, 345);
            if (ammoTypeUsed == bulletType.NORMAL) {
                PhotonNetwork.Instantiate(bulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueMiddle)); //Instantiation of a new bullet
                PhotonNetwork.Instantiate(bulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueRight)); //Instantiation of a new bullet
                PhotonNetwork.Instantiate(bulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueLeft)); //Instantiation of a new bullet
            }
            else if (ammoTypeUsed == bulletType.BOUNCY) { 
                PhotonNetwork.Instantiate(bouncyBulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueMiddle)); //Instantiation of a new bullet
                PhotonNetwork.Instantiate(bouncyBulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueRight)); //Instantiation of a new bullet
                PhotonNetwork.Instantiate(bouncyBulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueLeft)); //Instantiation of a new bullet
            }
            else if (ammoTypeUsed == bulletType.EXPLO) {
                PhotonNetwork.Instantiate(exploBulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueMiddle)); //Instantiation of a new bullet
                PhotonNetwork.Instantiate(exploBulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0,0, shotgunScatteringValueRight)); //Instantiation of a new bullet
                PhotonNetwork.Instantiate(exploBulletPrefab.name, shotgunFirePoint.transform.position, shotgunFirePoint.transform.rotation * Quaternion.Euler(0, 0, shotgunScatteringValueLeft)); //Instantiation of a new bullet
            }
            bulletsInWeaponMagazine--;
        }
    }

    [PunRPC]
    public void destroyWeaponSymbol() {
        Destroy(weaponSymbol);
    }

    [PunRPC]
    public void destroyAmmoSymbol() {
        Destroy(ammoSymbol);
    }

}
