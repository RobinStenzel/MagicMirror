using UnityEngine;
using System.Linq;
using Windows.Kinect;
using Microsoft.Kinect.Face;
using UnityEngine.UI;
using UnityEngine.Video;
using System;
using UnityEngine.SceneManagement;

public class MirrorScene : MonoBehaviour
{
    // Kinect members.
    private KinectSensor sensor;
    private ColorFrameReader colorReader;
    private BodyFrameReader bodyReader;
    private Body[] bodies;


    // Color frame display.
    private Texture2D texture;
    private byte[] pixels;
    private int width;
    private int height;

    // Visual elements.
    public GameObject quad;
    public GameObject textboxManager;
    public GameObject videoTransformation;
    public GameObject videoKamehameha;

    // Videos and Audio
    public VideoPlayer vpYellow;
    public VideoPlayer vpDeepYellow;
    public VideoPlayer vpBlue;
    public VideoPlayer vpYellowLoop;
    public VideoPlayer vpDeepYellowLoop;
    public VideoPlayer vpBlueLoop;
    public VideoPlayer vpKamehameha;
    public UnityEngine.AudioSource transformSound;
    public UnityEngine.AudioSource chargeSound;
    public UnityEngine.AudioSource saiyanChargeSound;
    public UnityEngine.AudioSource saiyanChargeSound2;
    public UnityEngine.AudioSource saiyanChargeSound3;
    public UnityEngine.AudioSource powerUp1;
    public UnityEngine.AudioSource powerUp2;
    public UnityEngine.AudioSource powerUp3;


    // Parameters
    public float scale = 2f;
    public float speed = 10f;

    //Strings for texbox and UI
    public Text textboxUp;
    public Text textboxDown;
    private string stringPowerLevel = "PowerLevel:";
    private string stringKamehameha = "Kamehameha: 0";
    private string stringTransformation = "Saiyan";
    private string stringIndication = "Indication:" + System.Environment.NewLine + System.Environment.NewLine 
        + "Amène tes mains ensemble pour générer de l'energie" + System.Environment.NewLine + System.Environment.NewLine
        + "Obtenir PowerLevel 400 pour apprendre le 'Kaméhaméha'";

    //Measuring time
    private float startTime = 0;
    private float ellapsedTime = 0;
    private float kamehaBreak;
    private bool timeChecker = false;
    private bool alreadyPlaying = false;
    
    //Booleans for interactions
    private float powerLevel = 0;
    private float kamehameha = 0;
    private float transformCounter = 0;
    private float loadingSide = 0;
    private bool chargeKamehameha = false;
    private bool inAnimationKameha = false;
    private bool kamehaBuffer = false;


    private void changeColorOfGameObject(UnityEngine.Color inColor, UnityEngine.Color outColor, GameObject obj, ref bool flag, ref float t)
    {
        float duration = 3f;
        UnityEngine.Color lerpedColor = UnityEngine.Color.Lerp(inColor, outColor, t);
        obj.GetComponent<Renderer>().material.color = lerpedColor;

        if (flag == true)
        {
            t -= Time.deltaTime / duration;
            if (t < 0.01f)
                flag = false;
        }
        else
        {
            t += Time.deltaTime / duration;
            if (t > 0.99f)
                flag = true;
        }
    }

    //Transform a 3d kinect point to a 2d point
    Vector3 map3dPointTo2d(CameraSpacePoint startPoint)
    {
        var colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(startPoint);
        var resultVector = new Vector2(0f, 0f);


        if (!float.IsInfinity(colorPoint.X) && !float.IsInfinity(colorPoint.Y))
        {
            resultVector.x = colorPoint.X;
            resultVector.y = colorPoint.Y;
        }

        // Map the 2D position to the Unity space.
        Vector3 worldPoint = Camera.main.ViewportToWorldPoint(new Vector3(resultVector.x / width, resultVector.y / height, 0f));
        return worldPoint;
    }

    private ulong currTrackingId = 0;
    private Body GetActiveBody()
    {
        if (currTrackingId <= 0)
        {
            foreach (Body body in this.bodies)
            {
                if (body.IsTracked)
                {
                    currTrackingId = body.TrackingId;
                    return body;
                }
            }

            return null;
        }
        else
        {
            foreach (Body body in this.bodies)
            {
                if (body.IsTracked && body.TrackingId == currTrackingId)
                {
                    return body;
                }
            }
        }

        currTrackingId = 0;
        return GetActiveBody();
    }


