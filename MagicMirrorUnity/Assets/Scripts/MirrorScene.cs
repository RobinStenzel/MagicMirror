using UnityEngine;
using System.Linq;
using Windows.Kinect;
using Microsoft.Kinect.Face;

public class MirrorScene : MonoBehaviour
{
    // Kinect members.
    private KinectSensor sensor;
    private ColorFrameReader colorReader;
    private BodyFrameReader bodyReader;
    private FaceFrameSource faceSource;
    private FaceFrameReader faceReader;
    private Body[] bodies;
    
    // Color frame display.
    private Texture2D texture;
    private byte[] pixels;
    private int width;
    private int height;

    // Visual elements.
    public GameObject quad;
    public GameObject ball;

    // Parameters
    public float scale = 2f;
    public float speed = 10f;
    public string expressionState = "idle";

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
                        if (faceReader != null)
                        {
                            using(var faceFrame = faceReader.AcquireLatestFrame())
                            {
                                if(faceFrame != null)
                                {
                                    FaceFrameResult result = faceFrame.FaceFrameResult;
                                    if(result != null)
                                    {
                                        // Detect Face Positions and Expressions
                                        var eyeLeft = result.FacePointsInColorSpace[FacePointType.EyeLeft];
                                        var eyeRight = result.FacePointsInColorSpace[FacePointType.EyeRight];
                                        var nose = result.FacePointsInColorSpace[FacePointType.Nose];
                                        var mouthLeft = result.FacePointsInColorSpace[FacePointType.MouthCornerLeft];
                                        var mouthRight = result.FacePointsInColorSpace[FacePointType.MouthCornerRight];

                                        var eyeLeftClosed = result.FaceProperties[FaceProperty.LeftEyeClosed];
                                        var eyeRightClosed = result.FaceProperties[FaceProperty.RightEyeClosed];
                                        var mouthOpen = result.FaceProperties[FaceProperty.MouthOpen];
                                        var happy = result.FaceProperties[FaceProperty.Happy];

                                        // Detect the hand (left or right) that is closest to the sensor.
                                        var handTipRight = body.Joints[JointType.HandTipRight].Position;
                                        var handTipLeft = body.Joints[JointType.HandTipLeft].Position;
                                        var closer = handTipRight.Z < handTipLeft.Z ? handTipRight : handTipLeft;

                                     
                                        // Map the 3D position of the hand to the 2D color frame (1920x1080).
                                        var pointRight = sensor.CoordinateMapper.MapCameraPointToColorSpace(handTipRight);
                                        var pointLeft = sensor.CoordinateMapper.MapCameraPointToColorSpace(handTipLeft);
                                        var positionRight = new Vector2(0f, 0f);
                                        var positionLeft = new Vector2(0f, 0f);

                                        if (!float.IsInfinity(pointRight.X) && !float.IsInfinity(pointRight.Y) && !float.IsInfinity(pointLeft.X) && !float.IsInfinity(pointLeft.Y))
                                        {
                                            positionRight.x = pointRight.X;
                                            positionRight.y = pointRight.Y;
                                            positionLeft.x = pointLeft.X;
                                            positionLeft.y = pointLeft.Y;
                                        }

                                        // Map the 2D position to the Unity space.
                                        var worldRight = Camera.main.ViewportToWorldPoint(new Vector3(positionRight.x / width, positionRight.y / height, 0f));
                                        var worldLeft = Camera.main.ViewportToWorldPoint(new Vector3(positionLeft.x / width, positionLeft.y / height, 0f));
                                        var centerHand = (worldRight + worldLeft) / 2;
                                        var center = quad.GetComponent<Renderer>().bounds.center;
                                        

                                        //Use Expressions to change behaviour
                                        if (eyeLeftClosed == DetectionResult.Yes || eyeLeftClosed == DetectionResult.Maybe)
                                        {
                                            expressionState = "leftEyeClosed";
                                        }
                                        if (eyeRightClosed == DetectionResult.Yes || eyeRightClosed == DetectionResult.Maybe)
                                        {
                                            expressionState = "rightEyeClosed";
                                            print("RIGGHT EYE CLOSED");
                                        }
                                        if (happy == DetectionResult.Yes || happy == DetectionResult.Maybe)
                                        {
                                            expressionState = "idle";
                                            print("happy");
                                        }


                                        var currentBallPosition = centerHand;

                                        switch (expressionState)
                                        {
                                            case "idle": currentBallPosition = centerHand;
                                                break;

                                            case "leftEyeClosed": currentBallPosition = worldLeft;
                                                break;

                                            case "rightEyeClosed": currentBallPosition = worldRight;
                                                break;

                                       
                                        }
                                        
                                        // Move and rotate the ball.
                                        ball.transform.localScale = new Vector3(scale, scale, scale) / closer.Z;
                                        ball.transform.position = new Vector3(currentBallPosition.x - 0.5f - center.x, -currentBallPosition.y + 0.5f, -1f);
                                        ball.transform.Rotate(0f, speed, 0f);
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

        if (sensor != null && sensor.IsOpen)
        {
            sensor.Close();
        }
    }
}