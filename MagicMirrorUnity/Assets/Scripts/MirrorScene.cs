﻿using UnityEngine;
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
    public UnityEngine.AudioSource powerUp1;
    public UnityEngine.AudioSource powerUp2;
    public UnityEngine.AudioSource powerUp3;


    // Parameters
    public float scale = 2f;
    public float speed = 10f;

    //Strings for texbox and UI
    public Text textboxLeft;
    public Text textboxRight;
    private string stringPowerLevel = "";
    private string stringKamehameha = "";
    private string stringTransformation = "";

    //Measuring time
    private float startTime = 0;
    private float ellapsedTime = 0;
    private bool timeChecker = false;
    private bool alreadyPlaying = false;

    //Booleans for interactions
    private float powerLevel = 290;
    private float kamehameha = 0;
    private float transformCounter = 2;
    private bool chargeKamehameha = false;
    private bool inAnimationKameha = false;


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
    //help function for the HD face recognition
   

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
                    Body body = bodies.Where(b => b.IsTracked).FirstOrDefault();
                    
                    
                    if (body != null)
                    {

                        textboxRight.text = stringPowerLevel + System.Environment.NewLine
                        + stringTransformation + System.Environment.NewLine + stringKamehameha;


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
                            rend = videoKamehameha.GetComponent<Renderer>();
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
                                saiyanChargeSound.Play();
                            }
                            else if(transformCounter == 1)
                            {
                                vpDeepYellow.Stop();
                                vpDeepYellowLoop.Play();
                                rend.enabled = true;
                                powerUp2.Play();
                                saiyanChargeSound.Play();
                            }
                            else if(transformCounter == 2)
                            {
                                vpBlue.Stop();
                                vpBlueLoop.Play();
                                rend.enabled = true;
                                powerUp3.Play();
                                saiyanChargeSound.Play();
                            }

                            timeChecker = false;
                        }

                        //Charge Energie if hands are close to each other and below middle of body
                        float distanceShoulders = Mathf.Abs(worldElbowRight.x - worldElbowLeft.x);
                        float distanceHands = Mathf.Abs(worldRight.x - worldLeft.x);
                        float ratioHandShoulder = distanceHands / distanceShoulders;
                        if (worldRight.x < worldLeftHip.x && worldLeft.x < worldLeftHip.x
                            || worldRight.x > worldRightHip.x && worldLeft.x > worldRightHip.x)
                        {
                            chargeKamehameha = true;
                        }
                        else
                        {
                            chargeKamehameha = false;
                        }

                        if(inAnimationKameha && Time.time - startTime > 0.2)
                        {
                            vpKamehameha.Play();
                            if(kamehameha == 0)
                            {
                                vpKamehameha.Stop();
                                inAnimationKameha = false;
                            }
                            else
                            {
                                kamehameha--;
                                stringKamehameha = "Kamehameha Level: " + kamehameha;
                            }
                        }

                        if (ratioHandShoulder < 0.6 && !timeChecker)
                        {
                            if (powerLevel < 301)
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
                                stringPowerLevel = "Power Level: " + powerLevel;
                                if (powerLevel % 100 == 0 && powerLevel < 400)
                                {
                                    Renderer rend = videoTransformation.GetComponent<Renderer>();
                                    if (powerLevel == 100)
                                    {
                                        chargeSound.Stop();
                                        vpYellow.Play();
                                        rend.enabled = true;
                                        stringTransformation = "Transformation: Super Saiyan";

                                    }
                                    else if (powerLevel == 200)
                                    {
                                        saiyanChargeSound.Stop();
                                        vpDeepYellow.Play();
                                        rend.enabled = true;
                                        transformCounter++;
                                        stringTransformation = "Transformation: Super Saiyan 2";
                                    }
                                    else if (powerLevel == 300)
                                    {
                                        saiyanChargeSound.Stop();
                                        vpBlue.Play();
                                        rend.enabled = true;
                                        transformCounter++;
                                        stringTransformation = "Transformation: Super Saiyan God";
                                    }
                                    transformSound.Play();
                                    startTime = Time.time;
                                    timeChecker = true;
                                }

                            }
                            //hands close and kamehameha position
                            else if(chargeKamehameha && !inAnimationKameha)
                            {
                                kamehameha++;
                                stringKamehameha = "Kamehameha Level: " + kamehameha;
                                if (kamehameha % 100 == 0)
                                {
                                    vpBlueLoop.Stop();
                                    saiyanChargeSound.Pause();
                                    Renderer rend = videoKamehameha.GetComponent<Renderer>();
                                    rend.enabled = true;
                                    inAnimationKameha = true;
                                    startTime = Time.time;
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
                            widthKamehamha = 8 - worldRight.x;
                        }
                        else
                        {
                            widthKamehamha = 8 + worldRight.x;
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