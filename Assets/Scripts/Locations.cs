using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Locations : MonoBehaviour
{
    public static Locations main;

    public bool use_gps;
    public bool use_compass;
    public Vector3 jessup_gps_location; // For altitude, just choose a point and that's our ground level.
    public double jessup_gps_latitude;
    public double jessup_gps_longitude;
    public float bearing_max_vel;
    float bearing_smooth_vel;
    public float bearing_current{get; private set;}
    public float bearing_fake_rot; // For debugging use. Use left and right arrow to change it slowly.

    public bool use_real_time_gps_tracking; // It was a legacy design. Until gps gives a more accurate position, I doubt it will be used.

    public bool loc_service_on{get; private set;}
    public static gps_status conn_status;
    public enum gps_status {Disabled, Unavailable, Connecting, TimeOut, ConnectUnable, Connected};
    public int gps_conn_max_wait = 10; // This is the max tries the system will attempt to connect to the device's GPS.
    public int gps_conn_cur_wait{get; private set;} // This is the number of tries remaining.

    public double last_updated_time{get; private set;}
    public Vector3 displ{get; private set;} = Vector3.zero;
    Vector3 accel = Vector3.zero;

    // Just to test on laptop.
    // Note that fake gps will be used with jessup_gps_location as origin. Hence 0,0 isn't rlly 0,0 in gps reading.
    //public Vector3 fake_gps; // x: lati, y: alti, z: long
    public bool use_fake_gps{get; private set;}

    void Awake()
    {
        main = this;
        GPSEncoder.SetLocalOrigin(new Vector2 ((float) jessup_gps_latitude, (float) jessup_gps_longitude));
    }

    // Start is called before the first frame update
    void Start()
    {
        if (use_gps) StartupGps();
        //Input.gyro.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (loc_service_on)
        {
            last_updated_time = Input.location.lastData.timestamp;
            bearing_current = Mathf.SmoothDampAngle(bearing_current, Input.compass.trueHeading, ref bearing_smooth_vel, bearing_max_vel);

        } else if (use_fake_gps) {
            if (Input.GetKey("left")) bearing_fake_rot += 1f;
            if (Input.GetKey("right")) bearing_fake_rot -= 1f;
            bearing_current = Mathf.SmoothDampAngle(bearing_current, bearing_fake_rot, ref bearing_smooth_vel, bearing_max_vel);
        }

        /*
        accel = Input.gyro.userAcceleration;
        displ += accel * 0.5f * Time.deltaTime * Time.deltaTime;

        cur_accel_txt_update_time += Time.deltaTime;
        if (cur_accel_txt_update_time >= accel_txt_update_time)
        {
            cur_accel_txt_update_time = 0;
            accel_txt.text = string.Format("Accel: x:{0:0.0####},\n y:{1:0.0####}, \nz:{2:0.0####}", accel.x, accel.y, accel.z);
            displ_txt.text = string.Format("Disp: x:{0:0.0####}, \ny:{1:0.0####}, \nz:{2:0.0####}", displ.x, displ.y, displ.z);
            time_disp_2.text = string.Format("{0}:{1}:{2}",t.Hour,t.Minute,t.Second);
        }*/
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
        return new Vector3(displ.x, displ.z, 0);
    }
}