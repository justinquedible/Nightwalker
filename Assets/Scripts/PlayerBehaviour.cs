using System.Collections;
using System.Collections.Generic;
using System.Runtime;//.Remoting.Messaging;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.FirstPerson;
using Random = UnityEngine.Random;

public class PlayerBehaviour : MonoBehaviour
{
    #region Public Variables
    [Header("Health Settings")]
    public GameObject healthSlider;
    public float health = 100;
    public float healthMax = 100;
    public float healValue = 5;
    public float secondToHeal = 10;
    public float removeHealthValue = 1;
    public float secondToRemoveHealth = 1;

    [Header("Flashlight Battery Settings")]
    public GameObject Flashlight;
    public GameObject batterySlider;
    public float battery = 100;
    public float batteryMax = 100;
    public float removeBatteryValue = 0.05f;
    public float secondToRemoveBattery = 5f;

    [Header("List of Street Lights")]
    public int positionFromLightToLoseHealth = 14;
    public GameObject[] streetlights;

    [Header("Audio Settings")]
    public AudioClip deathNoise;
    public AudioClip cameraNoise;
    public AudioClip heartbeatSound;
    public AudioClip intensifyAmbience;
    public AudioClip[] ambientNoises;
    

    // [Header("Page System Settings")]
    // public List<GameObject> pages = new List<GameObject>();
    // public int collectedPages;

    [Header("UI Settings")]
    public GameObject inGameMenuUI;
    public GameObject gameOverText;
    public GameObject pickUpUI;
    public GameObject finishedGameUI;
    // public GameObject pagesCount;
    public bool paused;
    #endregion

    #region Private Variables
    private int flickerOff;
    private Color oldColor;
    private Light FlashlightLight;
    private Vector3[] streetlightPositions = new Vector3[1];
    private Vector3 playerPosition;
    private bool isDead = false;
    private bool isCritical = false;
    private AudioSource audioSource;
    private float ambientVolume = 0.0f;
    private float heartbeatVolume = 0.0f;
    private IEnumerator heartbeatCoroutine;
    #endregion

	void Start ()
    {
        // set initial health values
        health = healthMax;
        battery = batteryMax;

        healthSlider.GetComponent<Slider>().maxValue = healthMax;
        healthSlider.GetComponent<Slider>().value = healthMax;

        // set initial battery values
        batterySlider.GetComponent<Slider>().maxValue = batteryMax;
        batterySlider.GetComponent<Slider>().value = batteryMax;

        // start consume flashlight battery
        StartCoroutine(RemoveBatteryCharge(removeBatteryValue, secondToRemoveBattery));
        
        // Start losing health only if player is in the dark
        StartCoroutine(RemovePlayerHealth(removeHealthValue, secondToRemoveHealth));

        // Start playing ambient noise, increases as player loses health
        StartCoroutine(StartAmbientNoise(health / 2));

        // Store starting color of flashlight
        FlashlightLight = Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>();
        
        // Store position of each streetlight
        for (int i = 0; i < streetlights.Length; i++)
        {
            streetlightPositions[i] = streetlights[i].transform.position;
        }

        // Store initial flashlight color
        oldColor = FlashlightLight.color;

        audioSource = GetComponent<AudioSource>();
    }

