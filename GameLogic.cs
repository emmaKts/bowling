using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameLogic : MonoBehaviour {

	public float speed;

	private Rigidbody rb;

	/* game logic variables */
	//flags
	private bool ball_thrown = false;
	private bool stopMotion = false;
	private bool sleep = true;
	private bool pinsCollision = false;

	// initializing ball position variables
	private Vector3 ballInitPos;
	private Vector3 pinInitPos;
	private Quaternion ballInitRot;
	private Quaternion pinInitRot;

	//strike variables
	private bool strike = false;
	private bool p1Strike = false;
	private bool p2Strike = false;
	private int count1Strikes = 0;
	private int count2Strikes = 0;
	private bool strike1Bonus = false;
	private bool strike2Bonus = false;

	//spare variables
	private bool spare = false;
	private bool p1Spare = false;
	private bool p2Spare = false;
	private bool spare1Bonus = false;
	private bool spare2Bonus = false;

	private GameObject[] pins;   //not keeping all pins because some are becoming inactive
	public GameObject[] allPins; //keep all pins

	//counters
	private int countHits = 0; //values: 1 or 2
	private int frame = 0;
	private int roll1 = 0; //first ball hit
	private int roll2 = 0; //second ball hit
	private int rolls = 0; //count the rolls for every player, values: 1, 2, 3, 4, 5... 48

	private int countRs = 0; //count reset scene

	private int[] player1Score = new int[11];
	private int[] player2Score = new int[11];

	//private float dt = Time.deltaTime*15; // 2 to 5 give reasonable results

	/* display score variables */
	//player 1 score display on UI
	public Text[] p1roll1Text;
	public Text[] p1roll2Text;
	public Text[] p1scoreText;

	//player 2 score display on UI
	public Text[] p2roll1Text;
	public Text[] p2roll2Text;
	public Text[] p2scoreText;
	public Text winner;

	//counters
	private int r1 = 0;
	private int r2 = 0;

	public GameObject playButton;
	public GameObject endButton;

	public AudioSource pinColl;
	public AudioSource startMusic;

	void Start()
	{
		playButton.SetActive (true);
	}

	public void playSound()
	{
		startMusic.Play ();
	}

	// Use this for initialization
	public void StartGame ()
	{
		playButton.SetActive (false);
		Debug.Log ("Start");

		rb = GetComponent<Rigidbody> ();
		rb.useGravity = false;
		pins = GameObject.FindGameObjectsWithTag ("Pins");
		stopMotion = true;
		//2 hits(rolls) for each frame
		// initializing ball position
		ResetBall ();
	}

	//Game logic
	// Update is called once per frame
	void Update ()
	{
		//at the beginning - when the ball is still not thrown
		// stop motion variable checks/stops ball's motion caused from keyboard after ball has been thrown
		if ((Input.GetKey(KeyCode.LeftArrow)||(Input.GetKey(KeyCode.RightArrow))) && !ball_thrown && stopMotion) {
			//using keyboard arrows to move the ball left or right just before the hit
			Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0, 0);
			transform.Translate (direction * .3f * Time.deltaTime);
		}
		//GetKeyDown : Returns true during the frame the user starts pressing down the key identified by name.
		//It will not return true until the user has released the key and pressed it again.
		//hit the ball
		if ((Input.GetKeyDown(KeyCode.UpArrow)) && !ball_thrown && stopMotion) { //up arrow - left mouse click
			rb.AddForce (transform.forward * speed);
			rb.useGravity = true;
			ball_thrown = true;
			countHits++;
		}
		//ball colliding with pins
		if (ball_thrown && pinsCollision){
			pinsCollision = false;
			ball_thrown = false;
			stopMotion = false;
			rolls++;
			//count frames
			if (rolls % 4 == 1) {
				if (frame >= 10) { //max value of frame = 10
					frame = 10;
				} else {
					frame++;
				}
				//Debug.Log ("frame: " + frame);
			}
			//Debug.Log("countHits = "+countHits);
			if (countHits == 2) { //next player
				StartCoroutine(ResetScene());
			}else{ //1st hit of the round - countHits resets back to zero each time RestartScene is called, so the only values it takes here are 1 and 2
				StartCoroutine (countThePins ());
			}
		}
	}

	//Physics
	void OnCollisionEnter(Collision collisionInfo){
		if (ball_thrown && ((collisionInfo.gameObject.CompareTag ("Pins")) || (collisionInfo.transform.name == "UnderLane"))) {
			// ball colliding with pins or underlane
			if (collisionInfo.gameObject.CompareTag ("Pins"))
			{
				pinColl.Play ();
			}
			pinsCollision = true;
			//Debug.Log ("In contact with pins or underlane object.");
		}
	}

	void ResetBall() {
		Debug.Log("Reset ball");
		//ball initial position
		ballInitPos = new Vector3(0.002f, 0.6f, -5.918f);
		transform.position = ballInitPos;
		ballInitRot = Quaternion.Euler(0, 0, 0);
		transform.rotation = ballInitRot;
	}

	IEnumerator countThePins(){
		yield return new WaitForSeconds (3.0f);
		foreach (GameObject pin in pins)
		{
			if (pin.transform.up.y < 0.99f) { //threshold found after testing
				roll1++;
				//Debug.Log ("pin element 1st round : " + pin +" - transform.up.y :"+pin.transform.up.y);
			}
		}
		//Debug.Log ("rolls: " +rolls);
		//Debug.Log ("r1: " +r1);
		//Debug.Log ("r2: " +r2);
		//filling the score arrays (UI)
		if (rolls % 4 == 1) { //1, 5, 9, 13...
			//Debug.Log ("test 1");
			p1roll1Text [r1].text = roll1.ToString ();
			if (roll1 == 10) {
					r1++;
			}
		} else if (rolls % 4 == 3) {//3, 7, 11, 15...
			//Debug.Log ("test 2");
			if (r2 == 13) {
				GameOver ();
			} else {
				p2roll1Text [r2].text = roll1.ToString ();
				if (roll1 == 10) {
					r2++;
				}
			}
		}
		//Debug.Log ("roll1: " +roll1);
		if (roll1 == 10) {
			Debug.Log ("Strike!");
			strike = true;
			rolls++;
			StartCoroutine (ResetScene ());
		}else if((rolls == 41 && spare1Bonus)||(rolls == 43 && spare2Bonus)){ //extra roll of 10th frame (countRs = 21 for player 1, countRs=22 for player 2)
			rolls++;
			StartCoroutine (ResetScene ());
		} else {
			ResetPosition ();
		}
	}

	void ResetPosition() { //prepare position for 2nd round
		Debug.Log("reset position");
		//reset the ball for the next round
		transform.position = ballInitPos;
		transform.rotation = ballInitRot;
		rb.useGravity = false;
		if (rb != null) {
			if (sleep) {
				//Debug.Log("sleeping");
				rb.Sleep();
			} else {
				//Debug.Log("not sleeping");
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
		//make fallen pins disappear
		foreach (GameObject pin in pins)
		{
			if (pin.transform.up.y < 0.99f) { //threshold found after testing
				pin.SetActive (false);
				//Debug.Log ("pin element 1st round: " + pin + " - transform.up.y : "+ pin.transform.up.y);
			}
		}
		//second try/ball
		StartGame ();
	}

	void ResetAllPosition(){ //prepare position for next frame
		//reset the ball for the next frame
		transform.position = ballInitPos;
		transform.rotation = ballInitRot;
		rb.useGravity = false;
		if (rb != null) {
			if (sleep) {
				//Debug.Log("sleeping");
				rb.Sleep();
			} else {
				//Debug.Log("not sleeping");
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
			}
		}
		//reseting pins for the next frame: activate inactive pins on initial position
		foreach (GameObject pin in allPins)
		{
			//Debug.Log ("pin element 1st round: " + pin);
			pin.GetComponent<PinsController> ().ResetPin (); //call PinsController script
			pin.SetActive (true);
		}
		StartGame ();
	}

	//playing alternately

	IEnumerator ResetScene() { //next frame
		Debug.Log ("reset scene");
		Debug.Log ("rolls: " +rolls);
		countRs++; //counts how many times resetScene() is being called : values from 1, 2, 3... 24
		Debug.Log ("countRs = " + countRs);
		if (!strike) {
			yield return new WaitForSeconds (3.0f);
			//count fallen pins from same player's second hit (roll2)
			if ((countRs == 23 && count1Strikes == 2) || (countRs == 24 && count2Strikes == 2)||(countRs == 21 && spare1Bonus)||(countRs == 22 && spare2Bonus)) { //2 strikes in a row at the 10th frame - no roll2
				Debug.Log ("countRs = " + countRs + " - count1strikes = " + count1Strikes + " - count2strikes = " + count2Strikes);
			} else {
				foreach (GameObject pin in pins) {
					if (pin.transform.up.y < 0.98f) {
						roll2++;
						//Debug.Log ("pin element resetscene : " + pin);
					}
				}
			}
			if ((roll1 + roll2) == 10) {
				spare = true;
			}
			Debug.Log ("roll2 = " + roll2);
			if (rolls % 4 == 2) { //2, 6, 10, 14...
				Debug.Log ("test 3");
				p1roll2Text [r1].text = roll2.ToString ();
				r1++;
			} else if (rolls % 4 == 0) { //4, 8, 12, 16...
				Debug.Log ("test 4");
				if (r2 == 13) {
					GameOver ();
				} else {
					p2roll2Text [r2].text = roll2.ToString ();
					r2++;
				}
			}
			if (!spare) {
				if ((countRs % 2 == 1) && !p1Strike && !p1Spare) {//even for player 1 - no strikes, no spares
					Debug.Log ("if 1");
					player1Score [frame] = player1Score [frame - 1] + roll1 + roll2;
					if (((countRs == 21)||(countRs == 23)) && !strike1Bonus && !strike2Bonus && spare1Bonus && !spare2Bonus) {
						GameOver ();
					}
					strike1Bonus = false;
					spare1Bonus = false;
				} else if ((countRs % 2 == 0) && !p2Strike && !p2Spare) { //odd for player 2 - no strikes, no spares
					Debug.Log ("if 2");
					player2Score [frame] = player2Score [frame - 1] + roll1 + roll2;
					if (((countRs == 20)||(countRs==22)||(countRs==24)) && !strike1Bonus && !strike2Bonus && !spare1Bonus && !spare2Bonus) { //10th frame, 2nd player, simple roll1-roll2 leads to the end of the game
						GameOver ();
					}
					strike2Bonus = false;
					spare2Bonus = false;
				} else if ((countRs % 2 == 1) && p1Strike) { //player 1 - strike points
					Debug.Log ("if 3");
					if (countRs == 21 && count1Strikes == 1) {
						player1Score [frame] = player1Score [frame - 1] + player1Score [frame] + roll1 + roll2;
						if (!strike2Bonus && !spare2Bonus) {
							GameOver ();
						}
					} else if (countRs == 21 && count1Strikes == 2) {
						Debug.Log ("if 4");
						player1Score [frame - 1] = player1Score [frame - 2] + player1Score [frame - 1] + player1Score [frame] + roll1;
						player1Score [frame] = player1Score [frame - 1] + player1Score [frame] + roll1 + roll2;
						if (!strike2Bonus && !spare2Bonus) {
							GameOver ();
						}
					} else if (countRs == 23 && count1Strikes == 2) {
						Debug.Log ("if 5");
						player1Score [frame] = player1Score [frame - 1] + player1Score [frame] + roll1;
						if (!strike2Bonus) {
							Debug.Log ("if 6");
							GameOver ();
						}
					} else {
						Debug.Log ("if 7");
						calculateStrikes (frame, count1Strikes, player1Score, roll1, roll2);
					}
					p1Strike = false;
					p1Spare = false;
					strike1Bonus = false;
					spare1Bonus = false;
				} else if ((countRs % 2 == 0) && p2Strike) { //player 2 - strike points
					Debug.Log ("if 8");
					if (countRs == 22 && count1Strikes == 1) {
						player2Score [frame] = player2Score [frame - 1] + player2Score [frame] + roll1 + roll2;
						if (!strike1Bonus) {
							GameOver ();
						}
					} else if (countRs == 22 && count1Strikes == 2) {
						Debug.Log ("if 9");
						player2Score [frame - 1] = player2Score [frame - 2] + player2Score [frame - 1] + player2Score [frame] + roll1;
						player2Score [frame] = player2Score [frame - 1] + player2Score [frame] + roll1 + roll2;
						if (!strike1Bonus) {
							Debug.Log ("if 10");
							GameOver ();
						}
					} else if (countRs == 24 && count1Strikes == 2) {
						Debug.Log ("if 11");
						player2Score [frame] = player2Score [frame - 1] + player2Score [frame] + roll1;
						GameOver ();
					} else {
						Debug.Log ("if 12");
						calculateStrikes (frame, count2Strikes, player2Score, roll1, roll2);
					}
					p2Strike = false;
					p2Spare = false;
					strike2Bonus = false;
					spare2Bonus = false;
				} else if ((countRs % 2 == 1) && p1Spare) { //player 1 - spare points
					Debug.Log ("if 13");
					if (countRs == 21) {
						player1Score [frame] = player1Score [frame - 1] + player1Score [frame] + roll1;
						if (!strike2Bonus && !spare2Bonus) {
							Debug.Log ("if 14");
							GameOver ();
						}
					} else {
						Debug.Log ("if 15");
						player1Score [frame - 1] = player1Score [frame - 2] + player1Score [frame - 1] + roll1;
						player1Score [frame] = player1Score [frame - 1] + roll1 + roll2;
					}
					p1Spare = false;
					p1Strike = false;
					spare1Bonus = false;
				} else if ((countRs % 2 == 0) && p2Spare) { //player 2 - spare points
					Debug.Log ("if 16");
					if (countRs == 22) {
						Debug.Log ("if 17");
						player2Score [frame] = player2Score [frame - 1] + player2Score [frame] + roll1;
						if (!strike1Bonus) {
							GameOver ();
						}
					} else {
						Debug.Log ("if 18");
						player2Score [frame - 1] = player2Score [frame - 2] + player2Score [frame - 1] + roll1;
						player2Score [frame] = player2Score [frame - 1] + roll1 + roll2;
					}
					p2Spare = false;
					p2Strike = false;
					spare2Bonus = false;
				}
			} else if (spare) {
				if ((countRs % 2 == 1) && !p1Spare) {//even for player 1
					Debug.Log ("if 19");
					player1Score [frame] = roll1 + roll2; //roll1 + roll2 = 10;
					p1Spare = true;
					if (p1Strike) {
						Debug.Log ("if 20");
						calculateStrikes (frame, count1Strikes, player1Score, roll1, roll2);
						player1Score [frame] = roll1 + roll2;
					}
					if (countRs == 19) {
						Debug.Log ("if 21");
						spare1Bonus = true;
					}
					if (((countRs == 21)||(countRs == 23)) && !strike2Bonus && !spare2Bonus) {
						GameOver ();
					}
					p1Strike = false;
					strike1Bonus = false;
				} else if ((countRs % 2 == 0) && !p2Spare) { //odd for player 2
					Debug.Log ("if 22");
					player2Score [frame] = roll1 + roll2;
					p2Spare = true;
					if (p2Strike) {
						Debug.Log ("if 23");
						calculateStrikes (frame, count2Strikes, player2Score, roll1, roll2);
						player2Score [frame] = roll1 + roll2;
					}
					if (countRs == 20) {
						Debug.Log ("if 24");
						spare2Bonus = true;
					}
					if ((countRs == 22 && !strike1Bonus && !spare1Bonus)||(countRs == 24)) {
						GameOver ();
					}
					p2Strike = false;
					strike2Bonus = false;
				} else if ((countRs % 2 == 1) && p1Spare) {//player 1- spares in a row
					Debug.Log ("if 25");
					player1Score [frame - 1] = player1Score [frame - 2] + player1Score [frame - 1] + roll1;
					player1Score [frame] = roll1 + roll2; //roll1 + roll2 = 10;
					if (countRs == 19) {
						Debug.Log ("if 25.5");
						spare1Bonus = true;
					}
					p1Strike = false;
					strike1Bonus = false;
				} else if ((countRs % 2 == 0) && p2Spare) {//player 2- spares in a row
					Debug.Log ("if 26");
					player2Score [frame - 1] = player2Score [frame - 2] + player2Score [frame - 1] + roll1;
					player2Score [frame] = roll1 + roll2; //roll1 + roll2 = 10;
					if (countRs == 20) {
						Debug.Log ("if 26.5");
						spare2Bonus = true;
					}
					p2Strike = false;
					strike2Bonus = false;
				}
			}
			for (int i = 1; i <= frame; i++) {
				Debug.Log ("p1score: " + player1Score [i] + " - of: " + i);
				Debug.Log ("p2score: " + player2Score [i] + " - of: " + i);
			}
		} else if (strike) {
			if (countRs % 2 == 1 && !p1Strike) { //strike was from player 1
				//player1Score [frame] = "X ";
				Debug.Log ("if 27");
				player1Score [frame] = roll1; //roll1 = 10
				p1Strike = true;
				count1Strikes = 1;
				if (countRs < 21 && p1Spare) {
					Debug.Log ("if 29");
					player1Score [frame-1] = player1Score [frame-2] + player1Score [frame-1] + player1Score [frame]; //player1Score [frame] = roll1
				}
				if (countRs == 19) {
					Debug.Log ("if 28");
					strike1Bonus = true;
				}
				if(countRs == 21 && p1Spare){ //extra roll of the 10th frame is strike and previous hit was spare
					player1Score [frame] = player1Score [frame-1] + 2 * player1Score [frame];
					if (!strike2Bonus || !spare2Bonus) { //if there was no strike or spare bonus for player two, the game is over
						GameOver ();
					}
				}
				if (countRs == 23 && !strike2Bonus) {
					GameOver ();
				}
				p1Spare = false;
				spare1Bonus = false;
			} else if (countRs % 2 == 0 && !p2Strike) { //strike was from player 2
				Debug.Log ("if 30");
				player2Score [frame] = roll1; //roll1 = 10
				p2Strike = true;
				count2Strikes = 1;
				if (countRs < 22 && p2Spare) {
					Debug.Log ("if 30.5");
					player2Score [frame-1] = player2Score [frame-2] + player2Score [frame-1] + player2Score [frame]; //player2Score [frame] = roll1
				}
				if (countRs == 20){
					Debug.Log ("if 31");
					strike2Bonus = true;
				}
				if (countRs == 22 && p2Spare) {
					Debug.Log ("if 32");
					player2Score [frame] = player2Score [frame - 1] + 2 * player2Score [frame];
					if (!strike1Bonus) { //if there was no strike bonus(there is no possibility that there was any spare bonus at this point) for player one, the game is over
						GameOver ();
					}
				}
				if (countRs == 24) {
					GameOver ();
				}
				p2Spare = false;
				spare2Bonus = false;
			} else if (countRs % 2 == 1 && p1Strike) { //strikes in a row from player 1
				//player1Score [frame] = "X ";
				Debug.Log ("if 33");
				player1Score [frame] = roll1; //roll1 = 10
				count1Strikes++;
				if (countRs == 19){
					Debug.Log ("if 31");
					strike1Bonus = true;
				}
				if (count1Strikes == 3 && countRs <= 19) { // strikes in a row
					Debug.Log ("if 34");
					player1Score [frame - 2] = player1Score [frame - 3] + player1Score [frame - 2] + player1Score [frame - 1] + player1Score [frame];
					count1Strikes--;
				} else if (count1Strikes == 3 && countRs == 21) {
					Debug.Log ("if 35");
					player1Score [9] = player1Score [8] + player1Score [9] + player1Score [10] + 10;
					count1Strikes--;
				} else if (count1Strikes == 3 && countRs == 23) {
					Debug.Log ("if 36");
					player1Score [10] = player1Score [9] + 30; //points from frame 9 plus 30 points from the next two strikes
					if (!strike2Bonus) {
						GameOver ();
					}
				}
				if ((countRs == 21 && !strike2Bonus && !spare2Bonus)||(countRs == 23 && !strike2Bonus)) {
					Debug.Log ("if 37");
					countRs++;
				}
				p1Spare = false;
				spare1Bonus = false;
			} else if (countRs % 2 == 0 && p2Strike) { //strikes in a row from player 2
				Debug.Log ("if 38");
				player2Score [frame] = roll1; //roll1 = 10
				count2Strikes++;
				if (countRs == 20){
					Debug.Log ("if 31");
					strike2Bonus = true;
				}
				if (count2Strikes == 3 && countRs <= 20) { // strikes in a row
					Debug.Log ("if 38");
					player2Score [frame - 2] = player2Score [frame - 3] + player2Score [frame - 2] + player2Score [frame - 1] + player2Score [frame];
					count2Strikes--;
				} else if (count2Strikes == 3 && countRs == 22) {
					Debug.Log ("if 39");
					player2Score [9] = player2Score [8] + player2Score [9] + player2Score [10] + 10;
					count2Strikes--;
				} else if (count2Strikes == 3 && countRs == 24) {
					Debug.Log ("if 40");
					player2Score [10] = player2Score [9] + 30; //points from frame 9 plus 30 points from the next two strikes
					GameOver ();
				}
				if ((countRs == 20 && !strike1Bonus && !spare1Bonus)||(countRs == 22 && !strike1Bonus)) {
					Debug.Log ("if 41");
					countRs++;
				}
				p2Spare = false;
				spare2Bonus = false;
			}
		}
		roll1 = 0;
		roll2 = 0;
		strike = false;
		spare = false;
		countHits = 0;
		for (int i = 1; i <= frame; i++) {
			Debug.Log ("p1score: " + player1Score [i] + "  of: " + i);
			Debug.Log ("p2score: " + player2Score [i] + "  of: " + i);
			p1scoreText [i-1].text = player1Score [i].ToString ();
			p2scoreText [i-1].text = player2Score [i].ToString ();
		}
		ResetAllPosition (); //next frame
	}

	void calculateStrikes (int frame, int countStrikes, int[] playerScore, int roll1, int roll2)
	{
		if (countStrikes == 1) {
			playerScore [frame - countStrikes] = playerScore [frame - (countStrikes + 1)] + playerScore [frame - countStrikes] + roll1 + roll2;
			playerScore [frame] = playerScore [frame - countStrikes] + roll1 + roll2;
		} else if (countStrikes == 2) {
			playerScore [frame - countStrikes] = playerScore [frame - (countStrikes + 1)] + playerScore [frame - countStrikes] + playerScore [frame - (countStrikes - 1)] + roll1;
			calculateStrikes (frame, 1, playerScore, roll1, roll2);
		}
	}

	public void GameOver(){
		if (player1Score [frame] > player2Score [frame]) {
			Debug.Log ("Player 1 wins with score: " + player1Score [frame]);
			winner.text = "Player 1 wins with score : " + player1Score [frame].ToString ();
		} else if (player1Score [frame] == player2Score [frame]) {
			Debug.Log ("Same score, no winner!");
			winner.text = "Same score, no winner!";
		} else {
			Debug.Log ("Player 2 wins with score: " + player2Score [frame]);
			winner.text = "Player 2 wins with score : " + player2Score [frame].ToString ();
		}
	}

	public void Restart(){
		Scene scene = SceneManager.GetActiveScene ();
		SceneManager.LoadScene (scene.name);
	}

	public void QuitGame(){
		Application.Quit ();
	}
}
