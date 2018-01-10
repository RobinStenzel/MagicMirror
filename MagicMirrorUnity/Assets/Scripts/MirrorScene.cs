﻿using UnityEngine;
using System.Linq;
using Windows.Kinect;
using Microsoft.Kinect.Face;
using UnityEngine.UI;

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

    // Parameters
    public float scale = 2f;
    public float speed = 10f;
    public string expressionState = "idle";

    //Strings for texbox
    public Text textboxLeft;
    private string stringEyeLeft = "";
    private string stringEyeRight = "";
    private string stringMouthOpen = "";
    private string stringHappy = "";
    private string stringGlasses = "";
    
    


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

        //Use Expressions to change behaviour
        if (eyeLeftClosed == DetectionResult.Yes || eyeLeftClosed == DetectionResult.Maybe)
        {
            expressionState = "leftEyeClosed";
            stringEyeLeft = "left eye: yes or maybe";
        }
        else
        {
            stringEyeLeft = "left eye: no";
        }
        if (eyeRightClosed == DetectionResult.Yes || eyeRightClosed == DetectionResult.Maybe)
        {
            expressionState = "rightEyeClosed";
            stringEyeRight = "right eye: yes or maybe";
        }
        else
        {
            stringEyeRight = "right eye: no";
        }
        if (happy == DetectionResult.Yes)
        {
            expressionState = "idle";
            stringHappy = "happy: yes";
        }
        else
        {
            stringHappy = "happy: no";

        }

    }
    void Start()
    {
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
        /*if (HDfaceReader != null)
        {
            using (var frame = HDfaceReader.AcquireLatestFrame())
            {
                if (frame != null && frame.IsFaceTracked)
                {
                    frame.GetAndRefreshFaceAlignmentResult(_faceAlignment);
                    UpdateFacePoints();
                }
            }
        }*/
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
                                        + stringEyeRight + System.Environment.NewLine + stringHappy;
                                        

                                        // Detect the hand (left or right) that is closest to the sensor.
                                        var handRight = body.Joints[JointType.HandTipRight].Position;
                                        var handLeft = body.Joints[JointType.HandTipLeft].Position;
                                        var closer = handRight.Z < handLeft.Z ? handRight : handLeft;

                                        // Map the 2D position to the Unity space.
                                        var worldRight = map3dPointTo2d(handRight);
                                        var worldLeft = map3dPointTo2d(handLeft);
                                        var worldLeftCheek = map3dPointTo2d(leftCheekBone);
                                        var worldRightCheek = map3dPointTo2d(rightCheekBone);
                                        var distanceCheeks = worldRightCheek.x - worldLeftCheek.x;
                                        var worldFrontHead = map3dPointTo2d(frontHeadCenter);
                                        var worldCloser = map3dPointTo2d(closer);

                                        var centerHand = (worldRight + worldLeft) / 2;
                                        var center = quad.GetComponent<Renderer>().bounds.center;
                                        

                                        

                                        var currentBallPosition = centerHand;

                                        switch (expressionState)
                                        {
                                            case "idle":
                                                currentBallPosition = centerHand;
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

                                        //Show hair
                                        //hair.transform.localScale = new Vector3(distanceCheeks*400, distanceCheeks*400, distanceCheeks*400);
                                        //hair.transform.position = new Vector3(worldFrontHead.x - center.x -1f*distanceCheeks, -worldFrontHead.y- distanceCheeks*3.8f, -1f);
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