	void Update ()
    {
        // Update player's position
        playerPosition = transform.position;

        // If user presses 'fire1' key, toggle the flashlight
        if (Input.GetButtonDown("Fire1"))
        {
            // Toggle flashlight on and off
            if (FlashlightLight.color != Color.black)
            {
                oldColor = FlashlightLight.color;
                FlashlightLight.color = Color.black;
            }
            else
            {
                FlashlightLight.color = oldColor;
            }
        }

        // update player health slider
        healthSlider.GetComponent<Slider>().value = health;

        // update battery slider
        batterySlider.GetComponent<Slider>().value = battery;

        // if health is under 75% increase ambience
        if (health / healthMax * 100 <= 75 && health / healthMax * 100 != 0)
        {
            if (ambientVolume == 0.0f)
            {
                StartCoroutine(intensifyAmbientAudio(intensifyAmbience, 0.25f));
            }
        }

        //if health is under 50%, ramp up ambience
        if (health / healthMax * 100 <= 50 && !isCritical)
        {
            Debug.Log("Player is at half sanity.");
            if (ambientVolume == 0.25f)
            {
                StartCoroutine(intensifyAmbientAudio(intensifyAmbience, 0.5f));
            }
            // this.GetComponent<AudioSource>().PlayOneShot(intensifyAmbience);
            isCritical = true;
        }

        // if health is low than 20%, intensiy ambience, play heartbeat sound
        if (health / healthMax * 100 <= 20 && health / healthMax * 100 != 0)
        {
            Debug.Log("You are dying.");
            if (ambientVolume == 0.5f)
            {
                StartCoroutine(intensifyAmbientAudio(intensifyAmbience, 1.0f));
            }
            if (heartbeatVolume == 0.0f)
            {
                heartbeatVolume = 0.25f;
                heartbeatCoroutine = playHeartbeatSound(heartbeatVolume, 1.0f);
                StartCoroutine(heartbeatCoroutine);
            }
            // this.GetComponent<AudioSource>().PlayOneShot(deathNoise);
        }

        // Stop coroutine of heart beat and start it again but louder
        if (health / healthMax * 100 <= 10 && health / healthMax * 100 != 0)
        {
            if (heartbeatVolume == 0.25f)
            {
                heartbeatVolume = 1.0f;
                StopCoroutine(heartbeatCoroutine);
                StartCoroutine(playHeartbeatSound(heartbeatVolume, 0.0f));
            }
        }

        // if health is low than 0
        if (health / healthMax * 100 <= 0)
        {
            Debug.Log("You are dead.");
            health = 0.0f;
        }
        //play death noise once
        if (health == 0.0f && !isDead)
        {
            this.GetComponent<AudioSource>().PlayOneShot(deathNoise);
            isDead = true;
        }

        // if battery is low 50%
        if (battery / batteryMax * 100 <= 50)
        {
            Debug.Log("Flashlight is running out of battery.");
            Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>().intensity = 3f;
        }

        // if battery is low 25%
        if (battery / batteryMax * 100 <= 25)
        {
            Debug.Log("Flashlight is almost without battery.");
            Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>().intensity = 2.5f;
            
            
            //Random Flicker Effect: If flickerOff, then light will flicker off.
            flickerOff = Random.Range(1,40);
            if (flickerOff == 1)
            {
                Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>().intensity = 0.0f;
            } 
        }

        // if battery is low 10%
        if (battery / batteryMax * 100 <= 10)
        {
            Debug.Log("You will be out of light.");
            Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>().intensity = 1.5f;           
            
            //Random Flicker Effect: If flickerOff, then light will flicker off.
            flickerOff = Random.Range(1,10);
            if(flickerOff == 1)
            {
                Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>().intensity = 0.0f;
            } 
        }

        // if battery out%
        if (battery / batteryMax * 100 <= 0)
        {

            battery = 0.00f;
            Debug.Log("The flashlight battery is out and you are out of the light.");
            Flashlight.transform.Find("Spotlight").gameObject.GetComponent<Light>().intensity = 0.0f;
            FlashlightLight.color = Color.black;
        }

        // page system
        // pagesCount.GetComponent<Text>().text = "Collected Pages: " + collectedPages + "/8";

        //animations
        if (Input.GetKey(KeyCode.LeftShift))
        {
            this.gameObject.GetComponent<Animation>().CrossFade("Run", 1);
        }
        else
        {
            this.gameObject.GetComponent<Animation>().CrossFade("Idle", 1);
        }
        // collected all pages
        // if (collectedPages >= 8)
        // {
        //     Debug.Log("You finished the game, congratulations...");
        //     Cursor.visible = true;

        //     // disable first person controller and show finished game UI
        //     this.gameObject.GetComponent<FirstPersonController>().enabled = false;
        //     inGameMenuUI.SetActive(false);
        //     finishedGameUI.SetActive(true);       

        //     // set play again button
        //     Button playAgainBtn = finishedGameUI.gameObject.transform.Find("PlayAgainBtn").GetComponent<Button>();
        //     playAgainBtn.onClick.AddListener(this.gameObject.GetComponent<MenuInGame>().PlayAgain);

        //     // set quit button
        //     Button quitBtn = finishedGameUI.gameObject.transform.Find("QuitBtn").GetComponent<Button>();
        //     quitBtn.onClick.AddListener(this.gameObject.GetComponent<MenuInGame>().QuitGame);
        // } 
    }

    private IEnumerator RemoveBatteryCharge(float value, float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);

