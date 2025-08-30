using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovementScript : MonoBehaviour {
	Rigidbody rb;
	//
	private Transform platformTr;
	private Vector3 lastPlatformPos;
	private bool riding;
	//

	[Tooltip("Current players speed")]
	public float currentSpeed;
	[Tooltip("Assign players camera here")]
	[HideInInspector]public Transform cameraMain;
	[Tooltip("Force that moves player into jump")]
	public float jumpForce = 500;
	[Tooltip("Position of the camera inside the player")]
	[HideInInspector]public Vector3 cameraPosition;

	/*
	 * Getting the Players rigidbody component.
	 * And grabbing the mainCamera from Players child transform.
	 */

	public GameObject startText;

	private Vector3 startPosition;

	private AudioSource teleportAudio;

	private AudioSource forceFieldAudio;

	private AudioSource whooshingAudio;

	void Awake(){
		rb = GetComponent<Rigidbody>();
		cameraMain = transform.Find("Main Camera").transform;
		bulletSpawn = cameraMain.Find ("BulletSpawn").transform;
		ignoreLayer = 1 << LayerMask.NameToLayer ("Player");

		startPosition = transform.position;

		GameObject teleportObject = GameObject.Find("teleport");
    	teleportAudio = teleportObject.GetComponent<AudioSource>();

		GameObject forceFieldObject = GameObject.Find("forceField");
		forceFieldAudio = forceFieldObject.GetComponent<AudioSource>();

		GameObject whooshingObject = GameObject.Find("whooshing");
		whooshingAudio = whooshingObject.GetComponent<AudioSource>();

		StartCoroutine(ShowStartText());
	}

	private IEnumerator ShowStartText()
    {
		startText.gameObject.SetActive(true);

		while (!Input.GetKeyDown(KeyCode.Return)) {
        	yield return null;
		}

		startText.gameObject.SetActive(false);
    }

	private Vector3 slowdownV;
	private Vector2 horizontalMovement;
	/*
	* Raycasting for meele attacks and input movement handling here.
	*/
	void FixedUpdate(){
		RaycastForMeleeAttacks ();

		PlayerMovementLogic ();
	}
	/*
	* Accordingly to input adds force and if magnitude is bigger it will clamp it.
	* If player leaves keys it will deaccelerate
	*/
	void PlayerMovementLogic(){
		currentSpeed = rb.linearVelocity.magnitude;
		horizontalMovement = new Vector2 (rb.linearVelocity.x, rb.linearVelocity.z);
		if (horizontalMovement.magnitude > maxSpeed){
			horizontalMovement = horizontalMovement.normalized;
			horizontalMovement *= maxSpeed;    
		}
		rb.linearVelocity = new Vector3 (
			horizontalMovement.x,
			rb.linearVelocity.y,
			horizontalMovement.y
		);
		if (grounded){
			rb.linearVelocity = Vector3.SmoothDamp(rb.linearVelocity,
				new Vector3(0,rb.linearVelocity.y,0),
				ref slowdownV,
				deaccelerationSpeed);
		}

		if (grounded) {
			rb.AddRelativeForce (Input.GetAxis ("Horizontal") * accelerationSpeed * Time.deltaTime, 0, Input.GetAxis ("Vertical") * accelerationSpeed * Time.deltaTime);
		} else {
			rb.AddRelativeForce (Input.GetAxis ("Horizontal") * accelerationSpeed / 2 * Time.deltaTime, 0, Input.GetAxis ("Vertical") * accelerationSpeed / 2 * Time.deltaTime);

		}
		/*
		 * Slippery issues fixed here
		 */
		if (Input.GetAxis ("Horizontal") != 0 || Input.GetAxis ("Vertical") != 0) {
			deaccelerationSpeed = 0.5f;
		} 
		else {
			// deaccelerationSpeed = 0.1f;
			deaccelerationSpeed = 10000.0f;
		}
	}
	/*
	* Handles jumping and ads the force and sounds.
	*/
	void Jumping(){
		if (Input.GetKeyDown (KeyCode.Space) && grounded) {
			rb.AddRelativeForce (Vector3.up * jumpForce);
			if (_jumpSound)
				_jumpSound.Play ();
			else
				print ("Missig jump sound.");
			_walkSound.Stop ();
			_runSound.Stop ();
		}
	}

	private bool isComplete = false;

	private Vector3 finalPosition = new Vector3(1221f, 376.7f, 1323f);

	public float launchForce = 1000f;

	public GameObject finalImage;

	private GunInventory GI;

	public GameObject finalText;

	/*
	* Update loop calling other stuff
	*/
	void Update(){
		

		Jumping ();

		Crouching();

		WalkingSound ();

		//tp
		if (teleporting && Input.GetKeyDown(KeyCode.T))
        {
			rb.linearVelocity = Vector3.zero;
			currentSpeed = 0;
			rb.position = tpPosition;
			teleportAudio.Play();
        }

		//game completed
		if (transform.position.y < -1000f) 
		{
			rb.position = finalPosition;
			teleportAudio.Play();
			teleporting = false;
			StartCoroutine(waitBeforeThrust());
		}

		if (isComplete)
		{
			rb.AddForce(Vector3.up * launchForce, ForceMode.Impulse);
		}
	}//end update

	private IEnumerator waitBeforeThrust()
	{
		yield return new WaitForSeconds(4f);
		isComplete = true;
		whooshingAudio.Play();
		yield return new WaitForSeconds(10f);
		GI = GetComponent<GunInventory>();
		GI.DeadMethod();
		whooshingAudio.Stop();
		finalImage.SetActive(true);
		yield return new WaitForSeconds(3f);
		finalText.SetActive(true);
		yield return new WaitForSeconds(3f);
		finalText.SetActive(false);
	}

	/*
	* Checks if player is grounded and plays the sound accorindlgy to his speed
	*/
	void WalkingSound(){
		if (_walkSound && _runSound) {
			if (RayCastGrounded ()) { //for walk sounsd using this because suraface is not straigh			
				if (currentSpeed > 1) {
					//				print ("unutra sam");
					if (maxSpeed == 3) {
						//	print ("tu sem");
						if (!_walkSound.isPlaying) {
							//	print ("playam hod");
							_walkSound.Play ();
							_runSound.Stop ();
						}					
					} else if (maxSpeed == 5) {
						//	print ("NE tu sem");

						if (!_runSound.isPlaying) {
							_walkSound.Stop ();
							_runSound.Play ();
						}
					}
				} else {
					_walkSound.Stop ();
					_runSound.Stop ();
				}
			} else {
				_walkSound.Stop ();
				_runSound.Stop ();
			}
		} else {
			print ("Missing walk and running sounds.");
		}

	}
	/*
	* Raycasts down to check if we are grounded along the gorunded method() because if the
	* floor is curvy it will go ON/OFF constatly this assures us if we are really grounded
	*/
	private bool RayCastGrounded(){
		RaycastHit groundedInfo;
		if(Physics.Raycast(transform.position, transform.up *-1f, out groundedInfo, 1, ~ignoreLayer)){
			Debug.DrawRay (transform.position, transform.up * -1f, Color.red, 0.0f);
			if(groundedInfo.transform != null){
				//print ("vracam true");
				return true;
			}
			else{
				//print ("vracam false");
				return false;
			}
		}
		//print ("nisam if dosao");

		return false;
	}

	/*
	* If player toggle the crouch it will scale the player to appear that is crouching
	*/
	void Crouching(){
		if(Input.GetKey(KeyCode.C)){
			transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1,0.6f,1), Time.deltaTime * 15);
		}
		else{
			transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1,1,1), Time.deltaTime * 15);

		}
	}


	[Tooltip("The maximum speed you want to achieve")]
	public int maxSpeed = 5;
	[Tooltip("The higher the number the faster it will stop")]
	public float deaccelerationSpeed = 15.0f;


	[Tooltip("Force that is applied when moving forward or backward")]
	public float accelerationSpeed = 50000.0f;


	[Tooltip("Tells us weather the player is grounded or not.")]
	public bool grounded;
	/*
	* checks if our player is contacting the ground in the angle less than 60 degrees
	*	if it is, set groudede to true
	*/
	void OnCollisionStay(Collision other){
		foreach(ContactPoint contact in other.contacts){
			if(Vector2.Angle(contact.normal,Vector3.up) < 60){
				grounded = true;
			}
		}
	}
	/*
	* On collision exit set grounded to false
	*/
	void OnCollisionExit ()
	{
		grounded = false;
	}


	RaycastHit hitInfo;
	private float meleeAttack_cooldown;
	private string currentWeapo;
	[Tooltip("Put 'Player' layer here")]
	[Header("Shooting Properties")]
	private LayerMask ignoreLayer;//to ignore player layer
	Ray ray1, ray2, ray3, ray4, ray5, ray6, ray7, ray8, ray9;
	private float rayDetectorMeeleSpace = 0.15f;
	private float offsetStart = 0.05f;
	[Tooltip("Put BulletSpawn gameobject here, palce from where bullets are created.")]
	[HideInInspector]
	public Transform bulletSpawn; //from here we shoot a ray to check where we hit him;
	/*
	* This method casts 9 rays in different directions. ( SEE scene tab and you will see 9 rays differently coloured).
	* Used to widley detect enemy infront and increase meele hit detectivity.
	* Checks for cooldown after last preformed meele attack.
	*/


	public bool been_to_meele_anim = false;
	private void RaycastForMeleeAttacks(){




		if (meleeAttack_cooldown > -5) {
			meleeAttack_cooldown -= 1 * Time.deltaTime;
		}


		if (GetComponent<GunInventory> ().currentGun) {
			if (GetComponent<GunInventory> ().currentGun.GetComponent<GunScript> ()) 
				currentWeapo = "gun";
		}

		//middle row
		ray1 = new Ray (bulletSpawn.position + (bulletSpawn.right*offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace));
		ray2 = new Ray (bulletSpawn.position - (bulletSpawn.right*offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace));
		ray3 = new Ray (bulletSpawn.position, bulletSpawn.forward);
		//upper row
		ray4 = new Ray (bulletSpawn.position + (bulletSpawn.right*offsetStart) + (bulletSpawn.up*offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace) + (bulletSpawn.up * rayDetectorMeeleSpace));
		ray5 = new Ray (bulletSpawn.position - (bulletSpawn.right*offsetStart) + (bulletSpawn.up*offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace) + (bulletSpawn.up * rayDetectorMeeleSpace));
		ray6 = new Ray (bulletSpawn.position + (bulletSpawn.up*offsetStart), bulletSpawn.forward + (bulletSpawn.up * rayDetectorMeeleSpace));
		//bottom row
		ray7 = new Ray (bulletSpawn.position + (bulletSpawn.right*offsetStart) - (bulletSpawn.up*offsetStart), bulletSpawn.forward + (bulletSpawn.right * rayDetectorMeeleSpace) - (bulletSpawn.up * rayDetectorMeeleSpace));
		ray8 = new Ray (bulletSpawn.position - (bulletSpawn.right*offsetStart) - (bulletSpawn.up*offsetStart), bulletSpawn.forward - (bulletSpawn.right * rayDetectorMeeleSpace) - (bulletSpawn.up * rayDetectorMeeleSpace));
		ray9 = new Ray (bulletSpawn.position -(bulletSpawn.up*offsetStart), bulletSpawn.forward - (bulletSpawn.up * rayDetectorMeeleSpace));

		Debug.DrawRay (ray1.origin, ray1.direction, Color.cyan);
		Debug.DrawRay (ray2.origin, ray2.direction, Color.cyan);
		Debug.DrawRay (ray3.origin, ray3.direction, Color.cyan);
		Debug.DrawRay (ray4.origin, ray4.direction, Color.red);
		Debug.DrawRay (ray5.origin, ray5.direction, Color.red);
		Debug.DrawRay (ray6.origin, ray6.direction, Color.red);
		Debug.DrawRay (ray7.origin, ray7.direction, Color.yellow);
		Debug.DrawRay (ray8.origin, ray8.direction, Color.yellow);
		Debug.DrawRay (ray9.origin, ray9.direction, Color.yellow);

		if (GetComponent<GunInventory> ().currentGun) {
			if (GetComponent<GunInventory> ().currentGun.GetComponent<GunScript> ().meeleAttack == false) {
				been_to_meele_anim = false;
			}
			if (GetComponent<GunInventory> ().currentGun.GetComponent<GunScript> ().meeleAttack == true && been_to_meele_anim == false) {
				been_to_meele_anim = true;
				//	if (isRunning == false) {
				StartCoroutine ("MeeleAttackWeaponHit");
				//	}
			}
		}

	}

	/*
	 *Method that is called if the waepon hit animation has been triggered the first time via Q input
	 *and if is, it will search for target and make damage
	 */
	IEnumerator MeeleAttackWeaponHit(){
		if (Physics.Raycast (ray1, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray2, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray3, out hitInfo, 2f, ~ignoreLayer)
			|| Physics.Raycast (ray4, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray5, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray6, out hitInfo, 2f, ~ignoreLayer)
			|| Physics.Raycast (ray7, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray8, out hitInfo, 2f, ~ignoreLayer) || Physics.Raycast (ray9, out hitInfo, 2f, ~ignoreLayer)) {
			//Debug.DrawRay (bulletSpawn.position, bulletSpawn.forward + (bulletSpawn.right*0.2f), Color.green, 0.0f);
			if (hitInfo.transform.tag=="Dummie") {
				Transform _other = hitInfo.transform.root.transform;
				if (_other.transform.tag == "Dummie") {
					print ("hit a dummie");
				}
				InstantiateBlood(hitInfo,false);
			}
		}
		yield return new WaitForEndOfFrame ();
	}

	[Header("BloodForMelleAttaacks")]
	RaycastHit hit;//stores info of hit;
	[Tooltip("Put your particle blood effect here.")]
	public GameObject bloodEffect;//blod effect prefab;
	/*
	* Upon hitting enemy it calls this method, gives it raycast hit info 
	* and at that position it creates our blood prefab.
	*/
	void InstantiateBlood (RaycastHit _hitPos,bool swordHitWithGunOrNot) {		

		if (currentWeapo == "gun") {
			GunScript.HitMarkerSound ();

			if (_hitSound)
				_hitSound.Play ();
			else
				print ("Missing hit sound");
			
			if (!swordHitWithGunOrNot) {
				if (bloodEffect)
					Instantiate (bloodEffect, _hitPos.point, Quaternion.identity);
				else
					print ("Missing blood effect prefab in the inspector.");
			}
		} 
	}
	private GameObject myBloodEffect;
	
	public GameObject superJumpText;
	public bool isSuper = false;

	public GameObject jumpEnhancerText;
	public bool isEnhanced = false;

	private Vector3 tpPosition;
	private bool teleporting = false;

	public GameObject deathText;

	public GameObject finalPlatText;

	public bool finalTextShown = false;

	private void OnCollisionEnter(Collision collision)
	{
		//teleport mechanic
		if (collision.gameObject.layer == LayerMask.NameToLayer("ForceField") || 
		collision.gameObject.layer == LayerMask.NameToLayer("MovingPlatformLayer"))
		{
			teleporting = true;
			tpPosition = transform.position;
			forceFieldAudio.Play();
		}

		if (collision.gameObject.CompareTag("SuperPlane") && isSuper == false)
		{
			isSuper = true;
			jumpForce = jumpForce * 4;
			StartCoroutine(ShowSuperJumpMessage());
		}

		if (collision.gameObject.CompareTag("JumpEnhancer") && isEnhanced == false)
		{
			isEnhanced = true;
			jumpForce = jumpForce * 2;
			StartCoroutine(ShowJumpEnhancerMessage());
		}

		if (collision.gameObject.CompareTag("Water")) {
			rb.linearVelocity = Vector3.zero;
			currentSpeed = 0;
			rb.position = startPosition;
			jumpForce = 20000;
			isSuper = false;
			isEnhanced = false;
			teleporting = false;
			finalTextShown = false;
			teleportAudio.Play();
			StartCoroutine(ShowDeathText());
		}

		if (collision.gameObject.CompareTag("FinalPlatform") && finalTextShown == false) {
			StartCoroutine(FinalPlatText());
			finalTextShown = true;
		}
	}

	private IEnumerator ShowSuperJumpMessage()
    {
		superJumpText.gameObject.SetActive(true);
		yield return new WaitForSeconds(2f);
		superJumpText.gameObject.SetActive(false);
    }

	private IEnumerator ShowJumpEnhancerMessage()
    {
		jumpEnhancerText.gameObject.SetActive(true);
		yield return new WaitForSeconds(2f);
		jumpEnhancerText.gameObject.SetActive(false);
    }

	private IEnumerator ShowDeathText()
	{
		deathText.gameObject.SetActive(true);
		yield return new WaitForSeconds(6f);
		deathText.gameObject.SetActive(false);
	}

	private IEnumerator FinalPlatText()
	{
		finalPlatText.gameObject.SetActive(true);
		yield return new WaitForSeconds(4f);
		finalPlatText.gameObject.SetActive(false);
	}


	[Header("Player SOUNDS")]
	[Tooltip("Jump sound when player jumps.")]
	public AudioSource _jumpSound;
	[Tooltip("Sound while player makes when successfully reloads weapon.")]
	public AudioSource _freakingZombiesSound;
	[Tooltip("Sound Bullet makes when hits target.")]
	public AudioSource _hitSound;
	[Tooltip("Walk sound player makes.")]
	public AudioSource _walkSound;
	[Tooltip("Run Sound player makes.")]
	public AudioSource _runSound;
}