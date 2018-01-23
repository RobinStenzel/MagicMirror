using UnityEngine;
using System.Linq;
using Windows.Kinect;
using Microsoft.Kinect.Face;
using UnityEngine.UI;
using UnityEngine.Video;
using System;

public class MirrorScene : MonoBehaviour
{
    // Kinect members.
    private KinectSensor sensor;
    private ColorFrameReader colorReader;
    private BodyFrameReader bodyReader;
    private FaceFrameSource faceSource;
    private FaceFrameReader faceReader;

    // Acquires HD face data and reads it.
    private HighDefinitionFaceFrameSource HDfaceSource = null;
    private HighDefinitionFaceFrameReader HDfaceReader = null;

    // Required to access the face vertices.
    private FaceAlignment _faceAlignment = null;

    // Required to access the face model points.
    private FaceModel _faceModel = null;
    private Body[] bodies;

    //points tracked by the HD reader
    CameraSpacePoint frontHeadCenter, leftCheekBone, rightCheekBone;
    float reference = 1;

    // Color frame display.
    private Texture2D texture;
    private byte[] pixels;
    private int width;
    private int height;

    // Visual elements.
    public GameObject quad;
    public GameObject ball;
    public GameObject hair;
    public GameObject textboxManager;
    public GameObject videoTransformation;
    public GameObject videoKamehameha;

    // Videos and Audio
    public VideoPlayer vpYellow;
    public VideoPlayer vpDeepYellow;
    public VideoPlayer vpBlue;
    public VideoPlayer vpKamehameha;
    public UnityEngine.AudioSource transformSound;
    public UnityEngine.AudioSource chargeSound;

    // Parameters
    public float scale = 2f;
    public float speed = 10f;
    public string expressionState = "idle";

    //Strings for texbox and UI
    public Text textboxLeft;
    private string stringEyeLeft = "";
    private string stringEyeRight = "";
    private string stringMouthOpen = "";
    private string stringHappy = "";
    private string stringGlasses = "";
    private string stringPowerLevel = "";
    private string stringKamehameha = "";
    private string stringAngle = "";

    //Color changing over time
    private bool flagHair = false;
    private bool flagBall = false;
    private float tHair = 0;
    private float tBall = 0;

    //Measuring time
    private float startTime = 0;
    private float ellapsedTime = 0;
    private bool timeChecker = false;
    private bool alreadyPlaying = false;

    //Booleans for interactions
    private float powerLevel = 0;
    private float kamehameha = 0;
    private bool chargeKamehameha = false;
    private bool inAnimation = false;


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
    private void UpdateFacePoints()
    {
        if (_faceModel == null) return;

        var vertices = _faceModel.CalculateVerticesForAlignment(_faceAlignment);
        if (vertices.Count > 0)
        {
            frontHeadCenter = vertices[28];
            leftCheekBone = vertices[458];
            rightCheekBone = vertices[674];
        }
    }

