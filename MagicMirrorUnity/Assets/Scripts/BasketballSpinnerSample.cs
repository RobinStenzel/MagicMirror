using UnityEngine;
using System.Linq;
using Windows.Kinect;

public class BasketballSpinnerSample : MonoBehaviour
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
    public GameObject ball;

    // Parameters
    public float scale = 2f;
    public float speed = 10f;

    void Start()
    {
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
                        // Detect the hand (left or right) that is closest to the sensor.
                        var handTipRight = body.Joints[JointType.HandTipRight].Position;
                        var handTipLeft = body.Joints[JointType.HandTipLeft].Position;
                        var closer = handTipRight.Z < handTipLeft.Z ? handTipRight : handTipLeft;

                        // Map the 3D position of the hand to the 2D color frame (1920x1080).
                        var point = sensor.CoordinateMapper.MapCameraPointToColorSpace(closer);
                        var position = new Vector2(0f, 0f);
                        
                        if (!float.IsInfinity(point.X) && !float.IsInfinity(point.Y))
                        {
                            position.x = point.X;
                            position.y = point.Y;
                        }

                        // Map the 2D position to the Unity space.
                        var world = Camera.main.ViewportToWorldPoint(new Vector3(position.x / width, position.y / height, 0f));
                        var center = quad.GetComponent<Renderer>().bounds.center;

                        // Move and rotate the ball.
                        ball.transform.localScale = new Vector3(scale, scale, scale) / closer.Z;
                        ball.transform.position = new Vector3(world.x - 0.5f - center.x, -world.y + 0.5f, -1f);
                        ball.transform.Rotate(0f, speed, 0f);
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