    void Start()
    {
        Renderer rend = videoTransformation.GetComponent<Renderer>();
        rend.enabled = false;
        rend = videoKamehameha.GetComponent<Renderer>();
        rend.enabled = false;

        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            // Initialize readers.
            bodyReader = sensor.BodyFrameSource.OpenReader();
            colorReader = sensor.ColorFrameSource.OpenReader();

            // Body frame data.
            bodies = new Body[sensor.BodyFrameSource.BodyCount];

            // Color frame data.
            width = sensor.ColorFrameSource.FrameDescription.Width;
            height = sensor.ColorFrameSource.FrameDescription.Height;
            pixels = new byte[width * height * 4];
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Assign the texture to the proper game object. Also, flip the texture vertically (Kinect bug).
            quad.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
            quad.GetComponent<Renderer>().sharedMaterial.SetTextureScale("_MainTex", new Vector2(-1, 1));

            sensor.Open();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        if (colorReader != null)
        {
            using (var frame = colorReader.AcquireLatestFrame())
            {
                if (frame != null)
                {
                    frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Rgba);
                    texture.LoadRawTextureData(pixels);
                    texture.Apply();
                }
            }
        }
        //HD face frame, update points and allignement
        if (bodyReader != null)
        {
            using (var frame = bodyReader.AcquireLatestFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(bodies);
                    Body body = GetActiveBody();
                    
                    
                    if (body != null)
                    {

                        textboxUp.text = stringTransformation + System.Environment.NewLine + System.Environment.NewLine + stringPowerLevel + System.Environment.NewLine
                         + System.Environment.NewLine + stringKamehameha;
                        textboxDown.text = stringIndication;


                        // Detect the hand (left or right) that is closest to the sensor.
                        var handRight = body.Joints[JointType.HandRight].Position;
                        var handLeft = body.Joints[JointType.HandLeft].Position;
                        var elbowLeft = body.Joints[JointType.ElbowLeft].Position;
                        var elbowRight = body.Joints[JointType.ElbowRight].Position;
                        var hipLeft = body.Joints[JointType.HipLeft].Position;
                        var hipRight = body.Joints[JointType.HipRight].Position;
                        var spineBase = body.Joints[JointType.SpineBase].Position;
                        var spineMid = body.Joints[JointType.SpineMid].Position;
                        var head = body.Joints[JointType.Head].Position;


                        var closer = handRight.Z < handLeft.Z ? handRight : handLeft;


                        // Map the 2D position to the Unity space.
                        var worldRight = map3dPointTo2d(handRight);
                        var worldLeft = map3dPointTo2d(handLeft);
                        var worldElbowRight = map3dPointTo2d(elbowRight);
                        var worldElbowLeft = map3dPointTo2d(elbowLeft);
                        var worldLeftHip = map3dPointTo2d(hipLeft);
                        var worldRightHip = map3dPointTo2d(hipRight);
                        var worldFrontHead = map3dPointTo2d(head);
                        var worldCloser = map3dPointTo2d(closer);
                        var worldSpineBase = map3dPointTo2d(spineBase);
                        var worldSpineMid = map3dPointTo2d(spineMid);


                        var midHand = (worldRight + worldLeft) / 2;
                        var center = quad.GetComponent<Renderer>().bounds.center;
                        var currentBallPosition = midHand;


                        ellapsedTime = Time.time - startTime;
                        if (timeChecker && ellapsedTime > 5.18f)
                        {
                            Renderer rend = videoTransformation.GetComponent<Renderer>();
                            rend.enabled = false;

                        }

                        if (timeChecker && ellapsedTime > 5.2f)
                        {
                            Renderer rend = videoTransformation.GetComponent<Renderer>();
                            if (transformCounter == 0)
                            {
                                vpYellow.Stop();
                                vpYellowLoop.Play();
                                rend.enabled = true;
                                powerUp1.Play();
                                stringTransformation = "Super Saiyan";
                                saiyanChargeSound.Play();
                            }
                            else if(transformCounter == 1)
                            {
                                vpDeepYellow.Stop();
                                vpDeepYellowLoop.Play();
                                rend.enabled = true;
                                powerUp2.Play();
                                stringTransformation = "Super Saiyan 2";
                                saiyanChargeSound2.Play();
                            }
                            else if(transformCounter == 2)
                            {
                                vpBlue.Stop();
                                vpBlueLoop.Play();
                                rend.enabled = true;
                                powerUp3.Play();
                                stringTransformation = "Super Saiyan God";
                                saiyanChargeSound3.Play();
                            }

                            timeChecker = false;
                        }

                        //Charge Energie if hands are close to each other and below middle of body
                        float distanceShoulders = Mathf.Abs(worldElbowRight.x - worldElbowLeft.x);
                        float distanceHands = Mathf.Abs(worldRight.x - worldLeft.x);
                        float ratioHandShoulder = distanceHands / distanceShoulders;
                        if ((worldRight.x < worldLeftHip.x && worldLeft.x < worldLeftHip.x
                            || worldRight.x > worldRightHip.x && worldLeft.x > worldRightHip.x)
                            && !inAnimationKameha && powerLevel >= 400 && ratioHandShoulder < 0.75f)
                        {
                            if(loadingSide == 0 && worldRight.x < worldLeftHip.x && worldLeft.x < worldLeftHip.x)
                            {
                                loadingSide = 1;
                            }
                            else if(loadingSide == 0 && worldRight.x > worldRightHip.x && worldLeft.x > worldRightHip.x)
                            {
                                loadingSide = 2;
                            }
                            else if(loadingSide == 1 && worldRight.x < worldLeftHip.x && worldLeft.x < worldLeftHip.x
                                || loadingSide == 2 && worldRight.x > worldRightHip.x && worldLeft.x > worldRightHip.x)
                            {
                                kamehameha += 2;
                                stringKamehameha = "Kamehameha: " + kamehameha;
                            }
                            else if (loadingSide == 2 && worldRight.x < worldLeftHip.x && worldLeft.x < worldLeftHip.x
                                || loadingSide == 1 && worldRight.x > worldRightHip.x && worldLeft.x > worldRightHip.x)
                            {
                                loadingSide = 0;
                                inAnimationKameha = true;
                                startTime = Time.time;
                                vpKamehameha.Play();
                            }
                                chargeKamehameha = true;
                        }
                        else
                        {
                            chargeKamehameha = false;
                        }

                        if(inAnimationKameha && Time.time - startTime > 0.2f && !kamehaBuffer)
                        {
                            Renderer render = videoKamehameha.GetComponent<Renderer>();
                            render.enabled = true;

                            if (kamehameha == 0)
                            {
                                vpKamehameha.Stop();
                                kamehaBuffer = true;
                                kamehaBreak = Time.time;
                                render.enabled = false;
                            }
                            else
                            {
                                kamehameha--;
                                stringKamehameha = "Kamehameha: " + kamehameha;
                            }
                        }

                        if(kamehaBuffer && Time.time - kamehaBreak > 3f)
                        {
                            kamehaBuffer = false;
                            inAnimationKameha = false;
                        }

                        if (ratioHandShoulder < 0.6 && !timeChecker)
                        {
                            if (powerLevel < 400)
                            {
                                if (!alreadyPlaying)
                                {
                                    alreadyPlaying = true;
                                    chargeSound.Play();
                                }
                                if (!timeChecker)
                                {
                                    chargeSound.UnPause();
                                }
                                powerLevel++;
                                if(powerLevel == 400)
                                {
                                    stringIndication = "Indication:" + System.Environment.NewLine + System.Environment.NewLine
                                                    + "Tiens tes mains à une coté de ton corps pour charger le 'Kaméhaméha'" + System.Environment.NewLine + System.Environment.NewLine
                                                    + "Amène-les à l'autre côté pour l'activer";
                                }
                                stringPowerLevel = "Power Level: " + powerLevel;
                                if (powerLevel % 100 == 0 && powerLevel < 400)
                                {
                                    Renderer rend = videoTransformation.GetComponent<Renderer>();
                                    if (powerLevel == 100)
                                    {
                                        chargeSound.Stop();
                                        vpYellow.Play();
                                        rend.enabled = true;
                                       }
                                    else if (powerLevel == 200)
                                    {
                                        saiyanChargeSound.Stop();
                                        vpDeepYellow.Play();
                                        rend.enabled = true;
                                        transformCounter++;
                                    }
                                    else if (powerLevel == 300)
                                    {
                                        saiyanChargeSound2.Stop();
                                        vpBlue.Play();
                                        rend.enabled = true;
                                        transformCounter++;
                                    }

                                    transformSound.Play();
                                    startTime = Time.time;
                                    timeChecker = true;
                                }

                            }
                            //hands close and kamehameha position
                            else
                            {

                                if (chargeKamehameha && !inAnimationKameha)
                                {
                                    vpBlueLoop.Stop();
                                    saiyanChargeSound3.Pause();
                                }
                            }

                        }

                        videoTransformation.transform.position = new Vector3(worldFrontHead.x, -worldSpineMid.y, 0);

                        //kamehameha
                        double xDiff = worldRight.x - worldElbowRight.x;
                        double yDiff = worldRight.y - worldElbowRight.y;
                        float angleRad = (float)Math.Atan2(yDiff, xDiff);
                        float angleDeg = (float)(Math.Atan2(yDiff, xDiff) / Math.PI * 180);
                        float widthKamehamha;
                        if (Math.Abs(angleDeg) < 90)
                        {
                            widthKamehamha = 12;
                        }
                        else
                        {
                            widthKamehamha = 12;
                        }
                        float xMovement = (float)(Math.Cos(-angleRad) * widthKamehamha / 2);
                        float yMovement = (float)(Math.Sin(-angleRad) * widthKamehamha / 2);
                        videoKamehameha.transform.rotation = Quaternion.Euler(0, 0, -angleDeg + 180);
                        videoKamehameha.transform.position = new Vector3(worldRight.x + xMovement, -worldRight.y + yMovement, 0);
                        videoKamehameha.transform.localScale = new Vector3(widthKamehamha, 4.5f, 1);

                    }


                
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        if (bodyReader != null)
        {
            bodyReader.Dispose();
        }

        if (colorReader != null)
        {
            colorReader.Dispose();
        }


        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
        }
    }
}