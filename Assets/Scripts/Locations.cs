using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locations : MonoBehaviour
{
    public static Locations main;

    [Header("Sensors")]
    public bool use_gps;
    public bool use_compass;

    [Header("Origin of campus's coordinate system")]
    public double jessup_gps_latitude;
    public double jessup_gps_longitude;
    public float meter_to_unit_ratio_latitude;
    public float meter_to_unit_ratio_longitude;

    [Header("Bearing configurations")]
    public float bearing_max_vel;
    float bearing_smooth_vel;
    public float bearing_current{get; private set;}
    public float bearing_fake_rot; // For debugging use. Use left and right arrow to change it slowly.

    public bool loc_service_on{get; private set;}
    public static gps_status conn_status;
    public enum gps_status {Disabled, Unavailable, Connecting, TimeOut, ConnectUnable, Connected};
    public int gps_conn_max_wait = 10; // This is the max tries the system will attempt to connect to the device's GPS.
    public int gps_conn_cur_wait{get; private set;} // This is the number of tries remaining.

    [Header("Legacy functions")]
    public bool use_real_time_gps_tracking; // It was a legacy design. Until gps gives a more accurate position, I doubt it will be used.
    

    public double last_updated_time{get; private set;}
    public Vector3 displ{get; private set;} = Vector3.zero;
    Vector3 accel = Vector3.zero;

    // Just to test on laptop.
    // Note that fake gps will consider jessup_gps as origin. Hence, it's an offset.
    public bool use_fake_gps{get; private set;}

    void Awake()
    {
        main = this;
        GPSEncoder.SetLocalOrigin(new Vector2 ((float) jessup_gps_latitude, (float) jessup_gps_longitude));
    }
    Vector3 gyro_angle;
    // Start is called before the first frame update
    void Start()
    {
        if (use_gps) StartupGps();
        Input.gyro.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (loc_service_on)
        {
            last_updated_time = Input.location.lastData.timestamp;
            bearing_current = Mathf.SmoothDampAngle(bearing_current, Input.compass.magneticHeading, ref bearing_smooth_vel, bearing_max_vel);
            //GetBearing();
        } else if (use_fake_gps) {
            if (Input.GetKey("left")) bearing_fake_rot += 1f;
            if (Input.GetKey("right")) bearing_fake_rot -= 1f;
            bearing_current = Mathf.SmoothDampAngle(bearing_current, bearing_fake_rot, ref bearing_smooth_vel, bearing_max_vel);
        }
    }

    IEnumerator StartLocationService()
    {
        if (!Input.location.isEnabledByUser)
        {
            conn_status = gps_status.Unavailable;
            use_fake_gps = true;
            yield break;
        }

        Input.location.Start(1f,1f);

        conn_status = gps_status.Connecting;
        gps_conn_cur_wait = gps_conn_max_wait;
        while (Input.location.status == LocationServiceStatus.Initializing && gps_conn_cur_wait > 0)
        {
            yield return new WaitForSeconds(1);
            gps_conn_cur_wait--;
        }

        if (gps_conn_cur_wait < 1)
        {
            conn_status = gps_status.TimeOut;
            yield break;
        }

        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            conn_status = gps_status.ConnectUnable;
            yield break;
        }
        else
        {
            conn_status = gps_status.Connected;
            loc_service_on = true;
            Input.compass.enabled = true;
        }
    }



    public void StartupGps()
    {
        StartCoroutine(StartLocationService());
    }

    // Running this assumes that loc_service_on is true. Use ways to prevent it from running otherwise.
    public Vector3 GetPosByGps()
    {
        if (!loc_service_on) return Vector3.zero; // Just in case somebody can use it.

        displ = GPSEncoder.GPSToUCS(Input.location.lastData.latitude, Input.location.lastData.longitude) - 
                        GPSEncoder.GPSToUCS((float) jessup_gps_latitude, (float) jessup_gps_longitude);
        displ = new Vector3(displ.x / meter_to_unit_ratio_latitude, 0, displ.z / meter_to_unit_ratio_longitude);
        return new Vector3(displ.x, displ.z, 0);
    }

    // This is a huge waste of time.
    /*public float GetBearing()
    {
        float heading = Mathf.Atan2(Input.compass.rawVector.y, Input.compass.rawVector.x) * 180 / Mathf.PI;
        //print(string.Format("{0}, {1}", heading, Input.compass.magneticHeading));
        //print(string.Format("{0}, {1}, {2}", Input.compass.rawVector.x, Input.compass.rawVector.y, Input.compass.rawVector.z));
        //gyro_angle += Input.gyro.rotationRateUnbiased * Input.gyro.updateInterval;
        //print(string.Format("{0}, {1}, {2}", gyro_angle.x, gyro_angle.y, gyro_angle.z));
        //Quaternion rot = (new Quaternion(Input.gyro.attitude.x, Input.gyro.attitude.y, -Input.gyro.attitude.z, -Input.gyro.attitude.w)) * Quaternion.Euler(90f, 0f, 0f);
        //float roll = Mathf.Atan2(2*rot.x*rot.w - 2*rot.y*rot.z, 1 - 2*rot.x*rot.x - 2*rot.z*rot.z);
        //float pitch = -Mathf.Atan2(2*rot.y*rot.w - 2*rot.x*rot.z, 1 - 2*rot.y*rot.y - 2*rot.z*rot.z);
        //float yaw = Mathf.Asin(2*rot.x*rot.y + 2*rot.z*rot.w);

        Quaternion rot = Input.gyro.attitude;
        float bad_roll = Mathf.Atan2(2*(rot.y*rot.w-rot.x*rot.z), 1-2*(rot.y*rot.y+rot.z*rot.z));
        float bad_pitch = Mathf.Atan2(2*(rot.x*rot.w-rot.y*rot.z), 1-2*(rot.x*rot.x+rot.z*rot.z));
        float yaw = Mathf.Asin(2*(rot.x*rot.y+rot.z*rot.w));
        float roll = bad_pitch;
        float pitch = -bad_roll;

        print(string.Format("{0}, {1}, {2}", Input.compass.trueHeading, bad_pitch* 180 / Mathf.PI, yaw * 180 / Mathf.PI));


        float mag_x = Input.compass.rawVector.x * Mathf.Cos(pitch) + 
            Input.compass.rawVector.y * Mathf.Sin(roll) * Mathf.Sin(pitch) - 
            Input.compass.rawVector.z * Mathf.Cos(roll) * Mathf.Sin(pitch);
        float mag_y = Input.compass.rawVector.y * Mathf.Cos(roll) + 
            Input.compass.rawVector.z * Mathf.Sin(roll);
        float good_heading = Mathf.Atan2(mag_y, mag_x) * 180 / Mathf.PI;

        //print(good_heading);
        //print(string.Format("{0}, {1}, {2}", true_rot.eulerAngles.x, true_rot.eulerAngles.y, true_rot.eulerAngles.z));

        return 0;
    }*/
}