            // Only lower flashlight battery if color is not black
            if (battery > 0)
            {
                if (FlashlightLight.color != Color.black)
                {
                    Debug.Log("Removing battery value: " + value);
                    battery -= value;
                }
            }
            else
            {
                Debug.Log("The flashlight battery is out");
            }
        }
    }

    public IEnumerator RemovePlayerHealth(float value, float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);
            Debug.Log(health);
            if (health > 0)
            {
                if ((! playerStandingInLight()) && (isFlashlightOff()))
                {
                    Debug.Log("Removing player health value: " + value);
                    health -= value;
                }
            }
            else
            {
                Debug.Log("You're dead");
                paused = true;
                gameOverText.SetActive(true);             
                inGameMenuUI.SetActive(true);
                inGameMenuUI.transform.Find("ContinueBtn").gameObject.GetComponent<Button>().interactable = false;
                inGameMenuUI.transform.Find("PlayAgainBtn").gameObject.GetComponent<Button>().interactable = true;
            }
        }
    }

    // function to heal player
    public IEnumerator StartHealPlayer(float value, float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);

            Debug.Log("Healling player value: " + value);

            if (health > 0 && health < healthMax)
            {
                health += value;
            }
            else
            {
                health = healthMax;
            }
        }
    }

    // Coroutine to play ambient noise when health is low
    public IEnumerator StartAmbientNoise(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time + 5);

            AudioSource audio = GetComponent<AudioSource>();
            int n = Random.Range(0, ambientNoises.Length);
            audio.clip = ambientNoises[n];
            audio.PlayOneShot(audio.clip);
        }
    }

    // Coroutine to play ambient noise when health is low
    public IEnumerator intensifyAmbientAudio(AudioClip audioClip, float volume)
    {
        while (true)
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.PlayOneShot(audioClip, volume);
            ambientVolume = volume;

            yield return new WaitForSeconds(audioClip.length);
        }
    }

    // Play heartbeat sound
    public IEnumerator playHeartbeatSound(float volume, float pauseLength)
    {
        while (true)
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.PlayOneShot(heartbeatSound, volume);

            yield return new WaitForSeconds(heartbeatSound.length + pauseLength);
        }
    }

    // Returns true if flashlight is off
    private bool isFlashlightOff()
    {
        return (FlashlightLight.color == Color.black);
    }

    // Calculates the distance between two points
    private float distance(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }

    // Returns true if player is standing in light
    private bool playerStandingInLight()
    {
        for (int i = 0; i < streetlights.Length; i++)
        {
            Vector3 light = streetlightPositions[i];
            float distanceFromLight = distance(playerPosition.x, playerPosition.z, light.x, light.z);
            if (distanceFromLight < positionFromLightToLoseHealth)
            {
                return true;
            }
            Debug.Log("DistanceFromLight: " + distanceFromLight);
        }
        return false;
    }

    // page system - show UI
    private void OnTriggerEnter(Collider collider)
    {
        // start noise when reach slender
        if (collider.gameObject.transform.tag == "Slender")
        {
            if (health > 0 && paused == false)
            {
                this.GetComponent<AudioSource>().PlayOneShot(cameraNoise);
                this.GetComponent<AudioSource>().loop = true;
            }            
        }

        // if (collider.gameObject.transform.tag == "Page")
        // {
        //     Debug.Log("You Found a Page: " + collider.gameObject.name + ", Press 'E' to pickup");
        //     pickUpUI.SetActive(true);      
        // }
    }

    // page system - pickup system
    // private void OnTriggerStay(Collider collider)
    // {
    //     if (collider.gameObject.transform.tag == "Page")
    //     {       
    //         if (Input.GetKeyDown(KeyCode.E))
    //         {
    //             Debug.Log("You get this page: " + collider.gameObject.name);

    //             // disable UI
    //             pickUpUI.SetActive(false);

    //             // add page to list
    //             pages.Add(collider.gameObject);
    //             collectedPages ++;

    //             // disable game object
    //             collider.gameObject.SetActive(false);
    //         }
    //     }
    // }

    private void OnTriggerExit(Collider collider)
    {
        // remove noise sound
        if (collider.gameObject.transform.tag == "Slender")
        {
            if (health > 0 && paused == false)
            {
                this.GetComponent<AudioSource>().clip = null;
                this.GetComponent<AudioSource>().loop = false;
            }          
        }

        // // disable UI
        // if (collider.gameObject.transform.tag == "Page")
        //     pickUpUI.SetActive(false);
    }
}