    //function to update face expressions
    void updateExpressions(FaceFrameResult result)
    {
        // Detect Face Positions and Expressions
        var eyeLeftClosed = result.FaceProperties[FaceProperty.LeftEyeClosed];
        var eyeRightClosed = result.FaceProperties[FaceProperty.RightEyeClosed];
        var mouthOpen = result.FaceProperties[FaceProperty.MouthOpen];
        var happy = result.FaceProperties[FaceProperty.Happy];
        var glasses = result.FaceProperties[FaceProperty.WearingGlasses];

        //Use Expressions to change behaviour
        if (eyeLeftClosed == DetectionResult.Yes || eyeLeftClosed == DetectionResult.Maybe)
        {
            expressionState = "leftEyeClosed";
            stringEyeLeft = "left eye: ";
            stringEyeLeft += eyeLeftClosed == DetectionResult.Yes ? "yes" : "maybe";
        }
        else
        {
            stringEyeLeft = "left eye: no";
        }
        if (eyeRightClosed == DetectionResult.Yes || eyeRightClosed == DetectionResult.Maybe)
        {
            expressionState = "rightEyeClosed";
            stringEyeRight = "right eye: ";
            stringEyeRight += eyeRightClosed == DetectionResult.Yes ? "yes" : "maybe";
        }
        else
        {
            stringEyeRight = "right eye: no";
        }
        if (happy == DetectionResult.Yes || happy == DetectionResult.Maybe)
        {
            expressionState = happy == DetectionResult.Yes ? "idle": expressionState;
            stringHappy = "smile: ";
            stringHappy += happy == DetectionResult.Yes ? "yes" : "maybe";
        }
        else
        {
            stringHappy = "smile: no";

        }
        if(mouthOpen == DetectionResult.Yes || mouthOpen == DetectionResult.Maybe)
        {
            stringMouthOpen = "Mouth opened";
            stringMouthOpen += mouthOpen == DetectionResult.Yes ? "yes" : "no";
        }
        if(glasses == DetectionResult.Yes)
        {
            stringGlasses = "glasses: yes";
        }
        else
        {
            stringGlasses = "glasses: no";
        }

    }
    void Start()
    {
        Renderer rend = videoTransformation.GetComponent<Renderer>();
        rend.enabled = false;
        rend = videoKamehameha.GetComponent<Renderer>();
        rend.enabled = false;
        rend = ball.GetComponent<Renderer>();
        rend.enabled = false;
        rend = hair.GetComponent<Renderer>();
        rend.enabled = false;
        sensor = KinectSensor.GetDefault();

        if (sensor != null)
        {
            // Initialize readers.
            bodyReader = sensor.BodyFrameSource.OpenReader();
            colorReader = sensor.ColorFrameSource.OpenReader();

            //Initialize face source and reader
            faceSource = FaceFrameSource.Create(sensor,0, FaceFrameFeatures.BoundingBoxInColorSpace |
                                                              FaceFrameFeatures.FaceEngagement |
                                                              FaceFrameFeatures.Glasses |
                                                              FaceFrameFeatures.Happy |
                                                              FaceFrameFeatures.LeftEyeClosed |
                                                              FaceFrameFeatures.MouthOpen |
                                                              FaceFrameFeatures.PointsInColorSpace |
                                                              FaceFrameFeatures.RightEyeClosed);
            faceReader = faceSource.OpenReader();

            // Listen for HD face data.
            HDfaceSource = HighDefinitionFaceFrameSource.Create(sensor);
            HDfaceReader = HDfaceSource.OpenReader();
            _faceModel = FaceModel.Create();
            _faceAlignment = FaceAlignment.Create();

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
        if (HDfaceReader != null)
        {
            using (var frame = HDfaceReader.AcquireLatestFrame())
            {
                if (frame != null && frame.IsFaceTracked)
                {
                    frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                    UpdateFacePoints();
                }
            }
        }
        if (bodyReader != null)
        {
            using (var frame = bodyReader.AcquireLatestFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(bodies);

                    var body = bodies.Where(b => b.IsTracked).FirstOrDefault();
                    if (body != null)
                    {
                        //Assign face to tracked body
                        if (!faceSource.IsTrackingIdValid)
                        {
                            faceSource.TrackingId = body.TrackingId;
                        }
                        if (!HDfaceSource.IsTrackingIdValid)
                        {
                            HDfaceSource.TrackingId = body.TrackingId;
                        }
                        if (faceReader != null)
                        {
                            using(var faceFrame = faceReader.AcquireLatestFrame())
                            {
                                if(faceFrame != null)
                                {
                                    FaceFrameResult result = faceFrame.FaceFrameResult;
                                    if(result != null)
                                    {
                                        updateExpressions(result);
                                        textboxLeft.text = stringEyeLeft + System.Environment.NewLine
                                        + stringEyeRight + System.Environment.NewLine + stringHappy + System.Environment.NewLine
                                        + stringGlasses + System.Environment.NewLine + stringPowerLevel + System.Environment.NewLine
                                        + stringAngle + System.Environment.NewLine + stringKamehameha;
                                        

                                        // Detect the hand (left or right) that is closest to the sensor.
                                        var handRight = body.Joints[JointType.HandRight].Position;
                                        var handLeft = body.Joints[JointType.HandLeft].Position;
                                        var elbowLeft = body.Joints[JointType.ElbowLeft].Position;
                                        var elbowRight = body.Joints[JointType.ElbowRight].Position;
                                        var hipLeft = body.Joints[JointType.HipLeft].Position;
                                        var hipRight = body.Joints[JointType.HipRight].Position;

                                        var closer = handRight.Z < handLeft.Z ? handRight : handLeft;
                                        

                                        // Map the 2D position to the Unity space.
                                        var worldRight = map3dPointTo2d(handRight);
                                        var worldLeft = map3dPointTo2d(handLeft);
                                        var worldLeftCheek = map3dPointTo2d(leftCheekBone);
                                        var worldRightCheek = map3dPointTo2d(rightCheekBone);
                                        var worldElbowRight = map3dPointTo2d(elbowRight);
                                        var worldElbowLeft = map3dPointTo2d(elbowLeft);
                                        var worldLeftHip = map3dPointTo2d(hipLeft);
                                        var worldRightHip = map3dPointTo2d(hipRight);
                                        var distanceCheeks = worldRightCheek.x - worldLeftCheek.x;
                                        var worldFrontHead = map3dPointTo2d(frontHeadCenter);
                                        var worldCloser = map3dPointTo2d(closer);
                                       

                                        var midHand = (worldRight + worldLeft) / 2;
                                        var center = quad.GetComponent<Renderer>().bounds.center;
                                        var currentBallPosition = midHand;


                                        ellapsedTime = Time.time - startTime;
                                        if(timeChecker && ellapsedTime > 5.18f)
                                        {
                                            Renderer rend = videoTransformation.GetComponent<Renderer>();
                                            rend.enabled = false;
                                            rend = videoKamehameha.GetComponent<Renderer>();
                                            rend.enabled = false;
                                            timeChecker = false;
                                        }

                                        //Charge Energie if hands are close to each other and below middle of body
                                        float distanceShoulders = Mathf.Abs(worldElbowRight.x - worldElbowLeft.x);
                                        float distanceHands = Mathf.Abs(worldRight.x - worldLeft.x);
                                        float ratioHandShoulder = distanceHands / distanceShoulders;
                                        if(worldRight.x < worldLeftHip.x && worldLeft.x < worldLeftHip.x
                                            || worldRight.x > worldRightHip.x && worldLeft.x > worldRightHip.x)
                                        {
                                            chargeKamehameha = true;
                                        }
                                        else
                                        {
                                            chargeKamehameha = false;
                                        }

                                        if (ratioHandShoulder < 0.6 && !timeChecker)
                                        {
                                            if (!chargeKamehameha)
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
                                                    chargeSound.Pause();
                                                    Renderer rend = videoTransformation.GetComponent<Renderer>();
                                                    if (powerLevel == 100)
                                                    {
                                                        vpYellow.Play();
                                                        rend.enabled = true;
                                                    }
                                                    else if (powerLevel == 200)
                                                    {
                                                        vpDeepYellow.Play();
                                                        rend.enabled = true;
                                                    }
                                                    else if (powerLevel == 300)
                                                    {
                                                        vpBlue.Play();
                                                        rend.enabled = true;
                                                    }
                                                    transformSound.Play();
                                                    startTime = Time.time;
                                                    timeChecker = true;
                                                }

                                            }
                                            //hands close and kamehameha position
                                            else
                                            {
                                                chargeSound.Pause();
                                                kamehameha++;
                                                stringKamehameha = "Kamehameha Level: " + kamehameha;
                                                if(kamehameha % 100 == 0)
                                                {
                                                    Renderer rend = videoKamehameha.GetComponent<Renderer>();
                                                    rend.enabled = true;
                                                    vpKamehameha.Play();
                                                    startTime = Time.time;
                                                    timeChecker = true;
                                                }
                                            }
                                        }
                                        //hands not close
                                        else
                                        {
                                            
                                        }
                                            


                                        switch (expressionState)
                                        {
                                            case "idle":
                                                currentBallPosition = midHand;
                                                reference = (handRight.Z + handLeft.Z)/2;
                                                break;

                                            case "leftEyeClosed":
                                                currentBallPosition = worldLeft;
                                                reference = handLeft.Z;
                                                break;

                                            case "rightEyeClosed":
                                                currentBallPosition = worldRight;
                                                reference = handRight.Z;
                                                break;

                                       
                                        }

                                        // Move and rotate the ball.
                                        ball.transform.localScale = new Vector3(scale, scale, scale) / reference;
                                        ball.transform.position = new Vector3(currentBallPosition.x - center.x, -currentBallPosition.y, -1f);
                                        //ball.transform.Rotate(0f, speed, 0f);
                                        videoTransformation.transform.position = new Vector3(worldFrontHead.x, 0, 0);

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
                                        float xMovement = (float) (Math.Cos(-angleRad) * widthKamehamha / 2);
                                        float yMovement = (float) (Math.Sin(-angleRad) * widthKamehamha / 2);
                                        videoKamehameha.transform.rotation = Quaternion.Euler(0, 0, -angleDeg + 180);
                                        stringAngle = "Angle: ";
                                        stringAngle += -angleDeg;
                                        videoKamehameha.transform.position = new Vector3(worldRight.x + xMovement, -worldRight.y + yMovement, 0);
                                        videoKamehameha.transform.localScale = new Vector3(widthKamehamha, 4.5f, 1);



                                        //Show hair
                                        Renderer rendo = hair.GetComponent<Renderer>();
                                        rendo.enabled = false;
                                        hair.transform.position = new Vector3(worldFrontHead.x, -3, 0);
                                        changeColorOfGameObject(UnityEngine.Color.black, UnityEngine.Color.yellow, hair, ref flagHair, ref tHair);
                                        changeColorOfGameObject(UnityEngine.Color.yellow, UnityEngine.Color.blue, ball, ref flagBall, ref tBall);
                                    }
                                }
                            }
                        }
                        
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

        if(faceReader != null)
        {
            faceReader.Dispose();
        }

        if (HDfaceReader != null)
        {
            HDfaceReader.Dispose();
        }

        if (_faceModel != null)
        {
            _faceModel.Dispose();
        }

        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
        }
    }
}